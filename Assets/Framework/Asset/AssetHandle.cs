using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.Asset
{
    public sealed class AssetHandle<T> : IDisposable where T : Object
    {
        private readonly YooAsset.AssetHandle _handle;
        private readonly Action<YooAsset.AssetHandle> _onDispose;
        private bool _disposed;

        internal AssetHandle(YooAsset.AssetHandle handle, Action<YooAsset.AssetHandle> onDispose = null)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _onDispose = onDispose;
        }

        public float Progress => _handle.Progress;
        public bool IsDone => _handle.IsDone;
        public bool IsValid => !_disposed && _handle.IsValid;
        public string Error => _handle.Error;

        public T Asset => _handle.AssetObject as T;

        internal GameObject Instantiate(Transform parent = null)
        {
            ThrowIfDisposed();
            return parent != null
                ? _handle.InstantiateSync(new YooAsset.InstantiateOptions(true, parent, false))
                : _handle.InstantiateSync();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _handle.Release();
            _onDispose?.Invoke(_handle);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AssetHandle<T>));
        }
    }
}
