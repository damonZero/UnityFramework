using Framework.Log;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Framework.View.Navigation
{
    public class NavigationManager
    {
        #region public: 属性

        //导航容器根节点
        public NavigateContainer Root { get; } = new() { Name = "Root", RelationshipChild = false };

        public static NavigationManager Instance { get; private set; }

        /// <summary>
        /// 导航容器状态变化前全局事件
        /// </summary>
        public NavigationEvent<NavigationBehaviour, NavigationStateType> BeforeContainerStateChange { get; } =
            new();

        /// <summary>
        /// 导航容器状态变化后全局事件
        /// </summary>
        public NavigationEvent<NavigationBehaviour, NavigationStateType> AfterContainerStateChange { get; } =
            new();

        /// <summary>
        /// 加载器Loader状态变化前全局事件
        /// </summary>
        public NavigationEvent<NavigationBehaviour, NavigationStateType> BeforeLoaderStateChange { get; } =
            new();

        /// <summary>
        /// Loader状态变化后全局事件
        /// </summary>
        public NavigationEvent<NavigationBehaviour, NavigationStateType> AfterLoaderStateChange { get; } =
            new();

        #endregion

        #region internal: 属性

        internal FormManager FormManager { get; private set; }
        internal ISceneManager SceneManager { get; private set; }

        #endregion

        #region private: 字段

        /// <summary>
        /// 每帧渲染签名追踪器。
        /// <para>
        /// 负责维护 "上一帧实际渲染画面" 的签名快照，并提供
        /// <see cref="INavigationRenderingSignatureTracker.HasRenderingSignatureChanged"/> /
        /// <see cref="INavigationRenderingSignatureTracker.HasStableRenderingSignature"/> 查询。
        /// </para>
        /// 默认由 <see cref="NavigationAutoRenderingSignatureTracker"/> 实现；
        /// 可通过 <see cref="SetRenderingSignatureTracker"/> 替换为自定义实现。
        /// </summary>
        private INavigationRenderingSignatureTracker _renderingSignatureTracker;

        /// <summary>
        /// 当前渲染签名追踪器实例。
        /// </summary>
        public INavigationRenderingSignatureTracker RenderingSignatureTracker => _renderingSignatureTracker;

        /// <summary>
        /// 自动追踪器所在的宿主 GameObject（仅当使用默认自动实现时创建）。
        /// </summary>
        private GameObject _autoTrackerHost;

        protected int _disableEventSystemCount = 0;

        #endregion

        #region public: 方法

        public NavigateContainer CreateContainer(string name)
        {
            var container = NavigationFactory.Instance.Get<NavigateContainer>();

            container.Name = name;
            container.AddToSystem = AddContainerToManager;
            container.JumpToContainerAsync = JumpToContainerAsync;
            container.BeforeStateChangeEvent.AddAsync(OnContainerBeforeStateChange, 0);
            container.AfterStateChangeEvent.AddAsync(OnContainerAfterStateChange, 0);
            container.beforeLoaderStateChange.AddAsync(OnLoaderBeforeStateChange, 0);
            container.afterLoaderStateChange.AddAsync(OnLoaderAfterStateChange, 0);

            return container;
        }

        /// <summary>
        /// 禁用事件系统
        /// </summary>
        /// <returns>是否实际执行了禁用操作，如果已经被禁用则返回false</returns>
        public virtual bool DisableEventSystem()
        {
            if (_disableEventSystemCount++ > 0) return false;

            SetEventSystemEnable(false);
            return true;
        }

        /// <summary>
        /// 启用事件系统
        /// </summary>
        /// <returns>是否实际执行了启用操作，如果仍有其他禁用请求则返回false</returns>
        public virtual bool EnableEventSystem()
        {
            if (--_disableEventSystemCount > 0) return false;

            SetEventSystemEnable(true);
            return true;
        }

        #endregion

        #region 生命周期方法

        public virtual void Init(FormManager formManager, ISceneManager sceneManager,
            Func<ITransition> defaultTransitionFactory = null,
            INavigationRenderingSignatureTracker renderingSignatureTracker = null)
        {
            Instance = this;

            FormManager = formManager;
            SceneManager = sceneManager;

            TransitionFactory.CreateDefault = defaultTransitionFactory;

            _renderingSignatureTracker = renderingSignatureTracker;
            EnsureAutoRenderingSignatureTracker();
        }

        public virtual void ShutDown()
        {
            FormManager = null;
            SceneManager = null;

            DestroyAutoTrackerHost();
            _renderingSignatureTracker = null;
        }

        #endregion

        #region Rendering Signature (画面构成签名，用于跨帧检测画面变化)

        /// <summary>
        /// 替换当前的渲染签名追踪器。
        /// <para>
        /// 传入自定义实现（如 <see cref="NavigationManualRenderingSignatureTracker"/>）可控制捕获时机；
        /// 传入 null 将恢复为默认的 <see cref="NavigationAutoRenderingSignatureTracker"/>。
        /// 自定义实现应保证其快照语义仍等价于“上一帧实际渲染画面”，否则变化检测结果会失真。
        /// </para>
        /// </summary>
        public void SetRenderingSignatureTracker(INavigationRenderingSignatureTracker tracker)
        {
            if (tracker == null)
            {
                Log.Debug("[NavigationManager] SetRenderingSignatureTracker(null): fallback to auto tracker.");
                EnsureAutoRenderingSignatureTracker();
                return;
            }

            // 如果当前是自动追踪器，销毁它的 GameObject
            DestroyAutoTrackerHost();
            _renderingSignatureTracker = tracker;
            Log.Debug($"[NavigationManager] Set custom rendering signature tracker: {tracker.GetType().Name}.");
        }

        private void EnsureAutoRenderingSignatureTracker()
        {
            if (_renderingSignatureTracker != null) return;

            DestroyAutoTrackerHost();
            _renderingSignatureTracker =
                NavigationAutoRenderingSignatureTracker.Create(out _autoTrackerHost);
            Log.Debug("[NavigationManager] Created default auto rendering signature tracker.");
        }

        private void DestroyAutoTrackerHost()
        {
            if (_autoTrackerHost != null)
            {
                UnityEngine.Object.Destroy(_autoTrackerHost);
                Log.Debug("[NavigationManager] Destroyed auto rendering signature tracker host.");
                _autoTrackerHost = null;
            }
        }

        #endregion

        #region protected: 方法

        protected virtual void SetEventSystemEnable(bool enable)
        {
            EventSystem.current.enabled = enable;
        }

        #endregion

        #region private: 方法


        private async UniTask OnContainerBeforeStateChange(NavigationBehaviour container, NavigationStateType state)
        {
            await BeforeContainerStateChange.InvokeAsync(container, state);
        }

        private async UniTask OnContainerAfterStateChange(NavigationBehaviour container, NavigationStateType state)
        {
            if (state == NavigationStateType.Close)
            {
                await RemoveClosedContainer(container, state);
            }

            await AfterContainerStateChange.InvokeAsync(container, state);
        }

        private async UniTask OnLoaderBeforeStateChange(NavigationBehaviour loader, NavigationStateType state)
        {
            await BeforeLoaderStateChange.InvokeAsync(loader, state);
        }

        private async UniTask OnLoaderAfterStateChange(NavigationBehaviour loader, NavigationStateType state)
        {
            await AfterLoaderChange(loader, state);

            await AfterLoaderStateChange.InvokeAsync(loader, state);
        }

        // 最后一个导航容器加载器变化
        private async UniTask AfterLoaderChange(NavigationBehaviour loader, NavigationStateType state)
        {
            // 导航容器状态变化处理
            if (loader is not NavigationLoader navigationLoader)
            {
                Log.Error($"出现异常：loader '{loader.Name}' 不是 {nameof(NavigationLoader)} 类型!!!" +
                          $" loader: {loader}");
                return;
            }

            var container = navigationLoader.ParentContainer;
            if (state == NavigationStateType.Open)
            {
                // 导航加载器打开时，是全屏且逻辑可见的，则隐藏前面的导航容器
                var loaderFullScreen = navigationLoader.FullScreenAndLogicalVisible();
                if (!loaderFullScreen)
                {
                    Log.Debug($"Loader:'{loader.Name}' 状态变化为 {state}，" +
                              $"所在导航容器'{container.Name}' 不是全屏可见，不做任何事情. " +
                              $"container: {container}, loader: {loader}");
                    return;
                }

                Log.Debug($"Loader:'{loader.Name}' 状态变化为 {state}，是全屏可见的，需要隐藏前置导航容器. " +
                          $"container: {container}, loader: {loader}");
                await HideFrontContainers(container);

                return;
            }

            var containerFullScreen = container.FullScreenAndLogicalVisible();
            if (containerFullScreen)
            {
                Log.Debug($"Loader:'{loader.Name}' 状态变化为 {state}，所在导航容器'{container.Name}' 是全屏可见，不做任何事情." +
                          $"container: {container}");
                return;
            }

            var rootFullScreen = Root.FullScreenAndLogicalVisible();
            if (rootFullScreen)
            {
                Log.Debug($"Loader:'{loader.Name}' 状态变化为 {state}，所在导航容器'{container.Name}' 不是全屏可见，" +
                          $"但Root导航容器是全屏可见，不做任何事情. " +
                          $"container: {container}, Root: {Root}");
                return;
            }

            Log.Debug($"Loader:'{loader.Name}' 状态变化为 {state}，所在导航容器'{container.Name}' 不是全屏，" +
                      $"Root导航容器也不是全屏，需要显示前置导航容器. " +
                      $"container: {container}, Root: {Root}");
            await ShowLastFullScreenContainer(container);
        }

        // 隐藏指定container之前的所有容器
        private async UniTask HideFrontContainers(NavigateContainer container)
        {
            Log.Debug($"准备隐藏导航容器'{container.Name}'之前的所有容器. " +
                      $"container: {container}");
            while (true)
            {
                var parent = container.Parent;
                var index = parent.Children.IndexOf(container);
                if (index < 0)
                {
                    Log.Error($"导航容器'{container.Name}'不在父容器'{parent.Name}'的子容器列表中, 无法隐藏前置容器! " +
                              $"{container}");
                    break;
                }

                var childrenToHide = NavigationFactory.GetContainerList();
                try
                {
                    for (var i = 0; i < index; i++)
                    {
                        childrenToHide.Add(parent.Children[i]);
                    }

                    foreach (var sibling in childrenToHide)
                    {
                        // 这个循环过程中，parent.Children可能会发生变化，所以要再检查
                        if (!parent.Children.Contains(sibling)) continue;

                        Log.Debug($"隐藏导航容器'{container.Name}'之前的容器 -> " +
                                  $"'{sibling.Name}' 是父容器 '{parent.Name}' 的子容器，执行 SetLogicalVisible(false) 隐藏. " +
                                  $"sibling: {sibling}, parent: {parent}");

                        // 隐藏排在container之前的sibling
                        await sibling.SetLogicalVisible(false);
                    }
                }
                finally
                {
                    NavigationFactory.ReleaseContainerList(childrenToHide);
                }

                // 隐藏父容器自身的界面 (Loaders)，因为它们在导航系统中排序在子容器之前
                if (parent.Loaders.Count > 0)
                {
                    var loadersToHide = NavigationFactory.GetLoaderList();
                    try
                    {
                        loadersToHide.AddRange(parent.Loaders);
                        foreach (var loader in loadersToHide)
                        {
                            if (!parent.Loaders.Contains(loader)) continue;

                            Log.Debug($"隐藏导航容器'{container.Name}'的父容器 '{parent.Name}' 自身的界面 -> '{loader.Name}'");
                            await loader.SetLogicalVisible(false);
                        }
                    }
                    finally
                    {
                        NavigationFactory.ReleaseLoaderList(loadersToHide);
                    }
                }

                if (parent == Root) break;

                // 递归向上，隐藏parent之前的其他容器
                Log.Debug($"继续递归向上隐藏parent导航容器'{parent.Name}'之前的所有容器. " +
                          $"container: {container} \n parent: {parent}");
                container = parent;
            }

            Log.Debug($"完成隐藏导航容器'{container.Name}'之前的所有容器. " +
                      $"container: {container}");
        }

        private void AddContainerToManager(NavigateContainer container)
        {
            if (container.Parent != null)
                throw new InvalidOperationException($"导航容器''{container.Name}''已添加到导航系统, 不能重复添加");

            Root.AddChildContainer(container);
        }

        /// <summary>
        /// 跳转到目标导航容器
        /// </summary>
        /// <param name="targetContainer"></param>
        /// <param name="cancellationToken"></param>
        private async UniTask JumpToContainerAsync(NavigateContainer targetContainer,
            CancellationToken cancellationToken)
        {
            var lastContainer = Root.GetLastContainer();
            if (targetContainer == lastContainer) return;
            if (targetContainer is not { EffectOther: true }) return;

            foreach (var container in Root.ForeachContainers(TraversalOrder.Reverse, includeSelf: false))
            {
                if (container == targetContainer) break;

                if (container.CanChangeTo(NavigationStateType.Close))
                {
                    await container.Close(cancellationToken);
                }
            }
        }

        // 关闭空导航容器
        private async UniTask RemoveClosedContainer(NavigationBehaviour container, NavigationStateType state)
        {
            try
            {
                if (!container.IsUnlocked(NavigationStateType.Close)) return;
                Log.Debug($"container: {container}, state: {state}");
                var empty = container as NavigateContainer;
                Log.Debug($"empty: {empty}");
                NavigationBehaviour lastContainer = Root.GetLastContainer();
                Log.Debug($"empty.Parent: {empty.Parent}");
                empty.Parent?.Children.Remove(empty);

                Log.Debug($"empty.EffectOther: {empty.EffectOther}");
                //默认显示前置组
                if (empty.EffectOther && empty == lastContainer)
                {
                    await ShowLastFullScreenContainer(null);
                }

                // 回收该导航容器对象
                NavigationFactory.Instance.Recycle(empty);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "Navigation exception", module: "Framework.View.Navigation");
            }
        }

        // 恢复到上一个全屏导航容器
        private async UniTask ShowLastFullScreenContainer(NavigateContainer except)
        {
            foreach (var container in Root.ForeachContainers(TraversalOrder.Reverse, includeSelf: false))
            {
                if (container == except) continue;

                var canShow = container.CurrentState is not NavigationStateType.Close;
                if (!canShow)
                {
                    if (container.IsFullScreen())
                    {
                        Log.Debug($"恢复到上一个全屏导航容器 -> " +
                                  $"'{container.Name}' 是全屏，但无法显示，恢复流程结束. " +
                                  $"container: {container}");
                        break;
                    }

                    Log.Debug($"恢复到上一个全屏导航容器 -> " +
                              $"'{container.Name}' 不是全屏，也无法显示，忽略. " +
                              $"container: {container}");
                    continue;
                }

                Log.Debug($"恢复到上一个全屏导航容器 -> " +
                          $"'{container.Name}' 执行显示 SetLogicalVisible(true), " +
                          $"container: {container}");
                await container.SetLogicalVisible(true);

                var parent = container.Parent;
                if (parent is { RelationshipChild: true })
                {
                    var parentCanShow =
                        parent.CurrentState is not (NavigationStateType.Close or NavigationStateType.None);
                    if (parentCanShow)
                    {
                        Log.Debug($"恢复到上一个全屏导航容器 -> " +
                                  $"'{container.Name}' 的父容器 '{parent.Name}' 执行显示 SetLogicalVisible(true), " +
                                  $"parent: {parent}");
                        await parent.SetLogicalVisible(true);
                    }
                    else
                    {
                        Log.Debug($"恢复到上一个全屏导航容器 -> " +
                                  $"'{container.Name}' 的父容器 '{parent.Name}' 无法显示，忽略. " +
                                  $"parent: {parent}");
                    }
                }
                else
                {
                    Log.Debug($"恢复到上一个全屏导航容器 -> '{container.Name}' 的父容器 '{parent?.Name}' 被忽略, " +
                              $"parent: {parent}, parent.RelationshipChild: {parent?.RelationshipChild}");
                }

                if (container.IsFullScreen())
                {
                    Log.Debug($"恢复到上一个全屏导航容器 -> {container} 是全屏，恢复流程结束");
                    break;
                }
            }
        }

        #endregion
    }
}
