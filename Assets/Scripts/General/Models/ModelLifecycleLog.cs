using System;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace General
{
    internal static partial class ModelLifecycleLog
    {
        [ZLoggerMessage(LogLevel.Information, "[ModelLifecycle] Load {name} (Priority={priority})")]
        internal static partial void ModelLoaded(ILogger logger, string name, int priority);

        [ZLoggerMessage(LogLevel.Warning, "[ModelLifecycle] Skip LoadAll because Core startup failed: {failedSystems}")]
        internal static partial void CoreStartupFailed(ILogger logger, string failedSystems);

        [ZLoggerMessage(LogLevel.Error, "[ModelLifecycle] Load 失败: {name}")]
        internal static partial void ModelLoadFailed(ILogger logger, string name, Exception e);

        [ZLoggerMessage(LogLevel.Information, "[ModelLifecycle] Unload {name}")]
        internal static partial void ModelUnloaded(ILogger logger, string name);

        [ZLoggerMessage(LogLevel.Error, "[ModelLifecycle] Unload 失败: {name}")]
        internal static partial void ModelUnloadFailed(ILogger logger, string name, Exception e);
    }
}
