using System.Collections.Generic;

namespace Framework.Cache
{
    /// <summary>
    /// 纯容量策略：仅保留 N 个条目，按 FIFO（先进先出）顺序淘汰。
    /// 选用 FIFO 而非无序 reservoir，确保 <see cref="TrySelectEvictionCandidate"/> 永远返回「最旧」key，
    /// 不会返回刚被 Put 的新 key（配合 BoundedStore.PutUnsafe 的 H1 守卫，彻底满足
    /// 「Put 后 key 必存在」不变量，且容量不会临时溢出）。
    /// 内部用 LinkedList + Dictionary&lt;key, node&gt; 双索引，OnAdded / OnRemoved / 淘汰均为 O(1)。
    /// </summary>
    public sealed class CapacityPolicy<TKey> : IStoreEvictionPolicy<TKey>
        where TKey : notnull
    {
        private readonly LinkedList<TKey> _order = new();
        private readonly Dictionary<TKey, LinkedListNode<TKey>> _nodes = new();

        public void OnAdded(TKey key)
        {
            // 已在追踪：保持原 FIFO 位置，不重复入列（BoundedStore 覆盖路径会先 OnRemoved，正常不会走到这里）。
            if (_nodes.ContainsKey(key))
            {
                return;
            }

            _nodes[key] = _order.AddLast(key);
        }

        public void OnAccessed(TKey key)
        {
            // 无排序语义，访问不改变淘汰顺序。
        }

        public void OnRemoved(TKey key)
        {
            if (_nodes.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _nodes.Remove(key);
            }
        }

        public void Clear()
        {
            _order.Clear();
            _nodes.Clear();
        }

        public bool TrySelectEvictionCandidate(out TKey key)
        {
            if (_order.Count > 0)
            {
                key = _order.First!.Value;
                return true;
            }

            key = default!;
            return false;
        }
    }
}
