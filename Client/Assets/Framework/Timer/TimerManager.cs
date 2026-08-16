using System;
using System.Collections.Generic;

namespace Framework.Timer
{
    /// <summary>
    /// 纯 C# tick-based 计时器调度器（非协程）。
    /// 由外部以固定帧调用 <see cref="Tick"/> 推进；计时器节点内部池化复用，最小化 GC。
    /// 不引用 UnityEngine / Log / UniTask，保持零依赖。
    /// </summary>
    public sealed class TimerManager : ITimerManager
    {
        private readonly List<TimerNode> _active = new();
        private readonly Stack<TimerNode> _free = new();
        private float _timeScale = 1f;

        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = value < 0f ? 0f : value;
        }

        public int ActiveCount => _active.Count;

        public TimerHandle ScheduleOnce(float delay, Action callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return Schedule(Math.Max(0f, delay), 0f, false, callback);
        }

        public TimerHandle ScheduleLoop(float interval, Action callback, float initialDelay = 0f)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (interval < 0f) throw new ArgumentOutOfRangeException(nameof(interval));
            return Schedule(Math.Max(0f, initialDelay), interval, true, callback);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            var dt = deltaTime * _timeScale;
            if (dt <= 0f)
                return;

            // 快照本次 Tick 开始时的计时器数量，回调中新建的计时器不在本次 Tick 触发。
            // 但回调可能调用 Clear() 清空列表，因此每次迭代重新校验 active.Count，防止越界。
            var active = _active;
            var snapshot = active.Count;
            for (var i = 0; i < snapshot && i < active.Count; i++)
            {
                var node = active[i];
                if (!node.IsActive || node.IsPaused)
                    continue;

                node.Remaining -= dt;
                if (node.Remaining > 0f)
                    continue;

                if (node.IsLoop)
                {
                    if (node.Interval > 0f)
                    {
                        // 卡顿错过多个周期时，把负的 Remaining 换算成 overshoot，
                        // 一次合并跳过已错过的周期并回正到下一个未来周期，
                        // 避免逐帧补发（每帧只 +Interval 会在卡顿后连续多帧触发）。
                        var overshoot = -node.Remaining;
                        node.Remaining = node.Interval - overshoot % node.Interval;
                    }
                    else
                    {
                        node.Remaining = 0f;
                    }

                    Invoke(node);
                }
                else
                {
                    node.IsActive = false;
                    Invoke(node);
                }
            }

            Compact();
        }

        public void Clear()
        {
            for (var i = 0; i < _active.Count; i++)
                Recycle(_active[i]);

            _active.Clear();
        }

        private TimerHandle Schedule(float delay, float interval, bool isLoop, Action callback)
        {
            var node = Rent();
            node.Callback = callback;
            node.Remaining = delay;
            node.Interval = interval;
            node.IsLoop = isLoop;
            node.IsPaused = false;
            node.IsActive = true;
            _active.Add(node);
            return new TimerHandle(node, node.Version);
        }

        private TimerNode Rent()
        {
            TimerNode node;
            if (_free.Count > 0)
                node = _free.Pop();
            else
                node = new TimerNode();

            node.Version++;
            return node;
        }

        private void Recycle(TimerNode node)
        {
            node.Reset();
            _free.Push(node);
        }

        private static void Invoke(TimerNode node)
        {
            var callback = node.Callback;
            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                // 计时器回调异常不能中断整帧调度，交给 Core 注入的日志钩子记录完整异常。
                TimerDependencies.LogError?.Invoke(ex);
            }
        }

        private void Compact()
        {
            var write = 0;
            for (var i = 0; i < _active.Count; i++)
            {
                var node = _active[i];
                if (!node.IsActive)
                {
                    Recycle(node);
                    continue;
                }

                _active[write++] = node;
            }

            if (write < _active.Count)
                _active.RemoveRange(write, _active.Count - write);
        }
    }
}
