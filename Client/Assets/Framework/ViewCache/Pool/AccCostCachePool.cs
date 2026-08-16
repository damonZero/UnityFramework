//**************************************************************************************
//Create By fred on 2019/04/28
//
//@Description Least accumulative cost cache
//**************************************************************************************
using System;
using System.Collections.Generic;

namespace Framework.ViewCache
{
    public class AccCostCachePool<TKey, TValue>
    {
        #region protected 成员
        protected readonly struct CostKey
        {
            internal CostKey(TKey key, ulong cost)
            {
                this.key = key;
                this.cost = cost;
            }

            internal readonly TKey key;
            internal readonly ulong cost;
        }

        protected sealed class Compare : IComparer<CostKey>
        {
            int IComparer<CostKey>.Compare(CostKey x, CostKey y)
            {
                if (x.cost < y.cost) return -1;
                if (x.cost > y.cost) return 1;

                return 0;
            }
        }

        protected readonly int _poolCapacity;
        protected readonly int _replicaLimit;
        protected readonly SortedDictionary<CostKey, Stack<TValue>> _pool;

        protected readonly Dictionary<TKey, ulong> _accCosts; // 记录各key对应的累积cost值

        #endregion

        public Action<TKey, IEnumerable<TValue>> CacheEvict { get; set; }

        public int ValueCount => _pool.Count;

        public AccCostCachePool(int capacity, int replicaLimit)
        {
            _poolCapacity = capacity;
            _replicaLimit = replicaLimit;
            _pool = new SortedDictionary<CostKey, Stack<TValue>>(new Compare());
            _accCosts = new Dictionary<TKey, ulong>();
        }

        /// <summary>
        /// 更新数据的累积cost，并尝试加入缓存
        /// </summary>
        /// <param name="key">数据的键</param>
        /// <param name="value">数据的值</param>
        /// <param name="cost">数据本次增加的cost</param>
        /// <returns>返回值表示是否真正加入了缓存</returns>
        public bool TryPut(TKey key, TValue value, ulong cost)
        {
            ulong newAccCost;

            // 1.更新累积cost
            if (_accCosts.TryGetValue(key, out ulong accCost))
            {
                newAccCost = cost + accCost;
                _accCosts[key] = newAccCost;
            }
            else
            {
                newAccCost = cost;
                _accCosts.Add(key, newAccCost);
            }

            // 2.移除旧的缓存值
            // SortedDictionary 按键升序遍历，首个 key 即最小 cost。此前直接读枚举器
            // Current（未 MoveNext）拿到 default，池满时按 default 剔除会抛 KeyNotFoundException。
            CostKey min = default;
            if (_pool.Count > 0)
            {
                foreach (var entry in _pool)
                {
                    min = entry.Key;
                    break;
                }
            }
            Stack<TValue> values = null;
            if (accCost >= min.cost)
            {
                CostKey costKey = new CostKey(key, accCost);
                if (_pool.TryGetValue(costKey, out values))
                {
                    _pool.Remove(costKey);
                }
            }

            // 3.添加新的缓存值
            bool shouldAdd = false;
            bool shouldEvict = false;
            if (ValueCount < _poolCapacity || key.Equals(min.key))
            {
                shouldAdd = true;
            }
            else if (newAccCost > min.cost)
            {
                shouldAdd = true;
                // 如果数量达到上限，就要剔除最小的
                if (ValueCount >= _poolCapacity)
                {
                    shouldEvict = true;
                }
            }

            if (shouldAdd)
            {
                CostKey newKey = new CostKey(key, newAccCost);
                if (values == null)
                {
                    values = new Stack<TValue>();
                    values.Push(value);
                }
                if (values.Count < _replicaLimit)
                {
                    values.Push(value);
                }
                _pool.Add(newKey, values);
            }

            if (shouldEvict)
            {
                var minValue = _pool[min];
                _pool.Remove(min);
                // 事件回调放到最后，能最大限度地避其带来的副作用
                CacheEvict?.Invoke(min.key, minValue);
            }

            return shouldAdd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(TKey key, out TValue value)
        {
            if (_accCosts.TryGetValue(key, out ulong cost))
            {
                CostKey costKey = new CostKey(key, cost);
                if (_pool.TryGetValue(costKey, out Stack<TValue> values))
                {
                    if (values.Count > 0)
                    {
                        value = values.Pop();
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }
}
