using System.Collections.Generic;

namespace Framework.Pool
{
    public static class CollectionPool
    {
        public static PooledList<T> RentList<T>()
        {
            return new PooledList<T>(ListPool<T>.Shared.Rent());
        }

        public static PooledHashSet<T> RentHashSet<T>()
        {
            return new PooledHashSet<T>(HashSetPool<T>.Shared.Rent());
        }

        public static PooledQueue<T> RentQueue<T>()
        {
            return new PooledQueue<T>(QueuePool<T>.Shared.Rent());
        }

        public static PooledStack<T> RentStack<T>()
        {
            return new PooledStack<T>(StackPool<T>.Shared.Rent());
        }

        public static PooledDictionary<TKey, TValue> RentDictionary<TKey, TValue>()
            where TKey : notnull
        {
            return new PooledDictionary<TKey, TValue>(DictionaryPool<TKey, TValue>.Shared.Rent());
        }

        internal static void Return<T>(List<T> value) => ListPool<T>.Shared.Return(value);
        internal static void Return<T>(HashSet<T> value) => HashSetPool<T>.Shared.Return(value);
        internal static void Return<T>(Queue<T> value) => QueuePool<T>.Shared.Return(value);
        internal static void Return<T>(Stack<T> value) => StackPool<T>.Shared.Return(value);
        internal static void Return<TKey, TValue>(Dictionary<TKey, TValue> value) where TKey : notnull => DictionaryPool<TKey, TValue>.Shared.Return(value);

        internal static class ListPool<T>
        {
            internal static readonly SingleThreadObjectPool<List<T>> Shared = new(() => new List<T>(), list => list.Clear(), 32);
        }

        internal static class HashSetPool<T>
        {
            internal static readonly SingleThreadObjectPool<HashSet<T>> Shared = new(() => new HashSet<T>(), set => set.Clear(), 32);
        }

        internal static class QueuePool<T>
        {
            internal static readonly SingleThreadObjectPool<Queue<T>> Shared = new(() => new Queue<T>(), queue => queue.Clear(), 32);
        }

        internal static class StackPool<T>
        {
            internal static readonly SingleThreadObjectPool<Stack<T>> Shared = new(() => new Stack<T>(), stack => stack.Clear(), 32);
        }

        internal static class DictionaryPool<TKey, TValue>
            where TKey : notnull
        {
            internal static readonly SingleThreadObjectPool<Dictionary<TKey, TValue>> Shared = new(() => new Dictionary<TKey, TValue>(), dict => dict.Clear(), 32);
        }
    }
}
