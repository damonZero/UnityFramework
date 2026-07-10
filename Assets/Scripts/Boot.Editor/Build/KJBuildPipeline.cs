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
                    $"BuildProfile not found: {DefaultProfilePath}. Create one via KJ/Build/Create Build Profile.");
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
                Debug.LogError($"[KJBuildPipeline] Command line build failed: {ex}");
                EditorApplication.Exit(1);
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

    public static class KJBuildPipelineMenu
    {
        [MenuItem("KJ/Build/Full Player Build & Validate")]
        private static void BuildFullPlayer()
        {
            try
            {
                var profile = KJBuildPipeline.LoadDefaultProfileOrThrow();
                Debug.Log($"[KJBuildPipeline] ========== FULL BUILD STARTED: {profile.ProfileName} ==========");
                var report = KJBuildPipeline.Build(profile);

                if (report.AllPassed)
                {
                    EditorUtility.DisplayDialog("Build Complete",
                        $"Build succeeded.\n\nReports: {new BuildPaths(profile).ReportsDir}", "OK");
                }
                else
                {
                    var failed = report.StageResults.Find(s => s.Status == StageStatus.Failed);
                    EditorUtility.DisplayDialog("Build Failed",
                        $"Failed at: {failed?.StageId ?? "Unknown"}\n\n{failed?.ErrorMessage}\n\nReports: {new BuildPaths(profile).ReportsDir}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KJBuildPipeline] Build failed before report generation: {ex}");
                EditorUtility.DisplayDialog("Build Failed", ex.Message, "OK");
            }
        }

    }
}
