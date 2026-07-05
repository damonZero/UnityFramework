using System;

namespace Framework.Log
{
    public readonly struct GameLogEntry
    {
        public GameLogEntry(
            GameLogLevel level,
            string module,
            string message,
            Exception exception = null,
            DateTimeOffset? timestampUtc = null,
            int? threadId = null)
        {
            Level = level;
            Module = module ?? string.Empty;
            Message = message ?? string.Empty;
            Exception = exception;
            TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow;
            ThreadId = threadId ?? Environment.CurrentManagedThreadId;
        }

        public GameLogLevel Level { get; }
        public string Module { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public DateTimeOffset TimestampUtc { get; }
        public int ThreadId { get; }
    }
}
