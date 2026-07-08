using System.IO;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 9 — 归档 + 报告生成。
    /// 输出 build_report.json + build_report.md。
    /// </summary>
    public static class StageReport
    {
        public static void Execute(BuildConfig config, BuildReport report)
        {
            Debug.Log("[S9] Report: Generating...");

            // 补充产物清单（文件或目录）
            string playerPath = config.GetPlayerPath();
            if (File.Exists(playerPath) || Directory.Exists(playerPath))
                report.AddArtifact(playerPath, "Player binary");

            string bundleDir = StageBuildYooAsset.LastOutputDirectory;
            if (string.IsNullOrEmpty(bundleDir))
            {
                // 回退
                string packageName = string.IsNullOrEmpty(config.PackageName)
                    ? "DefaultPackage" : config.PackageName;
                string streamingAssets = Application.streamingAssetsPath;
                if (Directory.Exists(streamingAssets))
                {
                    var versionFiles = Directory.GetFiles(streamingAssets,
                        $"{packageName}.version", SearchOption.AllDirectories);
                    if (versionFiles.Length > 0)
                        bundleDir = Path.GetDirectoryName(versionFiles[0]);
                }
            }
            if (Directory.Exists(bundleDir))
                report.AddArtifact(bundleDir, "YooAsset built-in bundle directory");

            // 写 JSON 报告
            string jsonPath = config.GetReportPath() + ".json";
            report.WriteJson(jsonPath);

            // 写 Markdown 报告
            string mdPath = config.GetReportPath() + ".md";
            report.WriteMarkdown(mdPath);

            Debug.Log($"[S9] JSON report: {jsonPath}");
            Debug.Log($"[S9] Markdown report: {mdPath}");

            Debug.Log("[S9] Report: DONE");
        }
    }
}
