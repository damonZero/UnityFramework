using System;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// KJ 构建打包全流程管线入口。
    /// 最新设计只接受 BuildProfile，并由 Stage fingerprint 控制增量跳过。
    /// </summary>
    public static class KJBuildPipeline
    {
        public const string DefaultProfilePath = "Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset";

        public static BuildReportData Build(BuildProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile), "BuildProfile is required");

            var context = new BuildContext { Profile = profile };
            var runner = new BuildPipelineRunner(context);
            return runner.Run();
        }

        public static BuildReportData BuildDefaultProfile()
        {
            return Build(LoadDefaultProfileOrThrow());
        }

        public static BuildProfile LoadDefaultProfileOrThrow()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(DefaultProfilePath);
            if (profile == null)
                throw new InvalidOperationException(
                    $"BuildProfile not found: {DefaultProfilePath}. Open KJ/Build/Dashboard and restore the default profile asset.");
            return profile;
        }

        public static BuildProfile LoadProfileOrThrow(string profilePath)
        {
            if (string.IsNullOrWhiteSpace(profilePath))
                return LoadDefaultProfileOrThrow();

            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
            if (profile == null)
                throw new InvalidOperationException($"BuildProfile not found: {profilePath}");
            return profile;
        }

        /// <summary>
        /// CI 无头入口: -executeMethod Boot.Editor.Build.KJBuildPipeline.BuildFromCommandLine -profile <assetPath>
        /// </summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                string profilePath = GetArg("profile");
                var profile = LoadProfileOrThrow(profilePath);
                var report = Build(profile);
                EditorApplication.Exit(report.AllPassed ? 0 : 1);
            }
            catch (Exception ex)
            {
                BuildLogger.Error($"[KJBuildPipeline] Command line build failed: {ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// CI entry: P2 + Host baseline. Runs HybridCLR GenerateAll (MethodBridge/link.xml/AOTGenericReferences),
        /// then reuses YooAsset artifacts for Host Player build.
        /// </summary>
        public static void BuildP2ThenHostBaseline()
        {
            BuildProfile profile = null;
            BuildContext context = null;
            try
            {
                profile = LoadProfileOrThrow(GetArg("profile"));
                context = new BuildContext
                {
                    Profile = profile,
                    Paths = new BuildPaths(profile),
                    Transaction = new BuildTransaction(),
                };
                context.Paths.EnsureDirectories();

                BuildLogger.Info("[KJBuildPipeline] P2: Generating HybridCLR (MethodBridge/link.xml/AOT)");
                new P2_GenerateStage().Execute(context);

                var p3 = new P3_HybridCLRStage();
                p3.Execute(context);
                p3.Verify(context);

                var p4 = new P4_BuildAssetStage();
                p4.Execute(context);
                p4.Verify(context);

                var config = new P5_ApplyConfigStage();
                config.Execute(context);
                config.Verify(context);

                var player = new P6_BuildPlayerStage();
                player.Execute(context);
                player.Verify(context);

                new P7_VerifyStage().Execute(context);
                BuildLogger.Info("[KJBuildPipeline] P2-Host baseline Player build succeeded.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                BuildLogger.Error($"[KJBuildPipeline] P2-Host baseline build failed: {ex}");
                EditorApplication.Exit(1);
            }
            finally
            {
                try { context?.Transaction?.Rollback(); }
                catch (Exception rollbackEx) { BuildLogger.Error($"[KJBuildPipeline] Rollback failed: {rollbackEx.Message}"); }
            }
        }

        /// <summary>
        /// Host smoke baseline: reuse already generated HybridCLR/YooAsset artifacts,
        /// apply Host config, and rebuild only the Player.
        /// </summary>
        public static void BuildHostBaselineFromCommandLine()
        {
            BuildProfile profile = null;
            BuildContext context = null;
            try
            {
                profile = LoadProfileOrThrow(GetArg("profile"));
                context = new BuildContext
                {
                    Profile = profile,
                    Paths = new BuildPaths(profile),
                    Transaction = new BuildTransaction(),
                };
                context.Paths.EnsureDirectories();

                new P4_BuildAssetStage().Verify(context);
                var config = new P5_ApplyConfigStage();
                config.Execute(context);
                config.Verify(context);
                var player = new P6_BuildPlayerStage();
                player.Execute(context);
                player.Verify(context);
                new P7_VerifyStage().Execute(context);
                BuildLogger.Info("[KJBuildPipeline] Host baseline Player build succeeded without P2/MethodBridge.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                BuildLogger.Error($"[KJBuildPipeline] Host baseline build failed: {ex}");
                EditorApplication.Exit(1);
            }
            finally
            {
                try { context?.Transaction?.Rollback(); }
                catch (Exception rollbackEx) { BuildLogger.Error($"[KJBuildPipeline] Rollback failed: {rollbackEx.Message}"); }
            }
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == $"-{name}" && i + 1 < args.Length)
                    return args[i + 1];

                string colonPrefix = $"-{name}:";
                string equalsPrefix = $"-{name}=";
                if (args[i].StartsWith(colonPrefix, StringComparison.OrdinalIgnoreCase))
                    return args[i].Substring(colonPrefix.Length);
                if (args[i].StartsWith(equalsPrefix, StringComparison.OrdinalIgnoreCase))
                    return args[i].Substring(equalsPrefix.Length);
            }
            return null;
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

}
