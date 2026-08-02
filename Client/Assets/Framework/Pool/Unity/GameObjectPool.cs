using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Framework.Cache;

namespace Framework.Pool
{
    /// <summary>
    /// 单一 prefab 路径下的全部实例状态（C-1 五字典合并）。
    /// 原先分散在 _idle / _instancesByPath / _idleCountByPath / _activeCountByPath /
    /// _cachedPrefabPaths 五个集合里的「同一概念实体」内聚到此处，消除多字典手工同步的状态不一致风险（§10.2-④）。
    /// </summary>
    internal sealed class PrefabPoolState
    {
        public readonly Stack<GameObject> Idle = new();
        public readonly HashSet<GameObject> Instances = new();
        public int ActiveCount;
        public int IdleCount;
        public bool IsPersistent;
        public bool IsPrefabCached;
    }

    public sealed class GameObjectPool
    {
        private readonly Transform _root;
        private readonly PoolContainerMode _mode;
        private readonly Vector3 _farAway;
        // C-1：五字典合并为单一状态字典。
        private readonly Dictionary<string, PrefabPoolState> _states = new();
        // C-3：O(1) 反向索引（ETPro instPathCache 精神），用于污染检测与防双回收。
        private readonly Dictionary<GameObject, string> _instanceToPath = new();
        private readonly BoundedStore<string, GameObject> _prefabCache;
        // C-2：实例库存策略（默认 CapacityInstancePolicy，保留 A-D1 行为）。
        private readonly IInstanceRecyclePolicy _recyclePolicy;
        // C-4：构造时记录主线程 id，用于运行时主线程断言。
        private readonly int _mainThreadId;

        public GameObjectPool(
            Transform root,
            int prefabCapacity = 64,
            PoolContainerMode mode = PoolContainerMode.ChangeParent,
            Vector3? farAway = null,
            int maxIdlePerPrefab = 64,
            IInstanceRecyclePolicy recyclePolicy = null)
        {
            _root = root;
            _mode = mode;
            _farAway = farAway ?? new Vector3(9999f, 9999f, 9999f);
            _recyclePolicy = recyclePolicy ?? new CapacityInstancePolicy(maxIdlePerPrefab);
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _prefabCache = new BoundedStore<string, GameObject>(prefabCapacity, new LruPolicy<string>(), OnPrefabEvicted);
        }

        public async UniTask<GameObject> GetAsync(string prefabPath, Transform parent = null)
        {
            AssertMainThread();

            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                throw new ArgumentNullException(nameof(prefabPath));
            }

            if (_states.TryGetValue(prefabPath, out var state) && state.Idle.Count > 0)
            {
                while (state.Idle.Count > 0)
                {
                    var inst = state.Idle.Pop();
                    state.IdleCount--;

                    if (inst == null)
                    {
                        // A-B1：被 Unity 销毁的 idle 实例（假 null）仍需从注册表清出，
                        // 否则 Clear() 会对其调 Object.Destroy(null)。反向索引跳过（Unity-null 不可作 key）。
                        UnregisterInstance(prefabPath, inst);
                        continue;
                    }

                    var tag = inst.GetComponent<PoolInstanceTag>();
                    if (tag == null || !tag.IsRecycled || tag.PrefabPath != prefabPath)
                    {
                    UnregisterInstance(prefabPath, inst);
                    DestroyInstance(inst);
                    continue;
                    }

                    ActivateInstance(inst, parent);
                    tag.IsRecycled = false;
                    state.ActiveCount++;
                    return inst;
                }
            }

            GameObject prefab = null;
            if (!_prefabCache.TryGet(prefabPath, out prefab))
            {
                var gate = PoolDependencies.LoadGates.GetOrAdd(prefabPath, static _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync(CancellationToken.None);
                try
                {
                    await UniTask.SwitchToMainThread();
                    if (!_prefabCache.TryGet(prefabPath, out prefab))
                    {
                        prefab = await LoadPrefabAsync(prefabPath);
                        await UniTask.SwitchToMainThread();
                        if (prefab == null)
                        {
                            return null;
                        }

                        _prefabCache.Put(prefabPath, prefab);
                        GetOrAddState(prefabPath).IsPrefabCached = true;
                    }
                }
                finally
                {
                    gate.Release();
                    if (!_prefabCache.TryGet(prefabPath, out _))
                    {
                        PoolDependencies.LoadGates.TryRemove(prefabPath, out _);
                    }
                }
            }

            await UniTask.SwitchToMainThread();
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
            AssertMainThread();

            if (instance == null)
            {
                return;
            }

            var tag = instance.GetComponent<PoolInstanceTag>();
            if (tag == null || string.IsNullOrEmpty(tag.PrefabPath))
            {
                DestroyInstance(instance);
                return;
            }

            var prefabPath = tag.PrefabPath;

            // C-3 反向索引污染检测（ETPro instPathCache）：实例被错误归还到别的池/路径。
            if (!_instanceToPath.TryGetValue(instance, out var registeredPath))
            {
                if (!tag.IsRecycled)
                {
                    Debug.LogError($"[GameObjectPool] 实例 '{instance.name}' 不属于当前对象池，PrefabPath='{prefabPath}'（已拒绝入库）。");
                }

                return;
            }

            // 已回收 → 直接返回（配合 tag.IsRecycled 的 O(1) 防双回收）。
            if (tag.IsRecycled)
            {
                return;
            }

            if (registeredPath != prefabPath)
            {
                Debug.LogError($"[GameObjectPool] 实例被错误归还至路径 '{prefabPath}'，但其注册路径为 '{registeredPath}'（反向索引污染，已销毁不入库）。");
                // 该实例在被错误归还时仍是 active（tag.IsRecycled==false），需补足注册路径的活跃计数，
                // 否则 GetActiveCount(registeredPath) 会虚高 1（整体 Review 发现的 Bug-1）。
                var regState = _states.TryGetValue(registeredPath, out var s) ? s : null;
                UnregisterInstance(registeredPath, instance);
                if (regState != null)
                {
                    regState.ActiveCount--;
                }
                DestroyInstance(instance);
                return;
            }

            var state = GetOrAddState(prefabPath);

            // C-2 实例库存策略化 + C-3 常驻保护（常驻路径永不被容量淘汰）。
            if (!state.IsPersistent && !_recyclePolicy.ShouldRetain(prefabPath, state.IdleCount, instance))
            {
                UnregisterInstance(prefabPath, instance);
                DeactivateInstance(instance);
                state.ActiveCount--;
                DestroyInstance(instance);
                return;
            }

            // 入 idle 库存：保留在 Instances 集合与反向索引中（实例仍归属该 prefabPath）。
            DeactivateInstance(instance);
            tag.IsRecycled = true;
            state.Idle.Push(instance);
            state.IdleCount++;
            state.ActiveCount--;
        }

        /// <summary>
        /// C-3 常驻保护：标记某 prefab 路径为常驻，其 idle 实例永不被容量淘汰。
        /// </summary>
        public void MarkPersistent(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return;
            }

            GetOrAddState(prefabPath).IsPersistent = true;
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
            AssertMainThread();

            using var pooledInstances = CollectionPool.RentList<GameObject>();
            var instanceSnapshot = pooledInstances.Value;
            foreach (var state in _states.Values)
            {
                foreach (var inst in state.Instances)
                {
                    if (inst != null)
                    {
                        instanceSnapshot.Add(inst);
                    }
                }
            }

            foreach (var inst in instanceSnapshot)
            {
                DestroyInstance(inst);
            }

            _states.Clear();
            _instanceToPath.Clear();
            _prefabCache.Clear();
        }

        public int GetIdleCount(string prefabPath)
        {
            return _states.TryGetValue(prefabPath, out var state) ? state.IdleCount : 0;
        }

        public int GetActiveCount(string prefabPath)
        {
            return _states.TryGetValue(prefabPath, out var state) ? state.ActiveCount : 0;
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
            var state = GetOrAddState(prefabPath);
            state.Instances.Add(instance);
            state.ActiveCount++;
            _instanceToPath[instance] = prefabPath;
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

        // EditMode（含 EditMode 测试）下 Object.Destroy 会触发 Error 日志（"Destroy may not be called
        // from edit mode"），被 NUnit 当非预期日志判失败；故 EditMode 用 DestroyImmediate（同步、立即可见），
        // 运行时仍用延迟 Destroy 避免卡帧。零行为语义变化。
        private void DestroyInstance(GameObject instance)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(instance);
#else
            Object.Destroy(instance);
#endif
        }

        private void OnPrefabEvicted(string prefabPath, GameObject prefab)
        {
            if (_states.TryGetValue(prefabPath, out var state))
            {
                state.IsPrefabCached = false;
            }

            PoolDependencies.ReleaseAssetByPath?.Invoke(prefabPath);
            PoolDependencies.LoadGates.TryRemove(prefabPath, out _);
        }

        private PrefabPoolState GetOrAddState(string prefabPath)
        {
            if (!_states.TryGetValue(prefabPath, out var state))
            {
                state = new PrefabPoolState();
                _states[prefabPath] = state;
            }

            return state;
        }

        // C-3：从「实例登记表 + 反向索引」移除一个实例；清理空状态以避免残留。
        private void UnregisterInstance(string prefabPath, GameObject instance)
        {
            if (_states.TryGetValue(prefabPath, out var state))
            {
                state.Instances.Remove(instance);
                if (state.Instances.Count == 0 && state.Idle.Count == 0 && state.ActiveCount <= 0 && !state.IsPrefabCached)
                {
                    _states.Remove(prefabPath);
                }
            }

            // Unity-null 实例不可作 key，仅在真实引用存在时清理反向索引。
            if (instance != null)
            {
                _instanceToPath.Remove(instance);
            }
        }

        // C-4：[MainThread] 运行时断言，把「仅主线程」从文档约束变为硬断言（§10.2-⑤ / §8.2-⑥）。
        private void AssertMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                throw new InvalidOperationException("GameObjectPool 仅允许在主线程调用。");
            }
        }
    }
}
