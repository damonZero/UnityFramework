using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Framework.Log;
using Framework.RuntimeLog;
using Framework.TestKit.Fakes;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    public sealed class RuntimeLogTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "KJRuntimeLogTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            GameLog.Sink = null;
            GameLog.ClearStartupBuffer();
            GameLog.SetStartupBufferCapacity(GameLog.DefaultStartupBufferCapacity);
            GameLog.ApplyProfile(GameLogProfile.FromEnvironment(GameLogEnvironment.Trace));
            RuntimeLogManager.DisposeCurrent();
        }

        [TearDown]
        public void TearDown()
        {
            GameLog.Sink = null;
            GameLog.ClearStartupBuffer();
            RuntimeLogManager.DisposeCurrent();

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void RuntimeLogSession_WritesJsonlAndLatestFiles()
        {
            var session = CreateSession(maintainLatest: true);
            session.Write(new RuntimeLogEntry
            {
                Level = GameLogLevel.Information,
                Module = "Core.Asset",
                Category = "Core.Asset.AssetSystem",
                Phase = "Core.Asset",
                Message = "Ready"
            });
            session.Dispose();

            Assert.That(File.Exists(session.LogFilePath), Is.True);
            Assert.That(File.Exists(session.SessionFilePath), Is.True);
            Assert.That(File.Exists(session.LatestLogFilePath), Is.True);
            Assert.That(File.Exists(session.LatestSessionFilePath), Is.True);

            var line = File.ReadLines(session.LogFilePath).Single();
            Assert.That(line, Does.Contain("\"schema\":\"kj.runtime.log.v1\""));
            Assert.That(line, Does.Contain("\"sessionId\":\"test-session\""));
            Assert.That(line, Does.Contain("\"seq\":1"));
            Assert.That(line, Does.Contain("\"module\":\"Core.Asset\""));
            Assert.That(line, Does.Contain("\"message\":\"Ready\""));

            var latestLine = File.ReadLines(session.LatestLogFilePath).Single();
            Assert.That(latestLine, Is.EqualTo(line));
        }

        [Test]
        public void RuntimeLogSession_EscapesMessagesAndWritesExceptionFields()
        {
            var session = CreateSession(maintainLatest: false);
            var exception = new InvalidOperationException("bad \"thing\"");
            session.Write(new RuntimeLogEntry
            {
                Level = GameLogLevel.Error,
                Module = "Boot",
                Category = "Boot.Entry",
                Message = "line1\nline2 \"quoted\"",
                ExceptionType = exception.GetType().FullName,
                ExceptionMessage = exception.Message,
                StackTrace = exception.ToString()
            });
            session.Dispose();

            var line = File.ReadLines(session.LogFilePath).Single();
            Assert.That(line, Does.Contain("line1\\nline2 \\\"quoted\\\""));
            Assert.That(line, Does.Contain("\"exceptionType\":\"System.InvalidOperationException\""));
            Assert.That(line, Does.Contain("bad \\\"thing\\\""));
            Assert.That(line, Does.Not.Contain("\nline2"));
        }

        [Test]
        public void RuntimeLogSession_RespectsMinimumLevel()
        {
            var session = CreateSession(maintainLatest: false, minimumLevel: GameLogLevel.Warning);
            session.Write(new RuntimeLogEntry
            {
                Level = GameLogLevel.Information,
                Message = "skip"
            });
            session.Write(new RuntimeLogEntry
            {
                Level = GameLogLevel.Error,
                Message = "keep"
            });
            session.Dispose();

            var line = File.ReadLines(session.LogFilePath).Single();
            Assert.That(line, Does.Not.Contain("skip"));
            Assert.That(line, Does.Contain("keep"));
            Assert.That(line, Does.Contain("\"seq\":1"));
        }

        [Test]
        public void GameLog_BuffersBeforeSinkAndReplaysOnInstall()
        {
            GameLog.SetStartupBufferCapacity(2);
            WriteGameLog(GameLogLevel.Information, "first", "Boot");
            WriteGameLog(GameLogLevel.Warning, "second", "Boot");
            WriteGameLog(GameLogLevel.Error, "third", "Boot");

            Assert.That(GameLog.BufferedEntryCount, Is.EqualTo(2));

            var sink = new RecordingRuntimeLogSink();
            GameLog.Sink = sink;

            Assert.That(sink.Count, Is.EqualTo(2));
            Assert.That(sink.Entries[0].Message, Is.EqualTo("second"));
            Assert.That(sink.Entries[1].Message, Is.EqualTo("third"));
            Assert.That(GameLog.BufferedEntryCount, Is.EqualTo(0));
        }

        [Test]
        public void RuntimeLogManager_DoesNotReplaceExistingBridgeSinkWhenSessionAlreadyExists()
        {
            var session = CreateSession(maintainLatest: false);
            RuntimeLogManager.Install(session, installGameLogSink: true);

            var bridgeSink = new RecordingRuntimeLogSink();
            GameLog.Sink = bridgeSink;

            var current = RuntimeLogManager.InstallIfNone(
                () => throw new InvalidOperationException("Should reuse current session."),
                installGameLogSink: true);

            Assert.That(current, Is.SameAs(session));
            Assert.That(GameLog.Sink, Is.SameAs(bridgeSink));
        }

        [Test]
        public void RuntimeLogLoggerProvider_WritesMicrosoftLoggerEntries()
        {
            var session = CreateSession(maintainLatest: false);
            using (var provider = new Core.Logging.RuntimeLogLoggerProvider(session))
            {
                var logger = provider.CreateLogger("Core.Systems.SystemManager");
                logger.LogError(new InvalidOperationException("boom"), "[SystemManager] Init failed");
            }
            session.Dispose();

            var line = File.ReadLines(session.LogFilePath).Single();
            Assert.That(line, Does.Contain("\"category\":\"Core.Systems.SystemManager\""));
            Assert.That(line, Does.Contain("\"phase\":\"Core.Init\""));
            Assert.That(line, Does.Contain("[SystemManager] Init failed"));
            Assert.That(line, Does.Contain("\"exceptionType\":\"System.InvalidOperationException\""));
        }

        private RuntimeLogSession CreateSession(
            bool maintainLatest,
            GameLogLevel minimumLevel = GameLogLevel.Trace)
        {
            return new RuntimeLogSession(new RuntimeLogSessionOptions
            {
                DirectoryPath = _tempDir,
                MaintainLatest = maintainLatest,
                MinimumLevel = minimumLevel,
                FrameProvider = () => 7,
                UtcNow = () => new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero),
                SessionInfo = new RuntimeLogSessionInfo
                {
                    SessionId = "test-session",
                    StartTimeUtc = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero),
                    ProjectName = "KJ",
                    UnityVersion = "2022.3.62f2",
                    Platform = "EditMode",
                    ApplicationVersion = "1.0.0",
                    BuildGuid = "build",
                    GitCommit = "commit",
                    LogProfile = "Trace:Trace",
                    AssetPlayMode = "EditorSimulate",
                    AssetPackageName = "DefaultPackage"
                }
            });
        }

        private static void WriteGameLog(GameLogLevel level, string message, string module)
        {
            var method = typeof(GameLog).GetMethod(
                "Log",
                BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { level, module, message, null, string.Empty });
        }
    }
}
