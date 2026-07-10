using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Plan
{
    /// <summary>
    /// Stage 输入规格 —— 描述一个 Stage 依赖哪些输入。
    /// </summary>
    [Serializable]
    public sealed class BuildStageInputs
    {
        /// <summary>监控的源文件/目录路径（相对项目根）</summary>
        public List<string> SourcePaths = new List<string>();

        /// <summary>依赖的上游 Stage ID 列表</summary>
        public List<string> DependsOnStages = new List<string>();

        /// <summary>依赖的工具版本（如 "2022.3.62f2" for Unity）</summary>
        public Dictionary<string, string> ToolVersions = new Dictionary<string, string>();

        /// <summary>Profile 快照哈希（所有 Stage 通用）</summary>
        public string ProfileHash;

        /// <summary>是否始终运行（忽略指纹检查）</summary>
        public bool AlwaysRun;

        public BuildStageInputs WithSourcePaths(params string[] paths)
        {
            SourcePaths.AddRange(paths);
            return this;
        }

        public BuildStageInputs WithDependsOn(params string[] stageIds)
        {
            DependsOnStages.AddRange(stageIds);
            return this;
        }

        public BuildStageInputs WithToolVersion(string tool, string version)
        {
            ToolVersions[tool] = version;
            return this;
        }
    }
}
