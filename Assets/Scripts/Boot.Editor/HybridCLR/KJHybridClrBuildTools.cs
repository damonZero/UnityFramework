using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boot;
using Framework.Asset;
using Framework.Asset.Editor.YooAsset;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using YooAsset.Editor;

namespace Boot.Editor.HybridCLR
{
    public static class KJHybridClrBuildTools
    {
        private const string MenuRoot = "KJ/HybridCLR/";
        private const string HotUpdateAssetRoot = "Assets/GameRes/HotUpdate";
        private const string DllAssetFolder = HotUpdateAssetRoot + "/Dlls";
        private const string MetadataAssetFolder = HotUpdateAssetRoot + "/AotMetadata";
        private const string YooAssetGroupName = "HotUpdate";
        private const string HotUpdateTag = "hotupdate";
        private const string BootScenePath = "Assets/GameRes/Scene/Boot/Main.unity";

        private static void GenerateAllAndSync()
        {
            InstallHybridClrRuntimeIfNeeded();
            EnsureBootSceneInBuildSettings();
            PrebuildCommand.GenerateAll();
            SyncExistingOutputs();
        }

        [MenuItem(MenuRoot + "Generate All Sync And Prepare Boot", priority = 9)]
        public static void GenerateAllSyncAndPrepareBoot()
        {
            GenerateAllAndSync();
            PrepareYooAssetEditorSimulatePackage();
            PrepareBootScene();
            ValidateOutputs();
        }

        [MenuItem(MenuRoot + "Prepare Runtime Assets And Boot", priority = 8)]
        public static void PrepareRuntimeAssetsAndBoot()
        {
            GenerateRuntimeAssetsAndSync();
            PrepareYooAssetEditorSimulatePackage();
            PrepareBootScene();
            ValidateOutputs();
        }

        [MenuItem(MenuRoot + "Prepare YooAsset Editor Simulate Package", priority = 14)]
        public static void PrepareYooAssetEditorSimulatePackage()
        {
            EnsureAssetFolders();
            var config = LoadAssetConfig();
            var packageName = GetAssetPackageName(config);

            EnsureYooAssetCollector(packageName);
            var result = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualRawBundle);
            if (result == null || string.IsNullOrWhiteSpace(result.PackageRootDirectory))
                throw new InvalidOperationException("YooAsset EditorSimulate build did not return a package root directory.");

            config.EditorSimulatePackageRoot = result.PackageRootDirectory;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[KJHybridClrBuildTools] Prepared YooAsset EditorSimulate package '{packageName}': {result.PackageRootDirectory}");
        }

        [MenuItem(MenuRoot + "Generate Runtime Assets And Sync", priority = 11)]
        public static void GenerateRuntimeAssetsAndSync()
        {
            InstallHybridClrRuntimeIfNeeded();
            EnsureBootSceneInBuildSettings();

            var target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);
            GenerateAotMetadataIfNeeded(target);
            SyncExistingOutputs();
        }

        [MenuItem(MenuRoot + "Maintenance/Install HybridCLR Runtime", priority = 1)]
        public static void InstallHybridClrRuntime()
        {
            var installer = new InstallerController();
            installer.InstallDefaultHybridCLR();
            if (!installer.HasInstalledHybridCLR())
                throw new InvalidOperationException("HybridCLR runtime installation failed.");

            Debug.Log("[KJHybridClrBuildTools] HybridCLR runtime is installed.");
        }

        private static void SyncExistingOutputs()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var settings = SettingsUtil.HybridCLRSettings;

            EnsureAssetFolders();
            CleanObsoleteSyncedFiles(DllAssetFolder, BuildExpectedHotUpdateFileNames());
            CleanObsoleteSyncedFiles(MetadataAssetFolder, BuildExpectedMetadataFileNames(settings.patchAOTAssemblies ?? Array.Empty<string>()));
            var hotUpdateEntries = CopyHotUpdateAssemblies(target);
            var metadataEntries = CopyAotMetadataAssemblies(target, settings.patchAOTAssemblies ?? Array.Empty<string>());

            AssetDatabase.Refresh();
            EnsureYooAssetCollector(GetConfiguredAssetPackageName());

            Debug.Log(
                $"[KJHybridClrBuildTools] Synced {hotUpdateEntries.Count} hot-update assemblies and {metadataEntries.Count} AOT metadata assemblies for {target}.");
        }

        private static void ApplyToOpenEntry()
        {
            var entry = UnityEngine.Object.FindObjectOfType<Entry>(true);
            if (entry == null)
                throw new InvalidOperationException("No Boot.Entry found in the open scenes.");

            var target = EditorUserBuildSettings.activeBuildTarget;
            var settings = SettingsUtil.HybridCLRSettings;

            var hotUpdateEntries = BuildHotUpdateEntries(target, requireFiles: false);
            var metadataEntries = BuildMetadataEntries(target, settings.patchAOTAssemblies ?? Array.Empty<string>(), requireFiles: false);

            var serialized = new SerializedObject(entry);
            var startupSettings = serialized.FindProperty("startupSettings");
            if (startupSettings == null)
                throw new InvalidOperationException("Boot.Entry.startupSettings serialized property was not found.");

            startupSettings.FindPropertyRelative("assetDownloadTag").stringValue = HotUpdateTag;
            startupSettings.FindPropertyRelative("startupTypeName").stringValue = "Project.Bootstrap.ProjectStartup, Project";
            startupSettings.FindPropertyRelative("startupMethodName").stringValue = "Start";
            ApplyMetadataEntries(startupSettings.FindPropertyRelative("aotMetadataAssemblies"), metadataEntries);
            ApplyAssemblyEntries(startupSettings.FindPropertyRelative("hotUpdateAssemblies"), hotUpdateEntries);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(entry);
            EditorSceneManager.MarkSceneDirty(entry.gameObject.scene);

            Debug.Log($"[KJHybridClrBuildTools] Applied HybridCLR startup entries to {entry.name}.");
        }

        [MenuItem(MenuRoot + "Maintenance/Prepare Boot Scene", priority = 20)]
        public static void PrepareBootScene()
        {
            OpenBootScene();
            ApplyToOpenEntry();
            EnsureBootSceneInBuildSettings();

            var scene = SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save boot scene: {BootScenePath}");

            AssetDatabase.SaveAssets();
            Debug.Log($"[KJHybridClrBuildTools] Prepared boot scene and build settings: {BootScenePath}.");
        }

        [MenuItem(MenuRoot + "Maintenance/Validate Outputs", priority = 30)]
        public static void ValidateOutputs()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var settings = SettingsUtil.HybridCLRSettings;
            var missing = new List<string>();

            foreach (var entry in BuildHotUpdateEntries(target, requireFiles: false))
            {
                if (!File.Exists(ToProjectPath(entry.AssetPath)))
                    missing.Add(entry.AssetPath);
            }

            foreach (var entry in BuildMetadataEntries(target, settings.patchAOTAssemblies ?? Array.Empty<string>(), requireFiles: false))
            {
                if (!File.Exists(ToProjectPath(entry.AssetPath)))
                    missing.Add(entry.AssetPath);
            }

            if (missing.Count > 0)
                throw new FileNotFoundException("[KJHybridClrBuildTools] Missing synced HybridCLR assets:\n" + string.Join("\n", missing));

            ValidateNoUnexpectedSyncedDlls(DllAssetFolder, BuildExpectedHotUpdateFileNames(), "hot-update");
            ValidateNoUnexpectedSyncedDlls(MetadataAssetFolder, BuildExpectedMetadataFileNames(settings.patchAOTAssemblies ?? Array.Empty<string>()), "AOT metadata");

            Debug.Log($"[KJHybridClrBuildTools] HybridCLR synced assets are valid for {target}.");
        }

        private static List<BootAssemblyEntry> CopyHotUpdateAssemblies(BuildTarget target)
        {
            var entries = BuildHotUpdateEntries(target, requireFiles: true);
            foreach (var entry in entries)
            {
                var sourcePath = Path.Combine(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target), entry.AssemblyName + ".dll");
                CopyDllAsBytes(ToProjectPath(sourcePath), entry.AssetPath);
            }

            return entries;
        }

        private static List<BootMetadataEntry> CopyAotMetadataAssemblies(BuildTarget target, IReadOnlyList<string> assemblyNames)
        {
            var entries = BuildMetadataEntries(target, assemblyNames, requireFiles: true);
            foreach (var entry in entries)
            {
                var sourcePath = Path.Combine(SettingsUtil.GetAssembliesPostIl2CppStripDir(target), entry.AssemblyName + ".dll");
                CopyDllAsBytes(ToProjectPath(sourcePath), entry.AssetPath);
            }

            return entries;
        }

        private static List<BootAssemblyEntry> BuildHotUpdateEntries(BuildTarget target, bool requireFiles)
        {
            var sourceDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            return GetConfiguredHotUpdateAssemblyNames()
                .Select(name =>
                {
                    var sourcePath = Path.Combine(sourceDir, name + ".dll");
                    if (requireFiles && !File.Exists(ToProjectPath(sourcePath)))
                        throw new FileNotFoundException($"Hot-update DLL not found. Run '{MenuRoot}Generate Runtime Assets And Sync' first.", sourcePath);

                    var fileName = $"Dlls/{name}.dll.bytes";
                    var assetPath = $"{DllAssetFolder}/{name}.dll.bytes";
                    return new BootAssemblyEntry(name, fileName, null, assetPath);
                })
                .ToList();
        }

        private static List<BootMetadataEntry> BuildMetadataEntries(BuildTarget target, IReadOnlyList<string> assemblyNames, bool requireFiles)
        {
            var sourceDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            return (assemblyNames ?? Array.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .Select(name =>
                {
                    var sourcePath = Path.Combine(sourceDir, name + ".dll");
                    if (requireFiles && !File.Exists(ToProjectPath(sourcePath)))
                        throw new FileNotFoundException($"AOT metadata DLL not found. Run '{MenuRoot}Prepare Runtime Assets And Boot' first.", sourcePath);

                    var fileName = $"AotMetadata/{name}.dll.bytes";
                    var assetPath = $"{MetadataAssetFolder}/{name}.dll.bytes";
                    return new BootMetadataEntry(name, fileName, null, assetPath);
                })
                .ToList();
        }

        private static List<string> GetConfiguredHotUpdateAssemblyNames()
        {
            var names = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidOperationException("HybridCLR hot-update assembly name cannot be empty.");

                if (!seen.Add(name))
                    throw new InvalidOperationException($"HybridCLR hot-update assembly is duplicated: {name}");

                ValidateRuntimePreloadAssemblyName(name);
                result.Add(name);
            }

            if (result.Count == 0)
                throw new InvalidOperationException("HybridCLR hot-update assemblies are not configured.");

            return result;
        }

        private static void ValidateRuntimePreloadAssemblyName(string assemblyName)
        {
            switch (assemblyName)
            {
                case "Launcher": // AOT shell - must never be published as a hot-update assembly
                case "TestKit":  // test-only assembly, not product code
                    throw new InvalidOperationException(
                        $"Assembly '{assemblyName}' is not supported by the current runtime preload publication path. This menu writes the Entry hot-update list used after Boot has already started, so startup/stable-contract assemblies need a dedicated startup-update manifest, load order, and restart policy before they are added. This is not an app-package requirement by itself.");
            }
        }

        private static HashSet<string> BuildExpectedHotUpdateFileNames()
        {
            return new HashSet<string>(
                GetConfiguredHotUpdateAssemblyNames().Select(name => name + ".dll.bytes"),
                StringComparer.Ordinal);
        }

        private static HashSet<string> BuildExpectedMetadataFileNames(IReadOnlyList<string> assemblyNames)
        {
            return new HashSet<string>(
                (assemblyNames ?? Array.Empty<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.Ordinal)
                    .Select(name => name + ".dll.bytes"),
                StringComparer.Ordinal);
        }

        private static void CopyDllAsBytes(string sourcePath, string assetPath)
        {
            var destinationPath = ToProjectPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException(destinationPath));
            File.Copy(sourcePath, destinationPath, true);
        }

        private static void CleanObsoleteSyncedFiles(string assetFolder, HashSet<string> expectedFileNames)
        {
            var folderPath = ToProjectPath(assetFolder);
            if (!Directory.Exists(folderPath))
                return;

            foreach (var filePath in Directory.GetFiles(folderPath, "*.dll.bytes", SearchOption.TopDirectoryOnly))
            {
                if (expectedFileNames.Contains(Path.GetFileName(filePath)))
                    continue;

                File.Delete(filePath);
                var metaPath = filePath + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
            }
        }

        private static void ValidateNoUnexpectedSyncedDlls(string assetFolder, HashSet<string> expectedFileNames, string label)
        {
            var folderPath = ToProjectPath(assetFolder);
            if (!Directory.Exists(folderPath))
                return;

            var unexpected = Directory.GetFiles(folderPath, "*.dll.bytes", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !expectedFileNames.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            if (unexpected.Length > 0)
            {
                throw new InvalidOperationException(
                    $"[KJHybridClrBuildTools] Unexpected synced {label} DLL assets in {assetFolder}:\n" + string.Join("\n", unexpected));
            }
        }

        private static void EnsureAssetFolders()
        {
            EnsureFolder("Assets/GameRes", "HotUpdate");
            EnsureFolder(HotUpdateAssetRoot, "Dlls");
            EnsureFolder(HotUpdateAssetRoot, "AotMetadata");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var fullPath = $"{parent}/{child}";
            if (AssetDatabase.IsValidFolder(fullPath))
                return;

            AssetDatabase.CreateFolder(parent, child);
        }

        private static void OpenBootScene()
        {
            if (!File.Exists(ToProjectPath(BootScenePath)))
                throw new FileNotFoundException("Boot scene was not found.", BootScenePath);

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Opening boot scene was cancelled.");

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && string.Equals(activeScene.path, BootScenePath, StringComparison.Ordinal))
                return;

            EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
        }

        private static void InstallHybridClrRuntimeIfNeeded()
        {
            var installer = new InstallerController();
            if (installer.HasInstalledHybridCLR() && installer.PackageVersion == installer.InstalledLibil2cppVersion)
                return;

            Debug.Log("[KJHybridClrBuildTools] Installing HybridCLR runtime before generation.");
            installer.InstallDefaultHybridCLR();
            if (!installer.HasInstalledHybridCLR())
                throw new InvalidOperationException("HybridCLR runtime installation failed.");
        }

        private static void GenerateAotMetadataIfNeeded(BuildTarget target)
        {
            var settings = SettingsUtil.HybridCLRSettings;
            var requiredAssemblies = settings.patchAOTAssemblies ?? Array.Empty<string>();
            if (requiredAssemblies.Length == 0)
                return;

            var sourceDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            var missing = requiredAssemblies
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .Where(name => !File.Exists(ToProjectPath(Path.Combine(sourceDir, name + ".dll"))))
                .ToArray();

            if (missing.Length == 0)
                return;

            Debug.Log(
                "[KJHybridClrBuildTools] Generating stripped AOT metadata DLLs for missing assemblies:\n" + string.Join("\n", missing));
            StripAOTDllCommand.GenerateStripedAOTDlls(target);
        }

        private static void EnsureBootSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var scene = scenes.FirstOrDefault(item => string.Equals(item.path, BootScenePath, StringComparison.Ordinal));
            if (scene == null)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(BootScenePath, true));
            }
            else
            {
                scene.enabled = true;
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureYooAssetCollector()
        {
            EnsureYooAssetCollector(GetConfiguredAssetPackageName());
        }

        private static void EnsureYooAssetCollector(string packageName)
        {
            var package = GetOrCreatePackage(packageName);
            package.IgnoreRuleName = nameof(KJAssetIgnoreRule);
            var group = GetOrCreateGroup(package, YooAssetGroupName);
            group.AssetTags = HotUpdateTag;
            group.ActiveRuleName = nameof(EnableGroup);

            UpsertRawFileCollector(group, DllAssetFolder);
            UpsertRawFileCollector(group, MetadataAssetFolder);
            BundleCollectorSettingData.ModifyPackage(package);
            BundleCollectorSettingData.ModifyGroup(package, group);
            BundleCollectorSettingData.SaveFile();
        }

        private static BundleCollectorPackage GetOrCreatePackage(string packageName)
        {
            var setting = BundleCollectorSettingData.Setting;
            var package = setting.Packages.FirstOrDefault(item => item.PackageName == packageName);
            return package ?? BundleCollectorSettingData.CreatePackage(packageName);
        }

        private static BundleCollectorGroup GetOrCreateGroup(BundleCollectorPackage package, string groupName)
        {
            var group = package.Groups.FirstOrDefault(item => item.GroupName == groupName);
            return group ?? BundleCollectorSettingData.CreateGroup(package, groupName);
        }

        private static void UpsertRawFileCollector(BundleCollectorGroup group, string collectPath)
        {
            var collector = group.Collectors.FirstOrDefault(item => item.CollectPath == collectPath);
            if (collector == null)
            {
                collector = new BundleCollector();
                BundleCollectorSettingData.CreateCollector(group, collector);
            }

            collector.CollectPath = collectPath;
            collector.CollectorGUID = AssetDatabase.AssetPathToGUID(collectPath);
            collector.CollectorType = ECollectorType.MainAssetCollector;
            collector.AddressRuleName = nameof(AddressDisable);
            collector.PackRuleName = nameof(PackRawFile);
            collector.FilterRuleName = nameof(CollectAll);
            collector.AssetTags = HotUpdateTag;
            collector.UserData = string.Empty;
            BundleCollectorSettingData.ModifyCollector(group, collector);
        }

        private static AssetConfig LoadAssetConfig()
        {
            EnsureFolder("Assets", "Resources");

            const string configPath = "Assets/Resources/AssetConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<AssetConfig>(configPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<AssetConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return config;
        }

        private static string GetConfiguredAssetPackageName()
        {
            return GetAssetPackageName(LoadAssetConfig());
        }

        private static string GetAssetPackageName(AssetConfig config)
        {
            return string.IsNullOrWhiteSpace(config.PackageName) ? "DefaultPackage" : config.PackageName;
        }

        private static void ApplyAssemblyEntries(SerializedProperty property, IReadOnlyList<BootAssemblyEntry> entries)
        {
            property.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("assemblyName").stringValue = entry.AssemblyName;
                item.FindPropertyRelative("fileName").stringValue = entry.FileName;
                item.FindPropertyRelative("resourcesPath").stringValue = entry.ResourcesPath;
                item.FindPropertyRelative("assetPath").stringValue = entry.AssetPath;
            }
        }

        private static void ApplyMetadataEntries(SerializedProperty property, IReadOnlyList<BootMetadataEntry> entries)
        {
            property.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("assemblyName").stringValue = entry.AssemblyName;
                item.FindPropertyRelative("fileName").stringValue = entry.FileName;
                item.FindPropertyRelative("resourcesPath").stringValue = entry.ResourcesPath;
                item.FindPropertyRelative("assetPath").stringValue = entry.AssetPath;
            }
        }

        private static string ToProjectPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? ".", assetPath));
        }
    }
}
