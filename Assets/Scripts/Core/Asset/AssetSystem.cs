using Core.Architecture;
using Framework.Asset;
using MessagePipe;
using UnityEngine;

namespace Core.Asset
{
    /// <summary>
    /// Central asset loading service.
    /// Caches handles keyed by path, protects concurrent loads, and releases
    /// handles on explicit Release or system shutdown.
    /// </summary>
    [CoreSystem]
    public sealed class AssetSystem : ISystem
    {
        private readonly IAssetRuntime _runtime;
        private readonly IPublisher<AssetSystemReadyEvent> _readyPublisher;

        public int Priority => AssetConstants.SystemPriority;

        public AssetSystem(
            IAssetRuntime runtime,
            IPublisher<AssetSystemReadyEvent> readyPublisher)
        {
            _runtime = runtime;
            _readyPublisher = readyPublisher;
        }

        // ──────────────────────────────────────────────
        //  ISystem lifecycle
        // ──────────────────────────────────────────────

        public void Init()
        {
            var config = Resources.Load<AssetConfig>("AssetConfig");
            if (config == null)
                Debug.LogWarning("[AssetSystem] AssetConfig.asset not found; using editor-simulate defaults.");
            _runtime.Initialize(config);
            _readyPublisher.Publish(new AssetSystemReadyEvent());
            Debug.Log("[AssetSystem] Ready");
        }

        public void Shutdown()
        {
            _runtime.Shutdown();
            Debug.Log("[AssetSystem] Shutdown");
        }
    }
}
