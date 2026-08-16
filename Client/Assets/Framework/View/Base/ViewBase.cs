// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using Framework.Log;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;

namespace Framework.View
{
    /// <summary>
    /// ViewBase：界面和场景的显示基类
    /// </summary>
    public abstract class ViewBase : ViewObject, IViewLifeCycle
    {
        #region public: 属性

        /// <summary>
        /// 调试名称
        /// 仅用于开发环境（调试、工具辅助、配置衔接等）
        /// </summary>
        public string DebugName => _debugName;

        /// <summary>
        /// 界面Open和Show时的自定义数据
        /// </summary>
        public object OpenShowData { get; set; }

        /// <summary>
        /// View 当前已完成的生命周期阶段（稳态）
        /// </summary>
        public ViewPhase CurrentPhase { get; protected set; } = ViewPhase.None;

        /// <summary>
        /// View 正在过渡到的目标阶段（None 表示无过渡中）
        /// </summary>
        public ViewPhase PendingPhase { get; protected set; } = ViewPhase.None;

        /// <summary>
        /// 是否正在执行生命周期过渡（PendingPhase 不为 None）
        /// </summary>
        public bool IsPhaseChanging => PendingPhase != ViewPhase.None;

        /// <summary>
        /// 是否处于正常打开状态（运行中）
        /// CurrentPhase 处于 Opened ~ Closed 之间（不含 Closed）
        /// </summary>
        public override bool Running => (CurrentPhase is >= ViewPhase.Opened and < ViewPhase.Closed)
                                        || PendingPhase is ViewPhase.Opened;

        /// <summary>
        /// 逻辑显示状态（给常规业务用的逻辑状态）
        /// </summary>
        public bool LogicalVisible => Running && GetVisibleState(LogicalVisibleController);

        /// <summary>
        /// 当前是否被渲染（真正的是否可见，给一些底层机制使用，业务上应该用LogicalVisible属性）
        /// </summary>
        public bool Rendering => CurrentPhase == ViewPhase.Shown && LogicalVisible;

        /// <summary>
        /// 控制此View的显隐状态的控制器列表
        /// </summary>
        public List<KeyValuePair<VisibleController, VisibleControllerState>> VisibilityControllers { get; private set; }

        /// <summary>
        /// 逻辑可见性控制器
        /// </summary>
        public abstract VisibleController LogicalVisibleController { get; }

        /// <summary>
        /// View显示和隐藏时的默认行为
        /// 用途1：在View开始Open->Show之间，不渲染View，避免还未准备好就显示
        /// 用途2：在View的Show->Hide->Show互相转换之间，控制View的渲染状态
        /// </summary>
        public IVisibleStrategy DefaultVisibleStrategy { get; protected set; }

        /// <summary>
        /// 视图View所属的生命周期循环驱动器
        /// </summary>
        public IViewLifeCycleExecutor LifeCycleExecutor { get; internal set; }

        #endregion

        #region public: 生命周期事件

        /// <summary>
        /// Static生命周期事件：全局共享，监听所有ViewBase实例的生命周期
        ///
        /// 用法（以PreOpen为例）：
        ///   持久监听：ViewBase.StaticLifeCycleEvents.PreOpen.Add(callback)
        ///   一次性监听：ViewBase.StaticLifeCycleEvents.PreOpen.AddOnce(callback)
        ///   取消监听：ViewBase.StaticLifeCycleEvents.PreOpen.Remove(callback)
        /// </summary>
        public static ViewLifeCycleEvents StaticLifeCycleEvents { get; } = new();

        /// <summary>
        /// 实例生命周期事件：每个ViewBase实例独立，仅监听当前实例的生命周期
        ///
        /// 用法（以PostClose为例）：
        ///   一次性关闭回调（不带View）：view.InstanceLifeCycleEvents.PostClose.AddOnce(callback)
        ///   一次性关闭回调（带View）：view.InstanceLifeCycleEvents.PostClose.AddOnce(callbackWithView)
        /// </summary>
        public ViewLifeCycleEvents InstanceLifeCycleEvents => _instanceLifeCycleEvents ??= new ViewLifeCycleEvents();

        private ViewLifeCycleEvents _instanceLifeCycleEvents;

        #endregion

        #region public: 其它实例事件

        /// <summary>
        /// 触发时机：在SetVisibleState中改变了控制器可见性状态之后
        /// </summary>
        public event Action<ViewBase, VisibleController, VisibleControllerState> VisibleControllerChanged;

        /// <summary>
        /// 触发时机：Show和Hide生命周期流程之后，Rendering状态变化
        /// </summary>
        public event Action<ViewBase> RenderingChanged;

        #endregion

        #region public：应用层主动调用/请求（关闭/隐藏/显示）

        public void Close()
        {
            RequestClose();
        }

        /// <summary>
        /// 主动请求关闭View，不等待关闭流程结束
        /// </summary>
        public void RequestClose()
        {
            if (!Running) return;

            // 调用上下文对象来关闭View
            var args = new LifeCycleArgs(LifeCycleCause.RequestedClose);
            LifeCycleExecutor.LifeCycleExecuteClose(this, args).Forget();
        }

        /// <summary>
        /// 主动请求关闭View，等待关闭流程结束
        /// </summary>
        /// <param name="data">可选参数，关闭时传递的数据</param>
        /// <param name="cancelToken">可选参数，取消令牌</param>
        public UniTask RequestCloseAsync(object data = null, CancellationToken cancelToken = default)
        {
            if (!Running) return UniTask.CompletedTask;

            // 调用上下文对象来关闭View
            var args = new LifeCycleArgs(LifeCycleCause.RequestedClose, data, cancelToken);
            return LifeCycleExecutor.LifeCycleExecuteClose(this, args);
        }

        /// <summary>
        /// 请求显示View
        /// 仅设置逻辑层可见性，最终是否显示取决于所有 VisibleController 的状态
        /// （例如渲染屏蔽系统可能仍会阻止显示）
        /// </summary>
        public UniTask RequestShow()
        {
            return SetLogicalVisible(true);
        }

        /// <summary>
        /// 请求隐藏View
        /// 仅设置逻辑层可见性，其他 VisibleController 的状态不受影响
        /// </summary>
        public UniTask RequestHide()
        {
            return SetLogicalVisible(false);
        }


        /// <summary>
        /// 设置逻辑层可见性
        ///
        /// 注意：
        ///     View是否显示或隐藏，由多个系统共同控制
        ///     只有当参与控制的系统全部都设置显示，那么View才会真的显示
        ///     详见API：SetVisibleState
        /// </summary>
        /// <param name="value">true表示显示，false表示隐藏</param>
        public UniTask SetLogicalVisible(bool value)
        {
            return SetVisibleState(LogicalVisibleController, value);
        }

        /// <summary>
        /// 设置View的显隐控制器状态
        ///
        /// 只要任意控制器设置状态为"false隐藏"，则View不显示
        /// 当所有控制器都为"true显示"，即未设置"false隐藏"，则View要显示
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="expectedState">true表示显示，false表示隐藏</param>
        /// <returns>返回值表示此次调用是否引起实际变化</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public UniTask<bool> SetVisibleState(VisibleController controller, bool expectedState)
        {
            if (PendingPhase == ViewPhase.Closed || CurrentPhase == ViewPhase.Closed)
            {
                Log.Error($"Can't set visible state when '{AssetName}' is closing/closed! " +
                          $"CurrentPhase:{CurrentPhase}, PendingPhase:{PendingPhase}, " +
                          $"controller:{controller.Name}, expectedState:{expectedState}", this);
                return UniTask.FromResult(false);
            }

            // 先加入到控制器列表
            var index = 0;
            var controllers = VisibilityControllers;
            if (controllers == null)
            {
                throw new NullReferenceException($"{name}: {nameof(VisibilityControllers)} is null");
            }

            foreach (var pair in controllers)
            {
                if (pair.Key == controller) break;
                ++index;
            }

            if (index >= controllers.Count)
            {
                var pair = new KeyValuePair<VisibleController, VisibleControllerState>(
                    controller, VisibleControllerState.Default);
                controllers.Add(pair);
            }

            // 如果要设置的状态不变，则返回false（表示未产生变化）
            var state = controllers[index].Value;
            if (state.ExpectedState == expectedState)
            {
                return UniTask.FromResult(false);
            }

            // 再修改期望状态
            Log.Debug($"{AssetName}, controller:{controller.Name}, " +
                      $"ExpectedState: {state.ExpectedState} -> {expectedState}, " +
                      $"CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
            state.ExpectedState = expectedState;
            controllers[index] = new KeyValuePair<VisibleController, VisibleControllerState>(controller, state);

            // 这里先抛出事件，让其他控制器可以响应处理逻辑
            // 其他控制器可能执行连锁反应逻辑，继续修改状态
            // 等所有控制器都反应完毕后，下面再调用RefreshRendering
            try
            {
                VisibleControllerChanged?.Invoke(this, controller, state);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            // （根据期望）刷新渲染状态
            return RefreshRendering();
        }

        /// <summary>
        /// 获取控制器对应的可见性状态
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public bool GetVisibleState(VisibleController controller)
        {
            foreach (var pair in VisibilityControllers)
            {
                if (pair.Key == controller) return pair.Value.ExpectedState;
            }

            // 如果此View不受目标controller控制，则返回默认值true
            return true;
        }

        /// <summary>
        /// 排除指定控制器，计算是否可见
        /// </summary>
        /// <param name="except">要排除的控制器</param>
        /// <returns></returns>
        public bool IsVisibleExcept(VisibleController except = null)
        {
            foreach (var pair in VisibilityControllers)
            {
                // 排除指定控制
                if (pair.Key == except) continue;

                // 只要有任何一个控制器不可见，则整个View就不可见
                if (!pair.Value.ExpectedState) return false;
            }

            return true;
        }

        #endregion


        #region protected: 内部状态

        /// <summary>
        /// 调试名称，设计目的用于开发环境调试、工具辅助、配置衔接等
        /// </summary>
        [SerializeField, Header("调试名称(中文名，仅用于开发环境调试/配置等)")]
        protected string _debugName = "";

        /// <summary>
        /// 等待执行生命周期操作的 FIFO 队列（参考 NavigationBehaviour.PreChangeStateAsync）
        /// </summary>
        private readonly Queue<int> _lifecycleWaiters = new();

        /// <summary>
        /// 自增的等待者ID，用于区分同一目标的不同请求
        /// </summary>
        private int _nextLifecycleWaiterId;

        /// <summary>
        /// 代次计数器，每次取消所有等待者时自增
        /// </summary>
        private int _lifecycleGeneration;

        /// <summary>
        /// 当前是否有生命周期操作正在执行
        /// <para>true: 有操作正在执行（ExecuteOpen/ExecuteClose/RefreshRendering 持有 slot），
        /// 新调用进入 FIFO 队列等待</para>
        /// <para>false: 无操作执行中，新调用直接获取执行权
        /// （若队列中有其他等待者则按顺序排队）</para>
        /// </summary>
        private bool _lifecycleActive;

        /// <summary>
        /// 等待生命周期操作 slot 的最大帧数，超时后强制继续（~5秒@60fps）
        /// </summary>
        private const int LIFECYCLE_WAIT_MAX_FRAMES = 300;

        /// <summary>
        /// 打开(Opened)阶段收到隐藏请求时置位，Open 完成后跳过首次 Show，保持不渲染
        /// </summary>
        private bool _pendingHide;

        #endregion

        #region protected: Monobehaviour生命周期

        protected override void Awake()
        {
            Log.Debug($"Awake begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
            base.Awake();

            StaticLifeCycleEvents.PreAwake.Invoke(this);

            OnViewAwake();

            StaticLifeCycleEvents.PostAwake.Invoke(this);
            Log.Debug($"Awake end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        protected override void OnDestroy()
        {
            Log.Debug($"OnDestroy begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            CancelAllLifecycleWaiters();

            StaticLifeCycleEvents.PreDestroy.Invoke(this);
            _instanceLifeCycleEvents?.PreDestroy.Invoke(this);

            OnViewDestroy();

            base.OnDestroy();

            StaticLifeCycleEvents.PostDestroy.Invoke(this);
            _instanceLifeCycleEvents?.PostDestroy.Invoke(this);

            Log.Debug($"OnDestroy end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        protected abstract void OnViewAwake();
        protected abstract void OnViewDestroy();

        #endregion

        #region protected: 供子类重写的生命周期方法

        /// <summary>
        /// OnOpen流程最开始调用
        /// </summary>
        protected virtual async UniTask OnPreOpenAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPreOpenAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 初始状态不渲染View，在OnPreShowAsync中设置为渲染，避免在打开过程中还未准备好导致画面表现异常
            DefaultVisibleStrategy ??= CreateDefaultVisibleStrategy();
            DefaultVisibleStrategy?.SetVisible(this, false);

            VisibilityControllers ??= CollectionPool<List<KeyValuePair<VisibleController, VisibleControllerState>>,
                KeyValuePair<VisibleController, VisibleControllerState>>.Get();

            // 执行所有生命周期PreOpen事件回调
            await StaticLifeCycleEvents.PreOpen.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PreOpen.InvokeAsync(this);

            // 执行所有component的OnPreOpenAsync方法
            var components = CopyViewComponents<IViewOpenComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPreOpenAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            Log.Debug($"OnPreOpenAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// 用于执行打开时的同步初始化逻辑，如初始化数据、注册事件等
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnOpen(LifeCycleArgs args)
        {
            Log.Debug($"OnOpen begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnOpen方法
            var components = CopyViewComponents<IViewOpenComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    try
                    {
                        component.OnViewOpen(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            OnOpen(args.Data);

            Log.Debug($"OnOpen end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// 这是一个简化了参数的OnOpen方法，用于保持对原来OnOpen的兼容
        /// </summary>
        /// <param name="data"></param>
        protected virtual void OnOpen(object data = null)
        {
        }


        protected virtual async UniTask OnPostOpenAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPostOpenAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnPostOpenAsync方法
            var components = CopyViewComponents<IViewOpenComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPostOpenAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            await StaticLifeCycleEvents.PostOpen.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PostOpen.InvokeAsync(this);

            Log.Debug($"OnPostOpenAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// View显示前，需要执行的异步逻辑
        ///
        /// 注意：此方法中的异步逻辑执行完毕，View才会真正显示
        /// </summary>
        /// <returns></returns>
        protected virtual async UniTask OnPreShowAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPreShowAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            await StaticLifeCycleEvents.PreShow.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PreShow.InvokeAsync(this);

            // 执行所有component的OnPreShowAsync方法
            var components = CopyViewComponents<IViewShowComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPreShowAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            try
            {
                // 调用各个Visible控制器设置为可见状态
                ExecuteVisibleChange(true);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            Log.Debug($"OnPreShowAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// View显示时，需要执行的同步逻辑
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnShow(LifeCycleArgs args)
        {
            Log.Debug($"OnShow begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnShow方法
            var components = CopyViewComponents<IViewShowComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    try
                    {
                        component.OnViewShow(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            OnShow(args.Data, args.IsOpen);

            Log.Debug($"OnShow end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// 这是一个简化了参数的OnShow方法，用于保持对原来OnShow的兼容
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isOpen"></param>
        protected virtual void OnShow(object data, bool isOpen)
        {
        }

        /// <summary>
        /// View显示后，需要执行的异步逻辑
        /// </summary>
        /// <returns></returns>
        protected virtual async UniTask OnPostShowAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPostShowAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}, " +
                      $"VisibilityControllers={VisibilityControllers}", this);

            // 执行所有component的OnPostShowAsync方法
            var components = CopyViewComponents<IViewShowComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPostShowAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            await StaticLifeCycleEvents.PostShow.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PostShow.InvokeAsync(this);

            Log.Debug(
                $"OnPostShowAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}, VisibilityControllers={VisibilityControllers}",
                this);
        }

        /// <summary>
        /// View隐藏前的异步方法
        /// </summary>
        /// <param name="args">生命周期参数</param>
        protected virtual async UniTask OnPreHideAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPreHideAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
            await StaticLifeCycleEvents.PreHide.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PreHide.InvokeAsync(this);

            // 执行所有component的OnPreHideAsync方法
            var components = CopyViewComponents<IViewHideComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPreHideAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            Log.Debug($"OnPreHideAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// View变为隐藏状态，子类可以在OnHide中编写同步处理逻辑
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnHide(LifeCycleArgs args)
        {
            Log.Debug($"OnHide begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnHide方法
            var components = CopyViewComponents<IViewHideComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    try
                    {
                        component.OnViewHide(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            OnHide(args.IsClose);

            Log.Debug($"OnHide end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// 这是一个简化了参数的OnHide方法，用于保持对原来OnHide的兼容
        /// </summary>
        /// <param name="isClose"></param>
        protected virtual void OnHide(bool isClose)
        {
        }

        protected virtual async UniTask OnPostHideAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPostHideAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnPostHideAsync方法
            var components = CopyViewComponents<IViewHideComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPostHideAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            try
            {
                // 调用各个Visible控制器设置为不可见状态
                ExecuteVisibleChange(false);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            await StaticLifeCycleEvents.PostHide.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PostHide.InvokeAsync(this);

            Log.Debug($"OnPostHideAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }


        /// <summary>
        /// 保存View数据
        /// </summary>
        /// <returns></returns>
        protected virtual object OnSave()
        {
            Log.Debug($"OnSave: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
            return null;
        }

        /// <summary>
        /// 清理View数据
        /// </summary>
        /// <returns></returns>
        protected virtual void ClearSave()
        {
            Log.Debug($"ClearSave: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        protected virtual async UniTask OnPreCloseAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPreCloseAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnPreCloseAsync方法
            var components = CopyViewComponents<IViewCloseComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPreCloseAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            await StaticLifeCycleEvents.PreClose.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PreClose.InvokeAsync(this);

            Log.Debug($"OnPreCloseAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// 关闭时调用
        /// </summary>
        protected virtual void OnClose(LifeCycleArgs args)
        {
            Log.Debug($"OnClose begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnClose方法
            var components = CopyViewComponents<IViewCloseComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    try
                    {
                        component.OnViewClose(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            Log.Debug($"OnClose end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        /// <summary>
        /// View关闭时，需要执行的异步逻辑
        ///
        /// 这个异步逻辑执行完毕后，View才会真正关闭
        /// </summary>
        /// <returns></returns>
        protected virtual async UniTask OnPostCloseAsync(LifeCycleArgs args)
        {
            Log.Debug($"OnPostCloseAsync begin: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行所有component的OnPostCloseAsync方法
            var components = CopyViewComponents<IViewCloseComponent>();
            if (components != null)
            {
                args.CancelToken.ThrowIfCancellationRequested();
                foreach (var component in components)
                {
                    try
                    {
                        await component.OnPostCloseAsync(args);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }

                RecycleComponentList(components);
            }

            CollectionPool<List<KeyValuePair<VisibleController, VisibleControllerState>>,
                KeyValuePair<VisibleController, VisibleControllerState>>.Release(VisibilityControllers);
            VisibilityControllers = null;

            await StaticLifeCycleEvents.PostClose.InvokeAsync(this);
            if (_instanceLifeCycleEvents != null) await _instanceLifeCycleEvents.PostClose.InvokeAsync(this);

            LifeCycleExecutor = null;

            Log.Debug($"OnPostCloseAsync end: {name}, CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
        }

        #endregion

        #region IViewLifeCycle流程方法

        /// <summary>
        /// 开启：设置状态、启用内部定时器、触发OnOpen回调等
        /// </summary>
        /// <param name="args"></param>
        async UniTask IViewLifeCycle.ExecuteOpen(LifeCycleArgs args)
        {
            if ((CurrentPhase != ViewPhase.None && CurrentPhase != ViewPhase.Closed) || IsPhaseChanging)
            {
                GameLog.Error($"{name} already in CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}," +
                               $" shouldn't call Open(...) again!", module: "Framework.View");
                return;
            }

            // 重新检查（等待期间状态可能已变）
            if ((CurrentPhase != ViewPhase.None && CurrentPhase != ViewPhase.Closed) || IsPhaseChanging)
                return;

            var data = args.Data;
            OpenShowData = data;

            var slotOk = await AcquireLifecycleSlot();
            if (this == null) return; // 等待期间对象可能被销毁（如 Shutdown 中 Destroy UI 根）
            if (!slotOk)
            {
                Log.Error($"Failed to acquire lifecycle slot for opening '{name}'! " +
                          $"CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
            }

            try
            {
                // 在 PreOpen 之前设置 PendingPhase，集中管理状态转变
                PendingPhase = ViewPhase.Opened;
                Log.Debug($"ExecuteOpen: {name}, {CurrentPhase} -> {PendingPhase}", this);

                try
                {
                    await OnPreOpenAsync(args);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }

                // 在调用 OnOpen 前完成状态转变，确保 OnOpen 及其之后的回调中 CurrentPhase 已处于 Opened
                if (CurrentPhase != ViewPhase.None && CurrentPhase != ViewPhase.Closed)
                    Log.Error($"[Phase Assertion] Open: unexpected CurrentPhase={CurrentPhase}," +
                              $"  expected: None/Closed, name={name}", this);
                if (PendingPhase != ViewPhase.Opened)
                    Log.Error($"[Phase Assertion] Open: unexpected PendingPhase={PendingPhase}," +
                              $"  expected: Opened, name={name}", this);
                var oldOpenPhase = CurrentPhase;
                CurrentPhase = ViewPhase.Opened;
                PendingPhase = ViewPhase.None;
                Log.Debug($"Phase transition: {name}, CurrentPhase: {oldOpenPhase} -> {CurrentPhase}", this);

                try
                {
                    OnOpen(args);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }

                try
                {
                    await OnPostOpenAsync(args);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }

                // Open 完成后自动执行首次 Show；若打开(Opened)阶段已请求隐藏，则跳过首次 Show，保持不渲染
                if (!_pendingHide)
                {
                    await InternalExecuteShow(args);
                }
                _pendingHide = false;
            }
            finally
            {
                ReleaseLifecycleSlot();
            }
        }


        /// <summary>
        /// 内部执行 Show 生命周期流程
        /// </summary>
        private async UniTask InternalExecuteShow(LifeCycleArgs args)
        {
            if (this == null) return; // 对象可能已销毁（异步生命周期中途被 Destroy）
            if (PendingPhase != ViewPhase.None)
            {
                Log.Error($"Cannot show '{AssetName}' when PendingPhase is {PendingPhase}! " +
                          $"CurrentPhase:{CurrentPhase}", this);
                return;
            }

            if (CurrentPhase != ViewPhase.Opened && CurrentPhase != ViewPhase.Hidden)
            {
                Log.Error($"Cannot show '{AssetName}' when CurrentPhase is {CurrentPhase}!", this);
                return;
            }

            // 在 PreShow 之前设置 PendingPhase，集中管理状态转变
            PendingPhase = ViewPhase.Shown;
            Log.Debug($"InternalExecuteShow: {name}, {CurrentPhase} -> {PendingPhase}", this);

            try
            {
                await OnPreShowAsync(args);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            // 在调用 OnShow 前完成状态转变，确保 OnShow 及其之后的回调中 CurrentPhase 已处于 Shown
            if (CurrentPhase != ViewPhase.Opened && CurrentPhase != ViewPhase.Hidden)
                Log.Error($"[Phase Assertion] Show: unexpected CurrentPhase={CurrentPhase}," +
                          $"  expected: Opened/Hidden, name={name}", this);
            if (PendingPhase != ViewPhase.Shown)
                Log.Error($"[Phase Assertion] Show: unexpected PendingPhase={PendingPhase}," +
                          $"  expected: Shown, name={name}", this);
            var oldShowPhase = CurrentPhase;
            CurrentPhase = ViewPhase.Shown;
            PendingPhase = ViewPhase.None;
            Log.Debug($"Phase transition: {name}, CurrentPhase: {oldShowPhase} -> {CurrentPhase}", this);

            try
            {
                OnShow(args);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            try
            {
                await OnPostShowAsync(args);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            // 最后抛出渲染状态改变完成事件
            try
            {
                RenderingChanged?.Invoke(this);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }
        }

        /// <summary>
        /// 内部执行 Hide 生命周期流程
        /// </summary>
        private async UniTask InternalExecuteHide(LifeCycleArgs args)
        {
            if (this == null) return; // 对象可能已销毁（异步生命周期中途被 Destroy）
            if (PendingPhase != ViewPhase.None)
            {
                Log.Error($"Cannot hide '{AssetName}' when PendingPhase is {PendingPhase}! " +
                          $"CurrentPhase:{CurrentPhase}", this);
                return;
            }

            if (CurrentPhase != ViewPhase.Shown)
            {
                Log.Error($"Cannot hide '{AssetName}' when CurrentPhase is {CurrentPhase}!", this);
                return;
            }

            // 在 PreHide 之前设置 PendingPhase，集中管理状态转变
            PendingPhase = ViewPhase.Hidden;
            Log.Debug($"InternalExecuteHide: {name}, {CurrentPhase} -> {PendingPhase}", this);

            try
            {
                await OnPreHideAsync(args);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            // 在调用 OnHide 前完成状态转变，确保 OnHide 及其之后的回调中 CurrentPhase 已处于 Hidden
            if (CurrentPhase != ViewPhase.Shown)
                Log.Error($"[Phase Assertion] Hide: unexpected CurrentPhase={CurrentPhase}," +
                          $"  expected: Shown, name={name}", this);
            if (PendingPhase != ViewPhase.Hidden)
                Log.Error($"[Phase Assertion] Hide: unexpected PendingPhase={PendingPhase}," +
                          $"  expected: Hidden, name={name}", this);
            var oldHidePhase = CurrentPhase;
            CurrentPhase = ViewPhase.Hidden;
            PendingPhase = ViewPhase.None;
            Log.Debug($"Phase transition: {name}, CurrentPhase: {oldHidePhase} -> {CurrentPhase}", this);

            try
            {
                OnHide(args);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            try
            {
                await OnPostHideAsync(args);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            // 最后抛出渲染状态改变完成事件
            try
            {
                RenderingChanged?.Invoke(this);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }
        }

        /// <summary>
        /// 保存View数据
        /// </summary>
        /// <returns></returns>
        object IViewLifeCycle.ExecuteSave()
        {
            try
            {
                return OnSave();
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            return null;
        }

        /// <summary>
        /// 清理View内存
        /// </summary>
        /// <returns></returns>
        void IViewLifeCycle.ExecuteClear()
        {
            try
            {
                ClearSave();
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }
        }

        /// <summary>
        /// 执行View关闭流程（只应该由View管理相关模块调用，其他系统、业务不应该调用）
        ///
        /// 清理状态、停止定时器、触发OnClose回调等
        /// </summary>
        async UniTask IViewLifeCycle.ExecuteClose(LifeCycleArgs args)
        {
            if (PendingPhase == ViewPhase.Opened)
            {
                Log.Error($"'{name}' is opening, cannot execute close! " +
                          $"CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);
                return;
            }

            // 如果已经处于关闭流程中（PendingPhase=Closed），或已经完全关闭（CurrentPhase=Closed），则不需要重复关闭
            if (CurrentPhase == ViewPhase.Closed || PendingPhase == ViewPhase.Closed) return;

            var slotOk = await AcquireLifecycleSlot();
            if (this == null) return; // 等待期间对象可能被销毁（如 Shutdown 中 Destroy UI 根）
            if (!slotOk) return;
            try
            {
                // 重新检查（等待期间状态可能已变）
                if (CurrentPhase == ViewPhase.Closed || PendingPhase == ViewPhase.Closed) return;

                if (CurrentPhase == ViewPhase.Shown)
                {
                    await InternalExecuteHide(args);
                }

                if (CurrentPhase != ViewPhase.Hidden)
                {
                    // View只能从Hidden转换Closed
                    Log.Error($"'{name}' should not do ExecuteClose when CurrentPhase is {CurrentPhase}! " +
                              $"Should be hidden first.", this);
                    return;
                }

                // 在 PreClose 之前设置 PendingPhase，集中管理状态转变
                PendingPhase = ViewPhase.Closed;
                Log.Debug($"ExecuteClose: {name}, {CurrentPhase} -> {PendingPhase}", this);

                try
                {
                    await OnPreCloseAsync(args);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }

                // 在调用 OnClose 前完成状态转变，确保 OnClose 及其之后的回调中 CurrentPhase 已处于 Closed
                if (CurrentPhase != ViewPhase.Hidden)
                    Log.Error($"[Phase Assertion] Close: unexpected CurrentPhase={CurrentPhase}," +
                              $"  expected: Hidden, name={name}", this);
                if (PendingPhase != ViewPhase.Closed)
                    Log.Error($"[Phase Assertion] Close: unexpected PendingPhase={PendingPhase}," +
                              $"  expected: Closed, name={name}", this);
                var oldClosePhase = CurrentPhase;
                CurrentPhase = ViewPhase.Closed;
                PendingPhase = ViewPhase.None;
                Log.Debug($"Phase transition: {name}, CurrentPhase: {oldClosePhase} -> {CurrentPhase}", this);

                try
                {
                    OnClose(args);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }

                try
                {
                    await OnPostCloseAsync(args);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }
            }
            finally
            {
                ReleaseLifecycleSlot();
            }
        }

        #endregion

        #region private 方法

        /// <summary>
        /// 等待获取生命周期操作的执行权（FIFO 队列化轮询，参考 NavigationBehaviour.PreChangeStateAsync）
        /// 当有操作正在执行或队列中有等待者时，必须排队等候
        /// </summary>
        /// <returns>true=成功获取执行权，false=被取消</returns>
        private async UniTask<bool> AcquireLifecycleSlot()
        {
            if (_lifecycleActive || _lifecycleWaiters.Count > 0)
            {
                var myId = ++_nextLifecycleWaiterId;
                var generation = _lifecycleGeneration;
                _lifecycleWaiters.Enqueue(myId);

                Log.Debug($"'{name}' lifecycle wait, 队列位置:{_lifecycleWaiters.Count}, " +
                          $"CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

                var waitFrames = 0;
                while (true)
                {
                    // 被外部取消（如 Destroy/Reset）
                    if (generation != _lifecycleGeneration) return false;

                    // 当前无进行中的操作，且轮到自己
                    if (!_lifecycleActive &&
                        _lifecycleWaiters.Count > 0 &&
                        _lifecycleWaiters.Peek() == myId)
                    {
                        _lifecycleWaiters.Dequeue();
                        break;
                    }

                    await UniTask.Yield();

                    if (++waitFrames >= LIFECYCLE_WAIT_MAX_FRAMES)
                    {
                        // 只有队首等待者才能强制超时，避免多个等待者同时 break
                        if (_lifecycleWaiters.Count > 0 && _lifecycleWaiters.Peek() == myId)
                        {
                            GameLog.Error($"'{name}' lifecycle wait timeout " +
                                           $"({LIFECYCLE_WAIT_MAX_FRAMES} frames), 强制继续. " +
                                           $"CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", module: "Framework.View");
                            _lifecycleActive = false;
                            _lifecycleWaiters.Dequeue();
                            break;
                        }

                        // 非队首：重置计时器，等待队首超时后逐个传导
                        waitFrames = 0;
                    }
                }
            }

            _lifecycleActive = true;
            return true;
        }

        /// <summary>
        /// 释放生命周期操作执行权，允许下一个排队的操作开始执行
        /// </summary>
        private void ReleaseLifecycleSlot()
        {
            _lifecycleActive = false;
        }

        /// <summary>
        /// 取消所有生命周期等待者（在 Destroy 等场景使用）
        /// </summary>
        private void CancelAllLifecycleWaiters()
        {
            ++_lifecycleGeneration;
            _lifecycleWaiters.Clear();
            _lifecycleActive = false;
        }

        /// <summary>
        /// 刷新渲染状态（入口）
        /// 所有调用统一通过 FIFO 队列串行执行，避免并发调用导致状态混乱
        /// </summary>
        /// <returns>返回渲染状态是否变化</returns>
        private async UniTask<bool> RefreshRendering()
        {
            // 在SetVisibleState中已经做了Closed判断，这里就不需要再判断了，保留这行注释以清晰说明
            // if (PendingPhase == ViewPhase.Closed || CurrentPhase == ViewPhase.Closed) return false;

            var ok = await AcquireLifecycleSlot();
            if (!ok) return false;
            try
            {
                return await RefreshRenderingCore();
            }
            finally
            {
                ReleaseLifecycleSlot();
            }
        }

        /// <summary>
        /// 刷新渲染状态（核心逻辑）
        /// 评估所有 VisibilityController 的期望状态，根据结果执行 Show 或 Hide
        /// </summary>
        private async UniTask<bool> RefreshRenderingCore()
        {
            // 已经关闭了，就不需要再Show或者Hide了
            if (PendingPhase == ViewPhase.Closed || CurrentPhase == ViewPhase.Closed) return false;

            var controllers = VisibilityControllers;

            var expectedVisibleCount = 0;
            foreach (var pair in controllers)
            {
                if (pair.Value.ExpectedState) ++expectedVisibleCount;
            }

            // 如果所有控制器都一致同意显示，则转变为显示状态
            var expectedVisible = (expectedVisibleCount == controllers.Count);

            Log.Debug($"{name}, expectedVisible: {expectedVisible}," +
                      $"CurrentPhase={CurrentPhase}, PendingPhase={PendingPhase}", this);

            // 执行生命周期流程
            if (expectedVisible)
            {
                // 如果已经是预期的Shown状态了，就跳过不执行
                if (CurrentPhase == ViewPhase.Shown) return false;

                var args = new LifeCycleArgs(LifeCycleCause.None, OpenShowData);
                await InternalExecuteShow(args);
            }
            else
            {
                // 如果已经是预期的Hidden状态了，就跳过不执行
                if (CurrentPhase == ViewPhase.Hidden) return false;

                // 正在打开(Opened)、尚未首次 Show 时收到隐藏请求：InternalExecuteHide 要求 CurrentPhase==Shown，
                // 此时无法执行 Hide，改为记录待隐藏标志，等 Open 完成后跳过首次 Show，保持不渲染。
                if (CurrentPhase == ViewPhase.Opened)
                {
                    _pendingHide = true;
                    return false;
                }

                var args = new LifeCycleArgs(LifeCycleCause.None);
                await InternalExecuteHide(args);
            }

            return true;
        }

        private void ExecuteVisibleChange(bool toVisible)
        {
            DefaultVisibleStrategy?.SetVisible(this, toVisible);

            var controllers = VisibilityControllers;
            for (var i = 0; i < controllers.Count; i++)
            {
                var (controller, state) = controllers[i];
                if (state.CurrentState == toVisible) continue;
                try
                {
                    controller.Strategy?.SetVisible(this, toVisible);
                }
                catch (Exception e)
                {
                    GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                }

                state.CurrentState = toVisible;
                controllers[i] = new KeyValuePair<VisibleController, VisibleControllerState>(controller, state);
            }
        }

        #endregion

        #region 其它

        /// <summary>
        /// 创建 DefaultVisibleStrategy 的抽象方法，交由子类实现
        /// </summary>
        protected abstract IVisibleStrategy CreateDefaultVisibleStrategy();

        public override string ToString()
        {
            return
                $"{GetType().Name}({AssetName}, Running:{Running}, CurrentPhase:{CurrentPhase}, PendingPhase:{PendingPhase})";
        }

        #endregion
    }
}
