using System;
using System.Collections.Generic;
using System.Threading;

namespace Framework.Pool
{
    internal sealed class SingleThreadObjectPool<T>
        where T : class
    {
        private readonly Stack<T> _idle;
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;
        private readonly int _maxIdle;

#if UNITY_ASSERTIONS
        private readonly HashSet<T> _idleSet;
        private readonly int _ownerThreadId;
#endif

        public SingleThreadObjectPool(Func<T> factory, Action<T> reset, int maxIdle)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset ?? throw new ArgumentNullException(nameof(reset));
            _maxIdle = Math.Max(0, maxIdle);
            _idle = new Stack<T>(_maxIdle > 0 ? _maxIdle : 4);

#if UNITY_ASSERTIONS
            _idleSet = new HashSet<T>(ReferenceComparer.Instance);
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
#endif
        }

        public T Rent()
        {
            AssertOwnerThread();

            if (_idle.Count <= 0)
            {
                return _factory();
            }

            var item = _idle.Pop();
#if UNITY_ASSERTIONS
            _idleSet.Remove(item);
#endif
            return item;
        }

        public void Return(T item)
        {
            if (item == null)
            {
                return;
            }

            AssertOwnerThread();

#if UNITY_ASSERTIONS
            if (_idleSet.Contains(item))
            {
                return;
            }
#endif

            _reset(item);

            if (_maxIdle > 0 && _idle.Count >= _maxIdle)
            {
                return;
            }

            _idle.Push(item);
#if UNITY_ASSERTIONS
            _idleSet.Add(item);
#endif
        }

        private void AssertOwnerThread()
        {
#if UNITY_ASSERTIONS
            if (Thread.CurrentThread.ManagedThreadId != _ownerThreadId)
            {
                throw new InvalidOperationException("SingleThreadObjectPool can only be used from the thread that created it.");
            }
#endif
        }

#if UNITY_ASSERTIONS
        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public static readonly ReferenceComparer Instance = new();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
#endif
    }
}
