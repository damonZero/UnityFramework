using System.IO;
using Boot.Editor.Build;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build.Tests
{
    /// <summary>
    /// 构建管线单元测试 —— 可自主执行，无需完整构建环境。
    /// 覆盖: BuildConfig / BuildReport / 续跑标记 / Stage 核心逻辑。
    ///
    /// 运行方式: Unity Test Runner → EditMode → Boot.Build.Editor.Tests
    /// </summary>
    public class BuildPipelineTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"KJ_BuildTest_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // ===== BuildConfig Tests =====

        [Test]
        public void BuildConfig_Defaults_AreReasonable()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();

            Assert.AreEqual(BuildTarget.StandaloneWindows64, config.Platform);
            Assert.IsTrue(config.Development, "Development should default to true for smoke testing");
            Assert.AreEqual("DefaultPackage", config.PackageName);
            Assert.IsTrue(config.SmokeEnabled, "Smoke should be enabled by default");
            Assert.IsTrue(config.SmokeTimeoutSec > 0, "Timeout must be positive");

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildConfig_GetOutputDir_UsesCustomDir()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();
            config.OutputDir = "Build/Custom";
            Assert.AreEqual("Build/Custom", config.GetOutputDir());
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildConfig_GetOutputDir_AutoDerives()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();
            config.Platform = BuildTarget.StandaloneWindows64;
            Assert.AreEqual("Build/StandaloneWindows64", config.GetOutputDir());
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildConfig_GetPlayerPath_ReturnsCorrectExtension()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();

            config.Platform = BuildTarget.StandaloneWindows64;
            Assert.IsTrue(config.GetPlayerPath().EndsWith(".exe"));

            config.Platform = BuildTarget.Android;
            Assert.IsTrue(config.GetPlayerPath().EndsWith(".apk"));

            config.Platform = BuildTarget.iOS;
            Assert.IsTrue(config.GetPlayerPath().EndsWith(".ipa"));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildConfig_GetReportPath_DefaultName()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();
            config.Platform = BuildTarget.StandaloneWindows64;
            Assert.IsTrue(config.GetReportPath().Contains("build_report"));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildConfig_Version_IsSettable()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();
            config.Version = "2.3.4";
            Assert.AreEqual("2.3.4", config.Version);
            Object.DestroyImmediate(config);
        }

        // ===== BuildReport Tests =====

        [Test]
        public void BuildReport_AddStage_CreatesCorrectEntry()
        {
            var report = new BuildReport();
            var stage = report.AddStage("S0_PreFlightCheck");

            Assert.AreEqual("S0_PreFlightCheck", stage.name);
            Assert.IsNotNull(stage.startedAt);
            Assert.AreEqual(0f, stage.durationSec);
            Assert.IsFalse(stage.passed);
            Assert.IsFalse(stage.skipped);
            Assert.AreEqual(1, report.stages.Count);
        }

        [Test]
        public void BuildReport_AddStage_MultipleStagesInOrder()
        {
            var report = new BuildReport();
            report.AddStage("S0");
            report.AddStage("S1");
            report.AddStage("S2");

            Assert.AreEqual(3, report.stages.Count);
            Assert.AreEqual("S0", report.stages[0].name);
            Assert.AreEqual("S1", report.stages[1].name);
            Assert.AreEqual("S2", report.stages[2].name);
        }

        [Test]
        public void BuildReport_AddArtifact_ComputesMetadata()
        {
            string testFile = Path.Combine(_tempDir, "test.bundle");
            File.WriteAllBytes(testFile, new byte[1024]);

            var report = new BuildReport();
            report.AddArtifact(testFile, "Test bundle");

            Assert.AreEqual(1, report.artifacts.Count);
            Assert.AreEqual("Test bundle", report.artifacts[0].description);
            Assert.IsTrue(report.artifacts[0].exists);
            Assert.AreEqual(1024, report.artifacts[0].sizeBytes);
            Assert.IsNotEmpty(report.artifacts[0].sha256);
        }

        [Test]
        public void BuildReport_AddArtifact_MissingFile()
        {
            var report = new BuildReport();
            report.AddArtifact("/nonexistent/path/file.bundle", "Missing file");

            Assert.AreEqual(1, report.artifacts.Count);
            Assert.IsFalse(report.artifacts[0].exists);
            Assert.AreEqual(0, report.artifacts[0].sizeBytes);
        }

        [Test]
        public void BuildReport_WriteJson_ProducesValidFile()
        {
            string jsonPath = Path.Combine(_tempDir, "report.json");
            var report = new BuildReport();
            report.platform = "StandaloneWindows64";
            report.packageName = "DefaultPackage";

            var stage = report.AddStage("S0");
            stage.passed = true;
            stage.durationSec = 1.5f;

            report.WriteJson(jsonPath);

            Assert.IsTrue(File.Exists(jsonPath));
            string jsonContent = File.ReadAllText(jsonPath);
            Assert.IsTrue(jsonContent.Contains("S0"));
            Assert.IsTrue(jsonContent.Contains("StandaloneWindows64"));
            Assert.IsTrue(jsonContent.Contains("DefaultPackage"));
        }

        [Test]
        public void BuildReport_WriteMarkdown_ProducesValidFile()
        {
            string mdPath = Path.Combine(_tempDir, "report.md");
            var report = new BuildReport();
            report.platform = "StandaloneWindows64";
            report.totalDuration = "00:05:30";

            var stage = report.AddStage("S0");
            stage.passed = true;
            stage.durationSec = 1.5f;

            var stage2 = report.AddStage("S4");
            stage2.passed = false;
            stage2.errorMessage = "Build failed";

            report.summary.allPassed = false;
            report.summary.failedStage = "S4";

            report.WriteMarkdown(mdPath);

            Assert.IsTrue(File.Exists(mdPath));
            string mdContent = File.ReadAllText(mdPath);
            Assert.IsTrue(mdContent.Contains("# KJ Build Pipeline Report"));
            Assert.IsTrue(mdContent.Contains("S0"));
            Assert.IsTrue(mdContent.Contains("S4"));
            Assert.IsTrue(mdContent.Contains("PASS"));
            Assert.IsTrue(mdContent.Contains("FAIL"));
            Assert.IsTrue(mdContent.Contains("FAILED"));
        }

        [Test]
        public void BuildReport_SmokeConclusion_IsNullIfNotSet()
        {
            var report = new BuildReport();
            Assert.IsNull(report.smoke);
        }

        [Test]
        public void SmokeResult_AllValues_AreDistinct()
        {
            // P1 修复验证：SmokeResult 枚举的每个值语义独立，
            // 确保 NotScheduled / Skipped / Passed / Failed 不会被混淆。
            Assert.AreNotEqual(SmokeResult.NotScheduled, SmokeResult.Passed);
            Assert.AreNotEqual(SmokeResult.Skipped, SmokeResult.Passed);
            Assert.AreNotEqual(SmokeResult.Failed, SmokeResult.Passed);
            Assert.AreNotEqual(SmokeResult.NotScheduled, SmokeResult.Skipped);
            Assert.AreNotEqual(SmokeResult.Skipped, SmokeResult.Failed);
        }

        [Test]
        public void SmokeConclusion_DefaultResult_IsNotScheduled()
        {
            var smoke = new SmokeConclusion();
            Assert.AreEqual(SmokeResult.NotScheduled, smoke.result,
                "Default SmokeResult should be NotScheduled (0), not a passing value");
        }

        [Test]
        public void BuildReport_SmokeConclusion_RecordsMilestones()
        {
            var report = new BuildReport();
            report.smoke = new SmokeConclusion
            {
                enabled = true,
                result = SmokeResult.Passed,
                bootLogPath = "/path/to/boot.log",
                runtimeLogPath = "/path/to/latest.jsonl",
                milestonesFound = new System.Collections.Generic.List<string> { "[Boot]", "ProjectStartup" }
            };

            Assert.IsTrue(report.smoke.enabled);
            Assert.AreEqual(SmokeResult.Passed, report.smoke.result);
            Assert.AreEqual(2, report.smoke.milestonesFound.Count);
        }

        [Test]
        public void BuildReport_BuildSummary_ComputesCorrectly()
        {
            var report = new BuildReport();
            var s0 = report.AddStage("S0");
            s0.passed = true;
            var s1 = report.AddStage("S1");
            s1.passed = true;
            var s2 = report.AddStage("S2");
            s2.passed = false;
            s2.skipped = false;
            var s3 = report.AddStage("S3");
            s3.skipped = true;
            s3.passed = true;

            int passed = 0, failed = 0, skipped = 0;
            foreach (var s in report.stages)
            {
                if (s.skipped) skipped++;
                else if (s.passed) passed++;
                else failed++;
            }

            Assert.AreEqual(2, passed, "S0 and S1 passed");
            Assert.AreEqual(1, failed, "S2 failed");
            Assert.AreEqual(1, skipped, "S3 skipped");
        }

        // ===== Marker / Resume Tests =====

        [Test]
        public void Markers_IsStageDone_ReturnsFalseWhenNoMarker()
        {
            Assert.IsFalse(KJBuildPipeline.IsStageDone("S9_Report"));
        }

        [Test]
        public void Markers_MarkAndCheck_Works()
        {
            KJBuildPipeline.MarkStageDone("S5_ApplyConfig");
            Assert.IsTrue(KJBuildPipeline.IsStageDone("S5_ApplyConfig"));

            // Cleanup
            KJBuildPipeline.ClearAllMarkers();
            Assert.IsFalse(KJBuildPipeline.IsStageDone("S5_ApplyConfig"));
        }

        [Test]
        public void Markers_ClearAllMarkers_RemovesAll()
        {
            KJBuildPipeline.MarkStageDone("S0");
            KJBuildPipeline.MarkStageDone("S1");
            KJBuildPipeline.MarkStageDone("S2");

            Assert.IsTrue(KJBuildPipeline.IsStageDone("S0"));
            Assert.IsTrue(KJBuildPipeline.IsStageDone("S1"));

            KJBuildPipeline.ClearAllMarkers();

            Assert.IsFalse(KJBuildPipeline.IsStageDone("S0"));
            Assert.IsFalse(KJBuildPipeline.IsStageDone("S1"));
            Assert.IsFalse(KJBuildPipeline.IsStageDone("S2"));
        }

        // ===== BuildFailedException Tests =====

        [Test]
        public void BuildFailedException_HasStageName()
        {
            var ex = new BuildFailedException("S4_BuildYooAsset", "YooAsset build failed");
            Assert.AreEqual("S4_BuildYooAsset", ex.StageName);
            Assert.AreEqual("YooAsset build failed", ex.Message);
        }

        [Test]
        public void BuildFailedException_HasInnerException()
        {
            var inner = new System.InvalidOperationException("inner error");
            var ex = new BuildFailedException("S6_BuildPlayer", "Build failed", inner);
            Assert.AreEqual("S6_BuildPlayer", ex.StageName);
            Assert.IsNotNull(ex.InnerException);
            Assert.AreEqual("inner error", ex.InnerException.Message);
        }

        // ===== ArtifactEntry Tests =====

        [Test]
        public void ArtifactEntry_Sha256_DifferentForDifferentContent()
        {
            string file1 = Path.Combine(_tempDir, "file1.txt");
            string file2 = Path.Combine(_tempDir, "file2.txt");
            File.WriteAllText(file1, "content A");
            File.WriteAllText(file2, "content B");

            var report = new BuildReport();
            report.AddArtifact(file1, "A");
            report.AddArtifact(file2, "B");

            Assert.AreNotEqual(report.artifacts[0].sha256, report.artifacts[1].sha256,
                "Different content should produce different SHA256");
        }

        [Test]
        public void ArtifactEntry_Sha256_SameForSameContent()
        {
            string file1 = Path.Combine(_tempDir, "same1.txt");
            string file2 = Path.Combine(_tempDir, "same2.txt");
            File.WriteAllText(file1, "identical content");
            File.WriteAllText(file2, "identical content");

            var report = new BuildReport();
            report.AddArtifact(file1, "copy1");
            report.AddArtifact(file2, "copy2");

            Assert.AreEqual(report.artifacts[0].sha256, report.artifacts[1].sha256,
                "Same content should produce same SHA256");
        }

        // ===== StageResult Tests =====

        [Test]
        public void StageResult_Invariants_CanBeTracked()
        {
            var stage = new StageResult
            {
                name = "S0",
                passed = true,
            };
            stage.invariants.Add("HC runtime installed");
            stage.invariants.Add("Platform match");
            stage.invariants.Add("Boot scene in BuildSettings");

            Assert.AreEqual(3, stage.invariants.Count);
        }

        [Test]
        public void StageResult_Skipped_HasReason()
        {
            var stage = new StageResult
            {
                name = "S8",
                skipped = true,
                skipReason = "Smoke disabled in config",
                passed = true,
            };

            Assert.IsTrue(stage.skipped);
            Assert.IsFalse(string.IsNullOrEmpty(stage.skipReason));
        }

        [Test]
        public void StageResult_Duration_ShouldBeNonNegative()
        {
            var stage = new StageResult();
            Assert.GreaterOrEqual(stage.durationSec, 0f);
        }

        // ===== Config Edge Cases =====

        [Test]
        public void BuildConfig_GetMarkerDir_UnderOutputDir()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();
            config.Platform = BuildTarget.StandaloneWindows64;
            string markerDir = config.GetMarkerDir();
            Assert.IsTrue(markerDir.Contains(".markers"));
            Assert.IsTrue(markerDir.StartsWith(config.GetOutputDir()));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildConfig_Platform_AllSupported()
        {
            var config = ScriptableObject.CreateInstance<BuildConfig>();

            BuildTarget[] supported = {
                BuildTarget.StandaloneWindows64,
                BuildTarget.StandaloneWindows,
                BuildTarget.Android,
                BuildTarget.iOS,
            };

            foreach (var target in supported)
            {
                config.Platform = target;
                Assert.IsFalse(string.IsNullOrEmpty(config.GetPlayerPath()),
                    $"GetPlayerPath should not be empty for {target}");
            }

            Object.DestroyImmediate(config);
        }
    }
}
