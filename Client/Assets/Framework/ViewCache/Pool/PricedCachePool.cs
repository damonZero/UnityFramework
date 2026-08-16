//**************************************************************************************
//Create By fred on 2019/04/28
//
//@Description 按价格排序的缓存池
//
//  1、缓存池中每个Key下能缓存多个Value，以栈的方式存储，每次获取时拿到的是最后放入的Value
//  2、缓存池中每个Key对应一个价格Price，Price大的优先驻留缓存池，即超过上限时剔除Price最小的Key
//**************************************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Framework.ViewCache
{
    public class PricedCachePool<TKey, TValue> : IEnumerable<KeyValuePair<TKey, Stack<TValue>>>, IEnumerable
    {
        #region protected 成员

        internal protected struct PricedKey
        {
            internal readonly TKey key;
            internal ulong price;

            internal PricedKey(TKey key, ulong price)
            {
                this.key = key;
                this.price = price;
            }
        }

        protected sealed class Compare : IComparer<PricedKey>
        {
            int IComparer<PricedKey>.Compare(PricedKey x, PricedKey y)
            {
                if (x.price < y.price) return -1;
                if (x.price > y.price) return 1;

                return Comparer<TKey>.Default.Compare(x.key, y.key);
            }
        }

        protected int _poolCapacity;
        protected readonly int _replicaLimit;
        protected readonly SortedDictionary<PricedKey, Stack<TValue>> _pool;
        protected readonly Dictionary<TKey, ulong> _prices;

        #endregion

        public Action<TKey, IEnumerable<TValue>, ulong> CacheEvict { get; set; }

        public int Count => _pool.Count;

        public int Capacity
        {
            get => _poolCapacity;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Capacity should be positive!");

                UpdateCapacity(value);
            }
        }

        public PricedCachePool(int capacity, int replicaLimit)
        {
            _poolCapacity = capacity;
            _replicaLimit = replicaLimit;
            _pool = new SortedDictionary<PricedKey, Stack<TValue>>(new Compare());
            _prices = new Dictionary<TKey, ulong>();
        }

        /// <summary>
        /// 尝试将要缓存的值放入缓存池中（会进行缓存数量、价格等检查）
        /// </summary>
        /// <param name="key">要缓存的键</param>
        /// <param name="value">要缓存的一个值</param>
        /// <param name="newPrice">键对应的最新价格</param>
        /// <returns>是否真正将值放入了缓冲池</returns>
        public bool TryPut(TKey key, TValue value, ulong newPrice)
        {
            if (_poolCapacity == 0)
            {
                return false;
            }

            if (_prices.TryGetValue(key, out var oldPrice))
            {
                var pricedKey = new PricedKey(key, oldPrice);
                var values = _pool[pricedKey];

                if (oldPrice != newPrice)
                {
                    _prices[key] = newPrice;
                    _pool.Remove(pricedKey);

                    pricedKey.price = newPrice;
                    _pool.Add(pricedKey, values);
                }

                if (values.Count < _replicaLimit)
                {
                    values.Push(value);
                    return true;
                }

                return false;
            }


            var addKey = new PricedKey(key, newPrice);
            var addValue = new Stack<TValue>();
            addValue.Push(value);
            _pool.Add(addKey, addValue);
            _prices.Add(key, newPrice);

            return true;
        }

        /// <summary>
        /// 尝试从缓存池中拿走一个值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryTake(TKey key, out TValue value)
        {
            if (_prices.TryGetValue(key, out var price))
            {
                var pricedKey = new PricedKey(key, price);
                var values = _pool[pricedKey];

                if (values.Count > 0)
                {
                    value = values.Pop();
                    return true;
                }
            }

            value = default;
            return false;
        }

        public void Clear()
        {
            _pool.Clear();
            _prices.Clear();
        }

        public void UpdateCapacity(int newCapacity)
        {
            _poolCapacity = newCapacity;

            while (_pool.Count > _poolCapacity)
            {
                var min = _pool.First();
                var evictKey = min.Key;
                var evictValue = min.Value;

                _pool.Remove(evictKey);
                _prices.Remove(evictKey.key);
                CacheEvict?.Invoke(evictKey.key, evictValue, evictKey.price);
            }
        }

        #region enumerate

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, Stack<TValue>>>
        {
            private SortedDictionary<PricedKey, Stack<TValue>>.Enumerator _enumerator;

            internal Enumerator(SortedDictionary<PricedKey, Stack<TValue>> dictionary)
            {
                _enumerator = dictionary.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            internal void Reset()
            {
                throw new NotImplementedException();
                // _enumerator.Reset();
            }

            void IEnumerator.Reset()
            {
                throw new NotImplementedException();
                // _enumerator.Reset();
            }

            public KeyValuePair<TKey, Stack<TValue>> Current
            {
                get
                {
                    var item = _enumerator.Current;
                    return new KeyValuePair<TKey, Stack<TValue>>(item.Key.key, item.Value);
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }

        IEnumerator<KeyValuePair<TKey, Stack<TValue>>> IEnumerable<KeyValuePair<TKey, Stack<TValue>>>.GetEnumerator()
        {
            return new Enumerator(_pool);
        }

        public IEnumerator GetEnumerator()
        {
            return new Enumerator(_pool);
        }

        #endregion
    }
}