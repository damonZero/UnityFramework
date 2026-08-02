using MessagePipe;
using VContainer.Unity;

namespace General.Bootstrap
{
    /// <summary>
    /// General 层反射入口（分层启动链 Phase 2）。
    /// 被 Core 层 <see cref="Core.Bootstrap.CoreLayerEntrypoint"/> 反射调用。
    /// 从父 scope 容器解析 MessagePipeOptions（RegisterMessagePipe 内部已注册为可解析 Singleton，
    /// 见分层启动计划 §0.1），存入 <see cref="GeneralLifetimeScope.PendingMessagePipeOptions"/> 供 Configure 消费；
    /// 再通过父 scope 的 CreateChild 创建 General 子 scope。
    /// </summary>
    public static class GeneralStartup
    {
        private static GeneralLifetimeScope _scope;

        public static void Start(LifetimeScope parentScope)
        {
            if (_scope != null)
                return;

            if (parentScope == null)
                throw new System.ArgumentNullException(nameof(parentScope));

            // 从 Core 容器解析唯一消息域配置（General 不重复 RegisterMessagePipe）。
            var options = parentScope.Container.Resolve(typeof(MessagePipeOptions)) as MessagePipeOptions;
            if (options == null)
                throw new System.InvalidOperationException(
                    "MessagePipeOptions not resolvable from parent scope. Core must RegisterInstance(options).");
            GeneralLifetimeScope.PendingMessagePipeOptions = options;

            _scope = parentScope.CreateChild<GeneralLifetimeScope>(childScopeName: nameof(GeneralLifetimeScope));
        }

        /// <summary>
        /// 显式重置 General scope 静态引用（Repair 场景，由 Core root 销毁级联触发）。
        /// </summary>
        public static void Reset()
        {
            _scope = null;
        }
    }
}
