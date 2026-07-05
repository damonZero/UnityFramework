using System;
using System.Reflection;

namespace Boot
{
    internal static class HybridClrReflection
    {
        public static MetadataLoadResult LoadMetadataForAotAssembly(byte[] bytes)
        {
            var runtimeApiType = Type.GetType("HybridCLR.RuntimeApi, HybridCLR.Runtime", throwOnError: false);
            if (runtimeApiType == null)
                return MetadataLoadResult.Failed("HybridCLR.RuntimeApi not found");

            var modeType = Type.GetType("HybridCLR.HomologousImageMode, HybridCLR.Runtime", throwOnError: false);
            if (modeType == null)
                return MetadataLoadResult.Failed("HomologousImageMode not found");

            var method = runtimeApiType.GetMethod(
                "LoadMetadataForAOTAssembly",
                BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return MetadataLoadResult.Failed("LoadMetadataForAOTAssembly not found");

            var mode = Enum.Parse(modeType, "SuperSet");
            var result = method.Invoke(null, new object[] { bytes, mode });
            var resultName = result?.ToString() ?? string.Empty;
            return string.Equals(resultName, "OK", StringComparison.Ordinal)
                ? MetadataLoadResult.Ok(resultName)
                : MetadataLoadResult.Failed(resultName);
        }

        public readonly struct MetadataLoadResult
        {
            private MetadataLoadResult(bool isOk, string resultName)
            {
                IsOk = isOk;
                ResultName = resultName;
            }

            public bool IsOk { get; }
            public string ResultName { get; }

            public static MetadataLoadResult Ok(string resultName) => new(true, resultName);
            public static MetadataLoadResult Failed(string resultName) => new(false, resultName);
        }
    }
}
