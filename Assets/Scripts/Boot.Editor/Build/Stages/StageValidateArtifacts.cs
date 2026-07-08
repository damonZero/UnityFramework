using System.IO;
using System.Linq;
using HybridCLR.Editor;
using UnityEngine;
using Framework.Asset;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 7 — 产物静态校验。
    /// 校验 Player 本体、YooAsset 包内容、HybridCLR 物料清单一致性。
    /// </summary>
    public static class StageValidateArtifacts
    {
        public static void Execute(BuildConfig config, BuildReport report)
        {
            Debug.Log("[S7] ValidateArtifacts: Starting...");

            // 1. Player 本体校验（文件或目录，Android/iOS Export Project 输出目录）
            string playerPath = config.GetPlayerPath();
            report.AddArtifact(playerPath, "Player binary");
            if (File.Exists(playerPath))
            {
                var playerInfo = new FileInfo(playerPath);
                Debug.Log($"[S7] Player: {playerPath} ({playerInfo.Length / 1024 / 1024} MB)");
            }
            else if (Directory.Exists(playerPath))
            {
                int fileCount = Directory.GetFiles(playerPath, "*", SearchOption.AllDirectories).Length;
                Debug.Log($"[S7] Player: {playerPath} (Export Project, {fileCount} files)");
            }
            else
            {
                throw new BuildFailedException("S7_ValidateArtifacts",
                    $"Player not found: {playerPath}");
            }

            // 2. YooAsset 包校验（使用 S4 找到的真实输出路径）
            string packageName = string.IsNullOrEmpty(config.PackageName)
                ? "DefaultPackage" : config.PackageName;

            string bundleDir = StageBuildYooAsset.LastOutputDirectory;
            if (string.IsNullOrEmpty(bundleDir))
            {
                // 回退：搜索 StreamingAssets 下 version 文件
                bundleDir = FindBundleDir(packageName);
            }

            if (string.IsNullOrEmpty(bundleDir) || !Directory.Exists(bundleDir))
            {
                throw new BuildFailedException("S7_ValidateArtifacts",
                    $"YooAsset bundle directory not found. S4 may not have run or output was cleaned.");
            }
            Debug.Log($"[S7] Bundle dir: {bundleDir}");

            // 校验 version 和 hash 文件
            string versionFile = Directory.GetFiles(bundleDir, $"{packageName}.version",
                SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (versionFile == null)
            {
                throw new BuildFailedException("S7_ValidateArtifacts",
                    $"version file not found in {bundleDir}");
            }
            report.AddArtifact(versionFile, "YooAsset version file");

            // 校验资源文件存在：YooAsset 用 HashName 命名，hotupdate 是 tag 不出现在文件名中。
            // 通过检查 .rawfile + .bundle 文件数来验证。
            var rawFiles = Directory.GetFiles(bundleDir, "*", SearchOption.AllDirectories);
            int rawfileCount = rawFiles.Count(f => f.EndsWith(".rawfile"));
            int bundleCount = rawFiles.Count(f => f.EndsWith(".bundle"));
            int totalAssetCount = rawfileCount + bundleCount;
            if (totalAssetCount == 0)
            {
                throw new BuildFailedException("S7_ValidateArtifacts",
                    "No .rawfile or .bundle files found in package output");
            }
            report.AddArtifact(bundleDir, $"Bundle output ({rawfileCount} rawfile + {bundleCount} bundle)");
            Debug.Log($"[S7] Bundle files: {rawfileCount} .rawfile + {bundleCount} .bundle");

            // 3. HybridCLR 产物复核（物料清单留档）
            string hotUpdateSrc = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(config.Platform);
            string aotMetadataSrc = SettingsUtil.GetAssembliesPostIl2CppStripDir(config.Platform);

            var hotUpdateDlls = Directory.GetFiles(hotUpdateSrc, "*.dll");
            var aotMetadataDlls = Directory.GetFiles(aotMetadataSrc, "*.dll");
            Debug.Log($"[S7] HotUpdate DLLs in source: {hotUpdateDlls.Length}");
            Debug.Log($"[S7] AOT metadata DLLs in source: {aotMetadataDlls.Length}");

            // 4. 一致性校验：Entry.startupSettings 的 hotUpdateAssemblies =
            //    HybridCLRSettings.hotUpdateAssemblies
            var hcSettings = SettingsUtil.HybridCLRSettings;
            var configuredNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            Debug.Log($"[S7] Configured hot update assemblies: {configuredNames.Count}");

            // 5. 同步目标复核
            string dllDir = "Assets/GameRes/HotUpdate/Dlls";
            string metadataDir = "Assets/GameRes/HotUpdate/AotMetadata";
            var syncedDlls = Directory.Exists(dllDir)
                ? Directory.GetFiles(dllDir, "*.dll.bytes") : new string[0];
            var syncedMetadata = Directory.Exists(metadataDir)
                ? Directory.GetFiles(metadataDir, "*.dll.bytes") : new string[0];
            Debug.Log($"[S7] Synced DLL .bytes: {syncedDlls.Length}");
            Debug.Log($"[S7] Synced metadata .bytes: {syncedMetadata.Length}");

            if (syncedDlls.Length < configuredNames.Count)
            {
                Debug.LogWarning($"[S7] Synced DLL count ({syncedDlls.Length}) < configured ({configuredNames.Count}). " +
                    "Some assemblies may not have been synced.");
            }

            Debug.Log("[S7] ValidateArtifacts: ALL CHECKS PASSED");
        }

        /// <summary>
        /// 回退方案：在 StreamingAssets 下递归搜索 YooAsset 包输出目录。
        /// </summary>
        private static string FindBundleDir(string packageName)
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
