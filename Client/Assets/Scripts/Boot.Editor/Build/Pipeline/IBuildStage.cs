using System.Collections.Generic;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Plan;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 接口 —— 构建管线中每个阶段的统一抽象。
    /// 每个 Stage 必须提供：输入、预期输出、跳过判断、执行、验证、失败分析。
    /// </summary>
    public interface IBuildStage
    {
        /// <summary>Stage 唯一标识（如 "P1.Preflight.Environment"）</summary>
        string Id { get; }

        /// <summary>Stage 显示名称</summary>
        string DisplayName { get; }

        /// <summary>Stage 实现版本；影响产物的代码变更时递增</summary>
        int Version { get; }

        /// <summary>执行顺序（升序）</summary>
        int Order { get; }

        /// <summary>Stage 分类</summary>
        string Category { get; }

        /// <summary>依赖的 Stage ID 列表</summary>
        IReadOnlyList<string> DependsOn { get; }

        /// <summary>Stage 策略</summary>
        BuildStagePolicy Policy { get; }

        /// <summary>声明本 Stage 的输入</summary>
        BuildStageInputs GetInputs(BuildContext context);

        /// <summary>声明本 Stage 的预期输出</summary>
        BuildStageOutputs GetExpectedOutputs(BuildContext context);

        /// <summary>判断本 Stage 是否可跳过</summary>
        BuildSkipDecision CanSkip(BuildContext context, BuildStageFingerprint previous);

        /// <summary>执行 Stage</summary>
        void Execute(BuildContext context);

        /// <summary>执行后验证（检查不变量和产物完整性）</summary>
        void Verify(BuildContext context);

        /// <summary>失败时分析原因，生成结构化问题列表</summary>
        IReadOnlyList<BuildIssue> AnalyzeFailure(BuildContext context, System.Exception exception);

        /// <summary>回滚（如果 Policy 含 Transactional）</summary>
        void Rollback(BuildContext context);
    }
}
