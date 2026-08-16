using System;
using Core.Systems;
using Core.Systems.Attributes;
using Core.UI;
using Framework.Asset;
using Framework.Log;
using Framework.Touch;
using Framework.View.Navigation;
using Framework.ViewCache;
using MessagePipe;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Core.ViewSystem
{
    /// <summary>
    /// View 系统（分层启动链 Core 层）。
    /// 负责创建 UI 根 Canvas，初始化并驱动界面/场景/导航三个子系统。
    /// 对应参考项目 AppSystems 的 ViewSystem，改为 KJ 的 [CoreSystem] + SystemManager 生命周期。
    /// </summary>
    [CoreSystem]
    public sealed class ViewSystem : ISystem, ITickableSystem
    {
        public static FormSubSystem FormSubSystem { get; private set; }
        public static SceneSubSystem SceneSubSystem { get; private set; }
        public static NavigationSubSystem NavigationSubSystem { get; private set; }

        private readonly IAssetSystem _assetSystem;
        private readonly IPublisher<FormLifecycleEvent> _formEventPublisher;
        private RectTransform _uiRoot;

        /// <summary>
        /// 晚于 AssetSystem(+0)、PoolService(+10)，确保 IAssetSystem 与池已就绪。
        /// </summary>
        public int Priority => AssetConstants.SystemPriority + 20;

        public ViewSystem(IAssetSystem assetSystem, IPublisher<FormLifecycleEvent> formEventPublisher)
        {
            _assetSystem = assetSystem ?? throw new ArgumentNullException(nameof(assetSystem));
            _formEventPublisher = formEventPublisher ?? throw new ArgumentNullException(nameof(formEventPublisher));
        }

        public void Init()
        {
            _uiRoot = CreateUIRoot();
            var safeRoot = CreateSafeUIRoot(_uiRoot);
            ScreenHelper.Init(_uiRoot.GetComponent<Canvas>(), _uiRoot, safeRoot);
            CreateEventSystem();

            FormSubSystem = new FormSubSystem();
            SceneSubSystem = new SceneSubSystem();
            NavigationSubSystem = new NavigationSubSystem();

            FormSubSystem.Init(safeRoot, _assetSystem, _formEventPublisher);
            SceneSubSystem.Init(_assetSystem);
            NavigationSubSystem.Init(FormSubSystem, SceneSubSystem, () => TransitionFactory.None);

            WireViewCacheDependencies();
        }

        public void Update(float deltaTime)
        {
            NavigationSubSystem.Update(deltaTime);
        }

        public void LateUpdate(float deltaTime)
        {
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
        }

        public void Shutdown()
        {
            NavigationSubSystem?.Shutdown();
            SceneSubSystem?.Shutdown();
            FormSubSystem?.Shutdown();

            NavigationSubSystem = null;
            SceneSubSystem = null;
            FormSubSystem = null;

            if (_uiRoot != null)
            {
                ScreenHelper.Cleanup();
                Object.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
            }

            UnwireViewCacheDependencies();
        }

        /// <summary>
        /// 接线 ViewCache 的资源桥接（对应参考项目 CacheSystem 对 CacheDependencies 的注入）。
        /// 与 PoolService 接线 PoolDependencies 同一模式：Framework 层 ViewCache 不引用资源系统，
        /// 由 Core 层注入 IAssetSystem 实现。
        /// </summary>
        private void WireViewCacheDependencies()
        {
            CacheDependencies.InstantiateGameObject = async (assetName, parent) =>
            {
                var prefab = await _assetSystem.LoadAssetAsync<GameObject>(assetName);
                if (prefab == null)
                {
                    GameLog.Error($"ViewCache 实例化失败，资源不存在：{assetName}", module: "Framework.ViewCache");
                    return null;
                }

                return Object.Instantiate(prefab, parent);
            };

            // KJ 的 IAssetSystem 暂未暴露资源静态内存查询，先返回 0 降级淘汰启发式，
            // 待 YooAsset 内存统计桥接后再替换为真实值（不影响正确性，仅影响缓存淘汰精度）。
            CacheDependencies.GetMemory = _ => 0;
        }

        private static void UnwireViewCacheDependencies()
        {
            CacheDependencies.InstantiateGameObject = null;
            CacheDependencies.GetMemory = null;
        }

        private static RectTransform CreateUIRoot()
        {
            var go = new GameObject("UIRoot", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ScreenHelper.StandardWidth, ScreenHelper.StandardHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ScreenHelper.MatchWidthOrHeight;

            return (RectTransform)go.transform;
        }

        /// <summary>
        /// 创建安全区根节点：铺满 UIRoot，由 ScreenHelper.ApplySafeArea 内缩边距。
        /// Form 均挂载到此节点下，保证刘海屏 / 底部指示条不遮挡 UI。
        /// </summary>
        private static RectTransform CreateSafeUIRoot(RectTransform uiRoot)
        {
            var go = new GameObject("SafeUIRoot", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(uiRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>
        /// 创建 EventSystem + 触摸输入模块（对应参考项目启动场景中预置的 EventSystem）。
        /// UGUI 的点击/拖拽等交互依赖它；KJ 的 CreateUIRoot 只建 Canvas，需在此补齐。
        /// </summary>
        private static void CreateEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneAdvInputModule));
            Object.DontDestroyOnLoad(go);
        }
    }
}
