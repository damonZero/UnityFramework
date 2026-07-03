using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.Asset;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Framework.TestKit.Fakes
{
    /// <summary>
    /// In-memory IAssetSystem fake. Async methods complete synchronously and do not simulate load timing.
    /// </summary>
    public sealed class RecordingAssetSystem : IAssetSystem
    {
        private readonly Dictionary<(string path, Type type), Object> _assets = new();
        private readonly List<string> _loadedPaths = new();
        private readonly List<string> _releasedPaths = new();

        public IReadOnlyList<string> LoadedPaths => _loadedPaths;
        public IReadOnlyList<string> ReleasedPaths => _releasedPaths;

        public void RegisterAsset<T>(string path, T asset) where T : Object
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var key = (path, typeof(T));
            if (_assets.ContainsKey(key))
                throw new InvalidOperationException($"Asset already registered. Path: {path}, Type: {typeof(T).FullName}");

            _assets.Add(key, asset);
        }

        public UniTask<T> LoadAssetAsync<T>(string path) where T : Object
        {
            var asset = FindAsset<T>(path);
            _loadedPaths.Add(path);
            return UniTask.FromResult(asset);
        }

        public UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path) where T : Object
        {
            throw new NotSupportedException("RecordingAssetSystem does not create backend asset handles.");
        }

        public UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent = null)
        {
            throw new NotSupportedException("RecordingAssetSystem does not create backend instance handles.");
        }

        public UniTask<AssetSceneHandle> LoadSceneAsync(string path, LoadSceneMode mode = LoadSceneMode.Single, Action<float> onProgress = null)
        {
            throw new NotSupportedException("RecordingAssetSystem does not load Unity scenes.");
        }

        public AssetDownloadHandle CreateDownloader(string tag = null)
        {
            throw new NotSupportedException("RecordingAssetSystem does not create download handles.");
        }

        public AssetDownloadHandle CreateDownloader(string[] tags)
        {
            throw new NotSupportedException("RecordingAssetSystem does not create download handles.");
        }

        public void Release<T>(string path) where T : Object
        {
            _releasedPaths.Add(path);
        }

        public void Release(string path)
        {
            _releasedPaths.Add(path);
        }

        public void UnloadUnused()
        {
        }

        public void ClearRecords()
        {
            _loadedPaths.Clear();
            _releasedPaths.Clear();
        }

        private T FindAsset<T>(string path) where T : Object
        {
            if (_assets.TryGetValue((path, typeof(T)), out var asset))
                return asset as T;

            Type foundType = null;
            foreach (var kv in _assets)
            {
                if (kv.Key.path == path && kv.Value is T typed)
                    return typed;

                if (kv.Key.path == path)
                    foundType = kv.Key.type;
            }

            if (foundType != null)
                throw new InvalidCastException($"Asset path found but type mismatch. Path: {path}, Registered: {foundType.FullName}, Requested: {typeof(T).FullName}");

            return null;
        }
    }
}
