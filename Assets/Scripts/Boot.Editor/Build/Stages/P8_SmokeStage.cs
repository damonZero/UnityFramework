using System;
using System.Collections.Generic;
using System.IO;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P8 Smoke — 运行时冒烟测试（多里程碑判定）。
    /// 支持：Standalone (Process.Start) / Android (ADB)。
    /// 里程碑：Launcher → YooAsset → HybridCLR → Boot → Core → SystemManager。
    /// </summary>
    public class P8_SmokeStage : BuildStageBase
    {
        /// <summary>必须全部命中的启动链里程碑</summary>
        private static readonly string[] RequiredMilestones =
        {
            "[BootLoader] YooAsset",
            "[BootLoader] all DLLs loaded",
            "[AssetSystem] Ready",
            "[SystemManager]",
        };

        public override string Id => "P8.Smoke";
        public override string DisplayName => "Runtime Smoke Test";
        public override int Order => 8;
        public override string Category => "Verify";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P7.Verify" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.AlwaysRun;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs { AlwaysRun = true };

        public override void Execute(BuildContext context)
        {
            var profile = context.Profile;
            var config = context.Config;

            bool smokeEnabled = profile?.SmokeEnabled ?? config?.SmokeEnabled ?? true;
            if (!smokeEnabled)
            {
                Debug.Log("[P8] Smoke disabled in config, skipping");
                return;
            }

            bool smokeRequired = profile?.IsSmokeMandatory ?? false;

            var buildTarget = profile?.Platform ?? config?.Platform ?? BuildTarget.StandaloneWindows64;
            Debug.Log($"[P8] Smoke: Testing on {buildTarget} (required={smokeRequired})...");

            if (buildTarget == BuildTarget.Android)
            {
                RunAndroidSmoke(context, profile, config);
            }
            else
            {
                RunStandaloneSmoke(context, profile, config);
            }
        }

        // ===== Standalone Smoke =====

        private void RunStandaloneSmoke(BuildContext context, BuildProfile profile, BuildConfig config)
        {
            string playerPath = profile?.GetPlayerPath() ?? config?.GetPlayerPath()
                ?? "Build/StandaloneWindows64/KJ.exe";

            if (Directory.Exists(playerPath))
            {
                Debug.LogWarning("[P8] Export Project — cannot execute, skipping smoke");
                return;
            }
            if (!File.Exists(playerPath))
                throw new BuildFailedException(Id, $"Player not found: {playerPath}");

            string logDir = GetPersistentDataPath();
            string bootLogPath = Path.Combine(logDir, "Logs", "Runtime", "boot.log");
            string runtimeLogPath = Path.Combine(logDir, "Logs", "Runtime", "latest.jsonl");

            // 清理旧日志
            string runtimeLogDir = Path.GetDirectoryName(bootLogPath);
            if (Directory.Exists(runtimeLogDir))
            {
                foreach (string f in Directory.GetFiles(runtimeLogDir))
                    File.Delete(f);
            }

            int timeout = profile?.SmokeTimeoutSec ?? config?.SmokeTimeoutSec ?? 120;
            Debug.Log($"[P8] Starting: {playerPath}, timeout={timeout}s");

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
                    throw new BuildFailedException(Id, "Failed to start Player process");

                bool finished = process.WaitForExit(timeout * 1000);
                if (!finished)
                {
                    process.Kill();
                    Debug.LogWarning($"[P8] Player timed out after {timeout}s");
                }
                else
                {
                    Debug.Log($"[P8] Player exited with code: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[P8] Process error: {ex.Message}");
            }

            // 读取日志并判定
            var parser = new SmokeLogParser();
            var result = parser.Evaluate(bootLogPath, runtimeLogPath, RequiredMilestones);

            if (!result.Passed)
            {
                string detail = result.FailureReason ?? "Unknown";
                throw new BuildFailedException(Id, $"Smoke FAILED: {detail}");
            }

            Debug.Log($"[P8] Smoke PASSED: {result.FoundMilestones.Count}/{result.RequiredCount} milestones");
        }

        // ===== Android Smoke =====

        private void RunAndroidSmoke(BuildContext context, BuildProfile profile, BuildConfig config)
        {
            string adb = FindAdb();
            if (adb == null)
            {
                Debug.LogWarning("[P8] Smoke: adb not found, skipping Android smoke");
                return;
            }

            string device = ResolveDevice(adb, profile?.SmokeDeviceSerial ?? config?.SmokeDeviceSerial);
            if (device == null)
            {
                Debug.LogWarning("[P8] Smoke: no online Android device, skipping");
                return;
            }

            int timeout = profile?.SmokeTimeoutSec ?? config?.SmokeTimeoutSec ?? 120;
            string packageId = PlayerSettings.applicationIdentifier;
            if (string.IsNullOrEmpty(packageId)) packageId = "com.DefaultCompany.KJ";

            string playerPath = profile?.GetPlayerPath() ?? config?.GetPlayerPath() ?? "Build/Android/KJ.apk";
            if (File.Exists(playerPath))
                RunAdb(adb, device, $"install -r -d \"{playerPath}\"", timeout);

            // 清理旧日志
            RunAdb(adb, device, $"shell rm -rf /sdcard/Android/data/{packageId}/files/Logs", timeout);

            // 启动应用
            string launchActivity = $"{packageId}/com.unity3d.player.UnityPlayerActivity";
            RunAdb(adb, device,
                $"shell am start -n {launchActivity} -a android.intent.action.MAIN -c android.intent.category.LAUNCHER",
                timeout);

            // 轮询拉取日志
            string localLogDir = Path.Combine(Path.GetTempPath(), $"KJSmoke_{device}_{DateTime.Now:yyyyMMddHHmmss}");
            Directory.CreateDirectory(localLogDir);

            bool booted = false;
            int waitedMs = 0;
            int maxWaitMs = timeout * 1000;
            int pollMs = 2000;
            while (waitedMs < maxWaitMs)
            {
                System.Threading.Thread.Sleep(pollMs);
                waitedMs += pollMs;
                RunAdb(adb, device,
                    $"pull /sdcard/Android/data/{packageId}/files/Logs/Runtime \"{localLogDir}\"", 30);
                if (File.Exists(Path.Combine(localLogDir, "boot.log")))
                {
                    booted = true;
                    System.Threading.Thread.Sleep(3000);
                    RunAdb(adb, device,
                        $"pull /sdcard/Android/data/{packageId}/files/Logs/Runtime \"{localLogDir}\"", 30);
                    break;
                }
            }

            if (!booted)
                throw new BuildFailedException(Id, $"boot.log not produced on device {device} within {timeout}s");

            string bootLogPath = Path.Combine(localLogDir, "boot.log");
            string runtimeLogPath = Path.Combine(localLogDir, "latest.jsonl");

            var parser = new SmokeLogParser();
            var result = parser.Evaluate(bootLogPath, runtimeLogPath, RequiredMilestones);

            if (!result.Passed)
                throw new BuildFailedException(Id, $"Android smoke FAILED: {result.FailureReason}");

            Debug.Log($"[P8] Android smoke PASSED: {result.FoundMilestones.Count}/{result.RequiredCount} milestones");
        }

        private static string GetPersistentDataPath()
        {
            string company = Application.companyName;
            string product = Application.productName;
            if (string.IsNullOrEmpty(company)) company = "DefaultCompany";
            if (string.IsNullOrEmpty(product)) product = "KJ";

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataRoot = Path.GetDirectoryName(localAppData);
            string localLow = Path.Combine(appDataRoot, "LocalLow");
            return Path.Combine(localLow, company, product);
        }

        // ===== ADB 辅助 =====

        private static string FindAdb()
        {
            foreach (string env in new[] { "ANDROID_SDK_ROOT", "ANDROID_HOME" })
            {
                string sdk = Environment.GetEnvironmentVariable(env);
                if (!string.IsNullOrEmpty(sdk))
                {
                    string p = Path.Combine(sdk, "platform-tools", "adb.exe");
                    if (File.Exists(p)) return p;
                }
            }
            string unitySdk = EditorPrefs.GetString("AndroidSdkRoot", "");
            if (!string.IsNullOrEmpty(unitySdk))
            {
                string p = Path.Combine(unitySdk, "platform-tools", "adb.exe");
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static string ResolveDevice(string adb, string preferredSerial)
        {
            if (!string.IsNullOrEmpty(preferredSerial)) return preferredSerial;
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = adb, Arguments = "devices",
                UseShellExecute = false, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null) return null;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            foreach (string raw in output.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("List") || line.Length == 0) continue;
                if (line.EndsWith("device")) return line.Split('\t')[0].Trim();
            }
            return null;
        }

        private static int RunAdb(string adb, string device, string args, int timeoutSec)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = adb, Arguments = $"-s {device} {args}",
                    UseShellExecute = false, RedirectStandardOutput = true,
                    RedirectStandardError = true, CreateNoWindow = true,
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return -1;
                bool finished = p.WaitForExit(timeoutSec * 1000);
                if (!finished) { p.Kill(); return -2; }
                return p.ExitCode;
            }
            catch { return -1; }
        }
    }
}
