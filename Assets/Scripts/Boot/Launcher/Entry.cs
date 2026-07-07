using System;
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
        [SerializeField]
        private BootStartupSettings startupSettings = new BootStartupSettings();

        [SerializeField]
        private MonoBehaviour startupView;

        private BootLoader _loader;
        private bool _isRunning;

        private void Awake()
        {
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
            _loader?.Dispose();
        }
    }
}
