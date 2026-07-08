using System;
using System.Collections.Generic;
using System.Threading;

namespace Framework.Cache
{
    /// <summary>
    /// 有界 KV 存储（Cache 的重构版，见 §10.5 目标架构）。
    /// 修复点：
    ///  - Put 覆盖旧值时走 Remove + Add 两步，策略感知值变化，旧值触发 onEvicted（A-B2 / §10.2-②）。
    ///  - GetOrAdd 同 key 并发 miss 仅一个线程执行 factory，其余等待复用结果（single-flight，§10.2-③）。
    ///  - 淘汰回调 onEvicted 与 factory 均在锁外执行，兼容既有并发测试。
    /// </summary>
    public sealed class BoundedStore<TKey, TValue> : ICache<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _values;
        private readonly IStoreEvictionPolicy<TKey> _policy;
        private readonly int _capacity;
        private readonly Action<TKey, TValue>? _onEvicted;
        private readonly object _gate = new();

        // single-flight：同一 key 并发 miss 时复用同一个 factory 计算结果。
        private readonly object _inflightGate = new();
        private readonly Dictionary<TKey, Lazy<TValue>> _inflight = new();

        public BoundedStore(int capacity, IStoreEvictionPolicy<TKey> policy, Action<TKey, TValue>? onEvicted = null)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _onEvicted = onEvicted;
            _values = new Dictionary<TKey, TValue>(capacity > 0 ? capacity : 4);
        }

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _values.Count;
                }
            }
        }

        public int Capacity => _capacity;

        public bool TryGet(TKey key, out TValue value)
        {
            lock (_gate)
            {
                if (_values.TryGetValue(key, out value!))
                {
                    _policy.OnAccessed(key);
                    return true;
                }

                value = default!;
                return false;
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            // 快路径（命中直接返回，不进入 inflight 协议）。
            lock (_gate)
            {
                if (_values.TryGetValue(key, out var existing))
                {
                    _policy.OnAccessed(key);
                    return existing;
                }
            }

            // single-flight：同一 key 仅一个 owner 执行 factory。
            Lazy<TValue> ownerLazy;
            bool isOwner;
            lock (_inflightGate)
            {
                // 二次检查（可能已被 Put / 另一 owner 提交）。
                lock (_gate)
                {
                    if (_values.TryGetValue(key, out var existing))
                    {
                        _policy.OnAccessed(key);
                        return existing;
                    }
                }

                if (_inflight.TryGetValue(key, out var existingLazy))
                {
                    ownerLazy = existingLazy;
                    isOwner = false;
                }
                else
                {
                    ownerLazy = new Lazy<TValue>(() => factory(key), LazyThreadSafetyMode.ExecutionAndPublication);
                    _inflight[key] = ownerLazy;
                    isOwner = true;
                }
            }

            if (!isOwner)
            {
                // 等待 owner 计算完成，再读取最终落库值。
                _ = ownerLazy.Value;
                lock (_gate)
                {
                    if (_values.TryGetValue(key, out var existing))
                    {
                        _policy.OnAccessed(key);
                        return existing;
                    }
                }

                return ownerLazy.Value;
            }

            TValue finalValue;
            List<(TKey Key, TValue Value)>? evictions = null;

            try
            {
                var computed = ownerLazy.Value;
                lock (_gate)
                {
                    if (_values.TryGetValue(key, out var existing))
                    {
                        // 计算期间他人已写入（Put 或其它 owner 提交）→ 丢弃本线程计算结果。
                        finalValue = existing;
                        evictions = new List<(TKey Key, TValue Value)> { (key, computed) };
                    }
                    else
                    {
                        PutUnsafe(key, computed, ref evictions);
                        finalValue = computed;
                    }
                }
            }
            finally
            {
                // H2 修复：factory 抛异常时 Lazy 进入 faulted 态；必须在 finally 中清除 inflight，
                // 否则该 key 永久故障、后续 GetOrAdd 复抛缓存异常。
                lock (_inflightGate)
                {
                    _inflight.Remove(key);
                }
            }

            InvokeEvictions(evictions);
            return finalValue;
        }

        public void Put(TKey key, TValue value)
        {
            List<(TKey Key, TValue Value)>? evictions = null;
            lock (_gate)
            {
                PutUnsafe(key, value, ref evictions);
            }

            InvokeEvictions(evictions);
        }

        public bool Remove(TKey key)
        {
            TValue value;
            bool removed;
            lock (_gate)
            {
                if (_values.TryGetValue(key, out value!))
                {
                    _values.Remove(key);
                    _policy.OnRemoved(key);
                    removed = true;
                }
                else
                {
                    removed = false;
                }
            }

            if (removed)
            {
                _onEvicted?.Invoke(key, value);
            }

            return removed;
        }

        public void Clear()
        {
            lock (_gate)
            {
                _values.Clear();
                _policy.Clear();
            }
        }

        private void InvokeEvictions(List<(TKey Key, TValue Value)>? evictions)
        {
            if (evictions == null || _onEvicted == null)
            {
                return;
            }

            foreach (var eviction in evictions)
            {
                _onEvicted.Invoke(eviction.Key, eviction.Value);
            }
        }

        private void PutUnsafe(TKey key, TValue value, ref List<(TKey Key, TValue Value)>? evictions)
        {
            // 覆盖语义：拆为 Remove(旧) + Add(新)，让策略感知值变化，旧值触发 onEvicted（A-B2 / §10.2-②）。
            if (_values.TryGetValue(key, out var oldValue))
            {
                _values.Remove(key);
                _policy.OnRemoved(key);
                evictions ??= new List<(TKey Key, TValue Value)>();
                evictions.Add((key, oldValue));
            }

            _values[key] = value;
            _policy.OnAdded(key);

            // H1 修复：淘汰循环不得选回「刚 Put 的 key」。无序策略（如旧 CapacityPolicy 的 reservoir）
            // 可能返回刚写入的 key，若直接淘汰会破坏「Put 后 key 必存在」不变量。遇到该情况退出循环，
            // 避免自旋；容量为 1 的边界下可能临时超出 1，需策略侧保证有序（见 CapacityPolicy 的 FIFO 实现）。
            while (_capacity > 0 && _values.Count > _capacity)
            {
                if (!_policy.TrySelectEvictionCandidate(out var evictKey) ||
                    EqualityComparer<TKey>.Default.Equals(evictKey, key))
                {
                    break;
                }

                if (_values.TryGetValue(evictKey, out var evictValue))
                {
                    _values.Remove(evictKey);
                    _policy.OnRemoved(evictKey);
                    evictions ??= new List<(TKey Key, TValue Value)>();
                    evictions.Add((evictKey, evictValue));
                }
                else
                {
                    // 策略返回的 key 已不在字典中 → 仅同步策略状态后重试。
                    _policy.OnRemoved(evictKey);
                }
            }
        }
    }
}
