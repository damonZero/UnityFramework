using System;
using System.Diagnostics;
using Framework.Log;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Core.Bootstrap
{
    internal static partial class CoreLayerEntrypointLog
    {
        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning)]
        [ZLoggerMessage(LogLevel.Warning, "[CoreLayerEntrypoint] Core startup failed, General not started: {failedSystems}")]
        internal static partial void CoreStartupFailed(ILogger logger, string failedSystems);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[CoreLayerEntrypoint] Core root scope is not ready (CoreStartup not invoked)")]
        internal static partial void CoreScopeNotReady(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[CoreLayerEntrypoint] General startup type not found: {startupTypeName}")]
        internal static partial void GeneralStartupTypeNotFound(ILogger logger, string startupTypeName);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[CoreLayerEntrypoint] General startup method not found: {startupTypeName}")]
        internal static partial void GeneralStartupMethodNotFound(ILogger logger, string startupTypeName);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[CoreLayerEntrypoint] General startup signature unsupported: {startupTypeName}")]
        internal static partial void GeneralStartupSignatureUnsupported(ILogger logger, string startupTypeName);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[CoreLayerEntrypoint] General startup failed: {startupTypeName}")]
        internal static partial void GeneralStartupFailed(ILogger logger, string startupTypeName, Exception e);
    }
}
