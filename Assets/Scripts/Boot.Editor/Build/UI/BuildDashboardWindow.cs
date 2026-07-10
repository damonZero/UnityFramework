using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Build Dashboard —— BuildProfile 驱动的构建入口和报告查看面板。
    /// </summary>
    public class BuildDashboardWindow : OdinMenuEditorWindow
    {
        private BuildProfile _profile;

        public static void Open()
        {
            var window = GetWindow<BuildDashboardWindow>();
            window.titleContent = new GUIContent("Build Dashboard");
            window.minSize = new Vector2(640, 520);
            window.Show();
        }

        [MenuItem("KJ/Build/Dashboard")]
        private static void OpenDashboard() => Open();

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadProfile();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Config.DrawSearchToolbar = true;
            tree.Add("Profile", new ProfileView(this));
            tree.Add("Stages", new StageView());
            tree.Add("Reports", new ReportsView(this));
            tree.Add("Diagnostics", new DiagnosticsView(this));
            return tree;
        }

        private void LoadProfile()
        {
            _profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(KJBuildPipeline.DefaultProfilePath);
        }

        private BuildProfile RequireProfile()
        {
            if (_profile == null)
                LoadProfile();
            if (_profile == null)
                throw new System.InvalidOperationException(
                    $"BuildProfile not found: {KJBuildPipeline.DefaultProfilePath}");
            return _profile;
        }

        private void RunBuild()
        {
            var profile = RequireProfile();
            Close();
            var report = KJBuildPipeline.Build(profile);
            Debug.Log($"[Dashboard] Build {(report.AllPassed ? "SUCCESS" : "FAILED")}");
        }

        [HideReferenceObjectPicker]
        public class ProfileView
        {
            private readonly BuildDashboardWindow _window;
            public ProfileView(BuildDashboardWindow window) { _window = window; }

            [Title("Build Profile")]
            [ShowInInspector, ReadOnly]
            public string ProfilePath => KJBuildPipeline.DefaultProfilePath;

            [ShowInInspector, InlineEditor(InlineEditorObjectFieldModes.Boxed)]
            public BuildProfile ActiveProfile => _window._profile;

            [Button("Reload Profile")]
            public void Reload() => _window.LoadProfile();

            [Button("Full Build", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
            public void FullBuild() => _window.RunBuild();

            [Button("Preflight Only")]
            public void PreflightOnly()
            {
                var profile = _window.RequireProfile();
                var context = new BuildContext
                {
                    Profile = profile,
                    Paths = new BuildPaths(profile),
                    Transaction = new BuildTransaction(),
                };
                new P1_PreflightStage().Execute(context);
            }
        }

        [HideReferenceObjectPicker]
        public class StageView
        {
            [Title("Registered P0-P9 Stages")]
            [ShowInInspector, TableList(IsReadOnly = true)]
            public System.Collections.Generic.List<StageEntry> Stages =>
                BuildStageRegistry.GetAll()
                    .Select(s => new StageEntry
                    {
                        Order = s.Order,
                        Id = s.Id,
                        Name = s.DisplayName,
                        Category = s.Category,
                        Policy = s.Policy.ToString(),
                    })
                    .ToList();
        }

        [HideReferenceObjectPicker]
        public class ReportsView
        {
            private readonly BuildDashboardWindow _window;
            public ReportsView(BuildDashboardWindow window) { _window = window; }

            [Button("Open Latest Report")]
            public void OpenLatest()
            {
                string root = _window._profile != null ? _window._profile.GetOutputDir() : "BuildBackup";
                if (!Directory.Exists(root))
                {
                    Debug.LogWarning($"Report root not found: {root}");
                    return;
                }

                var jsonFiles = Directory.GetFiles(root, "build_report.json", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTime).ToList();
                if (jsonFiles.Count > 0)
                    EditorUtility.RevealInFinder(jsonFiles[0]);
                else
                    Debug.LogWarning($"No build_report.json under {root}");
            }

            [Button("Open Output Directory")]
            public void OpenOutput()
            {
                string root = _window.RequireProfile().GetOutputDir();
                if (Directory.Exists(root))
                    EditorUtility.RevealInFinder(root);
                else
                    Debug.LogWarning($"Output directory not found: {root}");
            }
        }

        [HideReferenceObjectPicker]
        public class DiagnosticsView
        {
            private readonly BuildDashboardWindow _window;
            public DiagnosticsView(BuildDashboardWindow window) { _window = window; }

            [Button("Copy AI Handoff Path")]
            public void CopyHandoff()
            {
                var paths = new BuildPaths(_window.RequireProfile());
                string path = Path.Combine(paths.ReportsDir, "ai_handoff.json");
                if (File.Exists(path))
                {
                    GUIUtility.systemCopyBuffer = Path.GetFullPath(path);
                    Debug.Log($"[Dashboard] Copied: {path}");
                }
                else
                {
                    Debug.LogWarning($"No ai_handoff.json found: {path}");
                }
            }
        }

        [System.Serializable]
        public class StageEntry
        {
            public int Order;
            public string Id;
            public string Name;
            public string Category;
            public string Policy;
        }
    }
}
