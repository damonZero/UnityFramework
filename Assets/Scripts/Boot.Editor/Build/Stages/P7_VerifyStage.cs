using System;
using System.Collections.Generic;
using System.IO;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P7 Static Verify — 产物静态校验（Player、YooAsset 包、DLL/元数据、Formal 泄露）。
    /// </summary>
    public class P7_VerifyStage : BuildStageBase
    {
        public override string Id => "P7.Verify";
        public override string DisplayName => "Static Artifact Verification";
        public override int Order => 7;
        public override string Category => "Verify";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P6.Player" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.AlwaysRun;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs { AlwaysRun = true };

        public override void Execute(BuildContext context)
        {
            var profile = context.Profile;
            var buildTarget = profile?.Platform ?? BuildTarget.StandaloneWindows64;
            Debug.Log("[P7] Verify: Checking artifacts...");

            // 1. Player 文件存在
            string playerPath = profile?.GetPlayerPath()
                ?? context.Config?.GetPlayerPath()
                ?? $"Build/{buildTarget}/KJ.exe";

            if (File.Exists(playerPath))
            {
                var fi = new FileInfo(playerPath);
                if (fi.Length == 0)
                    throw new BuildFailedException(Id, $"Player file is empty: {playerPath}");
                Debug.Log($"[P7] ✓ Player: {playerPath} ({fi.Length / 1024 / 1024} MB)");
            }
            else if (Directory.Exists(playerPath))
            {
                int fileCount = Directory.GetFiles(playerPath, "*", SearchOption.AllDirectories).Length;
                Debug.Log($"[P7] ✓ Player (Export Project): {playerPath} ({fileCount} files)");
            }
            else
            {
                throw new BuildFailedException(Id, $"Player not found: {playerPath}");
            }

            // 2. YooAsset 包完整性
            string packageName = profile?.PackageName ?? "DefaultPackage";
            string streamingAssetsPackage = $"Assets/StreamingAssets/{packageName}";
            if (Directory.Exists(streamingAssetsPackage))
            {
                string versionFile = $"{streamingAssetsPackage}/{packageName}.version";
                if (!File.Exists(versionFile))
                    throw new BuildFailedException(Id,
                        $"Package version file missing: {versionFile}");
                Debug.Log($"[P7] ✓ StreamingAssets package: {streamingAssetsPackage}");
            }
            else
            {
                Debug.LogWarning($"[P7] StreamingAssets package not found: {streamingAssetsPackage}");
            }

            // 3. DLL / AOT metadata 数量
            string dllDir = "Assets/GameRes/HotUpdate/Dlls";
            string metadataDir = "Assets/GameRes/HotUpdate/AotMetadata";
            int dllCount = Directory.Exists(dllDir)
                ? Directory.GetFiles(dllDir, "*.bytes").Length : 0;
            int metadataCount = Directory.Exists(metadataDir)
                ? Directory.GetFiles(metadataDir, "*.bytes").Length : 0;
            Debug.Log($"[P7] HotUpdate DLLs: {dllCount}, AOT Metadata: {metadataCount}");

            if (dllCount == 0)
                throw new BuildFailedException(Id,
                    "No hot-update DLLs found in Assets/GameRes/HotUpdate/Dlls");
            if (metadataCount == 0)
                throw new BuildFailedException(Id,
                    "No AOT metadata DLLs found in Assets/GameRes/HotUpdate/AotMetadata");

            // 4. Formal 泄露检查
            if (profile != null && profile.RequireSigning)
            {
                FormalLeakageVerifier.Verify(context, buildTarget);
            }

            Debug.Log("[P7] Verify: ALL CHECKS PASSED");
        }
    }
}
