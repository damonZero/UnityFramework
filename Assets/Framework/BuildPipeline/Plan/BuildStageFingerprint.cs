using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Plan
{
    /// <summary>
    /// Stage 指纹 —— 记录一次成功完成的 Stage 的输入/输出/工具版本哈希。
    /// 用于增量构建时判断是否需要重跑。
    /// </summary>
    [Serializable]
    public sealed class BuildStageFingerprint
    {
        /// <summary>Stage ID</summary>
        public string StageId;

        /// <summary>Pipeline 版本</summary>
        public string PipelineVersion = "1.0.0";

        /// <summary>Stage 自身版本（代码变更时递增）</summary>
        public int StageVersion = 1;

        /// <summary>Profile 快照哈希</summary>
        public string ProfileHash;

        /// <summary>所有输入的组合哈希</summary>
        public string InputsHash;

        /// <summary>所有输出的组合哈希</summary>
        public string OutputsHash;

        /// <summary>工具版本组合哈希（Unity、HybridCLR、YooAsset、Gradle 等）</summary>
        public string ToolsHash;

        /// <summary>完成时间 UTC</summary>
        public string CompletedAtUtc;

        /// <summary>Unity 版本</summary>
        public string UnityVersion;

        /// <summary>关键包版本（HybridCLR、YooAsset）</summary>
        public Dictionary<string, string> PackageVersions = new Dictionary<string, string>();

        /// <summary>是否与另一个指纹完全匹配</summary>
        public bool Matches(BuildStageFingerprint other)
        {
            if (other == null) return false;
            return PipelineVersion == other.PipelineVersion
                && StageVersion == other.StageVersion
                && ProfileHash == other.ProfileHash
                && InputsHash == other.InputsHash
                && ToolsHash == other.ToolsHash;
        }
    }
}
