using System;

namespace Framework.Timer
{
    /// <summary>
    /// 计时器内部节点，由 <see cref="TimerManager"/> 池化复用，避免每次调度都堆分配。
    /// <see cref="Version"/> 在节点被重新租用时递增，用于让旧句柄失效。
    /// </summary>
    internal sealed class TimerNode
    {
        internal int Version;
        internal Action Callback;
        internal float Remaining;
        internal float Interval;
        internal bool IsLoop;
        internal bool IsPaused;
        internal bool IsActive;

        internal void Reset()
        {
            Callback = null;
            Remaining = 0f;
            Interval = 0f;
            IsLoop = false;
            IsPaused = false;
            IsActive = false;
        }
    }
}
