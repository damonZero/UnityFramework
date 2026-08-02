using System;
using System.Diagnostics;
using Framework.Log;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace General.Bootstrap
{
    internal static partial class GeneralLayerEntrypointLog
    {
        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[GeneralLayerEntrypoint] General layer ready (models loaded)")]
        internal static partial void GeneralReady(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[GeneralLayerEntrypoint] General startup failed (models): {failedModels}")]
        internal static partial void GeneralStartupFailed(ILogger logger, string failedModels);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[GeneralLayerEntrypoint] General root scope is not ready (GeneralStartup not invoked)")]
        internal static partial void GeneralScopeNotReady(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[GeneralLayerEntrypoint] Project startup type not found: {startupTypeName}")]
        internal static partial void ProjectStartupTypeNotFound(ILogger logger, string startupTypeName);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[GeneralLayerEntrypoint] Project startup method not found: {startupTypeName}")]
        internal static partial void ProjectStartupMethodNotFound(ILogger logger, string startupTypeName);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[GeneralLayerEntrypoint] Project startup signature unsupported: {startupTypeName}")]
        internal static partial void ProjectStartupSignatureUnsupported(ILogger logger, string startupTypeName);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[GeneralLayerEntrypoint] Project startup failed: {startupTypeName}")]
        internal static partial void ProjectStartupFailed(ILogger logger, string startupTypeName, Exception e);
    }
}
