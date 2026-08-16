//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航加载器基类
//@Description 封装通用属性和方法，为导航系统操作“场景、界面”的抽象基类
//**************************************************************************************

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.View;
using Framework.View.Navigation;

namespace Framework.View.Navigation
{
    public abstract class NavigationLoader : NavigationBehaviour, INavigateLoader
    {
        #region public properties

        /// <summary>
        /// 所属导航容器对象，方便外部访问
        /// </summary>
        public NavigateContainer ParentContainer { get; internal set; }

        /// <summary>
        /// 导航加载器对应的View
        /// </summary>
        public ViewBase View { get; protected set; }

        /// <summary>
        /// 加载和打开View的参数
        /// </summary>
        public abstract INavigateOptions ViewOptions { get; internal set; }

        /// <summary>
        /// 此View的管理器引用
        /// </summary>
        public IViewManager ViewManager { get; internal set; }

        /// <summary>
        /// Open和Show数据
        /// </summary>
        public object OpenShowData { get; set; }

        /// <summary>
        /// 是否为父级导航的入口
        /// </summary>
        public bool Entrance { get; internal set; }

        /// <summary>
        /// 导航模式
        /// </summary>
        public NavigationMode Mode { get; set; } = NavigationMode.OpenAndJump;

        /// <summary>
        /// 是否处于转场流程中
        /// </summary>
        public override bool Transitioning => Transition is { IsTransitioning: true };

        /// <summary>
        /// 转场对象
        /// </summary>
        public ITransition Transition => ViewOptions.Transition;

        /// <summary>
        /// 转场控制组件
        /// </summary>
        public TransitionViewComponent TransitionComponent { get; private set; }

        /// <summary>
        /// 渲染层级，用于多 View 堆叠时的画面构成分析。
        /// 对于无层级概念的子类（如 Scene），返回 <see cref="int.MinValue"/> 表示所有 Form 都在其之上。
        /// Rendering sort layer used for multi-View composition analysis.
        /// Subclasses without a layer concept (e.g. Scene) should return <see cref="int.MinValue"/>.
        /// </summary>
        public abstract int Layer { get; }

        #endregion

        #region public: 方法

        public async UniTask<TView> OpenViewAsync<TView>() where TView : ViewBase
        {
            var viewable = await OpenViewAsync();
            return viewable as TView;
        }

        public async UniTask<ViewBase> OpenViewAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Log.Error($"{GetType().Name} Name 不能为空或全空白，" +
                          $"请检查是否正确设置了导航加载器的 Name 属性！");
                return null;
            }

            if (Mode == 0)
            {
                Log.Error($"{GetType().Name}('{Name}') Mode未初始化，" +
                          $"容错处理：设置为 {nameof(NavigationMode.OpenAndJump)}！");
                Mode = NavigationMode.OpenAndJump;
            }

            // 验证是否能打开
            var canOpen = ParentContainer.ValidateLoaderOpen(this);
            if (!canOpen) return null;

            // 开始状态改变。
            // 这里开启 forceReentry (强制重入)，原因如下：
            // 1. 业务预期：即使 Loader 已经是 Open 状态，再次调用 Open 也需要重新触发完整流程（如 ParentContainer.BeforeLoaderOpen、SetLogicalVisible(true) 等）。
            // 2. 状态保护：通过进入 PreChangeStateAsync 流程，可以将 PendingState 设为 Open，从而在执行后续异步加载/初始化逻辑时，
            //    利用状态锁机制阻止其它并发的 Close 或 Clear 操作，确保线程安全。
            var ok = await PreChangeStateAsync(NavigationStateType.Open, true);
            if (!ok)
            {
                Log.Error($"无法打开 '{Name}'! {GetType().Name}('{Name}') 状态改变失败，无法执行 OpenViewAsync！" +
                          $"当前状态: {CurrentState}, Pending状态: {PendingState}");
                return View;
            }

            try
            {
                OpenCancellation ??= new CancellationTokenSource();
                var cancellationToken = OpenCancellation.Token;

                // 执行Parent容器操作
                await ParentContainer.BeforeLoaderOpen(this, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // 添加转场组件
                AddTransitionComponent(cancellationToken);

                var view = View;
                var alreadyOpen = view != null;
                if (!alreadyOpen)
                {
                    view = await DoOpenOnly(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (view != null)
                    {
                        Log.Debug($"{GetType().Name}('{Name}')成功打开 View: {view}");
                        View = view;
                    }
                    else
                    {
                        Log.Error($"Field to open '{Name}'! {GetType().Name}('{Name}')打开 View失败!");
                    }
                }
                else
                {
                    view.OpenShowData = OpenShowData;

                    // View 已打开但可能被隐藏，重新显示
                    await view.SetLogicalVisible(true);
                }
            }
            catch (OperationCanceledException canceledException)
            {
                Log.Debug($"{GetType().Name}('{Name}') OpenViewAsync 过程中被取消: {canceledException}");
                OpenCancellation = null;
            }
            finally
            {
                await PostChangeStateAsync(NavigationStateType.Open);
            }

            return View;
        }

        public override UniTask SetLogicalVisible(bool visible)
        {
            if (PendingState is NavigationStateType.Open or NavigationStateType.Close ||
                CurrentState is NavigationStateType.Close ||
                LockType.HasFlag(NavigationLockType.SetLogicalVisible))
            {
                return UniTask.CompletedTask;
            }

            if (View == null)
            {
                throw new Exception($"View ('{Name}') is null!");
            }

            return View.SetLogicalVisible(visible);
        }

        #endregion

        #region protected 生命周期方法

        protected override async UniTask LifeCycleDoClose(LifeCycleArgs args)
        {
            // 移除转场组件（因为转场只在Open和Show时起作用，到Close就已经没用了，所以要移除）
            RemoveTransitionComponent();

            // 此时View可能还没加载出来，所以要判断一下
            if (View != null)
            {
                if (ViewManager == null)
                {
                    Log.Error($"{GetType().Name}('{Name}')在关闭时，" +
                              $"发现{nameof(ViewManager)}为null，无法执行关闭！");
                }
                else
                {
                    await ViewManager.LifeCycleExecuteClose(View, args);
                }

                View = null;
            }
        }

        /// <summary>
        /// 打开View，仅执行Open逻辑（不执行Show）
        /// </summary>
        /// <returns>返回打开的View对象（如果打开失败或过程中被取消，则返回null）</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual async UniTask<ViewBase> DoOpenOnly(CancellationToken cancellationToken = default)
        {
            if (View != null) return View;

            try
            {
                // 手动开始转场效果：
                //      因为View打开之前可能会先关闭其他View，此时新View还没打开，画面显示可能异常
                //      所以先开始转场效果，确保整个过程有转场效果，避免画面异常
                TransitionComponent?.ManualStart();

                // 将自己作为生命周期执行器传给ViewOptions，提供View在生命周期方法中调用
                ViewOptions.LifeCycleExecutor = this;

                // 通过View管理器打开View
                var view = await ViewManager.OpenAsync(ViewOptions, cancellationToken);
                View = view;

                if (view != null)
                {
                    view.name = $"[{ParentContainer.Name}] {Name}"; // 界面名字前面加上导航容器名字，方便调试
                }
            }
            catch (Exception e)
            {
                OnError(e);
            }

            return View;
        }

        protected override void DoClear() // FIXME by fred 改名
        {
            if (View != null)
            {
                ((IViewLifeCycle)View).ExecuteClear();
            }
        }

        #endregion

        /// <summary>
        /// 保存
        /// </summary>
        /// <returns>保存返回数据</returns>
        internal virtual object Save()
        {
            try
            {
                object saveData = null;

                // 此时View可能还没加载出来，所以不调用AssertViewExist
                if (View != null)
                {
                    saveData = ((IViewLifeCycle)View).ExecuteSave();
                }

                return saveData;
            }
            catch (Exception e)
            {
                OnError(e);
                return null;
            }

            // var (_, saveData) = TryCall(() =>
            // {
            //     AssertFormExist();
            //     return Form.Save();
            // });
            // return saveData;
        }

        // /// <summary>
        // /// 复制Loader信息
        // /// </summary>
        // /// <param name="loader"></param>
        // internal abstract void CopyInfo(NavigationLoader loader);

        protected override async UniTask<bool> PreChangeStateAsync(NavigationStateType targetState, bool forceReentry = false)
        {
            var result = await base.PreChangeStateAsync(targetState, forceReentry);
            if (!result) return false;

            try
            {
                if (ParentContainer != null)
                    await ParentContainer.BeforeLoaderChangeStateAsync(this, targetState);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            return true;
        }

        protected override async UniTask PostChangeStateAsync(NavigationStateType targetState)
        {
            await base.PostChangeStateAsync(targetState);

            try
            {
                if (ParentContainer != null)
                {
                    await ParentContainer.AfterLoaderStateChange(this, targetState);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// 还原前设置
        /// </summary>
        internal virtual void BeforeSetRecover()
        {
        }

        #region IViewLifeCycleExecutor implementation

        UniTask IViewLifeCycleExecutor.LifeCycleExecuteClose(IViewLifeCycle view, LifeCycleArgs args)
        {
            if ((ViewBase)view != View)
            {
                Log.Error($"{GetType().Name}.{nameof(IViewLifeCycleExecutor.LifeCycleExecuteClose)}('{Name}') 错误，" +
                          $"传入的参数View({view})与实际View({View})不匹配");
                return UniTask.CompletedTask;
            }

            return LifeCycleExecuteCloseState(args);
        }

        #endregion

        #region protected

        /// <summary>
        /// 在ViewOptions上添加一个控制转场效果的组件 TransitionViewComponent
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected void AddTransitionComponent(CancellationToken cancellationToken)
        {
            var transition = Transition;
            if (transition is null or TransitionNoOp) return;

            var components = ViewOptions.Components;
            if (components == null)
            {
                components = new List<IViewComponent>(2);
                ViewOptions.Components = components;
            }

            var transitionComponent = TransitionComponent;
            if (transitionComponent == null)
            {
                transitionComponent = new TransitionViewComponent(transition, cancellationToken);
                TransitionComponent = transitionComponent;
            }
            else
            {
                transitionComponent.Init(transition, cancellationToken);
            }

            if (!components.Contains(transitionComponent))
            {
                components.Add(TransitionComponent);
            }
        }

        /// <summary>
        /// 从View和ViewOptions上移除转场组件
        /// </summary>
        protected void RemoveTransitionComponent()
        {
            if (TransitionComponent == null) return;

            // 仅移除，不销毁TransitionComponent对象，因为可能在下次打开时继续使用，避免重复创建销毁
            ViewOptions.Components?.Remove(TransitionComponent);
            View?.RemoveViewComponent(TransitionComponent);
            TransitionComponent.Reset();
        }

    #endregion

        /// <summary>
        /// 重置设置
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            ParentContainer = null;
            View = null;
            ViewOptions?.Reset();
            OpenShowData = null;
            Entrance = false;
            Mode = NavigationMode.OpenAndJump;
        }
    }
}
