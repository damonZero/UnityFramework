using System;
using Core.Systems;
using Core.Systems.Attributes;
using Framework.Log;
using Microsoft.Extensions.Logging;

namespace Core.Logging
{
    /// <summary>
    /// Bridges Framework-layer <see cref="GameLog"/> static delegates to the
    /// DI-managed ZLogger pipeline. Must run before any other system that logs.
    /// </summary>
    [CoreSystem]
    public sealed class GameLogBridge : ISystem
    {
        private readonly ILogger<GameLogBridge> _logger;
        public int Priority => int.MinValue;

        public GameLogBridge(ILogger<GameLogBridge> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Init()
        {
            GameLog.Write = (level, message) =>
            {
                switch (level)
                {
                    case GameLogLevel.Trace:
                        _logger.LogTrace(message);
                        break;
                    case GameLogLevel.Debug:
                        _logger.LogDebug(message);
                        break;
                    case GameLogLevel.Information:
                        _logger.LogInformation(message);
                        break;
                    case GameLogLevel.Warning:
                        _logger.LogWarning(message);
                        break;
                    case GameLogLevel.Error:
                        _logger.LogError(message);
                        break;
                    case GameLogLevel.Critical:
                        _logger.LogCritical(message);
                        break;
                }
            };
        }

        public void Shutdown()
        {
            GameLog.Write = null;
        }
    }
}
