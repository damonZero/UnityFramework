using System;
using Microsoft.Extensions.Logging;
using VContainer.Unity;

namespace Project.Bootstrap
{
    /// <summary>
    /// Project 层启动入口（分层启动链 Phase 3）。
    /// 实现 <see cref="IPostStartable"/>，在 <see cref="General.Models.ModelLifecycle"/>（IPostStartable，
    /// 先注册先触发）加载本层模型之后执行。标记 Project 启动完成；如有模型失败则记录。
    /// 注入 <see cref="General.IModelStartupStatus"/>：解析到本层 ModelLifecycle（Project scope 的
    /// Configure 总是 RegisterModelLifecycle；若未注册则 scope 构建失败，本项目不会启动，
    /// 因此不存在向上回退到 General 状态的分支）。
    /// </summary>
    public sealed class ProjectLayerEntrypoint : IPostStartable, IDisposable
    {
        private readonly General.IModelStartupStatus _modelStartupStatus;
        private readonly ILogger<ProjectLayerEntrypoint> _logger;

        public ProjectLayerEntrypoint(
            General.IModelStartupStatus modelStartupStatus,
            ILogger<ProjectLayerEntrypoint> logger)
        {
            _modelStartupStatus = modelStartupStatus ?? throw new ArgumentNullException(nameof(modelStartupStatus));
            _logger = logger;
        }

        public void PostStart()
        {
            if (_modelStartupStatus.IsLoaded)
            {
                ProjectLayerEntrypointLog.ProjectReady(_logger);
                return;
            }

            var failed = _modelStartupStatus.FailedModelNames.Count == 0
                ? "<none>"
                : string.Join(", ", _modelStartupStatus.FailedModelNames);
            ProjectLayerEntrypointLog.ProjectStartupFailed(_logger, failed);
        }

        public void Dispose()
        {
        }
    }
}
