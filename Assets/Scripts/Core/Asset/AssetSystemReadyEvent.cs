using Framework.Event;

namespace Core.Asset
{
    /// <summary>
    /// Published when the asset system has completed initialization
    /// and is ready to serve load requests.
    /// </summary>
    [GameEvent]
    public readonly struct AssetSystemReadyEvent
    {
    }
}
