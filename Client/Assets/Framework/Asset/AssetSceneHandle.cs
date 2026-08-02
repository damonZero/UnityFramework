using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Framework.Asset
{
    public sealed class AssetSceneHandle : IDisposable
    {
        private YooAsset.SceneHandle _handle;
        private readonly Action<YooAsset.SceneHandle> _onDispose;
        private readonly Func<YooAsset.SceneHandle, UniTask> _onUnloadStarted;
        private bool _disposed;

        internal AssetSceneHandle(
            YooAsset.SceneHandle handle,
            Action<YooAsset.SceneHandle> onDispose = null,
            Func<YooAsset.SceneHandle, UniTask> onUnloadStarted = null)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _onDispose = onDispose;
            _onUnloadStarted = onUnloadStarted;
        }

        public float Progress => _handle.Progress;
        public bool IsDone => _handle.IsDone;
        public bool IsValid => !_disposed && _handle.IsValid;
        public string Error => _handle.Error;
        public string SceneName => _handle.SceneName;
        public Scene Scene => _handle.SceneObject;

        public bool ActivateScene()
        {
            ThrowIfDisposed();
            return _handle.ActivateScene();
        }

        public async UniTask UnloadAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_onUnloadStarted != null)
                await _onUnloadStarted(_handle);
            else
                await _handle.UnloadSceneAsync().ToUniTask();

            _onDispose?.Invoke(_handle);
        }

        /// <summary>
        /// Begins an asynchronous scene unload. The unload is fire-and-forget;
        /// use <see cref="UnloadAsync"/> when you need to await completion.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_onUnloadStarted != null)
                _onUnloadStarted(_handle).Forget();
            else
                _handle.UnloadSceneAsync().ToUniTask().Forget();

            _onDispose?.Invoke(_handle);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AssetSceneHandle));
        }
    }
}
