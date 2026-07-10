using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Build Dashboard —— OdinMenuEditorWindow 驱动的构建管理面板。
    /// 支持 Profile 管理、Stage 监控、报告查看、日志定位、诊断入口。
    /// 菜单: KJ → Build → Dashboard
    /// </summary>
    public class BuildDashboardWindow : OdinMenuEditorWindow
    {
        // 由 BuildConfig.asset 解析的配置引用
        private BuildConfig _config;
        private BuildProfile _profile;

        // Stage 检测状态
        private bool[] _changeMask = new bool[10];
        private string[] _reasons = new string[10];

        public static void Open()
        {
            var window = GetWindow<BuildDashboardWindow>();
            window.titleContent = new GUIContent("Build Dashboard");
            window.minSize = new Vector2(640, 520);
            window.Show();
        }

        [MenuItem("KJ/Build/Dashboard")]
        private static void OpenDashboard()
        {
            Open();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Config.DrawSearchToolbar = true;

            // Profiles
            tree.Add("Profiles", new ProfileListView(this));
            tree.Add("Profiles/Dev", CreateDevProfile());
            tree.Add("Profiles/Formal", CreateFormalProfile());

            // Build
            tree.Add("Build Plan", new BuildPlanView(this));
            tree.Add("Stage Monitor", new StageMonitorView(this));

            // Results
            tree.Add("Reports", new ReportsView(this));
            tree.Add("Artifacts", new ArtifactsView(this));

            // Diagnostics
            tree.Add("Diagnostics", new DiagnosticsView(this));

            return tree;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadConfig();
            DetectChanges();
        }

        private void LoadConfig()
        {
            string configPath = "Assets/Scripts/Boot.Editor/Build/BuildConfig.asset";
            _config = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);

            // 尝试加载 Profile
            string profilePath = "Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset";
            _profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
        }

        public void DetectChanges()
        {
            _changeMask = StageDependencyTracker.DetectChanges(includeSmoke: false, _config);
            _reasons = new string[10];
            for (int i = 0; i < 10; i++)
                _reasons[i] = StageDependencyTracker.GetReason(i, _config);
        }

        public void RunFullBuild()
        {
            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<BuildConfig>();
                Debug.LogWarning("[Dashboard] No BuildConfig, using defaults");
            }

            KJBuildPipeline.ClearAllMarkers(_config);
            Close();
            var report = KJBuildPipeline.Build(_config);
            Debug.Log($"[Dashboard] Build {(report.summary.allPassed ? "SUCCESS" : "FAILED")}");
        }

        public void RunIncrementalBuild()
        {
            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<BuildConfig>();
                Debug.LogWarning("[Dashboard] No BuildConfig, using defaults");
            }

            bool[] mask = StageDependencyTracker.DetectChanges(includeSmoke: false, _config);
            Close();
            var report = KJBuildPipeline.BuildWithMask(_config, mask);
            Debug.Log($"[Dashboard] Incremental build {(report.summary.allPassed ? "SUCCESS" : "FAILED")}");
        }

        private static OdinMenuItem CreateDevProfile()
        {
            // Placeholder for profile creation
            return null;
        }

        private static OdinMenuItem CreateFormalProfile()
        {
            // Placeholder for profile creation
            return null;
        }

        // ---- Views ----

        [HideReferenceObjectPicker]
        public class ProfileListView
        {
            private readonly BuildDashboardWindow _window;

            public ProfileListView(BuildDashboardWindow window) { _window = window; }

            [Title("Build Profile")]
            [InfoBox("Select a profile and click Build to start the pipeline.")]
            [ShowInInspector]
            public string ActiveProfile { get; set; } = "Dev Android";

            [Button("Full Build", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
            public void FullBuild() => _window.RunFullBuild();

            [Button("Incremental Build"), GUIColor(0.3f, 0.9f, 0.3f)]
            public void IncrementalBuild() => _window.RunIncrementalBuild();

            [Button("Preflight Only")]
            public void PreflightOnly()
            {
                if (_window._config == null)
                    _window._config = ScriptableObject.CreateInstance<BuildConfig>();
                var context = new BuildContext { Config = _window._config };
                new P1_PreflightStage().Execute(context);
            }
        }

        [HideReferenceObjectPicker]
        public class BuildPlanView
        {
            private readonly BuildDashboardWindow _window;
            public BuildPlanView(BuildDashboardWindow window) { _window = window; }

            [Title("Build Plan")]
            [InfoBox("Stages that need to run are highlighted.")]
            [ShowInInspector, ReadOnly]
            public string OutputDir => _window._config?.GetOutputDir() ?? "Build/StandaloneWindows64";

            [ShowInInspector, ReadOnly]
            public string Platform => _window._config?.Platform.ToString() ?? "StandaloneWindows64";
        }

        [HideReferenceObjectPicker]
        public class StageMonitorView
        {
            private readonly BuildDashboardWindow _window;
            public StageMonitorView(BuildDashboardWindow window) { _window = window; }

            [Title("Stage Monitor")]
            [Button("Refresh Changes")]
            public void Refresh() => _window.DetectChanges();

            [ShowInInspector, TableList(IsReadOnly = true)]
            public System.Collections.Generic.List<StageEntry> Stages
            {
                get
                {
                    var list = new System.Collections.Generic.List<StageEntry>();
                    for (int i = 0; i < 10; i++)
                    {
                        list.Add(new StageEntry
                        {
                            Index = i,
                            Name = StageDependencyTracker.StageNames[i],
                            NeedsRun = _window._changeMask[i],
                            Reason = _window._reasons[i],
                        });
                    }
                    return list;
                }
            }
        }

        [HideReferenceObjectPicker]
        public class ReportsView
        {
            private readonly BuildDashboardWindow _window;
            public ReportsView(BuildDashboardWindow window) { _window = window; }

            [Title("Build Reports")]
            [Button("Open Latest Report")]
            public void OpenLatest()
            {
                string dir = "Build";
                if (Directory.Exists(dir))
                {
                    var jsonFiles = Directory.GetFiles(dir, "build_report.json", SearchOption.AllDirectories)
                        .OrderByDescending(File.GetLastWriteTime).ToList();
                    if (jsonFiles.Count > 0)
                        EditorUtility.RevealInFinder(jsonFiles[0]);
                }
            }

            [Button("Open Build Output Directory")]
            public void OpenOutput()
            {
                if (Directory.Exists("Build"))
                    EditorUtility.RevealInFinder("Build");
            }
        }

        [HideReferenceObjectPicker]
        public class ArtifactsView
        {
            private readonly BuildDashboardWindow _window;
            public ArtifactsView(BuildDashboardWindow window) { _window = window; }

            [Title("Artifacts")]
            [Button("Open Artifacts Directory")]
            public void OpenArtifacts()
            {
                string dir = "BuildBackup";
                if (Directory.Exists(dir))
                    EditorUtility.RevealInFinder(dir);
                else
                    Debug.LogWarning("BuildBackup directory not found. Run a build first.");
            }
        }

        [HideReferenceObjectPicker]
        public class DiagnosticsView
        {
            private readonly BuildDashboardWindow _window;
            public DiagnosticsView(BuildDashboardWindow window) { _window = window; }

            [Title("Diagnostics")]
            [Button("Copy AI Handoff Path")]
            public void CopyHandoff()
            {
                string dir = "Build/StandaloneWindows64/reports";
                string path = Path.Combine(dir, "ai_handoff.json");
                if (File.Exists(path))
                {
                    GUIUtility.systemCopyBuffer = Path.GetFullPath(path);
                    Debug.Log($"[Dashboard] Copied: {path}");
                }
                else
                {
                    Debug.LogWarning("No ai_handoff.json found. Build may not have failed or reports dir differs.");
                }
            }

            [Button("Open All Report Directories")]
            public void OpenReports()
            {
                string dir = "BuildBackup";
                if (Directory.Exists(dir))
                    EditorUtility.RevealInFinder(dir);
                else
                {
                    // Fallback to flat structure
                    if (Directory.Exists("Build"))
                        EditorUtility.RevealInFinder("Build");
                }
            }
        }

        [System.Serializable]
        public class StageEntry
        {
            public int Index;
            public string Name;
            public bool NeedsRun;
            public string Reason;
        }
    }
}
