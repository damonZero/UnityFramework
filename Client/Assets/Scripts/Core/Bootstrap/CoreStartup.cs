using System;
using Framework.Asset;
using Framework.Log;

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
            {
                // 同步释放容器：立即触发 SystemManager.ShutdownAll（逆序）/ ModelLifecycle.UnloadAll / 子 scope 级联，
                // 让旧系统停止 Tick。OnDestroy 里的 DisposeCore 幂等，后续 Object.Destroy 安全。
                scope.Dispose();
                UnityEngine.Object.Destroy(scope.gameObject);
            }

            // 级联清空子层静态 scope 引用：否则 Repair/软重启后 GeneralStartup/ProjectStartup 的
            // _scope 仍非空，Start 防重入会提前返回，导致下一层无法重建。
            // Core 不编译期引用 General/Project，沿用分层启动链反射契约。
            // 失败必须记录（而非静默丢弃）：否则 Repair/软重启后 General/Project 可能永不重建且无日志。
            LayerStartupReflector.InvokeReset("General.Bootstrap.GeneralStartup, General", LogResetFailure);
            LayerStartupReflector.InvokeReset("Project.Bootstrap.ProjectStartup, Project", LogResetFailure);
        }

        private static void LogResetFailure(string typeName, Exception error)
        {
            GameLog.Exception(error, $"[CoreStartup] Reset failed for {typeName}", "Core.Bootstrap.CoreStartup");
        }
    }
}
