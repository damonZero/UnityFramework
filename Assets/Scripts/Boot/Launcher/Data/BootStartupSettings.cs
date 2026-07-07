using System;
using UnityEngine;

namespace Boot
{
    [Serializable]
    public sealed class BootStartupSettings
    {
        [SerializeField]
        private bool enableAssetUpdate = true;

        [SerializeField]
        private bool enableHotUpdate = true;

        [SerializeField]
        private bool skipHotUpdateInEditor = true;

        [SerializeField]
        private string streamingAssetsRoot = "HotUpdate";

        [SerializeField]
        private string assetDownloadTag;

        [SerializeField]
        private string startupTypeName = "Project.Bootstrap.ProjectStartup, Project";

        [SerializeField]
        private string startupMethodName = "Start";

        [SerializeField]
        private BootMetadataEntry[] aotMetadataAssemblies = Array.Empty<BootMetadataEntry>();

        [SerializeField]
        private BootAssemblyEntry[] hotUpdateAssemblies = Array.Empty<BootAssemblyEntry>();

        public bool EnableAssetUpdate => enableAssetUpdate;
        public bool EnableHotUpdate => enableHotUpdate;
        public bool SkipHotUpdateInEditor => skipHotUpdateInEditor;
        public string StreamingAssetsRoot => streamingAssetsRoot;
        public string AssetDownloadTag => assetDownloadTag;
        public string StartupTypeName => startupTypeName;
        public string StartupMethodName => startupMethodName;
        public BootMetadataEntry[] AotMetadataAssemblies => aotMetadataAssemblies ?? Array.Empty<BootMetadataEntry>();
        public BootAssemblyEntry[] HotUpdateAssemblies => hotUpdateAssemblies ?? Array.Empty<BootAssemblyEntry>();
    }
}
