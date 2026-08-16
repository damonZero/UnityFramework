using YooAsset;

namespace Framework.Asset
{
    public static class AssetRuntimeFactory
    {
        public static IAssetRuntime Create()
        {
            return new AssetRuntime();
        }

        /// <summary>
        /// 用 AOT 侧已创建的 YooAsset package 包装出热更层 AssetRuntime。
        /// 放在具体工厂（而非 IAssetRuntime 稳定接口）上，避免 YooAsset.ResourcePackage
        /// 类型泄漏到稳定接口——上层 Core 依赖 IAssetRuntime 而不引用 YooAsset。
        /// </summary>
        public static IAssetRuntime CreateFromPackage(AssetConfig config, ResourcePackage existingPackage)
        {
            var runtime = new AssetRuntime();
            runtime.WrapFromExistingPackage(config, existingPackage);
            return runtime;
        }
    }
}
