//**************************************************************************************
//Create By liangc on 2022/4/25
//@Description 对象缓存管理：将使用过（开启过）的对象按一定规则驻留在内存中，以节约下次开启时再次加载的开销
//
//  主要包括两个缓冲池：
//      1、存在一个主缓存池，目的是缓存用的整个游戏期间加载开销最多、内存收益高的那些对象
//          简单来说目的就是：消耗一定内存去缓存某种对象，能减少的总加载开销最多
//      2、存在一个次级缓存池，目的是缓存刚刚使用过的对象
//          因为刚使用过的对象再次开启的几率通常是比较高的

//  缓存放置流程简介：
//      1、关闭某个对象A后，将A放入次级缓存池（一个LRU缓存）
//      2、若次级缓存池达到上限，则移出最早放入的缓存对象B（可能是多个B的副本）
//      3、根据从统计数据中得到的对象B的收益，尝试将B放入主缓存池（一个PricedCachePool）
//**************************************************************************************

using Framework.Log;
using System;
using System.Text;
using System.Collections.Generic;
using Framework.ViewCache;
namespace Framework.View
{
    /// <summary>
    /// 场景缓存策略
    /// </summary>
    public class SceneCacheStrategy : AbstractStrategy<string>
    {
        //主缓存池，根据统计‘string’，价值决定驻留‘string’
        private PricedCachePool<string, string> _primaryPool;

        //次级缓存池，用于缓存少量的最近关闭的‘string’
        // private MemoryCache<string, Stack<string>> _secondaryCache;

        //主次缓存比例
        private readonly float _primaryRatio = 0.7f;

        protected override void OnCapacityChanged(int capacity, int oldCapacity)
        {
            // Log.Cache.Info($"SceneCacheStrategy.OnCapacityChanged {oldCapacity} >> {capacity}");
            // GetCapacity(capacity, out var primaryCapacity, out var secondaryCapacity);
            _primaryPool.Capacity = capacity;
            // _secondaryCache.Capacity = secondaryCapacity;
        }

        //FIXME by liangc:GetCapacity InitCapacity?
        private void GetCapacity(int capacity, out int primaryCapacity, out int secondaryCapacity)
        {
            primaryCapacity = (int)(capacity * _primaryRatio);
            secondaryCapacity = capacity - primaryCapacity;
        }

        private int ReplicaLimit => 1;

        public SceneCacheStrategy()
        {
            // GetCapacity(DEFAULT_CAPACITY, out var primaryCapacity, out var secondaryCapacity);
            _primaryPool = new PricedCachePool<string, string>(DEFAULT_CAPACITY, ReplicaLimit)
            {
                CacheEvict = OnPrimaryEvict
            };

            // _secondaryCache = new MemoryCache<string, Stack<string>>(secondaryCapacity);
            // _secondaryCache.SetPolicy(typeof(LruEvictionPolicy<,>));
            // _secondaryCache.Policy.OnEvict = OnSecondaryEvict;
        }

        //主缓存对象删除回调函数
        protected virtual void OnPrimaryEvict(string key, IEnumerable<string> value, ulong price)
        {
            foreach (var cache in value)
            {
                Eviction?.Invoke(cache);
            }
        }

        // 未使用函数，直接注释掉
        //次级缓存对象被删除的回调函数：尝试将缓存包含的对象放入主缓存池
        // protected virtual void OnSecondaryEvict(IManagedCache<string,
        //     Stack<string>> source, string key, Stack<string> values, EvictionReason reason)
        // {
            // DebugLog.Log(CacheUtil.CACHER_LOG_DEBUG, $"OnSecondaryEvict : {key}, 111 reason = {reason}");
            // if (reason == EvictionReason.Removal) return;
            // DebugLog.Log(CacheUtil.CACHER_LOG_DEBUG, $"OnSecondaryEvict : {key}, 222 reason = {reason}");
            //
            // if (values.Count == 0) return;
            //
            // bool tryPut = true;
            // long profit = GetProfit(key);
            // foreach (var value in values)
            // {
            //     if (tryPut && _primaryPool.TryPut(key, value, (ulong)profit))
            //     {
            //     }
            //     else
            //     {
            //         tryPut = false;
            //         Eviction?.Invoke(value);
            //     }
            // }
        // }

        private long GetProfit(string key)
        {
            var sceneName = key;
            return GetStatistics<MemoryStatistics>(sceneName) * GetStatistics<LoadTimeStatistics>(sceneName);
        }

        public override void Clear()
        {
            // Log.Cache.Info("SceneCacheStrategy.Clear!");
            _primaryPool.Clear();
            // _secondaryCache.ForEach(s => _secondaryCache[s].Clear() );
        }

        protected override List<Type> NeedStatisticsList()
        {
            return new List<Type>()
            {
                typeof(MemoryStatistics),
                typeof(LoadTimeStatistics),
            };
        }

        protected override void Put(string key)
        {
            var price = (ulong)GetProfit(key);
            GameLog.Debug($"key[{key}], price: {price}", module: "Framework.ViewCache");
            if (!_primaryPool.TryPut(key, key, price))
            {
                Eviction?.Invoke(key);
            }
        }

        protected override void Take(string key)
        {
            _primaryPool.TryTake(key, out _);
        }

        public override void AfterTake(string key)
        {
            var sceneName = key;
            foreach (var statistics in _statisticsList)
            {
                statistics.AfterTake(sceneName);
            }

            Take(key);
        }


        public override void AfterPut(string key)
        {
            var sceneName = key;
            foreach (var statistics in _statisticsList)
            {
                statistics.Put(sceneName);
            }

            Put(key);
        }

        public override void Update(float elapsed = 0)
        {
            base.Update(elapsed);
            _primaryPool.UpdateCapacity(Capacity);
        }

        public override void BeforeTake(string key)
        {
            var sceneName = key;
            base.BeforeTake(sceneName);
        }

        private const int DEFAULT_CAPACITY = 20;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"_primaryPool: Count-{_primaryPool.Count}, Capacity-{_primaryPool.Capacity}\t");
            return $"SceneCacheStrategy: {sb}";
        }

    }
}
