using Core.Logging;
using Core.Systems;
using Framework.Asset;
using Framework.Log;
using Framework.RuntimeLog;
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
        public static MessagePipeOptions RegisterCoreServices(this IContainerBuilder builder, IAssetRuntime assetRuntime = null)
        {
            // ── ZLogger / Microsoft.Extensions.Logging ──
            var runtimeLogSession = RuntimeLogBootstrap.EnsureInstalled(assetRuntime);
            var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddFilter((category, level) =>
                    GameLog.Profile.IsEnabled(category ?? GameLog.DefaultModule, ToGameLogLevel(level)));
                logging.AddProvider(new RuntimeLogLoggerProvider(runtimeLogSession));
                logging.AddZLoggerUnityDebug(options =>
                {
                    options.PrettyStacktrace = true;
                });
            });
            builder.RegisterInstance(loggerFactory).As<ILoggerFactory>();
            GameLogBridge.Install(runtimeLogSession, loggerFactory.CreateLogger<GameLogBridge>());
            builder.RegisterDisposeCallback(_ =>
            {
                GameLogBridge.Uninstall();
                loggerFactory.Dispose();
                RuntimeLogManager.DisposeCurrent(runtimeLogSession);
            });

            // 用 SimpleLogger<T> 替代 Logger<T> —— 后者的 Log<TState> 在 AOT 侧 DLL，
            // HybridCLR 无法穿透其泛型实例化。SimpleLogger<T> 在热更 Core 程序集中，直接可用。
            builder.Register(typeof(SimpleLogger<>), Lifetime.Singleton).As(typeof(ILogger<>));

            // ── MessagePipe ──
            var options = builder.RegisterMessagePipe();

            // ── Framework Asset ──
            if (assetRuntime == null)
            {
                builder.Register<AssetRuntime>(Lifetime.Singleton)
                    .AsSelf()
                    .AsImplementedInterfaces();
            }
            else
            {
                if (assetRuntime is not IAssetSystem assetSystem)
                    throw new System.InvalidOperationException("Boot asset runtime must also implement IAssetSystem.");

                builder.RegisterInstance(assetRuntime)
                    .As<IAssetRuntime>();
                builder.RegisterInstance(assetSystem)
                    .As<IAssetSystem>();
            }

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
