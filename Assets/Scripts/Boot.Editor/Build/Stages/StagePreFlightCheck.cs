using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Framework.Asset;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 0 — 预检 Guard。把 90% 低级错误挡在编译前。
    /// </summary>
    public static class StagePreFlightCheck
    {
        public static void Execute(BuildConfig config)
        {
            Debug.Log("[S0] PreFlightCheck: Starting...");

            // 1. 校验 HybridCLR 运行时已安装
            var installer = new InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new BuildFailedException("S0_PreFlightCheck",
                    "HybridCLR runtime not installed. Run 'KJ/HybridCLR/Install HybridCLR Runtime' first.");
            }
            Debug.Log("[S0] HybridCLR runtime: installed");

            // 2. 自动切换平台（构建管线自包含环境准备）
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            if (activeTarget != config.Platform)
            {
                Debug.LogWarning($"[S0] Switching platform: {activeTarget} → {config.Platform}");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildPipeline.GetBuildTargetGroup(config.Platform), config.Platform))
                {
                    throw new BuildFailedException("S0_PreFlightCheck",
                        $"Failed to switch platform from {activeTarget} to {config.Platform}. " +
                        "Check that the platform module is installed in Unity Hub.");
                }
                activeTarget = EditorUserBuildSettings.activeBuildTarget;
            }
            Debug.Log($"[S0] Platform: {activeTarget}");

            // 3. 校验 Boot 场景存在且在 BuildSettings
            string bootScenePath = "Assets/GameRes/Scene/Boot/Main.unity";
            if (!File.Exists(bootScenePath))
            {
                throw new BuildFailedException("S0_PreFlightCheck",
                    $"Boot scene not found at: {bootScenePath}");
            }

            bool inBuildSettings = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == bootScenePath && scene.enabled)
                {
                    inBuildSettings = true;
                    break;
                }
            }
            if (!inBuildSettings)
            {
                throw new BuildFailedException("S0_PreFlightCheck",
                    "Boot scene not in BuildSettings. Run 'KJ/HybridCLR/Prepare Boot Scene' first.");
            }
            Debug.Log("[S0] Boot scene: in BuildSettings");

            // 4. 校验 AssetConfig 存在
            var assetConfig = Resources.Load<AssetConfig>("AssetConfig");
            if (assetConfig == null)
            {
                throw new BuildFailedException("S0_PreFlightCheck",
                    "AssetConfig not found at Resources/AssetConfig.asset");
            }
            Debug.Log($"[S0] AssetConfig: found (PackageName={assetConfig.PackageName})");

            // 5. 校验 IL2CPP
            var currentBackend = PlayerSettings.GetScriptingBackend(
                BuildPipeline.GetBuildTargetGroup(config.Platform));
            if (currentBackend != ScriptingImplementation.IL2CPP)
            {
                throw new BuildFailedException("S0_PreFlightCheck",
                    $"ScriptingBackend is {currentBackend}, must be IL2CPP for HybridCLR Player build.");
            }
            Debug.Log("[S0] ScriptingBackend: IL2CPP ✓");

            Debug.Log("[S0] PreFlightCheck: ALL CHECKS PASSED");
        }
    }
}
