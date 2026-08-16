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
        private readonly HashSet<T> _idleSet;

#if UNITY_ASSERTIONS
        private readonly int _ownerThreadId;
#endif

        public SingleThreadObjectPool(Func<T> factory, Action<T> reset, int maxIdle)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset ?? throw new ArgumentNullException(nameof(reset));
            _maxIdle = Math.Max(0, maxIdle);
            _idle = new Stack<T>(_maxIdle > 0 ? _maxIdle : 4);
            _idleSet = new HashSet<T>(ReferenceComparer.Instance);

#if UNITY_ASSERTIONS
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
            _idleSet.Remove(item);
            return item;
        }

        public void Return(T item)
        {
            if (item == null)
            {
                return;
            }

            AssertOwnerThread();

            // 防双回收：同一实例经值拷贝后两次 Return 会损坏共享池，必须在所有构建下拦截（不能只在 UNITY_ASSERTIONS 下）。
            if (_idleSet.Contains(item))
            {
                return;
            }

            _reset(item);

            if (_maxIdle > 0 && _idle.Count >= _maxIdle)
            {
                return;
            }

            _idle.Push(item);
            _idleSet.Add(item);
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

        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public static readonly ReferenceComparer Instance = new();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
