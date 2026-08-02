using System.Collections.Generic;

namespace Framework.Cache
{
    /// <summary>
    /// O(1) LRU 淘汰策略，使用 OnAdded/OnAccessed/OnRemoved 三方法。
    /// </summary>
    public sealed class LruPolicy<TKey> : IStoreEvictionPolicy<TKey>
        where TKey : notnull
    {
        private readonly LinkedList<TKey> _order = new();
        private readonly Dictionary<TKey, LinkedListNode<TKey>> _nodes = new();

        public void OnAdded(TKey key)
        {
            if (_nodes.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _nodes.Remove(key);
            }

            _nodes[key] = _order.AddFirst(key);
        }

        public void OnAccessed(TKey key)
        {
            if (_nodes.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _nodes[key] = _order.AddFirst(key);
            }
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
            var last = _order.Last;
            if (last is null)
            {
                key = default!;
                return false;
            }

            key = last.Value;
            return true;
        }
    }
}
