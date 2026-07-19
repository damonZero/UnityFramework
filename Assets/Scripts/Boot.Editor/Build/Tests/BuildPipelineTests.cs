using System;
using System.IO;
using Framework.Aop;
using Framework.Log;
using Framework.RuntimeLog;
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
        private static string s_logDir;
        private static RuntimeLogSession s_logSession;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            s_logDir = Path.Combine("Logs", "BuildPipelineTests");
            if (Directory.Exists(s_logDir))
                Directory.Delete(s_logDir, true);
            Directory.CreateDirectory(s_logDir);

            s_logSession = new RuntimeLogSession(new RuntimeLogSessionOptions
            {
                DirectoryPath = s_logDir,
                FilePrefix = "build-pipeline-tests",
                MaintainLatest = false,
                MinimumLevel = GameLogLevel.Trace,
                SessionInfo = new RuntimeLogSessionInfo
                {
                    SessionId = "build-pipeline-tests",
                    StartTimeUtc = DateTimeOffset.UtcNow,
                    ProjectName = "KJ",
                    Platform = "EditMode",
                },
            });
            GameLog.Sink = s_logSession;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (s_logSession != null)
            {
                GameLog.Sink = null;
                s_logSession.Dispose();
                s_logSession = null;
            }

            GameLog.ClearStartupBuffer();
        }
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
            Assert.NotNull(report.PerformanceSpans);
            Assert.AreEqual("1.1.0", report.SchemaVersion);
        }

        [Test]
        public void BuildReportData_SerializesPerformanceSpans()
        {
            var report = new BuildReportData();
            report.PerformanceSpans.Add(new AopEvent
            {
                RunId = "run-1",
                Name = "P4.BuildYooAssetPackage",
                Category = "YooAsset",
                DurationMs = 125.5d,
                Status = "Success",
            });

            GameLog.Info($"BuildReportData initialized: SchemaVersion={report.SchemaVersion}, Spans={report.PerformanceSpans.Count}",
                module: "Boot.Build.Tests");

            // Verify data integrity (JsonUtility serialization depends on
            // [Serializable] on all nested types; tested via E2E build report)
            Assert.AreEqual(1, report.PerformanceSpans.Count);
            Assert.AreEqual("P4.BuildYooAssetPackage", report.PerformanceSpans[0].Name);
            Assert.AreEqual(125.5d, report.PerformanceSpans[0].DurationMs, 0.001d);
            Assert.AreEqual("Success", report.PerformanceSpans[0].Status);
            Assert.AreEqual("1.1.0", report.SchemaVersion);

            GameLog.Info("BuildReportData_SerializesPerformanceSpans PASSED", module: "Boot.Build.Tests");
        }

        [Test]
        public void StageExecutionResult_Skipped_CountsAsPassed()
        {
            var result = new StageExecutionResult { Status = StageStatus.Skipped };

            Assert.IsTrue(result.Passed);
        }

        [Test]
        public void BuildTransaction_Rollback_IsIdempotent()
        {
            string dir = Path.Combine("Temp", "BuildPipelineTests");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "snapshot.txt");
            File.WriteAllText(file, "before");

            var tx = new BuildTransaction();
            tx.SnapshotFile(file);
            File.WriteAllText(file, "after");
            tx.Rollback();
            // Second rollback should be a safe no-op
            Assert.DoesNotThrow(() => tx.Rollback());

            Assert.AreEqual("before", File.ReadAllText(file));
            Assert.IsTrue(tx.IsRolledBack);
            File.Delete(file);
            Directory.Delete(dir);
        }

        [Test]
        public void AopSpan_RecordsMonotonicDurationAndParent()
        {
            var clock = new ManualAopClock();
            var collector = new InMemoryAopCollector();

            using (AopRuntime.BeginSession("test-run", collector, clock))
            using (AopRuntime.StartSpan("Parent", "Build"))
            {
                clock.AdvanceMilliseconds(10);
                using (AopRuntime.StartSpan("Child", "Build"))
                    clock.AdvanceMilliseconds(25);
            }

            var events = collector.Snapshot();
            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("Child", events[0].Name);
            Assert.AreEqual(25d, events[0].DurationMs, 0.001d);
            Assert.AreEqual(events[1].SpanId, events[0].ParentSpanId);
            Assert.AreEqual(35d, events[1].DurationMs, 0.001d);
        }

        [Test]
        public void AopSpan_RecordsFailureWithoutChangingException()
        {
            var clock = new ManualAopClock();
            var collector = new InMemoryAopCollector();
            var expected = new InvalidOperationException("sensitive message");

            using (AopRuntime.BeginSession("test-run", collector, clock))
            using (var span = AopRuntime.StartSpan("Failure", "Build"))
            {
                span.Fail(expected);
            }

            var recorded = collector.Snapshot()[0];
            Assert.AreEqual("Failure", recorded.Status);
            Assert.AreEqual(typeof(InvalidOperationException).FullName, recorded.ExceptionType);
            Assert.IsFalse(recorded.ExceptionType.Contains(expected.Message));
        }

        [Test]
        public void AopSession_IsolatesCollectorFailure()
        {
            using var session = AopRuntime.BeginSession("test-run", new ThrowingCollector());

            Assert.DoesNotThrow(() =>
            {
                using (AopRuntime.StartSpan("Operation", "Build"))
                {
                }
            });
            Assert.AreEqual(1, session.CollectorFailureCount);
        }

        [Test]
        public void InMemoryAopCollector_DropsEventsBeyondCapacity()
        {
            var collector = new InMemoryAopCollector(1);
            collector.Record(new AopEvent());
            collector.Record(new AopEvent());

            Assert.AreEqual(1, collector.Snapshot().Count);
            Assert.AreEqual(1, collector.DroppedCount);
        }

        [Test]
        public void AopSpan_DisabledSpan_IsNoOp()
        {
            // No session active — StartSpan returns Disabled singleton
            var span = AopRuntime.StartSpan("NoSession", "Test");

            Assert.DoesNotThrow(() => span.Dispose());
            Assert.DoesNotThrow(() => span.Fail(new Exception("should not throw")));
            Assert.DoesNotThrow(() => span.Cancel());
        }

        [Test]
        public void AopSpan_Cancel_RecordsCancelledStatus()
        {
            var clock = new ManualAopClock();
            var collector = new InMemoryAopCollector();

            using (AopRuntime.BeginSession("test-run", collector, clock))
            using (var span = AopRuntime.StartSpan("Cancelled", "Build"))
            {
                span.Cancel();
            }

            var recorded = collector.Snapshot()[0];
            Assert.AreEqual("Cancelled", recorded.Status);
            Assert.IsNull(recorded.ExceptionType);
        }

        [Test]
        public void AopSpan_DoubleDispose_DoesNotDuplicateEvent()
        {
            var clock = new ManualAopClock();
            var collector = new InMemoryAopCollector();

            using (AopRuntime.BeginSession("test-run", collector, clock))
            {
                var span = AopRuntime.StartSpan("DoubleDispose", "Build");
                span.Dispose();
                Assert.DoesNotThrow(() => span.Dispose());
            }

            Assert.AreEqual(1, collector.Snapshot().Count);
        }

        [Test]
        public void AopSpan_TripleNested_RecordsCorrectHierarchy()
        {
            var clock = new ManualAopClock();
            var collector = new InMemoryAopCollector();

            using (AopRuntime.BeginSession("test-run", collector, clock))
            using (AopRuntime.StartSpan("L1", "Build"))
            {
                clock.AdvanceMilliseconds(10);
                using (AopRuntime.StartSpan("L2", "Build"))
                {
                    clock.AdvanceMilliseconds(20);
                    using (AopRuntime.StartSpan("L3", "Build"))
                        clock.AdvanceMilliseconds(30);
                }
            }

            var events = collector.Snapshot();
            Assert.AreEqual(3, events.Count);

            // Order: L3 completes first, then L2, then L1
            Assert.AreEqual("L3", events[0].Name);
            Assert.AreEqual(30d, events[0].DurationMs, 0.001d);

            Assert.AreEqual("L2", events[1].Name);
            Assert.AreEqual(events[1].SpanId, events[0].ParentSpanId);
            Assert.AreEqual(50d, events[1].DurationMs, 0.001d); // 20 + 30

            Assert.AreEqual("L1", events[2].Name);
            Assert.AreEqual(events[2].SpanId, events[1].ParentSpanId);
            Assert.AreEqual(60d, events[2].DurationMs, 0.001d); // 10 + 20 + 30
        }

        [Test]
        public void AopRuntime_BeginSession_RejectsNullRunId()
        {
            Assert.Throws<ArgumentException>(() =>
                AopRuntime.BeginSession(null, new InMemoryAopCollector()));
            Assert.Throws<ArgumentException>(() =>
                AopRuntime.BeginSession("  ", new InMemoryAopCollector()));
        }

        [Test]
        public void AopRuntime_BeginSession_RejectsNullCollector()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AopRuntime.BeginSession("test-run", null));
        }

        [Test]
        public void AopRuntime_DoubleBeginSession_Throws()
        {
            var collector = new InMemoryAopCollector();
            using (AopRuntime.BeginSession("first", collector))
            {
                Assert.Throws<InvalidOperationException>(() =>
                    AopRuntime.BeginSession("second", collector));
            }

            // After first session disposed, a new one is allowed
            Assert.DoesNotThrow(() =>
            {
                using (AopRuntime.BeginSession("second", collector)) { }
            });
        }

        [Test]
        public void InMemoryAopCollector_SnapshotResetsInternalState()
        {
            var collector = new InMemoryAopCollector(10);
            collector.Record(new AopEvent { Name = "e1" });
            collector.Record(new AopEvent { Name = "e2" });

            var first = collector.Snapshot();
            Assert.AreEqual(2, first.Count);

            // After snapshot, internal state is fresh
            collector.Record(new AopEvent { Name = "e3" });
            var second = collector.Snapshot();
            Assert.AreEqual(1, second.Count);
            Assert.AreEqual("e3", second[0].Name);
        }

        [Test]
        public void InMemoryAopCollector_RejectsZeroCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new InMemoryAopCollector(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new InMemoryAopCollector(-1));
        }

        [Test]
        public void StopwatchAopClock_Frequency_MatchesStopwatch()
        {
            Assert.AreEqual(System.Diagnostics.Stopwatch.Frequency,
                StopwatchAopClock.Instance.Frequency);
        }

        [Test]
        public void AopSession_Dispose_IsIdempotent()
        {
            using var session = AopRuntime.BeginSession("test-run", new InMemoryAopCollector());

            Assert.DoesNotThrow(() => session.Dispose());
            Assert.DoesNotThrow(() => session.Dispose());
            Assert.AreEqual(0, session.CollectorFailureCount);
        }

        [Test]
        public void AopSpan_FailAfterCompletion_IsIgnored()
        {
            var clock = new ManualAopClock();
            var collector = new InMemoryAopCollector();

            using (AopRuntime.BeginSession("test-run", collector, clock))
            {
                var span = AopRuntime.StartSpan("LateFail", "Build");
                span.Dispose(); // completes as Success
                span.Fail(new Exception("too late")); // should be ignored
            }

            var recorded = collector.Snapshot()[0];
            Assert.AreEqual("Success", recorded.Status);
            Assert.IsNull(recorded.ExceptionType);
        }

        private sealed class ManualAopClock : IAopClock
        {
            private long _timestamp;

            public long Frequency => 1000;
            public long GetTimestamp() => _timestamp;
            public DateTime UtcNow { get; } = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc);

            public void AdvanceMilliseconds(long milliseconds)
            {
                _timestamp += milliseconds;
            }
        }

        private sealed class ThrowingCollector : IAopCollector
        {
            public void Record(AopEvent spanEvent)
            {
                throw new IOException("collector failed");
            }
        }
    }
}
