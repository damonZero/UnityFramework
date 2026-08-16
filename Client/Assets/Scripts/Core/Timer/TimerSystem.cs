using System;
using Core.Systems;
using Core.Systems.Attributes;
using Framework.Asset;
using Framework.Log;
using Framework.Timer;

namespace Core.Timer
{
    /// <summary>
    /// Framework.Timer 的 Core 桥接：由 SystemManager 每帧驱动，并把计时器回调异常接入 GameLog。
    /// </summary>
    [CoreSystem]
    public sealed class TimerSystem : ISystem, ITickableSystem
    {
        private readonly ITimerManager _timer;

        public int Priority => AssetConstants.SystemPriority + 20;

        public TimerSystem(ITimerManager timer)
        {
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        }

        public void Init()
        {
            TimerDependencies.LogError = ex =>
                GameLog.Exception(ex, "Timer callback failed", module: nameof(TimerSystem));
        }

        public void Shutdown()
        {
            _timer.Clear();
            TimerDependencies.LogError = null;
        }

        public void Update(float deltaTime) => _timer.Tick(deltaTime);

        public void LateUpdate(float deltaTime)
        {
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
        }
    }
}
