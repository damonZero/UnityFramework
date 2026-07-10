using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Reports
{
    /// <summary>
    /// AI 可读构建交接数据 —— 包含失败信息、日志路径、相关文件、建议下一步操作。
    /// 每次构建无论成败都输出此文件，供 AI/CI 自动诊断。
    /// </summary>
    [Serializable]
    public sealed class AiBuildHandoff
    {
        /// <summary>Schema 版本</summary>
        public string SchemaVersion = "1.0.0";

        /// <summary>构建 ID</summary>
        public string RunId;

        /// <summary>构建目标摘要</summary>
        public string ProfileName;

        public string Platform;
        public string Environment;
        public string Version;

        /// <summary>构建是否完全成功</summary>
        public bool Success;

        /// <summary>失败的 Stage（如有）</summary>
        public string FailedStage;

        /// <summary>阻断性问题列表</summary>
        public List<AiBuildIssue> BlockingIssues = new List<AiBuildIssue>();

        /// <summary>关键日志文件路径</summary>
        public List<string> LogPaths = new List<string>();

        /// <summary>最近日志摘要（~200 行）</summary>
        public string LogTail;

        /// <summary>相关文件列表</summary>
        public List<string> RelatedFiles = new List<string>();

        /// <summary>建议下一步操作命令</summary>
        public List<string> SuggestedActions = new List<string>();
    }

    [Serializable]
    public sealed class AiBuildIssue
    {
        public string Code;
        public string Severity;
        public string StageId;
        public string Message;
        public List<string> Evidence = new List<string>();
        public string SuggestedFix;
    }
}
