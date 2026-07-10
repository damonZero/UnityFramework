using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// CI 命令行入口 —— 供 batchmode / CI 系统调用。
    /// 用法: Unity -batchmode -quit -projectPath <project>
    ///   -executeMethod Boot.Editor.Build.BuildCommandLine.Run
    ///   -profile Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset
    ///   -mode Full
    ///   -outputRoot BuildBackup
    /// </summary>
    public static class BuildCommandLine
    {
        public static void Run()
        {
            try
            {
                var args = ParseArgs();

                // 优先使用 BuildProfile
                BuildProfile profile = null;
                if (!string.IsNullOrEmpty(args.ProfilePath))
                {
                    profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(args.ProfilePath);
                    if (profile == null)
                    {
                        Debug.LogError($"[BuildCI] BuildProfile not found: {args.ProfilePath}");
                        EditorApplication.Exit((int)Framework.BuildPipeline.CI.BuildExitCode.ConfigError);
                        return;
                    }
                }

                // 回退到 BuildConfig
                BuildConfig config = null;
                if (profile == null)
                {
                    string configPath = string.IsNullOrEmpty(args.ConfigPath)
                        ? "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset"
                        : args.ConfigPath;
                    config = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
                    if (config == null)
                    {
                        config = ScriptableObject.CreateInstance<BuildConfig>();
                        Debug.LogWarning("[BuildCI] Using default BuildConfig");
                    }

                    if (!string.IsNullOrEmpty(args.Platform))
                        config.Platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), args.Platform);
                    if (!string.IsNullOrEmpty(args.Version))
                        config.Version = args.Version;
                    if (!string.IsNullOrEmpty(args.OutputRoot))
                        config.OutputDir = args.OutputRoot;
                }
                else
                {
                    if (!string.IsNullOrEmpty(args.OutputRoot))
                        profile.OutputRoot = args.OutputRoot;
                }

                Debug.Log("[BuildCI] ========== CI BUILD STARTED ==========");

                BuildReport report;

                if (profile != null)
                {
                    // 通过 Profile + Runner 执行
                    var context = new BuildContext { Profile = profile };
                    var runner = new BuildPipelineRunner(context);
                    var reportData = runner.Run();

                    // 将新报告转换为旧报告格式以兼容退出码判定
                    report = ConvertToLegacyReport(reportData);
                }
                else
                {
                    // 通过现有 KJBuildPipeline 执行
                    bool isFull = args.Mode?.ToLowerInvariant() == "full";
                    if (isFull)
                        KJBuildPipeline.ClearAllMarkers(config);

                    report = KJBuildPipeline.Build(config);
                }

                int exitCode = report.summary.allPassed
                    ? (int)Framework.BuildPipeline.CI.BuildExitCode.Success
                    : (int)Framework.BuildPipeline.CI.BuildExitCode.UnknownError;

                Debug.Log($"[BuildCI] Build result: {(report.summary.allPassed ? "SUCCESS" : "FAILED")}");
                Debug.Log($"[BuildCI] Exit code: {exitCode}");

                EditorApplication.Exit(exitCode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildCI] Fatal: {ex}");
                EditorApplication.Exit((int)Framework.BuildPipeline.CI.BuildExitCode.UnknownError);
            }
        }

        private static BuildArgs ParseArgs()
        {
            var args = new BuildArgs();
            string[] cliArgs = Environment.GetCommandLineArgs();

            for (int i = 0; i < cliArgs.Length; i++)
            {
                if (cliArgs[i] == "-profile" && i + 1 < cliArgs.Length)
                    args.ProfilePath = cliArgs[++i];
                else if (cliArgs[i] == "-config" && i + 1 < cliArgs.Length)
                    args.ConfigPath = cliArgs[++i];
                else if (cliArgs[i] == "-mode" && i + 1 < cliArgs.Length)
                    args.Mode = cliArgs[++i];
                else if (cliArgs[i] == "-platform" && i + 1 < cliArgs.Length)
                    args.Platform = cliArgs[++i];
                else if (cliArgs[i] == "-version" && i + 1 < cliArgs.Length)
                    args.Version = cliArgs[++i];
                else if (cliArgs[i] == "-outputRoot" && i + 1 < cliArgs.Length)
                    args.OutputRoot = cliArgs[++i];
            }

            return args;
        }

        private static BuildReport ConvertToLegacyReport(BuildReportData data)
        {
            var report = new BuildReport
            {
                platform = data.Platform,
                version = data.Version,
            };

            if (data.EnvironmentSnapshot != null)
            {
                report.buildStartedAt = data.EnvironmentSnapshot.CapturedAtUtc;
            }

            foreach (var sr in data.StageResults)
            {
                var legacy = report.AddStage(sr.DisplayName);
                legacy.passed = sr.Status == StageStatus.Passed || sr.Status == StageStatus.Skipped;
                legacy.skipped = sr.Status == StageStatus.Skipped;
                legacy.durationSec = sr.DurationMs / 1000f;
                legacy.errorMessage = sr.ErrorMessage;
            }

            report.summary.allPassed = data.AllPassed;
            return report;
        }
    }

    /// <summary>
    /// CI 命令行参数
    /// </summary>
    public class BuildArgs
    {
        public string ProfilePath;
        public string ConfigPath;
        public string Mode = "Full";        // Full / Incremental
        public string Platform;
        public string Version;
        public string OutputRoot;
    }
}
