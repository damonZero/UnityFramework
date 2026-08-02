using System;
using General;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Project.Bootstrap
{
    /// <summary>
    /// Project 层 LifetimeScope（分层启动链 Phase 3）。General scope 的 child。
    /// 只注册 Project 程序集的事件 broker、模型与 ModelLifecycle。
    /// 消息域由 Core scope 统一建立，本层不调用 RegisterMessagePipe（分层启动计划 §0.1）。
    /// </summary>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        /// <summary>
        /// 由 <see cref="ProjectStartup.Start"/> 从父容器解析后注入，供 <see cref="Configure"/> 消费。
        /// 用后即清，避免跨 scope 污染。
        /// </summary>
        internal static MessagePipeOptions PendingMessagePipeOptions { get; set; }

        protected override void Configure(IContainerBuilder builder)
        {
            var options = PendingMessagePipeOptions
                          ?? throw new InvalidOperationException(
                              "MessagePipeOptions is missing. ProjectStartup must resolve it from the parent scope before creating the Project scope.");

            // 本层事件 broker（不调用 RegisterMessagePipe）
            builder.RegisterBusinessEvents(options, typeof(ProjectLifetimeScope).Assembly);
            // 本层模型（只扫 Project 程序集）
            builder.RegisterModels(typeof(ProjectLifetimeScope).Assembly);
            // 本层 ModelLifecycle（IPostStartable）
            builder.RegisterModelLifecycle();
            // 本层启动入口（IPostStartable，在 ModelLifecycle 之后触发）
            builder.RegisterEntryPoint<ProjectLayerEntrypoint>();

            PendingMessagePipeOptions = null;
        }
    }
}
