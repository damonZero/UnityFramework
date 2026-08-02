using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Diagnostics
{
    /// <summary>
    /// 结构化构建问题 —— 包含错误码、阶段、证据、可能原因、修复建议。
    /// 面向机器读取，供 CI、AI 诊断使用。
    /// </summary>
    [Serializable]
    public sealed class BuildIssue
    {
        /// <summary>错误码（稳定标识，如 "KJ-BUILD-HYB-001"）</summary>
        public string Code;

        /// <summary>严重级别</summary>
        public BuildIssueSeverity Severity;

        /// <summary>所属 Stage ID</summary>
        public string StageId;

        /// <summary>人类可读的问题描述</summary>
        public string Message;

        /// <summary>问题证据（日志行、路径、数值等）</summary>
        public List<string> Evidence = new List<string>();

        /// <summary>可能的原因</summary>
        public string LikelyCause;

        /// <summary>建议的修复步骤</summary>
        public string SuggestedFix;

        /// <summary>相关文件路径</summary>
        public List<string> RelatedFiles = new List<string>();

        /// <summary>是否为阻断性问题（阻断后续 Stage 执行）</summary>
        public bool IsBlocking;

        public override string ToString() => $"[{Code}] {Severity}: {Message}";

        // ===== 工厂方法 =====

        public static BuildIssue Error(string code, string stageId, string message,
            string likelyCause = null, string suggestedFix = null, bool blocking = true)
        {
            return new BuildIssue
            {
                Code = code,
                Severity = BuildIssueSeverity.Error,
                StageId = stageId,
                Message = message,
                LikelyCause = likelyCause,
                SuggestedFix = suggestedFix,
                IsBlocking = blocking,
            };
        }

        public static BuildIssue Warning(string code, string stageId, string message,
            string likelyCause = null, string suggestedFix = null)
        {
            return new BuildIssue
            {
                Code = code,
                Severity = BuildIssueSeverity.Warning,
                StageId = stageId,
                Message = message,
                LikelyCause = likelyCause,
                SuggestedFix = suggestedFix,
                IsBlocking = false,
            };
        }

        public static BuildIssue Info(string code, string stageId, string message)
        {
            return new BuildIssue
            {
                Code = code,
                Severity = BuildIssueSeverity.Info,
                StageId = stageId,
                Message = message,
                IsBlocking = false,
            };
        }
    }
}
