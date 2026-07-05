using System;
using UnityEngine;

namespace Boot
{
    [Serializable]
    public sealed class BootMetadataEntry
    {
        public BootMetadataEntry()
        {
        }

        public BootMetadataEntry(string assemblyName, string fileName, string resourcesPath = null, string assetPath = null)
        {
            this.assemblyName = assemblyName;
            this.fileName = fileName;
            this.resourcesPath = resourcesPath;
            this.assetPath = assetPath;
        }

        [SerializeField]
        private string assemblyName;

        [SerializeField]
        private string fileName;

        [SerializeField]
        private string resourcesPath;

        [SerializeField]
        private string assetPath;

        public string AssemblyName => assemblyName;
        public string FileName => fileName;
        public string ResourcesPath => resourcesPath;
        public string AssetPath => assetPath;
    }
}
