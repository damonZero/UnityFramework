using System.IO;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建输出路径集 —— 所有路径收敛于此，避免散落在各处硬编码。
    /// </summary>
    public class BuildPaths
    {
        public string ArchiveRoot { get; }
        public string ArtifactsDir { get; }
        public string LogsDir { get; }
        public string ReportsDir { get; }
        public string StateDir { get; }
        public string TempDir { get; }
        public string BuildPlanPath { get; }

        public BuildPaths(BuildProfile profile)
        {
            ArchiveRoot = profile.GetOutputDir();
            ArtifactsDir = Path.Combine(ArchiveRoot, "artifacts");
            LogsDir = Path.Combine(ArchiveRoot, "logs");
            ReportsDir = Path.Combine(ArchiveRoot, "reports");
            StateDir = Path.Combine(ArchiveRoot, "state");
            TempDir = Path.Combine("Library", "KJBuild", profile.ProfileName ?? "default");
            BuildPlanPath = Path.Combine(StateDir, "build_plan.json");
        }

        public void EnsureDirectories()
        {
            foreach (string dir in new[] { ArtifactsDir, LogsDir, ReportsDir, StateDir, TempDir })
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
        }
    }
}
