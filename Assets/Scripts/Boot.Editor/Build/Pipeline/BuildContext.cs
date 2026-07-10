using System;
using System.Collections.Generic;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Plan;
using Framework.BuildPipeline.Reports;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 单次构建的上下文 —— Stage 之间通过此对象共享状态，不通过静态字段。
    /// </summary>
    public class BuildContext
    {
        /// <summary>构建唯一 ID（格式 yyyyMMdd_HHmmss_{shortGuid}）</summary>
        public string RunId { get; } = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}".Substring(0, 25);

        /// <summary>UTC 开始时间</summary>
        public DateTime StartedAtUtc { get; } = DateTime.UtcNow;

        /// <summary>项目根目录绝对路径</summary>
        public string ProjectRoot { get; } = Environment.CurrentDirectory;

        /// <summary>当前使用的 Profile</summary>
        public BuildProfile Profile { get; set; }

        /// <summary>旧版 BuildConfig 兼容引用（后续逐步移除）</summary>
        public BuildConfig Config { get; set; }

        /// <summary>本次构建计划</summary>
        public BuildPlan Plan { get; set; }

        /// <summary>产物清单</summary>
        public BuildArtifactManifest Artifacts { get; set; } = new BuildArtifactManifest();

        /// <summary>累积的结构化问题</summary>
        public List<BuildIssue> Issues { get; set; } = new List<BuildIssue>();

        /// <summary>输出路径集</summary>
        public BuildPaths Paths { get; set; }

        /// <summary>事务系统（若 Stage 需要修改项目状态）</summary>
        public BuildConfigTransaction Transaction { get; set; }

        /// <summary>是否已请求取消</summary>
        public bool IsCancellationRequested { get; set; }

        public void AddIssue(BuildIssue issue)
        {
            Issues.Add(issue);
        }

        public void AddArtifact(string path, string description, long sizeBytes, string sha256 = "")
        {
            Artifacts.Add(path, description, sizeBytes, sha256);
        }
    }
}
