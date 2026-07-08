namespace Framework.Cache
{
    /// <summary>
    /// 组合策略：将多个 <see cref="IStoreEvictionPolicy{TKey}"/> 扇出。
    /// 任一子策略返回淘汰候选即淘汰该 key（OnRemoved 会扇出到所有子策略以保持一致）。
    /// </summary>
    public sealed class CompositePolicy<TKey> : IStoreEvictionPolicy<TKey>
        where TKey : notnull
    {
        private readonly IStoreEvictionPolicy<TKey>[] _policies;

        public CompositePolicy(params IStoreEvictionPolicy<TKey>[] policies)
        {
            _policies = policies ?? throw new System.ArgumentNullException(nameof(policies));
        }

        public void OnAdded(TKey key)
        {
            foreach (var policy in _policies)
            {
                policy.OnAdded(key);
            }
        }

        public void OnAccessed(TKey key)
        {
            foreach (var policy in _policies)
            {
                policy.OnAccessed(key);
            }
        }

        public void OnRemoved(TKey key)
        {
            foreach (var policy in _policies)
            {
                policy.OnRemoved(key);
            }
        }

        public void Clear()
        {
            foreach (var policy in _policies)
            {
                policy.Clear();
            }
        }

        public bool TrySelectEvictionCandidate(out TKey key)
        {
            foreach (var policy in _policies)
            {
                if (policy.TrySelectEvictionCandidate(out key))
                {
                    return true;
                }
            }

            key = default!;
            return false;
        }
    }
}
