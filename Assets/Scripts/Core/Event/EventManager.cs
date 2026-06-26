using System;
using System.Collections.Generic;
using UnityEngine;

namespace KJ.Core
{
    /// <summary>
    /// 事件管理器，基于事件 ID 的发布/订阅系统。
    /// 支持优先级排序、Owner 管理（按 Owner 批量取消订阅）。
    /// </summary>
    public class EventManager : IModule
    {
        public int Priority => 200;

        /// <summary>
        /// 单个事件订阅。
        /// </summary>
        private class Subscription
        {
            public Action Handler;
            public object Owner;
            public int Priority;
        }

        // eventId → subscriptions
        private readonly Dictionary<int, List<Subscription>> _events = new Dictionary<int, List<Subscription>>();
        private readonly List<Subscription> _pool = new List<Subscription>();

        public void Init()
        {
            Debug.Log("[EventManager] Init");
        }

        public void Shutdown()
        {
            _events.Clear();
            _pool.Clear();
            Debug.Log("[EventManager] Shutdown");
        }

        /// <summary>
        /// 订阅事件。
        /// </summary>
        /// <param name="eventId">事件 ID</param>
        /// <param name="handler">回调</param>
        /// <param name="owner">拥有者，用于批量取消</param>
        /// <param name="priority">优先级，越大越先执行</param>
        public void Subscribe(int eventId, Action handler, object owner, int priority = 0)
        {
            if (handler == null)
            {
                Debug.LogError("[EventManager] Subscribe: handler is null");
                return;
            }

            if (!_events.TryGetValue(eventId, out var list))
            {
                list = new List<Subscription>();
                _events[eventId] = list;
            }

            list.Add(new Subscription
            {
                Handler = handler,
                Owner = owner,
                Priority = priority
            });

            // 按优先级降序排列
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// 触发事件，同步执行所有订阅者。
        /// </summary>
        public void Fire(int eventId)
        {
            if (!_events.TryGetValue(eventId, out var list))
                return;

            // 遍历副本，防止在回调中修改列表
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Handler?.Invoke();
            }
        }

        /// <summary>
        /// 取消指定事件的指定回调。
        /// </summary>
        public void Unsubscribe(int eventId, Action handler)
        {
            if (!_events.TryGetValue(eventId, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Handler == handler)
                {
                    list.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 取消某个 Owner 的所有订阅。
        /// </summary>
        public void UnsubscribeAll(object owner)
        {
            if (owner == null) return;

            foreach (var kvp in _events)
            {
                var list = kvp.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].Owner == owner)
                    {
                        list.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有事件订阅。
        /// </summary>
        public void Clear()
        {
            _events.Clear();
        }
    }
}
