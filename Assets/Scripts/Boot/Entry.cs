using System.Collections;
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
            StartCoroutine(RunStartup());
        }

        public void Repair()
        {
            if (_isRunning)
                return;

            StartCoroutine(RunStartup());
        }

        private IEnumerator RunStartup()
        {
            _isRunning = true;
            var view = startupView as IBootStartupView;
            _runner?.Dispose();
            _runner = new BootUpdateRunner(startupSettings, view);

            var routine = _runner.Run();
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                        break;

                    current = routine.Current;
                }
                catch (System.Exception e)
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
                    _isRunning = false;
                    yield break;
                }

                yield return current;
            }

            _isRunning = false;
        }

        private void OnDestroy()
        {
            _runner?.Dispose();
            RuntimeLogManager.Flush();
        }
    }
}
