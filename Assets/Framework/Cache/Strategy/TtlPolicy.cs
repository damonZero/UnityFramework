using System;
using System.Collections.Generic;

namespace Framework.Cache
{
    /// <summary>
    /// TTL 淘汰策略：按写入/访问时间戳淘汰过期项。
    /// 注意：本策略仅对「已过期」项返回淘汰候选；若需硬性容量上限，请配合
    /// <see cref="CapacityPolicy{TKey}"/> 通过 <see cref="CompositePolicy{TKey}"/> 组合使用。
    /// </summary>
    public sealed class TtlPolicy<TKey> : IStoreEvictionPolicy<TKey>
        where TKey : notnull
    {
        private readonly TimeSpan _ttl;
        private readonly bool _refreshOnAccess;
        private readonly Func<long> _clock;
        private readonly Dictionary<TKey, long> _timestamps = new();

        public TtlPolicy(TimeSpan ttl, bool refreshOnAccess = true, Func<long> clock = null)
        {
            if (ttl < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(ttl));
            }

            _ttl = ttl;
            _refreshOnAccess = refreshOnAccess;
            _clock = clock ?? (() => DateTime.UtcNow.Ticks);
        }

        public void OnAdded(TKey key) => _timestamps[key] = _clock();

        public void OnAccessed(TKey key)
        {
            if (_refreshOnAccess)
            {
                _timestamps[key] = _clock();
            }
        }

        public void OnRemoved(TKey key) => _timestamps.Remove(key);

        public void Clear() => _timestamps.Clear();

        public bool TrySelectEvictionCandidate(out TKey key)
        {
            var now = _clock();
            var threshold = _ttl.Ticks;
            foreach (var kv in _timestamps)
            {
                if (now - kv.Value >= threshold)
                {
                    key = kv.Key;
                    return true;
                }
            }

            key = default!;
            return false;
        }
    }
}
