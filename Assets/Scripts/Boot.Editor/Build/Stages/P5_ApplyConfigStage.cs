using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P5 Apply Runtime Config — 事务化写入 AssetConfig、环境开关、CDN、日志策略。
    /// 构建完成后自动回滚 AssetConfig.Mode 到 EditorSimulate。
    /// </summary>
    public class P5_ApplyConfigStage : BuildStageBase
    {
        private const string AssetConfigPath = "Assets/Resources/AssetConfig.asset";
        private static string _originalConfigYaml;

        public override string Id => "P5.ApplyConfig";
        public override string DisplayName => "Apply Runtime Config";
        public override int Order => 5;
        public override string Category => "Config";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P4.Assets" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.Transactional;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs()
                .WithSourcePaths("Assets/Resources/AssetConfig.asset")
                .WithDependsOn("P4.Assets");

        public override void Execute(BuildContext context)
        {
            Debug.Log("[P5] ApplyConfig: Writing runtime configuration...");

            // Snapshot 原始内容
            _originalConfigYaml = ReadAssetConfigYaml();
            context.Transaction.SnapshotFile(AssetConfigPath);

            // Snapshot Scripting Defines
            var buildTarget = context.Profile?.Platform ?? context.Config?.Platform
                ?? BuildTarget.StandaloneWindows64;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            context.Transaction.SnapshotScriptingDefines(targetGroup);

            // 1. 写 AssetConfig.Mode = Offline
            SetAssetConfigModeToOffline();

            // 2. 写 Scripting Define Symbols（按环境）
            ApplyScriptingDefines(context, targetGroup);

            // 3. 刷新资产数据库
            AssetDatabase.Refresh();

            Debug.Log("[P5] ApplyConfig: Configuration applied (Mode=Offline)");
        }

        public override void Verify(BuildContext context)
        {
            // 验证 AssetConfig.Mode 确实为 Offline
            string yaml = ReadAssetConfigYaml();
            var match = Regex.Match(yaml, @"Mode:\s*(\d+)");
            if (!match.Success || match.Groups[1].Value != "1")
                throw new InvalidOperationException($"AssetConfig.Mode is not Offline (got: {match.Groups[1].Value})");
            Debug.Log("[P5] ✓ AssetConfig.Mode verified as Offline");
        }

        public override void Rollback(BuildContext context)
        {
            RollbackAssetConfig();
        }

        // ===== YAML 直接写入（绕过 ScriptableObject 序列化竞态）=====

        private static string ReadAssetConfigYaml()
        {
            return File.ReadAllText(AssetConfigPath);
        }

        private static void SetAssetConfigModeToOffline()
        {
            string yaml = File.ReadAllText(AssetConfigPath);
            // Mode: 0 (EditorSimulate) → Mode: 1 (Offline)
            yaml = Regex.Replace(yaml, @"\bMode:\s*0\b", "Mode: 1");
            File.WriteAllText(AssetConfigPath, yaml);
            AssetDatabase.ImportAsset(AssetConfigPath,
                ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[P5] AssetConfig.Mode set to 1 (Offline) via YAML direct write");
        }

        /// <summary>
        /// 回滚 AssetConfig.Mode 到 EditorSimulate。在构建完成后由 Runner 调用。
        /// </summary>
        public static void RollbackAssetConfig()
        {
            if (_originalConfigYaml == null)
            {
                Debug.LogWarning("[P5] No original AssetConfig YAML to rollback");
                return;
            }

            try
            {
                File.WriteAllText(AssetConfigPath, _originalConfigYaml);
                AssetDatabase.ImportAsset(AssetConfigPath,
                    ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[P5] AssetConfig rolled back to original (EditorSimulate)");
                _originalConfigYaml = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[P5] Failed to rollback AssetConfig: {ex.Message}");
            }
        }

        // ===== Scripting Define Symbols =====

        private void ApplyScriptingDefines(BuildContext context, BuildTargetGroup targetGroup)
        {
            if (context.Profile == null) return;

            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            string current = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var defines = new HashSet<string>(
                current.Split(';', StringSplitOptions.RemoveEmptyEntries));

            // 按环境添加/移除 define
            var env = context.Profile.Environment;
            if (env == Framework.BuildPipeline.Environment.BuildEnvironment.Dev)
            {
                defines.Add("KJ_DEV");
                if (context.Profile.EnableGm) defines.Add("KJ_GM_ENABLED");
                if (context.Profile.EnableDebugUi) defines.Add("KJ_DEBUG_UI");
            }
            else if (env == Framework.BuildPipeline.Environment.BuildEnvironment.Formal
                  || env == Framework.BuildPipeline.Environment.BuildEnvironment.Audit)
            {
                defines.Remove("KJ_DEV");
                defines.Remove("KJ_GM_ENABLED");
                defines.Remove("KJ_DEBUG_UI");
            }

            // 添加 KJ_BUILD_PIPELINE 标记
            defines.Add("KJ_BUILD_PIPELINE");

            string newDefines = string.Join(";", defines);
            if (newDefines != current)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
                Debug.Log($"[P5] ScriptingDefines updated: {current} → {newDefines}");
            }
        }
    }
}
