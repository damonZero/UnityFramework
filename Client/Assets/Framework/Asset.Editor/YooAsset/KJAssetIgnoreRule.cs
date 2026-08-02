using System;
using YooAsset.Editor;

namespace Framework.Asset.Editor.YooAsset
{
    public sealed class KJAssetIgnoreRule : IAssetIgnoreRule
    {
        private static readonly NormalIgnoreRule DefaultRule = new NormalIgnoreRule();

        public bool IsIgnoreAsset(EditorAssetInfo assetInfo)
        {
            if (assetInfo == null || string.IsNullOrEmpty(assetInfo.AssetPath))
                return true;

            return HasPrivatePathSegment(assetInfo.AssetPath) || DefaultRule.IsIgnoreAsset(assetInfo);
        }

        private static bool HasPrivatePathSegment(string assetPath)
        {
            var segments = assetPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                if (segment.StartsWith("_", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
