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
            Debug.Log("[P0] Plan: Generating build plan...");

            // 验证 Profile
            var profile = context.Profile;
            if (profile == null)
            {
                // 如果使用旧 BuildConfig，从 Config 构建 Profile
                if (context.Config != null)
                {
                    Debug.Log("[P0] Using legacy BuildConfig");
                    context.Paths = new BuildPaths(context.Config);
                }
                else
                {
                    throw new InvalidOperationException("No BuildProfile or BuildConfig provided");
                }
            }
            else
            {
                context.Paths = new BuildPaths(profile);
            }

            // 验证 Profile 合法性
            if (profile != null)
            {
                var issues = BuildProfileValidator.Validate(profile);
                context.Issues.AddRange(issues);

                // 阻断性错误（Profile 自身有问题）→ Plan 阶段标记失败
                foreach (var issue in issues)
                {
                    if (issue.IsBlocking)
                        Debug.LogWarning($"[P0] Profile issue: [{issue.Code}] {issue.Message}");
                }
            }

            // 验证 Stage 依赖完整性
            var depErrors = BuildStageRegistry.ValidateDependencies();
            if (depErrors.Count > 0)
            {
                foreach (string err in depErrors)
                    Debug.LogError($"[P0] Dependency error: {err}");
                throw new InvalidOperationException(
                    $"Stage dependency validation failed ({depErrors.Count} errors)");
            }

            // 确保输出目录
            context.Paths.EnsureDirectories();

            Debug.Log($"[P0] Plan complete. Output: {context.Paths.ArchiveRoot}");
            Debug.Log($"[P0] Reports: {context.Paths.ReportsDir}");
            Debug.Log($"[P0] Logs: {context.Paths.LogsDir}");
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            if (context.Profile == null && context.Config == null)
                throw new InvalidOperationException("No build configuration provided");
        }
    }
}
