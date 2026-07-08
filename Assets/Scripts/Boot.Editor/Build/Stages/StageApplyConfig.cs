using System.IO;
using System.Text.RegularExpressions;
using Framework.Asset;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 5 — 写 Entry 启动配置 + 设 AssetConfig.Mode=Offline。
    /// 构建后自动回滚（见 RollbackAssetConfig）。
    /// </summary>
    public static class StageApplyConfig
    {
        private const string AssetConfigPath = "Assets/Resources/AssetConfig.asset";
        // Group 1 = prefix "  Mode: ", Group 2 = digits "0"/"1"/"2"
        private static readonly Regex ModeFieldRegex = new Regex(@"^(\s*Mode:\s*)(\d+)", RegexOptions.Multiline);

        private static bool s_modeChanged = false;
        private static AssetConfig.PlayMode s_originalMode;

        // ================================================================
        // Editor 启动安全网：若上一次构建被进程崩溃/杀进程中断，
        // RollbackAssetConfig 未执行，AssetConfig.Mode 可能停留在 Offline。
        // 这会导致开发者下次 Editor Play 失败（YooAsset 初始化失败）。
        // 在 Editor 启动时检测并自动修复。
        // ================================================================
        static StageApplyConfig()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                string fullPath = Path.GetFullPath(AssetConfigPath);
                if (!File.Exists(fullPath))
                    return;

                try
                {
                    string yaml = File.ReadAllText(fullPath);
                    var match = ModeFieldRegex.Match(yaml);
                    if (!match.Success) return;

                    if (int.TryParse(match.Groups[2].Value, out int modeInt)
                        && modeInt == (int)AssetConfig.PlayMode.Offline)
                    {
                        Debug.LogWarning("[StageApplyConfig] AssetConfig.Mode was left at Offline " +
                            "(likely from a crashed build). Auto-resetting to EditorSimulate.");
                        int editorSimulateInt = (int)AssetConfig.PlayMode.EditorSimulate;
                        yaml = ModeFieldRegex.Replace(yaml, $"${{1}}{editorSimulateInt}");
                        File.WriteAllText(fullPath, yaml);
                        AssetDatabase.ImportAsset(AssetConfigPath, ImportAssetOptions.ForceSynchronousImport);
                        Debug.Log("[StageApplyConfig] AssetConfig.Mode auto-reset to EditorSimulate ✓");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[StageApplyConfig] Failed to auto-reset AssetConfig.Mode: {ex.Message}");
                }
            };
        }

        public static void Execute(BuildConfig config)
        {
            Debug.Log("[S5] ApplyConfig: Starting...");

            // 先确认 AssetConfig 文件存在
            string fullPath = Path.GetFullPath(AssetConfigPath);
            if (!File.Exists(fullPath))
            {
                throw new BuildFailedException("S5_ApplyConfig",
                    $"AssetConfig not found at {fullPath}. Run Stage 0 first or create one.");
            }

            // ================================================================
            // 关键修复：绕过 ScriptableObject API，直接修改 .asset YAML 文件。
            //
            // 原因：通过 Resources.Load + SetDirty + SaveAssets 修改 ScriptableObject
            // 存在序列化时机问题 —— S6 开头的 AssetDatabase.Refresh() 可能在 SaveAssets
            // 完全落盘前就重新导入了资产，导致 Player Build 仍使用旧的 Mode: 0。
            //
            // 直接写 YAML + AssetDatabase.ImportAsset 确保磁盘与资产数据库同步。
            // ================================================================
            string yaml = File.ReadAllText(fullPath);

            // 读原始 Mode
            var match = ModeFieldRegex.Match(yaml);
            if (!match.Success)
            {
                throw new BuildFailedException("S5_ApplyConfig",
                    "Cannot find 'Mode:' field in AssetConfig.asset YAML.");
            }

            int originalModeInt;
            if (!int.TryParse(match.Groups[2].Value, out originalModeInt))
            {
                throw new BuildFailedException("S5_ApplyConfig",
                    $"Cannot parse Mode value in AssetConfig.asset: '{match.Value}'");
            }

            s_originalMode = (AssetConfig.PlayMode)originalModeInt;
            Debug.Log($"[S5] AssetConfig.Mode: {s_originalMode} → Offline (YAML direct write)");

            // 替换 Mode: X → Mode: 1
            int offlineInt = (int)AssetConfig.PlayMode.Offline;
            yaml = ModeFieldRegex.Replace(yaml, $"${{1}}{offlineInt}");
            File.WriteAllText(fullPath, yaml);

            // 导入修改后的资源 —— 用 ImportAsset 更精准，不触发其他资产的无关导入
            AssetDatabase.ImportAsset(AssetConfigPath, ImportAssetOptions.ForceSynchronousImport);
            s_modeChanged = true;
            Debug.Log("[S5] AssetConfig re-imported into AssetDatabase with Mode=Offline");

            // 复用现有 ApplyToOpenEntry —— 写 hotUpdateAssemblies / aotMetadataAssemblies
            Boot.Editor.HybridCLR.KJHybridClrBuildTools.ApplyToOpenEntry();
            Debug.Log("[S5] ApplyToOpenEntry done");

            // 复用现有 PrepareBootScene —— 确保 Boot 场景在 BuildSettings 并保存
            Boot.Editor.HybridCLR.KJHybridClrBuildTools.PrepareBootScene();
            Debug.Log("[S5] PrepareBootScene done");

            Debug.Log("[S5] ApplyConfig: DONE");
        }

        /// <summary>
        /// 回滚 AssetConfig.Mode 到构建前的值（同样用 YAML 直接写，确保可靠性）。
        /// 由 KJBuildPipeline.Build() 在构建结束后调用。
        /// </summary>
        public static void RollbackAssetConfig()
        {
            if (!s_modeChanged) return;

            string fullPath = Path.GetFullPath(AssetConfigPath);
            if (!File.Exists(fullPath)) return;

            string yaml = File.ReadAllText(fullPath);
            int originalInt = (int)s_originalMode;
            yaml = ModeFieldRegex.Replace(yaml, $"${{1}}{originalInt}");
            File.WriteAllText(fullPath, yaml);
            AssetDatabase.ImportAsset(AssetConfigPath, ImportAssetOptions.ForceSynchronousImport);
            s_modeChanged = false;

            Debug.Log($"[S5] AssetConfig.Mode rolled back to {s_originalMode} (YAML direct write)");
        }
    }
}
