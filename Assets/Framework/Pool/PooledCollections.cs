using System;
using System.Collections.Generic;

namespace Framework.Pool
{
    public struct PooledList<T> : IDisposable
    {
        private List<T> _value;

        public PooledList(List<T> value) => _value = value;
        public List<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    public struct PooledHashSet<T> : IDisposable
    {
        private HashSet<T> _value;

        public PooledHashSet(HashSet<T> value) => _value = value;
        public HashSet<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    public struct PooledQueue<T> : IDisposable
    {
        private Queue<T> _value;

        public PooledQueue(Queue<T> value) => _value = value;
        public Queue<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    public struct PooledStack<T> : IDisposable
    {
        private Stack<T> _value;

        public PooledStack(Stack<T> value) => _value = value;
        public Stack<T> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }

    public struct PooledDictionary<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        private Dictionary<TKey, TValue> _value;

        public PooledDictionary(Dictionary<TKey, TValue> value) => _value = value;
        public Dictionary<TKey, TValue> Value => _value;

        public void Dispose()
        {
            if (_value == null)
                return;

            CollectionPool.Return(_value);
            _value = null;
        }
    }
}
