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
    /// AssetRuntime.WrapFromExistingPackage, and the 10 hot-update assemblies
    /// declared in ProjectSettings/HybridCLRSettings.asset.
    /// </summary>
    public sealed class HybridCLRBootTests
    {
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
            int before = Boot.BootStartupLog.Snapshot.Count;
            Boot.BootStartupLog.Info("info-msg");
            Boot.BootStartupLog.Warn("warn-msg");
            Boot.BootStartupLog.Error("error-msg");

            var snap = Boot.BootStartupLog.Snapshot;
            Assert.That(snap.Count, Is.EqualTo(before + 3));
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
            var e = new Boot.BootAssemblyEntry("Boot", "Boot.dll", "res/Boot", "assets/Boot");
            Assert.That(e.AssemblyName, Is.EqualTo("Boot"));
            Assert.That(e.FileName, Is.EqualTo("Boot.dll"));
            Assert.That(e.ResourcesPath, Is.EqualTo("res/Boot"));
            Assert.That(e.AssetPath, Is.EqualTo("assets/Boot"));
        }

        [Test]
        public void BootMetadataEntry_ConstructorSetsProperties()
        {
            var e = new Boot.BootMetadataEntry("mscorlib", "mscorlib.dll", null, "assets/mscorlib");
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
        public void AssetRuntime_WrapFromExistingPackage_SetsReady()
        {
            var runtime = Framework.Asset.AssetRuntimeFactory.Create();
            var cfg = ScriptableObject.CreateInstance<Framework.Asset.AssetConfig>();
            var pkg = CreateTestPackage("WrapTest");
            Assert.That(runtime.IsReady, Is.False);

            runtime.WrapFromExistingPackage(cfg, pkg);
            Assert.That(runtime.IsReady, Is.True);
        }

        [Test]
        public void AssetRuntime_WrapFromExistingPackage_NullGuards()
        {
            var runtime = Framework.Asset.AssetRuntimeFactory.Create();
            var cfg = ScriptableObject.CreateInstance<Framework.Asset.AssetConfig>();
            var pkg = CreateTestPackage("Guard");

            Assert.That(() => runtime.WrapFromExistingPackage(null, pkg), Throws.ArgumentNullException);
            Assert.That(() => runtime.WrapFromExistingPackage(cfg, null), Throws.ArgumentNullException);
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
        public void HybridCLRSettings_ContainsAllTenHotUpdateAssemblies()
        {
            var path = System.IO.Path.Combine(Application.dataPath, "..", "ProjectSettings", "HybridCLRSettings.asset");
            Assert.That(System.IO.File.Exists(path), Is.True, "HybridCLRSettings.asset not found at " + path);

            var text = System.IO.File.ReadAllText(path);
            var block = text.Split(new[] { "hotUpdateAssemblies:" }, System.StringSplitOptions.None)[1]
                            .Split(new[] { "preserveHotUpdateAssemblies:" }, System.StringSplitOptions.None)[0];
            var actual = block.Split(new[] { '\n' })
                .Select(l => l.Trim())
                .Where(l => l.StartsWith("- "))
                .Select(l => l.Substring(2).Trim())
                .ToList();

            var expected = new System.Collections.Generic.List<string>
            {
                "Boot", "Core", "General", "Project",
                "Pool", "Cache", "Event", "Asset", "Log", "RuntimeLog"
            };
            CollectionAssert.AreEquivalent(expected, actual);
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
            var hotUpdate = new System.Collections.Generic.List<string>
            {
                "Boot", "Core", "General", "Project",
                "Pool", "Cache", "Event", "Asset", "Log", "RuntimeLog"
            };
            var leaks = hotUpdate.Where(h => referenced.Contains(h)).ToList();
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

        [Test]
        public void AllTenHotUpdateAssembliesAreLoaded()
        {
            // Every declared hot-update assembly must actually compile and load.
            // Complements HybridCLRSettings_ContainsAllTenHotUpdateAssemblies (which
            // checks the name list) by proving the asmdefs exist and are loadable.
            var loaded = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name).ToList();
            var expected = new System.Collections.Generic.List<string>
            {
                "Boot", "Core", "General", "Project",
                "Pool", "Cache", "Event", "Asset", "Log", "RuntimeLog"
            };
            var missing = expected.Where(e => !loaded.Contains(e)).ToList();
            Assert.That(missing, Is.Empty,
                "Hot-update assemblies missing from the loaded AppDomain: " + string.Join(", ", missing));
        }
    }
}
