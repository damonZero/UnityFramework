using System;
using System.Collections.Generic;
using System.IO;
using Boot.Editor.HybridCLR;
using Boot.Editor.Build.Telemetry;
using Framework.BuildPipeline.Plan;
using HybridCLR.Editor;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P3 HybridCLR — 编译热更 DLL + AOT metadata + 同步 .dll.bytes。
    /// </summary>
    public class P3_HybridCLRStage : BuildStageBase
    {
        private const string DllAssetFolder = "Assets/GameRes/HotUpdate/Dlls";
        private const string MetadataAssetFolder = "Assets/GameRes/HotUpdate/AotMetadata";

        public override string Id => "P3.HybridCLR";
        public override string DisplayName => "Sync HybridCLR DLLs + AOT Metadata";
        public override int Version => 4;
        public override int Order => 3;
        public override string Category => "HybridCLR";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P2.Generate" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.ProducesArtifacts;

        public override BuildStageInputs GetInputs(BuildContext context)
        {
            var inputs = new BuildStageInputs()
                .WithSourcePaths(
                    SettingsUtil.GetHotUpdateDllsOutputDirByTarget(context.Profile.Platform),
                    SettingsUtil.GetAssembliesPostIl2CppStripDir(context.Profile.Platform),
                    "ProjectSettings/HybridCLRSettings.asset")
                .WithDependsOn("P2.Generate");
            inputs.ProfileHash = context.Profile.ComputeHybridClrProfileHash();
            return inputs;
        }

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
        {
            var outputs = new BuildStageOutputs()
                .WithRequiredDirectory(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(context.Profile.Platform))
                .WithRequiredDirectory(SettingsUtil.GetAssembliesPostIl2CppStripDir(context.Profile.Platform))
                .WithRequiredDirectory(DllAssetFolder)
                .WithRequiredDirectory(MetadataAssetFolder);

            foreach (string assemblyName in SettingsUtil.HybridCLRSettings.patchAOTAssemblies ?? Array.Empty<string>())
            {
                outputs.WithRequiredFile(
                    Path.Combine(MetadataAssetFolder, $"{assemblyName}.dll.bytes"));
            }

            return outputs;
        }

        public override void Execute(BuildContext context)
        {
            BuildLogger.Info("[P3] HybridCLR: Syncing DLLs to YooAsset source...");

            // P2 owns compilation and the expensive generated-code validation. P3 only copies
            // the verified outputs into the YooAsset source folders.
            BuildTelemetry.Measure(
                "P3.SyncHotUpdateAssets",
                "HybridCLR",
                KJHybridClrBuildTools.SyncExistingOutputs);

            BuildTelemetry.Measure(
                "P3.RefreshAssetDatabase",
                "UnityEditor",
                AssetDatabase.Refresh);

            BuildLogger.Info("[P3] HybridCLR: DONE");
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            BuildLogger.Info("[P3] ✓ DLL sync verified");
        }

    }
}
