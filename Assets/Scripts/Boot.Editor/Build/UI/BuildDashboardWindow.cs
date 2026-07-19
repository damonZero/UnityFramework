using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.BuildPipeline.Diagnostics;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// BuildProfile-driven one-click build entry and result dashboard.
    /// </summary>
    public sealed class BuildDashboardWindow : OdinMenuEditorWindow
    {
        private const string SelectedProfileKey = "KJ.BuildDashboard.SelectedProfile";

        private readonly List<BuildProfile> _profiles = new List<BuildProfile>();
        private BuildProfile _profile;
        private BuildReportData _lastReport;
        private string _lastError;
        private bool _isBuilding;

        public static void Open()
        {
            var window = GetWindow<BuildDashboardWindow>();
            window.titleContent = new GUIContent("KJ Build");
            window.minSize = new Vector2(760, 560);
            window.Show();
        }

        [MenuItem("KJ/Build/Dashboard")]
        private static void OpenDashboard() => Open();

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshProfiles();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Config.DrawSearchToolbar = false;
            tree.Add("Build", new BuildView(this));
            tree.Add("Profile", new ProfileView(this));
            tree.Add("Stages", new StageView());
            tree.Add("Results", new ResultsView(this));
            return tree;
        }

        private void RefreshProfiles()
        {
            string selectedPath = _profile != null
                ? AssetDatabase.GetAssetPath(_profile)
                : EditorPrefs.GetString(SelectedProfileKey, KJBuildPipeline.DefaultProfilePath);

            _profiles.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:BuildProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
                if (profile != null)
                    _profiles.Add(profile);
            }

            _profiles.Sort((left, right) => string.Compare(
                GetProfileLabel(left), GetProfileLabel(right), StringComparison.OrdinalIgnoreCase));

            _profile = _profiles.FirstOrDefault(profile =>
                           string.Equals(AssetDatabase.GetAssetPath(profile), selectedPath,
                               StringComparison.OrdinalIgnoreCase))
                       ?? _profiles.FirstOrDefault(profile =>
                           string.Equals(AssetDatabase.GetAssetPath(profile), KJBuildPipeline.DefaultProfilePath,
                               StringComparison.OrdinalIgnoreCase))
                       ?? _profiles.FirstOrDefault();

            RememberSelectedProfile();
            TryLoadLatestReport();
        }

        private void SelectProfile(BuildProfile profile)
        {
            if (_profile == profile)
                return;

            _profile = profile;
            _lastReport = null;
            _lastError = null;
            RememberSelectedProfile();
            TryLoadLatestReport();
            Repaint();
        }

        private void CreateProfile()
        {
            string path;
            if (AssetDatabase.LoadAssetAtPath<BuildProfile>(KJBuildPipeline.DefaultProfilePath) == null)
            {
                path = KJBuildPipeline.DefaultProfilePath;
            }
            else
            {
                path = EditorUtility.SaveFilePanelInProject(
                    "Create Build Profile",
                    "BuildProfile",
                    "asset",
                    "Choose a location for the new BuildProfile.",
                    "Assets/Scripts/Boot.Editor/Build/Config");
            }

            if (string.IsNullOrWhiteSpace(path))
                return;

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var profile = CreateInstance<BuildProfile>();
            profile.ProfileName = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshProfiles();
            SelectProfile(profile);
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            ForceMenuTreeRebuild();
        }

        private void RememberSelectedProfile()
        {
            string path = _profile == null ? string.Empty : AssetDatabase.GetAssetPath(_profile);
            EditorPrefs.SetString(SelectedProfileKey, path);
        }

        private IEnumerable<ValueDropdownItem<BuildProfile>> GetProfileOptions()
        {
            return _profiles.Select(profile =>
                new ValueDropdownItem<BuildProfile>(GetProfileLabel(profile), profile));
        }

        private static string GetProfileLabel(BuildProfile profile)
        {
            if (profile == null)
                return "<Missing>";

            string name = string.IsNullOrWhiteSpace(profile.ProfileName) ? profile.name : profile.ProfileName;
            return $"{name}  [{profile.Environment} / {profile.Platform}]";
        }

        private List<BuildIssue> ValidateProfile()
        {
            return BuildProfileValidator.Validate(_profile);
        }

        private bool CanBuild()
        {
            return !_isBuilding
                   && _profile != null
                   && !EditorApplication.isCompiling
                   && !EditorApplication.isPlayingOrWillChangePlaymode
                   && !UnityEditor.BuildPipeline.isBuildingPlayer
                   && ValidateProfile().All(issue =>
                       issue.Severity != BuildIssueSeverity.Error && !issue.IsBlocking);
        }

        private void RunBuild()
        {
            if (!CanBuild())
            {
                EditorUtility.DisplayDialog("Build unavailable",
                    GetBuildBlockReason(), "OK");
                return;
            }

            _isBuilding = true;
            _lastError = null;
            _lastReport = null;
            Repaint();

            try
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[BuildDashboard] One-click build started: {GetProfileLabel(_profile)}");
                _lastReport = KJBuildPipeline.Build(_profile);

                string result = _lastReport.AllPassed ? "succeeded" : "failed";
                Debug.Log($"[BuildDashboard] Build {result}: {GetProfileLabel(_profile)}");
                EditorUtility.DisplayDialog(
                    _lastReport.AllPassed ? "Build succeeded" : "Build failed",
                    GetResultMessage(_lastReport), "OK");
            }
            catch (Exception ex)
            {
                _lastError = ex.ToString();
                Debug.LogError($"[BuildDashboard] Build failed before completion: {ex}");
                EditorUtility.DisplayDialog("Build failed", ex.Message, "OK");
            }
            finally
            {
                _isBuilding = false;
                ForceMenuTreeRebuild();
                Repaint();
            }
        }

        private string GetBuildBlockReason()
        {
            if (_profile == null)
                return "No BuildProfile was found. Create or restore a profile first.";
            if (EditorApplication.isCompiling)
                return "Unity is compiling scripts.";
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Exit Play Mode before building.";
            if (_isBuilding || UnityEditor.BuildPipeline.isBuildingPlayer)
                return "A build is already running.";

            var blocking = ValidateProfile().FirstOrDefault(issue =>
                issue.Severity == BuildIssueSeverity.Error || issue.IsBlocking);
            return blocking == null
                ? "The build is temporarily unavailable."
                : $"[{blocking.Code}] {blocking.Message}";
        }

        private void TryLoadLatestReport()
        {
            _lastReport = null;
            if (_profile == null)
                return;

            string reportPath = GetLatestFile(_profile.GetOutputDir(), "build_report.json");
            if (string.IsNullOrEmpty(reportPath))
                return;

            try
            {
                _lastReport = JsonUtility.FromJson<BuildReportData>(File.ReadAllText(reportPath));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildDashboard] Cannot read latest report: {ex.Message}");
            }
        }

        private static string GetResultMessage(BuildReportData report)
        {
            string profileName = report == null ? string.Empty : report.ProfileName;
            var failed = report?.StageResults?.FirstOrDefault(stage => stage.Status == StageStatus.Failed);
            if (report != null && report.AllPassed)
                return $"Profile: {report.ProfileName}\nDuration: {FormatDuration(report.TotalDurationMs)}";

            return failed == null
                ? $"Profile: {profileName}\nSee the Results tab and build report for details."
                : $"Stage: {failed.StageId} - {failed.DisplayName}\n{failed.ErrorMessage}";
        }

        private static string GetLatestFile(string root, string fileName)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return null;

            return Directory.GetFiles(root, fileName, SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static void RevealFile(string path, string missingMessage)
        {
            if (!string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path)))
                EditorUtility.RevealInFinder(Path.GetFullPath(path));
            else
                Debug.LogWarning(missingMessage);
        }

        private static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 1000)
                return $"{milliseconds} ms";

            return TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss");
        }

        [HideReferenceObjectPicker]
        private sealed class BuildView
        {
            private readonly BuildDashboardWindow _window;

            public BuildView(BuildDashboardWindow window)
            {
                _window = window;
            }

            [Title("One-click Build")]
            [ShowInInspector, ValueDropdown(nameof(ProfileOptions))]
            [LabelText("Profile")]
            public BuildProfile ActiveProfile
            {
                get => _window._profile;
                set => _window.SelectProfile(value);
            }

            [ShowInInspector, ReadOnly, LabelText("Target")]
            public string Target => _window._profile == null
                ? "-"
                : $"{_window._profile.Environment} / {_window._profile.Platform}";

            [ShowInInspector, ReadOnly, LabelText("Version")]
            public string Version => _window._profile == null
                ? "-"
                : $"{_window._profile.VersionName} ({_window._profile.VersionCode})";

            [ShowInInspector, ReadOnly, LabelText("Player")]
            public string PlayerPath => _window._profile?.GetPlayerPath() ?? "-";

            [ShowInInspector, ReadOnly, LabelText("Validation")]
            public string ValidationSummary
            {
                get
                {
                    var issues = _window.ValidateProfile();
                    int errors = issues.Count(issue =>
                        issue.Severity == BuildIssueSeverity.Error || issue.IsBlocking);
                    int warnings = issues.Count(issue => issue.Severity == BuildIssueSeverity.Warning);
                    return errors == 0
                        ? warnings == 0 ? "Ready" : $"Ready with {warnings} warning(s)"
                        : $"Blocked by {errors} error(s)";
                }
            }

            [Button("One-click Build", ButtonSizes.Large), GUIColor(0.20f, 0.72f, 0.38f)]
            [EnableIf(nameof(BuildEnabled))]
            public void OneClickBuild() => _window.RunBuild();

            [Button("Refresh")]
            public void Refresh()
            {
                _window.RefreshProfiles();
                _window.ForceMenuTreeRebuild();
            }

            [Title("Validation")]
            [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true)]
            public List<IssueEntry> Issues => _window.ValidateProfile()
                .Select(IssueEntry.FromIssue)
                .ToList();

            [ShowInInspector, ReadOnly, MultiLineProperty(4), LabelText("Last Error")]
            [ShowIf(nameof(HasLastError))]
            public string LastError => _window._lastError;

            private bool BuildEnabled => _window.CanBuild();
            private bool HasLastError => !string.IsNullOrWhiteSpace(_window._lastError);
            private IEnumerable<ValueDropdownItem<BuildProfile>> ProfileOptions =>
                _window.GetProfileOptions();
        }

        [HideReferenceObjectPicker]
        private sealed class ProfileView
        {
            private readonly BuildDashboardWindow _window;

            public ProfileView(BuildDashboardWindow window)
            {
                _window = window;
            }

            [Title("Build Profile")]
            [ShowInInspector, ValueDropdown(nameof(ProfileOptions))]
            [LabelText("Profile")]
            public BuildProfile ActiveProfile
            {
                get => _window._profile;
                set => _window.SelectProfile(value);
            }

            [ShowInInspector, InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
            [HideLabel]
            public BuildProfile Settings => _window._profile;

            [Button("Select Asset")]
            public void SelectAsset()
            {
                Selection.activeObject = _window._profile;
                if (_window._profile != null)
                    EditorGUIUtility.PingObject(_window._profile);
            }

            [Button("Create Profile")]
            public void CreateProfile() => _window.CreateProfile();

            [Button("Refresh Profiles")]
            public void RefreshProfiles()
            {
                _window.RefreshProfiles();
                _window.ForceMenuTreeRebuild();
            }

            private IEnumerable<ValueDropdownItem<BuildProfile>> ProfileOptions =>
                _window.GetProfileOptions();
        }

        [HideReferenceObjectPicker]
        private sealed class StageView
        {
            [Title("Registered P0-P9 Stages")]
            [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true)]
            public List<StageEntry> Stages => BuildStageRegistry.GetAll()
                .Select(stage => new StageEntry
                {
                    Order = stage.Order,
                    Id = stage.Id,
                    Name = stage.DisplayName,
                    Category = stage.Category,
                    Policy = stage.Policy.ToString(),
                })
                .ToList();
        }

        [HideReferenceObjectPicker]
        private sealed class ResultsView
        {
            private readonly BuildDashboardWindow _window;

            public ResultsView(BuildDashboardWindow window)
            {
                _window = window;
            }

            [Title("Latest Result")]
            [ShowInInspector, ReadOnly, LabelText("Status")]
            public string Status => _window._lastReport == null
                ? "No report"
                : _window._lastReport.AllPassed ? "Succeeded" : "Failed";

            [ShowInInspector, ReadOnly, LabelText("Run ID")]
            public string RunId => _window._lastReport?.RunId ?? "-";

            [ShowInInspector, ReadOnly, LabelText("Duration")]
            public string Duration => _window._lastReport == null
                ? "-"
                : FormatDuration(_window._lastReport.TotalDurationMs);

            [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true)]
            public List<ResultEntry> Stages => _window._lastReport?.StageResults?
                .Select(ResultEntry.FromResult)
                .ToList() ?? new List<ResultEntry>();

            [Button("Open Report")]
            public void OpenReport()
            {
                string path = _window._profile == null
                    ? null
                    : GetLatestFile(_window._profile.GetOutputDir(), "build_report.md");
                RevealFile(path, "No build_report.md was found for the selected profile.");
            }

            [Button("Open Artifacts")]
            public void OpenArtifacts()
            {
                string path = _window._profile == null ? null : _window._profile.GetArchiveDir();
                RevealFile(path, "The artifact directory does not exist yet.");
            }

            [Button("Open Output")]
            public void OpenOutput()
            {
                string path = _window._profile?.GetOutputDir();
                RevealFile(path, "The build output directory does not exist yet.");
            }

            [Button("Copy AI Handoff Path")]
            public void CopyHandoffPath()
            {
                string path = _window._profile == null
                    ? null
                    : GetLatestFile(_window._profile.GetOutputDir(), "ai_handoff.json");
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    GUIUtility.systemCopyBuffer = Path.GetFullPath(path);
                    Debug.Log($"[BuildDashboard] Copied: {path}");
                }
                else
                {
                    Debug.LogWarning("No ai_handoff.json was found for the selected profile.");
                }
            }
        }

        [Serializable]
        private sealed class IssueEntry
        {
            [TableColumnWidth(70, Resizable = false)] public string Severity;
            [TableColumnWidth(160)] public string Code;
            public string Message;
            public string SuggestedFix;

            public static IssueEntry FromIssue(BuildIssue issue)
            {
                return new IssueEntry
                {
                    Severity = issue.Severity.ToString(),
                    Code = issue.Code,
                    Message = issue.Message,
                    SuggestedFix = issue.SuggestedFix,
                };
            }
        }

        [Serializable]
        private sealed class StageEntry
        {
            [TableColumnWidth(45, Resizable = false)] public int Order;
            [TableColumnWidth(100)] public string Id;
            public string Name;
            [TableColumnWidth(100)] public string Category;
            [TableColumnWidth(150)] public string Policy;
        }

        [Serializable]
        private sealed class ResultEntry
        {
            [TableColumnWidth(100)] public string Stage;
            public string Name;
            [TableColumnWidth(75, Resizable = false)] public string Status;
            [TableColumnWidth(90, Resizable = false)] public string Duration;
            public string Error;

            public static ResultEntry FromResult(StageExecutionResult result)
            {
                return new ResultEntry
                {
                    Stage = result.StageId,
                    Name = result.DisplayName,
                    Status = result.Status.ToString(),
                    Duration = FormatDuration(result.DurationMs),
                    Error = result.ErrorMessage,
                };
            }
        }
    }
}
