using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Plan
{
    /// <summary>
    /// Stage 预期输出规格 —— 描述一个 Stage 产出什么文件/目录。
    /// 用于验证 Stage 是否真正成功、增量跳过后产物是否仍在。
    /// </summary>
    [Serializable]
    public sealed class BuildStageOutputs
    {
        /// <summary>必须存在的输出文件路径</summary>
        public List<string> RequiredFiles = new List<string>();

        /// <summary>必须存在的输出目录路径</summary>
        public List<string> RequiredDirectories = new List<string>();

        /// <summary>输出产物的 SHA256 哈希（用于指纹比对）</summary>
        public string OutputsHash;

        /// <summary>产物描述（供报告展示）</summary>
        public Dictionary<string, string> Descriptions = new Dictionary<string, string>();

        public BuildStageOutputs WithRequiredFile(string path)
        {
            RequiredFiles.Add(path);
            return this;
        }

        public BuildStageOutputs WithRequiredDirectory(string path)
        {
            RequiredDirectories.Add(path);
            return this;
        }

        public BuildStageOutputs WithDescription(string path, string desc)
        {
            Descriptions[path] = desc;
            return this;
        }
    }
}
