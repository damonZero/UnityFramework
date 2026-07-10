using System;
using System.Collections.Generic;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建环境快照 —— 记录 Unity、Git、OS、SDK 等版本信息。
    /// 每次构建开始时采集，写入构建报告。
    /// </summary>
    public class BuildEnvironmentSnapshot
    {
        public string UnityVersion;
        public string ProjectPath;
        public string MachineName;
        public string OsVersion;
        public string GitBranch;
        public string GitCommit;
        public string GitCommitShort;
        public bool IsDirty;

        /// <summary>HybridCLR 版本</summary>
        public string HybridCLRVersion;

        /// <summary>YooAsset 版本</summary>
        public string YooAssetVersion;

        /// <summary>采集时间 UTC</summary>
        public string CapturedAtUtc = DateTime.UtcNow.ToString("o");

        /// <summary>捕获当前环境快照</summary>
        public static BuildEnvironmentSnapshot Capture()
        {
            var snapshot = new BuildEnvironmentSnapshot
            {
                UnityVersion = UnityEngine.Application.unityVersion,
                ProjectPath = Environment.CurrentDirectory,
                MachineName = Environment.MachineName,
                OsVersion = Environment.OSVersion.ToString(),
            };

            // Git 信息
            try
            {
                snapshot.GitBranch = RunGitShort("rev-parse --abbrev-ref HEAD");
                snapshot.GitCommit = RunGitShort("rev-parse HEAD");
                snapshot.GitCommitShort = RunGitShort("rev-parse --short HEAD");
                string status = RunGitShort("status --porcelain");
                snapshot.IsDirty = !string.IsNullOrEmpty(status);
            }
            catch
            {
                // Git 不可用不是构建失败的原因
            }

            // HybridCLR 版本
            try
            {
                var hybridCLRType = Type.GetType("HybridCLR.Editor.BuildConfig, HybridCLR.Editor");
                if (hybridCLRType != null)
                {
                    var verField = hybridCLRType.GetField("HybridCLRVersion");
                    if (verField != null)
                        snapshot.HybridCLRVersion = verField.GetValue(null)?.ToString() ?? "unknown";
                }
            }
            catch
            {
                snapshot.HybridCLRVersion = "unknown";
            }

            return snapshot;
        }

        private static string RunGitShort(string args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null) return "";
            string output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(3000);
            return output;
        }
    }
}
