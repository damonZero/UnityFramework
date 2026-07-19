using System;
using System.Threading;

namespace Framework.Aop
{
    public sealed class AopSpan : IDisposable
    {
        internal static readonly AopSpan Disabled = new AopSpan();

        private readonly AopSessionState _state;
        private readonly AopSpan _parent;
        private readonly string _spanId;
        private readonly string _name;
        private readonly string _category;
        private readonly long _startedTimestamp;
        private readonly DateTime _startedAtUtc;
        private int _completed;
        private string _status = "Success";
        private string _exceptionType;

        private AopSpan()
        {
        }

        internal AopSpan(AopSessionState state, AopSpan parent, string name, string category)
        {
            _state = state;
            _parent = parent;
            _spanId = state.NextSpanId();
            _name = name;
            _category = category ?? string.Empty;
            _startedTimestamp = state.Clock.GetTimestamp();
            _startedAtUtc = state.Clock.UtcNow;
        }

        internal AopSessionState SessionState => _state;
        internal string SpanId => _spanId;

        public void Fail(Exception exception)
        {
            if (_state == null || Volatile.Read(ref _completed) != 0)
                return;

            _status = "Failure";
            _exceptionType = exception?.GetType().FullName;
        }

        public void Cancel()
        {
            if (_state == null || Volatile.Read(ref _completed) != 0)
                return;

            _status = "Cancelled";
        }

        public void Dispose()
        {
            if (_state == null || Interlocked.Exchange(ref _completed, 1) != 0)
                return;

            long durationTicks = Math.Max(0, _state.Clock.GetTimestamp() - _startedTimestamp);
            double durationMs = durationTicks * 1000.0 / _state.Clock.Frequency;

            _state.Publish(new AopEvent
            {
                RunId = _state.RunId,
                SpanId = _spanId,
                ParentSpanId = _parent?.SpanId,
                Name = _name,
                Category = _category,
                StartedAtUtc = _startedAtUtc.ToString("o"),
                DurationTicks = durationTicks,
                DurationMs = durationMs,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                Status = _status,
                ExceptionType = _exceptionType,
            });

            AopRuntime.RestoreCurrent(this, _parent);
        }
    }
}
