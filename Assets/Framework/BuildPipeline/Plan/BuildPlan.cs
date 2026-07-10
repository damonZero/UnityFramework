using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Plan
{
    /// <summary>
    /// 构建计划 —— 记录本次构建将执行哪些 Stage、哪些被跳过、跳过原因。
    /// 在构建开始前生成并写入 state 目录。
    /// </summary>
    [Serializable]
    public sealed class BuildPlan
    {
        /// <summary>Schema 版本</summary>
        public string SchemaVersion = "1.0.0";

        /// <summary>构建 ID</summary>
        public string RunId;

        /// <summary>Pipeline 版本</summary>
        public string PipelineVersion = "1.0.0";

        /// <summary>环境标识</summary>
        public string Environment;

        /// <summary>目标平台</summary>
        public string Platform;

        /// <summary>版本号</summary>
        public string Version;

        /// <summary>生成时间 UTC</summary>
        public string GeneratedAtUtc;

        /// <summary>各 Stage 计划条目</summary>
        public List<BuildPlanEntry> Entries = new List<BuildPlanEntry>();

        /// <summary>计划执行的 Stage 总数</summary>
        public int TotalStages => Entries.Count;

        /// <summary>将被跳过的 Stage 数</summary>
        public int SkippedStages => Entries.FindAll(e => e.WillSkip).Count;

        /// <summary>将被执行的 Stage 数</summary>
        public int RunningStages => Entries.Count - SkippedStages;

        public void AddEntry(string stageId, string displayName, int order, bool willSkip,
            string skipReasonCode = null, string skipHumanReason = null)
        {
            Entries.Add(new BuildPlanEntry
            {
                StageId = stageId,
                DisplayName = displayName,
                Order = order,
                WillSkip = willSkip,
                SkipReasonCode = skipReasonCode,
                SkipHumanReason = skipHumanReason,
            });
        }
    }

    [Serializable]
    public sealed class BuildPlanEntry
    {
        public string StageId;
        public string DisplayName;
        public int Order;
        public bool WillSkip;
        public string SkipReasonCode;
        public string SkipHumanReason;
        public List<string> DependsOn = new List<string>();
    }
}
