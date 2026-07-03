using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Framework.Cache;

namespace Framework.Pool
{
    public sealed class GameObjectPool
    {
        private readonly Transform _root;
        private readonly PoolContainerMode _mode;
        private readonly Vector3 _farAway;
        private readonly Dictionary<string, Stack<GameObject>> _idle = new();
        private readonly Dictionary<string, HashSet<GameObject>> _instancesByPath = new();
        private readonly Dictionary<string, int> _idleCountByPath = new();
        private readonly Dictionary<string, int> _activeCountByPath = new();
        private readonly HashSet<string> _cachedPrefabPaths = new();
        private readonly Cache<string, GameObject> _prefabCache;

        public GameObjectPool(Transform root, int prefabCapacity = 64, PoolContainerMode mode = PoolContainerMode.ChangeParent, Vector3? farAway = null)
        {
            _root = root;
            _mode = mode;
            _farAway = farAway ?? new Vector3(9999f, 9999f, 9999f);
            _prefabCache = new Cache<string, GameObject>(prefabCapacity, new LruCachePolicy<string>(), OnPrefabEvicted);
        }

        public async UniTask<GameObject> GetAsync(string prefabPath, Transform parent = null)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                throw new ArgumentNullException(nameof(prefabPath));
            }

            if (_idle.TryGetValue(prefabPath, out var stack) && stack.Count > 0)
            {
                while (stack.Count > 0)
                {
                    var inst = stack.Pop();
                    DecrementCount(_idleCountByPath, prefabPath);
                    if (inst == null)
                    {
                        RemoveInstanceFromRegistry(prefabPath, inst);
                        continue;
                    }

                    var tag = inst.GetComponent<PoolInstanceTag>();
                    if (tag == null || !tag.IsRecycled || tag.PrefabPath != prefabPath)
                    {
                        RemoveInstanceFromRegistry(prefabPath, inst);
                        Object.Destroy(inst);
                        continue;
                    }

                    ActivateInstance(inst, parent);
                    tag.IsRecycled = false;
                    IncrementCount(_activeCountByPath, prefabPath);
                    return inst;
                }
            }

            if (!_prefabCache.TryGet(prefabPath, out var prefab))
            {
                var gate = PoolDependencies.LoadGates.GetOrAdd(prefabPath, static _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync(CancellationToken.None);
                try
                {
                    if (!_prefabCache.TryGet(prefabPath, out prefab))
                    {
                        prefab = await LoadPrefabAsync(prefabPath);
                        if (prefab == null)
                        {
                            return null;
                        }

                        _prefabCache.Put(prefabPath, prefab);
                        _cachedPrefabPaths.Add(prefabPath);
                    }
                }
                finally
                {
                    gate.Release();
                }
            }

            if (prefab == null)
            {
                return null;
            }

            var instance = Object.Instantiate(prefab);
            RegisterInstance(prefabPath, instance, parent);
            return instance;
        }

        public void Recycle(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var tag = instance.GetComponent<PoolInstanceTag>();
            if (tag == null || string.IsNullOrEmpty(tag.PrefabPath))
            {
                Object.Destroy(instance);
                return;
            }

            if (tag.IsRecycled)
            {
                return;
            }

            var prefabPath = tag.PrefabPath;
            if (!_idle.TryGetValue(prefabPath, out var stack))
            {
                stack = new Stack<GameObject>();
                _idle[prefabPath] = stack;
            }

            RemoveInstanceFromRegistry(prefabPath, instance);
            DeactivateInstance(instance);
            tag.IsRecycled = true;
            stack.Push(instance);
            AddInstanceToRegistry(prefabPath, instance);
            DecrementCount(_activeCountByPath, prefabPath);
            IncrementCount(_idleCountByPath, prefabPath);
        }

        public void Warmup(string prefabPath, int count, Transform parent = null)
        {
            if (count <= 0)
            {
                return;
            }

            WarmupInternal(prefabPath, count, parent).Forget();
        }

        public void Clear()
        {
            using var pooledInstances = CollectionPool.RentList<GameObject>();
            var instanceSnapshot = pooledInstances.Value;
            foreach (var pathSet in _instancesByPath.Values)
            {
                foreach (var inst in pathSet)
                {
                    if (inst != null)
                    {
                        instanceSnapshot.Add(inst);
                    }
                }
            }

            foreach (var inst in instanceSnapshot)
            {
                Object.Destroy(inst);
            }

            using var pooledPrefabPaths = CollectionPool.RentList<string>();
            var prefabPaths = pooledPrefabPaths.Value;
            foreach (var prefabPath in _cachedPrefabPaths)
            {
                prefabPaths.Add(prefabPath);
            }

            foreach (var prefabPath in prefabPaths)
            {
                PoolDependencies.ReleaseAssetByPath?.Invoke(prefabPath);
                PoolDependencies.LoadGates.TryRemove(prefabPath, out _);
            }

            _idle.Clear();
            _instancesByPath.Clear();
            _idleCountByPath.Clear();
            _activeCountByPath.Clear();
            _cachedPrefabPaths.Clear();
            _prefabCache.Clear();
        }

        public int GetIdleCount(string prefabPath)
        {
            return _idleCountByPath.TryGetValue(prefabPath, out var count) ? count : 0;
        }

        public int GetActiveCount(string prefabPath)
        {
            return _activeCountByPath.TryGetValue(prefabPath, out var count) ? count : 0;
        }

        private async UniTask WarmupInternal(string prefabPath, int count, Transform parent)
        {
            for (var i = 0; i < count; i++)
            {
                var inst = await GetAsync(prefabPath, parent);
                if (inst == null)
                {
                    return;
                }

                Recycle(inst);
            }
        }

        private async UniTask<GameObject> LoadPrefabAsync(string prefabPath)
        {
            if (PoolDependencies.LoadAssetAsync == null)
            {
                throw new InvalidOperationException("PoolDependencies.LoadAssetAsync is not configured.");
            }

            return await PoolDependencies.LoadAssetAsync(prefabPath, null);
        }

        private void RegisterInstance(string prefabPath, GameObject instance, Transform parent)
        {
            var tag = instance.GetComponent<PoolInstanceTag>() ?? instance.AddComponent<PoolInstanceTag>();
            tag.PrefabPath = prefabPath;
            tag.IsRecycled = false;
            AddInstanceToRegistry(prefabPath, instance);
            IncrementCount(_activeCountByPath, prefabPath);
            ActivateInstance(instance, parent);
        }

        private void ActivateInstance(GameObject instance, Transform parent)
        {
            var transform = instance.transform;
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }
            else if (_root != null)
            {
                transform.SetParent(_root, false);
            }

            if (_mode == PoolContainerMode.MovePos)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            instance.SetActive(true);
        }

        private void DeactivateInstance(GameObject instance)
        {
            instance.SetActive(false);
            var transform = instance.transform;
            if (_mode == PoolContainerMode.ChangeParent)
            {
                transform.SetParent(_root, false);
            }
            else
            {
                transform.position = _farAway;
            }
        }

        private void OnPrefabEvicted(string prefabPath, GameObject prefab)
        {
            _cachedPrefabPaths.Remove(prefabPath);
            if (PoolDependencies.ReleaseAssetByPath != null)
            {
                PoolDependencies.ReleaseAssetByPath(prefabPath);
            }
        }

        private void AddInstanceToRegistry(string prefabPath, GameObject instance)
        {
            if (!_instancesByPath.TryGetValue(prefabPath, out var instances))
            {
                instances = new HashSet<GameObject>();
                _instancesByPath[prefabPath] = instances;
            }

            instances.Add(instance);
        }

        private void RemoveInstanceFromRegistry(string prefabPath, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!_instancesByPath.TryGetValue(prefabPath, out var instances))
            {
                return;
            }

            instances.Remove(instance);
            if (instances.Count == 0)
            {
                _instancesByPath.Remove(prefabPath);
            }
        }

        private static void IncrementCount(Dictionary<string, int> counts, string key)
        {
            counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        private static void DecrementCount(Dictionary<string, int> counts, string key)
        {
            if (!counts.TryGetValue(key, out var count))
            {
                return;
            }

            count--;
            if (count <= 0)
            {
                counts.Remove(key);
            }
            else
            {
                counts[key] = count;
            }
        }
    }
}
