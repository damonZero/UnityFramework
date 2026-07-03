using System.Collections.Generic;

namespace Framework.Cache
{
    public sealed class LruCachePolicy<TKey> : ICacheEvictionPolicy<TKey>
        where TKey : notnull
    {
        private readonly LinkedList<TKey> _order = new();
        private readonly Dictionary<TKey, LinkedListNode<TKey>> _nodes = new();

        public void Touch(TKey key)
        {
            if (_nodes.TryGetValue(key, out var node))
            {
                _order.Remove(node);
                _order.AddFirst(node);
                return;
            }

            var newNode = _order.AddFirst(key);
            _nodes[key] = newNode;
        }

        public void Remove(TKey key)
        {
            if (!_nodes.TryGetValue(key, out var node))
            {
                return;
            }

            _order.Remove(node);
            _nodes.Remove(key);
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

        public void Clear()
        {
            _order.Clear();
            _nodes.Clear();
        }
    }
}
