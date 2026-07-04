using Microsoft.Extensions.Logging;
using ZLogger;

namespace Core.Asset
{
    internal static partial class AssetSystemLog
    {
        [ZLoggerMessage(LogLevel.Warning, "[AssetSystem] AssetConfig.asset not found; using editor-simulate defaults.")]
        internal static partial void ConfigNotFound(ILogger logger);

        [ZLoggerMessage(LogLevel.Information, "[AssetSystem] Ready")]
        internal static partial void Ready(ILogger logger);

        [ZLoggerMessage(LogLevel.Error, "[AssetSystem] Initialize failed; ready event will not be published.")]
        internal static partial void InitializeFailed(ILogger logger);

        [ZLoggerMessage(LogLevel.Information, "[AssetSystem] Shutdown")]
        internal static partial void Shutdown(ILogger logger);
    }
}
