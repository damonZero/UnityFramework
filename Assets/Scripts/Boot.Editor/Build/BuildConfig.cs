using UnityEngine;
using UnityEditor;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建配置 —— 驱动整个构建管线的可变参数。
    /// v1 字段覆盖：平台 / IL2CPP / development / 输出 / 冒烟。
    /// v2 预留：环境 / 签名 / 加密 / 分包（字段已声明但 v1 不实现逻辑）。
    /// </summary>
    public class BuildConfig : ScriptableObject
    {
        // ===== v1 字段 =====

        [Header("Platform")]
        [Tooltip("目标平台")]
        public BuildTarget Platform = BuildTarget.StandaloneWindows64;

        [Header("Build")]
        [Tooltip("是否为 Development 构建（冒烟=true，带符号/日志；发布=false）")]
        public bool Development = true;

        [Tooltip("产物版本号")]
        public string Version = "1.0.0";

        [Header("Asset")]
        [Tooltip("YooAsset 包名")]
        public string PackageName = "DefaultPackage";

        [Tooltip("资源下载 Tag")]
        public string AssetDownloadTag = "hotupdate";

        [Tooltip("启动类型全名")]
        public string StartupTypeName = "Project.Bootstrap.ProjectStartup, Project";

        [Tooltip("启动方法名")]
        public string StartupMethodName = "Start";

        [Header("Output")]
        [Tooltip("构建输出根目录（相对项目根），为空则自动推导为 Build/{Platform}")]
        public string OutputDir = "";

        [Header("Smoke")]
        [Tooltip("是否在构建后执行无头冒烟")]
        public bool SmokeEnabled = true;

        [Tooltip("冒烟超时（秒）")]
        public int SmokeTimeoutSec = 120;

        // ===== v2 预留字段（v1 仅声明，不实现逻辑）=====

        [Header("Extension (v2 reserved)")]
        [Tooltip("[v2] 构建环境：Dev / Profiling / Pre / Release")]
        public string BuildEnvironment = "Dev";

        [Tooltip("[v2] CDN 基础 URL")]
        public string CdnBaseUrl = "";

        // ---- 辅助方法 ----

        public string GetOutputDir()
        {
            if (!string.IsNullOrEmpty(OutputDir))
                return OutputDir;
            return $"Build/{Platform}";
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

        public string GetReportPath(string suffix = "")
        {
            string dir = GetOutputDir();
            string name = string.IsNullOrEmpty(suffix) ? "build_report" : $"build_report_{suffix}";
            return $"{dir}/{name}";
        }

        public string GetMarkerDir()
        {
            return $"{GetOutputDir()}/.markers";
        }

        [MenuItem("KJ/Build/Create BuildConfig")]
        private static void CreateBuildConfig()
        {
            var config = CreateInstance<BuildConfig>();
            string path = "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildConfig] Created at {path}");
        }
    }
}
