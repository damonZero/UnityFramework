using Core.Systems.Attributes;
using Microsoft.Extensions.Logging;

namespace Core.Systems
{
    /// <summary>
    /// 最小可运行系统，用于验证启动链路和容器接入。
    /// </summary>
    [CoreSystem]
    public sealed class StartupProbeSystem : ISystem
    {
        private readonly ILogger<StartupProbeSystem> _logger;

        public int Priority => 0;

        public StartupProbeSystem(ILogger<StartupProbeSystem> logger)
        {
            _logger = logger;
        }

        public void Init()
        {
            StartupProbeSystemLog.ProbeInit(_logger);
        }

        public void Shutdown()
        {
            StartupProbeSystemLog.ProbeShutdown(_logger);
        }
    }
}
