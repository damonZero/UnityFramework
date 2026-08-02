using System;
using Framework.Log;
using Framework.RuntimeLog;
using Microsoft.Extensions.Logging;

namespace Core.Logging
{
    [ProviderAlias("KJRuntimeLog")]
    public sealed class RuntimeLogLoggerProvider : ILoggerProvider
    {
        private readonly RuntimeLogSession _session;

        public RuntimeLogLoggerProvider(RuntimeLogSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RuntimeLogLogger(_session, categoryName);
        }

        public void Dispose()
        {
            _session.Flush();
        }
    }

    internal sealed class RuntimeLogLogger : ILogger
    {
        private readonly RuntimeLogSession _session;
        private readonly string _categoryName;

        public RuntimeLogLogger(RuntimeLogSession session, string categoryName)
        {
            _session = session;
            _categoryName = string.IsNullOrWhiteSpace(categoryName)
                ? GameLog.DefaultModule
                : categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (string.Equals(_categoryName, "Core.Logging.GameLogBridge", StringComparison.Ordinal))
                return false;

            var level = ToGameLogLevel(logLevel);
            return level < GameLogLevel.None && GameLog.Profile.IsEnabled(_categoryName, level);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter != null ? formatter(state, exception) : state?.ToString();
            _session.Write(new RuntimeLogEntry
            {
                Level = ToGameLogLevel(logLevel),
                Module = _categoryName,
                Category = _categoryName,
                Phase = RuntimeLogPhaseResolver.Resolve(_categoryName, _categoryName, message),
                Message = message ?? string.Empty,
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception?.Message,
                StackTrace = exception?.ToString()
            });
        }

        private static GameLogLevel ToGameLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => GameLogLevel.Trace,
                LogLevel.Debug => GameLogLevel.Debug,
                LogLevel.Information => GameLogLevel.Information,
                LogLevel.Warning => GameLogLevel.Warning,
                LogLevel.Error => GameLogLevel.Error,
                LogLevel.Critical => GameLogLevel.Critical,
                _ => GameLogLevel.None
            };
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
