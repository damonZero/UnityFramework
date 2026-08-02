using System;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace General.Bootstrap
{
    /// <summary>
    /// General 层 LifetimeScope（分层启动链 Phase 2）。Core scope 的 child。
    /// 只注册 General 程序集的事件 broker、模型与 ModelLifecycle。
    /// 消息域由 Core scope 统一建立，本层不调用 RegisterMessagePipe（分层启动计划 §0.1）。
    /// </summary>
    public sealed class GeneralLifetimeScope : LifetimeScope
    {
        /// <summary>
        /// 由 <see cref="GeneralStartup.Start"/> 从父容器解析后注入，供 <see cref="Configure"/> 消费。
        /// 用后即清，避免跨 scope 污染。
        /// </summary>
        internal static MessagePipeOptions PendingMessagePipeOptions { get; set; }

        protected override void Configure(IContainerBuilder builder)
        {
            var options = PendingMessagePipeOptions
                          ?? throw new InvalidOperationException(
                              "MessagePipeOptions is missing. GeneralStartup must resolve it from the Core container before creating the General scope.");

            // 本层事件 broker（不调用 RegisterMessagePipe）
            builder.RegisterBusinessEvents(options, typeof(GeneralLifetimeScope).Assembly);
            // 本层模型（只扫 General 程序集）
            builder.RegisterModels(typeof(GeneralLifetimeScope).Assembly);
            // 本层 ModelLifecycle（IPostStartable）
            builder.RegisterModelLifecycle();
            // 本层启动入口（IPostStartable，在 ModelLifecycle 之后触发）
            builder.RegisterEntryPoint<GeneralLayerEntrypoint>();

            PendingMessagePipeOptions = null;
        }
    }
}
