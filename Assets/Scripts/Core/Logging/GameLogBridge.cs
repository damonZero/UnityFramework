using System;
using Framework.Log;
using Microsoft.Extensions.Logging;

namespace Core.Logging
{
    /// <summary>
    /// Bridges Framework-layer <see cref="GameLog"/> static delegates to the
    /// DI-managed ZLogger pipeline.
    /// </summary>
    public sealed class GameLogBridge : IGameLogSink
    {
        private readonly ILogger<GameLogBridge> _logger;

        public GameLogBridge(ILogger<GameLogBridge> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static void Install(ILogger<GameLogBridge> logger)
        {
            GameLog.Sink = new GameLogBridge(logger);
        }

        public static void Uninstall()
        {
            if (GameLog.Sink is GameLogBridge)
                GameLog.Sink = null;
        }

        public void Write(in GameLogEntry entry)
        {
            var logLevel = ToMicrosoftLogLevel(entry.Level);
            if (entry.Exception != null)
            {
                _logger.Log(logLevel, entry.Exception, "[{Module}] {Message}", entry.Module, entry.Message);
                return;
            }

            _logger.Log(logLevel, "[{Module}] {Message}", entry.Module, entry.Message);
        }

        private static LogLevel ToMicrosoftLogLevel(GameLogLevel level)
        {
            return level switch
            {
                GameLogLevel.Trace => LogLevel.Trace,
                GameLogLevel.Debug => LogLevel.Debug,
                GameLogLevel.Information => LogLevel.Information,
                GameLogLevel.Warning => LogLevel.Warning,
                GameLogLevel.Error => LogLevel.Error,
                GameLogLevel.Critical => LogLevel.Critical,
                _ => LogLevel.None
            };
        }
    }
}
