using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Plan
{
    /// <summary>
    /// 跳过决策 —— 包含是否可跳过及其原因。
    /// 机器可读，不允许只靠缓存文件存在而无明确原因。
    /// </summary>
    [Serializable]
    public sealed class BuildSkipDecision
    {
        /// <summary>是否可跳过此 Stage</summary>
        public bool CanSkip;

        /// <summary>跳过原因代码（如 "INPUT_FINGERPRINT_UNCHANGED"）</summary>
        public string ReasonCode;

        /// <summary>人类可读的跳过原因</summary>
        public string HumanReason;

        /// <summary>支持跳过决策的证据</summary>
        public List<string> Evidence = new List<string>();

        // ===== 常见原因代码 =====
        public const string CodeInputUnchanged = "INPUT_FINGERPRINT_UNCHANGED";
        public const string CodeOutputExists = "OUTPUTS_STILL_VALID";
        public const string CodeStageSkippedByMask = "SKIPPED_BY_MASK";
        public const string CodeStageSkippedByPolicy = "SKIPPED_BY_POLICY";

        public static BuildSkipDecision DoNotSkip(string reason = null)
            => new BuildSkipDecision { CanSkip = false, HumanReason = reason ?? "必须运行" };

        public static BuildSkipDecision SkipBecause(string reasonCode, string humanReason,
            List<string> evidence = null)
            => new BuildSkipDecision
            {
                CanSkip = true,
                ReasonCode = reasonCode,
                HumanReason = humanReason,
                Evidence = evidence ?? new List<string>(),
            };
    }
}
