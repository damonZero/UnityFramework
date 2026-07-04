using System.Diagnostics;
using Framework.Log;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Core.Asset
{
    internal static partial class AssetSystemLog
    {
        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning)]
        [ZLoggerMessage(LogLevel.Warning, "[AssetSystem] AssetConfig.asset not found; using editor-simulate defaults.")]
        internal static partial void ConfigNotFound(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[AssetSystem] Ready")]
        internal static partial void Ready(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[AssetSystem] Initialize failed; ready event will not be published.")]
        internal static partial void InitializeFailed(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[AssetSystem] Shutdown")]
        internal static partial void Shutdown(ILogger logger);
    }
}
