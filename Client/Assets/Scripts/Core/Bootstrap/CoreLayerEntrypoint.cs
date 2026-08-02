using System;
using Core.Systems;
using Microsoft.Extensions.Logging;
using VContainer;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 层启动入口（分层启动链 Phase 2）。
    /// 实现 <see cref="IPostStartable"/>，在 <see cref="SystemManager"/>（IStartable）全部
    /// Init 成功后触发。检查 Core 启动状态：仅当全部系统初始化成功才反射启动 General；
    /// 否则记录失败并阻断，不创建下一层。
    /// 通过 <see cref="IObjectResolver.ApplicationOrigin"/> 获取本层 scope（VContainer 构建时
    /// ApplicationOrigin 指向 LifetimeScope），避免依赖静态 RootScope 的时序问题。
    /// </summary>
    public sealed class CoreLayerEntrypoint : IPostStartable, IDisposable
    {
        private readonly ICoreStartupStatus _coreStartupStatus;
        private readonly ILogger<CoreLayerEntrypoint> _logger;
        private readonly IObjectResolver _resolver;

        public CoreLayerEntrypoint(
            ICoreStartupStatus coreStartupStatus,
            ILogger<CoreLayerEntrypoint> logger,
            IObjectResolver resolver)
        {
            _coreStartupStatus = coreStartupStatus ?? throw new ArgumentNullException(nameof(coreStartupStatus));
            _logger = logger;
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void PostStart()
        {
            if (!_coreStartupStatus.IsStarted || _coreStartupStatus.HasInitFailures)
            {
                var failed = _coreStartupStatus.FailedSystemNames.Count == 0
                    ? "<none>"
                    : string.Join(", ", _coreStartupStatus.FailedSystemNames);
                CoreLayerEntrypointLog.CoreStartupFailed(_logger, failed);
                return;
            }

            // ApplicationOrigin 指向创建本 scope 的 LifetimeScope（VContainer Build 时赋值）。
            var coreScope = _resolver.ApplicationOrigin as LifetimeScope;
            if (coreScope == null)
            {
                CoreLayerEntrypointLog.CoreScopeNotReady(_logger);
                return;
            }

            // 反射启动 General（Core 不编译期引用 General）。
            // 契约：General.Bootstrap.GeneralStartup.Start(LifetimeScope)
            const string startupTypeName = "General.Bootstrap.GeneralStartup, General";
            LayerStartupReflector.InvokeStart(startupTypeName, coreScope, (result, typeName, ex) =>
            {
                switch (result)
                {
                    case LayerStartupReflector.InvokeResult.TypeNotFound:
                        CoreLayerEntrypointLog.GeneralStartupTypeNotFound(_logger, typeName);
                        break;
                    case LayerStartupReflector.InvokeResult.MethodNotFound:
                        CoreLayerEntrypointLog.GeneralStartupMethodNotFound(_logger, typeName);
                        break;
                    case LayerStartupReflector.InvokeResult.SignatureUnsupported:
                        CoreLayerEntrypointLog.GeneralStartupSignatureUnsupported(_logger, typeName);
                        break;
                    case LayerStartupReflector.InvokeResult.InvokeFailed:
                        CoreLayerEntrypointLog.GeneralStartupFailed(_logger, typeName, ex);
                        break;
                }
            });
        }

        public void Dispose()
        {
        }
    }
}
