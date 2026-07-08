using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Build Stage Manager —— 可视化构建阶段管理面板。
    /// - 自动检测文件变更，标出需要重跑的 Stage
    /// - 支持手动勾选/取消任意 Stage
    /// - 一键增量构建 / 全量构建
    /// 菜单: KJ → Build → Build Stage Manager...
    /// </summary>
    public class BuildStagePanel : EditorWindow
    {
        private bool[] _mask = new bool[10];
        private string[] _reasons = new string[10];
        private bool _initialized = false;
        private Vector2 _scrollPos;

        private static readonly string[] StageLabels =
        {
            "S0  PreFlightCheck     ─ 前置校验",
            "S1  GenerateAll         ─ HybridCLR 生成（MethodBridge/link.xml）",
            "S2  Compile             ─ 编译热更 DLL + AOT metadata",
            "S3  Sync                ─ 拷贝 DLL → HotUpdate 目录",
            "S4  BuildYooAsset       ─ YooAsset 打包 AssetBundle",
            "S5  ApplyConfig         ─ 写 AssetConfig.Mode=Offline",
            "S6  BuildPlayer         ─ Unity Player 构建（IL2CPP + Export）",
            "S7  ValidateArtifacts   ─ 产物校验",
            "S8  SmokeRun            ─ 冒烟测试",
            "S9  Report              ─ 输出构建报告",
        };

        [MenuItem("KJ/Build/Build Stage Manager...")]
        public static void Open()
        {
            var window = GetWindow<BuildStagePanel>(false, "Build Stage Manager", true);
            window.minSize = new Vector2(520, 460);
            window.Show();
        }

        private void OnEnable()
        {
            DetectChanges();
        }

        private void DetectChanges()
        {
            _mask = StageDependencyTracker.DetectChanges();
            _reasons = new string[10];
            for (int i = 0; i < 10; i++)
                _reasons[i] = StageDependencyTracker.GetReason(i);
            _initialized = true;
            Repaint();
        }

        private void OnGUI()
        {
            if (!_initialized) DetectChanges();

            // ── 标题 ──
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Build Stage Manager", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("自动检测有变更的 Stage，也可手动勾选。", EditorStyles.miniLabel);
            GUILayout.Space(4);

            // ── 快捷按钮 ──
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍  检测变更", GUILayout.Height(28)))
                DetectChanges();

            if (GUILayout.Button("☑  全选", GUILayout.Height(28)))
            {
                for (int i = 0; i < 10; i++) _mask[i] = true;
                Repaint();
            }

            if (GUILayout.Button("☐  全不选", GUILayout.Height(28)))
            {
                for (int i = 0; i < 10; i++) _mask[i] = false;
                Repaint();
            }

            if (GUILayout.Button("⟳  仅选需要重跑的", GUILayout.Height(28)))
            {
                DetectChanges();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            // ── Stage 列表 ──
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < 10; i++)
            {
                DrawStageRow(i);
                if (i < 9) GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);

            // ── 底部操作栏 ──
            EditorGUILayout.BeginHorizontal();

            int selectedCount = _mask.Count(m => m);
            GUI.enabled = selectedCount > 0;

            if (GUILayout.Button($"▶  增量构建 ({selectedCount}/10 Stage)", GUILayout.Height(36)))
            {
                ExecuteBuild(_mask);
            }

            GUI.enabled = true;

            if (GUILayout.Button("▶  全量构建 (10/10)", GUILayout.Height(36)))
            {
                ExecuteBuild(null);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
        }

        private void DrawStageRow(int index)
        {
            bool isNeeded = _mask[index];
            string reason = _reasons[index];
            string markerPath = System.IO.Path.Combine("Build/.markers", $".{StageDependencyTracker.StageNames[index]}.done");
            bool hasMarker = System.IO.File.Exists(markerPath);

            // 背景色标记
            Color bgColor = GUI.backgroundColor;
            if (isNeeded)
                GUI.backgroundColor = new Color(1f, 0.85f, 0.75f); // 浅橙 = 需要重跑
            else if (hasMarker)
                GUI.backgroundColor = new Color(0.75f, 0.95f, 0.75f); // 浅绿 = 已完成
            else
                GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f); // 灰 = 未跑过

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginHorizontal();

            // checkbox
            _mask[index] = EditorGUILayout.Toggle(_mask[index], GUILayout.Width(20));

            // 状态图标 + 标签
            string statusIcon = isNeeded ? "⚠" : (hasMarker ? "✓" : "○");
            string statusLabel = isNeeded ? "[需重跑]" : (hasMarker ? "[已完成]" : "[未跑]");
            Color originalColor = GUI.color;

            if (isNeeded)
                GUI.color = new Color(0.8f, 0.3f, 0.1f);
            else if (hasMarker)
                GUI.color = new Color(0.2f, 0.6f, 0.2f);
            else
                GUI.color = Color.gray;

            EditorGUILayout.LabelField($"{statusIcon} {StageLabels[index]}", EditorStyles.miniBoldLabel);
            GUI.color = originalColor;

            // 原因
            if (!string.IsNullOrEmpty(reason) && isNeeded)
                EditorGUILayout.LabelField(reason, EditorStyles.miniLabel, GUILayout.Width(240));
            else
                GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ExecuteBuild(bool[] mask)
        {
            string configPath = "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildConfig>();
                Debug.LogWarning("[BuildStagePanel] BuildConfig.asset not found, using defaults.");
            }

            // 关闭面板
            Close();

            bool isIncremental = mask != null;
            Debug.Log($"[BuildStagePanel] ========== {(isIncremental ? "INCREMENTAL" : "FULL")} BUILD STARTED ==========");

            var report = KJBuildPipeline.BuildWithMask(config, mask);
            Debug.Log($"[BuildStagePanel] Build result: {(report.summary.allPassed ? "SUCCESS" : "FAILED")}");

            if (report.summary.allPassed)
                EditorUtility.DisplayDialog("Build Complete",
                    $"Build succeeded!\nDuration: {report.totalDuration}\n\nReport: {config.GetReportPath()}.json", "OK");
            else
                EditorUtility.DisplayDialog("Build Failed",
                    $"Failed at: {report.summary.failedStage}\n\n{report.summary.errorMessage}\n\nReport: {config.GetReportPath()}.json", "OK");
        }
    }
}
