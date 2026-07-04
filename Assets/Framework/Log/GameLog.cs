using System;

namespace Framework.Log
{
    /// <summary>
    /// Log levels matching Microsoft.Extensions.Logging.LogLevel for easy bridging.
    /// </summary>
    public enum GameLogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    /// <summary>
    /// Static delegate-based logging bridge for Framework layer assemblies.
    /// Framework cannot depend on DI, so logging goes through a static delegate
    /// that Core wires up via <c>GameLogBridge</c> during startup.
    ///
    /// Pattern mirrors <see cref="Framework.Pool.PoolDependencies"/>.
    /// </summary>
    public static class GameLog
    {
        /// <summary>
        /// Wired by Core during startup. When null, logs fall back to Unity Debug
        /// so early boot/runtime failures are still visible.
        /// </summary>
        public static Action<GameLogLevel, string> Write { get; set; }

        public static void Trace(string message) => Log(GameLogLevel.Trace, message);
        public static void Debug(string message) => Log(GameLogLevel.Debug, message);
        public static void Info(string message) => Log(GameLogLevel.Information, message);
        public static void Warn(string message) => Log(GameLogLevel.Warning, message);
        public static void Error(string message) => Log(GameLogLevel.Error, message);
        public static void Critical(string message) => Log(GameLogLevel.Critical, message);

        private static void Log(GameLogLevel level, string message)
        {
            var writer = Write;
            if (writer != null)
            {
                writer(level, message);
                return;
            }

            switch (level)
            {
                case GameLogLevel.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case GameLogLevel.Error:
                case GameLogLevel.Critical:
                    UnityEngine.Debug.LogError(message);
                    break;
                default:
                    UnityEngine.Debug.Log(message);
                    break;
            }
        }
    }
}
