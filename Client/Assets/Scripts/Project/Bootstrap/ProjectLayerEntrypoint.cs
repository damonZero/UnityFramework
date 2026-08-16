using System;
using Cysharp.Threading.Tasks;
using Framework.Log;
using Microsoft.Extensions.Logging;
using Project.Demo;
using VContainer;
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
        private readonly IObjectResolver _resolver;

        public ProjectLayerEntrypoint(
            General.IModelStartupStatus modelStartupStatus,
            ILogger<ProjectLayerEntrypoint> logger,
            IObjectResolver resolver)
        {
            _modelStartupStatus = modelStartupStatus ?? throw new ArgumentNullException(nameof(modelStartupStatus));
            _logger = logger;
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void PostStart()
        {
            if (_modelStartupStatus.IsLoaded)
            {
                ProjectLayerEntrypointLog.ProjectReady(_logger);
                // 静态 Scope 供 MVVM 等框架层按需解析 ViewModel/Model（最终指向叶子 Project scope）。
                var projectScope = _resolver.ApplicationOrigin as LifetimeScope;
                if (projectScope != null)
                {
                    Framework.DependencyInjection.Dependencies.Scope = projectScope;
                }
                // 热更内容标记：发布新补丁后此日志应显示最新补丁版本号（验证内容级热更）。
                GameLog.Info("[Project] Hot-update runtime marker: v1.0.2", "Project.Bootstrap");

                // 测试：打开 DemoForm 验证 UI 框架链路（临时验证用，稳定后移除）。
                OpenDemoFormForTest().Forget();
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

        /// <summary>
        /// 打开 DemoForm 验证 UI 框架链路（临时测试用）。
        /// 注意：依赖 DemoForm.prefab 可被 IAssetSystem 加载（YooAsset 收集，见
        /// .planning/YOOASSET_RESOURCE_COLLECTION.md），否则走到「打开失败」分支。
        /// </summary>
        private static async UniTask OpenDemoFormForTest()
        {
            var form = await ViewDemo.OpenDemoForm();
            if (form != null)
            {
                GameLog.Info($"[Demo] DemoForm 打开成功: {form}", "Project.Bootstrap");
            }
            else
            {
                GameLog.Error("[Demo] DemoForm 打开失败：请确认 DemoForm.prefab 已加入 YooAsset 收集",
                    "Project.Bootstrap");
            }
        }
    }
}
