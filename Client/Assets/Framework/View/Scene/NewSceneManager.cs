// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// ********************************************************************

using Framework.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using ThreadPriority = UnityEngine.ThreadPriority;

using Cysharp.Threading.Tasks;

using Framework.ViewCache;
using Object = System.Object;

namespace Framework.View
{
    public class SceneManager : ISceneManager
    {
        /// <summary>
        /// 当前激活的场景（新建的物体会自动放到此场景下、光照贴图会使用此场景的）
        /// the active Scene is the Scene which will be used as the target for new GameObjects instantiated by scripts and from what Scene the lighting settings are used.
        /// https://docs.unity3d.com/2018.4/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html
        /// </summary>
        public BaseScene ActiveScene { get; private set; }

        /// <summary>
        /// 场景完成所有准备工作，变为激活状态（新建的物体会自动放到此场景下、光照贴图会使用此场景的）
        /// the active Scene is the Scene which will be used as the target for new GameObjects instantiated by scripts and from what Scene the lighting settings are used.
        /// https://docs.unity3d.com/2018.4/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html
        /// </summary>
        public event Action<BaseScene> SceneActive;

        /// <summary>
        /// 事件：卸载场景完成
        /// </summary>
        public event Action<Scene> SceneUnloaded;


        /// <summary>
        /// 刚加载完成、还未被使用的Scene会放在这个列表中
        /// </summary>
        private readonly List<Scene> _justLoadedScenes = new();

        /// <summary>
        /// 所有已被加载并使用的场景（包含当前ActiveScene和其它非激活/缓存的）
        /// </summary>
        public List<BaseScene> AllScenes { get; } = new();

        /// <summary>
        /// 正在加载的场景名字
        /// </summary>
        public List<string> LoadingScenes { get; } = new();

        /// <summary>
        /// 加载线程优先级
        /// https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-backgroundLoadingPriority.html
        /// </summary>
        private readonly ThreadPriority _defaultLoadingPriority;

        public SceneManager()
        {
            /*
             * The default value is ThreadPriority.BelowNormal, however some platforms override it:
                    Universal Windows Platform - ThreadPriority.High
                    Consoles - ThreadPriority.Normal
             */
            _defaultLoadingPriority = Application.backgroundLoadingPriority;
            if (Application.platform is RuntimePlatform.Android or RuntimePlatform.IPhonePlayer)
            {
                Log.Debug($"Application.backgroundLoadingPriority is {_defaultLoadingPriority}");
                if (_defaultLoadingPriority != ThreadPriority.BelowNormal)
                {
                    Log.Error($"获取 Application.backgroundLoadingPriority 为 {_defaultLoadingPriority}, " +
                                        $"不是预期的 {ThreadPriority.BelowNormal}，需要排查原因");
                }
            }
        }

        public virtual void Init()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public virtual void Shutdown()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        public virtual bool HasRenderingScene()
        {
            return ActiveScene != null;
        }

        public async UniTask<ViewBase> OpenAsync(IViewOptions options, CancellationToken cancelToken = default)
        {
            // 1. 加载场景
            var baseScene = await LoadSceneAsync(options.AssetName, cancelToken);

            // 2. 激活场景
            var setActiveResult = await ChangeActiveScene(baseScene);

            // 3. 场景执行Open流程
            if (setActiveResult)
            {
                // 设置参数
                baseScene.LifeCycleExecutor = options.LifeCycleExecutor ?? this;
                // 添加组件
                if (options.Components != null)
                {
                    foreach (var component in options.Components)
                    {
                        baseScene.AddViewComponent(component);
                    }
                }

                var param = new LifeCycleArgs(LifeCycleCause.Open, options.Data, cancelToken);
                await ((IViewLifeCycle)baseScene).ExecuteOpen(param);
            }

            return baseScene;
        }

        protected async UniTask<BaseScene> LoadSceneAsync(string sceneName, CancellationToken cancelToken)
        {
            if (sceneName.EndsWith(".unity"))
            {
                sceneName = Path.GetFileNameWithoutExtension(sceneName);
            }

            BeforeLoadScene(sceneName);

            // 开始加载场景
            await LoadUnitySceneAsync(sceneName, LoadSceneMode.Additive);

            // 等待场景加载完成
            Scene loadedScene = default;
            while (true)
            {
                for (var i = 0; i < _justLoadedScenes.Count; i++)
                {
                    var scene = _justLoadedScenes[i];

                    if (scene.name == sceneName)
                    {
                        loadedScene = scene;
                        _justLoadedScenes.RemoveAt(i);
                        break;
                    }
                }

                if (loadedScene.handle != 0) break;

                await UniTask.Yield();
            }

            var baseScene = AfterLoadScene(loadedScene);

            // 如果操作取消，则卸载场景
            if (cancelToken.IsCancellationRequested || baseScene == null)
            {
                await UnloadSceneAsync(loadedScene);
                return null;
            }

            return baseScene;
        }

        /// <summary>
        /// 改变激活场景
        /// </summary>
        /// <param name="baseScene"></param>
        /// <returns></returns>
        private async UniTask<bool> ChangeActiveScene(BaseScene baseScene)
        {
            await PreActiveSceneChange();

            var unityScene = baseScene.UnityScene;
            var setActiveResult = UnityEngine.SceneManagement.SceneManager.SetActiveScene(unityScene);
            if (!setActiveResult)
            {
                Log.Error($"Failed to SetActiveScene({{" +
                               $"handle:{unityScene.handle}, name:{unityScene.name}, " +
                               $"isValid:{unityScene.IsValid()}, isLoaded:{unityScene.isLoaded}}})");
            }

            await PostActiveSceneChange();

            return setActiveResult;
        }

        /// <summary>
        /// 查找GameObject所在场景对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public BaseScene FindScene(GameObject obj)
        {
            return FindScene(obj.scene);
        }

        /// <summary>
        /// 根据Unity Scene查找场景对象
        /// </summary>
        /// <param name="unityScene"></param>
        /// <returns></returns>
        public virtual BaseScene FindScene(Scene unityScene)
        {
            return FindAndCacheBaseScene(unityScene);
        }

        /// <summary>
        /// 根据名字查找场景对象
        ///
        /// 注意：不排除同时存在多个同名场景的情况，这里只会返回一个
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public virtual BaseScene FindScene(string sceneName)
        {
            foreach (var scene in AllScenes)
            {
                if (scene.AssetName == sceneName)
                {
                    return scene;
                }
            }

            return null;
        }

        #region protected

        protected virtual void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
        {
            Log.Debug($"{nameof(OnSceneLoaded)} : {loadedScene.name}" +
                                $"(IsValid:{loadedScene.IsValid()}, isLoaded:{loadedScene.isLoaded}), " +
                                $"mode:{mode}");
            _justLoadedScenes.Add(loadedScene);
        }

        /// <summary>
        /// 激活场景改变
        ///
        /// 两种情况下会触发：
        ///     1. OpenAsync -> SetActiveScene -> 触发 OnActiveSceneChanged
        ///     2. UnloadSceneAsync -> 场景卸载后，Unity自动设置新的激活场景 -> 触发 OnActiveSceneChanged
        /// </summary>
        protected virtual void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            Log.Debug($"{nameof(OnActiveSceneChanged)} : {oldScene.name} -> {newScene.name}" +
                      $"(IsValid:{newScene.IsValid()}, isLoaded:{newScene.isLoaded})");

            var baseScene = FindScene(newScene);
            ActiveScene = baseScene;
        }

        protected virtual void OnSceneUnloaded(Scene unloadedScene)
        {
            Log.Debug($"{nameof(OnSceneUnloaded)} : {unloadedScene.name}" +
                                $"(IsValid:{unloadedScene.IsValid()}, isLoaded:{unloadedScene.isLoaded})");
        }

        /// <summary>
        /// 加载场景之前的逻辑
        /// </summary>
        /// <param name="sceneName"></param>
        protected virtual void BeforeLoadScene(string sceneName)
        {
            Log.Debug($"{nameof(BeforeLoadScene)} : {sceneName}");

            // FIXME by fred 改进这个内存统计实现方式（因为过程中可能有其它资源加载）
            StatisticsFactory.Get<MemoryStatistics>().BeforeTake(sceneName);

            // 场景加载时把异步加载优先级设置最高，因为不需要考虑卡顿的问题
            if (LoadingScenes.Count == 0)
            {
                Application.backgroundLoadingPriority = ThreadPriority.High;

                if (Application.isPlaying && !Application.isEditor)
                {
                    QualitySettings.streamingMipmapsMemoryBudget = 10;
                    QualitySettings.streamingMipmapsMaxLevelReduction = 5;
                }
            }

            LoadingScenes.Add(sceneName);
        }


        /// <summary>
        /// Unity Scene完成加载之后的逻辑
        /// </summary>
        /// <param name="loadedScene"></param>
        /// <returns></returns>
        protected virtual BaseScene AfterLoadScene(Scene loadedScene)
        {
            Log.Debug($"{nameof(AfterLoadScene)} : {loadedScene.name}" +
                      $"(isValid:{loadedScene.IsValid()}, isLoaded:{loadedScene.isLoaded}))");

            LoadingScenes.Remove(loadedScene.name);

            // 恢复默认优先级（如果有多个场景同时加载，等最后一个场景加载完成后再恢复优先级）
            if (LoadingScenes.Count == 0)
            {
                Application.backgroundLoadingPriority = _defaultLoadingPriority;
                if (Application.isPlaying && !Application.isEditor)
                {
                    QualitySettings.streamingMipmapsMemoryBudget = 200;
                    QualitySettings.streamingMipmapsMaxLevelReduction = 2;
                }
            }

            StatisticsFactory.Get<MemoryStatistics>().AfterTake(loadedScene.name);

            // 添加场景对象到AllScenes
            var baseScene = FindAndCacheBaseScene(loadedScene);

            if (!baseScene)
            {
                GameLog.Error($"{loadedScene.name} 中未挂载 {nameof(BaseScene)} 组件，请检查场景！", module: "Framework.View");
            }

            return baseScene;
        }

        /// <summary>
        /// 找到场景中的BaseScene组件，缓存起来
        /// </summary>
        /// <param name="unityScene"></param>
        /// <returns></returns>
        private BaseScene FindAndCacheBaseScene(Scene unityScene)
        {
            foreach (var baseScene in AllScenes)
            {
                if (baseScene.UnityScene == unityScene) return baseScene;
            }

            if (unityScene.IsValid())
            {
                var baseScene = FindBaseSceneInUnityScene(unityScene);
                if (baseScene)
                {
                    AllScenes.Add(baseScene);
                    return baseScene;
                }
            }

            return null;
        }

        /// <summary>
        /// 把场景中带的BaseScene组件找出来
        /// </summary>
        /// <param name="unityScene"></param>
        /// <returns></returns>
        protected BaseScene FindBaseSceneInUnityScene(Scene unityScene)
        {
            // 返回所有已加载场景中的 BaseScene，再按 scene 过滤。
            // 此实现方式更快，因为Unity底层做了优化
            //      不需要遍历场景中所有的 GameObject 来找 BaseScene 组件了（尤其当场景中 GameObject 数量较多时）
            var all = UnityEngine.Object.FindObjectsByType(typeof(BaseScene),
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            Log.Debug($"BaseScene all count: {all.Length}");
            foreach (var obj in all)
            {
                if (obj is BaseScene bs && bs.gameObject.scene == unityScene)
                {
                    return bs;
                }
            }

            // var list = CollectionPool<List<GameObject>, GameObject>.Get();
            // // 遍历场景所有根节点，找到BaseScene组件（如果有多个，优先返回active的那个）
            // unityScene.GetRootGameObjects(list);
            // BaseScene baseScene = null;
            // foreach (var rootObject in list)
            // {
            //     var component = rootObject.GetComponentInChildren<BaseScene>();
            //     if (component == null) continue;
            //
            //     baseScene = component;
            //     if (component.gameObject.activeInHierarchy) break;
            // }
            // CollectionPool<List<GameObject>, GameObject>.Release(list);

            return null;
        }

        /// <summary>
        /// 激活场景改变之前
        /// </summary>
        private async UniTask PreActiveSceneChange()
        {
            if (ActiveScene == null) return;

            Log.Debug($"{nameof(PreActiveSceneChange)} : {ActiveScene}");
            try
            {
                await ActiveScene.BeforeLoseActive();
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            ActiveScene = null;
        }

        /// <summary>
        /// 激活场景改变之后
        /// </summary>
        protected async UniTask PostActiveSceneChange()
        {
            Log.Debug($"{nameof(PostActiveSceneChange)} : {ActiveScene}", ActiveScene);

            if (ActiveScene == null) return;

            try
            {
                await ActiveScene.AfterGainActive();
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            try
            {
                SceneActive?.Invoke(ActiveScene);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }
        }

        private async UniTask ExecuteCloseAsync(BaseScene scene)
        {
            var oldActiveScene = ActiveScene;
            var isActiveScene = oldActiveScene == scene;
            if (isActiveScene)
            {
                await PreActiveSceneChange();
            }

            // FIXME by fred 引入场景缓存机制，关闭场景时优先进入缓存，只有当缓存满了才真正卸载场景
            await UnloadSceneAsync(scene.UnityScene);

            if (isActiveScene)
            {
                var newActiveScene = ActiveScene;
                if (newActiveScene == null) return;

                if (newActiveScene != oldActiveScene)
                {
                    await PostActiveSceneChange();
                }
                else
                {
                    GameLog.Error($"{nameof(ExecuteCloseAsync)} : " +
                                   $"卸载激活场景'{oldActiveScene.AssetName}'失败了，激活场景({newActiveScene})没有改变！", module: "Framework.View");
                }
            }
        }

        private async UniTask UnloadSceneAsync(Scene loadedScene)
        {
            BeforeUnloadScene(loadedScene);

            await UnloadUnitySceneAsync(loadedScene);

            AfterUnloadScene(loadedScene);
        }

        protected virtual void BeforeUnloadScene(Scene loadedScene)
        {
            Log.Debug($"{nameof(UnloadSceneAsync)} : {loadedScene.name}" +
                      $"(IsValid:{loadedScene.IsValid()}, isLoaded:{loadedScene.isLoaded})");

            var list = CollectionPool<List<GameObject>, GameObject>.Get();
            try
            {
                // 先把场景所有gameObject销毁（解决加载场景比卸载快，导致逻辑异常的问题）
                //      1. Destroy不会立即执行，导致截图可能会出现2个场景重叠的情况
                //      2. rootGameObjects中的获取的GameObject可能为null
                loadedScene.GetRootGameObjects(list);
                foreach (var rootObject in list)
                {
                    if (rootObject) rootObject.SetActive(false);
                    UnityEngine.Object.Destroy(rootObject);
                }
            }
            finally
            {
                CollectionPool<List<GameObject>, GameObject>.Release(list);
            }
        }

        protected virtual void AfterUnloadScene(Scene unloadedScene)
        {
            Log.Debug($"{nameof(AfterUnloadScene)} : {unloadedScene.name}" +
                      $"(IsValid:{unloadedScene.IsValid()}, isLoaded:{unloadedScene.isLoaded})");

            try
            {
                SceneUnloaded?.Invoke(unloadedScene);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }
        }


        protected virtual UniTask LoadUnitySceneAsync(string sceneName, LoadSceneMode mode)
        {
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode).ToUniTask();
        }

        protected virtual async UniTask UnloadUnitySceneAsync(Scene loadedScene)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(loadedScene);

            while (loadedScene.IsValid())
            {
                await UniTask.Yield();
            }
        }

        #endregion

        #region IViewLifeCycleExecutor implementation

        async UniTask IViewLifeCycleExecutor.LifeCycleExecuteClose(IViewLifeCycle view, LifeCycleArgs args)
        {
            if (view is not BaseScene scene)
            {
                Log.Error($"Failed to close view, {view} is not {nameof(BaseScene)}");
                return;
            }

            await view.ExecuteClose(args);

            AllScenes.Remove(scene);
            await ExecuteCloseAsync(scene);
        }

        #endregion

    }
}
