using System;
using System.Collections.Generic;
using System.IO;
using Boot.Editor.Build.Telemetry;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P6 BuildPlayer — BuildPipeline.BuildPlayer + Android Gradle 后处理。
    /// </summary>
    public class P6_BuildPlayerStage : BuildStageBase
    {
        public override string Id => "P6.Player";
        public override string DisplayName => "Build Player (IL2CPP)";
        public override int Version => 2;
        public override int Order => 6;
        public override string Category => "Player";
        public override IReadOnlyList<string> DependsOn { get; } = new[]
            { "P4.Assets", "P5.ApplyConfig" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.ProducesArtifacts;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs()
                .WithDependsOn("P4.Assets", "P5.ApplyConfig");

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
        {
            string playerPath = context.Profile.GetPlayerPath();
            return new BuildStageOutputs()
                .WithRequiredFile(playerPath);
        }

        public override void Execute(BuildContext context)
        {
            var profile = context.Profile ?? throw new InvalidOperationException("BuildProfile is required");
            var buildTarget = profile.Platform;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            Debug.Log($"[P6] BuildPlayer: Building for {buildTarget}...");

            // 1. 强制 IL2CPP
            var currentBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            if (currentBackend != ScriptingImplementation.IL2CPP)
            {
                Debug.Log($"[P6] Switching ScriptingBackend to IL2CPP");
                context.Transaction.SnapshotScriptingBackend(targetGroup);
                PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.IL2CPP);
            }

            // 2. Development Build
            context.Transaction.SnapshotBoolSetting(
                "EditorUserBuildSettings.development",
                v => EditorUserBuildSettings.development = v,
                () => EditorUserBuildSettings.development);
            context.Transaction.SnapshotBoolSetting(
                "EditorUserBuildSettings.allowDebugging",
                v => EditorUserBuildSettings.allowDebugging = v,
                () => EditorUserBuildSettings.allowDebugging);
            bool isDev = profile.DevelopmentBuild;
            EditorUserBuildSettings.development = isDev;
            EditorUserBuildSettings.allowDebugging = isDev;

            // 3. Android 平台预检
            if (buildTarget == BuildTarget.Android)
            {
                string androidPlayer = Path.Combine(
                    Path.GetDirectoryName(EditorApplication.applicationPath),
                    "Data", "PlaybackEngines", "AndroidPlayer");
                if (!Directory.Exists(androidPlayer))
                    throw new BuildFailedException(Id, "Android Build Support module not installed");

                context.Transaction.SnapshotBoolSetting(
                    "EditorUserBuildSettings.exportAsGoogleAndroidProject",
                    v => EditorUserBuildSettings.exportAsGoogleAndroidProject = v,
                    () => EditorUserBuildSettings.exportAsGoogleAndroidProject);
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            }

            // 4. 刷新资源
            BuildTelemetry.Measure(
                "P6.RefreshAssetDatabase",
                "UnityEditor",
                AssetDatabase.Refresh);

            // 5. 构建 Player
            string playerOutputPath = profile.GetPlayerPath();
            string outputDir = Path.GetDirectoryName(playerOutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            if (Directory.Exists(playerOutputPath))
                Directory.Delete(playerOutputPath, true);

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = playerOutputPath,
                target = buildTarget,
                targetGroup = targetGroup,
                options = isDev
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            Debug.Log($"[P6] Output: {playerOutputPath}");
            Debug.Log($"[P6] Scenes: {options.scenes.Length}, Development: {isDev}");

            var buildReport = BuildTelemetry.Measure(
                "P6.BuildPlayer",
                "Player",
                () => BuildPipeline.BuildPlayer(options));

            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                int errors = buildReport.summary.totalErrors;
                throw new BuildFailedException(Id,
                    $"BuildPlayer failed: {errors} errors, result={buildReport.summary.result}");
            }

            // 6. 记录产物
            long playerSize = 0;
            if (File.Exists(playerOutputPath))
                playerSize = new FileInfo(playerOutputPath).Length;
            else if (Directory.Exists(playerOutputPath))
            {
                foreach (string f in Directory.GetFiles(playerOutputPath, "*", SearchOption.AllDirectories))
                    playerSize += new FileInfo(f).Length;
            }
            context.AddArtifact(playerOutputPath, $"Player ({buildTarget})", playerSize);

            Debug.Log($"[P6] BuildPlayer: DONE ({playerSize / 1024 / 1024} MB)");
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            Debug.Log("[P6] ✓ Player artifact verified");
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }
            return scenes.ToArray();
        }
    }
}
