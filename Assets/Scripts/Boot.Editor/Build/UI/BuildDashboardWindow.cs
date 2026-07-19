using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boot.Editor.HybridCLR;
using Framework.BuildPipeline.Diagnostics;
using HybridCLR.Editor.Commands;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建面板 — BuildProfile 驱动的一键打包 + 热更补丁发布。
    /// </summary>
    public sealed class BuildDashboardWindow : OdinMenuEditorWindow
    {
        private const string SelectedProfileKey = "KJ.BuildDashboard.SelectedProfile";
        private const string HotUpdateVersionKey = "KJ.BuildDashboard.HotUpdateVersion";

        private readonly List<BuildProfile> _profiles = new List<BuildProfile>();
        private BuildProfile _profile;
        private BuildReportData _lastReport;
        private string _lastError;
        private bool _isBuilding;
        private bool _isPublishing;

        public static void Open()
        {
            var window = GetWindow<BuildDashboardWindow>();
            window.titleContent = new GUIContent("KJ 构建");
            window.minSize = new Vector2(780, 620);
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
            tree.Add("构建", new BuildView(this));
            tree.Add("配置", new BuildView.ProfileView(this));
            tree.Add("阶段", new BuildView.StageView());
            tree.Add("结果", new BuildView.ResultsView(this));
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
                    "新建构建配置",
                    "BuildProfile",
                    "asset",
                    "选择构建配置存放位置",
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
                return "<缺失>";

            string name = string.IsNullOrWhiteSpace(profile.ProfileName) ? profile.name : profile.ProfileName;
            string platform = profile.Platform == BuildTarget.Android ? "Android" :
                profile.Platform == BuildTarget.StandaloneWindows64 ? "Win64" :
                profile.Platform == BuildTarget.iOS ? "iOS" :
                profile.Platform.ToString();
            return $"{name}  [{profile.Environment} / {platform}]";
        }

        private List<BuildIssue> ValidateProfile()
        {
            return BuildProfileValidator.Validate(_profile);
        }

        private bool CanBuild()
        {
            return !_isBuilding
                   && !_isPublishing
                   && _profile != null
                   && !EditorApplication.isCompiling
                   && !EditorApplication.isPlayingOrWillChangePlaymode
                   && !UnityEditor.BuildPipeline.isBuildingPlayer
                   && ValidateProfile().All(issue =>
                       issue.Severity != BuildIssueSeverity.Error && !issue.IsBlocking);
        }

        private bool CanPublish()
        {
            return !_isBuilding
                   && !_isPublishing
                   && _profile != null
                   && !EditorApplication.isCompiling
                   && !EditorApplication.isPlayingOrWillChangePlaymode
                   && !UnityEditor.BuildPipeline.isBuildingPlayer;
        }

        private void RunBuild()
        {
            if (!CanBuild())
            {
                EditorUtility.DisplayDialog("无法构建",
                    GetBuildBlockReason(), "确定");
                return;
            }

            _isBuilding = true;
            _lastError = null;
            _lastReport = null;
            Repaint();

            try
            {
                AssetDatabase.SaveAssets();
                BuildLogger.Info($"[BuildDashboard] 一键打包开始: {GetProfileLabel(_profile)}");
                _lastReport = KJBuildPipeline.Build(_profile);

                string result = _lastReport.AllPassed ? "成功" : "失败";
                BuildLogger.Info($"[BuildDashboard] 打包{result}: {GetProfileLabel(_profile)}");
                EditorUtility.DisplayDialog(
                    _lastReport.AllPassed ? "打包成功" : "打包失败",
                    GetResultMessage(_lastReport), "确定");
            }
            catch (Exception ex)
            {
                _lastError = ex.ToString();
                BuildLogger.Error($"[BuildDashboard] 打包异常终止: {ex}");
                EditorUtility.DisplayDialog("打包失败", ex.Message, "确定");
            }
            finally
            {
                _isBuilding = false;
                ForceMenuTreeRebuild();
                Repaint();
            }
        }

        private void RunPublish(string version)
        {
            if (!CanPublish())
            {
                EditorUtility.DisplayDialog("无法发布",
                    _isBuilding || _isPublishing ? "已有构建或发布正在执行" : "请检查构建配置", "确定");
                return;
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                EditorUtility.DisplayDialog("版本号不能为空", "请输入热更版本号（如 1.0.1）", "确定");
                return;
            }

            _isPublishing = true;
            _lastError = null;
            Repaint();

            try
            {
                AssetDatabase.SaveAssets();
                BuildLogger.Info($"[BuildDashboard] 发布热更 {version}: {GetProfileLabel(_profile)}");
                HostUpdatePublisher.Publish(version);
                EditorPrefs.SetString(HotUpdateVersionKey, version);

                BuildLogger.Info($"[BuildDashboard] 热更 {version} 发布完成");
                EditorUtility.DisplayDialog("发布成功",
                    $"热更补丁 {version} 已发布到 CDN 目录:\n{HostUpdatePublisher.ServerRoot}", "确定");
            }
            catch (Exception ex)
            {
                _lastError = ex.ToString();
                BuildLogger.Error($"[BuildDashboard] 热更发布失败: {ex}");
                EditorUtility.DisplayDialog("发布失败", ex.Message, "确定");
            }
            finally
            {
                _isPublishing = false;
                ForceMenuTreeRebuild();
                Repaint();
            }
        }

        private string GetBuildBlockReason()
        {
            if (_profile == null)
                return "未找到构建配置，请先创建或恢复配置。";
            if (EditorApplication.isCompiling)
                return "Unity 正在编译脚本，请稍候。";
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "请先退出运行模式再构建。";
            if (_isBuilding || UnityEditor.BuildPipeline.isBuildingPlayer)
                return "已有构建正在执行中。";

            var blocking = ValidateProfile().FirstOrDefault(issue =>
                issue.Severity == BuildIssueSeverity.Error || issue.IsBlocking);
            return blocking == null
                ? "构建暂时不可用。"
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
                BuildLogger.Warn($"[BuildDashboard] 无法读取最近报告: {ex.Message}");
            }
        }

        private static string GetResultMessage(BuildReportData report)
        {
            string profileName = report == null ? string.Empty : report.ProfileName;
            var failed = report?.StageResults?.FirstOrDefault(stage => stage.Status == StageStatus.Failed);
            if (report != null && report.AllPassed)
                return $"配置: {report.ProfileName}\n总耗时: {FormatDuration(report.TotalDurationMs)}";

            return failed == null
                ? "配置: " + profileName + "\n请查看「结果」页和构建报告了解详情。"
                : $"失败阶段: {failed.StageId} - {failed.DisplayName}\n{failed.ErrorMessage}";
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
                BuildLogger.Warn(missingMessage);
        }

        private static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 1000)
                return $"{milliseconds} ms";

            return TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss");
        }

        [HideReferenceObjectPicker]
        public sealed class BuildView
        {
            private readonly BuildDashboardWindow _window;
            private string _hotUpdateVersion;
            private bool _installAfterBuild;
            private string _selectedSmokeDevice;

            public BuildView(BuildDashboardWindow window)
            {
                _window = window;
                _hotUpdateVersion = EditorPrefs.GetString(HotUpdateVersionKey, "1.0.1");
            }

            // ===== 一键打包 =====

            [Title("一键打包")]
            [ShowInInspector, ValueDropdown(nameof(ProfileOptions))]
            [LabelText("构建配置")]
            public BuildProfile ActiveProfile
            {
                get => _window._profile;
                set => _window.SelectProfile(value);
            }

            [ShowInInspector, ReadOnly, LabelText("目标平台")]
            public string Target => _window._profile == null
                ? "-"
                : $"{_window._profile.Environment} / {_window._profile.Platform}";

            [ShowInInspector, ReadOnly, LabelText("版本")]
            public string Version => _window._profile == null
                ? "-"
                : $"{_window._profile.VersionName} (build {_window._profile.VersionCode})";

            [ShowInInspector, ReadOnly, LabelText("产物路径")]
            public string PlayerPath => _window._profile?.GetPlayerPath() ?? "-";

            [ShowInInspector, ReadOnly, LabelText("校验状态")]
            public string ValidationSummary
            {
                get
                {
                    var issues = _window.ValidateProfile();
                    int errors = issues.Count(issue =>
                        issue.Severity == BuildIssueSeverity.Error || issue.IsBlocking);
                    int warnings = issues.Count(issue => issue.Severity == BuildIssueSeverity.Warning);
                    return errors == 0
                        ? warnings == 0 ? "✓ 就绪" : $"⚠ 就绪（{warnings} 个警告）"
                        : $"✗ 被 {errors} 个错误阻止";
                }
            }

            [Title("安装与冒烟（Android）")]
            [ShowInInspector, LabelText("打包后安装到设备")]
            [Tooltip("勾选后，一键打包完成后会自动安装 APK 到选中设备并执行冒烟验证。")]
            public bool InstallAfterBuild
            {
                get => _installAfterBuild;
                set => _installAfterBuild = value;
            }

            [ShowInInspector, ValueDropdown(nameof(SmokeDeviceOptions))]
            [LabelText("目标设备")]
            [EnableIf(nameof(InstallAfterBuild))]
            [Tooltip("选择要安装的设备。列表来自 ADB devices，也可手动输入设备序列号。")]
            public string SelectedSmokeDevice
            {
                get
                {
                    if (string.IsNullOrEmpty(_selectedSmokeDevice))
                        _selectedSmokeDevice = FindFirstOnlineDevice();
                    return _selectedSmokeDevice;
                }
                set => _selectedSmokeDevice = value;
            }

            [Button("刷新设备列表"), GUIColor(0.5f, 0.5f, 0.5f)]
            [EnableIf(nameof(InstallAfterBuild))]
            public void RefreshDevices() => _selectedSmokeDevice = FindFirstOnlineDevice();

            [Button("一键打包", ButtonSizes.Large), GUIColor(0.20f, 0.72f, 0.38f)]
            [EnableIf(nameof(BuildEnabled))]
            public void OneClickBuild()
            {
                // 勾了安装 → 写给 Profile，P8 冒烟自动安装运行
                if (_installAfterBuild && _window._profile != null)
                {
                    _window._profile.SmokeEnabled = true;
                    _window._profile.SmokeDeviceSerial = SelectedSmokeDevice;
                }
                else if (_window._profile != null)
                {
                    _window._profile.SmokeEnabled = false;
                }
                _window.RunBuild();
            }

            [Button("刷新"), GUIColor(0.5f, 0.5f, 0.5f)]
            public void Refresh()
            {
                _window.RefreshProfiles();
                _window.ForceMenuTreeRebuild();
            }

            // ===== 热更补丁 =====

            [Title("发布热更补丁")]
            [ShowInInspector]
            [LabelText("热更版本号")]
            public string HotUpdateVersion
            {
                get => _hotUpdateVersion;
                set
                {
                    _hotUpdateVersion = value;
                    EditorPrefs.SetString(HotUpdateVersionKey, value);
                }
            }

            [ShowInInspector, LabelText("发布后安装到设备")]
            [Tooltip("勾选后，发布完成后会自动安装 APK 到选中设备。")]
            public bool InstallAfterPublish { get; set; }

            [ShowInInspector, ValueDropdown(nameof(SmokeDeviceOptions))]
            [LabelText("目标设备")]
            [EnableIf(nameof(InstallAfterPublish))]
            public string PublishDevice { get; set; }

            [Button("发布热更补丁", ButtonSizes.Large), GUIColor(0.22f, 0.55f, 0.91f)]
            [EnableIf(nameof(PublishEnabled))]
            public void PublishHotUpdate()
            {
                _window.RunPublish(_hotUpdateVersion);

                // 发布完成后安装到设备
                if (InstallAfterPublish)
                {
                    string device = string.IsNullOrEmpty(PublishDevice)
                        ? FindFirstOnlineDevice() : PublishDevice;
                    if (!string.IsNullOrEmpty(device) && _window._profile != null)
                    {
                        InstallApkToDevice(device, _window._profile.GetPlayerPath());
                    }
                }
            }

            // ===== 校验详情 =====

            [Title("校验详情")]
            [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true)]
            public List<IssueEntry> Issues => _window.ValidateProfile()
                .Select(IssueEntry.FromIssue)
                .ToList();

            [ShowInInspector, ReadOnly, MultiLineProperty(4), LabelText("最近错误")]
            [ShowIf(nameof(HasLastError))]
            public string LastError => _window._lastError;

            private bool BuildEnabled => _window.CanBuild();
            private bool PublishEnabled => _window.CanPublish();
            private bool HasLastError => !string.IsNullOrWhiteSpace(_window._lastError);
            private IEnumerable<ValueDropdownItem<BuildProfile>> ProfileOptions =>
                _window.GetProfileOptions();

            private ValueDropdownList<string> SmokeDeviceOptions
            {
                get
                {
                    var list = new ValueDropdownList<string>();
                    var devices = ListOnlineDevices();
                    if (devices.Count == 0)
                        list.Add("(未检测到设备)", "");
                    foreach (string d in devices)
                        list.Add(d, d);
                    if (!string.IsNullOrEmpty(_selectedSmokeDevice) && !devices.Contains(_selectedSmokeDevice))
                        list.Add(_selectedSmokeDevice + " (手动输入)", _selectedSmokeDevice);
                    return list;
                }
            }

            private static List<string> ListOnlineDevices()
            {
                var devices = new List<string>();
                string adb = FindAdbPath();
                if (adb == null) return devices;
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = adb, Arguments = "devices",
                        UseShellExecute = false, RedirectStandardOutput = true,
                        RedirectStandardError = true, CreateNoWindow = true,
                    };
                    using var p = System.Diagnostics.Process.Start(psi);
                    if (p == null) return devices;
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    foreach (string raw in output.Split('\n'))
                    {
                        string line = raw.Trim();
                        if (line.StartsWith("List") || line.Length == 0) continue;
                        if (line.EndsWith("device"))
                            devices.Add(line.Split('\t')[0].Trim());
                    }
                }
                catch { /* ADB 不可用时忽略 */ }
                return devices;
            }

            private static string FindFirstOnlineDevice()
            {
                var devices = ListOnlineDevices();
                return devices.FirstOrDefault() ?? "";
            }

            private static string FindAdbPath()
            {
                foreach (string env in new[] { "ANDROID_SDK_ROOT", "ANDROID_HOME" })
                {
                    string sdk = Environment.GetEnvironmentVariable(env);
                    if (!string.IsNullOrEmpty(sdk))
                    {
                        string p = Path.Combine(sdk, "platform-tools", "adb.exe");
                        if (File.Exists(p)) return p;
                    }
                }
                string unitySdk = EditorPrefs.GetString("AndroidSdkRoot", "");
                if (!string.IsNullOrEmpty(unitySdk))
                {
                    string p = Path.Combine(unitySdk, "platform-tools", "adb.exe");
                    if (File.Exists(p)) return p;
                }
                return null;
            }

            private static void InstallApkToDevice(string device, string apkPath)
            {
                if (!File.Exists(apkPath))
                {
                    BuildLogger.Error($"[Dashboard] APK 不存在: {apkPath}");
                    return;
                }
                string adb = FindAdbPath();
                if (adb == null)
                {
                    BuildLogger.Error("[Dashboard] 未找到 ADB");
                    return;
                }
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = adb,
                        Arguments = $"-s {device} install -r -d \"{apkPath}\"",
                        UseShellExecute = false, RedirectStandardOutput = true,
                        RedirectStandardError = true, CreateNoWindow = true,
                    };
                    using var p = System.Diagnostics.Process.Start(psi);
                    if (p == null) return;
                    string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    BuildLogger.Info($"[Dashboard] 安装到 {device}: {(p.ExitCode == 0 ? "成功" : "失败")}\n{output}");
                }
                catch (Exception ex)
                {
                    BuildLogger.Error($"[Dashboard] 安装失败: {ex.Message}");
                }
            }

        [HideReferenceObjectPicker]
        public sealed class ProfileView
        {
            private readonly BuildDashboardWindow _window;

            public ProfileView(BuildDashboardWindow window)
            {
                _window = window;
            }

            [Title("构建配置")]
            [ShowInInspector, ValueDropdown(nameof(ProfileOptions))]
            [LabelText("配置")]
            public BuildProfile ActiveProfile
            {
                get => _window._profile;
                set => _window.SelectProfile(value);
            }

            [ShowInInspector, InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
            [HideLabel]
            public BuildProfile Settings => _window._profile;

            [Button("定位资源")]
            public void SelectAsset()
            {
                Selection.activeObject = _window._profile;
                if (_window._profile != null)
                    EditorGUIUtility.PingObject(_window._profile);
            }

            [Button("新建配置")]
            public void CreateProfile() => _window.CreateProfile();

            [Button("刷新列表")]
            public void RefreshProfiles()
            {
                _window.RefreshProfiles();
                _window.ForceMenuTreeRebuild();
            }

            private IEnumerable<ValueDropdownItem<BuildProfile>> ProfileOptions =>
                _window.GetProfileOptions();
        }

        [HideReferenceObjectPicker]
        public sealed class StageView
        {
            [Title("P0-P9 构建阶段")]
            [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true)]
            public List<StageEntry> Stages => BuildStageRegistry.GetAll()
                .Select(stage => new StageEntry
                {
                    Order = stage.Order,
                    Id = stage.Id,
                    Name = stage.DisplayName,
                    Category = stage.Category,
                    Policy = FormatPolicy(stage.Policy),
                })
                .ToList();

            private static string FormatPolicy(BuildStagePolicy policy)
            {
                var parts = new System.Text.StringBuilder();
                if ((policy & BuildStagePolicy.Required) != 0) parts.Append("必须 ");
                if ((policy & BuildStagePolicy.Optional) != 0) parts.Append("可选 ");
                if ((policy & BuildStagePolicy.AlwaysRun) != 0) parts.Append("总是执行 ");
                if ((policy & BuildStagePolicy.NoSkip) != 0) parts.Append("禁止跳过 ");
                if ((policy & BuildStagePolicy.Transactional) != 0) parts.Append("事务 ");
                if ((policy & BuildStagePolicy.ProducesArtifacts) != 0) parts.Append("产物 ");
                return parts.ToString().Trim();
            }
        }

        [HideReferenceObjectPicker]
        public sealed class ResultsView
        {
            private readonly BuildDashboardWindow _window;

            public ResultsView(BuildDashboardWindow window)
            {
                _window = window;
            }

            [Title("最近结果")]
            [ShowInInspector, ReadOnly, LabelText("状态")]
            public string Status => _window._lastReport == null
                ? "无报告"
                : _window._lastReport.AllPassed ? "✓ 成功" : "✗ 失败";

            [ShowInInspector, ReadOnly, LabelText("运行编号")]
            public string RunId => _window._lastReport?.RunId ?? "-";

            [ShowInInspector, ReadOnly, LabelText("总耗时")]
            public string Duration => _window._lastReport == null
                ? "-"
                : FormatDuration(_window._lastReport.TotalDurationMs);

            [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true)]
            public List<ResultEntry> Stages => _window._lastReport?.StageResults?
                .Select(ResultEntry.FromResult)
                .ToList() ?? new List<ResultEntry>();

            [Button("打开报告")]
            public void OpenReport()
            {
                string path = _window._profile == null
                    ? null
                    : GetLatestFile(_window._profile.GetOutputDir(), "build_report.md");
                RevealFile(path, "未找到 build_report.md");
            }

            [Button("打开产物")]
            public void OpenArtifacts()
            {
                string path = _window._profile == null ? null : _window._profile.GetArchiveDir();
                RevealFile(path, "产物目录尚不存在");
            }

            [Button("打开输出")]
            public void OpenOutput()
            {
                string path = _window._profile?.GetOutputDir();
                RevealFile(path, "构建输出目录尚不存在");
            }

            [Button("复制 AI 诊断路径")]
            public void CopyHandoffPath()
            {
                string path = _window._profile == null
                    ? null
                    : GetLatestFile(_window._profile.GetOutputDir(), "ai_handoff.json");
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    GUIUtility.systemCopyBuffer = Path.GetFullPath(path);
                    BuildLogger.Info($"[BuildDashboard] 已复制: {path}");
                }
                else
                {
                    BuildLogger.Warn("未找到 ai_handoff.json");
                }
            }
        }

        [Serializable]
        public sealed class IssueEntry
        {
            [TableColumnWidth(70, Resizable = false), LabelText("严重度")]
            public string Severity;
            [TableColumnWidth(160), LabelText("代码")]
            public string Code;
            [LabelText("说明")]
            public string Message;
            [LabelText("建议修复")]
            public string SuggestedFix;

            public static IssueEntry FromIssue(BuildIssue issue)
            {
                return new IssueEntry
                {
                    Severity = issue.Severity == BuildIssueSeverity.Error ? "错误" :
                        issue.Severity == BuildIssueSeverity.Warning ? "警告" : "信息",
                    Code = issue.Code,
                    Message = issue.Message,
                    SuggestedFix = issue.SuggestedFix,
                };
            }
        }

        [Serializable]
        public sealed class StageEntry
        {
            [TableColumnWidth(45, Resizable = false), LabelText("序号")]
            public int Order;
            [TableColumnWidth(100), LabelText("标识")]
            public string Id;
            [LabelText("名称")]
            public string Name;
            [TableColumnWidth(100), LabelText("分类")]
            public string Category;
            [TableColumnWidth(160), LabelText("策略")]
            public string Policy;
        }

        [Serializable]
        public sealed class ResultEntry
        {
            [TableColumnWidth(100), LabelText("阶段")]
            public string Stage;
            [LabelText("名称")]
            public string Name;
            [TableColumnWidth(75, Resizable = false), LabelText("状态")]
            public string Status;
            [TableColumnWidth(90, Resizable = false), LabelText("耗时")]
            public string Duration;
            [LabelText("错误")]
            public string Error;

            public static ResultEntry FromResult(StageExecutionResult result)
            {
                return new ResultEntry
                {
                    Stage = result.StageId,
                    Name = result.DisplayName,
                    Status = result.Status == StageStatus.Passed ? "通过" :
                        result.Status == StageStatus.Failed ? "失败" :
                        result.Status == StageStatus.Skipped ? "跳过" : "未知",
                    Duration = FormatDuration(result.DurationMs),
                    Error = result.ErrorMessage,
                };
            }
        }
    }
}
}
