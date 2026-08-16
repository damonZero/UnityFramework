//**************************************************************************************
//Create By Liangc on 2023/11/15
//
//@Description 导航容器类
//**************************************************************************************

using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 管理多个View("场景/界面")对象，提供相关的操作以及与NavigationManager交互
    /// </summary>
    public class NavigateContainer : NavigationBehaviour, INavigateContainer
    {
        #region public properties

        /// <summary>
        /// 父导航容器
        /// </summary>
        public NavigateContainer Parent { get; protected set; }

        /// <summary>
        /// 子导航容器集合
        /// </summary>
        public List<NavigateContainer> Children { get; } = new();

        /// <summary>
        /// 导航容器内的管理View加载器对象集合
        /// </summary>
        public List<NavigationLoader> Loaders { get; } = new();

        /// <summary>
        /// 影响其他导航容器
        /// </summary>
        public bool EffectOther { get; set; } = true;

        //导航容器缓存
        public NavigatorCached Cache { get; } = new();

        public Action<NavigateContainer> AddToSystem { get; set; }

        public Func<NavigateContainer, CancellationToken, UniTask> JumpToContainerAsync { get; set; }

        // //加载器状态变化前监听器
        public readonly NavigationEvent<NavigationBehaviour, NavigationStateType> beforeLoaderStateChange = new();

        //加载器状态变化后监听器
        public readonly NavigationEvent<NavigationBehaviour, NavigationStateType> afterLoaderStateChange = new();

        //是否关联子导航容器(新建子导航容器时忽略,还原子导航容器时一起还原父导航容器)
        public bool RelationshipChild { get; set; } = true;

        //是否正在还原中
        public bool Recovering { get; internal set; }

        //是否为空导航容器(导航容器缓存数量为0 且 正在清理元素数量为0)
        public bool Empty => Loaders.Count == 0 && Cache.Empty && Children.Count == 0;

        //所有导航容器数量(包含自己,_childGroups.Count==0时为1)
        public int ContainerCount
        {
            get
            {
                var count = 0;
                foreach (var _ in ForeachContainers(TraversalOrder.Forward))
                {
                    count++;
                }

                return count;
            }
        }

        public int DefaultLayer { get; protected set; }

        #endregion

        #region internal properties

        /// <summary>
        /// 记录的前一次全屏状态
        /// </summary>
        internal bool LastFullScreen { get; set; } = false;

        #endregion

        #region constructor

        public NavigateContainer() : this(0)
        {
        }

        public NavigateContainer(int defaultLayer)
        {
            DefaultLayer = defaultLayer;
        }

        #endregion

        #region 适用于界面的简化调用API

        /// <summary>
        /// 打开界面
        /// </summary>
        /// <param name="layer">界面显示层级</param>
        /// <param name="data">附带的数据，可选参数</param>
        /// <param name="formName">界面预制体名字，可选参数，不填的话就用TForm类型名</param>
        /// <typeparam name="TForm">界面脚本类型</typeparam>
        /// <returns></returns>
        public UniTask<TForm> OpenFormAsync<TForm>(int layer, object data = null, string formName = null)
            where TForm : BaseForm
        {
            if (layer <= 0)
            {
                throw new ArgumentException($"[{nameof(NavigateContainer)}.{nameof(OpenFormAsync)}] " +
                                            $"Invalid Layer: layer={layer}\n" +
                                            $"参数layer不合法，请设置大于0的Layer值！");
            }

            var options = NavigateFormOptions.Pool.Rent();
            options.Layer = layer;
            options.Data = data;
            options.AssetName = formName ?? typeof(TForm).Name;
            return OpenViewAsync<TForm>(options);

            // var formLoader = AddForm<TForm>(layer, formName);
            // formLoader.FormOptions.Data = data;
            // formLoader.OpenData = data;
            // formLoader.ShowData = data;
            // return formLoader.OpenViewAsync<TForm>();
        }

        /// <summary>
        /// 打开界面
        /// </summary>
        /// <param name="options">界面打开参数项</param>
        /// <typeparam name="TForm"></typeparam>
        /// <returns></returns>
        public UniTask<TForm> OpenFormAsync<TForm>(NavigateFormOptions options)
            where TForm : BaseForm
        {
            if (options.Layer <= 0)
            {
                throw new ArgumentException($"[{nameof(NavigateContainer)}.{nameof(OpenFormAsync)}] " +
                                            $"Invalid Layer: options.Layer={options.Layer}\n" +
                                            $"参数options.Layer不合法，请设置大于0的Layer值！");
            }

            return OpenViewAsync<TForm>(options);
        }


        /// <summary>
        /// 添加一个界面到导航容器中，不执行打开动作
        /// 若界面已存在，如果已经存在，则仅设置options
        /// </summary>
        /// <param name="layer">界面显示层级</param>
        /// <param name="assetName">界面预制体名字</param>
        /// <typeparam name="TForm"></typeparam>
        /// <returns>返回界面对应的加载器</returns>
        public NavigationFormLoader AddForm<TForm>(int layer, string assetName = null) where TForm : BaseForm
        {
            assetName = string.IsNullOrEmpty(assetName) ? typeof(TForm).Name : assetName;

            var options = NavigateFormOptions.Pool.Rent();
            options.Layer = layer;
            options.AssetName = assetName;
            var loader = AddView<TForm>(options);
            return loader as NavigationFormLoader;
        }

        #endregion


        #region 适用于场景的简化调用API

        public UniTask<TScene> OpenSceneAsync<TScene>(object data = null, string sceneName = null)
            where TScene : BaseScene
        {
            var options = NavigateSceneOptions.Pool.Rent();
            options.Data = data;
            options.AssetName = sceneName ?? typeof(TScene).Name;
            return OpenViewAsync<TScene>(options);
        }

        public UniTask<TScene> OpenSceneAsync<TScene>(NavigateSceneOptions options)
            where TScene : BaseScene
        {
            return OpenViewAsync<TScene>(options);
        }

        public NavigationSceneLoader AddScene<TScene>(string sceneName = null) where TScene : BaseScene
        {
            sceneName = string.IsNullOrEmpty(sceneName) ? typeof(TScene).Name : sceneName;

            var options = NavigateSceneOptions.Pool.Rent();
            options.AssetName = sceneName;
            var loader = AddView<TScene>(options);
            return loader as NavigationSceneLoader;
        }

        #endregion

        #region 界面和场景通用的API

        /// <summary>
        /// 添加一个View到导航容器中，不执行打开动作
        ///
        /// 若View已存在，如果已经存在，则直接返回已存在的加载器
        ///
        /// 此方法等同于 <see cref="FindOrCreateLoader{TView}(INavigateOptions)"/>
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="TView"></typeparam>
        /// <returns></returns>
        public INavigateLoader AddView<TView>(INavigateOptions options) where TView : ViewBase
        {
            return FindOrCreateLoader<TView>(options);
        }

        /// <summary>
        /// 打开View，支持全部参数项
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="TView"></typeparam>
        /// <returns></returns>
        public virtual UniTask<TView> OpenViewAsync<TView>(INavigateOptions options) where TView : ViewBase
        {
            var loader = AddView<TView>(options);
            return loader.OpenViewAsync<TView>();
        }

        /// <summary>
        /// 查找View
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <param name="assetName"></param>
        public TView FindView<TView>(string assetName = null) where TView : ViewBase
        {
            assetName = string.IsNullOrEmpty(assetName) ? typeof(TView).Name : assetName;
            var loader = FindLoader(assetName);

            return loader?.View as TView;
        }

        #endregion

        #region 加载器API

        public INavigateLoader FindLoader<TView>() where TView : ViewBase
        {
            return FindLoader(typeof(TView).Name);
        }

        /// <summary>
        /// 查找或创建加载器
        /// </summary>
        /// <param name="assetName">要加载的视图资产名字，不带后缀</param>
        /// <typeparam name="TView"></typeparam>
        /// <returns></returns>
        public INavigateLoader FindOrCreateLoader<TView>(string assetName = null) where TView : ViewBase
        {
            var viewType = typeof(TView);
            assetName = string.IsNullOrEmpty(assetName) ? viewType.Name : assetName;

            var loader = FindLoader(assetName);
            if (loader != null) return loader;

            if (typeof(BaseForm).IsAssignableFrom(viewType))
            {
                var formOptions = NavigateFormOptions.Pool.Rent();
                formOptions.AssetName = assetName;
                formOptions.Container = Name;
                loader = CreateFormLoader(formOptions);
            }
            else
            {
                var sceneOptions = NavigateSceneOptions.Pool.Rent();
                sceneOptions.AssetName = assetName;
                sceneOptions.Container = Name;
                loader = CreateSceneLoader(sceneOptions);
            }

            return loader;
        }


        public INavigateLoader FindOrCreateLoader<TView>(INavigateOptions options) where TView : ViewBase
        {
            var viewType = typeof(TView);

            // 确定参数：assetName
            var assetName = options.AssetName;
            if (string.IsNullOrEmpty(assetName))
            {
                assetName = viewType.Name;
                options.AssetName = assetName;
            }

            // 填充参数：options.Container
            if (string.IsNullOrEmpty(options.Container))
            {
                options.Container = Name;
            }
            else if (options.Container != Name)
            {
                Log.Error(
                    $"[{nameof(NavigateContainer)}.{nameof(FindOrCreateLoader)}] " +
                    $"options.Container:'{options.Container}' does not match current container:'{Name}'\n" +
                    $"options中的Container:'{options.Container}'与当前容器:'{Name}'不一致，请检查修正逻辑或置空");
                options.Container = Name;
            }

            // 查找加载器，若不存在则创建
            NavigationLoader loader = null;
            var consumed = false;

            if (options.Mode.HasFlag(NavigationMode.FindOrNew))
            {
                loader = FindLoader(assetName);
            }

            if (loader == null)
            {
                NavigationLoader newLoader;
                if (typeof(BaseForm).IsAssignableFrom(viewType))
                {
                    newLoader = CreateFormLoader(options);
                }
                else
                {
                    newLoader = CreateSceneLoader(options);
                }

                loader = newLoader;
                consumed = true;
            }
            else if (loader.ViewOptions != options &&
                     loader.CurrentState is NavigationStateType.None &&
                     loader.PendingState is NavigationStateType.None)
            {
                loader.ViewOptions?.RecycleToPool();
                loader.ViewOptions = options;
                consumed = true;
            }

            // 填充加载器参数
            loader.Mode = options.Mode;
            loader.OpenShowData = options.Data;

            // 加载器已存在且处于 Open/转场流程中时，options 未被消费，归还池避免泄漏
            if (!consumed && loader.ViewOptions != options)
            {
                options.RecycleToPool();
            }

            return loader;
        }

        #endregion

        /// <summary>
        /// 添加子导航容器
        /// </summary>
        /// <param name="childContainer">导航容器对象</param>
        /// <returns></returns>
        public bool AddChildContainer(NavigateContainer childContainer)
        {
            if (childContainer == null)
            {
                throw new ArgumentNullException(nameof(childContainer),
                    $"[{nameof(NavigateContainer)}.{nameof(AddChildContainer)}] childContainer参数不能为空");
            }

            foreach (var container in ForeachContainers())
            {
                if (container.Name == childContainer.Name)
                {
                    throw new ArgumentException(
                        $"[{nameof(NavigateContainer)}.{nameof(AddChildContainer)}] " +
                        $"已经存在同名容器: '{childContainer.Name}'");
                }
            }

            Children.Add(childContainer);
            childContainer.Parent = this;
            return true;
        }

        /// <summary>
        /// 获取最后一个操作的导航容器
        /// </summary>
        /// <param name="firstLayer">只在首层子节点查找</param>
        /// <returns></returns>
        public INavigateContainer LastContainer(bool firstLayer = false)
        {
            return GetLastContainer(firstLayer);
        }

        // FIXME by fred 合并这两个方法

        /// <summary>
        /// 获取最后一个操作的导航容器
        /// </summary>
        /// <param name="firstLayer">只在首层子节点查找</param>
        /// <returns></returns>
        public NavigateContainer GetLastContainer(bool firstLayer = false)
        {
            if (Children.Count == 0)
                return this;
            var curLevelLast = Children[^1];
            if (firstLayer) return curLevelLast;
            return curLevelLast.GetLastContainer(false);
        }

        #region 迭代器

        /// <summary>
        /// 安全遍历loader，循环中可以对loader进行增删操作，不会导致遍历异常，但不会遍历到新增的loader
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NavigationLoader> SafeForeach(List<NavigationLoader> loaders)
        {
            var list = NavigationFactory.GetLoaderList();
            try
            {
                foreach (var t in loaders)
                {
                    list.Add(t);
                }

                foreach (var loader in list)
                {
                    yield return loader;
                }
            }
            finally
            {
                NavigationFactory.ReleaseLoaderList(list);
            }
        }

        /// <summary>
        /// 遍历所有Loader（包括缓存中的）
        /// </summary>
        /// <param name="order">遍历顺序</param>
        /// <returns>Loader迭代器</returns>
        public IEnumerable<NavigationLoader> ForeachLoaders(TraversalOrder order = TraversalOrder.Forward)
        {
            if (order == TraversalOrder.Forward)
            {
                foreach (var t in Loaders)
                {
                    yield return t;
                }
            }
            else
            {
                for (var i = Loaders.Count - 1; i >= 0; i--)
                {
                    if (i >= Loaders.Count)
                    {
                        Log.Error("【Navigation】遍历错误！请检查业务逻辑");
                        yield break;
                    }

                    yield return Loaders[i];
                }
            }

            // 遍历缓存中的Loader
            foreach (var loader in Cache.Loaders(order))
            {
                yield return loader;
            }
        }

        /// <summary>
        /// 遍历导航容器树
        /// </summary>
        /// <param name="order">遍历顺序（Forward=前序深度优先，Reverse=后序深度优先）</param>
        /// <param name="includeSelf">是否包含自身</param>
        /// <returns>容器迭代器</returns>
        public IEnumerable<NavigateContainer> ForeachContainers(
            TraversalOrder order = TraversalOrder.Forward, bool includeSelf = true)
        {
            if (order == TraversalOrder.Forward)
            {
                // 前序遍历：先返回自身，再遍历子节点
                if (includeSelf)
                {
                    yield return this;
                }

                if (Children.Count == 0) yield break;

                var list = NavigationFactory.GetContainerList();
                try
                {
                    list.AddRange(Children);

                    foreach (var child in list)
                    {
                        // 遍历过程中Children可能发生变化，所以要判断一下child是否还在
                        if (!Children.Contains(child)) continue;

                        foreach (var container in child.ForeachContainers(TraversalOrder.Forward, true))
                        {
                            yield return container;
                        }
                    }
                }
                finally
                {
                    NavigationFactory.ReleaseContainerList(list);
                }
            }
            else
            {
                if (Children.Count > 0)
                {
                    var list = NavigationFactory.GetContainerList();
                    try
                    {
                        list.AddRange(Children);

                        // 后序遍历：先遍历子节点，再返回自身
                        for (var i = list.Count - 1; i >= 0; i--)
                        {
                            var child = list[i];

                            // 遍历过程中Children可能发生变化，所以要判断一下child是否还在
                            if (!Children.Contains(child)) continue;

                            foreach (var container in child.ForeachContainers(TraversalOrder.Reverse, true))
                            {
                                yield return container;
                            }
                        }
                    }
                    finally
                    {
                        NavigationFactory.ReleaseContainerList(list);
                    }
                }


                if (includeSelf)
                {
                    yield return this;
                }
            }
        }

        #endregion

        /// <summary>
        /// 导航容器内存大小
        /// </summary>
        public override int Memory
        {
            get
            {
                var memory = 0;
                foreach (var loader in Loaders)
                {
                    memory += loader.Memory;
                }

                return memory;
            }
            protected set { }
        }

        /// <summary>
        /// 当前container中是否有任何View逻辑可见
        /// </summary>
        public override bool LogicalVisible
        {
            get
            {
                foreach (var loader in Loaders)
                {
                    if (loader.LogicalVisible) return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 当前container中是否有任何View被渲染
        /// </summary>
        public override bool Rendering
        {
            get
            {
                foreach (var loader in Loaders)
                {
                    if (loader.Rendering)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 是否正在转场中
        /// </summary>
        public override bool Transitioning
        {
            get
            {
                foreach (var loader in Loaders)
                {
                    if (loader.Transitioning) return true;
                }

                return false;
            }
        }

        //获取Loader
        private NavigationLoader InitLoader(NavigationLoader loader)
        {
            loader.ParentContainer = this;
            // loader.OpenData = param.data;
            // loader.ShowData = param.data;
            // loader.Mode = param.mode;
            loader.Entrance = Empty; //param.isEntrance || Empty;
            // loader.TransitionType = param.transitionType;
            // loader.TransitionBegin = param.transitionStart;
            // loader.OpenBegin = OnLoaderOpenBegin;
            // loader.OpenEnd = OnLoaderOpenEnd;
            // loader.BeforeStateChangeEvent.Add(BeforeLoaderStateChange);
            // loader.AfterStateChangeEvent.Add(AfterLoaderStateChange);
            return loader;
        }

        // /// <summary>
        // /// 打开界面
        // /// </summary>
        // /// <param name="param">打开参数</param>
        // /// <returns></returns>
        // public virtual async UniTask<NavigationLoader> OpenForm(NavigationOpenParam param)
        // {
        //     NavigateLog.Log($"打开界面:{param.name}到{Name},参数:{param}");
        //
        //     // 界面是同步加载的，预定义的静态转场没有意义，只需执行自定义转场
        //     if (!param.transitionType.HasFlag(NavigationTransitionType.Custom))
        //         param.transitionType = NavigationTransitionType.None;
        //
        //     var loader = NavigationFactory.Instance.Get<NavigationFormLoader>();
        //     loader.Layer = param.layer;
        //     InitLoader(param, loader);
        //     loaders.Add(loader);
        //
        //     await loader.OpenViewAsync();
        //     // var retLoader = await OpenAsync(loader);
        //     // if (retLoader != loader) NavigationFactory.Instance.ReleaseFormLoader(loader);
        //     return loader;
        // }

        [System.ThreadStatic] private static StringBuilder _toStringBuilder;

        public override string ToString()
        {
            _toStringBuilder ??= new StringBuilder(256);
            var sb = _toStringBuilder;
            sb.Clear();
            sb.Append($"\n导航容器:[{Name}] CurrentState:{CurrentState},PendingState:{PendingState},");
            sb.Append($"LockType:{LockType},ClearType:{Cache?.ClearType}");
            sb.Append($"CacheState:{Cache?.CurState},Parent:{Parent?.Name},Transitioning:{Transitioning}\n");
            if (CurrentState == NavigationStateType.Clear)
            {
                sb.Append($"       {Cache}");
            }
            else
            {
                foreach (var loader in Loaders)
                {
                    sb.Append("       ").Append(loader).Append('\n');
                }
            }

            return sb.ToString();
        }

        //使用加载器打开场景/界面
        private async UniTask<NavigationLoader> OpenAsync(NavigationLoader loader)
        {
            await loader.OpenViewAsync();

            return loader;
        }

        /// <summary>
        /// 执行关闭逻辑
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NavigationException"></exception>
        protected override async UniTask LifeCycleDoClose(LifeCycleArgs args)
        {
            Cache.Reset();

            // 首先递归关闭所有的子容器，给一定的容错次数
            var childFaultTolerance = Children.Count;
            while (Children.Count > 0)
            {
                var child = Children[^1];
                await child.Close(args.CancelToken);

                // 调用 child.Close 后会触发事件从 Children 列表中移除 child
                // 如果没有被移除说明没有成功执行，给予容错机会继续尝试关闭
                if (Children.IndexOf(child) < 0) continue;

                if (--childFaultTolerance < 0)
                    throw new NavigationException($"Failed to close child container '{child.Name}'");
            }

            // 关闭所有的loader，给一定的容错次数
            var faultTolerance = Loaders.Count;
            while (Loaders.Count > 0)
            {
                // 先关闭最后一个，因为移除列表最后一个的效率比移除第一个高
                var loader = Loaders[^1];
                await loader.Close();

                // 调用loader.Close后会从loaders列表中移除loader
                // 如果没有被移除说明loader.Close没有成功执行，给予容错机会继续尝试关闭，直到成功或者容错机会用完
                if (Loaders.IndexOf(loader) < 0) continue;

                //如果容错机会用完,抛出一个错误,否则容错机会减少
                if (--faultTolerance < 0)
                    throw new NavigationException($"Failed to close '{loader.Name}'");
            }

            // return TryCall<NavigationLifecycleException>(() =>
            // {
            //     if (!CanChangeTo(NavigationStateType.Close, true)) return;
            //     BeforeChangeState(NavigationStateType.Close);
            //     Cache.Reset();
            //     if (loaders.Count == 0)
            //     {
            //         BeforeChangeState(NavigationStateType.Close);
            //         ChangeState(NavigationStateType.Close);
            //     }
            //     else
            //     {
            //         //给一定数量的关闭容错机会
            //         var faultCount = loaders.Count;
            //         while (loaders.Count > 0)
            //         {
            //             var count = loaders.Count;
            //             // 先关闭最后一个，因为移除列表最后一个的效率比移除第一个高
            //             var loader = loaders[loaders.Count - 1];
            //             loader.Close();
            //             if (loaders.Count < count) continue;
            //
            //             //如果容错机会用完,抛出一个错误,否则容错机会减少
            //             if (faultCount <= 0)
            //                 throw new NavigationException($"Failed to close '{loader.Name}'");
            //             faultCount--;
            //         }
            //         //监听每个loader关闭,关闭完成后再设置整个导航容器的状态
            //     }
            // });
        }

        /// <summary>
        /// NavigationBehaviour：清理
        /// </summary>
        /// <returns></returns>
        protected override void DoClear()
        {
            //业务层设置的不还原类型认为不重要,可直接关闭
            if (Cache.ClearType == NavigationClearType.NoRecover)
            {
                Close();
            }
            else
            {
                Cache.Clear(Loaders);
            }
            // return TryCall<NavigationLifecycleException>(() =>
            // {
            //     if (!CanChangeTo(NavigationStateType.Clear, true)) return;
            //     BeforeChangeState(NavigationStateType.Clear);
            //     //业务层设置的不还原类型认为不重要,可直接关闭
            //     if (Cache.ClearType == NavigationClearType.NoRecover)
            //         Close();
            //     else
            //         Cache.Clear(loaders);
            //
            //     ChangeState(NavigationStateType.Clear);
            // });
        }

        /// <summary>
        /// 查找加载器
        /// </summary>
        /// <param name="targetName">目标名</param>
        /// <param name="cacheFind">是否在缓存中查找</param>
        /// <param name="recursive">是否递归查找子容器</param>
        /// <returns>找到的加载器</returns>
        public NavigationLoader FindLoader(string targetName, bool cacheFind = true, bool recursive = false)
        {
            if (cacheFind && CurrentState == NavigationStateType.Clear)
                return Cache.FindLoader(targetName);

            foreach (var loader in Loaders)
            {
                if (loader.Name == targetName) return loader;
            }

            if (recursive)
            {
                foreach (var child in Children)
                {
                    var loader = child.FindLoader(targetName, cacheFind, true);
                    if (loader != null) return loader;
                }
            }

            return null;
        }

        private NavigationFormLoader CreateFormLoader(INavigateOptions options)
        {
            var formLoader = CreateLoader<NavigationFormLoader>(options, NavigationManager.Instance.FormManager);
            return formLoader;
        }

        private NavigationSceneLoader CreateSceneLoader(INavigateOptions options)
        {
            var sceneLoader = CreateLoader<NavigationSceneLoader>(options, NavigationManager.Instance.SceneManager);
            return sceneLoader;
        }

        private TLoader CreateLoader<TLoader>(INavigateOptions options, IViewManager manager)
            where TLoader : NavigationLoader, new()
        {
            var assetName = options.AssetName;
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException($"assetName:'{assetName}' is null or empty");
            }

            // var loader = FindLoader(assetName);
            // if (loader != null) return loader;

            // Fixme by fred 优化锁定机制
            if (!IsUnlocked(NavigationStateType.Open))
            {
                Log.Error($"Navigation container('{Name}') is locked, can not open: '{assetName}'.\n" +
                          $"导航容器'{Name}'已经被锁定,不能往其中打开'{assetName}',请检查逻辑修复此问题!!");
                return null;
            }

            // var loader = new TLoader();
            var loader = NavigationFactory.Instance.Get<TLoader>();
            loader.Name = assetName;
            loader.ViewOptions = options;
            loader.ViewManager = manager;
            InitLoader(loader);
            Loaders.Add(loader);

            Log.Debug($"[{nameof(NavigateContainer)}.{nameof(CreateLoader)}] " +
                      $"Container:'{Name}', loader:'{loader.Name}'");

            return loader;
        }

        /// <summary>
        /// 检查针对目标状态是否未被锁定（带 verifySingle 参数的重载）
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <param name="verifySingle">是否验证单例锁</param>
        /// <returns></returns>
        public virtual bool IsUnlocked(NavigationStateType targetState, bool verifySingle)
        {
            if (verifySingle && LockType.HasFlag(NavigationLockType.Single))
                return true;
            return base.IsUnlocked(targetState);
        }

        /// <summary>
        /// 是否有入口loader
        /// </summary>
        /// <returns></returns>
        public bool HasEntrance()
        {
            foreach (var loader in Loaders)
            {
                if (loader.Entrance)
                    return true;
            }

            return Cache.HasEntrance();
        }

        public override async UniTask SetLogicalVisible(bool visible)
        {
            if (LockType.HasFlag(NavigationLockType.SetLogicalVisible))
            {
                return;
            }

            if (visible)
            {
                // 执行缓存还原
                if (Cache.CurState >= NavigationClearType.AllRecover)
                {
                    StartRecover();
                    await Cache.Recover(OpenAsync);
                    EndRecover();
                }

                foreach (var loader in SafeForeach(Loaders))
                {
                    await loader.SetLogicalVisible(true);
                }
            }
            else
            {
                foreach (var loader in SafeForeach(Loaders))
                {
                    await loader.SetLogicalVisible(false);
                }
            }

            foreach (var child in Children)
            {
                await child.SetLogicalVisible(visible);
            }
        }

        /// <summary>
        /// 判断是否为全屏组
        /// </summary>
        /// <returns></returns>
        public override bool IsFullScreen()
        {
            foreach (var loader in Loaders)
            {
                if (loader.IsFullScreen())
                    return true;
            }

            return false;
        }

        public override bool FullScreenAndLogicalVisible()
        {
            foreach (var loader in Loaders)
            {
                if (loader.FullScreenAndLogicalVisible()) return true;
            }

            // 逆序遍历判断子容器，一般性能更好，因为更靠后的容器更有可能是全屏且逻辑可见的
            var children = Children;
            var count = children.Count;
            for (var i = count - 1; i >= 0; i--)
            {
                if (children[i].FullScreenAndLogicalVisible()) return true;
            }

            return false;
        }

        /// <summary>
        /// 是否全屏且正在渲染
        /// </summary>
        public override bool FullScreenAndRendering()
        {
            foreach (var loader in Loaders)
            {
                if (loader.FullScreenAndRendering()) return true;
            }

            // 逆序遍历判断子容器，一般性能更好，因为更靠后的容器更有可能是全屏且正在渲染的
            var children = Children;
            var count = children.Count;
            for (var i = count - 1; i >= 0; i--)
            {
                if (children[i].FullScreenAndRendering()) return true;
            }

            return false;
        }

        #region Rendering Signature (画面构成签名)

        /// <summary>
        /// 采集"当前渲染画面构成的 View 签名"，用于跨帧比对检测画面变化（如延迟截图策略）。
        /// <para>
        /// 遍历顺序固定（Loaders 正序 + Children 逆序递归），因此只要树结构不变，
        /// 相同的渲染构成会产生"引用序列完全一致"的列表。
        /// </para>
        /// <para>调用前 <paramref name="output"/> 不会被清空，由调用方负责重置。</para>
        /// </summary>
        /// <param name="mode">检测策略。<see cref="RenderingSignatureMode.None"/> 时直接返回，不做任何收集。</param>
        /// <param name="output">输出列表（不可为 null）。</param>
        public void CollectRenderingSignature(RenderingSignatureMode mode, List<ViewBase> output)
        {
            if (output == null) return;
            if (mode == RenderingSignatureMode.None) return;

            switch (mode)
            {
                case RenderingSignatureMode.TopmostFullScreen:
                {
                    var top = FindTopmostFullScreenRenderingLoader();
                    if (top != null && top.View != null) output.Add(top.View);
                    break;
                }
                case RenderingSignatureMode.AboveTopmostFullScreen:
                {
                    var top = FindTopmostFullScreenRenderingLoader();
                    if (top == null) return;
                    var baselineLayer = top.Layer;
                    // 先收集基准 View 自身，再收集其 Layer 之上的所有渲染 View
                    if (top.View != null) output.Add(top.View);
                    CollectRenderingViewsAboveLayer(baselineLayer, top, output);
                    break;
                }
            }
        }

        /// <summary>
        /// 查找最顶层的"全屏且正在渲染"加载器（深度优先、逆序子容器优先，更贴近 top-most 语义）。
        /// </summary>
        private NavigationLoader FindTopmostFullScreenRenderingLoader()
        {
            // 逆序子容器优先（越靠后的子容器通常越顶层）
            var children = Children;
            for (var i = children.Count - 1; i >= 0; i--)
            {
                var found = children[i].FindTopmostFullScreenRenderingLoader();
                if (found != null) return found;
            }

            // 在本容器内逆序查找，取"最后一个"命中者作为顶层
            var loaders = Loaders;
            for (var i = loaders.Count - 1; i >= 0; i--)
            {
                if (loaders[i].FullScreenAndRendering()) return loaders[i];
            }

            return null;
        }

        /// <summary>
        /// 收集"Layer 严格大于 baselineLayer"的所有正在渲染的加载器对应的 View。
        /// 遇到基准自身时跳过（避免重复）。遍历全部子树，顺序固定以便跨帧稳定比对。
        /// </summary>
        private void CollectRenderingViewsAboveLayer(int baselineLayer, NavigationLoader skip, List<ViewBase> output)
        {
            foreach (var loader in Loaders)
            {
                if (loader == skip) continue;
                if (!loader.Rendering) continue;
                if (loader.Layer <= baselineLayer) continue;
                if (loader.View != null) output.Add(loader.View);
            }

            foreach (var child in Children)
            {
                child.CollectRenderingViewsAboveLayer(baselineLayer, skip, output);
            }
        }

        #endregion

        /// <summary>
        /// 获取第一个加载器
        /// </summary>
        /// <returns></returns>
        public NavigationLoader GetFirstLoader()
        {
            if (CurrentState == NavigationStateType.Clear)
                return Cache.GetFirstLoader();
            return Loaders.Count == 0 ? null : Loaders[0];
        }

        /// <summary>
        /// 获取最后一个加载器
        /// </summary>
        /// <returns></returns>
        public NavigationLoader GetLastLoader()
        {
            if (CurrentState == NavigationStateType.Clear)
                return Cache.GetLastLoader();
            return Loaders.Count == 0 ? null : Loaders[Loaders.Count - 1];
        }

        /// <summary>
        /// NavigationBehaviour：还原导航容器对象
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Parent = null;
            Loaders.Clear();
            Children.Clear();
            afterLoaderStateChange.Clear();
            beforeLoaderStateChange.Clear();
            EffectOther = true;
            RelationshipChild = true;
            Recovering = false;
            Cache.Reset();
        }

        // /// <summary>
        // /// 结束转场
        // /// </summary>
        // public void EndTransition()
        // {
        //     if (!Transitioning) return;
        //     // 第一个是执行转场效果的加载器
        //     _transitioningLoaders[0].EndTransition();
        // }

        /// <summary>
        /// 检查当前状态是否允许切换到目标状态
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <returns></returns>
        /// <exception cref="NavigationVerifyException"></exception>
        public override bool IsStateValid(NavigationStateType targetState)
        {
            var passVerify = true;

            //已关闭后不能进行其他操作
            if (CurrentState == NavigationStateType.Close)
                passVerify = false;
            //清理操作根据清理类型决定是否可操作
            else if (targetState == NavigationStateType.Clear)
                passVerify = Cache.VerifyRepeat();

            if (!passVerify)
            {
                Log.Debug($"导航容器:'{Name}'在状态:{CurrentState}中,不允许执行操作:{targetState}");
            }

            return passVerify;
        }

        //开始还原组
        private void StartRecover()
        {
            EffectOther = false;
            Recovering = true;
        }

        //结束还原组
        private void EndRecover()
        {
            EffectOther = true;
            Recovering = false;
        }

        internal bool ValidateLoaderOpen(NavigationLoader loader)
        {
            var pass = IsUnlocked(NavigationStateType.Open);
            if (!pass)
            {
                Log.Error($"导航容器:'{Name}'在处于锁定中:{LockType}，不允许打开:{loader.ViewOptions?.AssetName}");
                return false;
            }

            if (PendingState == NavigationStateType.Close)
            {
                Log.Error($"导航容器:'{Name}'在处于状态转换{CurrentState}->{PendingState}中，" +
                          $"不允许打开:{loader.ViewOptions?.AssetName}");
                return false;
            }

            return true;
        }

        internal async UniTask BeforeLoaderOpen(NavigationLoader loader,
            CancellationToken cancellationToken = default)
        {
            // 检测当前容器是否已经加入到导航系统,如果未加入,则加入到导航系统
            if (Parent == null)
            {
                Log.Debug($"导航容器:'{Name}'未加入到导航系统, 已自动加入");
                AddToSystem(this);
            }

            if (loader.Mode.HasFlag(NavigationMode.JumpToContainer) && JumpToContainerAsync != null)
            {
                await JumpToContainerAsync.Invoke(this, cancellationToken);
            }
        }

        //处理事件：加载器中的场景/界面关闭完成
        private void OnLoaderCloseFinished(NavigationLoader loader)
        {
            if (Loaders.Remove(loader))
            {
                Log.Debug($"Remove loader: {loader.Name} from container: {Name}");
            }
            else
            {
                throw new NavigationException($"[{nameof(OnLoaderCloseFinished)}] " +
                                              $"Failed to remove '{loader.Name}' from container:{Name}");
            }

            if (!Cache.IsLoaderCached(loader))
            {
                NavigationFactory.Instance.Recycle(loader);
            }
            else if (!HasEntrance())
            {
                // 若Loader关闭后没有入口,则自动指定一个新入口
                var firstLoader = GetFirstLoader();
                if (firstLoader != null)
                    firstLoader.Entrance = true;
            }
        }

        //NavigationBehaviour:改变状态
        protected override async UniTask PostChangeStateAsync(NavigationStateType targetState)
        {
            //只清理内存不改变当前状态,完全清理后改为清理状态
            if (targetState == NavigationStateType.Clear && Cache.Visible())
                return;
            await base.PostChangeStateAsync(targetState);
        }

        internal async UniTask BeforeLoaderChangeStateAsync(NavigationLoader child, NavigationStateType targetState)
        {
            if (beforeLoaderStateChange != null)
                await beforeLoaderStateChange.InvokeAsync(child, targetState);

            if (targetState is NavigationStateType.Open)
            {
                if (CurrentState != targetState && PendingState == NavigationStateType.None)
                {
                    // 跟着子级一起改变状态
                    await PreChangeStateAsync(targetState);
                }
            }
        }

        internal async UniTask AfterLoaderStateChange(NavigationLoader child, NavigationStateType targetState)
        {
            try
            {
                if (afterLoaderStateChange != null)
                    await afterLoaderStateChange.InvokeAsync(child, targetState);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            switch (targetState)
            {
                case NavigationStateType.Open:
                {
                    if (PendingState == targetState)
                    {
                        // 跟着子级一起改变状态
                        await PostChangeStateAsync(targetState);
                    }

                    break;
                }
                case NavigationStateType.Close:
                {
                    OnLoaderCloseFinished(child);

                    if (PendingState != targetState)
                    {
                        // 如果container本身不处于状态改变过程中，在子loader Close 后需要判断是否切换状态
                        // 典型的场景：
                        //      container中仅有/仅剩一个loader，此时外部调用loader关闭
                        //      当loader关闭流程结束，逻辑走到AfterLoaderStateChange时，container需要跟着切换为Close状态
                        await ChangeStateWhenAllChildrenChanged(targetState);
                    }

                    break;
                }
            }
        }

        private async UniTask ChangeStateWhenAllChildrenChanged(NavigationStateType targetState)
        {
            var allChange = true;
            foreach (var loader in Loaders)
            {
                if (loader.CurrentState == targetState) continue;

                allChange = false;
                break;
            }

            if (allChange)
            {
                // 全部都改变了才改变状态
                var ok = await PreChangeStateAsync(targetState);
                if (ok)
                {
                    await PostChangeStateAsync(targetState);
                }
                else
                {
                    Log.Error($"Failed to change state to {targetState} when all children changed, container:'{Name}'");
                }
            }
        }
    }
}
