using System;
using System.Diagnostics;
using Framework.Log;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Core.Systems
{
    internal static partial class SystemManagerLog
    {
        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning)]
        [ZLoggerMessage(LogLevel.Warning, "[SystemManager] 已初始化，禁止再次注册: {name}")]
        internal static partial void AlreadyInitialized(ILogger logger, string name);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning)]
        [ZLoggerMessage(LogLevel.Warning, "[SystemManager] 系统已注册，跳过: {name}")]
        internal static partial void SystemAlreadyRegistered(ILogger logger, string name);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning)]
        [ZLoggerMessage(LogLevel.Warning, "[SystemManager] 已初始化，跳过")]
        internal static partial void AlreadyInitializedSkip(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[SystemManager] 开始初始化 {count} 个系统")]
        internal static partial void InitStart(ILogger logger, int count);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[SystemManager] Init [{index}/{total}] {name} (Priority={priority})")]
        internal static partial void InitProgress(ILogger logger, int index, int total, string name, int priority);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[SystemManager] Init 失败: {name}")]
        internal static partial void InitFailed(ILogger logger, string name, Exception e);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[SystemManager] 全部初始化完成")]
        internal static partial void InitComplete(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[SystemManager] 初始化完成但存在失败系统: {failedSystems}")]
        internal static partial void InitCompleteWithFailures(ILogger logger, string failedSystems);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[SystemManager] 开始关闭系统")]
        internal static partial void ShutdownStart(ILogger logger);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[SystemManager] Shutdown {name}")]
        internal static partial void ShutdownProgress(ILogger logger, string name);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information), Conditional(GameLogSymbols.Warning), Conditional(GameLogSymbols.Error)]
        [ZLoggerMessage(LogLevel.Error, "[SystemManager] Shutdown 失败: {name}")]
        internal static partial void ShutdownFailed(ILogger logger, string name, Exception e);

        [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
        [ZLoggerMessage(LogLevel.Information, "[SystemManager] 全部关闭完成")]
        internal static partial void ShutdownComplete(ILogger logger);
    }
}
