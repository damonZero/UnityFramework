using System;
using YooAsset;

namespace Framework.Asset
{
    public sealed class AssetInitializeHandle
    {
        private readonly AsyncOperationBase _operation;
        private readonly Action<AssetInitializeHandle> _onCompleted;
        private bool _completed;
        private bool _isSucceeded;
        private string _error;

        internal AssetInitializeHandle(AsyncOperationBase operation, Action<AssetInitializeHandle> onCompleted)
        {
            _operation = operation;
            _onCompleted = onCompleted;
        }

        public float Progress
        {
            get
            {
                TryComplete();
                return _completed ? 1f : _operation?.Progress ?? 1f;
            }
        }

        public bool IsDone
        {
            get
            {
                TryComplete();
                return _completed;
            }
        }

        public bool IsSucceeded
        {
            get
            {
                TryComplete();
                return _completed && _isSucceeded;
            }
        }

        public string Error
        {
            get
            {
                TryComplete();
                return _error;
            }
        }

        internal static AssetInitializeHandle Succeeded()
        {
            return new AssetInitializeHandle(null, null)
            {
                _completed = true,
                _isSucceeded = true
            };
        }

        internal static AssetInitializeHandle Failed(string error)
        {
            return new AssetInitializeHandle(null, null)
            {
                _completed = true,
                _isSucceeded = false,
                _error = error
            };
        }

        private void TryComplete()
        {
            if (_completed)
                return;

            if (_operation == null)
            {
                _completed = true;
                _isSucceeded = true;
                return;
            }

            if (!_operation.IsDone)
                return;

            _completed = true;
            _isSucceeded = _operation.Status == EOperationStatus.Succeeded;
            _error = _operation.Error;
            _onCompleted?.Invoke(this);
        }
    }
}
