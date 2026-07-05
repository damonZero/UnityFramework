using System;
using System.Collections.Generic;
using Framework.Log;

namespace Framework.RuntimeLog
{
    public sealed class RuntimeLogEntry
    {
        public DateTimeOffset TimeUtc { get; set; } = DateTimeOffset.UtcNow;
        public long Seq { get; internal set; }
        public int? Frame { get; set; }
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
        public GameLogLevel Level { get; set; } = GameLogLevel.Information;
        public string Module { get; set; } = GameLog.DefaultModule;
        public string Category { get; set; } = GameLog.DefaultModule;
        public string Phase { get; set; } = RuntimeLogPhaseResolver.DefaultPhase;
        public string Message { get; set; } = string.Empty;
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public IReadOnlyDictionary<string, string> Context { get; set; }

        public static RuntimeLogEntry FromGameLog(in GameLogEntry entry)
        {
            return new RuntimeLogEntry
            {
                TimeUtc = entry.TimestampUtc,
                ThreadId = entry.ThreadId,
                Level = entry.Level,
                Module = string.IsNullOrWhiteSpace(entry.Module) ? GameLog.DefaultModule : entry.Module,
                Category = string.IsNullOrWhiteSpace(entry.Module) ? GameLog.DefaultModule : entry.Module,
                Phase = RuntimeLogPhaseResolver.Resolve(entry.Module, entry.Module, entry.Message),
                Message = entry.Message ?? string.Empty,
                ExceptionType = entry.Exception?.GetType().FullName,
                ExceptionMessage = entry.Exception?.Message,
                StackTrace = entry.Exception?.ToString()
            };
        }
    }
}
