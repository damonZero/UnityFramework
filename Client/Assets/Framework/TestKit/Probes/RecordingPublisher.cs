using System.Collections.Generic;
using MessagePipe;

namespace Framework.TestKit.Probes
{
    public sealed class RecordingPublisher<TEvent> : IPublisher<TEvent>
    {
        private readonly List<TEvent> _publishedEvents = new();

        public IReadOnlyList<TEvent> PublishedEvents => _publishedEvents;
        public int Count => _publishedEvents.Count;

        public void Publish(TEvent message)
        {
            _publishedEvents.Add(message);
        }

        public TEvent AssertSingle()
        {
            return new RecordingEventSink<TEvent>(_publishedEvents).AssertSingle();
        }

        public void AssertEmpty()
        {
            new RecordingEventSink<TEvent>(_publishedEvents).AssertEmpty();
        }

        public void Clear()
        {
            _publishedEvents.Clear();
        }
    }
}
