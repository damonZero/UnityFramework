using Microsoft.Extensions.Logging;
using ZLogger;

namespace Core.Systems
{
    internal static partial class StartupProbeSystemLog
    {
        [ZLoggerMessage(LogLevel.Information, "[StartupProbeSystem] Init")]
        internal static partial void ProbeInit(ILogger logger);

        [ZLoggerMessage(LogLevel.Information, "[StartupProbeSystem] Shutdown")]
        internal static partial void ProbeShutdown(ILogger logger);
    }
}
