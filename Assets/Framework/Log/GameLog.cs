using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        Critical = 5,
        None = 6
    }

    /// <summary>
    /// Stable logging facade for Framework, Boot, and places where DI logging is
    /// not available. Core wires <see cref="Sink"/> to ZLogger during startup.
    /// </summary>
    /// <remarks>
    /// Multiple <see cref="ConditionalAttribute"/> entries on one method are OR
    /// conditions: the call site is kept when any listed symbol is defined.
    /// <see cref="GameLogProfile.IsEnabled"/> then applies the runtime level and
    /// module filter as a second gate.
    /// </remarks>
    public static class GameLog
    {
        public const string DefaultModule = "Default";
        public const string SymbolTrace = GameLogSymbols.Trace;
        public const string SymbolDebug = GameLogSymbols.Debug;
        public const string SymbolInformation = GameLogSymbols.Information;
        public const string SymbolWarning = GameLogSymbols.Warning;
        public const string SymbolError = GameLogSymbols.Error;
        public const string SymbolCritical = GameLogSymbols.Critical;
        public const string CSharpRootPath = "Assets";
        public const string CSharpFilePattern = @"\w+.cs$";

        [GameLogTree(CSharpRootPath, CSharpFilePattern)]
        public const string CSharpModuleTree = "KJ C# Logs";

        private static GameLogProfile _profile = CreateDefaultProfile();

        /// <summary>
        /// Wired by Core during startup. Keep the implementation outside Framework
        /// so Framework stays independent from DI and ZLogger.
        /// </summary>
        public static IGameLogSink Sink { get; set; }

        public static GameLogProfile Profile => _profile;

        public static void ApplyProfile(GameLogProfile profile)
        {
            _profile = profile ?? GameLogProfile.Silent();
        }

        public static void SetMinimumLevel(GameLogLevel level) => _profile.SetMinimumLevel(level);

        public static void ApplyEnvironment(GameLogEnvironment environment) =>
            _profile.ApplyEnvironment(environment);

        public static void SetModuleMinimumLevel(string module, GameLogLevel level) =>
            _profile.SetModuleMinimumLevel(module, level);

        public static void SetModuleEnabled(string module, bool enabled) =>
            _profile.SetModuleEnabled(module, enabled);

        public static void ApplyModuleRules(System.Collections.Generic.IEnumerable<GameLogModuleRule> rules) =>
            _profile.ApplyModuleRules(rules);

        public static bool IsEnabled(GameLogLevel level, string module = DefaultModule) =>
            _profile.IsEnabled(module, level);

        [Conditional(SymbolTrace)]
        public static void Trace(
            string message,
            string module = DefaultModule,
            [CallerFilePath] string filePath = "") =>
            Log(GameLogLevel.Trace, module, message, filePath: filePath);

        [Conditional(GameLogSymbols.UnityEditor)]
        [Conditional(GameLogSymbols.DevelopmentBuild)]
        [Conditional(SymbolTrace)]
        [Conditional(SymbolDebug)]
        public static void Debug(
            string message,
            string module = DefaultModule,
            [CallerFilePath] string filePath = "") =>
            Log(GameLogLevel.Debug, module, message, filePath: filePath);

        [Conditional(GameLogSymbols.UnityEditor)]
        [Conditional(GameLogSymbols.DevelopmentBuild)]
        [Conditional(SymbolTrace)]
        [Conditional(SymbolDebug)]
        [Conditional(SymbolInformation)]
        public static void Info(
            string message,
            string module = DefaultModule,
            [CallerFilePath] string filePath = "") =>
            Log(GameLogLevel.Information, module, message, filePath: filePath);

        [Conditional(GameLogSymbols.UnityEditor)]
        [Conditional(GameLogSymbols.DevelopmentBuild)]
        [Conditional(SymbolTrace)]
        [Conditional(SymbolDebug)]
        [Conditional(SymbolInformation)]
        [Conditional(SymbolWarning)]
        public static void Warn(
            string message,
            string module = DefaultModule,
            [CallerFilePath] string filePath = "") =>
            Log(GameLogLevel.Warning, module, message, filePath: filePath);

        [Conditional(GameLogSymbols.UnityEditor)]
        [Conditional(GameLogSymbols.DevelopmentBuild)]
        [Conditional(SymbolTrace)]
        [Conditional(SymbolDebug)]
        [Conditional(SymbolInformation)]
        [Conditional(SymbolWarning)]
        [Conditional(SymbolError)]
        public static void Error(
            string message,
            string module = DefaultModule,
            [CallerFilePath] string filePath = "") =>
            Log(GameLogLevel.Error, module, message, filePath: filePath);

        [Conditional(GameLogSymbols.UnityEditor)]
        [Conditional(GameLogSymbols.DevelopmentBuild)]
        [Conditional(SymbolTrace)]
        [Conditional(SymbolDebug)]
        [Conditional(SymbolInformation)]
        [Conditional(SymbolWarning)]
        [Conditional(SymbolError)]
        [Conditional(SymbolCritical)]
        public static void Critical(
            string message,
            string module = DefaultModule,
            [CallerFilePath] string filePath = "") =>
            Log(GameLogLevel.Critical, module, message, filePath: filePath);

        [Conditional(GameLogSymbols.UnityEditor)]
        [Conditional(GameLogSymbols.DevelopmentBuild)]
        [Conditional(SymbolTrace)]
        [Conditional(SymbolDebug)]
        [Conditional(SymbolInformation)]
        [Conditional(SymbolWarning)]
        [Conditional(SymbolError)]
        public static void Exception(
            Exception exception,
            string message,
            string module = DefaultModule,
            [CallerFilePath] string filePath = "") =>
            Log(GameLogLevel.Error, module, message, exception, filePath);

        private static void Log(
            GameLogLevel level,
            string module,
            string message,
            Exception exception = null,
            string filePath = "")
        {
            module = NormalizeModule(module, filePath);
            if (!_profile.IsEnabled(module, level))
                return;

            Sink?.Write(new GameLogEntry(level, module, message, exception));
        }

        private static string NormalizeModule(string module, string filePath)
        {
            if (!string.IsNullOrWhiteSpace(module) && module != DefaultModule)
                return module;

            if (string.IsNullOrEmpty(filePath))
                return DefaultModule;

            var normalized = filePath.Replace('\\', '/');
            var index = normalized.IndexOf("/Assets/", StringComparison.Ordinal);
            return index >= 0 ? normalized[(index + 8)..] : DefaultModule;
        }

        private static GameLogProfile CreateDefaultProfile()
        {
#if KJ_LOG_TRACE
            return GameLogProfile.FromEnvironment(GameLogEnvironment.Trace);
#elif UNITY_EDITOR || DEVELOPMENT_BUILD || KJ_LOG_DEBUG
            return GameLogProfile.Development();
#elif KJ_LOG_INFORMATION
            return GameLogProfile.FromEnvironment(GameLogEnvironment.Qa);
#elif KJ_LOG_WARNING
            return GameLogProfile.FromEnvironment(GameLogEnvironment.FormalMonitoring);
#elif KJ_LOG_ERROR
            return GameLogProfile.Formal();
#elif KJ_LOG_CRITICAL
            var profile = GameLogProfile.FromEnvironment(GameLogEnvironment.Formal);
            profile.SetMinimumLevel(GameLogLevel.Critical);
            return profile;
#else
            return GameLogProfile.Silent();
#endif
        }
    }
}
