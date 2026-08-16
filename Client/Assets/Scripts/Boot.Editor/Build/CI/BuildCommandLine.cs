using System;
using Framework.BuildPipeline.CI;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// CI 命令行入口。
    /// 用法:
    /// Unity -batchmode -quit -projectPath <project>
    ///   -executeMethod Boot.Editor.Build.BuildCommandLine.Run
    ///   -profile Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset
    ///   -outputRoot BuildBackup/CI
    /// </summary>
    public static class BuildCommandLine
    {
        public static void Run()
        {
            try
            {
                var args = ParseArgs();
                var profile = KJBuildPipeline.LoadProfileOrThrow(args.ProfilePath);

                if (!string.IsNullOrEmpty(args.OutputRoot))
                    profile.OutputRoot = args.OutputRoot;
                if (!string.IsNullOrEmpty(args.Version))
                    profile.VersionName = args.Version;
                if (!string.IsNullOrEmpty(args.Platform))
                    profile.Platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), args.Platform);

                BuildLogger.Info($"[BuildCI] ========== CI BUILD STARTED: {profile.ProfileName} ==========");
                var report = KJBuildPipeline.Build(profile, args.ForceFullRebuild);

                int exitCode = ResolveExitCode(report);

                BuildLogger.Info($"[BuildCI] Build result: {(report.AllPassed ? "SUCCESS" : "FAILED")}");
                BuildLogger.Info($"[BuildCI] Exit code: {exitCode}");
                EditorApplication.Exit(exitCode);
            }
            catch (Exception ex)
            {
                BuildLogger.Error($"[BuildCI] Fatal: {ex}");
                EditorApplication.Exit((int)BuildExitCode.UnknownError);
            }
        }

        /// <summary>
        /// 从报告推导稳定的 CI 退出码：优先取第一个失败 Stage 对应的分类码，
        /// 而不是把所有失败都塌缩成 UnknownError(99)。
        /// </summary>
        private static int ResolveExitCode(BuildReportData report)
        {
            if (report.AllPassed)
                return (int)BuildExitCode.Success;

            var failed = report.StageResults.Find(s => s.Status == StageStatus.Failed);
            if (failed == null)
                return (int)BuildExitCode.UnknownError;

            return MapStageIdToExitCode(failed.StageId);
        }

        private static int MapStageIdToExitCode(string stageId)
        {
            if (string.IsNullOrEmpty(stageId))
                return (int)BuildExitCode.UnknownError;

            // StageId 形如 "P1.Preflight" —— 取 "P1" 前缀映射到 BuildExitCode 分类。
            int dot = stageId.IndexOf('.');
            string prefix = dot > 0 ? stageId.Substring(0, dot) : stageId;
            switch (prefix)
            {
                case "P0": return (int)BuildExitCode.ConfigError;
                case "P1": return (int)BuildExitCode.PreflightFailed;
                case "P2":
                case "P3": return (int)BuildExitCode.GenerateFailed;
                case "P4": return (int)BuildExitCode.AssetFailed;
                case "P5": return (int)BuildExitCode.ConfigFailed;
                case "P6": return (int)BuildExitCode.PlayerFailed;
                case "P7": return (int)BuildExitCode.VerifyFailed;
                case "P8": return (int)BuildExitCode.SmokeFailed;
                case "P9":
                case "P10": return (int)BuildExitCode.ReportFailed;
                default: return (int)BuildExitCode.UnknownError;
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
                else if (cliArgs[i] == "-platform" && i + 1 < cliArgs.Length)
                    args.Platform = cliArgs[++i];
                else if (cliArgs[i] == "-version" && i + 1 < cliArgs.Length)
                    args.Version = cliArgs[++i];
                else if (cliArgs[i] == "-outputRoot" && i + 1 < cliArgs.Length)
                    args.OutputRoot = cliArgs[++i];
                else if (cliArgs[i] == "-full")
                    args.ForceFullRebuild = true;
            }

            return args;
        }
    }

    public class BuildArgs
    {
        public string ProfilePath;
        public string Platform;
        public string Version;
        public string OutputRoot;
        public bool ForceFullRebuild;
    }
}
