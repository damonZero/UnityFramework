namespace Framework.Cache
{
    /// <summary>
    /// 淘汰策略契约（三方法重塑）。
    /// 由 <see cref="BoundedStore{TKey,TValue}"/> 在锁内决定调用哪个方法，
    /// 策略自身不再需要维护「值是否存在」的冗余投影字典。
    /// </summary>
    public interface IStoreEvictionPolicy<TKey>
        where TKey : notnull
    {
        /// <summary>新条目加入（Put 新增 / 覆盖后的 Add 阶段）。</summary>
        void OnAdded(TKey key);

        /// <summary>已有条目被访问（TryGet 命中）。</summary>
        void OnAccessed(TKey key);

        /// <summary>条目被移除（Remove / 覆盖前的 Remove 阶段 / 容量淘汰）。</summary>
        void OnRemoved(TKey key);

        /// <summary>返回应被淘汰的候选 key；无可淘汰项时返回 false。</summary>
        bool TrySelectEvictionCandidate(out TKey key);

        void Clear();
    }
}
