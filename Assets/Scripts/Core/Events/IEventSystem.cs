using System;

namespace Core
{
    /// <summary>
    /// 事件系统对外入口。
    /// </summary>
    public interface IEventSystem
    {
        IDisposable Subscribe(EventId eventId, Action handler, int priority = 0, object owner = null);
        IDisposable Subscribe<T>(EventId eventId, Action<T> handler, int priority = 0, object owner = null);
        IDisposable SubscribeUntil(EventId eventId, Func<bool> handler, int priority = 0, object owner = null);
        IDisposable SubscribeUntil<T>(EventId eventId, Func<T, bool> handler, int priority = 0, object owner = null);
        void UnsubscribeOwner(object owner);
        void Fire(EventId eventId);
        void Fire<T>(EventId eventId, T payload);
        bool FireUntil(EventId eventId);
        bool FireUntil<T>(EventId eventId, T payload);
        void Clear();
    }
}
