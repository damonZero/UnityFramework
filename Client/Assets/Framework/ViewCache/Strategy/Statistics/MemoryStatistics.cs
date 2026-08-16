using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Framework.ViewCache
{
    public class MemoryStatistics : AbstractCacheStatistics
    {
        //FIXME by liangc:为啥这边用接口,'LoadTimeStatistics'用字典呢
        protected readonly IDictionary<string, long> _memProfits;
        protected readonly IDictionary<string, TimeMemory> _beforeMemTime;

        /// <summary>
        /// 内存计算比例（内存值 = 动态内存差值*比例 + 静态内存值*(1-比例)）
        /// </summary>
        protected float MemRatio
        {
            get => _memRatio;
            set
            {
                if (value > 1)
                    _memRatio = 1;
                else if (value < 0)
                    _memRatio = 0;
                else
                    _memRatio = value;
            }
        }

        private float _memRatio = 0.5f;

        /// <summary>
        /// 时间内存缓存
        /// </summary>
        protected struct TimeMemory
        {
            public float time;
            public long memory;
        }

        public MemoryStatistics()
        {
            _beforeMemTime = new Dictionary<string, TimeMemory>();
            _memProfits = new Dictionary<string, long>();
        }

        public override void BeforeTake(string key)
        {
            if (_memProfits.TryGetValue(key, out _))
            {
                return;
            }

            _beforeMemTime[key] = new TimeMemory
            {
                memory = Profiler.usedHeapSizeLong,
                time = Time.realtimeSinceStartup
            };
        }

        /// <summary>
        /// 打开后回调
        /// </summary>
        /// <param name="key">关键字</param>
        public override void AfterTake(string key)
        {
            if (_memProfits.TryGetValue(key, out _))
            {
                return;
            }

            if (!_beforeMemTime.TryGetValue(key, out var cache))
            {
                _memProfits[key] = 0;
                return;
            }

            _beforeMemTime.Remove(key);

            //收益值计算
            var consumeTime = Time.realtimeSinceStartup - cache.time;
            var dynamicMem = Profiler.usedHeapSizeLong - cache.memory;
            //Editor下未统计，获取值为0
            // var staticMem  = AssetBundleIndex.HasBundleIndex(key)
            //     ? AssetBundleIndex.GetMemory(key) : AssetBundleIndex.GetMemory(key + ".unity");

            var staticMem = CacheDependencies.GetMemory != null ? CacheDependencies.GetMemory(key) : 0;

            _memProfits[key] = GetMemoryProfit(dynamicMem, staticMem, consumeTime);
        }

        public override long GetScore(string key)
        {
            if (!_memProfits.TryGetValue(key, out var value))
            {
                value = 0;
            }

            return value;
        }


        /// <summary>
        /// 获取内存收益
        /// </summary>
        /// <param name="dynamicMem">动态内存(字节)</param>
        /// <param name="staticMem">静态内存(字节)</param>
        /// <param name="time">耗时</param>
        /// <returns></returns>
        protected virtual uint GetMemoryProfit(long dynamicMem, int staticMem, float time)
        {
            //内存值(KB) = 动态内存差值*比例 + 静态内存值*(1-比例)
            float memTmp = (dynamicMem * MemRatio + staticMem * (1 - MemRatio)) / 1024;
            // memTmp 可能为 0（GetMemory 默认返回 0 或 Editor 下未统计），此时无内存收益，返回 0 避免除零
            if (memTmp <= 0)
                return 0;
            //时间消耗放大1000000倍,避免因时间太小,计算出的收益值太小
            return (uint) (time * 1000000 / memTmp);
        }

        public override string ToString()
        {
            var str = $"\n{GetType().FullName} (\n ";
            foreach (var memProfit in _memProfits)
            {
                str += $"key = {memProfit.Key}, value = {memProfit.Value} \n";
            }
            return str + ")\n";
        }
    }
}
