using System;

namespace Framework.Pool
{
    public struct PoolLease<T> : IPoolLease<T>
        where T : class
    {
        private readonly IPool<T> _pool;
        private readonly T _value;
        private bool _isDisposed;

        internal PoolLease(IPool<T> pool, T value)
        {
            _pool = pool;
            _value = value;
            _isDisposed = false;
        }

        public T Value => _value;

        public bool IsDisposed => _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _pool?.Return(_value);
        }
    }
}
