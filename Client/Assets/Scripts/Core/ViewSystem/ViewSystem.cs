using System;
using Core.Systems;
using Core.Systems.Attributes;
using Core.UI;
using E7.NotchSolution;
using Framework.Asset;
using Framework.Coverage;
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
            CreateCoverageRoot(_uiRoot, safeRoot);

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
            NavigationSubSystem?.Update(deltaTime);
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

            if (UICamera.Camera != null)
            {
                Object.Destroy(UICamera.Camera.gameObject);
            }

            UICamera.Unbind();

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
            var uiCamera = CreateUICamera();

            var go = new GameObject("UIRoot", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = ResolveUiLayer(); // UI 相机只渲染 UI 层，UIRoot 必须挂到 UI 层（默认 layer 0 会渲染不到）

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = UICamera.DefaultPlaneDistance;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ScreenHelper.StandardWidth, ScreenHelper.StandardHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ScreenHelper.MatchWidthOrHeight;

            uiCamera.GetComponent<UICameraAdapter>().uiRootCanvas = canvas;
            UICamera.Bind(uiCamera, canvas);

            return (RectTransform)go.transform;
        }

        /// <summary>
        /// 创建专用 UI 相机：正交、只渲染 UI 层、ClearFlags=Depth（不清除颜色，让 3D 场景渲染在下层）。
        /// 对应参考项目 UI.unity 场景中的 UICamera 节点；KJ 无 UI 场景，改为运行时创建。
        /// </summary>
        private static Camera CreateUICamera()
        {
            var go = new GameObject(nameof(UICamera), typeof(Camera), typeof(UICameraAdapter));

            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.Depth;
            cam.cullingMask = 1 << ResolveUiLayer();
            cam.depth = 100f; // 高于主场景相机，UI 覆盖在 3D 之上

            return cam;
        }

        private static int ResolveUiLayer()
        {
            var layer = LayerMask.NameToLayer("UI");
            return layer >= 0 ? layer : 5; // Unity 默认 layer 5 = UI
        }

        /// <summary>
        /// 创建安全区根节点：SafePadding（E7 Notch Solution）驱动自身 RectTransform 铺满 UIRoot 并按
        /// Screen.safeArea 内缩边距。Form 均挂载到此节点下，保证刘海屏 / 底部指示条不遮挡 UI。
        /// </summary>
        private static RectTransform CreateSafeUIRoot(RectTransform uiRoot)
        {
            // IsMainSafePadding 必须在 Awake 前就位（Awake 里据此设 SafePadding.Instance），
            // 故先以 inactive 创建、赋值后再激活，确保 ScreenHelper 能读到 SafePadding.Instance。
            var go = new GameObject("SafeUIRoot", typeof(RectTransform));
            go.layer = ResolveUiLayer(); // 与 UIRoot 一致，保证 SafeUIRoot 在 UI 层
            go.SetActive(false);
            var safePadding = go.AddComponent<SafePadding>();
            safePadding.IsMainSafePadding = true;
            var rt = (RectTransform)go.transform;
            rt.SetParent(uiRoot, false);
            go.SetActive(true);
            return rt;
        }

        /// <summary>
        /// 创建 CoverageRoot（界面遮挡检测根节点）。对应参考项目 StartScreen.prefab 上的 CoverageRoot 组件
        /// （其 root 字段指向 SafeArea）。KJ 无 UI 场景，改为运行时创建，root 指向 SafeUIRoot。
        /// 缺失会导致 CoverageRoot.Global 恒 null，FormCoverage 在 FormPostShow 时 NRE。
        /// </summary>
        private static void CreateCoverageRoot(RectTransform uiRoot, RectTransform safeRoot)
        {
            var coverageRoot = uiRoot.gameObject.AddComponent<CoverageRoot>();
            coverageRoot.root = safeRoot;
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
        }
    }
}
