using System.Collections.Generic;
using System.Linq;
using Framework.BuildPipeline.Diagnostics;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建分析器 —— 在构建结束后对所有 Issue 进行分类、优先级排序、
    /// 合并重复问题，并给出综合诊断。
    /// </summary>
    public class BuildAnalyzer
    {
        /// <summary>
        /// 分析构建后的问题列表，输出诊断摘要。
        /// </summary>
        public BuildAnalysisResult Analyze(List<BuildIssue> issues)
        {
            var result = new BuildAnalysisResult
            {
                TotalIssues = issues.Count,
                ErrorCount = issues.Count(i => i.Severity == BuildIssueSeverity.Error),
                WarningCount = issues.Count(i => i.Severity == BuildIssueSeverity.Warning),
                InfoCount = issues.Count(i => i.Severity == BuildIssueSeverity.Info),
                BlockingCount = issues.Count(i => i.IsBlocking),
            };

            // 按严重级别排序
            result.CriticalIssues = issues
                .Where(i => i.Severity == BuildIssueSeverity.Error && i.IsBlocking)
                .ToList();

            // 按 Stage 分组
            result.IssuesByStage = issues
                .GroupBy(i => i.StageId ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.ToList());

            // 生成建议
            result.Recommendations = GenerateRecommendations(issues);

            return result;
        }

        private static List<string> GenerateRecommendations(List<BuildIssue> issues)
        {
            var recs = new List<string>();

            var blocking = issues.Where(i => i.IsBlocking && i.Severity == BuildIssueSeverity.Error).ToList();
            if (blocking.Count > 0)
            {
                recs.Add($"{blocking.Count} blocking error(s) must be resolved first.");
                foreach (var issue in blocking)
                {
                    if (!string.IsNullOrEmpty(issue.SuggestedFix))
                        recs.Add($"  [{issue.Code}] {issue.SuggestedFix}");
                    else
                        recs.Add($"  [{issue.Code}] {issue.Message}");
                }
            }

            // Preflight 失败 → 检查环境
            if (issues.Any(i => i.StageId?.Contains("Preflight") == true && i.IsBlocking))
            {
                recs.Add("Preflight checks failed — verify Unity version, platform modules, and project settings.");
            }

            // HybridCLR 失败 → 检查编译
            if (issues.Any(i => i.StageId?.Contains("HybridCLR") == true && i.IsBlocking))
            {
                recs.Add("HybridCLR stage failed — check compilation errors and assembly count.");
            }

            // Player 构建失败 → 检查 IL2CPP 和平台工具链
            if (issues.Any(i => i.StageId?.Contains("Player") == true && i.IsBlocking))
            {
                recs.Add("Player build failed — check IL2CPP backend, platform modules, and Gradle/Xcode versions.");
            }

            if (recs.Count == 0)
                recs.Add("No critical issues found. Review warnings for optimization opportunities.");

            return recs;
        }
    }

    /// <summary>
    /// 构建分析结果
    /// </summary>
    public class BuildAnalysisResult
    {
        public int TotalIssues;
        public int ErrorCount;
        public int WarningCount;
        public int InfoCount;
        public int BlockingCount;

        /// <summary>阻断性问题（最紧急）</summary>
        public List<BuildIssue> CriticalIssues = new List<BuildIssue>();

        /// <summary>按 Stage 分组的问题</summary>
        public Dictionary<string, List<BuildIssue>> IssuesByStage =
            new Dictionary<string, List<BuildIssue>>();

        /// <summary>建议的修复步骤</summary>
        public List<string> Recommendations = new List<string>();

        /// <summary>构建是否可继续（无阻断性 Error）</summary>
        public bool CanContinue => CriticalIssues.Count == 0;
    }
}
