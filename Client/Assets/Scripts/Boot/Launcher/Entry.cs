using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Boot
{
    /// <summary>
    /// AOT game entry point (lives in the Launcher assembly). It must NOT reference
    /// any hot-update type directly. It constructs the AOT <see cref="BootLoader"/>
    /// which initializes YooAsset, loads the hot-update assemblies, and reflects
    /// <c>Boot.BootUpdateRunner.Start</c> to hand control to the hot-update layer.
    /// Early/startup errors are recorded via <see cref="BootStartupLog"/> (AOT).
    /// </summary>
    public class Entry : MonoBehaviour
    {
        /// <summary>AOT 入口单例（软重启时作为持久根 <c>GameRestart.bootComponent</c> 保留）。</summary>
        public static Entry Instance { get; private set; }

        [SerializeField]
        private BootStartupSettings startupSettings = new BootStartupSettings();

        [SerializeField]
        private MonoBehaviour startupView;

        private BootLoader _loader;
        private bool _isRunning;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RunStartupAsync().Forget();
        }

        public void Repair()
        {
            if (_isRunning)
                return;

            // Repair 需要重新走完整启动链。若 Core/General/Project scope 已创建（此前部分启动成功），
            // 先反射销毁 Core root scope（连带子 scope），重置静态引用，避免防重入拦截重建。
            // Boot 不编译期引用 Core，用与 Start 相同的反射契约。
            TryResetHotUpdateScope();

            RunStartupAsync().Forget();
        }

        /// <summary>
        /// 反射调用 <c>Core.Bootstrap.CoreStartup.Reset()</c> 销毁并重置 Core root scope。
        /// 仅当热更层已加载（程序集存在）时执行；失败静默（Repair 后 Start 会兜底重建）。
        /// </summary>
        private static void TryResetHotUpdateScope()
        {
            const string startupTypeName = "Core.Bootstrap.CoreStartup, Core";
            try
            {
                var type = Type.GetType(startupTypeName, throwOnError: false);
                var method = type?.GetMethod("Reset",
                    BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                    method.Invoke(null, Array.Empty<object>());
            }
            catch (Exception e)
            {
                // Reset 失败不阻塞 Repair；RunStartupAsync 会尝试重建。
                BootStartupLog.Warn($"[Entry] CoreStartup.Reset failed during Repair (scope may be stale): {e}");
            }
        }

        private async UniTaskVoid RunStartupAsync()
        {
            _isRunning = true;
            var view = startupView as IBootStartupView;
            _loader?.Dispose();
            _loader = new BootLoader(startupSettings, view);

            try
            {
                await _loader.RunAsync();
            }
            catch (Exception e)
            {
                BootStartupLog.Error($"[Entry] Startup failed: {e}");
                view?.SetStatus("Startup failed");
                view?.SetRepairVisible(true);
            }
            finally
            {
                _isRunning = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            _loader?.Dispose();
        }
    }
}
