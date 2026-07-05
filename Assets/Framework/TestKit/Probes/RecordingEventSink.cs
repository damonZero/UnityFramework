using System.Collections.Generic;
using NUnit.Framework;

namespace Framework.TestKit.Probes
{
    public sealed class RecordingEventSink<TEvent>
    {
        private readonly List<TEvent> _events = new();

        public IReadOnlyList<TEvent> Events => _events;
        public int Count => _events.Count;

        public RecordingEventSink()
        {
        }

        internal RecordingEventSink(List<TEvent> events)
        {
            _events = events;
        }

        public void Record(TEvent evt)
        {
            _events.Add(evt);
        }

        public void Clear()
        {
            _events.Clear();
        }

        public TEvent AssertSingle()
        {
            Assert.AreEqual(1, _events.Count, $"Expected 1 event, got {_events.Count}: [{string.Join(", ", _events)}].");
            return _events[0];
        }

        public void AssertEmpty()
        {
            Assert.AreEqual(0, _events.Count, $"Expected 0 events, got {_events.Count}: [{string.Join(", ", _events)}].");
        }
    }
}
