using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Framework.Asset;
using Framework.Log;
using Framework.RuntimeLog;
using HybridCLR;
using UnityEngine;
using UnityEngine.Networking;

namespace Boot
{
    public sealed class BootUpdateRunner : IDisposable
    {
        private readonly BootStartupSettings _settings;
        private readonly IBootStartupView _view;
        private readonly IAssetRuntime _assetRuntime;
        private bool _disposed;
        private bool _assetRuntimeTransferred;

        public BootUpdateRunner(BootStartupSettings settings, IBootStartupView view)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view;
            _assetRuntime = AssetRuntimeFactory.Create();
        }

        public async UniTask RunAsync()
        {
            GameLog.Info("[Boot] Startup begin", "Boot");
            _view?.SetRepairVisible(false);
            _view?.SetProgress(0f);

            await InitializeAssetsAsync();
            await UpdateAssetsAsync();
            await LoadHotUpdateCodeAsync();
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

        private async UniTask InitializeAssetsAsync()
        {
            GameLog.Info("[Boot] Initializing resources", "Boot.Asset");
            _view?.SetStatus("Initializing resources");
            var config = Resources.Load<AssetConfig>("AssetConfig");
            var initHandle = _assetRuntime.BeginInitialize(config);
            while (!initHandle.IsDone)
            {
                _view?.SetProgress(Mathf.Lerp(0f, 0.05f, initHandle.Progress));
                await UniTask.Yield();
            }

            if (!initHandle.IsSucceeded)
            {
                var mode = config == null ? "<missing>" : config.Mode.ToString();
                var packageName = config == null ? "<missing>" : config.PackageName;
                var error = string.IsNullOrWhiteSpace(initHandle.Error)
                    ? _assetRuntime.LastError
                    : initHandle.Error;
                throw new InvalidOperationException(
                    $"[Boot] Asset runtime initialization failed. Mode={mode}, PackageName={packageName}, Error={error}");
            }

            _view?.SetProgress(0.05f);
            RuntimeLogManager.Current?.UpdateSessionInfo(info =>
            {
                if (config == null)
                    return;

                info.AssetPlayMode = config.Mode.ToString();
                info.AssetPackageName = config.PackageName;
            });
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

        private async UniTask LoadHotUpdateCodeAsync()
        {
            if (!_settings.EnableHotUpdate)
                return;

#if UNITY_EDITOR
            if (_settings.SkipHotUpdateInEditor)
            {
                GameLog.Info("[Boot] Hot update skipped in Editor", "Boot.HybridCLR");
                _view?.SetStatus("Using Editor assemblies");
                return;
            }
#endif

            GameLog.Info("[Boot] Loading metadata", "Boot.HybridCLR");
            _view?.SetStatus("Loading metadata");
            await LoadAotMetadataAsync();

            GameLog.Info("[Boot] Loading code", "Boot.HybridCLR");
            _view?.SetStatus("Loading code");
            await LoadHotUpdateAssembliesAsync();
            _view?.SetProgress(0.85f);
        }

        private async UniTask LoadAotMetadataAsync()
        {
            var entries = _settings.AotMetadataAssemblies;
            if (entries == null)
                return;

            RuntimeLogManager.Current?.UpdateSessionInfo(info =>
            {
                info.AotMetadataAssemblies.Clear();
                info.AotMetadataAssemblies.AddRange(entries
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.AssemblyName))
                    .Select(entry => entry.AssemblyName));
            });

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
                    continue;

                var bytes = await LoadBytesAsync(entry.AssetPath, entry.FileName, entry.ResourcesPath);
                if (bytes == null || bytes.Length == 0)
                    continue;

                var loadResult = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                if (loadResult != LoadImageErrorCode.OK)
                    throw new InvalidOperationException(
                        $"[Boot] Load AOT metadata failed: {entry.AssemblyName}, result={loadResult}");
            }
        }

        private async UniTask LoadHotUpdateAssembliesAsync()
        {
            var entries = _settings.HotUpdateAssemblies;
            if (entries == null || entries.Length == 0)
            {
                if (_settings.EnableHotUpdate)
                    throw new InvalidOperationException("[Boot] Hot-update assemblies are not configured on Entry.");
                return;
            }

            RuntimeLogManager.Current?.UpdateSessionInfo(info =>
            {
                info.HotUpdateAssemblies.Clear();
                info.HotUpdateAssemblies.AddRange(entries
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.AssemblyName))
                    .Select(entry => entry.AssemblyName));
            });

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
                    continue;

                if (IsAssemblyLoaded(entry.AssemblyName))
                    continue;

                var bytes = await LoadBytesAsync(entry.AssetPath, entry.FileName, entry.ResourcesPath);
                if (bytes == null || bytes.Length == 0)
                    throw new FileNotFoundException($"[Boot] Hot-update DLL not found: {entry.AssemblyName}");

                Assembly.Load(bytes);
            }
        }

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

        /// <summary>
        /// Loads bytes from a DLL/metadata entry using the following priority:
        /// 1. YooAsset raw asset path (always works on all platforms).
        /// 2. StreamingAssets file name — uses <see cref="UnityWebRequest"/> on Android
        ///    because <see cref="File.Exists"/> cannot read APK-embedded files.
        /// 3. Resources TextAsset path as final fallback.
        /// </summary>
        private async UniTask<byte[]> LoadBytesAsync(string assetPath, string fileName, string resourcesPath)
        {
            // Priority 1: YooAsset
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                var bytes = _assetRuntime.LoadRawBytes(assetPath);
                if (bytes.Length > 0)
                    return bytes;
            }

            // Priority 2: StreamingAssets (Android-safe via UnityWebRequest)
            // Note: non-Android path is synchronous; the async wrapper here is intentional for
            // API uniformity so callers don't need platform-specific call sites.
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var path = BuildStreamingAssetsPath(fileName);
#if UNITY_ANDROID && !UNITY_EDITOR
                using var request = UnityWebRequest.Get(path);
                request.SendWebRequest();
                while (!request.isDone)
                    await UniTask.Yield();
                if (request.result == UnityWebRequest.Result.Success && request.downloadHandler.data.Length > 0)
                    return request.downloadHandler.data;
#else
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
#endif
            }

            // Priority 3: Resources fallback
            if (!string.IsNullOrWhiteSpace(resourcesPath))
            {
                var asset = Resources.Load<TextAsset>(resourcesPath);
                if (asset != null)
                    return asset.bytes;
            }

            return Array.Empty<byte>();
        }

        private string BuildStreamingAssetsPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(_settings.StreamingAssetsRoot))
                return Path.Combine(Application.streamingAssetsPath, fileName);

            return Path.Combine(Application.streamingAssetsPath, _settings.StreamingAssetsRoot, fileName);
        }

        private static bool IsAssemblyLoaded(string assemblyName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
