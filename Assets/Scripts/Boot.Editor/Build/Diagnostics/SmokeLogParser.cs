using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 冒烟日志解析器 —— 多里程碑判定。
    /// 从 boot.log 和 latest.jsonl 中验证启动链各阶段完整性：
    /// Launcher → YooAsset → HybridCLR → Boot → Core → SystemManager。
    /// </summary>
    public class SmokeLogParser
    {
        /// <summary>解析结果</summary>
        public SmokeLogResult Evaluate(string bootLogPath, string runtimeLogPath,
            string[] requiredMilestones)
        {
            var result = new SmokeLogResult
            {
                RequiredCount = requiredMilestones?.Length ?? 0,
            };

            // 1. 读取日志
            var bootEntries = ReadBootLog(bootLogPath);
            var runtimeLines = ReadRuntimeLog(runtimeLogPath);

            result.TotalBootEntries = bootEntries.Count;
            result.TotalRuntimeLines = runtimeLines.Count;

            // 2. 错误检查：boot.log 不得出现 Error/Failed
            var bootErrors = bootEntries
                .Where(e => e.Contains("Error", StringComparison.OrdinalIgnoreCase)
                         || e.Contains("ERROR", StringComparison.Ordinal)
                         || e.Contains("Failed", StringComparison.OrdinalIgnoreCase)
                         || e.Contains("Exception", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (bootErrors.Count > 0)
            {
                result.Passed = false;
                result.HasBootErrors = true;
                result.BootErrors = bootErrors;
                result.FailureReason = $"boot.log contains {bootErrors.Count} error(s)";
                result.FirstError = bootErrors.FirstOrDefault();
                return result;
            }

            // 3. 里程碑判定
            if (requiredMilestones != null)
            {
                foreach (string milestone in requiredMilestones)
                {
                    bool inBoot = bootEntries.Any(l =>
                        l.Contains(milestone, StringComparison.OrdinalIgnoreCase));
                    bool inRuntime = runtimeLines.Any(l =>
                        l.Contains(milestone, StringComparison.OrdinalIgnoreCase));

                    if (inBoot || inRuntime)
                    {
                        result.FoundMilestones.Add(milestone);
                    }
                    else
                    {
                        result.MissingMilestones.Add(milestone);
                    }
                }
            }

            result.Passed = result.MissingMilestones.Count == 0
                && result.BootErrors.Count == 0;

            if (!result.Passed && result.MissingMilestones.Count > 0)
            {
                result.FailureReason = $"Missing milestones: {string.Join(", ", result.MissingMilestones)}";
            }

            return result;
        }

        private static List<string> ReadBootLog(string path)
        {
            var entries = new List<string>();
            try
            {
                if (File.Exists(path))
                    entries.AddRange(File.ReadAllLines(path));
            }
            catch { /* 文件不可读不是冒烟失败 */ }
            return entries;
        }

        private static List<string> ReadRuntimeLog(string path)
        {
            var entries = new List<string>();
            try
            {
                if (File.Exists(path))
                    entries.AddRange(File.ReadAllLines(path));
            }
            catch { }
            return entries;
        }
    }

    /// <summary>
    /// 冒烟日志解析结果
    /// </summary>
    public class SmokeLogResult
    {
        public bool Passed;
        public int RequiredCount;
        public int TotalBootEntries;
        public int TotalRuntimeLines;

        /// <summary>命中的里程碑</summary>
        public List<string> FoundMilestones = new List<string>();

        /// <summary>缺失的里程碑</summary>
        public List<string> MissingMilestones = new List<string>();

        /// <summary>boot.log 中的错误条目</summary>
        public bool HasBootErrors;
        public List<string> BootErrors = new List<string>();
        public string FirstError;

        /// <summary>失败原因（人类可读）</summary>
        public string FailureReason;
    }
}
