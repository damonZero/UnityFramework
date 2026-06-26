using System.Collections.Generic;
using UnityEngine;

namespace KJ.Core
{
    /// <summary>
    /// 资源管理器，封装 Resources.Load。
    /// 后期可替换为 Addressables / AssetBundle，接口不变。
    /// </summary>
    public class ResourceManager : IModule
    {
        public int Priority => 100;

        // path → loaded asset (缓存)
        private readonly Dictionary<string, AssetHandle> _cache = new Dictionary<string, AssetHandle>();

        public void Init()
        {
            Debug.Log("[ResourceManager] Init");
        }

        public void Shutdown()
        {
            foreach (var kvp in _cache)
            {
                kvp.Value.Release();
            }

            _cache.Clear();
            Debug.Log("[ResourceManager] Shutdown");
        }

        /// <summary>
        /// 同步加载资源。
        /// </summary>
        /// <param name="path">Resources 下的相对路径（不含扩展名）</param>
        public AssetHandle Load(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ResourceManager] Load: path is null or empty");
                return null;
            }

            // 命中缓存
            if (_cache.TryGetValue(path, out var cached) && cached.IsValid)
            {
                return cached;
            }

            var asset = Resources.Load(path);
            var handle = new AssetHandle(path, asset);

            if (!handle.IsValid)
            {
                Debug.LogWarning($"[ResourceManager] Load failed: {path}");
            }

            _cache[path] = handle;
            return handle;
        }

        /// <summary>
        /// 同步加载资源（类型化）。
        /// </summary>
        public AssetHandle Load<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ResourceManager] Load<T>: path is null or empty");
                return null;
            }

            if (_cache.TryGetValue(path, out var cached) && cached.IsValid)
            {
                return cached;
            }

            var asset = Resources.Load<T>(path);
            var handle = new AssetHandle(path, asset);

            if (!handle.IsValid)
            {
                Debug.LogWarning($"[ResourceManager] Load<T> failed: {path}");
            }

            _cache[path] = handle;
            return handle;
        }

        /// <summary>
        /// 释放资源句柄。
        /// </summary>
        public void Unload(AssetHandle handle)
        {
            if (handle == null) return;

            if (_cache.ContainsKey(handle.Path))
            {
                _cache.Remove(handle.Path);
            }

            handle.Release();
        }

        /// <summary>
        /// 释放所有缓存资源。
        /// </summary>
        public void UnloadAll()
        {
            foreach (var kvp in _cache)
            {
                kvp.Value.Release();
            }

            _cache.Clear();
        }
    }
}
