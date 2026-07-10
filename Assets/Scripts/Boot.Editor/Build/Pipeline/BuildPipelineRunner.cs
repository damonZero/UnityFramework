using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Plan;
using Framework.BuildPipeline.Reports;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建管线执行器 —— 按 BuildPlan 驱动 IBuildStage 顺序执行。
    /// </summary>
    public class BuildPipelineRunner
    {
        private readonly BuildContext _ctx;
        private readonly List<StageExecutionResult> _stageResults = new List<StageExecutionResult>();
        private bool _allPassed = true;

        public BuildPipelineRunner(BuildContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
        }

        public BuildReportData Run()
        {
            var snapshot = BuildEnvironmentSnapshot.Capture();
            var stages = BuildStageRegistry.GetAll();

            var depErrors = BuildStageRegistry.ValidateDependencies();
            if (depErrors.Count > 0)
            {
                foreach (string e in depErrors)
                    Debug.LogError($"[BuildPipelineRunner] Dependency error: {e}");
                throw new InvalidOperationException(
                    $"BuildStage dependency validation failed: {depErrors.Count} errors");
            }

            _ctx.Plan = GeneratePlan(stages);
            _ctx.Paths.EnsureDirectories();
            WriteBuildPlan();
            _ctx.Transaction = new BuildConfigTransaction();

            Debug.Log($"[BuildPipelineRunner] Build starting: RunId={_ctx.RunId}, Platform={_ctx.Profile?.Platform}");

            try
            {
                foreach (var stage in stages)
                {
                    if (_ctx.IsCancellationRequested)
                    {
                        Debug.LogWarning("[BuildPipelineRunner] Build cancelled by user");
                        _ctx.Transaction.Rollback();
                        break;
                    }

                    var planEntry = _ctx.Plan.Entries.Find(e => e.StageId == stage.Id);
                    var result = ExecuteStage(stage, planEntry?.WillSkip ?? false);

                    if (!result.Passed && stage.Policy.HasFlag(BuildStagePolicy.Required))
                    {
                        _allPassed = false;
                        Debug.LogError($"[BuildPipelineRunner] Required stage failed: {stage.Id}, aborting");
                        _ctx.Transaction.Rollback();
                        break;
                    }

                    if (!result.Passed)
                        _allPassed = false;
                }
            }
            catch (Exception ex)
            {
                _allPassed = false;
                Debug.LogError($"[BuildPipelineRunner] Unexpected error: {ex}");
                try { _ctx.Transaction.Rollback(); } catch { }
            }

            var report = BuildReport();
            report.EnvironmentSnapshot = snapshot;
            WriteReports(report);
            return report;
        }

        // ===== Stage 执行 =====

        private StageExecutionResult ExecuteStage(IBuildStage stage, bool planSaysSkip)
        {
            Debug.Log($"[BuildPipelineRunner] === {stage.Id}: {stage.DisplayName} ===");

            var result = new StageExecutionResult
            {
                StageId = stage.Id,
                DisplayName = stage.DisplayName,
                StartedAtUtc = DateTime.UtcNow.ToString("o"),
            };

            try
            {
                if (planSaysSkip)
                {
                    result.Status = StageStatus.Skipped;
                    result.SkipReason = "Plan indicated skip";
                    _stageResults.Add(result);
                    return result;
                }

                stage.Execute(_ctx);

                try { stage.Verify(_ctx); }
                catch (Exception verifyEx)
                {
                    result.Status = StageStatus.Failed;
                    result.ErrorMessage = $"Verification failed: {verifyEx.Message}";
                    result.Issues.AddRange(AnalyzeStageIssues(stage, verifyEx));
                    if (stage.Policy.HasFlag(BuildStagePolicy.Transactional))
                        stage.Rollback(_ctx);
                    _stageResults.Add(result);
                    return result;
                }

                WriteStageFingerprint(stage);
                result.Status = StageStatus.Passed;
                Debug.Log($"[BuildPipelineRunner] {stage.Id} PASSED");
            }
            catch (Exception ex)
            {
                result.Status = StageStatus.Failed;
                result.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                result.Issues.AddRange(AnalyzeStageIssues(stage, ex));
                if (stage.Policy.HasFlag(BuildStagePolicy.Transactional))
                {
                    try { stage.Rollback(_ctx); } catch { }
                }
            }
            finally
            {
                result.FinishedAtUtc = DateTime.UtcNow.ToString("o");
                result.DurationMs = (long)(DateTime.UtcNow - DateTime.Parse(result.StartedAtUtc)).TotalMilliseconds;
            }

            _stageResults.Add(result);
            return result;
        }

        // ===== 计划 =====

        private BuildPlan GeneratePlan(IReadOnlyList<IBuildStage> stages)
        {
            var plan = new BuildPlan
            {
                RunId = _ctx.RunId,
                Environment = _ctx.Profile?.Environment.ToString() ?? "Unknown",
                Platform = _ctx.Profile?.Platform.ToString() ?? "Unknown",
                Version = _ctx.Profile?.VersionName ?? "0.0.0",
                GeneratedAtUtc = DateTime.UtcNow.ToString("o"),
            };

            foreach (var stage in stages)
            {
                var previous = LoadPreviousFingerprint(stage.Id);
                var decision = stage.CanSkip(_ctx, previous);
                plan.AddEntry(stage.Id, stage.DisplayName, stage.Order,
                    decision.CanSkip, decision.ReasonCode, decision.HumanReason);
            }

            return plan;
        }

        private void WriteBuildPlan()
        {
            try
            {
                string json = JsonUtility.ToJson(_ctx.Plan, true);
                File.WriteAllText(_ctx.Paths.BuildPlanPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildPipelineRunner] Failed to write build plan: {ex.Message}");
            }
        }

        private BuildStageFingerprint LoadPreviousFingerprint(string stageId)
        {
            string markerPath = Path.Combine(_ctx.Paths.StateDir, $"{stageId}.fingerprint.json");
            if (!File.Exists(markerPath)) return null;
            try
            {
                return JsonUtility.FromJson<BuildStageFingerprint>(File.ReadAllText(markerPath));
            }
            catch { return null; }
        }

        private void WriteStageFingerprint(IBuildStage stage)
        {
            var fp = new BuildStageFingerprint
            {
                StageId = stage.Id,
                ProfileHash = _ctx.Profile?.ComputeProfileHash() ?? "",
                InputsHash = "stage-completed",
                OutputsHash = "stage-completed",
                CompletedAtUtc = DateTime.UtcNow.ToString("o"),
                UnityVersion = Application.unityVersion,
            };

            try
            {
                string markerPath = Path.Combine(_ctx.Paths.StateDir, $"{stage.Id}.fingerprint.json");
                File.WriteAllText(markerPath, JsonUtility.ToJson(fp, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildPipelineRunner] Failed to write fingerprint: {ex.Message}");
            }
        }

        // ===== 报告 =====

        private BuildReportData BuildReport()
        {
            return new BuildReportData
            {
                SchemaVersion = "1.0.0",
                RunId = _ctx.RunId,
                PipelineVersion = "1.0.0",
                ProfileName = _ctx.Profile?.ProfileName,
                Platform = _ctx.Profile?.Platform.ToString(),
                Environment = _ctx.Profile?.Environment.ToString(),
                Version = _ctx.Profile?.VersionName,
                BuildStartedAt = _ctx.StartedAtUtc.ToString("o"),
                BuildFinishedAt = DateTime.UtcNow.ToString("o"),
                TotalDurationMs = (long)(DateTime.UtcNow - _ctx.StartedAtUtc).TotalMilliseconds,
                AllPassed = _allPassed,
                StageResults = _stageResults,
                Issues = _ctx.Issues,
            };
        }

        private void WriteReports(BuildReportData report)
        {
            new BuildReportWriter().WriteAll(report, _ctx.Paths);
        }

        private List<Framework.BuildPipeline.Diagnostics.BuildIssue> AnalyzeStageIssues(IBuildStage stage, Exception ex)
        {
            try
            {
                var issues = stage.AnalyzeFailure(_ctx, ex);
                _ctx.Issues.AddRange(issues);
                return new List<Framework.BuildPipeline.Diagnostics.BuildIssue>(issues);
            }
            catch
            {
                var fallback = Framework.BuildPipeline.Diagnostics.BuildIssue.Error(
                    "KJ-BUILD-UNKNOWN-000", stage.Id, ex.Message);
                _ctx.Issues.Add(fallback);
                return new List<Framework.BuildPipeline.Diagnostics.BuildIssue> { fallback };
            }
        }
    }

    // ===== 报告相关类型 =====

    /// <summary>
    /// 报告写入器 —— 输出 JSON、Markdown、AI handoff。
    /// </summary>
    public class BuildReportWriter
    {
        public void WriteAll(BuildReportData report, BuildPaths paths)
        {
            try
            {
                File.WriteAllText(Path.Combine(paths.ReportsDir, "build_report.json"),
                    JsonUtility.ToJson(report, true));
            }
            catch (Exception ex) { Debug.LogError($"[BuildReportWriter] JSON: {ex.Message}"); }

            try { WriteMarkdown(report, Path.Combine(paths.ReportsDir, "build_report.md")); }
            catch (Exception ex) { Debug.LogError($"[BuildReportWriter] MD: {ex.Message}"); }

            try { WriteHandoff(report, Path.Combine(paths.ReportsDir, "ai_handoff.json")); }
            catch (Exception ex) { Debug.LogError($"[BuildReportWriter] Handoff: {ex.Message}"); }
        }

        private static void WriteMarkdown(BuildReportData report, string path)
        {
            using var sw = new StreamWriter(path);
            sw.WriteLine("# KJ Build Report");
            sw.WriteLine();
            sw.WriteLine($"- **Run ID**: {report.RunId}");
            sw.WriteLine($"- **Profile**: {report.ProfileName}");
            sw.WriteLine($"- **Platform**: {report.Platform}");
            sw.WriteLine($"- **Environment**: {report.Environment}");
            sw.WriteLine($"- **Version**: {report.Version}");
            sw.WriteLine($"- **Result**: {(report.AllPassed ? "SUCCESS" : "FAILED")}");
            sw.WriteLine($"- **Duration**: {report.TotalDurationMs / 1000.0:F1}s");
            sw.WriteLine();
            sw.WriteLine("## Stages");
            sw.WriteLine("| Stage | Status | Duration | Error |");
            sw.WriteLine("|-------|--------|----------|-------|");
            foreach (var s in report.StageResults)
                sw.WriteLine($"| {s.DisplayName} | {s.Status} | {s.DurationMs}ms | {s.ErrorMessage ?? ""} |");
            sw.WriteLine();
            if (report.Issues != null && report.Issues.Count > 0)
            {
                sw.WriteLine("## Issues");
                foreach (var issue in report.Issues)
                {
                    sw.WriteLine($"- **[{issue.Code}]** {issue.Severity}: {issue.Message}");
                    if (!string.IsNullOrEmpty(issue.SuggestedFix))
                        sw.WriteLine($"  → Fix: {issue.SuggestedFix}");
                }
            }
        }

        private static void WriteHandoff(BuildReportData report, string path)
        {
            var handoff = new AiBuildHandoff
            {
                RunId = report.RunId,
                ProfileName = report.ProfileName,
                Platform = report.Platform,
                Environment = report.Environment,
                Version = report.Version,
                Success = report.AllPassed,
            };

            if (!report.AllPassed)
            {
                var failed = report.StageResults.Find(s => s.Status == StageStatus.Failed);
                if (failed != null)
                {
                    handoff.FailedStage = failed.StageId;
                    foreach (var issue in failed.Issues)
                    {
                        handoff.BlockingIssues.Add(new AiBuildIssue
                        {
                            Code = issue.Code,
                            Severity = issue.Severity.ToString(),
                            StageId = issue.StageId,
                            Message = issue.Message,
                            Evidence = new List<string>(issue.Evidence),
                            SuggestedFix = issue.SuggestedFix,
                        });
                    }
                }
                handoff.SuggestedActions.Add("Check Unity Console for detailed error");
                handoff.SuggestedActions.Add("Re-run KJ/Build/Full Player Build & Validate");
            }

            File.WriteAllText(path, JsonUtility.ToJson(handoff, true));
        }
    }

    // ===== 结果与报告类型 =====

    /// <summary>
    /// Stage 执行结果（新架构用，区别于 BuildReport.StageResult）
    /// </summary>
    [Serializable]
    public class StageExecutionResult
    {
        public string StageId;
        public string DisplayName;
        public StageStatus Status = StageStatus.Pending;
        public string StartedAtUtc;
        public string FinishedAtUtc;
        public long DurationMs;
        public string SkipReason;
        public string ErrorMessage;
        public List<Framework.BuildPipeline.Diagnostics.BuildIssue> Issues = new List<Framework.BuildPipeline.Diagnostics.BuildIssue>();
        public bool Passed => Status == StageStatus.Passed || Status == StageStatus.Skipped;
    }

    public enum StageStatus
    {
        Pending,
        Running,
        Passed,
        Failed,
        Skipped,
    }

    /// <summary>
    /// 构建报告顶层数据结构
    /// </summary>
    [Serializable]
    public class BuildReportData
    {
        public string SchemaVersion = "1.0.0";
        public string RunId;
        public string PipelineVersion = "1.0.0";
        public string ProfileName;
        public string Platform;
        public string Environment;
        public string Version;
        public string BuildStartedAt;
        public string BuildFinishedAt;
        public long TotalDurationMs;
        public bool AllPassed;
        public List<StageExecutionResult> StageResults = new List<StageExecutionResult>();
        public List<Framework.BuildPipeline.Diagnostics.BuildIssue> Issues = new List<Framework.BuildPipeline.Diagnostics.BuildIssue>();
        public BuildEnvironmentSnapshot EnvironmentSnapshot;
    }
}
