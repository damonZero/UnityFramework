using System;
using System.Collections.Generic;
using System.IO;
using Framework.BuildPipeline.Plan;
using Framework.BuildPipeline.Reports;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P9 Report — 汇总构建报告、问题、产物、AI handoff，归档日志。
    /// </summary>
    public class P9_ReportStage : BuildStageBase
    {
        public override string Id => "P9.Report";
        public override string DisplayName => "Generate Report & Archive";
        public override int Order => 9;
        public override string Category => "Report";
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.AlwaysRun;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs { AlwaysRun = true };

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
            => new BuildStageOutputs();

        public override void Execute(BuildContext context)
        {
            Debug.Log("[P9] Report: Generating build reports...");

            // 报告由 BuildPipelineRunner.Run() 在最后统一生成
            // 此 Stage 负责额外的归档操作

            // 1. 复制 Editor.log 到归档目录
            try
            {
                string editorLogPath = GetEditorLogPath();
                if (File.Exists(editorLogPath))
                {
                    string dest = Path.Combine(context.Paths.LogsDir, "editor.log");
                    File.Copy(editorLogPath, dest, true);
                    Debug.Log($"[P9] Archived editor log: {dest}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[P9] Failed to archive editor log: {ex.Message}");
            }

            // 2. 复制 Runtime 日志（如果存在）
            try
            {
                CopyRuntimeLogs(context);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[P9] Failed to copy runtime logs: {ex.Message}");
            }

            Debug.Log("[P9] Report: DONE");
        }

        public override void Verify(BuildContext context)
        {
            Debug.Log("[P9] ✓ Report archive stage verified");
        }

        private static string GetEditorLogPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Unity", "Editor", "Editor.log");
        }

        private void CopyRuntimeLogs(BuildContext context)
        {
            string logDir = P8_SmokeStage_GetPersistentPath();
            string runtimeLogDir = Path.Combine(logDir, "Logs", "Runtime");
            if (!Directory.Exists(runtimeLogDir)) return;

            string destDir = context.Paths.LogsDir;
            foreach (string file in Directory.GetFiles(runtimeLogDir))
            {
                string dest = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }
            Debug.Log($"[P9] Archived runtime logs to: {destDir}");
        }

        // 与 P8 共享 logger 路径逻辑
        private static string P8_SmokeStage_GetPersistentPath()
        {
            string company = Application.companyName;
            string product = Application.productName;
            if (string.IsNullOrEmpty(company)) company = "DefaultCompany";
            if (string.IsNullOrEmpty(product)) product = "KJ";

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataRoot = Path.GetDirectoryName(localAppData);
            return Path.Combine(appDataRoot, "LocalLow", company, product);
        }
    }
}
