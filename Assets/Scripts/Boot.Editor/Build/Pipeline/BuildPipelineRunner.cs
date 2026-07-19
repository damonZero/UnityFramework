using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Framework.Aop;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Plan;
using Framework.BuildPipeline.Reports;
using UnityEditor;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

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
        private long _buildStartedTimestamp;

        public BuildPipelineRunner(BuildContext context)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
        }

        public BuildReportData Run()
        {
            if (_ctx.Profile == null)
                throw new InvalidOperationException("BuildProfile is required.");

            _buildStartedTimestamp = Stopwatch.GetTimestamp();
            var telemetryCollector = new InMemoryAopCollector();
            using var telemetrySession = AopRuntime.BeginSession(_ctx.RunId, telemetryCollector);

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

            _ctx.Paths = new BuildPaths(_ctx.Profile);
            _ctx.Paths.EnsureDirectories();
            _ctx.Transaction = new BuildTransaction();
            _ctx.Plan = GeneratePlan(stages);
            _ctx.Paths.EnsureDirectories();
            WriteBuildPlan();

            Debug.Log($"[BuildPipelineRunner] Build starting: RunId={_ctx.RunId}, Profile={_ctx.Profile.ProfileName}, Platform={_ctx.Profile.Platform}");

            try
            {
                foreach (var stage in stages)
                {
                    if (_ctx.IsCancellationRequested)
                    {
                        Debug.LogWarning("[BuildPipelineRunner] Build cancelled by user");
                        _allPassed = false;
                        break;
                    }

                    var planEntry = _ctx.Plan.Entries.Find(e => e.StageId == stage.Id);
                    var result = ExecuteStage(stage, planEntry?.WillSkip ?? false);

                    if (!result.Passed && stage.Policy.HasFlag(BuildStagePolicy.Required))
                    {
                        _allPassed = false;
                        Debug.LogError($"[BuildPipelineRunner] Required stage failed: {stage.Id}, aborting");
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
            }
            finally
            {
                try { _ctx.Transaction.Rollback(); }
                catch (Exception ex) { Debug.LogError($"[BuildPipelineRunner] Rollback failed: {ex.Message}"); }
            }

            var report = BuildReport(
                telemetryCollector.Snapshot(),
                telemetryCollector.DroppedCount,
                telemetrySession.CollectorFailureCount);
            report.EnvironmentSnapshot = snapshot;
            WriteReports(report);
            VerifyReportFiles();
            Debug.Log($"[BuildTelemetry] Captured {report.PerformanceSpans.Count} spans, " +
                      $"dropped={report.PerformanceDroppedCount}, collectorFailures={report.PerformanceCollectorFailureCount}");
            return report;
        }

        // ===== Stage 执行 =====

        private StageExecutionResult ExecuteStage(IBuildStage stage, bool planSaysSkip)
        {
            Debug.Log($"[BuildPipelineRunner] === {stage.Id}: {stage.DisplayName} ===");
            long startedTimestamp = Stopwatch.GetTimestamp();

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
                result.DurationMs = ElapsedMilliseconds(startedTimestamp);
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
                if (decision.CanSkip)
                {
                    var current = ComputeStageFingerprint(stage, includeOutputs: false);
                    if (!current.Matches(previous))
                    {
                        decision = BuildSkipDecision.DoNotSkip("Input, tool, profile, or pipeline fingerprint changed");
                    }
                }
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
            string fingerprintPath = Path.Combine(_ctx.Paths.StateDir, $"{stageId}.fingerprint.json");
            if (!File.Exists(fingerprintPath)) return null;
            try
            {
                return JsonUtility.FromJson<BuildStageFingerprint>(File.ReadAllText(fingerprintPath));
            }
            catch { return null; }
        }

        private void WriteStageFingerprint(IBuildStage stage)
        {
            var fp = ComputeStageFingerprint(stage, includeOutputs: true);
            fp.CompletedAtUtc = DateTime.UtcNow.ToString("o");

            try
            {
                string fingerprintPath = Path.Combine(_ctx.Paths.StateDir, $"{stage.Id}.fingerprint.json");
                File.WriteAllText(fingerprintPath, JsonUtility.ToJson(fp, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildPipelineRunner] Failed to write fingerprint: {ex.Message}");
            }
        }

        private BuildStageFingerprint ComputeStageFingerprint(IBuildStage stage, bool includeOutputs)
        {
            var inputs = stage.GetInputs(_ctx);
            inputs.ProfileHash = _ctx.Profile.ComputeProfileHash();
            inputs.WithToolVersion("Unity", Application.unityVersion);

            var outputs = stage.GetExpectedOutputs(_ctx);

            return new BuildStageFingerprint
            {
                StageId = stage.Id,
                PipelineVersion = "1.0.0",
                StageVersion = 1,
                ProfileHash = inputs.ProfileHash,
                InputsHash = HashInputs(inputs),
                OutputsHash = includeOutputs ? HashOutputs(outputs) : "",
                ToolsHash = HashTools(inputs),
                UnityVersion = Application.unityVersion,
            };
        }

        private static string HashInputs(BuildStageInputs inputs)
        {
            var sb = new StringBuilder();
            sb.Append(inputs.ProfileHash).Append('\n');
            foreach (string path in inputs.SourcePaths.OrderBy(p => p))
                AppendPathFingerprint(sb, path);
            foreach (string dep in inputs.DependsOnStages.OrderBy(d => d))
                sb.Append("dep:").Append(dep).Append('\n');
            return Sha256(sb.ToString());
        }

        private static string HashOutputs(BuildStageOutputs outputs)
        {
            var sb = new StringBuilder();
            foreach (string file in outputs.RequiredFiles.OrderBy(p => p))
                AppendPathFingerprint(sb, file);
            foreach (string dir in outputs.RequiredDirectories.OrderBy(p => p))
                AppendPathFingerprint(sb, dir);
            return Sha256(sb.ToString());
        }

        private static string HashTools(BuildStageInputs inputs)
        {
            var sb = new StringBuilder();
            foreach (var kv in inputs.ToolVersions.OrderBy(kv => kv.Key))
                sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
            return Sha256(sb.ToString());
        }

        private static void AppendPathFingerprint(StringBuilder sb, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            string normalized = path.Replace('\\', '/');
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                sb.Append("file:").Append(normalized).Append('|')
                    .Append(fi.Length).Append('|')
                    .Append(fi.LastWriteTimeUtc.Ticks).Append('\n');
                return;
            }

            if (!Directory.Exists(path))
            {
                sb.Append("missing:").Append(normalized).Append('\n');
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                         .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(f => f.Replace('\\', '/')))
            {
                var fi = new FileInfo(file);
                sb.Append("file:").Append(file.Replace('\\', '/')).Append('|')
                    .Append(fi.Length).Append('|')
                    .Append(fi.LastWriteTimeUtc.Ticks).Append('\n');
            }
        }

        private static string Sha256(string text)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        // ===== 报告 =====

        private BuildReportData BuildReport(
            List<AopEvent> performanceSpans,
            int performanceDroppedCount,
            int performanceCollectorFailureCount)
        {
            return new BuildReportData
            {
                SchemaVersion = "1.1.0",
                RunId = _ctx.RunId,
                PipelineVersion = "1.0.0",
                ProfileName = _ctx.Profile?.ProfileName,
                Platform = _ctx.Profile?.Platform.ToString(),
                Environment = _ctx.Profile?.Environment.ToString(),
                Version = _ctx.Profile?.VersionName,
                BuildStartedAt = _ctx.StartedAtUtc.ToString("o"),
                BuildFinishedAt = DateTime.UtcNow.ToString("o"),
                TotalDurationMs = ElapsedMilliseconds(_buildStartedTimestamp),
                AllPassed = _allPassed,
                StageResults = _stageResults,
                Issues = _ctx.Issues,
                PerformanceSpans = performanceSpans,
                PerformanceDroppedCount = performanceDroppedCount,
                PerformanceCollectorFailureCount = performanceCollectorFailureCount,
            };
        }

        private static long ElapsedMilliseconds(long startedTimestamp)
        {
            long elapsedTicks = Math.Max(0, Stopwatch.GetTimestamp() - startedTimestamp);
            return (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency);
        }

        private void WriteReports(BuildReportData report)
        {
            new BuildReportWriter().WriteAll(report, _ctx.Paths);
        }

        private void VerifyReportFiles()
        {
            foreach (string file in new[]
            {
                Path.Combine(_ctx.Paths.ReportsDir, "build_report.json"),
                Path.Combine(_ctx.Paths.ReportsDir, "build_report.md"),
                Path.Combine(_ctx.Paths.ReportsDir, "ai_handoff.json"),
            })
            {
                if (!File.Exists(file) || new FileInfo(file).Length == 0)
                    throw new InvalidOperationException($"Build report was not written correctly: {file}");
            }
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

            if (report.PerformanceSpans != null && report.PerformanceSpans.Count > 0)
            {
                sw.WriteLine("## Performance Spans");
                sw.WriteLine($"Captured: {report.PerformanceSpans.Count}, Dropped: {report.PerformanceDroppedCount}, Collector failures: {report.PerformanceCollectorFailureCount}");
                sw.WriteLine();
                sw.WriteLine("| Operation | Category | Status | Duration | Exception |");
                sw.WriteLine("|-----------|----------|--------|----------|-----------|");
                foreach (var span in report.PerformanceSpans.OrderByDescending(s => s.DurationMs))
                {
                    sw.WriteLine($"| {span.Name} | {span.Category} | {span.Status} | {span.DurationMs:F2}ms | {span.ExceptionType ?? ""} |");
                }
                sw.WriteLine();
            }

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
                handoff.SuggestedActions.Add("Open KJ/Build/Dashboard and run a full build");
            }

            File.WriteAllText(path, JsonUtility.ToJson(handoff, true));
        }
    }

    // ===== 结果与报告类型 =====

    /// <summary>
    /// Stage 执行结果。
    /// </summary>
    /// <summary>
    /// Stage 执行结果。
    /// 注意：使用 public field 而非 property 是因为 UnityEngine.JsonUtility 不支持属性序列化。
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
    /// 构建报告顶层数据结构。
    /// 注意：使用 public field 而非 property 是因为 UnityEngine.JsonUtility 不支持属性序列化。
    /// </summary>
    [Serializable]
    public class BuildReportData
    {
        public string SchemaVersion = "1.1.0";
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
        public List<AopEvent> PerformanceSpans = new List<AopEvent>();
        public int PerformanceDroppedCount;
        public int PerformanceCollectorFailureCount;
    }
}
