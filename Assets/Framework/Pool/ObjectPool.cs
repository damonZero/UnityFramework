using System;
using System.Collections.Generic;
using System.Threading;

namespace Framework.Pool
{
    public sealed class ObjectPool<T> : IPool<T>
        where T : class
    {
        private readonly Stack<T> _idle;
        private readonly Func<T> _factory;
        private readonly Action<T>? _reset;
        private readonly int _maxIdle;
        private readonly object _gate = new();

        private int _createdCount;
        private int _rentCount;
        private int _returnCount;

        public ObjectPool(Func<T> factory, Action<T>? reset = null, int maxIdle = 64, int preload = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
            _maxIdle = Math.Max(0, maxIdle);
            _idle = new Stack<T>(_maxIdle > 0 ? _maxIdle : 4);

            for (var i = 0; i < preload; i++)
            {
                _idle.Push(Create());
            }
        }

        public int IdleCount
        {
            get
            {
                lock (_gate)
                {
                    return _idle.Count;
                }
            }
        }

        public int CreatedCount => _createdCount;
        public int RentCount => _rentCount;
        public int ReturnCount => _returnCount;

        public T Rent()
        {
            lock (_gate)
            {
                _rentCount++;
                if (_idle.Count > 0)
                {
                    return _idle.Pop();
                }
            }

            return Create();
        }

        public IPoolLease<T> RentLease()
        {
            return new PoolLease<T>(this, Rent());
        }

        public void Return(T item)
        {
            if (item is null)
            {
                return;
            }

            _reset?.Invoke(item);

            lock (_gate)
            {
                _returnCount++;
                if (_maxIdle <= 0 || _idle.Count < _maxIdle)
                {
                    _idle.Push(item);
                }
            }
        }

        public PoolStatistics GetStatistics()
        {
            lock (_gate)
            {
                return new PoolStatistics(_idle.Count, _createdCount, _rentCount, _returnCount, _maxIdle);
            }
        }

        private T Create()
        {
            var item = _factory();
            Interlocked.Increment(ref _createdCount);
            return item;
        }
    }
}
