namespace Framework.Cache
{
    public interface ICacheEvictionPolicy<TKey>
        where TKey : notnull
    {
        void Touch(TKey key);

        void Remove(TKey key);

        bool TrySelectEvictionCandidate(out TKey key);

        void Clear();
    }
}
