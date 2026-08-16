using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Framework.Asset;
using NUnit.Framework;
using UnityEngine;
using YooAsset;

namespace Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the HybridCLR hot-update boot boundary (HYB-03):
    /// the AOT Launcher shell (BootStartupLog / BootBridge / BootRemoteService),
    /// the AOT-shared AssetConfig, the hot-update Boot.BootUpdateRunner contract,
    /// AssetRuntime.WrapFromExistingPackage, and the hot-update assemblies
    /// declared in ProjectSettings/HybridCLRSettings.asset (validated dynamically
    /// against the asmdef dependency graph rather than a hardcoded list).
    /// </summary>
    public sealed class HybridCLRBootTests
    {
        /// <summary>
        /// 从唯一事实源 <c>ProjectSettings/HybridCLRSettings.asset</c> 动态读取 hotUpdateAssemblies。
        /// 测试不再硬编码程序集清单 —— 新增热更程序集只需改 asset，结构校验自动覆盖。
        /// </summary>
        private static string[] ReadHotUpdateAssemblies()
        {
            var path = System.IO.Path.Combine(Application.dataPath, "..", "ProjectSettings", "HybridCLRSettings.asset");
            var result = new List<string>();
            var inBlock = false;

            foreach (var raw in System.IO.File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (!inBlock)
                {
                    if (line == "hotUpdateAssemblies:")
                    {
                        inBlock = true;
                        continue;
                    }
                    continue;
                }

                if (line.StartsWith("- "))
                {
                    result.Add(line.Substring(2).Trim());
                    continue;
                }

                break; // 块结束（下一个顶层 key）
            }

            return result.ToArray();
        }

        /// <summary>
        /// 扫描 <c>Assets/</c> 下所有 .asmdef，返回 name → 引用名集合。
        /// 用于校验清单里的程序集确实存在、以及依赖顺序合法。
        /// </summary>
        private static Dictionary<string, HashSet<string>> ReadAsmdefReferences()
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var asmdefPath in System.IO.Directory.GetFiles(
                         Application.dataPath, "*.asmdef", System.IO.SearchOption.AllDirectories))
            {
                var text = System.IO.File.ReadAllText(asmdefPath);
                var nameMatch = System.Text.RegularExpressions.Regex.Match(text, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                if (!nameMatch.Success)
                    continue;

                var name = nameMatch.Groups[1].Value;
                var refs = new HashSet<string>(StringComparer.Ordinal);
                var refMatch = System.Text.RegularExpressions.Regex.Match(
                    text, "\"references\"\\s*:\\s*\\[(?<body>.*?)\\]",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                if (refMatch.Success)
                {
                    foreach (System.Text.RegularExpressions.Match m in
                             System.Text.RegularExpressions.Regex.Matches(refMatch.Groups["body"].Value, "\"([^\"]+)\""))
                    {
                        refs.Add(m.Groups[1].Value);
                    }
                }

                map[name] = refs;
            }

            return map;
        }

        private static ResourcePackage CreateTestPackage(string name)
        {
            var ctor = typeof(ResourcePackage).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            Assert.That(ctor, Is.Not.Null, "YooAsset ResourcePackage internal ctor(string) not found.");
            return (ResourcePackage)ctor.Invoke(new object[] { name });
        }

        [Test]
        public void BootStartupLog_RecordsSnapshotEntries()
        {
            // Snapshot 是「快照并清空」语义：先消费历史条目，避免其它测试残留污染基线。
            _ = Boot.BootStartupLog.Snapshot;

            Boot.BootStartupLog.Info("info-msg");
            Boot.BootStartupLog.Warn("warn-msg");
            Boot.BootStartupLog.Error("error-msg");

            var snap = Boot.BootStartupLog.Snapshot;
            Assert.That(snap.Count, Is.EqualTo(3));
            Assert.That(snap.Any(e => e.Level == Boot.BootStartupLogLevel.Info && e.Message == "info-msg"), Is.True);
            Assert.That(snap.Any(e => e.Level == Boot.BootStartupLogLevel.Warn && e.Message == "warn-msg"), Is.True);
            Assert.That(snap.Any(e => e.Level == Boot.BootStartupLogLevel.Error && e.Message == "error-msg"), Is.True);
        }

        [Test]
        public void BootRemoteService_DefaultUrlUsesBaseUrl()
        {
            var svc = new Boot.BootRemoteService("http://cdn.example.com");
            var urls = svc.GetRemoteUrls("asset_1.bundle");
            Assert.That(urls, Is.Not.Null);
            Assert.That(urls.Count, Is.EqualTo(1));
            Assert.That(urls[0], Is.EqualTo("http://cdn.example.com/asset_1.bundle"));
        }

        [Test]
        public void BootRemoteService_CustomUrlProviderOverrides()
        {
            Boot.BootRemoteService.CustomUrlProvider = fn => new List<string> { "http://a/" + fn, "http://b/" + fn };
            try
            {
                var svc = new Boot.BootRemoteService("http://ignored");
                var urls = svc.GetRemoteUrls("x.bundle");
                Assert.That(urls.Count, Is.EqualTo(2));
                Assert.That(urls[0], Is.EqualTo("http://a/x.bundle"));
                Assert.That(urls[1], Is.EqualTo("http://b/x.bundle"));
            }
            finally
            {
                Boot.BootRemoteService.CustomUrlProvider = null;
            }
        }

        [Test]
        public void AssetConfig_PlayModeHasExpectedValues()
        {
            var values = Enum.GetNames(typeof(Framework.Asset.AssetConfig.PlayMode));
            Assert.That(values, Does.Contain("EditorSimulate"));
            Assert.That(values, Does.Contain("Offline"));
            Assert.That(values, Does.Contain("Host"));

            var cfg = ScriptableObject.CreateInstance<Framework.Asset.AssetConfig>();
            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg.Mode, Is.EqualTo(Framework.Asset.AssetConfig.PlayMode.EditorSimulate));
        }

        [Test]
        public void BootAssemblyEntry_ConstructorSetsProperties()
        {
            var e = new Boot.BootAssemblyEntry("Boot", "Boot.dll", "assets/Boot");
            Assert.That(e.AssemblyName, Is.EqualTo("Boot"));
            Assert.That(e.FileName, Is.EqualTo("Boot.dll"));
            Assert.That(e.AssetPath, Is.EqualTo("assets/Boot"));
        }

        [Test]
        public void BootMetadataEntry_ConstructorSetsProperties()
        {
            var e = new Boot.BootMetadataEntry("mscorlib", "mscorlib.dll", "assets/mscorlib");
            Assert.That(e.AssemblyName, Is.EqualTo("mscorlib"));
            Assert.That(e.FileName, Is.EqualTo("mscorlib.dll"));
            Assert.That(e.AssetPath, Is.EqualTo("assets/mscorlib"));
        }

        [Test]
        public void BootStartupSettings_Defaults()
        {
            var s = new Boot.BootStartupSettings();
            Assert.That(s.EnableHotUpdate, Is.True);
            Assert.That(s.EnableAssetUpdate, Is.True);
            Assert.That(s.SkipHotUpdateInEditor, Is.True);
            Assert.That(s.HotUpdateAssemblies, Is.Not.Null);
            Assert.That(s.AotMetadataAssemblies, Is.Not.Null);
        }

        [Test]
        public void BootBridge_ExposesState_AndEmptyEarlyLogsWhenNull()
        {
            var pkg = CreateTestPackage("TestPackage");
            var cfg = ScriptableObject.CreateInstance<Framework.Asset.AssetConfig>();
            var settings = new Boot.BootStartupSettings();
            var bridge = new Boot.BootBridge(pkg, settings, null, cfg, null);

            Assert.That(bridge.Package, Is.SameAs(pkg));
            Assert.That(bridge.Settings, Is.SameAs(settings));
            Assert.That(bridge.View, Is.Null);
            Assert.That(bridge.Config, Is.SameAs(cfg));
            Assert.That(bridge.EarlyLogs, Is.Not.Null);
            Assert.That(bridge.EarlyLogs.Count, Is.EqualTo(0));
        }

        [Test]
        public void AssetRuntime_CreateFromPackage_SetsReady()
        {
            var cfg = ScriptableObject.CreateInstance<Framework.Asset.AssetConfig>();
            var pkg = CreateTestPackage("WrapTest");

            var runtime = Framework.Asset.AssetRuntimeFactory.CreateFromPackage(cfg, pkg);
            Assert.That(runtime.IsReady, Is.True);
        }

        [Test]
        public void AssetRuntime_CreateFromPackage_NullGuards()
        {
            var cfg = ScriptableObject.CreateInstance<Framework.Asset.AssetConfig>();
            var pkg = CreateTestPackage("Guard");

            Assert.That(() => Framework.Asset.AssetRuntimeFactory.CreateFromPackage(null, pkg), Throws.ArgumentNullException);
            Assert.That(() => Framework.Asset.AssetRuntimeFactory.CreateFromPackage(cfg, null), Throws.ArgumentNullException);
        }

        [Test]
        public void BootUpdateRunner_HasStaticStartTakingBootBridge()
        {
            var t = typeof(Boot.BootUpdateRunner);
            var m = t.GetMethod("Start", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Boot.BootBridge) }, null);
            Assert.That(m, Is.Not.Null,
                "Boot.BootUpdateRunner.Start(BootBridge) must exist for the AOT BootLoader reflection call.");
        }

        [Test]
        public void HotUpdateAssemblies_ContainLayeredStartupChain()
        {
            var names = ReadHotUpdateAssemblies();
            Assert.That(names, Is.Not.Empty, "hotUpdateAssemblies is empty in HybridCLRSettings.asset.");
            Assert.That(names, Does.Contain("Boot"), "Layered startup chain requires 'Boot' in hotUpdateAssemblies.");
            Assert.That(names, Does.Contain("Core"), "Layered startup chain requires 'Core' in hotUpdateAssemblies.");
            Assert.That(names, Does.Contain("General"), "Layered startup chain requires 'General' in hotUpdateAssemblies.");
            Assert.That(names, Does.Contain("Project"), "Layered startup chain requires 'Project' in hotUpdateAssemblies.");
        }

        [Test]
        public void HotUpdateAssemblies_HaveNoDuplicatesOrEmptyNames()
        {
            var names = ReadHotUpdateAssemblies();
            Assert.That(names, Does.Not.Contain(""), "hotUpdateAssemblies contains an empty assembly name.");
            Assert.That(names, Is.Unique, "hotUpdateAssemblies contains duplicate assembly names.");
        }

        [Test]
        public void HotUpdateAssemblies_AllCorrespondToAsmdef()
        {
            var names = ReadHotUpdateAssemblies();
            var asmdefs = ReadAsmdefReferences();
            var missing = names.Where(n => !asmdefs.ContainsKey(n)).ToList();
            Assert.That(missing, Is.Empty,
                "hotUpdateAssemblies entries have no matching .asmdef: " + string.Join(", ", missing));
        }

        [Test]
        public void HotUpdateAssemblies_AreInDependencyOrder()
        {
            var names = ReadHotUpdateAssemblies();
            var asmdefs = ReadAsmdefReferences();

            var order = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < names.Length; i++)
                order[names[i]] = i;

            foreach (var name in names)
            {
                if (!asmdefs.TryGetValue(name, out var refs))
                    continue; // 存在性已由 HotUpdateAssemblies_AllCorrespondToAsmdef 覆盖

                foreach (var reference in refs)
                {
                    if (order.TryGetValue(reference, out var refIdx) && refIdx >= order[name])
                    {
                        Assert.Fail(
                            $"hotUpdateAssemblies ordering is invalid: '{name}' depends on '{reference}' " +
                            $"which appears after it. Move '{reference}' before '{name}'.");
                    }
                }
            }
        }

        [Test]
        public void Launcher_DoesNotReferenceHotUpdateAssemblies()
        {
            // THE core HYB-03 invariant: the AOT Launcher shell must never take a
            // compile-time dependency on a hot-update assembly. If it did, the
            // asmdef fission would be meaningless and the AOT build would pull
            // hot-update code into the shell. Guard it at runtime.
            var launcherAsm = typeof(Boot.BootLoader).Assembly;
            var referenced = launcherAsm.GetReferencedAssemblies().Select(a => a.Name).ToList();
            var hotUpdate = new HashSet<string>(ReadHotUpdateAssemblies(), StringComparer.Ordinal);
            var leaks = hotUpdate.Where(referenced.Contains).ToList();
            Assert.That(leaks, Is.Empty,
                "Launcher (AOT) references hot-update assemblies -> HYB-03 boundary broken: " + string.Join(", ", leaks));
        }

        [Test]
        public void BootLoader_ResolvesBootUpdateRunnerByAssemblyQualifiedName()
        {
            // BootLoader.ReflectBootUpdateRunnerStart resolves the hot-update entry
            // point with the EXACT string "Boot.BootUpdateRunner, Boot". If the
            // assembly is ever renamed or the type moves, the AOT shell throws at
            // runtime. Mirror that resolution here so a drift fails the build.
            var type = Type.GetType("Boot.BootUpdateRunner, Boot");
            Assert.That(type, Is.Not.Null,
                "BootLoader reflects 'Boot.BootUpdateRunner, Boot'; the type must live in an assembly literally named 'Boot'.");
            Assert.That(type, Is.EqualTo(typeof(Boot.BootUpdateRunner)));

            var method = type.GetMethod("Start",
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Boot.BootBridge) }, null);
            Assert.That(method, Is.Not.Null,
                "Boot.BootUpdateRunner.Start(BootBridge) must exist for the AOT BootLoader reflection handoff.");
        }

        // ── 分层启动链反射契约（layered-startup-chain.md）──

        [Test]
        public void BootStartupSettings_DefaultStartupTypeIsCore()
        {
            // 分层启动链 Phase 1：Boot 的正式入口必须是 Core，不再直接调用 Project。
            var s = new Boot.BootStartupSettings();
            Assert.That(s.StartupTypeName,
                Is.EqualTo("Core.Bootstrap.CoreStartup, Core"),
                "BootStartupSettings default startupTypeName must point to CoreStartup.");
        }

        [Test]
        public void CoreStartup_HasStaticStartTakingIAssetRuntime()
        {
            var t = typeof(Core.Bootstrap.CoreStartup);
            var m = t.GetMethod("Start", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Framework.Asset.IAssetRuntime) }, null);
            Assert.That(m, Is.Not.Null,
                "Core.Bootstrap.CoreStartup.Start(IAssetRuntime) must exist for Boot reflection.");
        }

        [Test]
        public void CoreStartup_HasStaticResetForRepair()
        {
            // Repair 场景：Entry 反射 CoreStartup.Reset() 销毁 scope 后重建完整启动链。
            var t = typeof(Core.Bootstrap.CoreStartup);
            var m = t.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static, null,
                System.Array.Empty<System.Type>(), null);
            Assert.That(m, Is.Not.Null,
                "Core.Bootstrap.CoreStartup.Reset() must exist for Entry.Repair reflection.");
        }

        [Test]
        public void GeneralStartup_HasStaticStartTakingLifetimeScope()
        {
            var t = typeof(General.Bootstrap.GeneralStartup);
            var m = t.GetMethod("Start", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(VContainer.Unity.LifetimeScope) }, null);
            Assert.That(m, Is.Not.Null,
                "General.Bootstrap.GeneralStartup.Start(LifetimeScope) must exist for Core reflection.");
        }

        [Test]
        public void ProjectStartup_HasStaticStartTakingLifetimeScope()
        {
            var t = typeof(Project.Bootstrap.ProjectStartup);
            var m = t.GetMethod("Start", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(VContainer.Unity.LifetimeScope) }, null);
            Assert.That(m, Is.Not.Null,
                "Project.Bootstrap.ProjectStartup.Start(LifetimeScope) must exist for General reflection.");
        }

        [Test]
        public void CoreDoesNotReferenceGeneralOrProject()
        {
            // 依赖方向红线：Core 不编译期引用 General/Project（层间靠反射）。
            var coreAsm = typeof(Core.Bootstrap.CoreStartup).Assembly;
            var referenced = coreAsm.GetReferencedAssemblies().Select(a => a.Name).ToList();
            Assert.That(referenced, Does.Not.Contain("General"));
            Assert.That(referenced, Does.Not.Contain("Project"));
        }

        [Test]
        public void GeneralDoesNotReferenceProject()
        {
            var generalAsm = typeof(General.Bootstrap.GeneralStartup).Assembly;
            var referenced = generalAsm.GetReferencedAssemblies().Select(a => a.Name).ToList();
            Assert.That(referenced, Does.Not.Contain("Project"));
        }
    }
}
