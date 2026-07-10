using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建报告 —— 机器可读 + 人读摘要。
    /// 记录每个 Stage 的耗时/状态/不变量，以及产物清单和冒烟结论。
    /// </summary>
    [Serializable]
    public class BuildReport
    {
        // 元信息
        public string pipelineVersion = "1.0.0";
        public string buildStartedAt;
        public string buildFinishedAt;
        public string totalDuration;

        // 配置摘要
        public string platform;
        public bool development;
        public string packageName;
        public string version;

        // 阶段结果
        public List<StageResult> stages = new List<StageResult>();

        // 产物清单
        public List<ArtifactEntry> artifacts = new List<ArtifactEntry>();

        // 冒烟结论
        public SmokeConclusion smoke;

        // 总体结论
        public BuildSummary summary = new BuildSummary();

        /// <summary>
        /// 添加阶段结果
        /// </summary>
        public StageResult AddStage(string name)
        {
            var s = new StageResult { name = name, startedAt = DateTime.Now.ToString("o") };
            stages.Add(s);
            return s;
        }

        /// <summary>
        /// 添加产物条目。同时支持文件与目录：
        /// 目录会计算其下所有文件的总大小（sha256 留空，因为目录无单一哈希）。
        /// </summary>
        public void AddArtifact(string path, string description = "")
        {
            string fullPath = Path.GetFullPath(path);
            bool isFile = File.Exists(fullPath);
            bool isDir = Directory.Exists(fullPath);
            bool exists = isFile || isDir;

            long size = 0;
            string sha = "";
            if (isFile)
            {
                size = new FileInfo(fullPath).Length;
                sha = ComputeSha256(fullPath);
            }
            else if (isDir)
            {
                size = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)
                    .Sum(f =>
                    {
                        try { return new FileInfo(f).Length; }
                        catch { return 0L; }
                    });
                sha = "";
            }

            var entry = new ArtifactEntry
            {
                path = path,
                description = description,
                exists = exists,
                sizeBytes = size,
                sha256 = sha
            };

            artifacts.Add(entry);
        }

        /// <summary>
        /// 写入 JSON 报告
        /// </summary>
        public void WriteJson(string path)
        {
            buildFinishedAt = DateTime.Now.ToString("o");
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
            Debug.Log($"[BuildReport] JSON report written to {path}");
        }

        /// <summary>
        /// 写入人读 Markdown 报告
        /// </summary>
        public void WriteMarkdown(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var sw = new StreamWriter(path);
            sw.WriteLine("# KJ Build Pipeline Report");
            sw.WriteLine();
            sw.WriteLine($"- **Platform**: {platform}");
            sw.WriteLine($"- **Package**: {packageName}");
            sw.WriteLine($"- **Version**: {version}");
            sw.WriteLine($"- **Development**: {development}");
            sw.WriteLine($"- **Duration**: {totalDuration}");
            sw.WriteLine();

            sw.WriteLine("## Stage Results");
            sw.WriteLine();
            sw.WriteLine("| Stage | Status | Duration |");
            sw.WriteLine("|-------|--------|----------|");
            foreach (var s in stages)
            {
                sw.WriteLine($"| {s.name} | {(s.passed ? "PASS" : "FAIL")} | {s.durationSec:F1}s |");
            }
            sw.WriteLine();

            if (smoke != null && smoke.enabled)
            {
                sw.WriteLine("## Smoke Test");
                sw.WriteLine($"- **Result**: {smoke.result}");
                sw.WriteLine($"- **Log Path**: {smoke.logPath}");
                sw.WriteLine();
            }

            sw.WriteLine("## Summary");
            sw.WriteLine($"- **Overall**: {(summary.allPassed ? "SUCCESS" : "FAILED")}");
            if (!string.IsNullOrEmpty(summary.failedStage))
                sw.WriteLine($"- **Failed at**: {summary.failedStage}");
            if (!string.IsNullOrEmpty(summary.errorMessage))
                sw.WriteLine($"- **Error**: {summary.errorMessage}");

            Debug.Log($"[BuildReport] Markdown report written to {path}");
        }

        private static string ComputeSha256(string filePath)
        {
            try
            {
                using var sha = SHA256.Create();
                using var fs = File.OpenRead(filePath);
                byte[] hash = sha.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return "ERROR";
            }
        }
    }

    [Serializable]
    public class StageResult
    {
        public string name;
        public string startedAt;
        public string finishedAt;
        public float durationSec;
        public bool passed;
        public bool skipped;
        public string skipReason;
        public string errorMessage;
        public List<string> invariants = new List<string>();
    }

    [Serializable]
    public class ArtifactEntry
    {
        public string path;
        public string description;
        public bool exists;
        public long sizeBytes;
        public string sha256;
    }

    [Serializable]
    public class SmokeConclusion
    {
        /// <summary>冒烟是否被配置为启用（Stage 8 是否被调度）。</summary>
        public bool enabled;

        /// <summary>冒烟执行结果枚举 —— 区分"未运行 / 跳过 / 通过 / 失败"。</summary>
        public SmokeResult result;

        /// <summary>
        /// [已废弃] 保留以兼容旧报告读取。新代码请用 <see cref="result"/>。
        /// </summary>
        [System.Obsolete("Use result instead.")]
        public bool passed
        {
            get => result == SmokeResult.Passed;
            set => result = value ? SmokeResult.Passed : SmokeResult.Failed;
        }

        public string bootLogPath;
        public string runtimeLogPath;
        public List<string> milestonesFound = new List<string>();
        public List<string> errorsFound = new List<string>();
        public string logPath;
    }

    /// <summary>
    /// 冒烟执行结果 —— 区分"未运行 / 跳过 / 通过 / 失败"，
    /// 避免把"没跑"误报为"通过"。
    /// </summary>
    public enum SmokeResult
    {
        /// <summary>冒烟未被调度（SmokeEnabled=false）。</summary>
        NotScheduled,
        /// <summary>因平台/环境原因跳过（Android 需 adb，Export Project 不可直接执行等）。</summary>
        Skipped,
        /// <summary>冒烟执行且判定成功。</summary>
        Passed,
        /// <summary>冒烟执行但判定失败。</summary>
        Failed,
    }

    [Serializable]
    public class BuildSummary
    {
        public bool allPassed;
        public string failedStage;
        public string errorMessage;
        public int stagesPassed;
        public int stagesFailed;
        public int stagesSkipped;
    }
}
