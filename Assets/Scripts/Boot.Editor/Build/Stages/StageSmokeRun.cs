using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 8 — 无头运行冒烟。
    /// 启动 Player → 抓 boot.log + latest.jsonl → 判定 Boot→ProjectStartup 成功。
    /// </summary>
    public static class StageSmokeRun
    {
        public static void Execute(BuildConfig config, BuildReport report)
        {
            Debug.Log("[S8] SmokeRun: Starting...");

            report.smoke = new SmokeConclusion { enabled = true };

            // Android 需要 adb，暂不支持 Process.Start 直接启动 .apk
            if (config.Platform == BuildTarget.Android)
            {
                Debug.LogWarning("[S8] SmokeRun: Android platform — smoke test requires adb. " +
                    "Skipping process-based smoke; verify manually or via CI device farm.");
                report.smoke.result = SmokeResult.Skipped;
                report.smoke.logPath = "(skipped — Android requires adb)";
                return;
            }

            string playerPath = config.GetPlayerPath();
            if (!File.Exists(playerPath) && !Directory.Exists(playerPath))
            {
                throw new BuildFailedException("S8_SmokeRun",
                    $"Player not found at: {playerPath}");
            }

            // 目录输出（Export Project）不可直接执行 Process.Start
            if (Directory.Exists(playerPath))
            {
                Debug.LogWarning("[S8] SmokeRun: Export Project output is a directory, " +
                    "cannot execute directly. Skipping smoke.");
                report.smoke.result = SmokeResult.Skipped;
                report.smoke.logPath = "(skipped — Export Project directory)";
                return;
            }

            // 确定日志路径
            string persistentDataPath = GetPersistentDataPath();
            string logDir = Path.Combine(persistentDataPath, "Logs", "Runtime");
            string bootLogPath = Path.Combine(logDir, "boot.log");
            string runtimeLogPath = Path.Combine(logDir, "latest.jsonl");

            report.smoke.bootLogPath = bootLogPath;
            report.smoke.runtimeLogPath = runtimeLogPath;

            Debug.Log($"[S8] Player: {playerPath}");
            Debug.Log($"[S8] Expected log dir: {logDir}");

            // 清理旧日志（避免读到上轮结果）
            if (Directory.Exists(logDir))
            {
                foreach (string f in Directory.GetFiles(logDir))
                    File.Delete(f);
                Debug.Log("[S8] Cleaned old logs");
            }

            // 启动 Player
            int exitCode = -1;
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = playerPath,
                    Arguments = "-batchmode -nographics",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null)
                {
                    throw new BuildFailedException("S8_SmokeRun", "Failed to start player process");
                }

                Debug.Log($"[S8] Player started, PID={process.Id}, timeout={config.SmokeTimeoutSec}s");

                // 等待完成或超时
                bool finished = process.WaitForExit(config.SmokeTimeoutSec * 1000);
                if (finished)
                {
                    exitCode = process.ExitCode;
                    Debug.Log($"[S8] Player exited with code: {exitCode}");
                }
                else
                {
                    process.Kill();
                    Debug.LogWarning($"[S8] Player timed out after {config.SmokeTimeoutSec}s, killed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[S8] Process exception: {ex.Message}");
            }

            // 解析日志
            var bootLogEntries = ReadBootLog(bootLogPath);
            var runtimeLogLines = ReadRuntimeLog(runtimeLogPath);

            Debug.Log($"[S8] boot.log lines: {bootLogEntries.Count}");
            Debug.Log($"[S8] latest.jsonl lines: {runtimeLogLines.Count}");

            // 判定：boot.log 无 AOT 错误
            var bootErrors = bootLogEntries
                .Where(e => e.Contains("Error", StringComparison.OrdinalIgnoreCase)
                         || e.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
                         || e.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (bootErrors.Count > 0)
            {
                report.smoke.errorsFound = bootErrors;
                throw new BuildFailedException("S8_SmokeRun",
                    $"AOT errors in boot.log ({bootErrors.Count} errors). " +
                    $"First error: {bootErrors.FirstOrDefault()}");
            }

            // 判定：latest.jsonl 含成功里程碑
            string[] expectedMilestones = {
                "[Boot] Starting game",
                "ProjectStartup",
                "ProjectLifetimeScope",
                "BOOT_OK",
                "PROJECTSTARTUP_OK"
            };

            var milestonesFound = new List<string>();
            foreach (string milestone in expectedMilestones)
            {
                foreach (string line in runtimeLogLines)
                {
                    if (line.Contains(milestone, StringComparison.OrdinalIgnoreCase))
                    {
                        milestonesFound.Add(milestone);
                        break;
                    }
                }
            }

            report.smoke.milestonesFound = milestonesFound;
            report.smoke.logPath = logDir;

            Debug.Log($"[S8] Milestones found: {milestonesFound.Count}/{expectedMilestones.Length}");

            bool smokePassed = bootErrors.Count == 0 && milestonesFound.Count > 0;
            report.smoke.result = smokePassed ? SmokeResult.Passed : SmokeResult.Failed;

            if (!smokePassed)
            {
                string detail = milestonesFound.Count == 0
                    ? "No success milestones found in latest.jsonl"
                    : $"Only {milestonesFound.Count}/{expectedMilestones.Length} milestones found";
                throw new BuildFailedException("S8_SmokeRun", detail);
            }

            Debug.Log("[S8] SmokeRun: PASSED");
        }

        private static List<string> ReadBootLog(string path)
        {
            var entries = new List<string>();
            try
            {
                if (File.Exists(path))
                    entries.AddRange(File.ReadAllLines(path));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[S8] Cannot read boot.log: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                Debug.LogWarning($"[S8] Cannot read latest.jsonl: {ex.Message}");
            }
            return entries;
        }

        private static string GetPersistentDataPath()
        {
            // Player 下: Application.persistentDataPath
            // Win Standalone: %USERPROFILE%/AppData/LocalLow/<company>/<product>/
            // Android: /storage/emulated/0/Android/data/<package>/files/
            string company = Application.companyName;
            string product = Application.productName;

            if (string.IsNullOrEmpty(company)) company = "DefaultCompany";
            if (string.IsNullOrEmpty(product)) product = "KJ";

            // LocalLow = 从 Local 向上一级再进 LocalLow
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // localAppData = C:\Users\xxx\AppData\Local
            // LocalLow = C:\Users\xxx\AppData\LocalLow
            string appDataRoot = Path.GetDirectoryName(localAppData); // ...\AppData
            string localLow = Path.Combine(appDataRoot, "LocalLow");
            return Path.Combine(localLow, company, product);
        }
    }
}
