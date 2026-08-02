using System;
using UnityEngine;

namespace Boot
{
    [Serializable]
    public sealed class BootAssemblyEntry
    {
        public BootAssemblyEntry()
        {
        }

        public BootAssemblyEntry(string assemblyName, string fileName, string assetPath = null)
        {
            this.assemblyName = assemblyName;
            this.fileName = fileName;
            this.assetPath = assetPath;
        }

        [SerializeField]
        private string assemblyName;

        [SerializeField]
        private string fileName;

        [SerializeField]
        private string assetPath;

        public string AssemblyName => assemblyName;
        public string FileName => fileName;
        public string AssetPath => assetPath;
    }
}
