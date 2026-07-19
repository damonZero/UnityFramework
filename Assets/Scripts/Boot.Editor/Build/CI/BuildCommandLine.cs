using System;
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
                var report = KJBuildPipeline.Build(profile);

                int exitCode = report.AllPassed
                    ? (int)Framework.BuildPipeline.CI.BuildExitCode.Success
                    : (int)Framework.BuildPipeline.CI.BuildExitCode.UnknownError;

                BuildLogger.Info($"[BuildCI] Build result: {(report.AllPassed ? "SUCCESS" : "FAILED")}");
                BuildLogger.Info($"[BuildCI] Exit code: {exitCode}");
                EditorApplication.Exit(exitCode);
            }
            catch (Exception ex)
            {
                BuildLogger.Error($"[BuildCI] Fatal: {ex}");
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
                else if (cliArgs[i] == "-platform" && i + 1 < cliArgs.Length)
                    args.Platform = cliArgs[++i];
                else if (cliArgs[i] == "-version" && i + 1 < cliArgs.Length)
                    args.Version = cliArgs[++i];
                else if (cliArgs[i] == "-outputRoot" && i + 1 < cliArgs.Length)
                    args.OutputRoot = cliArgs[++i];
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
    }
}
