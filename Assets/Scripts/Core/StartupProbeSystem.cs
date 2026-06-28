using UnityEngine;

namespace KJ.Core
{
    /// <summary>
    /// 最小可运行系统，用于验证启动链路和容器接入。
    /// </summary>
    public sealed class StartupProbeSystem : ISystem
    {
        public int Priority => 0;

        public void Init()
        {
            Debug.Log("[StartupProbeSystem] Init");
        }

        public void Shutdown()
        {
            Debug.Log("[StartupProbeSystem] Shutdown");
        }
    }
}
