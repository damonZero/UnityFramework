using Core.Systems;
using Framework.Asset;
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
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddZLoggerUnityDebug();
            });
            builder.RegisterInstance(loggerFactory).As<ILoggerFactory>();
            builder.RegisterDisposeCallback(_ => loggerFactory.Dispose());
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
    }
}
