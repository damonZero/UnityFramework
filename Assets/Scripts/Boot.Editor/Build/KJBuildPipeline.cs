using System;
using UnityEditor;

namespace Boot.Editor.Build
{
    /// <summary>
    /// KJ 构建打包全流程管线入口。
    /// 最新设计只接受 BuildProfile，并由 Stage fingerprint 控制增量跳过。
    /// </summary>
    public static class KJBuildPipeline
    {
        public const string DefaultProfilePath = "Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset";

        public static BuildReportData Build(BuildProfile profile, bool forceFullRebuild = false)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile), "BuildProfile is required");

            var context = new BuildContext
            {
                Profile = profile,
                ForceFullRebuild = forceFullRebuild,
            };
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
