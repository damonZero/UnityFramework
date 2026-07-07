using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Framework.Asset;
using Framework.Log;
using Framework.RuntimeLog;
using UnityEngine;

namespace Boot
{
    public sealed class BootUpdateRunner : IDisposable
    {
        private readonly BootBridge _bridge;
        private readonly BootStartupSettings _settings;
        private readonly IBootStartupView _view;
        private readonly IAssetRuntime _assetRuntime;
        private bool _disposed;
        private bool _assetRuntimeTransferred;

        private BootUpdateRunner(BootBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _settings = bridge.Settings ?? throw new ArgumentNullException(nameof(bridge), "bridge.Settings is null");
            _view = bridge.View;
            _assetRuntime = AssetRuntimeFactory.Create();
            _assetRuntime.WrapFromExistingPackage(bridge.Config, bridge.Package);
        }

        /// <summary>
        /// Entry point reflected by the AOT shell BootLoader once the hot-update
        /// assemblies (including this one) are loaded.
        /// </summary>
        public static void Start(BootBridge bridge)
        {
            if (bridge == null)
                throw new ArgumentNullException(nameof(bridge));

            new BootUpdateRunner(bridge).RunAsync().Forget();
        }

        public async UniTask RunAsync()
        {
            GameLog.Info("[Boot] Startup begin", "Boot");
            _view?.SetRepairVisible(false);
            _view?.SetProgress(0f);

            BootRuntimeLogBootstrap.EnsureInstalled(_settings);
            ReplayEarlyLogs();
            await UpdateAssetsAsync();
            StartGame();
            RuntimeLogManager.Flush();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (!_assetRuntimeTransferred)
                _assetRuntime.Shutdown();
        }

        private async UniTask UpdateAssetsAsync()
        {
            if (!_settings.EnableAssetUpdate)
                return;

            GameLog.Info("[Boot] Checking resources", "Boot.Asset");
            _view?.SetStatus("Checking resources");
            var manifest = _assetRuntime.UpdateManifest();
            while (!manifest.IsVersionDone)
            {
                _view?.SetProgress(Mathf.Lerp(0.05f, 0.2f, manifest.Progress * 2f));
                await UniTask.Yield();
            }

            if (!manifest.IsVersionSucceeded)
                throw new InvalidOperationException($"[Boot] Resource version request failed: {manifest.Error}");

            if (!manifest.IsManifestDone)
                manifest.StartManifest();

            while (!manifest.IsDone)
            {
                _view?.SetProgress(Mathf.Lerp(0.2f, 0.35f, Mathf.Clamp01((manifest.Progress - 0.5f) * 2f)));
                await UniTask.Yield();
            }

            if (!manifest.IsSucceeded)
                throw new InvalidOperationException($"[Boot] Resource manifest update failed: {manifest.Error}");

            var downloader = string.IsNullOrWhiteSpace(_settings.AssetDownloadTag)
                ? _assetRuntime.CreateDownloader()
                : _assetRuntime.CreateDownloader(_settings.AssetDownloadTag);

            if (downloader.TotalDownloadCount <= 0)
            {
                GameLog.Info("[Boot] Resource update skipped; no downloads", "Boot.Asset");
                _view?.SetProgress(0.35f);
                return;
            }

            GameLog.Info($"[Boot] Downloading resources: {downloader.TotalDownloadCount}", "Boot.Asset");
            _view?.SetStatus("Updating resources");
            downloader.Start();
            while (!downloader.IsDone)
            {
                _view?.SetProgress(Mathf.Lerp(0.35f, 0.65f, downloader.Progress));
                await UniTask.Yield();
            }

            if (!downloader.IsSucceeded)
                throw new InvalidOperationException($"[Boot] Resource update failed: {downloader.Error}");

            _view?.SetProgress(0.65f);
        }

        private void ReplayEarlyLogs()
        {
            foreach (var entry in _bridge.EarlyLogs)
            {
                if (entry == null)
                    continue;

                RuntimeLogManager.Current?.Write(new RuntimeLogEntry
                {
                    Level = ToGameLogLevel(entry.Level),
                    Module = "Boot.AOT",
                    Category = "Boot.AOT",
                    Phase = "Boot",
                    Message = entry.Message,
                    ExceptionType = null,
                    ExceptionMessage = null,
                    StackTrace = null
                });
            }
        }

        private static GameLogLevel ToGameLogLevel(BootStartupLogLevel level) => level switch
        {
            BootStartupLogLevel.Warn => GameLogLevel.Warning,
            BootStartupLogLevel.Error => GameLogLevel.Error,
            _ => GameLogLevel.Information
        };

        private void StartGame()
        {
            GameLog.Info("[Boot] Starting game", "Boot");
            _view?.SetStatus("Starting game");
            _view?.SetProgress(0.95f);

            if (string.IsNullOrWhiteSpace(_settings.StartupTypeName))
                throw new InvalidOperationException("[Boot] Startup type name is empty.");

            var type = Type.GetType(_settings.StartupTypeName, throwOnError: false);
            if (type == null)
                throw new InvalidOperationException($"[Boot] Startup type not found: {_settings.StartupTypeName}");

            var methodName = string.IsNullOrWhiteSpace(_settings.StartupMethodName)
                ? "Start"
                : _settings.StartupMethodName;
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException($"[Boot] Startup method not found: {_settings.StartupTypeName}.{methodName}");

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                method.Invoke(null, Array.Empty<object>());
            else if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(_assetRuntime))
            {
                method.Invoke(null, new object[] { _assetRuntime });
                _assetRuntimeTransferred = true;
            }
            else
                throw new InvalidOperationException($"[Boot] Startup method signature is unsupported: {_settings.StartupTypeName}.{methodName}");

            _view?.SetProgress(1f);
        }
    }
}
