using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.ViewCache
{
    public class FIFOCacheStrategy : FIFOCacheStrategy<string>
    {

    }
    /// <summary>
    /// FIFO缓存淘汰策略实现
    /// 按照先进先出原则移除最早加入的缓存项
    /// </summary>
    public class FIFOCacheStrategy<KeyT> : AbstractStrategy<KeyT>
    {
        private List<KeyT> _caches;

        public FIFOCacheStrategy()
        {
            _caches = new List<KeyT>();
        }


        public override void Clear()
        {
            // Log.Cache.Info("LRUCacheStrategy.Clear!");
            _caches.Clear();
        }

        protected override List<Type> NeedStatisticsList()
        {
            return new List<Type>()
            {
            };
        }

        protected override void Put(KeyT key)
        {
            _caches.Add(key);
        }

        public override void BeforeTake(KeyT key)
        {
            base.BeforeTake(key);

            if (_caches.Contains(key))
                return;

            //说明是新增，那么需要检查是否超过预期了
            var need = _caches.Count + 1;
            var offset = need - Capacity;
            offset = Math.Min(offset, Capacity);
            if (offset <= 0)
                return;

            for (var i = 0; i < offset; i++)
            {
                Eviction?.Invoke(_caches[i]);
            }

            _caches.RemoveRange(0, offset);
        }

        protected override void Take(KeyT key)
        {
            if (_caches.Remove(key)) return;
        }

        public override void Update(float elapsed = 0)
        {
            var count = _caches.Count;
            var offset = count - Capacity;

            if (offset <= 0)
                return;

            for (var i = 0; i < offset; i++)
            {
                Eviction?.Invoke(_caches[i]);
            }

            _caches.RemoveRange(0, offset);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach(var cacheName in _caches)
            {
                sb.Append($"{cacheName}\t");
            }
            return $"FIFOCacheStrategy: {sb.ToString()}";
        }
    }
}
