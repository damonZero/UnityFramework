using System;
using System.Collections.Generic;
using System.IO;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Plan;
using HybridCLR.Editor;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Framework.Asset;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P1 Preflight — 构建前环境/平台/工具链/签名/场景全面预检。
    /// </summary>
    public class P1_PreflightStage : BuildStageBase
    {
        private const string BootScenePath = "Assets/GameRes/Scene/Boot/Main.unity";

        public override string Id => "P1.Preflight";
        public override string DisplayName => "Environment Preflight";
        public override int Order => 1;
        public override string Category => "Preflight";
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.AlwaysRun;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs { AlwaysRun = true };

        public override void Execute(BuildContext context)
        {
            var profile = context.Profile ?? throw new InvalidOperationException("BuildProfile is required");
            var buildTarget = profile.Platform;

            Debug.Log("[P1] Preflight: Starting checks...");

            // 1. HybridCLR 运行时已安装
            var installer = new InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.PreHybridCLRNotInstalled, Id,
                    "HybridCLR runtime not installed. Run KJ/HybridCLR/Maintenance/Install HybridCLR Runtime first."));
                throw new BuildFailedException(Id, "HybridCLR runtime not installed");
            }
            Debug.Log("[P1] ✓ HybridCLR runtime: installed");

            // 2. 平台切换
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            if (activeTarget != buildTarget)
            {
                Debug.LogWarning($"[P1] Switching platform: {activeTarget} → {buildTarget}");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget))
                {
                    context.AddIssue(BuildIssue.Error(
                        BuildErrorCodes.PrePlatformMismatch, Id,
                        $"Failed to switch platform to {buildTarget}. Check platform module in Unity Hub."));
                    throw new BuildFailedException(Id, $"Platform switch failed: {buildTarget}");
                }
            }
            Debug.Log($"[P1] ✓ Platform: {buildTarget}");

            // 3. Boot 场景存在且在 BuildSettings
            if (!File.Exists(BootScenePath))
            {
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.PreBootSceneMissing, Id,
                    $"Boot scene not found at {BootScenePath}"));
                throw new BuildFailedException(Id, "Boot scene missing");
            }

            bool inBuildSettings = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == BootScenePath && scene.enabled)
                { inBuildSettings = true; break; }
            }
            if (!inBuildSettings)
            {
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.PreBootSceneMissing, Id,
                    "Boot scene not in BuildSettings. Run KJ/HybridCLR/Maintenance/Prepare Boot Scene first."));
                throw new BuildFailedException(Id, "Boot scene not in BuildSettings");
            }
            Debug.Log("[P1] ✓ Boot scene: in BuildSettings");

            // 4. AssetConfig 存在
            var assetConfig = Resources.Load<AssetConfig>("AssetConfig");
            if (assetConfig == null)
            {
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.PreAssetConfigMissing, Id,
                    "AssetConfig not found at Resources/AssetConfig.asset"));
                throw new BuildFailedException(Id, "AssetConfig missing");
            }
            Debug.Log($"[P1] ✓ AssetConfig: {assetConfig.PackageName}");

            // 5. IL2CPP 强制
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var currentBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            if (currentBackend != ScriptingImplementation.IL2CPP)
                Debug.LogWarning($"[P1] ScriptingBackend is {currentBackend}; P6 will switch it transactionally to IL2CPP.");
            else
                Debug.Log("[P1] ✓ ScriptingBackend: IL2CPP");

            // 6. Android 平台检查
            if (buildTarget == BuildTarget.Android)
            {
                ValidateAndroidTools(context);
            }

            // 7. Formal 环境强制校验
            if (profile != null && profile.RequireSigning)
            {
                ValidateFormalSettings(context, profile, targetGroup);
            }

            Debug.Log("[P1] Preflight: ALL CHECKS PASSED");
        }

        private void ValidateFormalSettings(BuildContext context, BuildProfile profile,
            BuildTargetGroup targetGroup)
        {
            // Profile 级别的校验已在 BuildProfileValidator 中完成
            // 这里补充运行时校验

            // PlayerSettings 泄漏检查
            bool devBuild = EditorUserBuildSettings.development;
            bool scriptDebug = EditorUserBuildSettings.allowDebugging;

            if (devBuild)
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.FormalDebugBuildLeak, Id,
                    "Development Build is enabled — must be false for Formal/Audit",
                    null, "Disable Development Build in BuildProfile"));

            if (scriptDebug)
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.FormalScriptDebuggingLeak, Id,
                    "Script Debugging is enabled — must be false for Formal/Audit"));

            // Scripting Define 检查
            string defines = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(targetGroup));
            if (defines.Contains("KJ_GM_ENABLED") || defines.Contains("KJ_DEBUG_UI"))
                context.AddIssue(BuildIssue.Warning(
                    BuildErrorCodes.FormalDefineLeak, Id,
                    "Debug defines detected in Formal environment",
                    null, "Remove KJ_GM_ENABLED / KJ_DEBUG_UI from Scripting Define Symbols"));

            Debug.Log("[P1] ✓ Formal settings validated");
        }

        private void ValidateAndroidTools(BuildContext context)
        {
            string androidPlayer = Path.Combine(
                Path.GetDirectoryName(EditorApplication.applicationPath),
                "Data", "PlaybackEngines", "AndroidPlayer");

            if (!Directory.Exists(androidPlayer))
            {
                context.AddIssue(Framework.BuildPipeline.Diagnostics.BuildIssue.Error(
                    Framework.BuildPipeline.Diagnostics.BuildErrorCodes.PreAndroidModuleMissing, Id,
                    "Android Build Support module not installed. " +
                    "Install via Unity Hub → Installs → Add Modules → Android Build Support."));
                throw new BuildFailedException(Id, "Android module missing");
            }

            Debug.Log("[P1] ✓ Android module present");
        }
    }
}
