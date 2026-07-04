using System;

namespace Framework.Log
{
    public readonly struct GameLogEntry
    {
        public GameLogEntry(
            GameLogLevel level,
            string module,
            string message,
            Exception exception = null)
        {
            Level = level;
            Module = module ?? string.Empty;
            Message = message ?? string.Empty;
            Exception = exception;
        }

        public GameLogLevel Level { get; }
        public string Module { get; }
        public string Message { get; }
        public Exception Exception { get; }
    }
}
