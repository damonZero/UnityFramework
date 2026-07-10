using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// KJ 构建打包全流程管线 —— 基于 IBuildStage 插件化架构。
    /// 使用: KJBuildPipeline.Build(config) 或菜单 KJ/Build/*
    /// </summary>
    public static class KJBuildPipeline
    {
        /// <summary>
        /// 全量构建：清除所有 marker，通过 BuildPipelineRunner 执行 P0-P9。
        /// </summary>
        public static BuildReport Build(BuildConfig config)
        {
            ClearAllMarkers(config);

            var context = new BuildContext { Config = config };
            var runner = new BuildPipelineRunner(context);
            var reportData = runner.Run();

            return ToLegacyReport(reportData, config);
        }

        /// <summary>
        /// 按掩码选择性构建。
        /// mask[i]=true  → Stage i 强制重跑；mask[i]=false → 强制跳过。
        /// mask=null     → Runner 自动判断（指纹比对）。
        /// </summary>
        public static BuildReport BuildWithMask(BuildConfig config, bool[] stageMask)
        {
            if (stageMask != null)
            {
                for (int i = 0; i < 10 && i < stageMask.Length; i++)
                {
                    if (stageMask[i])
                        ClearStageMarker(StageDependencyTracker.StageNames[i]);
                }
            }

            var context = new BuildContext { Config = config };
            var runner = new BuildPipelineRunner(context);
            var reportData = runner.Run();

            return ToLegacyReport(reportData, config);
        }

        /// <summary>
        /// 增量构建：自动检测变更，仅重跑有变化的 Stage 及其下游。
        /// </summary>
        public static BuildReport IncrementalBuild(BuildConfig config, bool includeSmoke = false)
        {
            bool[] mask = StageDependencyTracker.DetectChanges(includeSmoke, config);
            Debug.Log($"[KJBuildPipeline] Incremental mask: {string.Join(", ", mask)}");
            return BuildWithMask(config, mask);
        }

        /// <summary>
        /// CI 无头入口: -executeMethod Boot.Editor.Build.KJBuildPipeline.BuildFromCommandLine
        /// </summary>
        public static void BuildFromCommandLine()
        {
            string platform = GetArg("platform") ?? "StandaloneWindows64";
            bool dev = GetArgBool("development", true);
            string version = GetArg("version") ?? Application.version;
            string configPath = GetArg("config");

            BuildConfig config;
            if (!string.IsNullOrEmpty(configPath))
            {
                config = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
                if (config == null)
                {
                    Debug.LogError($"[KJBuildPipeline] BuildConfig not found at: {configPath}");
                    EditorApplication.Exit(1);
                    return;
                }
            }
            else
            {
                config = ScriptableObject.CreateInstance<BuildConfig>();
                config.Platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), platform);
                config.Development = dev;
                config.Version = version;
            }

            var report = Build(config);
            EditorApplication.Exit(report.summary.allPassed ? 0 : 1);
        }

        // ===== Marker 管理 =====

        public static bool IsStageDone(string stageName, BuildConfig config = null)
        {
            string markerDir = GetMarkerDir(config);
            if (Directory.Exists(markerDir))
                return File.Exists(Path.Combine(markerDir, $".{stageName}.done"));
            return false;
        }

        public static void MarkStageDone(string stageName, BuildConfig config = null)
        {
            string markerDir = GetMarkerDir(config);
            if (!Directory.Exists(markerDir))
                Directory.CreateDirectory(markerDir);
            File.WriteAllText(Path.Combine(markerDir, $".{stageName}.done"), DateTime.Now.ToString("o"));
        }

        public static void ClearAllMarkers(BuildConfig config = null)
        {
            string markerDir = GetMarkerDir(config);
            if (Directory.Exists(markerDir))
            {
                foreach (string f in Directory.GetFiles(markerDir, "*.done"))
                    File.Delete(f);
                Debug.Log("[KJBuildPipeline] All stage markers cleared");
            }
        }

        public static void ClearStageMarker(string stageName, BuildConfig config = null)
        {
            string markerPath = Path.Combine(GetMarkerDir(config), $".{stageName}.done");
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
                Debug.Log($"[KJBuildPipeline] Marker cleared: {stageName}");
            }
        }

        private static string GetMarkerDir(BuildConfig config)
        {
            if (config == null)
                config = LoadConfigOrDefault();
            return config.GetMarkerDir();
        }

        private static BuildConfig LoadConfigOrDefault()
        {
            string configPath = "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset";
            var c = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
            return c ?? new BuildConfig();
        }

        // ===== 报告转换（Runner → 兼容旧格式）=====

        private static BuildReport ToLegacyReport(BuildReportData data, BuildConfig config)
        {
            var report = new BuildReport
            {
                platform = data.Platform,
                development = config?.Development ?? true,
                packageName = config?.PackageName ?? "DefaultPackage",
                version = data.Version,
                buildStartedAt = data.BuildStartedAt,
                totalDuration = data.TotalDurationMs > 0
                    ? TimeSpan.FromMilliseconds(data.TotalDurationMs).ToString(@"hh\:mm\:ss")
                    : "00:00:00",
            };

            foreach (var sr in data.StageResults)
            {
                var legacy = report.AddStage(sr.DisplayName);
                legacy.passed = sr.Status == StageStatus.Passed || sr.Status == StageStatus.Skipped;
                legacy.skipped = sr.Status == StageStatus.Skipped;
                legacy.durationSec = sr.DurationMs / 1000f;
                legacy.errorMessage = sr.ErrorMessage;
                if (sr.Status == StageStatus.Skipped)
                    legacy.skipReason = sr.SkipReason;
            }

            report.summary.allPassed = data.AllPassed;
            if (!data.AllPassed)
            {
                var failed = data.StageResults.Find(s => s.Status == StageStatus.Failed);
                if (failed != null)
                {
                    report.summary.failedStage = failed.StageId;
                    report.summary.errorMessage = failed.ErrorMessage;
                }
            }
            report.summary.stagesPassed = data.StageResults.FindAll(s => s.Passed).Count;
            report.summary.stagesFailed = data.StageResults.FindAll(
                s => s.Status == StageStatus.Failed).Count;
            report.summary.stagesSkipped = data.StageResults.FindAll(
                s => s.Status == StageStatus.Skipped).Count;

            // 汇总输出
            Debug.Log($"[KJBuildPipeline] ========== BUILD {(report.summary.allPassed ? "SUCCESS" : "FAILED")} ==========");
            Debug.Log($"[KJBuildPipeline] Duration: {report.totalDuration}");
            Debug.Log($"[KJBuildPipeline] Stages: {report.summary.stagesPassed} passed, {report.summary.stagesFailed} failed, {report.summary.stagesSkipped} skipped");

            return report;
        }

        // ===== CLI 参数解析 =====

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string prefix in new[] { $"-{name}:", $"-{name}=" })
            {
                foreach (string arg in args)
                {
                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return arg.Substring(prefix.Length);
                }
            }
            return null;
        }

        private static bool GetArgBool(string name, bool defaultValue)
        {
            string val = GetArg(name);
            if (val == null) return defaultValue;
            if (bool.TryParse(val, out bool result)) return result;
            return defaultValue;
        }
    }

    /// <summary>
    /// 构建失败异常 —— 携带阶段名称，方便外层报告定位。
    /// </summary>
    public class BuildFailedException : Exception
    {
        public string StageName { get; }

        public BuildFailedException(string stageName, string message, Exception inner = null)
            : base(message, inner)
        {
            StageName = stageName;
        }
    }

    // ===== Editor 菜单入口 =====

    public static class KJBuildPipelineMenu
    {
        [UnityEditor.MenuItem("KJ/Build/Dashboard", priority = 0)]
        private static void OpenDashboard()
        {
            BuildDashboardWindow.Open();
        }

        [UnityEditor.MenuItem("KJ/Build/Full Player Build & Validate")]
        private static void BuildFullPlayer()
        {
            string configPath = "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset";
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildConfig>();
                Debug.LogWarning("[KJBuildPipeline] BuildConfig.asset not found, using defaults.");
            }

            Debug.Log("[KJBuildPipeline] ========== FULL BUILD STARTED ==========");
            var report = KJBuildPipeline.Build(config);
            Debug.Log($"[KJBuildPipeline] Build result: {(report.summary.allPassed ? "SUCCESS" : "FAILED")}");

            if (report.summary.allPassed)
                UnityEditor.EditorUtility.DisplayDialog("Build Complete",
                    $"Build succeeded!\n\nReport: {config.GetReportPath()}.json", "OK");
            else
                UnityEditor.EditorUtility.DisplayDialog("Build Failed",
                    $"Build failed at: {report.summary.failedStage}\n\n{report.summary.errorMessage}\n\nReport: {config.GetReportPath()}.json", "OK");
        }

        [UnityEditor.MenuItem("KJ/Build/Incremental Build (Auto-detect changes)")]
        private static void BuildIncremental()
        {
            string configPath = "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset";
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildConfig>();
                Debug.LogWarning("[KJBuildPipeline] BuildConfig.asset not found, using defaults.");
            }

            Debug.Log("[KJBuildPipeline] ========== INCREMENTAL BUILD STARTED ==========");
            var report = KJBuildPipeline.IncrementalBuild(config);
            Debug.Log($"[KJBuildPipeline] Build result: {(report.summary.allPassed ? "SUCCESS" : "FAILED")}");

            if (report.summary.allPassed)
                UnityEditor.EditorUtility.DisplayDialog("Build Complete",
                    $"Incremental build succeeded!\n\nReport: {config.GetReportPath()}.json", "OK");
            else
                UnityEditor.EditorUtility.DisplayDialog("Build Failed",
                    $"Build failed at: {report.summary.failedStage}\n\n{report.summary.errorMessage}\n\nReport: {config.GetReportPath()}.json", "OK");
        }

        [UnityEditor.MenuItem("KJ/Build/Build Stage Manager...")]
        private static void OpenBuildStagePanel()
        {
            BuildStagePanel.Open();
        }

        [UnityEditor.MenuItem("KJ/Build/Clear All Stage Markers")]
        private static void ClearMarkers()
        {
            KJBuildPipeline.ClearAllMarkers();
            UnityEditor.EditorUtility.DisplayDialog("Markers Cleared",
                "All .stageN.done markers have been deleted.\nNext build will run all stages from scratch.", "OK");
        }
    }
}
