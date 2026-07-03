using System;
using System.Collections.Generic;

namespace Framework.Cache
{
    public sealed class Cache<TKey, TValue> : ICache<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _values;
        private readonly ICacheEvictionPolicy<TKey> _policy;
        private readonly int _capacity;
        private readonly Action<TKey, TValue>? _onEvicted;
        private readonly object _gate = new();

        public Cache(int capacity, ICacheEvictionPolicy<TKey> policy, Action<TKey, TValue>? onEvicted = null)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _onEvicted = onEvicted;
            _values = new Dictionary<TKey, TValue>(capacity > 0 ? capacity : 4);
        }

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _values.Count;
                }
            }
        }

        public int Capacity => _capacity;

        public bool TryGet(TKey key, out TValue value)
        {
            lock (_gate)
            {
                if (_values.TryGetValue(key, out value!))
                {
                    _policy.Touch(key);
                    return true;
                }

                value = default!;
                return false;
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (_gate)
            {
                if (_values.TryGetValue(key, out var existing))
                {
                    _policy.Touch(key);
                    return existing;
                }

                var value = factory(key);
                PutUnsafe(key, value);
                return value;
            }
        }

        public void Put(TKey key, TValue value)
        {
            lock (_gate)
            {
                PutUnsafe(key, value);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_gate)
            {
                if (!_values.TryGetValue(key, out var value))
                {
                    return false;
                }

                _values.Remove(key);
                _policy.Remove(key);
                _onEvicted?.Invoke(key, value);
                return true;
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _values.Clear();
                _policy.Clear();
            }
        }

        private void PutUnsafe(TKey key, TValue value)
        {
            _values[key] = value;
            _policy.Touch(key);

            while (_capacity > 0 && _values.Count > _capacity && _policy.TrySelectEvictionCandidate(out var evictKey))
            {
                if (_values.TryGetValue(evictKey, out var evictValue))
                {
                    _values.Remove(evictKey);
                    _policy.Remove(evictKey);
                    _onEvicted?.Invoke(evictKey, evictValue);
                }
                else
                {
                    _policy.Remove(evictKey);
                }
            }
        }
    }
}
