using YooAsset;

namespace Framework.Asset
{
    public interface IAssetRuntime
    {
        AssetConfig Config { get; }
        bool IsReady { get; }
        string LastError { get; }
        AssetInitializeHandle BeginInitialize(AssetConfig config);
        bool Initialize(AssetConfig config);
        AssetDownloadHandle CreateDownloader(string tag = null);
        AssetDownloadHandle CreateDownloader(string[] tags);
        AssetUpdateManifestHandle UpdateManifest();
        byte[] LoadRawBytes(string path);
        void WrapFromExistingPackage(AssetConfig config, ResourcePackage existingPackage);
        void Shutdown();
    }
}
