namespace Framework.Asset
{
    public sealed class AssetUpdateManifestHandle
    {
        private readonly YooAsset.ResourcePackage _package;
        private readonly YooAsset.RequestPackageVersionOperation _versionOperation;
        private YooAsset.LoadPackageManifestOperation _manifestOperation;

        internal AssetUpdateManifestHandle(
            YooAsset.ResourcePackage package,
            YooAsset.RequestPackageVersionOperation versionOperation,
            int timeout)
        {
            _package = package;
            _versionOperation = versionOperation;
            Timeout = timeout;
        }

        public int Timeout { get; }
        public string PackageVersion => _versionOperation?.PackageVersion;
        public float Progress
        {
            get
            {
                if (_versionOperation == null)
                    return 1f;
                if (!_versionOperation.IsDone)
                    return _versionOperation.Progress * 0.5f;
                if (_manifestOperation == null)
                    return 0.5f;
                return 0.5f + _manifestOperation.Progress * 0.5f;
            }
        }
        public bool IsVersionDone => _versionOperation == null || _versionOperation.IsDone;
        public bool IsVersionSucceeded =>
            _versionOperation != null &&
            _versionOperation.Status == YooAsset.EOperationStatus.Succeeded;
        public bool IsManifestDone => _manifestOperation != null && _manifestOperation.IsDone;
        public bool IsDone => IsVersionDone && (!IsVersionSucceeded || IsManifestDone);
        public bool IsSucceeded =>
            _versionOperation != null &&
            _versionOperation.Status == YooAsset.EOperationStatus.Succeeded &&
            _manifestOperation != null &&
            _manifestOperation.Status == YooAsset.EOperationStatus.Succeeded;
        public string Error
        {
            get
            {
                if (_versionOperation != null && _versionOperation.Status == YooAsset.EOperationStatus.Failed)
                    return _versionOperation.Error;
                if (_manifestOperation != null && _manifestOperation.Status == YooAsset.EOperationStatus.Failed)
                    return _manifestOperation.Error;
                return null;
            }
        }

        public void StartManifest()
        {
            if (_manifestOperation != null)
                return;
            if (!IsVersionSucceeded)
                throw new System.InvalidOperationException($"Package version request failed: {Error}");

            var options = new YooAsset.LoadPackageManifestOptions(PackageVersion, Timeout);
            _manifestOperation = _package.LoadPackageManifestAsync(options);
        }
    }
}
