using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Lightweight stage viewer retained for the existing menu path.
    /// Stage overview for the BuildProfile-only pipeline.
    /// </summary>
    public class BuildStagePanel : EditorWindow
    {
        private Vector2 _scrollPos;

        [MenuItem("KJ/Build/Build Stage Manager...")]
        public static void Open()
        {
            var window = GetWindow<BuildStagePanel>(false, "Build Stages", true);
            window.minSize = new Vector2(560, 420);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Build Stages (P0-P9)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The build pipeline is BuildProfile-driven. Incremental behavior is controlled by stage fingerprints.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Dashboard", GUILayout.Height(30)))
                BuildDashboardWindow.Open();
            if (GUILayout.Button("Run Full Build", GUILayout.Height(30)))
                RunFullBuild();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var stage in BuildStageRegistry.GetAll().OrderBy(s => s.Order))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{stage.Order}. {stage.Id}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(stage.DisplayName);
                EditorGUILayout.LabelField($"Category: {stage.Category}");
                EditorGUILayout.LabelField($"Policy: {stage.Policy}");
                string deps = stage.DependsOn == null || stage.DependsOn.Count == 0
                    ? "(none)"
                    : string.Join(", ", stage.DependsOn);
                EditorGUILayout.LabelField($"Depends on: {deps}");
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void RunFullBuild()
        {
            try
            {
                var report = KJBuildPipeline.BuildDefaultProfile();
                Debug.Log($"[BuildStagePanel] Build {(report.AllPassed ? "SUCCESS" : "FAILED")}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BuildStagePanel] Build failed: {ex}");
                EditorUtility.DisplayDialog("Build Failed", ex.Message, "OK");
            }
        }
    }
}
