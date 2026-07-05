namespace Framework.Asset
{
    public static class AssetRuntimeFactory
    {
        public static IAssetRuntime Create()
        {
            return new AssetRuntime();
        }
    }
}
