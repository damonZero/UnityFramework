using System;
using System.Diagnostics;
using Framework.Log;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Project.Bootstrap
{
    internal static partial class ProjectLayerEntrypointLog
    {
        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[ProjectLayerEntrypoint] Project layer ready (models loaded)")]
        internal static partial void ProjectReady(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[ProjectLayerEntrypoint] Project startup failed (models): {failedModels}")]
        internal static partial void ProjectStartupFailed(ILogger logger, string failedModels);
    }
}
