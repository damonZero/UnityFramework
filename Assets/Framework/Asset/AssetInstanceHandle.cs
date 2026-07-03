using System;
using UnityEngine;

namespace Framework.Asset
{
    public sealed class AssetInstanceHandle : IDisposable
    {
        private readonly AssetHandle<GameObject> _assetHandle;
        private bool _disposed;

        internal AssetInstanceHandle(GameObject instance, AssetHandle<GameObject> assetHandle)
        {
            Instance = instance;
            _assetHandle = assetHandle ?? throw new ArgumentNullException(nameof(assetHandle));
        }

        public GameObject Instance { get; private set; }
        public AssetHandle<GameObject> SourceHandle => _assetHandle;
        public bool IsValid => !_disposed && Instance != null && _assetHandle.IsValid;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (Instance != null)
                UnityEngine.Object.Destroy(Instance);

            Instance = null;
            _assetHandle.Dispose();
        }
    }
}
