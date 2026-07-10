using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build.Tests
{
    /// <summary>
    /// BuildProfile-only pipeline smoke tests.
    /// Heavy Unity build stages are covered by integration/manual validation.
    /// </summary>
    public class BuildPipelineTests
    {
        [Test]
        public void BuildProfile_Defaults_AreReasonable()
        {
            var profile = ScriptableObject.CreateInstance<BuildProfile>();

            Assert.AreEqual("New Profile", profile.ProfileName);
            Assert.AreEqual(BuildTarget.StandaloneWindows64, profile.Platform);
            Assert.AreEqual("DefaultPackage", profile.PackageName);
            Assert.IsTrue(profile.DevelopmentBuild);
            Assert.IsTrue(profile.SmokeEnabled);
        }

        [Test]
        public void BuildProfile_GetOutputDir_UsesOutputRoot()
        {
            var profile = ScriptableObject.CreateInstance<BuildProfile>();
            profile.OutputRoot = "BuildBackup/Test";

            Assert.AreEqual("BuildBackup/Test", profile.GetOutputDir());
        }

        [Test]
        public void BuildProfile_GetPlayerPath_ReturnsPlatformExtension()
        {
            var profile = ScriptableObject.CreateInstance<BuildProfile>();
            profile.OutputRoot = "BuildBackup/Test";

            profile.Platform = BuildTarget.StandaloneWindows64;
            Assert.AreEqual("BuildBackup/Test/KJ.exe", profile.GetPlayerPath());

            profile.Platform = BuildTarget.Android;
            Assert.AreEqual("BuildBackup/Test/KJ.apk", profile.GetPlayerPath());
        }

        [Test]
        public void BuildProfile_ComputeProfileHash_ChangesWhenBuildInputsChange()
        {
            var profile = ScriptableObject.CreateInstance<BuildProfile>();
            string before = profile.ComputeProfileHash();

            profile.VersionName = "9.9.9";
            string after = profile.ComputeProfileHash();

            Assert.AreNotEqual(before, after);
        }

        [Test]
        public void BuildPaths_DerivesExpectedDirectories()
        {
            var profile = ScriptableObject.CreateInstance<BuildProfile>();
            profile.OutputRoot = "BuildBackup/TestPaths";

            var paths = new BuildPaths(profile);

            Assert.AreEqual("BuildBackup/TestPaths", paths.ArchiveRoot);
            Assert.AreEqual(Path.Combine("BuildBackup/TestPaths", "artifacts"), paths.ArtifactsDir);
            Assert.AreEqual(Path.Combine("BuildBackup/TestPaths", "reports"), paths.ReportsDir);
            Assert.AreEqual(Path.Combine("BuildBackup/TestPaths", "state"), paths.StateDir);
        }

        [Test]
        public void BuildStageRegistry_RegistersP0ToP9InOrder()
        {
            var stages = BuildStageRegistry.GetAll();

            Assert.AreEqual(10, stages.Count);
            Assert.AreEqual("P0.Plan", stages[0].Id);
            Assert.AreEqual("P9.Report", stages[9].Id);
            CollectionAssert.IsEmpty(BuildStageRegistry.ValidateDependencies());
        }

        [Test]
        public void BuildReportData_DefaultLists_AreNonNull()
        {
            var report = new BuildReportData();

            Assert.NotNull(report.StageResults);
            Assert.NotNull(report.Issues);
            Assert.AreEqual("1.0.0", report.SchemaVersion);
        }

        [Test]
        public void StageExecutionResult_Skipped_CountsAsPassed()
        {
            var result = new StageExecutionResult { Status = StageStatus.Skipped };

            Assert.IsTrue(result.Passed);
        }

        [Test]
        public void BuildTransaction_Rollback_RestoresSnapshotFile()
        {
            string dir = Path.Combine("Temp", "BuildPipelineTests");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "snapshot.txt");
            File.WriteAllText(file, "before");

            var tx = new BuildTransaction();
            tx.SnapshotFile(file);
            File.WriteAllText(file, "after");
            tx.Rollback();

            Assert.AreEqual("before", File.ReadAllText(file));
            File.Delete(file);
            Directory.Delete(dir);
        }
    }
}
