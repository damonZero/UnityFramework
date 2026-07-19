using System;
using System.Collections.Generic;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Plan;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 抽象基类 —— 提供 IBuildStage 的默认实现，子类仅需重写核心方法。
    /// </summary>
    public abstract class BuildStageBase : IBuildStage
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public virtual int Version => 1;
        public abstract int Order { get; }
        public abstract string Category { get; }
        public virtual IReadOnlyList<string> DependsOn { get; } = Array.Empty<string>();
        public abstract BuildStagePolicy Policy { get; }

        public virtual BuildStageInputs GetInputs(BuildContext context) =>
            new BuildStageInputs { AlwaysRun = Policy.HasFlag(BuildStagePolicy.AlwaysRun) };

        public virtual BuildStageOutputs GetExpectedOutputs(BuildContext context) =>
            new BuildStageOutputs();

        public virtual BuildSkipDecision CanSkip(BuildContext context, BuildStageFingerprint previous)
        {
            if (Policy.HasFlag(BuildStagePolicy.AlwaysRun)
                || Policy.HasFlag(BuildStagePolicy.NoSkip))
                return BuildSkipDecision.DoNotSkip("Policy requires this stage to run");

            if (previous == null)
                return BuildSkipDecision.DoNotSkip("No previous fingerprint");

            var inputs = GetInputs(context);
            if (inputs.AlwaysRun)
                return BuildSkipDecision.DoNotSkip("Stage marked as AlwaysRun");

            // 检查输出是否仍然存在
            var outputs = GetExpectedOutputs(context);
            foreach (string file in outputs.RequiredFiles)
            {
                if (!System.IO.File.Exists(file))
                    return BuildSkipDecision.DoNotSkip($"Required output missing: {file}");
            }
            foreach (string dir in outputs.RequiredDirectories)
            {
                if (!System.IO.Directory.Exists(dir))
                    return BuildSkipDecision.DoNotSkip($"Required output directory missing: {dir}");
            }

            return BuildSkipDecision.SkipBecause(
                BuildSkipDecision.CodeInputUnchanged,
                "Input fingerprint unchanged and outputs present",
                new List<string> { $"Previous fingerprint: {previous.InputsHash}" });
        }

        public abstract void Execute(BuildContext context);

        public virtual void Verify(BuildContext context)
        {
            var outputs = GetExpectedOutputs(context);
            foreach (string file in outputs.RequiredFiles)
            {
                if (!System.IO.File.Exists(file))
                    throw new InvalidOperationException($"Expected output file missing: {file}");
                if (new System.IO.FileInfo(file).Length == 0)
                    throw new InvalidOperationException($"Expected output file is empty: {file}");
            }
            foreach (string dir in outputs.RequiredDirectories)
            {
                if (!System.IO.Directory.Exists(dir))
                    throw new InvalidOperationException($"Expected output directory missing: {dir}");
            }
        }

        public virtual IReadOnlyList<BuildIssue> AnalyzeFailure(BuildContext context, Exception exception)
        {
            return new List<BuildIssue>
            {
                BuildIssue.Error("KJ-BUILD-UNKNOWN-000", Id,
                    $"Stage failed: {exception.Message}",
                    exception.GetType().Name,
                    "Check Unity Console for detailed error")
            };
        }

        public virtual void Rollback(BuildContext context)
        {
            // 默认无操作；Transactional Stage 需要重写
        }
    }
}
