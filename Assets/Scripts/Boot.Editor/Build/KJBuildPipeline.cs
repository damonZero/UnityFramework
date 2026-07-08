using System;
using System.IO;
using System.Linq;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using Framework.Asset;

namespace Boot.Editor.Build
{
    /// <summary>
    /// KJ 构建打包全流程管线 —— 单一编排器。
    /// 使用: KJBuildPipeline.Build(config) 或 -executeMethod Boot.Editor.Build.KJBuildPipeline.BuildFromCommandLine
    /// </summary>
    public static class KJBuildPipeline
    {
        private const string HotUpdateAssetRoot = "Assets/GameRes/HotUpdate";
        private const string DllAssetFolder = HotUpdateAssetRoot + "/Dlls";
        private const string MetadataAssetFolder = HotUpdateAssetRoot + "/AotMetadata";
        private const string YooAssetGroupName = "HotUpdate";
        private const string HotUpdateTag = "hotupdate";
        private const string BootScenePath = "Assets/GameRes/Scene/Boot/Main.unity";

        // ===== 总入口 =====

        /// <summary>
        /// 全量构建：按 Stage 0-9 顺序执行全部阶段（使用标记续跑）。
        /// </summary>
        public static BuildReport Build(BuildConfig config)
        {
            return BuildWithMask(config, null);
        }

        /// <summary>
        /// 按掩码选择性构建。
        /// mask[i]=true  → Stage i 强制重跑（清除标记）；mask[i]=false → 强制跳过。
        /// mask=null     → 使用标记文件决定是否跳过（原有行为）。
        /// </summary>
        public static BuildReport BuildWithMask(BuildConfig config, bool[] stageMask)
        {
            // 对于 mask 中指定要跑的 Stage，先清除标记以确保不会跳过
            if (stageMask != null)
            {
                for (int i = 0; i < 10 && i < stageMask.Length; i++)
                {
                    if (stageMask[i])
                        ClearStageMarker(StageDependencyTracker.StageNames[i]);
                }
            }

            var report = new BuildReport
            {
                platform = config.Platform.ToString(),
                development = config.Development,
                packageName = config.PackageName,
                version = config.Version,
                buildStartedAt = DateTime.Now.ToString("o")
            };

            DateTime buildStart = DateTime.Now;

            try
            {
                // Stage 0
                RunStage(report, 0, () => StagePreFlightCheck.Execute(config), stageMask);

                // Stage 1
                RunStage(report, 1, () => StageGenerateAll.Execute(config), stageMask);

                // Stage 2
                RunStage(report, 2, () => StageCompile.Execute(config), stageMask);

                // Stage 3
                RunStage(report, 3, () => StageSync.Execute(config), stageMask);

                // Stage 4
                RunStage(report, 4, () => StageBuildYooAsset.Execute(config), stageMask);

                // Stage 5
                RunStage(report, 5, () => StageApplyConfig.Execute(config), stageMask);

                // Stage 6
                RunStage(report, 6, () => StageBuildPlayer.Execute(config), stageMask);

                // Stage 7
                RunStage(report, 7, () => StageValidateArtifacts.Execute(config, report), stageMask);

                // Stage 8
                if (config.SmokeEnabled)
                    RunStage(report, 8, () => StageSmokeRun.Execute(config, report), stageMask);
                else
                    SkipStage(report, "S8_SmokeRun", "Smoke disabled in config");

                // Stage 9
                RunStage(report, 9, () => StageReport.Execute(config, report), stageMask);

                report.summary.allPassed = true;
            }
            catch (BuildFailedException ex)
            {
                report.summary.allPassed = false;
                report.summary.failedStage = ex.StageName;
                report.summary.errorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                report.summary.allPassed = false;
                report.summary.failedStage = "Unknown";
                report.summary.errorMessage = ex.ToString();
            }

            report.totalDuration = (DateTime.Now - buildStart).ToString(@"hh\:mm\:ss");
            report.summary.stagesPassed = report.stages.Count(s => s.passed);
            report.summary.stagesFailed = report.stages.Count(s => !s.passed && !s.skipped);
            report.summary.stagesSkipped = report.stages.Count(s => s.skipped);

            // 写报告
            string jsonPath = config.GetReportPath() + ".json";
            string mdPath = config.GetReportPath() + ".md";
            report.WriteJson(jsonPath);
            report.WriteMarkdown(mdPath);

            // 回滚 AssetConfig （Stage 5 改了 Mode）
            StageApplyConfig.RollbackAssetConfig();

            // 汇总输出
            Debug.Log($"[KJBuildPipeline] ========== BUILD {(report.summary.allPassed ? "SUCCESS" : "FAILED")} ==========");
            Debug.Log($"[KJBuildPipeline] Duration: {report.totalDuration}");
            Debug.Log($"[KJBuildPipeline] Stages: {report.summary.stagesPassed} passed, {report.summary.stagesFailed} failed, {report.summary.stagesSkipped} skipped");
            Debug.Log($"[KJBuildPipeline] Report: {jsonPath}");
            Debug.Log($"[KJBuildPipeline] =========================================");

            return report;
        }

        /// <summary>
        /// 增量构建：自动检测变更，仅重跑有变化的 Stage 及其下游。
        /// </summary>
        public static BuildReport IncrementalBuild(BuildConfig config, bool includeSmoke = false)
        {
            bool[] mask = StageDependencyTracker.DetectChanges(includeSmoke);
            Debug.Log($"[KJBuildPipeline] Incremental mask: {string.Join(", ", mask)}");
            return BuildWithMask(config, mask);
        }

        /// <summary>
        /// CI 无头入口: -executeMethod Boot.Editor.Build.KJBuildPipeline.BuildFromCommandLine
        /// </summary>
        public static void BuildFromCommandLine()
        {
            // 从命令行参数解析
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

        // ===== 辅助方法 =====

        private static void RunStage(BuildReport report, int stageIndex, Action action, bool[] stageMask)
        {
            string stageName = StageDependencyTracker.StageNames[stageIndex];
            var stage = report.AddStage(stageName);

            // 掩码级别显式跳过（优先级高于标记检查）
            if (stageMask != null && stageIndex < stageMask.Length && !stageMask[stageIndex])
            {
                stage.skipped = true;
                stage.skipReason = "排除（掩码或增量检测未触发）";
                stage.passed = true;
                stage.durationSec = 0;
                Debug.Log($"[KJBuildPipeline] {stageName} SKIPPED (excluded by mask)");
                return;
            }

            // 标记续跑检查
            if (IsStageDone(stageName))
            {
                stage.skipped = true;
                stage.skipReason = "Marker found, stage already completed";
                stage.passed = true;
                stage.durationSec = 0;
                Debug.Log($"[KJBuildPipeline] {stageName} SKIPPED (marker found)");
                return;
            }

            DateTime start = DateTime.Now;
            try
            {
                action();
                stage.passed = true;
                MarkStageDone(stageName);
                Debug.Log($"[KJBuildPipeline] {stageName} PASSED");
            }
            catch (Exception ex)
            {
                stage.passed = false;
                stage.errorMessage = ex.Message;
                Debug.LogError($"[KJBuildPipeline] {stageName} FAILED: {ex.Message}");

                // 逐个 Stage 的异常最终会被外部 catch 捕获并终止管线
                // 但我们需要确保内部抛出的异常能传递到外层
                throw new BuildFailedException(stageName, ex.Message, ex);
            }
            finally
            {
                stage.durationSec = (float)(DateTime.Now - start).TotalSeconds;
                stage.finishedAt = DateTime.Now.ToString("o");
            }
        }

        private static void SkipStage(BuildReport report, string stageName, string reason)
        {
            var stage = report.AddStage(stageName);
            stage.skipped = true;
            stage.skipReason = reason;
            stage.passed = true;
            stage.durationSec = 0;
            Debug.Log($"[KJBuildPipeline] {stageName} SKIPPED ({reason})");
        }

        public static bool IsStageDone(string stageName)
        {
            string markerDir = "Build/.markers";
            if (Directory.Exists(markerDir))
                return File.Exists(Path.Combine(markerDir, $".{stageName}.done"));
            return false;
        }

        public static void MarkStageDone(string stageName)
        {
            string markerDir = "Build/.markers";
            if (!Directory.Exists(markerDir))
                Directory.CreateDirectory(markerDir);
            File.WriteAllText(Path.Combine(markerDir, $".{stageName}.done"), DateTime.Now.ToString("o"));
        }

        public static void ClearAllMarkers()
        {
            string markerDir = "Build/.markers";
            if (Directory.Exists(markerDir))
            {
                foreach (string f in Directory.GetFiles(markerDir, "*.done"))
                    File.Delete(f);
                Debug.Log("[KJBuildPipeline] All stage markers cleared");
            }
        }

        public static void ClearStageMarker(string stageName)
        {
            string markerPath = Path.Combine("Build/.markers", $".{stageName}.done");
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
                Debug.Log($"[KJBuildPipeline] Marker cleared: {stageName}");
            }
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            string prefix = $"-{name}:";
            foreach (string arg in args)
            {
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(prefix.Length);
            }
            // 也检查 -name=value 格式
            prefix = $"-{name}=";
            foreach (string arg in args)
            {
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(prefix.Length);
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

    /// <summary>
    /// Editor 菜单入口：KJ/Build/Full Player Build &amp; Validate
    /// </summary>
    public static class KJBuildPipelineMenu
    {
        [UnityEditor.MenuItem("KJ/Build/Full Player Build & Validate")]
        private static void BuildFullPlayer()
        {
            string configPath = "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset";
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildConfig>();
                Debug.LogWarning("[KJBuildPipeline] BuildConfig.asset not found, using defaults. " +
                    "Create one via KJ/Build/Create BuildConfig.");
            }

            Debug.Log("[KJBuildPipeline] ========== FULL BUILD STARTED ==========");
            var report = KJBuildPipeline.Build(config);
            Debug.Log($"[KJBuildPipeline] Build result: {(report.summary.allPassed ? "SUCCESS" : "FAILED")}");

            if (report.summary.allPassed)
                UnityEditor.EditorUtility.DisplayDialog("Build Complete", $"Build succeeded!\n\nReport: {config.GetReportPath()}.json", "OK");
            else
                UnityEditor.EditorUtility.DisplayDialog("Build Failed", $"Build failed at: {report.summary.failedStage}\n\n{report.summary.errorMessage}\n\nReport: {config.GetReportPath()}.json", "OK");
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
                UnityEditor.EditorUtility.DisplayDialog("Build Complete", $"Incremental build succeeded!\n\nReport: {config.GetReportPath()}.json", "OK");
            else
                UnityEditor.EditorUtility.DisplayDialog("Build Failed", $"Build failed at: {report.summary.failedStage}\n\n{report.summary.errorMessage}\n\nReport: {config.GetReportPath()}.json", "OK");
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
            UnityEditor.EditorUtility.DisplayDialog("Markers Cleared", "All .stageN.done markers have been deleted.\nNext build will run all stages from scratch.", "OK");
        }
    }
}
