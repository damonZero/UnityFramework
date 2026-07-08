using System.IO;
using System.Linq;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 3 — 同步 DLL 到 YooAsset 源目录。
    /// 复用现有 SyncExistingOutputs（清理 → 复制 AOT → 复制热更 → Refresh → 确保收集器）。
    /// </summary>
    public static class StageSync
    {
        public static void Execute(BuildConfig config)
        {
            Debug.Log("[S3] Sync: Starting...");

            // 复用现有的完整同步方法
            Boot.Editor.HybridCLR.KJHybridClrBuildTools.SyncExistingOutputs();

            // 不变量：同步后目标目录含 DLL
            string dllDir = "Assets/GameRes/HotUpdate/Dlls";
            string metadataDir = "Assets/GameRes/HotUpdate/AotMetadata";

            if (!Directory.Exists(dllDir))
            {
                throw new BuildFailedException("S3_Sync",
                    $"DLL sync target directory not found: {dllDir}");
            }
            if (!Directory.Exists(metadataDir))
            {
                throw new BuildFailedException("S3_Sync",
                    $"Metadata sync target directory not found: {metadataDir}");
            }

            var dllFiles = Directory.GetFiles(dllDir, "*.dll.bytes");
            var metadataFiles = Directory.GetFiles(metadataDir, "*.dll.bytes");

            Debug.Log($"[S3] Synced DLLs: {dllFiles.Length}, Metadata: {metadataFiles.Length}");

            if (dllFiles.Length == 0)
            {
                throw new BuildFailedException("S3_Sync",
                    $"No .dll.bytes files found in {dllDir} after sync.");
            }

            Debug.Log("[S3] Sync: DONE");
        }
    }
}
