using Framework.Asset;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 层反射入口（分层启动链 Phase 1）。
    /// 被 Boot 层通过 <c>BootStartupSettings.StartupTypeName</c> 反射调用，
    /// 创建 Core root scope。Core 程序集不引用 Boot，入口靠程序集限定名契约绑定。
    /// </summary>
    public static class CoreStartup
    {
        private static CoreLifetimeScope _rootScope;

        public static void Start(IAssetRuntime bootAssetRuntime = null)
        {
            if (_rootScope != null)
                return;

            var root = new UnityEngine.GameObject(nameof(CoreLifetimeScope));
            UnityEngine.Object.DontDestroyOnLoad(root);
            CoreLifetimeScope.PendingBootAssetRuntime = bootAssetRuntime;
            _rootScope = root.AddComponent<CoreLifetimeScope>();
        }

        /// <summary>
        /// 显式重置 Core root scope（Repair 场景）。销毁现有 scope 并清空静态引用，
        /// 使下次 <see cref="Start"/> 重建完整启动链。被 Boot 层 <c>Entry.Repair</c>
        /// 反射调用（Boot 不编译期引用 Core）。
        /// </summary>
        public static void Reset()
        {
            if (_rootScope == null)
                return;

            var scope = _rootScope;
            _rootScope = null;
            if (scope != null)
                UnityEngine.Object.Destroy(scope.gameObject);
        }
    }
}
