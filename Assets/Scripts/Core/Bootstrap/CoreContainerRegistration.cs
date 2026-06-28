using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace KJ.Core
{
    /// <summary>
    /// Core 层容器注册入口。
    /// 负责注册 MessagePipe 基础设施、EventSystem 以及所有 Core 层系统。
    /// </summary>
    public static class CoreContainerRegistration
    {
        public static void RegisterCoreServices(this IContainerBuilder builder)
        {
            // 1. 注册 MessagePipe 基础设施（诊断信息、过滤器工厂等）
            var options = builder.RegisterMessagePipe();

            // 2. 注册 EventEnvelope 的 MessageBroker（提供 IPublisher + ISubscriber）
            builder.RegisterMessageBroker<EventSystem.EventEnvelope>(options);

            // 3. 注册 Core 层系统
            builder.RegisterEntryPoint<SystemManager>();
            builder.Register<StartupProbeSystem>(Lifetime.Singleton).As<ISystem>();
            builder.Register<EventSystem>(Lifetime.Singleton).As<IEventSystem, ISystem>();
        }
    }
}
