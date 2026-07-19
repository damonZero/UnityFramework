using System;
using System.Collections.Generic;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P0 Plan — 生成构建计划、计算变更范围、确定输出目录和报告目录。
    /// </summary>
    public class P0_PlanStage : BuildStageBase
    {
        public override string Id => "P0.Plan";
        public override string DisplayName => "Generate Build Plan";
        public override int Order => 0;
        public override string Category => "Plan";
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.AlwaysRun;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs { AlwaysRun = true };

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
            => new BuildStageOutputs()
                .WithRequiredFile(context.Paths.BuildPlanPath);

        public override void Execute(BuildContext context)
        {
            BuildLogger.Info("[P0] Plan: Generating build plan...");

            var profile = context.Profile;
            if (profile == null)
                throw new InvalidOperationException("BuildProfile is required");

            // 验证 Profile 合法性
            var issues = BuildProfileValidator.Validate(profile);
            context.Issues.AddRange(issues);
            foreach (var issue in issues)
            {
                if (issue.IsBlocking)
                    throw new InvalidOperationException($"Profile issue: [{issue.Code}] {issue.Message}");
            }

            // 验证 Stage 依赖完整性
            var depErrors = BuildStageRegistry.ValidateDependencies();
            if (depErrors.Count > 0)
            {
                foreach (string err in depErrors)
                    BuildLogger.Error($"[P0] Dependency error: {err}");
                throw new InvalidOperationException(
                    $"Stage dependency validation failed ({depErrors.Count} errors)");
            }

            // 确保输出目录
            context.Paths.EnsureDirectories();

            BuildLogger.Info($"[P0] Plan complete. Output: {context.Paths.ArchiveRoot}");
            BuildLogger.Info($"[P0] Reports: {context.Paths.ReportsDir}");
            BuildLogger.Info($"[P0] Logs: {context.Paths.LogsDir}");
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            if (context.Profile == null)
                throw new InvalidOperationException("No BuildProfile provided");
        }
    }
}
