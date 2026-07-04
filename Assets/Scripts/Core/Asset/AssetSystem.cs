using Core.Systems;
using Core.Systems.Attributes;
using Framework.Asset;
using MessagePipe;
using Microsoft.Extensions.Logging;
using UnityEngine;

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
            var config = Resources.Load<AssetConfig>("AssetConfig");
            if (config == null)
                AssetSystemLog.ConfigNotFound(_logger);
            if (!_runtime.Initialize(config) || !_runtime.IsReady)
            {
                AssetSystemLog.InitializeFailed(_logger);
                throw new System.InvalidOperationException("AssetSystem failed to initialize runtime.");
            }

            _readyPublisher.Publish(new AssetSystemReadyEvent());
            AssetSystemLog.Ready(_logger);
        }

        public void Shutdown()
        {
            _runtime.Shutdown();
            AssetSystemLog.Shutdown(_logger);
        }
    }
}
