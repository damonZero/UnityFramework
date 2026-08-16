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

        /// <summary>
        /// 软释放：释放已加载句柄与内部缓存，但保留底层 YooAsset package 与 IsReady 状态。
        /// 用于软重启——Core scope 销毁时只释放资源占用，不拆 YooAsset，重建后仍可加载。
        /// </summary>
        void Release();

        /// <summary>
        /// 硬销毁：Release() + YooAssets.Destroy()。用于进程退出 / 整机重启，不可再加载。
        /// </summary>
        void Destroy();
    }
}
