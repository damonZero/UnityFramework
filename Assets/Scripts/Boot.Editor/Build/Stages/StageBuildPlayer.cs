using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 6 — Unity Player 构建（IL2CPP 强制）。
    /// 产出 Build/{Platform}/KJ.{ext}
    /// </summary>
    public static class StageBuildPlayer
    {
        public static void Execute(BuildConfig config)
        {
            Debug.Log("[S6] BuildPlayer: Starting...");

            // 强制 IL2CPP
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(config.Platform);
            var currentBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            if (currentBackend != ScriptingImplementation.IL2CPP)
            {
                Debug.Log($"[S6] Switching ScriptingBackend: {currentBackend} → IL2CPP");
                PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.IL2CPP);
            }

            // 加条件编译宏（可选，供 KJ_BUILD_PIPELINE 条件编译）
            string originalDefines = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(targetGroup));
            string newDefines = originalDefines;
            if (!newDefines.Contains("KJ_BUILD_PIPELINE"))
            {
                newDefines = string.IsNullOrEmpty(originalDefines)
                    ? "KJ_BUILD_PIPELINE"
                    : originalDefines + ";KJ_BUILD_PIPELINE";
                PlayerSettings.SetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(targetGroup), newDefines);
                Debug.Log("[S6] Added KJ_BUILD_PIPELINE define symbol");
            }

            // 确保 StreamingAssets 已刷新
            AssetDatabase.Refresh();

            // 构建选项
            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = config.GetPlayerPath(),
                target = config.Platform,
                targetGroup = targetGroup,
                options = config.Development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(options.locationPathName);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            Debug.Log($"[S6] Building player: {options.locationPathName}");
            Debug.Log($"[S6] Target: {options.target}, Development: {config.Development}");
            Debug.Log($"[S6] Scenes: {options.scenes.Length}");

            // 执行构建
            var buildReport = BuildPipeline.BuildPlayer(options);

            // 校验
            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                int errors = buildReport.summary.totalErrors;
                throw new BuildFailedException("S6_BuildPlayer",
                    $"BuildPlayer failed with {errors} error(s). " +
                    $"Result: {buildReport.summary.result}");
            }

            // 不变量：输出文件或目录存在
            // Android (Export Project) / iOS 输出目录；Standalone 输出单个文件
            string playerPath = options.locationPathName;
            if (!File.Exists(playerPath) && !Directory.Exists(playerPath))
            {
                throw new BuildFailedException("S6_BuildPlayer",
                    $"Player not found at: {playerPath}");
            }

            if (File.Exists(playerPath))
            {
                var fileInfo = new FileInfo(playerPath);
                if (fileInfo.Length == 0)
                {
                    throw new BuildFailedException("S6_BuildPlayer",
                        $"Player file is empty: {playerPath}");
                }
                Debug.Log($"[S6] Player built: {playerPath} ({fileInfo.Length / 1024 / 1024} MB)");
            }
            else
            {
                // 目录输出（Android Gradle 工程 / iOS Xcode 工程）
                int fileCount = Directory.GetFiles(playerPath, "*", SearchOption.AllDirectories).Length;
                Debug.Log($"[S6] Player built (Export Project): {playerPath} ({fileCount} files)");
            }

            // 复核 IL2CPP 后端
            var finalBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            Debug.Log($"[S6] Final ScriptingBackend: {finalBackend}");

            Debug.Log("[S6] BuildPlayer: DONE");
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }
            return scenes.ToArray();
        }
    }
}
