using MessagePipe;
using VContainer.Unity;

namespace Project.Bootstrap
{
    /// <summary>
    /// Project 层反射入口（分层启动链 Phase 3）。
    /// 被 General 层 <see cref="General.Bootstrap.GeneralLayerEntrypoint"/> 反射调用。
    /// 从父 scope 容器解析 MessagePipeOptions（Core 已注册，见分层启动计划 §0.1），
    /// 存入 <see cref="ProjectLifetimeScope.PendingMessagePipeOptions"/> 供 Configure 消费；
    /// 再通过父 scope 的 CreateChild 创建 Project 子 scope。
    /// </summary>
    public static class ProjectStartup
    {
        private static ProjectLifetimeScope _scope;

        public static void Start(LifetimeScope parentScope)
        {
            if (_scope != null)
                return;

            if (parentScope == null)
                throw new System.ArgumentNullException(nameof(parentScope));

            // 从父容器解析唯一消息域配置（Project 不重复 RegisterMessagePipe）。
            var options = parentScope.Container.Resolve(typeof(MessagePipeOptions)) as MessagePipeOptions;
            if (options == null)
                throw new System.InvalidOperationException(
                    "MessagePipeOptions not resolvable from parent scope. Core must RegisterMessagePipe.");
            ProjectLifetimeScope.PendingMessagePipeOptions = options;

            _scope = parentScope.CreateChild<ProjectLifetimeScope>(childScopeName: nameof(ProjectLifetimeScope));
        }

        /// <summary>
        /// 显式重置 Project scope 静态引用（Repair/软重启场景，由 Core root 销毁级联反射触发）。
        /// 清空 _scope 与挂起的消息域配置，使下次 <see cref="Start"/> 能重建。
        /// </summary>
        public static void Reset()
        {
            _scope = null;
            ProjectLifetimeScope.PendingMessagePipeOptions = null;
        }
    }
}
