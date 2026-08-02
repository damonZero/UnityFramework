using System;

namespace Framework.Pool
{
    public interface IPoolLease<out T> : IDisposable
    {
        T Value { get; }

        bool IsDisposed { get; }
    }
}
