using Core.Systems;
using Core.Systems.Attributes;
using Framework.Asset;
using MessagePipe;
using Microsoft.Extensions.Logging;

namespace Core.Asset
{
    /// <summary>
    /// Central asset loading service.
    /// </summary>
    [CoreSystem]
    public sealed class AssetSystem : ISystem
    {
        private readonly IAssetRuntime _runtime;
        private readonly IPublisher<AssetSystemReadyEvent> _readyPublisher;
        private readonly ILogger<AssetSystem> _logger;

        public int Priority => AssetConstants.SystemPriority;

        public AssetSystem(
            IAssetRuntime runtime,
            IPublisher<AssetSystemReadyEvent> readyPublisher,
            ILogger<AssetSystem> logger)
        {
            _runtime = runtime;
            _readyPublisher = readyPublisher;
            _logger = logger;
        }

        public void Init()
        {
            if (_runtime.IsReady)
            {
                _readyPublisher.Publish(new AssetSystemReadyEvent());
                AssetSystemLog.Ready(_logger);
                return;
            }

            AssetSystemLog.InitializeFailed(_logger);
            throw new System.InvalidOperationException(
                $"AssetSystem requires a ready IAssetRuntime. Boot should initialize resources before Core starts. Error={_runtime.LastError}");
        }

        public void Shutdown()
        {
            _runtime.Shutdown();
            AssetSystemLog.Shutdown(_logger);
        }
    }
}
