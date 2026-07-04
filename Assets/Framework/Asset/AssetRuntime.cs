using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.Log;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework.Asset
{
    public sealed class AssetRuntime : IAssetRuntime
    {
        private AssetConfig _config;
        private ResourcePackage _defaultPackage;
        private readonly Dictionary<AssetCacheKey, YooAsset.AssetHandle> _assetHandles = new();
        private readonly Dictionary<string, YooAsset.SceneHandle> _sceneHandles = new();
        private readonly Dictionary<string, UniTask> _sceneUnloadTasks = new();
        private readonly HashSet<YooAsset.AssetHandle> _ownedAssetHandles = new();
        private readonly HashSet<YooAsset.SceneHandle> _ownedSceneHandles = new();
        private readonly List<AssetCacheKey> _releaseKeys = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private int _downloadMaxConcurrency = 10;
        private int _failedRetryCount = 3;

        public AssetConfig Config => _config;
        public bool IsReady { get; private set; }
        public ResourcePackage DefaultPackage => _defaultPackage;

        public bool Initialize(AssetConfig config)
        {
            if (IsReady)
                return true;

            if (_defaultPackage != null)
                Shutdown();

            var yooAssetsInitialized = false;
            try
            {
                _config = config;
                _downloadMaxConcurrency = config == null ? 10 : Math.Max(1, config.DownloadMaxConcurrency);
                _failedRetryCount = config == null ? 3 : Math.Max(0, config.FailedRetryCount);

                YooAssets.Initialize();
                yooAssetsInitialized = true;
                var packageName = GetPackageName(config);
                _defaultPackage = YooAssets.CreatePackage(packageName);

                InitializePackageOptions options = config == null
                    ? new EditorSimulateModeOptions
                    {
                        EditorFileSystemParameters =
                            FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageName)
                    }
                    : BuildOptions(config);

                var initOp = _defaultPackage.InitializePackageAsync(options);
                initOp.WaitForCompletion();

                if (initOp.Status == EOperationStatus.Succeeded)
                {
                    IsReady = true;
                    return true;
                }

                GameLog.Error($"[AssetRuntime] Initialization failed: {initOp.Error}");
            }
            catch (Exception e)
            {
                GameLog.Error($"[AssetRuntime] Initialization failed: {e}");
            }

            if (_defaultPackage != null || IsReady)
                Shutdown();
            else if (yooAssetsInitialized)
                YooAssets.Destroy();

            _config = null;
            _defaultPackage = null;
            IsReady = false;
            return false;
        }

        public void Shutdown()
        {
            if (_defaultPackage == null && !IsReady)
                return;

            IsReady = false;
            _config = null;

            foreach (var kv in _assetHandles)
            {
                try { kv.Value.Release(); } catch (Exception e) { GameLog.Error($"[AssetRuntime] Error releasing {kv.Key}: {e}"); }
            }

            foreach (var handle in _ownedAssetHandles)
            {
                try { handle.Release(); } catch (Exception e) { GameLog.Error($"[AssetRuntime] Error releasing owned asset handle: {e}"); }
            }

            foreach (var handle in _ownedSceneHandles)
            {
                try { UnloadSceneSynchronously(handle); } catch (Exception e) { GameLog.Error($"[AssetRuntime] Error unloading owned scene handle: {e}"); }
            }

            _assetHandles.Clear();
            _sceneHandles.Clear();
            _sceneUnloadTasks.Clear();
            _ownedAssetHandles.Clear();
            _ownedSceneHandles.Clear();
            _defaultPackage = null;
            YooAssets.Destroy();
        }

        public async UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            EnsureReady();

            var handle = _defaultPackage.LoadAssetAsync<T>(path);
            await handle.ToUniTask();
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
            if (_assetHandles.TryGetValue(key, out var cached) && cached.AssetObject is T asset)
                return asset;

            await _gate.WaitAsync();
            try
            {
                if (_assetHandles.TryGetValue(key, out cached) && cached.AssetObject is T cachedAsset)
                    return cachedAsset;

                var handle = _defaultPackage.LoadAssetAsync<T>(path);
                await handle.ToUniTask();
                if (handle.Status != EOperationStatus.Succeeded)
                {
                    GameLog.Error($"[AssetRuntime] Load failed: {path} - {handle.Error}");
                    handle.Release();
                    return null;
                }

                _assetHandles[key] = handle;
                return handle.AssetObject as T;
            }
            finally
            {
                _gate.Release();
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
                handle.UnloadSceneAsync();
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

        public void Release<T>(string path) where T : Object
        {
            var key = AssetCacheKey.Create<T>(path);
            if (!_assetHandles.TryGetValue(key, out var handle))
                return;

            handle.Release();
            _assetHandles.Remove(key);
        }

        public void Release(string path)
        {
            _releaseKeys.Clear();
            foreach (var key in _assetHandles.Keys)
            {
                if (key.Path == path)
                    _releaseKeys.Add(key);
            }

            foreach (var key in _releaseKeys)
            {
                _assetHandles[key].Release();
                _assetHandles.Remove(key);
            }
            _releaseKeys.Clear();

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
            var packageName = GetPackageName(config);
            return config.Mode switch
            {
                AssetConfig.PlayMode.EditorSimulate => new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageName)
                },
                AssetConfig.PlayMode.Offline => new OfflinePlayModeOptions
                {
                    BuiltinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(packageName)
                },
                AssetConfig.PlayMode.Host => new HostPlayModeOptions
                {
                    BuiltinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(packageName),
                    CacheFileSystemParameters = BuildSandboxParameters(config)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(config.Mode), config.Mode, null)
            };
        }

        private FileSystemParameters BuildSandboxParameters(AssetConfig config)
        {
            var packageName = GetPackageName(config);
            var cdnBaseUrl = string.IsNullOrWhiteSpace(config.CdnBaseUrl)
                ? "http://127.0.0.1:8080/CDN"
                : config.CdnBaseUrl;
            var parameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
                new CdnRemoteService(cdnBaseUrl), packageName);
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

        private sealed class CdnRemoteService : IRemoteService
        {
            private readonly string _baseUrl;
            public CdnRemoteService(string baseUrl) => _baseUrl = baseUrl.TrimEnd('/');
            public IReadOnlyList<string> GetRemoteUrls(string fileName) => new[] { $"{_baseUrl}/{fileName}" };
        }
    }
}
