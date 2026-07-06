using System;
using Cysharp.Threading.Tasks;
using Framework.Log;
using Framework.RuntimeLog;
using UnityEngine;

namespace Boot
{
    /// <summary>
    /// 游戏启动入口。只负责启动期资源/代码更新，再把正式游戏流程交给热更层入口。
    /// </summary>
    public class Entry : MonoBehaviour
    {
        [SerializeField]
        private BootStartupSettings startupSettings = new();

        [SerializeField]
        private MonoBehaviour startupView;

        private BootUpdateRunner _runner;
        private bool _isRunning;

        private void Awake()
        {
            BootRuntimeLogBootstrap.EnsureInstalled(startupSettings);
            DontDestroyOnLoad(gameObject);
            RunStartupAsync().Forget();
        }

        public void Repair()
        {
            if (_isRunning)
                return;

            RunStartupAsync().Forget();
        }

        private async UniTaskVoid RunStartupAsync()
        {
            _isRunning = true;
            var view = startupView as IBootStartupView;
            _runner?.Dispose();
            _runner = new BootUpdateRunner(startupSettings, view);

            try
            {
                await _runner.RunAsync();
            }
            catch (Exception e)
            {
                RuntimeLogManager.Current?.Write(new RuntimeLogEntry
                {
                    Level = GameLogLevel.Error,
                    Module = "Boot.Entry",
                    Category = "Boot.Entry",
                    Phase = "Boot",
                    Message = "Startup failed",
                    ExceptionType = e.GetType().FullName,
                    ExceptionMessage = e.Message,
                    StackTrace = e.ToString()
                });
                RuntimeLogManager.Flush();
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
            _runner?.Dispose();
            RuntimeLogManager.Flush();
        }
    }
}
