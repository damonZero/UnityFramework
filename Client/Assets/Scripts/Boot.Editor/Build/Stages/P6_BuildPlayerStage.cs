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
        public override int Version => 3;
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

            BuildLogger.Info($"[P6] BuildPlayer: Building for {buildTarget}...");

            // 1. 强制 IL2CPP
            var currentBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            if (currentBackend != ScriptingImplementation.IL2CPP)
            {
                BuildLogger.Info($"[P6] Switching ScriptingBackend to IL2CPP");
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
            // 修复：allowDebugging（脚本调试）应遵循独立的 ScriptDebugging 配置，
            // 而非绑定 DevelopmentBuild —— 否则「release + 脚本调试」的 QA/Profiling Profile 会静默失效。
            EditorUserBuildSettings.allowDebugging = profile.ScriptDebugging;

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

                // 应用身份 + 签名：Formal/Audit 必须，否则产出未签名/错误 applicationId 的 APK。
                ApplyAndroidIdentityAndSigning(context, profile);
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

            // 修复：Development 与 AllowDebugging 是两个独立开关，分别遵循 DevelopmentBuild 与 ScriptDebugging。
            BuildOptions buildOptions = BuildOptions.None;
            if (isDev) buildOptions |= BuildOptions.Development;
            if (profile.ScriptDebugging) buildOptions |= BuildOptions.AllowDebugging;

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = playerOutputPath,
                target = buildTarget,
                targetGroup = targetGroup,
                options = buildOptions,
            };

            BuildLogger.Info($"[P6] Output: {playerOutputPath}");
            BuildLogger.Info($"[P6] Scenes: {options.scenes.Length}, Development: {isDev}");

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

            BuildLogger.Info($"[P6] BuildPlayer: DONE ({playerSize / 1024 / 1024} MB)");
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            BuildLogger.Info("[P6] ✓ Player artifact verified");
        }

        private void ApplyAndroidIdentityAndSigning(BuildContext context, BuildProfile profile)
        {
            context.Transaction.SnapshotAndroidSigning();

            if (!string.IsNullOrWhiteSpace(profile.PackageId))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, profile.PackageId);
            if (!string.IsNullOrWhiteSpace(profile.VersionName))
                PlayerSettings.bundleVersion = profile.VersionName;
            PlayerSettings.Android.bundleVersionCode = profile.VersionCode;

            if (!profile.RequireSigning)
                return;

            if (string.IsNullOrWhiteSpace(profile.KeystorePath))
                throw new BuildFailedException(Id, "Android keystore path is empty but signing is required (Formal/Audit).");
            if (string.IsNullOrWhiteSpace(profile.KeystoreAlias))
                throw new BuildFailedException(Id, "Android keystore alias is empty but signing is required (Formal/Audit).");

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = profile.KeystorePath;
            PlayerSettings.Android.keystorePass = profile.KeystorePassword ?? string.Empty;
            PlayerSettings.Android.keyaliasName = profile.KeystoreAlias;
            PlayerSettings.Android.keyaliasPass = profile.KeystorePassword ?? string.Empty;
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
