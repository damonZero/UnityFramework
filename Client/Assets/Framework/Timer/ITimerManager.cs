using System;

namespace Framework.Timer
{
    /// <summary>
    /// 计时器调度器接口 — 纯 C# tick-based（非协程）。
    /// 由外部（Core 层 SystemManager）以固定帧调用 <see cref="Tick"/> 推进。
    /// </summary>
    public interface ITimerManager
    {
        /// <summary>
        /// 全局时间缩放。0 表示暂停全部计时器（不影响单计时器的 Pause/Resume 状态）。
        /// 负值会被钳制为 0。
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// 当前列表中的计时器数量（已取消/已完成的计时器在下次 <see cref="Tick"/> 时移除）。
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// 调度一次性计时器，delay 秒后触发一次。
        /// </summary>
        TimerHandle ScheduleOnce(float delay, Action callback);

        /// <summary>
        /// 调度循环计时器，每 interval 秒触发一次；首次触发前等待 initialDelay 秒。
        /// </summary>
        TimerHandle ScheduleLoop(float interval, Action callback, float initialDelay = 0f);

        /// <summary>
        /// 推进计时器，传入自上次调用以来经过的秒数。
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 取消并回收全部计时器。
        /// </summary>
        void Clear();
    }
}
