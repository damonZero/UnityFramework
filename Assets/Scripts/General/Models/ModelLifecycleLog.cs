using System;
using System.Diagnostics;
using Framework.Log;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace General
{
    internal static partial class ModelLifecycleLog
    {
        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[ModelLifecycle] Load {name} (Priority={priority})")]
        internal static partial void ModelLoaded(ILogger logger, string name, int priority);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning)]
        [ZLoggerMessage(LogLevel.Warning, "[ModelLifecycle] Skip LoadAll because Core startup failed: {failedSystems}")]
        internal static partial void CoreStartupFailed(ILogger logger, string failedSystems);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[ModelLifecycle] Load 失败: {name}")]
        internal static partial void ModelLoadFailed(ILogger logger, string name, Exception e);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[ModelLifecycle] Unload {name}")]
        internal static partial void ModelUnloaded(ILogger logger, string name);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[ModelLifecycle] Unload 失败: {name}")]
        internal static partial void ModelUnloadFailed(ILogger logger, string name, Exception e);
    }
}
