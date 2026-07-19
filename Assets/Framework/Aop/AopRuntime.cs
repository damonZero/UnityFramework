using System;
using System.Threading;

namespace Framework.Aop
{
    public static class AopRuntime
    {
        private static readonly object Gate = new object();
        private static readonly AsyncLocal<AopSpan> CurrentSpanSlot = new AsyncLocal<AopSpan>();
        private static AopSessionState _activeSession;

        public static AopSession BeginSession(
            string runId,
            IAopCollector collector,
            IAopClock clock = null)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("Run ID is required.", nameof(runId));
            if (collector == null)
                throw new ArgumentNullException(nameof(collector));

            lock (Gate)
            {
                if (_activeSession != null)
                    throw new InvalidOperationException("An AOP session is already active.");

                var state = new AopSessionState(
                    runId,
                    collector,
                    clock ?? StopwatchAopClock.Instance);
                _activeSession = state;
                CurrentSpanSlot.Value = null;
                return new AopSession(state);
            }
        }

        public static AopSpan StartSpan(string name, string category = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Span name is required.", nameof(name));

            var state = Volatile.Read(ref _activeSession);
            if (state == null)
                return AopSpan.Disabled;

            var parent = CurrentSpanSlot.Value;
            if (parent != null && !ReferenceEquals(parent.SessionState, state))
                parent = null;

            var span = new AopSpan(state, parent, name, category);
            CurrentSpanSlot.Value = span;
            return span;
        }

        internal static void RestoreCurrent(AopSpan completed, AopSpan parent)
        {
            if (ReferenceEquals(CurrentSpanSlot.Value, completed))
                CurrentSpanSlot.Value = parent;
        }

        internal static void EndSession(AopSessionState state)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(_activeSession, state))
                    return;

                _activeSession = null;
                CurrentSpanSlot.Value = null;
            }
        }
    }

    public sealed class AopSession : IDisposable
    {
        private AopSessionState _state;

        internal AopSession(AopSessionState state)
        {
            _state = state;
        }

        public int CollectorFailureCount
        {
            get
            {
                var state = Volatile.Read(ref _state);
                return state?.CollectorFailureCount ?? 0;
            }
        }

        public void Dispose()
        {
            var state = Interlocked.Exchange(ref _state, null);
            if (state != null)
                AopRuntime.EndSession(state);
        }
    }

    internal sealed class AopSessionState
    {
        private long _nextSpanId;
        private int _collectorFailureCount;

        public AopSessionState(string runId, IAopCollector collector, IAopClock clock)
        {
            if (clock.Frequency <= 0)
                throw new ArgumentOutOfRangeException(nameof(clock), "Clock frequency must be positive.");

            RunId = runId;
            Collector = collector;
            Clock = clock;
        }

        public string RunId { get; }
        public IAopCollector Collector { get; }
        public IAopClock Clock { get; }
        public int CollectorFailureCount => Volatile.Read(ref _collectorFailureCount);

        public string NextSpanId()
            => Interlocked.Increment(ref _nextSpanId).ToString(System.Globalization.CultureInfo.InvariantCulture);

        public void Publish(AopEvent spanEvent)
        {
            try
            {
                Collector.Record(spanEvent);
            }
            catch
            {
                Interlocked.Increment(ref _collectorFailureCount);
            }
        }
    }
}
