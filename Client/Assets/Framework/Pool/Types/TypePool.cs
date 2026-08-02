using System;
using System.Collections.Concurrent;

namespace Framework.Pool
{
    public static class TypePool
    {
        private static readonly ConcurrentDictionary<Type, object> Pools = new();

        public static ObjectPool<T> Register<T>(Func<T> factory, Action<T>? reset = null, int maxIdle = 64)
            where T : class
        {
            var pool = new ObjectPool<T>(factory, reset, maxIdle);
            Pools[typeof(T)] = pool;
            return pool;
        }

        public static bool TryGet<T>(out ObjectPool<T> pool)
            where T : class
        {
            if (Pools.TryGetValue(typeof(T), out var value) && value is ObjectPool<T> typed)
            {
                pool = typed;
                return true;
            }

            pool = null!;
            return false;
        }

        public static ObjectPool<T> GetOrCreate<T>(Func<T>? factory = null, Action<T>? reset = null, int maxIdle = 64)
            where T : class, new()
        {
            if (TryGet<T>(out var pool))
            {
                return pool;
            }

            return Register(factory ?? (() => new T()), reset, maxIdle);
        }
    }
}
