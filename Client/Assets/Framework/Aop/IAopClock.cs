using System;
using System.Diagnostics;

namespace Framework.Aop
{
    public interface IAopClock
    {
        long Frequency { get; }
        long GetTimestamp();
        DateTime UtcNow { get; }
    }

    public sealed class StopwatchAopClock : IAopClock
    {
        public static readonly StopwatchAopClock Instance = new StopwatchAopClock();

        private StopwatchAopClock()
        {
        }

        public long Frequency => Stopwatch.Frequency;
        public long GetTimestamp() => Stopwatch.GetTimestamp();
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
