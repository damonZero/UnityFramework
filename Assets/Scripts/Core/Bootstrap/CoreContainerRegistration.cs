using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Core.Architecture
{
    /// <summary>
    /// Core 层容器注册入口。
    /// 负责注册 MessagePipe 基础设施、EventSystem 以及所有 Core 层系统。
    /// </summary>
    public static class CoreContainerRegistration
    {
        public static MessagePipeOptions RegisterCoreServices(this IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();

            builder.RegisterArchitecture(options, typeof(CoreContainerRegistration).Assembly);
            builder.RegisterEntryPoint<SystemManager>();
            return options;
        }
    }
}
