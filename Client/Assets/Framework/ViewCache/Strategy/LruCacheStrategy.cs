using System;
using System.Collections.Generic;
using Cysharp.Text;

namespace Framework.ViewCache
{
    /// <summary>
    /// LRU（Least Recently Used）缓存策略
    /// 使用递增计数器跟踪访问顺序，当缓存满时淘汰最久未使用的项
    /// 使用LinkedList维护访问顺序，避免排序操作，实现O(1)的插入、访问和淘汰
    /// </summary>
    public class LruCacheStrategy<KeyT> : AbstractStrategy<KeyT>
    {
        // 只记录空闲对象的访问顺序
        private readonly LinkedList<KeyT> _idleAccessOrder = new();
        private readonly Dictionary<KeyT, LinkedListNode<KeyT>> _idleNodeMap = new();

        // 记录正在使用的对象（用于状态跟踪，不参与淘汰）
        private readonly HashSet<KeyT> _inUseObjects = new();

        protected override List<Type> NeedStatisticsList()
        {
            return new List<Type>();
        }

        protected override void Put(KeyT key)
        {
            // 对象归还到池中，从使用中移除，加入空闲队列
            _inUseObjects.Remove(key);
            MoveToFirstIdle(key);

            // 只检查空闲对象的容量限制
            CheckIdleCapacityAndEvict();
        }

        protected override void Take(KeyT key)
        {
            // 对象被取走，从空闲队列移除，加入使用中
            if (_idleNodeMap.TryGetValue(key, out var node))
            {
                _idleAccessOrder.Remove(node);
                _idleNodeMap.Remove(key);
            }

            _inUseObjects.Add(key);
        }

        private void MoveToFirstIdle(KeyT key)
        {
            // 如果已在空闲队列中，先移除
            if (_idleNodeMap.TryGetValue(key, out var existingNode))
            {
                _idleAccessOrder.Remove(existingNode);
            }

            // 添加到空闲队列头部
            var newNode = _idleAccessOrder.AddFirst(key);
            _idleNodeMap[key] = newNode;
        }

        private void CheckIdleCapacityAndEvict()
        {
            if (Capacity <= 0)
            {
                // 容量为0时，清空所有空闲对象
                while (_idleAccessOrder.Count > 0)
                {
                    EvictLastIdle();
                }

                return;
            }

            // 只当空闲对象数量超过容量时才淘汰
            while (_idleAccessOrder.Count > Capacity)
            {
                EvictLastIdle();
            }
        }

        private void EvictLastIdle()
        {
            var lastNode = _idleAccessOrder.Last;
            if (lastNode == null) return;

            var keyToEvict = lastNode.Value;
            _idleAccessOrder.RemoveLast();
            _idleNodeMap.Remove(keyToEvict);

            // 触发淘汰事件
            Eviction?.Invoke(keyToEvict);
        }

        public override void Destroy(KeyT key)
        {
            base.Destroy(key);

            // 从所有数据结构中移除
            _inUseObjects.Remove(key);
            if (!_idleNodeMap.TryGetValue(key, out var node)) return;
            _idleAccessOrder.Remove(node);
            _idleNodeMap.Remove(key);
        }

        public override void Clear()
        {
            _idleAccessOrder.Clear();
            _idleNodeMap.Clear();
            _inUseObjects.Clear();
        }

        public override string ToString()
        {
            using var sb = ZString.CreateStringBuilder();

            sb.Append("LRU Strategy: ");
            sb.Append(_inUseObjects.Count);
            sb.Append(" in-use, ");
            sb.Append(_idleAccessOrder.Count);
            sb.Append("/");
            sb.Append(Capacity);
            sb.Append(" idle");

            if (Capacity > 0)
            {
                var idleUsageRate = (float)_idleAccessOrder.Count / Capacity * 100f;
                sb.Append(" (");
                sb.Append(idleUsageRate.ToString("F1"));
                sb.Append("% idle capacity)");
            }

            if (_idleAccessOrder.Count > 0)
            {
                sb.AppendLine();
                sb.Append("  Most Recent Idle: ");
                sb.Append(_idleAccessOrder.First?.Value.ToString() ?? "None");

                if (_idleAccessOrder.Count > 1)
                {
                    sb.AppendLine();
                    sb.Append("  Least Recent Idle: ");
                    sb.Append(_idleAccessOrder.Last?.Value.ToString() ?? "None");
                }
            }

            return sb.ToString();
        }
    }


    /// <summary>
    /// LRU缓存策略，默认使用string作为key
    /// </summary>
    public class LruCacheStrategy : LruCacheStrategy<string>
    {

    }
}
