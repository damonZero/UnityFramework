using System;
using System.Collections.Generic;

namespace Framework.Cache
{
    public sealed class ResourceCache<TKey, TValue> : ICacheResContainer<TKey, TValue>
        where TKey : notnull
    {
        private readonly Func<TKey, TValue> _factory;
        private readonly Action<TValue>? _reset;
        private readonly Dictionary<TKey, TValue> _values = new();

        public ResourceCache(Func<TKey, TValue> factory, Action<TValue>? reset = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
        }

        public bool TryGet(TKey key, out TValue value) => _values.TryGetValue(key, out value!);

        public TValue GetOrCreate(TKey key)
        {
            if (_values.TryGetValue(key, out var value))
            {
                return value;
            }

            value = _factory(key);
            _values[key] = value;
            return value;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            if (_values.TryGetValue(key, out value!))
            {
                _values.Remove(key);
                _reset?.Invoke(value);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            if (_reset != null)
            {
                foreach (var value in _values.Values)
                {
                    _reset(value);
                }
            }

            _values.Clear();
        }
    }
}
