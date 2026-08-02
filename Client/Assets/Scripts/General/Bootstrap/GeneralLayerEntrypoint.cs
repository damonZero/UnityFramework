using System;
using Core.Bootstrap;
using Microsoft.Extensions.Logging;
using VContainer;
using VContainer.Unity;

namespace General.Bootstrap
{
    /// <summary>
    /// General 层启动入口（分层启动链 Phase 3）。
    /// 实现 <see cref="IPostStartable"/>，在 <see cref="General.Models.ModelLifecycle"/>（IPostStartable，
    /// 先注册先触发）加载本层模型之后执行。仅当本层模型全部加载成功才反射启动 Project；
    /// 否则记录失败并阻断，不创建下一层。
    /// 通过 <see cref="IObjectResolver.ApplicationOrigin"/> 获取本层 scope（VContainer 构建时
    /// ApplicationOrigin 指向 LifetimeScope），避免依赖静态 RootScope 的时序问题。
    /// </summary>
    public sealed class GeneralLayerEntrypoint : IPostStartable, IDisposable
    {
        private readonly IModelStartupStatus _modelStartupStatus;
        private readonly ILogger<GeneralLayerEntrypoint> _logger;
        private readonly IObjectResolver _resolver;

        public GeneralLayerEntrypoint(
            IModelStartupStatus modelStartupStatus,
            ILogger<GeneralLayerEntrypoint> logger,
            IObjectResolver resolver)
        {
            _modelStartupStatus = modelStartupStatus ?? throw new ArgumentNullException(nameof(modelStartupStatus));
            _logger = logger;
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void PostStart()
        {
            if (!_modelStartupStatus.IsLoaded)
            {
                var failed = _modelStartupStatus.FailedModelNames.Count == 0
                    ? "<none>"
                    : string.Join(", ", _modelStartupStatus.FailedModelNames);
                GeneralLayerEntrypointLog.GeneralStartupFailed(_logger, failed);
                return;
            }

            GeneralLayerEntrypointLog.GeneralReady(_logger);

            // ApplicationOrigin 指向创建本 scope 的 LifetimeScope（VContainer Build 时赋值）。
            var generalScope = _resolver.ApplicationOrigin as LifetimeScope;
            if (generalScope == null)
            {
                GeneralLayerEntrypointLog.GeneralScopeNotReady(_logger);
                return;
            }

            // 反射启动 Project（General 不编译期引用 Project）。
            // 契约：Project.Bootstrap.ProjectStartup.Start(LifetimeScope)
            const string startupTypeName = "Project.Bootstrap.ProjectStartup, Project";
            LayerStartupReflector.InvokeStart(startupTypeName, generalScope, (result, typeName, ex) =>
            {
                switch (result)
                {
                    case LayerStartupReflector.InvokeResult.TypeNotFound:
                        GeneralLayerEntrypointLog.ProjectStartupTypeNotFound(_logger, typeName);
                        break;
                    case LayerStartupReflector.InvokeResult.MethodNotFound:
                        GeneralLayerEntrypointLog.ProjectStartupMethodNotFound(_logger, typeName);
                        break;
                    case LayerStartupReflector.InvokeResult.SignatureUnsupported:
                        GeneralLayerEntrypointLog.ProjectStartupSignatureUnsupported(_logger, typeName);
                        break;
                    case LayerStartupReflector.InvokeResult.InvokeFailed:
                        GeneralLayerEntrypointLog.ProjectStartupFailed(_logger, typeName, ex);
                        break;
                }
            });
        }

        public void Dispose()
        {
        }
    }
}
