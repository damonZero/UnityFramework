using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 4 — YooAsset 正式包构建（Scriptable Build Pipeline）。
    /// 输出到 Assets/StreamingAssets/（Offline 模式 builtin 文件系统）。
    /// </summary>
    public static class StageBuildYooAsset
    {
        /// <summary>
        /// S4 构建完成后，供后续 Stage 使用的 StreamingAssets 中 YooAsset 包输出目录。
        /// </summary>
        public static string LastOutputDirectory { get; private set; }

        public static void Execute(BuildConfig config)
        {
            Debug.Log("[S4] BuildYooAsset: Starting...");

            string packageName = string.IsNullOrEmpty(config.PackageName) ? "DefaultPackage" : config.PackageName;
            string version = string.IsNullOrEmpty(config.Version) ? "1.0.0" : config.Version;

            // 前置清理：清空 StreamingAssets 旧产物
            string streamingAssetsRoot = Application.streamingAssetsPath;
            string buildTargetDir = Path.Combine(streamingAssetsRoot, packageName);
            if (Directory.Exists(buildTargetDir))
            {
                Directory.Delete(buildTargetDir, true);
                Debug.Log($"[S4] Cleaned old bundle output: {buildTargetDir}");
            }
            AssetDatabase.Refresh();

            // 构建参数
            var buildParams = new ScriptableBuildParameters
            {
                BuildOutputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot(),
                BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
                BuildBundleType = (int)EBundleType.AssetBundle,
                BuildTarget = config.Platform,
                PackageName = packageName,
                PackageVersion = version,

                // 压缩与优化
                CompressOption = ECompressOption.LZ4,
                DisableWriteTypeTree = true,          // IL2CPP 不需要 TypeTree
                FileNameStyle = EFileNameStyle.HashName,
                VerifyBuildingResult = true,

                // Offline: 拷贝到 StreamingAssets
                BundledCopyOption = EBundledCopyOption.ClearAndCopyAll,

                // 共享资源打包
                EnableSharePackRule = true,
                SingleReferencedPackAlone = true,

                // 清理缓存确保干净构建
                ClearBuildCacheFiles = true,
            };

            Debug.Log($"[S4] BuildParams: OutputRoot={buildParams.BuildOutputRoot}");
            Debug.Log($"[S4] BuildParams: BundledRoot={buildParams.BundledFileRoot}");
            Debug.Log($"[S4] BuildParams: Package={packageName}, Version={version}");
            Debug.Log($"[S4] BuildParams: Target={config.Platform}, Compress=LZ4");

            // 执行生产构建
            var pipeline = new ScriptableBuildPipeline();
            var result = pipeline.Run(buildParams, true);

            if (!result.Success)
            {
                throw new BuildFailedException("S4_BuildYooAsset",
                    $"YooAsset build failed: {result.ErrorInfo}");
            }

            Debug.Log($"[S4] YooAsset build SUCCESS: {result.OutputPackageDirectory}");

            // 校验产物：递归搜索 YooAsset 在 StreamingAssets 中的实际输出位置
            // （YooAsset 可能输出到 StreamingAssets/yoo/PackageName/ 等嵌套路径）
            string outputDir = FindBundleOutputDir(packageName);
            if (outputDir == null)
            {
                throw new BuildFailedException("S4_BuildYooAsset",
                    $"Cannot find YooAsset output for package '{packageName}' under StreamingAssets. " +
                    $"Searched for '{packageName}.version' file.");
            }
            LastOutputDirectory = outputDir;
            Debug.Log($"[S4] Found bundle output: {outputDir}");

            string versionFile = Path.Combine(outputDir, $"{packageName}.version");

            // 校验产物：HotUpdate 组使用 PackRawFile 规则，输出 .rawfile 文件。
            var outputFiles = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories);
            long totalSize = outputFiles.Sum(f => new FileInfo(f).Length);
            Debug.Log($"[S4] Bundle output: {outputFiles.Length} files, total size: {totalSize} bytes");

            // 至少有 version + hash + 若干 rawfile
            if (outputFiles.Length < 3)
            {
                throw new BuildFailedException("S4_BuildYooAsset",
                    $"Bundle output too few files ({outputFiles.Length}). Expected at least .version + .hash + bundles.");
            }

            // 确保有 rawfile 或 bundle 资源文件
            int rawfileCount = outputFiles.Count(f => f.EndsWith(".rawfile"));
            int bundleCount = outputFiles.Count(f => f.EndsWith(".bundle"));
            int totalBundleCount = rawfileCount + bundleCount;
            if (totalBundleCount == 0)
            {
                throw new BuildFailedException("S4_BuildYooAsset",
                    "No .rawfile or .bundle files in output. HotUpdate group may have no assets to collect.");
            }
            Debug.Log($"[S4] Found {rawfileCount} .rawfile + {bundleCount} .bundle files");

            // AssetDatabase Refresh 以确保 Unity 看到新的 StreamingAssets
            AssetDatabase.Refresh();
            Debug.Log("[S4] AssetDatabase refreshed after bundle build");

            Debug.Log("[S4] BuildYooAsset: DONE");
        }

        /// <summary>
        /// 在 StreamingAssets 下递归搜索 YooAsset 包的实际输出目录。
        /// 通过查找 "{packageName}.version" 文件定位，兼容 YooAsset 任意嵌套层级。
        /// </summary>
        private static string FindBundleOutputDir(string packageName)
        {
            string streamingAssets = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssets))
                return null;

            string searchPattern = $"{packageName}.version";
            var versionFiles = Directory.GetFiles(streamingAssets, searchPattern,
                SearchOption.AllDirectories);
            if (versionFiles.Length > 0)
                return Path.GetDirectoryName(versionFiles[0]);

            return null;
        }
    }
}
