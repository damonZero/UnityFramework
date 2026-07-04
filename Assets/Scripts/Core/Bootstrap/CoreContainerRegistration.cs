using Core.Logging;
using Core.Systems;
using Framework.Asset;
using Framework.Log;
using MessagePipe;
using Microsoft.Extensions.Logging;
using VContainer;
using VContainer.Unity;
using ZLogger.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 层容器注册入口。
    /// 负责注册 MessagePipe 基础设施、ZLogger、以及所有 Core 层系统。
    /// </summary>
    public static class CoreContainerRegistration
    {
        public static MessagePipeOptions RegisterCoreServices(this IContainerBuilder builder)
        {
            // ── ZLogger / Microsoft.Extensions.Logging ──
            var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddFilter((category, level) =>
                    GameLog.Profile.IsEnabled(category ?? GameLog.DefaultModule, ToGameLogLevel(level)));
                logging.AddZLoggerUnityDebug(options =>
                {
                    options.PrettyStacktrace = true;
                });
            });
            builder.RegisterInstance(loggerFactory).As<ILoggerFactory>();
            GameLogBridge.Install(loggerFactory.CreateLogger<GameLogBridge>());
            builder.RegisterDisposeCallback(_ =>
            {
                GameLogBridge.Uninstall();
                loggerFactory.Dispose();
            });
            builder.Register(typeof(Logger<>), Lifetime.Singleton).As(typeof(ILogger<>));

            // ── MessagePipe ──
            var options = builder.RegisterMessagePipe();

            // ── Framework Asset ──
            builder.Register<AssetRuntime>(Lifetime.Singleton)
                .AsSelf()
                .AsImplementedInterfaces();

            // ── Core Types (scans [CoreSystem] types including GameLogBridge) ──
            builder.RegisterCoreTypes(options, typeof(CoreContainerRegistration).Assembly);
            builder.RegisterEntryPoint<SystemManager>();
            return options;
        }

        private static GameLogLevel ToGameLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => GameLogLevel.Trace,
                LogLevel.Debug => GameLogLevel.Debug,
                LogLevel.Information => GameLogLevel.Information,
                LogLevel.Warning => GameLogLevel.Warning,
                LogLevel.Error => GameLogLevel.Error,
                LogLevel.Critical => GameLogLevel.Critical,
                _ => GameLogLevel.None
            };
        }
    }
}
