namespace Framework.Cache
{
    public interface ICacheResContainer<TKey, TValue>
        where TKey : notnull
    {
        bool TryGet(TKey key, out TValue value);

        TValue GetOrCreate(TKey key);

        bool TryRemove(TKey key, out TValue value);

        void Clear();
    }
}
