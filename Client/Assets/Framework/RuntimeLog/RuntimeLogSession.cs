using System;
using System.IO;
using System.Text;
using Framework.Log;

namespace Framework.RuntimeLog
{
    public sealed class RuntimeLogSession : IGameLogSink, IDisposable
    {
        private readonly object _gate = new();
        private readonly RuntimeLogSessionOptions _options;
        private readonly RuntimeLogSessionInfo _sessionInfo;
        private readonly StreamWriter _logWriter;
        private readonly StreamWriter _latestLogWriter;
        private long _nextSeq;
        private bool _disposed;

        public RuntimeLogSession(RuntimeLogSessionOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.DirectoryPath))
                throw new ArgumentException("Runtime log directory path is required.", nameof(options));

            Directory.CreateDirectory(_options.DirectoryPath);

            _sessionInfo = _options.SessionInfo ?? new RuntimeLogSessionInfo();
            _sessionInfo.SessionId = string.IsNullOrWhiteSpace(_sessionInfo.SessionId)
                ? RuntimeLogSessionId.Create(_options.UtcNow())
                : _sessionInfo.SessionId;
            _sessionInfo.StartTimeUtc = _sessionInfo.StartTimeUtc == default
                ? _options.UtcNow()
                : _sessionInfo.StartTimeUtc.ToUniversalTime();

            SessionId = _sessionInfo.SessionId;
            var prefix = string.IsNullOrWhiteSpace(_options.FilePrefix)
                ? RuntimeLogFileName.Create(_sessionInfo.StartTimeUtc, _sessionInfo.Platform, SessionId)
                : RuntimeLogFileName.Sanitize(_options.FilePrefix);

            LogFilePath = Path.Combine(_options.DirectoryPath, prefix + ".jsonl");
            SessionFilePath = Path.Combine(_options.DirectoryPath, prefix + ".session.json");
            LatestLogFilePath = Path.Combine(_options.DirectoryPath, RuntimeLogConstants.LatestLogFileName);
            LatestSessionFilePath = Path.Combine(_options.DirectoryPath, RuntimeLogConstants.LatestSessionFileName);

            _logWriter = CreateWriter(LogFilePath);
            if (_options.MaintainLatest)
                _latestLogWriter = CreateWriter(LatestLogFilePath);

            WriteSessionManifestLocked();
        }

        public string SessionId { get; }
        public string LogFilePath { get; }
        public string SessionFilePath { get; }
        public string LatestLogFilePath { get; }
        public string LatestSessionFilePath { get; }
        public RuntimeLogSessionInfo SessionInfo => _sessionInfo;

        public void Write(in GameLogEntry entry)
        {
            Write(RuntimeLogEntry.FromGameLog(entry));
        }

        public void Write(RuntimeLogEntry entry)
        {
            if (entry == null)
                return;

            lock (_gate)
            {
                if (_disposed || entry.Level < _options.MinimumLevel || entry.Level >= GameLogLevel.None)
                    return;

                entry.Seq = ++_nextSeq;
                entry.Frame ??= _options.FrameProvider?.Invoke();
                if (entry.TimeUtc == default)
                    entry.TimeUtc = _options.UtcNow();

                var line = RuntimeLogJson.SerializeEntry(entry, SessionId);
                _logWriter.WriteLine(line);
                _latestLogWriter?.WriteLine(line);
            }
        }

        public void UpdateSessionInfo(Action<RuntimeLogSessionInfo> update)
        {
            if (update == null)
                return;

            lock (_gate)
            {
                if (_disposed)
                    return;

                update(_sessionInfo);
                WriteSessionManifestLocked();
            }
        }

        public void Flush()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _logWriter.Flush();
                _latestLogWriter?.Flush();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _sessionInfo.EndTimeUtc = _options.UtcNow();
                WriteSessionManifestLocked();
                _logWriter.Flush();
                _latestLogWriter?.Flush();
                _logWriter.Dispose();
                _latestLogWriter?.Dispose();
            }
        }

        private void WriteSessionManifestLocked()
        {
            var json = RuntimeLogJson.SerializeSession(_sessionInfo);
            File.WriteAllText(SessionFilePath, json, Encoding.UTF8);
            if (_options.MaintainLatest)
                File.WriteAllText(LatestSessionFilePath, json, Encoding.UTF8);
        }

        private static StreamWriter CreateWriter(string path)
        {
            var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            return new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        }
    }
}
