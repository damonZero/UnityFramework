using Framework.Log;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.ViewCache
{
    public abstract class AbstractStrategy<KeyT> : IStrategy<KeyT>
    {
        protected List<ICacheStatistics> _statisticsList;

        public Action<KeyT> Eviction { get; set; }


        public virtual void Destroy(KeyT key)
        {
        }

        public abstract void Clear();

        protected abstract List<Type> NeedStatisticsList();
        protected abstract void Put(KeyT key);
        protected abstract void Take(KeyT key);

        protected virtual void OnCapacityChanged(int capacity, int oldCapacity)
        {
        }

        public virtual void Update(float elapsed = 0)
        {
        }

        public virtual void Init()
        {
            _statisticsList = new List<ICacheStatistics>();
            foreach (var type in NeedStatisticsList())
            {
                _statisticsList.Add(StatisticsFactory.Get(type));
            }
        }


        private int _capacity;

        public int Capacity
        {
            get => _capacity;
            set
            {
                var old = _capacity;
                _capacity = value;
                OnCapacityChanged(value, old);
            }
        }

        public virtual void BeforeTake(KeyT key)
        {
            foreach (var statistics in _statisticsList)
            {
                statistics.BeforeTake(key.ToString());
            }
        }

        protected long GetStatistics<T>(KeyT key) where T : ICacheStatistics
        {
            var type = typeof(T);
            return GetStatistics(type, key);
        }

        protected long GetStatistics(Type type, KeyT key)
        {
            var res = _statisticsList.Find(statistics => statistics.GetType() == type);
#if UNITY_EDITOR
            if (res == null)
                GameLog.Error($"Get Statistics《{type.FullName}》Empty!!!", module: "Framework.ViewCache");
#endif
            return res?.GetScore(key.ToString()) ?? 0;
        }

        public virtual void AfterTake(KeyT key)
        {
            foreach (var statistics in _statisticsList)
            {
                statistics.AfterTake(key.ToString());
            }

            Take(key);
        }

        public virtual void AfterPut(KeyT key)
        {
            foreach (var statistics in _statisticsList)
            {
                statistics.Put(key.ToString());
            }

            Put(key);
        }

        public override string ToString()
        {
            var str = $"\n{GetType().FullName}:[[ \n";
            _statisticsList.ForEach(statistics => str += statistics);
            str += "]]";
            return str;
        }
    }
}
