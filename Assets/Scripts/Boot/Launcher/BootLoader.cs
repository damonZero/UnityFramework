using System;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Framework.Asset;
using HybridCLR;
using UnityEngine;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// AOT startup shell. Runs entirely on the AOT side:
    /// <list type="number">
    ///   <item>Loads <see cref="AssetConfig"/> (from the AOT-shared assembly).</item>
    ///   <item>Initializes YooAsset and creates the default package.</item>
    ///   <item>Downloads + loads every hot-update DLL (including Boot itself) via
    ///         YooAsset native raw-file API — never via the hot-update
    ///         <c>IAssetRuntime</c> (that would be an AOT -> hot-update reverse
    ///         reference).</item>
    ///   <item>Builds a <see cref="BootBridge"/> and reflects
    ///         <c>Boot.BootUpdateRunner.Start(bridge)</c> to hand control to the
    ///         hot-update layer.</item>
    /// </list>
    /// Must not reference any hot-update Framework.* type, <c>GameLog</c>, or
    /// <c>RuntimeLogManager</c>.
    /// </summary>
    public sealed class BootLoader : IDisposable
    {
        private readonly BootStartupSettings _settings;
        private readonly IBootStartupView _view;
        private ResourcePackage _package;
        private bool _disposed;

        public BootLoader(BootStartupSettings settings, IBootStartupView view)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view;
        }

        public void Dispose()
        {
            // The package is owned by AssetRuntime after WrapFromExistingPackage,
            // so the launcher must not release it. This is intentionally a no-op.
            _disposed = true;
        }

        public async UniTask RunAsync()
        {
            BootStartupLog.Info("[BootLoader] Startup begin");
            _view?.SetRepairVisible(false);
            _view?.SetProgress(0f);

            try
            {
                var config = LoadAssetConfig();
                _package = await InitializeYooAsset(config);
                _view?.SetProgress(0.1f);

                if (_settings.EnableHotUpdate)
                    await LoadHotUpdateCodeAsync();

                _view?.SetProgress(0.9f);
                var bridge = new BootBridge(_package, _settings, _view, config, BootStartupLog.Snapshot);
                ReflectBootUpdateRunnerStart(bridge);
                _view?.SetProgress(1f);
            }
            catch (Exception e)
            {
                BootStartupLog.Error($"[BootLoader] Startup failed: {e}");
                _view?.SetStatus("Startup failed");
                _view?.SetRepairVisible(true);
                throw;
            }
        }

        private AssetConfig LoadAssetConfig()
        {
            var config = Resources.Load<AssetConfig>("AssetConfig");
            if (config == null)
                throw new InvalidOperationException("[BootLoader] AssetConfig not found at Resources/AssetConfig. Create Assets/Resources/AssetConfig.asset.");
            return config;
        }

        private async UniTask<ResourcePackage> InitializeYooAsset(AssetConfig config)
        {
            YooAssets.Initialize();
            var packageName = string.IsNullOrWhiteSpace(config.PackageName) ? "DefaultPackage" : config.PackageName;
            var package = YooAssets.CreatePackage(packageName);
            var operation = package.InitializePackageAsync(BuildOptions(config, packageName));
            await operation.ToUniTask();
            if (operation.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"[BootLoader] YooAsset package initialization failed: {operation.Error}");

            // InitializePackageAsync only sets up the file system; it does NOT load or
            // activate the package manifest. Without an active manifest, LoadAssetSync
            // throws "Active package manifest not found" (which broke the whole boot
            // chain: AOT metadata + hot-update DLLs failed to load on device).
            // Request the version, then load the manifest to populate
            // FileSystemHost.ActiveManifest before any asset load.
            var versionOp = package.RequestPackageVersionAsync();
            await versionOp.ToUniTask();
            if (versionOp.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"[BootLoader] YooAsset request package version failed: {versionOp.Error}");

            var timeout = config.DownloadTimeout > 0 ? config.DownloadTimeout : 60;
            var manifestOp = package.LoadPackageManifestAsync(
                new LoadPackageManifestOptions(versionOp.PackageVersion, timeout));
            await manifestOp.ToUniTask();
            if (manifestOp.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"[BootLoader] YooAsset load package manifest failed: {manifestOp.Error}");

            BootStartupLog.Info($"[BootLoader] YooAsset ready: package={packageName}, version={versionOp.PackageVersion}");
            return package;
        }

        private InitializePackageOptions BuildOptions(AssetConfig config, string packageName)
        {
            switch (config.Mode)
            {
                case AssetConfig.PlayMode.EditorSimulate:
                    return new EditorSimulateModeOptions
                    {
                        EditorFileSystemParameters =
                            FileSystemParameters.CreateDefaultEditorFileSystemParameters(GetEditorSimulatePackageRoot(config))
                    };
                case AssetConfig.PlayMode.Offline:
                    return new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters =
                            FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                    };
                case AssetConfig.PlayMode.Host:
                    return new HostPlayModeOptions
                    {
                        BuiltinFileSystemParameters =
                            FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                        CacheFileSystemParameters = BuildSandboxParameters(config, packageName)
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(config.Mode), config.Mode, null);
            }
        }

        private static string GetEditorSimulatePackageRoot(AssetConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.EditorSimulatePackageRoot))
                return config.EditorSimulatePackageRoot;

            throw new InvalidOperationException(
                "[BootLoader] AssetConfig.EditorSimulatePackageRoot is empty. Run KJ/HybridCLR/Prepare YooAsset Editor Simulate Package (or Prepare Runtime Assets And Boot) before entering Play Mode.");
        }

        private FileSystemParameters BuildSandboxParameters(AssetConfig config, string packageName)
        {
            var cdnBaseUrl = string.IsNullOrWhiteSpace(config.CdnBaseUrl)
                ? "http://127.0.0.1:8080/CDN"
                : config.CdnBaseUrl;
            var parameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
                new BootRemoteService(cdnBaseUrl), packageName);
            parameters.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, config.DownloadMaxConcurrency);
            parameters.AddParameter(EFileSystemParameter.DownloadWatchdogTimeout, config.DownloadTimeout);
            return parameters;
        }

        private UniTask LoadHotUpdateCodeAsync()
        {
            if (!_settings.EnableHotUpdate)
                return UniTask.CompletedTask;

#if UNITY_EDITOR
            if (_settings.SkipHotUpdateInEditor)
            {
                BootStartupLog.Info("[BootLoader] Hot update skipped in Editor");
                _view?.SetStatus("Using Editor assemblies");
                return UniTask.CompletedTask;
            }
#endif

            BootStartupLog.Info("[BootLoader] Loading AOT metadata");
            _view?.SetStatus("Loading metadata");
            LoadAotMetadata();

            BootStartupLog.Info("[BootLoader] Loading code");
            _view?.SetStatus("Loading code");
            LoadHotUpdateAssemblies();
            return UniTask.CompletedTask;
        }

        private void LoadAotMetadata()
        {
            foreach (var entry in _settings.AotMetadataAssemblies)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
                    continue;

                var bytes = LoadRawBytes(entry.AssetPath);
                if (bytes == null || bytes.Length == 0)
                    continue;

                var result = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                if (result != LoadImageErrorCode.OK)
                    throw new InvalidOperationException(
                        $"[BootLoader] Load AOT metadata failed: {entry.AssemblyName}, result={result}");
            }
        }

        private void LoadHotUpdateAssemblies()
        {
            foreach (var entry in _settings.HotUpdateAssemblies)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
                    continue;

                if (IsAssemblyLoaded(entry.AssemblyName))
                    continue;

                var bytes = LoadRawBytes(entry.AssetPath);
                if (bytes == null || bytes.Length == 0)
                    throw new FileNotFoundException($"[BootLoader] Hot-update DLL not found: {entry.AssemblyName}");

                Assembly.Load(bytes);
            }
        }

        private byte[] LoadRawBytes(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || _package == null)
                return Array.Empty<byte>();

            var handle = _package.LoadAssetSync<RawFileObject>(assetPath);
            try
            {
                if (handle.Status != EOperationStatus.Succeeded)
                    return Array.Empty<byte>();

                var rawFile = handle.GetAssetObject<RawFileObject>();
                return rawFile?.GetBytes() ?? Array.Empty<byte>();
            }
            finally
            {
                handle.Release();
            }
        }

        private static bool IsAssemblyLoaded(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void ReflectBootUpdateRunnerStart(BootBridge bridge)
        {
            BootStartupLog.Info("[BootLoader] Handing control to hot-update Boot layer");
            var type = Type.GetType("Boot.BootUpdateRunner, Boot");
            if (type == null)
                throw new InvalidOperationException("[BootLoader] Could not resolve Boot.BootUpdateRunner in the loaded assemblies.");

            var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("[BootLoader] Boot.BootUpdateRunner.Start(BootBridge) was not found.");

            method.Invoke(null, new object[] { bridge });
        }
    }
}
