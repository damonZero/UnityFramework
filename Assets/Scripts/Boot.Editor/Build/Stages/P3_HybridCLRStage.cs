using System;
using System.Collections.Generic;
using System.IO;
using Boot.Editor.Build.Telemetry;
using Framework.BuildPipeline.Plan;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P3 HybridCLR — 编译热更 DLL + AOT metadata + 同步 .dll.bytes。
    /// </summary>
    public class P3_HybridCLRStage : BuildStageBase
    {
        private const string HotUpdateDllSource = "HybridCLRData/HotUpdateDlls";
        private const string AOTMetadataSource = "HybridCLRData/AssembliesPostIl2CppStrip";
        private const string DllAssetFolder = "Assets/GameRes/HotUpdate/Dlls";
        private const string MetadataAssetFolder = "Assets/GameRes/HotUpdate/AotMetadata";

        public override string Id => "P3.HybridCLR";
        public override string DisplayName => "Compile HotUpdate DLLs + AOT Metadata";
        public override int Order => 3;
        public override string Category => "HybridCLR";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P2.Generate" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.ProducesArtifacts;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs()
                .WithSourcePaths(
                    "Assets/Scripts/Boot/",
                    "Assets/Scripts/Core/",
                    "Assets/Scripts/General/",
                    "Assets/Scripts/Project/",
                    "Assets/Framework/")
                .WithDependsOn("P2.Generate");

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
            => new BuildStageOutputs()
                .WithRequiredDirectory(HotUpdateDllSource)
                .WithRequiredDirectory(AOTMetadataSource)
                .WithRequiredDirectory(DllAssetFolder)
                .WithRequiredDirectory(MetadataAssetFolder);

        public override void Execute(BuildContext context)
        {
            Debug.Log("[P3] HybridCLR: Syncing DLLs to YooAsset source...");

            var profile = context.Profile ?? throw new InvalidOperationException("BuildProfile is required");
            var buildTarget = profile.Platform;
            string targetName = GetHybridCLRTargetName(buildTarget);

            // 1. 清理旧产物
            string hotUpdatePath = Path.Combine(HotUpdateDllSource, targetName);
            string aotMetaPath = Path.Combine(AOTMetadataSource, targetName);
            BuildTelemetry.Measure("P3.CleanOutputs", "HybridCLR", () =>
            {
                CleanDirectory(hotUpdatePath);
                CleanDirectory(aotMetaPath);
            });

            // 2. 编译热更 DLL（P2 PrebuildCommand.GenerateAll 已编译，此处补充以确保产物存在）
            // 注意: P2 中 CompileDllCommand.CompileDll 使用的是 EditorUserBuildSettings.development，
            // 这里使用 profile.DevelopmentBuild 确保一致性
            BuildTelemetry.Measure(
                "P3.CompileHotUpdateDlls",
                "HybridCLR",
                () => CompileDllCommand.CompileDll(buildTarget, profile.DevelopmentBuild));

            // 3. 同步到 YooAsset 源目录
            BuildTelemetry.Measure(
                "P3.SyncHotUpdateAssets",
                "HybridCLR",
                SyncToAssetDirectory);

            BuildTelemetry.Measure(
                "P3.RefreshAssetDatabase",
                "UnityEditor",
                AssetDatabase.Refresh);

            Debug.Log("[P3] HybridCLR: DONE");
        }

        private void CleanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (string f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    File.Delete(f);
                Debug.Log($"[P3] Cleaned: {path}");
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }

        private void SyncToAssetDirectory()
        {
            // 复用现有同步逻辑——直接调用以下方法：
            // 这里实现内联同步以确保兼容

            // 复制热更 DLL → Assets/GameRes/HotUpdate/Dlls/
            CopyAllDlls(HotUpdateDllSource, DllAssetFolder, "*.dll", ".dll.bytes");
            // 复制 AOT metadata → Assets/GameRes/HotUpdate/AotMetadata/
            CopyAllDlls(AOTMetadataSource, MetadataAssetFolder, "*.dll", ".dll.bytes");

            // 删除过期的 .bytes 文件
            CleanObsoleteBytes(DllAssetFolder);
            CleanObsoleteBytes(MetadataAssetFolder);
        }

        private void CopyAllDlls(string sourceRoot, string destDir, string pattern, string suffix)
        {
            if (!Directory.Exists(sourceRoot)) return;
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            var sourceDlls = Directory.GetFiles(sourceRoot, pattern, SearchOption.AllDirectories);
            foreach (string src in sourceDlls)
            {
                string destFileName = Path.GetFileName(src) + suffix;
                string dest = Path.Combine(destDir, destFileName);
                File.Copy(src, dest, true);
            }
            Debug.Log($"[P3] Copied {sourceDlls.Length} files to {destDir}");
        }

        private void CleanObsoleteBytes(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (string f in Directory.GetFiles(dir, "*.dll.bytes"))
            {
                // 检查对应的源 DLL 是否仍存在
                string baseName = Path.GetFileNameWithoutExtension(
                    Path.GetFileNameWithoutExtension(f)); // "Core" from "Core.dll.bytes"
                bool found = false;
                foreach (string srcDir in new[] { HotUpdateDllSource, AOTMetadataSource })
                {
                    if (!Directory.Exists(srcDir)) continue;
                    foreach (string srcDll in Directory.GetFiles(srcDir, "*.dll", SearchOption.AllDirectories))
                    {
                        if (Path.GetFileNameWithoutExtension(srcDll) == baseName)
                        { found = true; break; }
                    }
                    if (found) break;
                }
                if (!found)
                {
                    File.Delete(f);
                    Debug.Log($"[P3] Removed obsolete: {f}");
                }
            }
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            Debug.Log("[P3] ✓ DLL sync verified");
        }

        private static string GetHybridCLRTargetName(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneWindows64 => "StandaloneWindows64",
            BuildTarget.Android => "Android",
            BuildTarget.iOS => "iOS",
            _ => "StandaloneWindows64",
        };
    }
}
