using System;
using Framework.Asset;
using Framework.BuildPipeline.Environment;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建档案 —— 环境驱动的构建配置。
    /// 所有环境差异收敛到一个 Profile：Dev 可开 GM/调试，Formal 全禁。
    /// 构建管线唯一配置源：环境、平台、签名、日志、冒烟和输出策略。
    /// </summary>
    public class BuildProfile : ScriptableObject
    {
        // ===== Identity =====

        [Header("Identity")]
        [Tooltip("Odin 列表展示名称")]
        public string ProfileName = "New Profile";

        [Tooltip("构建环境")]
        public BuildEnvironment Environment = BuildEnvironment.Dev;

        [Tooltip("渠道名，如 internal / googleplay / tap")]
        public string Channel = "internal";

        // ===== Version =====

        [Header("Version")]
        [Tooltip("语义版本")]
        public string VersionName = "1.0.0";

        [Tooltip("Android versionCode / iOS build number")]
        public int VersionCode = 1;

        // ===== Platform =====

        [Header("Platform")]
        [Tooltip("目标平台")]
        public BuildTarget Platform = BuildTarget.StandaloneWindows64;

        // ===== Android =====

        [Header("Android")]
        [Tooltip("Application Identifier")]
        public string PackageId = "";

        [Tooltip("Keystore 路径（Formal/Audit 必填）")]
        public string KeystorePath = "";

        [Tooltip("Keystore 别名")]
        public string KeystoreAlias = "";

        [Tooltip("Keystore 密码（建议 CI 环境变量注入，勿硬编码）")]
        public string KeystorePassword = "";

        // ===== Build =====

        [Header("Build")]
        [Tooltip("是否为 Development Build")]
        public bool DevelopmentBuild = true;

        [Tooltip("是否允许脚本调试")]
        public bool ScriptDebugging = false;

        [Tooltip("是否开启 Profiler")]
        public bool EnableProfiler = false;

        [Tooltip("额外 Scripting Define Symbols")]
        public string[] ExtraScriptingDefines = Array.Empty<string>();

        // ===== YooAsset =====

        [Header("YooAsset")]
        [Tooltip("包名")]
        public string PackageName = "DefaultPackage";

        [Tooltip("资源下载 Tag")]
        public string AssetDownloadTag = "hotupdate";

        [Tooltip("启动类型全名")]
        public string StartupTypeName = "Project.Bootstrap.ProjectStartup, Project";

        [Tooltip("启动方法名")]
        public string StartupMethodName = "Start";

        [Tooltip("CDN 基础 URL（Host 模式必填）")]
        public string CdnBaseUrl = "";

        [Tooltip("Player 运行时资源模式")]
        public AssetConfig.PlayMode AssetMode = AssetConfig.PlayMode.Offline;

        // ===== Logging =====

        [Header("Logging")]
        [Tooltip("是否启用 RuntimeLog (JSONL)")]
        public bool EnableRuntimeLog = true;

        // ===== Feature Flags =====

        [Header("Feature Flags")]
        [Tooltip("GM 开关")]
        public bool EnableGm = false;

        [Tooltip("Debug UI 开关")]
        public bool EnableDebugUi = false;

        // ===== Smoke =====

        [Header("Smoke")]
        [Tooltip("是否调度冒烟")]
        public bool SmokeEnabled = true;

        [Tooltip("冒烟是否不允许跳过（Formal/Audit 必须）")]
        public bool SmokeRequired = false;

        [Tooltip("ADB 设备序列号（空则自动选取）")]
        public string SmokeDeviceSerial = "";

        [Tooltip("冒烟超时（秒）")]
        public int SmokeTimeoutSec = 120;

        // ===== Output =====

        [Header("Output")]
        [Tooltip("构建输出根目录（相对项目根），为空则自动推导")]
        public string OutputRoot = "";

        [Tooltip("本地保留最近构建数量")]
        public int KeepLastBuildCount = 5;

        // ---- 派生属性 ----

        /// <summary>是否需要签名</summary>
        public bool RequireSigning =>
            Environment == BuildEnvironment.Formal || Environment == BuildEnvironment.Audit;

        /// <summary>冒烟是否必须跑且不可跳过</summary>
        public bool IsSmokeMandatory =>
            SmokeEnabled && (SmokeRequired || Environment == BuildEnvironment.Formal
                || Environment == BuildEnvironment.Audit);

        // ---- 辅助方法 ----

        public string GetOutputDir()
        {
            if (!string.IsNullOrEmpty(OutputRoot))
                return OutputRoot;
            return $"BuildBackup/{Environment}/{VersionName}/{VersionCode}";
        }

        public string GetPlayerPath()
        {
            string dir = GetOutputDir();
            string ext = Platform switch
            {
                BuildTarget.StandaloneWindows64 => ".exe",
                BuildTarget.StandaloneWindows => ".exe",
                BuildTarget.Android => ".apk",
                BuildTarget.iOS => ".ipa",
                _ => ""
            };
            return $"{dir}/KJ{ext}";
        }

        public string GetReportDir() => $"{GetOutputDir()}/reports";

        public string GetArchiveDir() => $"{GetOutputDir()}/artifacts";

        public string GetLogsDir() => $"{GetOutputDir()}/logs";

        public string GetStateDir() => $"{GetOutputDir()}/state";

        /// <summary>
        /// 生成不可变快照哈希 —— 用于指纹计算。
        /// 包含所有影响构建结果的 Profile 字段。
        /// </summary>
        public string ComputeProfileHash()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Environment); sb.Append('|');
            sb.Append(Platform); sb.Append('|');
            sb.Append(VersionName); sb.Append('|');
            sb.Append(VersionCode); sb.Append('|');
            sb.Append(DevelopmentBuild); sb.Append('|');
            sb.Append(ScriptDebugging); sb.Append('|');
            sb.Append(EnableProfiler); sb.Append('|');
            sb.Append(EnableGm); sb.Append('|');
            sb.Append(EnableDebugUi); sb.Append('|');
            sb.Append(EnableRuntimeLog); sb.Append('|');
            sb.Append(PackageName); sb.Append('|');
            sb.Append(Channel); sb.Append('|');
            sb.Append(PackageId); sb.Append('|');
            // Runtime asset config is fingerprinted by P5 through the profile asset itself.
            // Keep this slot stable so CDN-only changes do not invalidate P2/P3/P4.
            sb.Append(""); sb.Append('|');
            sb.Append(SmokeRequired);
            foreach (var d in ExtraScriptingDefines ?? Array.Empty<string>())
                { sb.Append('|'); sb.Append(d); }

            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

    }
}
