using UnityEngine;

namespace Framework.Asset
{
    [CreateAssetMenu(menuName = "Asset/Config")]
    public class AssetConfig : ScriptableObject
    {
        public PlayMode Mode = PlayMode.EditorSimulate;
        public string PackageName = "DefaultPackage";
        public string CdnBaseUrl = "http://127.0.0.1:8080/CDN";
        public int DownloadTimeout = 60;
        public int DownloadMaxConcurrency = 10;
        public int FailedRetryCount = 3;

        public enum PlayMode
        {
            EditorSimulate,
            Offline,
            Host
        }
    }
}
