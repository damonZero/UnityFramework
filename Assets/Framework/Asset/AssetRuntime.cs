using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Framework.Log;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework.Asset
{
    public sealed class AssetRuntime : IAssetRuntime, IAssetSystem
    {
        private AssetConfig _config;
        private ResourcePackage _defaultPackage;
        private readonly Dictionary<AssetCacheKey, YooAsset.AssetHandle> _assetHandles = new();
        private readonly Dictionary<string, YooAsset.SceneHandle> _sceneHandles = new();
        private readonly Dictionary<string, UniTask> _sceneUnloadTasks = new();
        private readonly HashSet<YooAsset.AssetHandle> _ownedAssetHandles = new();
        private readonly HashSet<YooAsset.SceneHandle> _ownedSceneHandles = new();
        private readonly List<AssetCacheKey> _releaseKeys = new();
        private readonly Dictionary<AssetCacheKey, PendingAssetLoad> _loadingTasks = new();
        private readonly object _gate = new();
        private int _lifecycleVersion;
        private int _downloadMaxConcurrency = 10;
        private int _failedRetryCount = 3;
        private AssetInitializeHandle _initializeHandle;

        public AssetConfig Config => _config;
        public bool IsReady { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public AssetInitializeHandle BeginInitialize(AssetConfig config)
        {
            if (IsReady)
                return AssetInitializeHandle.Succeeded();

            if (_initializeHandle != null)
            {
                if (!_initializeHandle.IsDone)
                    return _initializeHandle;

                _initializeHandle = null;
            }

            if (IsReady)
                return AssetInitializeHandle.Succeeded();

            try
            {
                PrepareInitialize(config);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                GameLog.Error($"[AssetRuntime] Initialization failed: {e}");
                CleanupAfterInitializeFailure();
                return AssetInitializeHandle.Failed(LastError);
            }

            var handle = AssetInitializeHandle.Pending();
            _initializeHandle = handle;
            RunInitializeAsync(handle).Forget();
            return handle;
        }

        public bool Initialize(AssetConfig config)
        {
            if (IsReady)
                return true;

            LastError = "Synchronous AssetRuntime.Initialize is not supported by YooAsset package initialization. Use BeginInitialize and poll the returned handle.";
            GameLog.Error($"[AssetRuntime] {LastError}");
            return false;
        }

        public void Shutdown()
        {
            if (_defaultPackage == null && !IsReady)
                return;

            IsReady = false;
            _initializeHandle = null;
            _config = null;

            List<YooAsset.AssetHandle> handlesToRelease = new();
            List<PendingAssetLoad> pendingLoads = new();
            lock (_gate)
            {
                _lifecycleVersion++;
                foreach (var kv in _assetHandles)
                {
                    handlesToRelease.Add(kv.Value);
                }
                foreach (var kv in _loadingTasks)
                {
                    pendingLoads.Add(kv.Value);
                }
                _assetHandles.Clear();
                _loadingTasks.Clear();
            }

            foreach (var pending in pendingLoads)
            {
                pending.Cancel();
            }

            foreach (var handle in handlesToRelease)
            {
                try { handle.Release(); } catch (Exception e) { GameLog.Error($"[AssetRuntime] Error releasing cached handle: {e}"); }
            }

            foreach (var handle in _ownedAssetHandles)
            {
                try { handle.Release(); } catch (Exception e) { GameLog.Error($"[AssetRuntime] Error releasing owned asset handle: {e}"); }
            }

            foreach (var handle in _ownedSceneHandles)
            {
                try { UnloadSceneSynchronously(handle); } catch (Exception e) { GameLog.Error($"[AssetRuntime] Error unloading owned scene handle: {e}"); }
            }

            _sceneHandles.Clear();
            _sceneUnloadTasks.Clear();
            _ownedAssetHandles.Clear();
            _ownedSceneHandles.Clear();
            _defaultPackage = null;
            YooAssets.Destroy();
        }

        public void WrapFromExistingPackage(AssetConfig config, ResourcePackage existingPackage)
        {
            if (existingPackage == null)
                throw new ArgumentNullException(nameof(existingPackage));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (IsReady)
                throw new InvalidOperationException("AssetRuntime is already initialized.");

            LastError = string.Empty;
            _config = config;
            _defaultPackage = existingPackage;
            _downloadMaxConcurrency = Math.Max(1, config.DownloadMaxConcurrency);
            _failedRetryCount = Math.Max(0, config.FailedRetryCount);

            lock (_gate)
            {
                _lifecycleVersion++;
            }

            IsReady = true;
        }

        /// <summary>
        /// Synchronous setup before the async initialize chain: tear down any prior
        /// state, cache config, create the YooAsset package. Does NOT start loading.
        /// </summary>
        private void PrepareInitialize(AssetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "AssetConfig is missing. Create Assets/Resources/AssetConfig.asset before starting the asset runtime.");

            if (_defaultPackage != null || IsReady)
                Shutdown();
            else if (YooAssets.IsInitialized)
                YooAssets.Destroy();

            LastError = string.Empty;
            IsReady = false;
            _config = config;
            _downloadMaxConcurrency = Math.Max(1, config.DownloadMaxConcurrency);
            _failedRetryCount = Math.Max(0, config.FailedRetryCount);

            lock (_gate)
            {
                _lifecycleVersion++;
            }

            YooAssets.Initialize();
            _defaultPackage = YooAssets.CreatePackage(GetPackageName(config));
        }

        /// <summary>
        /// Full YooAsset initialize chain: InitializePackage (file system) →
        /// RequestPackageVersion → LoadPackageManifest (activates ActiveManifest).
        /// The manifest step is mandatory; without it LoadAsset* throws
        /// "Active package manifest not found".
        /// </summary>
        private async UniTaskVoid RunInitializeAsync(AssetInitializeHandle handle)
        {
            try
            {
                var initOp = _defaultPackage.InitializePackageAsync(BuildOptions(_config));
                await initOp.ToUniTask();
                if (initOp.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(initOp.Error) ? "YooAsset package initialization failed." : initOp.Error);

                var versionOp = _defaultPackage.RequestPackageVersionAsync();
                await versionOp.ToUniTask();
                if (versionOp.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(versionOp.Error) ? "YooAsset request package version failed." : versionOp.Error);

                var timeout = _config == null ? 60 : Math.Max(1, _config.DownloadTimeout);
                var manifestOp = _defaultPackage.LoadPackageManifestAsync(
                    new LoadPackageManifestOptions(versionOp.PackageVersion, timeout));
                await manifestOp.ToUniTask();
                if (manifestOp.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(manifestOp.Error) ? "YooAsset load package manifest failed." : manifestOp.Error);

                IsReady = true;
                LastError = string.Empty;
                _initializeHandle = null;
                handle.Complete(true, null);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                GameLog.Error($"[AssetRuntime] Initialization failed: {e}");
                CleanupAfterInitializeFailure();
                handle.Complete(false, LastError);
            }
        }

        private void CleanupAfterInitializeFailure()
        {
            var error = LastError;

            try
            {
                if (_defaultPackage != null || IsReady)
                    Shutdown();
                else if (YooAssets.IsInitialized)
                    YooAssets.Destroy();
            }
            catch (Exception e)
            {
                GameLog.Error($"[AssetRuntime] Cleanup after initialization failure failed: {e}");
            }
            finally
            {
                _initializeHandle = null;
                _config = null;
                _defaultPackage = null;
                IsReady = false;
                LastError = error;
            }
        }

        public async UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            EnsureReady();

            var handle = _defaultPackage.LoadAssetAsync<T>(path);
            await handle.ToUniTask();

            // Guard against Shutdown() arriving while the load was in-flight.
            // If IsReady is false, _ownedAssetHandles has already been cleared,
            // so adding the handle here would create a permanent leak.
            if (!IsReady)
            {
                GameLog.Warn($"[AssetRuntime] LoadAssetHandleAsync: runtime shut down during load of '{path}'. Releasing handle.");
                handle.Release();
                return null;
            }

            if (handle.Status != EOperationStatus.Succeeded)
            {
                GameLog.Error($"[AssetRuntime] Load failed: {path} - {handle.Error}");
                handle.Release();
                return null;
            }

            _ownedAssetHandles.Add(handle);
            return new AssetHandle<T>(handle, h => _ownedAssetHandles.Remove(h));
        }

        public async UniTask<T> LoadAssetAsync<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            EnsureReady();

            var key = AssetCacheKey.Create<T>(path);
            PendingAssetLoad pendingLoad;
            bool shouldStartLoad = false;

            lock (_gate)
            {
                EnsureReady();

                if (_assetHandles.TryGetValue(key, out var cached) && cached.AssetObject is T asset)
                    return asset;

                if (!_loadingTasks.TryGetValue(key, out pendingLoad))
                {
                    pendingLoad = new PendingAssetLoad(_lifecycleVersion, _defaultPackage);
                    _loadingTasks[key] = pendingLoad;
                    shouldStartLoad = true;
                }
            }

            if (shouldStartLoad)
            {
                LoadAssetInternalAsync<T>(key, path, pendingLoad).Forget();
            }

            var loadedObj = await pendingLoad.Task;
            return loadedObj as T;
        }

        private async UniTaskVoid LoadAssetInternalAsync<T>(AssetCacheKey key, string path, PendingAssetLoad pendingLoad) where T : Object
        {
            YooAsset.AssetHandle handle = null;
            try
            {
                handle = pendingLoad.Package.LoadAssetAsync<T>(path);
                await handle.ToUniTask();
                if (handle.Status != EOperationStatus.Succeeded)
                {
                    GameLog.Error($"[AssetRuntime] Load failed: {path} - {handle.Error}");
                    handle.Release();
                    pendingLoad.TrySetResult(null);
                    return;
                }

                var shouldCache = false;
                lock (_gate)
                {
                    shouldCache = IsReady &&
                        pendingLoad.LifecycleVersion == _lifecycleVersion &&
                        ReferenceEquals(pendingLoad.Package, _defaultPackage) &&
                        !pendingLoad.ReleaseRequested;

                    if (shouldCache)
                    {
                        _assetHandles[key] = handle;
                    }
                }

                var loadedObject = shouldCache ? handle.AssetObject : null;
                if (!shouldCache)
                {
                    handle.Release();
                }
                pendingLoad.TrySetResult(loadedObject);
            }
            catch (Exception e)
            {
                GameLog.Error($"[AssetRuntime] Load exception: {path} - {e}");
                handle?.Release();
                pendingLoad.TrySetResult(null);
            }
            finally
            {
                lock (_gate)
                {
                    if (_loadingTasks.TryGetValue(key, out var current) && ReferenceEquals(current, pendingLoad))
                    {
                        _loadingTasks.Remove(key);
                    }
                }
            }
        }

        public async UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent = null)
        {
            var handle = await LoadAssetHandleAsync<GameObject>(path);
            if (handle == null || handle.Asset == null)
                return null;

            var instance = handle.Instantiate(parent);
            return new AssetInstanceHandle(instance, handle);
        }

        public async UniTask<AssetSceneHandle> LoadSceneAsync(string path, LoadSceneMode mode = LoadSceneMode.Single, Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            EnsureReady();

            if (_sceneUnloadTasks.TryGetValue(path, out var pendingUnload))
            {
                await pendingUnload;
                _sceneUnloadTasks.Remove(path);
            }

            if (_sceneHandles.TryGetValue(path, out var oldHandle))
                await StartSceneUnloadAsync(path, oldHandle);

            var handle = _defaultPackage.LoadSceneAsync(path, mode);
            _sceneHandles[path] = handle;

            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.Progress);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            onProgress?.Invoke(1f);
            if (handle.Status != EOperationStatus.Succeeded)
            {
                GameLog.Error($"[AssetRuntime] Scene load failed: {path} - {handle.Error}");
                _sceneHandles.Remove(path);
                await handle.UnloadSceneAsync().ToUniTask();
                return null;
            }

            _ownedSceneHandles.Add(handle);
            return new AssetSceneHandle(handle, h =>
            {
                _ownedSceneHandles.Remove(h);
                if (_sceneHandles.TryGetValue(path, out var current) && ReferenceEquals(current, h))
                    _sceneHandles.Remove(path);
            }, h => StartSceneUnloadAsync(path, h));
        }

        public AssetDownloadHandle CreateDownloader(string tag = null)
        {
            EnsureReady();
            var options = tag == null
                ? new ResourceDownloaderOptions(_downloadMaxConcurrency, _failedRetryCount)
                : new ResourceDownloaderOptions(tag, _downloadMaxConcurrency, _failedRetryCount);
            return new AssetDownloadHandle(_defaultPackage.CreateResourceDownloader(options));
        }

        public AssetDownloadHandle CreateDownloader(string[] tags)
        {
            EnsureReady();
            var options = tags == null || tags.Length == 0
                ? new ResourceDownloaderOptions(_downloadMaxConcurrency, _failedRetryCount)
                : new ResourceDownloaderOptions(tags, _downloadMaxConcurrency, _failedRetryCount);
            return new AssetDownloadHandle(_defaultPackage.CreateResourceDownloader(options));
        }

        public AssetUpdateManifestHandle UpdateManifest()
        {
            EnsureReady();
            var timeout = _config == null ? 60 : Math.Max(1, _config.DownloadTimeout);
            var options = new RequestPackageVersionOptions(true, timeout);
            return new AssetUpdateManifestHandle(_defaultPackage, _defaultPackage.RequestPackageVersionAsync(options), timeout);
        }

        public byte[] LoadRawBytes(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            EnsureReady();

            var handle = _defaultPackage.LoadAssetSync<RawFileObject>(path);
            try
            {
                if (handle.Status != EOperationStatus.Succeeded)
                {
                    GameLog.Error($"[AssetRuntime] Raw file load failed: {path} - {handle.Error}");
                    return Array.Empty<byte>();
                }

                var rawFile = handle.GetAssetObject<RawFileObject>();
                return rawFile?.GetBytes() ?? Array.Empty<byte>();
            }
            finally
            {
                handle.Release();
            }
        }

        public void Release<T>(string path) where T : Object
        {
            var key = AssetCacheKey.Create<T>(path);
            YooAsset.AssetHandle handle = null;
            lock (_gate)
            {
                if (_loadingTasks.TryGetValue(key, out var pendingLoad))
                {
                    pendingLoad.Cancel();
                    _loadingTasks.Remove(key);
                }

                if (_assetHandles.TryGetValue(key, out handle))
                {
                    _assetHandles.Remove(key);
                }
            }

            handle?.Release();
        }

        public void Release(string path)
        {
            List<YooAsset.AssetHandle> handlesToRelease = new();
            lock (_gate)
            {
                _releaseKeys.Clear();
                foreach (var key in _assetHandles.Keys)
                {
                    if (key.Path == path)
                        _releaseKeys.Add(key);
                }

                foreach (var key in _releaseKeys)
                {
                    if (_assetHandles.TryGetValue(key, out var handle))
                    {
                        handlesToRelease.Add(handle);
                        _assetHandles.Remove(key);
                    }
                }
                _releaseKeys.Clear();

                foreach (var kv in _loadingTasks)
                {
                    if (kv.Key.Path == path)
                    {
                        _releaseKeys.Add(kv.Key);
                    }
                }
                foreach (var key in _releaseKeys)
                {
                    if (_loadingTasks.TryGetValue(key, out var pendingLoad))
                    {
                        pendingLoad.Cancel();
                        _loadingTasks.Remove(key);
                    }
                }
                _releaseKeys.Clear();
            }

            foreach (var handle in handlesToRelease)
            {
                handle.Release();
            }

            if (_sceneHandles.TryGetValue(path, out var sceneHandle))
            {
                StartSceneUnloadAsync(path, sceneHandle).Forget();
                _sceneHandles.Remove(path);
                _ownedSceneHandles.Remove(sceneHandle);
            }
        }

        public void UnloadUnused()
        {
            EnsureReady();
            _defaultPackage.UnloadUnusedAssetsAsync().WaitForCompletion();
        }

        private void EnsureReady()
        {
            if (!IsReady || _defaultPackage == null)
                throw new InvalidOperationException("AssetRuntime is not initialized.");
        }

        private static string GetPackageName(AssetConfig config)
        {
            return string.IsNullOrWhiteSpace(config?.PackageName) ? "DefaultPackage" : config.PackageName;
        }

        private InitializePackageOptions BuildOptions(AssetConfig config)
        {
            return config.Mode switch
            {
                AssetConfig.PlayMode.EditorSimulate => new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(GetEditorSimulatePackageRoot(config))
                },
                AssetConfig.PlayMode.Offline => new OfflinePlayModeOptions
                {
                    BuiltinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                },
                AssetConfig.PlayMode.Host => new HostPlayModeOptions
                {
                    BuiltinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                    CacheFileSystemParameters = BuildSandboxParameters(config)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(config.Mode), config.Mode, null)
            };
        }

        private static string GetEditorSimulatePackageRoot(AssetConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.EditorSimulatePackageRoot))
                return config.EditorSimulatePackageRoot;

            throw new InvalidOperationException(
                "AssetConfig.EditorSimulatePackageRoot is empty. Run KJ/HybridCLR/Prepare YooAsset Editor Simulate Package or KJ/HybridCLR/Prepare Runtime Assets And Boot before entering Play Mode.");
        }

        private FileSystemParameters BuildSandboxParameters(AssetConfig config)
        {
            var cdnBaseUrl = string.IsNullOrWhiteSpace(config.CdnBaseUrl)
                ? "http://127.0.0.1:8080/CDN"
                : config.CdnBaseUrl;
            var parameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
                new CdnRemoteService(cdnBaseUrl));
            parameters.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, config.DownloadMaxConcurrency);
            parameters.AddParameter(EFileSystemParameter.DownloadWatchdogTimeout, config.DownloadTimeout);
            return parameters;
        }

        private async UniTask StartSceneUnloadAsync(string path, YooAsset.SceneHandle handle)
        {
            if (handle == null)
                return;

            if (_sceneUnloadTasks.TryGetValue(path, out var pendingUnload))
                await pendingUnload;

            var task = UnloadSceneInternalAsync(path, handle);
            _sceneUnloadTasks[path] = task;
            await task;
        }

        private async UniTask UnloadSceneInternalAsync(string path, YooAsset.SceneHandle handle)
        {
            try
            {
                await handle.UnloadSceneAsync().ToUniTask();
            }
            finally
            {
                if (_sceneHandles.TryGetValue(path, out var current) && ReferenceEquals(current, handle))
                    _sceneHandles.Remove(path);
                _ownedSceneHandles.Remove(handle);
                _sceneUnloadTasks.Remove(path);
            }
        }

        private static void UnloadSceneSynchronously(YooAsset.SceneHandle handle)
        {
            if (handle == null)
                return;

            var operation = handle.UnloadSceneAsync();
            operation.WaitForCompletion();
        }

        private readonly struct AssetCacheKey : IEquatable<AssetCacheKey>
        {
            public readonly string Path;
            private readonly Type _type;

            private AssetCacheKey(string path, Type type)
            {
                Path = path;
                _type = type;
            }

            public static AssetCacheKey Create<T>(string path) where T : Object => new(path, typeof(T));
            public bool Equals(AssetCacheKey other) => Path == other.Path && _type == other._type;
            public override bool Equals(object obj) => obj is AssetCacheKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Path, _type);
        }

        private sealed class PendingAssetLoad
        {
            private readonly TaskCompletionSource<Object> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public PendingAssetLoad(int lifecycleVersion, ResourcePackage package)
            {
                LifecycleVersion = lifecycleVersion;
                Package = package;
            }

            public int LifecycleVersion { get; }
            public ResourcePackage Package { get; }
            public bool ReleaseRequested { get; set; }
            public Task<Object> Task => _completion.Task;
            public void TrySetResult(Object asset) => _completion.TrySetResult(asset);
            public void Cancel()
            {
                ReleaseRequested = true;
                TrySetResult(null);
            }
        }

        private sealed class CdnRemoteService : IRemoteService
        {
            private readonly string _baseUrl;
            public CdnRemoteService(string baseUrl) => _baseUrl = baseUrl.TrimEnd('/');
            public IReadOnlyList<string> GetRemoteUrls(string fileName) => new[] { $"{_baseUrl}/{fileName}" };
        }
    }
}
