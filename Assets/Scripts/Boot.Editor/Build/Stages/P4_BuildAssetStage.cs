using System;
using System.Collections.Generic;
using System.IO;
using Boot.Editor.Build.Telemetry;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P4 Build Asset — YooAsset 生产构建（输出到 StreamingAssets）。
    /// </summary>
    public class P4_BuildAssetStage : BuildStageBase
    {
        public override string Id => "P4.Assets";
        public override string DisplayName => "Build YooAsset Bundles";
        public override int Order => 4;
        public override string Category => "YooAsset";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P3.HybridCLR" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.ProducesArtifacts;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs()
                .WithSourcePaths("Assets/GameRes/HotUpdate/")
                .WithDependsOn("P3.HybridCLR");

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
        {
            string packageName = context.Profile.PackageName;
            string streamingAssetsRoot = $"Assets/StreamingAssets/{packageName}";
            return new BuildStageOutputs()
                .WithRequiredDirectory(streamingAssetsRoot)
                .WithRequiredFile($"{streamingAssetsRoot}/{packageName}.version");
        }

        public override void Execute(BuildContext context)
        {
            var profile = context.Profile ?? throw new InvalidOperationException("BuildProfile is required");
            string packageName = profile.PackageName;
            var buildTarget = profile.Platform;

            Debug.Log($"[P4] BuildAsset: Building YooAsset package '{packageName}' for {buildTarget}");

            // 1. 清理旧 StreamingAssets 包
            string streamingAssetsPath = $"Assets/StreamingAssets/{packageName}";
            BuildTelemetry.Measure("P4.CleanStreamingAssets", "YooAsset", () =>
            {
                if (Directory.Exists(streamingAssetsPath))
                {
                    Directory.Delete(streamingAssetsPath, true);
                    File.Delete($"{streamingAssetsPath}.meta");
                }
            });

            // 2. YooAsset 生产构建
            var buildParams = new ScriptableBuildParameters
            {
                BuildOutputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot(),
                BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
                BuildBundleType = (int)EBundleType.AssetBundle,
                BuildTarget = buildTarget,
                PackageName = packageName,
                PackageVersion = profile.VersionName,
                CompressOption = ECompressOption.LZ4,
                DisableWriteTypeTree = true,
                FileNameStyle = EFileNameStyle.HashName,
                VerifyBuildingResult = true,
                BundledCopyOption = EBundledCopyOption.ClearAndCopyAll,
                EnableSharePackRule = true,
                SingleReferencedPackAlone = true,
                ClearBuildCacheFiles = true,
            };

            var pipeline = new ScriptableBuildPipeline();
            var buildResult = BuildTelemetry.Measure(
                "P4.BuildYooAssetPackage",
                "YooAsset",
                () => pipeline.Run(buildParams, true));
            BuildTelemetry.Measure(
                "P4.RefreshAssetDatabase",
                "UnityEditor",
                AssetDatabase.Refresh);

            if (!buildResult.Success)
            {
                throw new BuildFailedException(Id,
                    $"YooAsset build failed: {buildResult.ErrorInfo}");
            }

            Debug.Log("[P4] BuildAsset: DONE");
        }

        public override void Verify(BuildContext context)
        {
            string packageName = context.Profile.PackageName;
            string streamingAssetsPath = $"Assets/StreamingAssets/{packageName}";
            string versionFile = $"{streamingAssetsPath}/{packageName}.version";

            if (!Directory.Exists(streamingAssetsPath))
                throw new InvalidOperationException($"StreamingAssets package not found: {streamingAssetsPath}");
            if (!File.Exists(versionFile))
                throw new InvalidOperationException($"Package version file not found: {versionFile}");

            Debug.Log("[P4] ✓ YooAsset package verified");
        }
    }
}
