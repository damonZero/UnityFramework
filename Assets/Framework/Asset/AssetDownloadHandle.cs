using Cysharp.Threading.Tasks;

namespace Framework.Asset
{
    public sealed class AssetDownloadHandle
    {
        private readonly YooAsset.ResourceDownloaderOperation _operation;

        internal AssetDownloadHandle(YooAsset.ResourceDownloaderOperation operation)
        {
            _operation = operation;
        }

        public float Progress => _operation?.Progress ?? 1f;
        public bool IsDone => _operation == null || _operation.IsDone;
        public bool IsSucceeded => _operation != null && _operation.Status == YooAsset.EOperationStatus.Succeeded;
        public string Error => _operation?.Error;
        public int TotalDownloadCount => _operation?.TotalDownloadCount ?? 0;
        public long TotalDownloadBytes => _operation?.TotalDownloadBytes ?? 0;
        public int CurrentDownloadCount => _operation?.CurrentDownloadCount ?? 0;
        public long CurrentDownloadBytes => _operation?.CurrentDownloadBytes ?? 0;

        public void Start()
        {
            _operation?.StartDownload();
        }

        public void Pause()
        {
            _operation?.PauseDownload();
        }

        public void Resume()
        {
            _operation?.ResumeDownload();
        }

        public void Cancel()
        {
            _operation?.CancelDownload();
        }

        public UniTask WaitAsync()
        {
            return _operation == null ? UniTask.CompletedTask : _operation.ToUniTask();
        }
    }
}
