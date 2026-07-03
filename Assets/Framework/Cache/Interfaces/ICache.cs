using System;

namespace Framework.Cache
{
    public interface ICache<TKey, TValue>
        where TKey : notnull
    {
        int Count { get; }

        int Capacity { get; }

        bool TryGet(TKey key, out TValue value);

        TValue GetOrAdd(TKey key, Func<TKey, TValue> factory);

        void Put(TKey key, TValue value);

        bool Remove(TKey key);

        void Clear();
    }
}
