using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// MessagePipe-backed event system.
    /// 普通事件走 MessagePipe IPublisher/ISubscriber（由 VContainer 注入），
    /// FireUntil 保留框架语义的最小短路层。
    /// </summary>
    internal sealed class EventSystem : ISystem, IEventSystem
    {
        private readonly IPublisher<EventEnvelope> _publisher;
        private readonly ISubscriber<EventEnvelope> _subscriber;
        private readonly Dictionary<EventId, List<Func<object, bool>>> _untilHandlers = new();
        private readonly Dictionary<object, List<IDisposable>> _ownerDisposables = new();
        private readonly List<IDisposable> _allDisposables = new();

        public EventSystem(IPublisher<EventEnvelope> publisher, ISubscriber<EventEnvelope> subscriber)
        {
            _publisher = publisher;
            _subscriber = subscriber;
        }

        public int Priority => 0;

        public void Init()
        {
            Debug.Log("[EventSystem] Init");
        }

        public void Shutdown()
        {
            Clear();
            Debug.Log("[EventSystem] Shutdown");
        }

        public IDisposable Subscribe(EventId eventId, Action handler, int priority = 0, object owner = null)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var disposable = _subscriber.Subscribe(new EnvelopeHandler(envelope =>
            {
                if (envelope.EventId.Equals(eventId))
                    handler();
            }), Array.Empty<MessageHandlerFilter<EventEnvelope>>());

            Track(owner, disposable);
            return disposable;
        }

        public IDisposable Subscribe<T>(EventId eventId, Action<T> handler, int priority = 0, object owner = null)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var disposable = _subscriber.Subscribe(new EnvelopeHandler(envelope =>
            {
                if (!envelope.EventId.Equals(eventId))
                    return;

                if (envelope.Payload is T typed)
                {
                    handler(typed);
                    return;
                }

                if (envelope.Payload == null && default(T) == null)
                {
                    handler(default);
                    return;
                }

                var payloadTypeName = envelope.Payload?.GetType().Name ?? "null";
                throw new InvalidOperationException($"[EventSystem] Payload type mismatch. Expected {typeof(T).Name}, got {payloadTypeName}");
            }), Array.Empty<MessageHandlerFilter<EventEnvelope>>());

            Track(owner, disposable);
            return disposable;
        }

        public IDisposable SubscribeUntil(EventId eventId, Func<bool> handler, int priority = 0, object owner = null)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_untilHandlers.TryGetValue(eventId, out var list))
            {
                list = new List<Func<object, bool>>();
                _untilHandlers[eventId] = list;
            }

            Func<object, bool> wrapped = _ => handler();
            list.Add(wrapped);
            return TrackUntilOwner(owner, eventId, wrapped);
        }

        public IDisposable SubscribeUntil<T>(EventId eventId, Func<T, bool> handler, int priority = 0, object owner = null)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_untilHandlers.TryGetValue(eventId, out var list))
            {
                list = new List<Func<object, bool>>();
                _untilHandlers[eventId] = list;
            }

            Func<object, bool> wrapped = payload => handler(payload is T typed ? typed : default);
            list.Add(wrapped);
            return TrackUntilOwner(owner, eventId, wrapped);
        }

        public void UnsubscribeOwner(object owner)
        {
            if (owner == null) return;

            if (_ownerDisposables.TryGetValue(owner, out var disposables))
            {
                for (var i = disposables.Count - 1; i >= 0; i--)
                {
                    disposables[i].Dispose();
                    _allDisposables.Remove(disposables[i]);
                }

                _ownerDisposables.Remove(owner);
            }
        }

        public void Fire(EventId eventId)
        {
            _publisher.Publish(new EventEnvelope(eventId, null));
        }

        public void Fire<T>(EventId eventId, T payload)
        {
            _publisher.Publish(new EventEnvelope(eventId, payload));
        }

        public bool FireUntil(EventId eventId)
        {
            return FireUntilInternal(eventId, null);
        }

        public bool FireUntil<T>(EventId eventId, T payload)
        {
            return FireUntilInternal(eventId, payload);
        }

        public void Clear()
        {
            for (var i = _allDisposables.Count - 1; i >= 0; i--)
            {
                _allDisposables[i].Dispose();
            }

            _allDisposables.Clear();
            _ownerDisposables.Clear();
            _untilHandlers.Clear();
        }

        private bool FireUntilInternal(EventId eventId, object payload)
        {
            if (!_untilHandlers.TryGetValue(eventId, out var list))
                return false;

            var snapshot = list.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i](payload))
                    return true;
            }

            return false;
        }

        private IDisposable TrackUntilOwner(object owner, EventId eventId, Func<object, bool> handler)
        {
            var disposable = new UntilSubscription(this, eventId, handler);
            Track(owner, disposable);
            return disposable;
        }

        private void Track(object owner, IDisposable disposable)
        {
            _allDisposables.Add(disposable);

            if (owner == null) return;

            if (!_ownerDisposables.TryGetValue(owner, out var list))
            {
                list = new List<IDisposable>();
                _ownerDisposables[owner] = list;
            }

            list.Add(disposable);
        }

        private void RemoveUntilHandler(EventId eventId, Func<object, bool> handler)
        {
            if (_untilHandlers.TryGetValue(eventId, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                    _untilHandlers.Remove(eventId);
            }
        }

        private sealed class EnvelopeHandler : IMessageHandler<EventEnvelope>
        {
            private readonly Action<EventEnvelope> _handler;
            public EnvelopeHandler(Action<EventEnvelope> handler) => _handler = handler;
            public void Handle(EventEnvelope message) => _handler(message);
        }

        private sealed class UntilSubscription : IDisposable
        {
            private readonly EventSystem _owner;
            private readonly EventId _eventId;
            private readonly Func<object, bool> _handler;
            private bool _disposed;

            public UntilSubscription(EventSystem owner, EventId eventId, Func<object, bool> handler)
            {
                _owner = owner;
                _eventId = eventId;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _owner.RemoveUntilHandler(_eventId, _handler);
            }
        }

        internal readonly struct EventEnvelope
        {
            public EventEnvelope(EventId eventId, object payload)
            {
                EventId = eventId;
                Payload = payload;
            }

            public EventId EventId { get; }
            public object Payload { get; }
        }
    }
}
