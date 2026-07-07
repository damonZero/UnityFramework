using System.Collections.Generic;
using Framework.Asset;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// Carries the AOT-initialized runtime state across the AOT -> hot-update
    /// boundary. Constructed on the AOT side (Launcher) and consumed by the
    /// hot-update <c>BootUpdateRunner</c>.
    /// </summary>
    public sealed class BootBridge
    {
        public ResourcePackage Package { get; }
        public BootStartupSettings Settings { get; }
        public IBootStartupView View { get; }
        public AssetConfig Config { get; }
        public IReadOnlyList<BootStartupLogEntry> EarlyLogs { get; }

        public BootBridge(
            ResourcePackage package,
            BootStartupSettings settings,
            IBootStartupView view,
            AssetConfig config,
            IReadOnlyList<BootStartupLogEntry> earlyLogs)
        {
            Package = package ?? throw new System.ArgumentNullException(nameof(package));
            Settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            View = view;
            Config = config ?? throw new System.ArgumentNullException(nameof(config));
            EarlyLogs = earlyLogs ?? System.Array.Empty<BootStartupLogEntry>();
        }
    }
}
