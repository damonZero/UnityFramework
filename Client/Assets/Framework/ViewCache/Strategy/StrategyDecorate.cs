using System;
using System.Collections.Generic;
using System.Globalization;

namespace Framework.ViewCache
{
    /// <summary>
    /// 增加一个装饰者对缓存命中进行统计，测试的时候需要查看各种策略的效果
    /// </summary>
    public class StrategyDecorate<KeyT> : IStrategy<KeyT>
    {
        private readonly IStrategy<KeyT> _strategy;
        private List<KeyT> _record;
        private int _loadTotalTime;
        private int _missCount;

        public StrategyDecorate(IStrategy<KeyT> strategy)
        {
            _strategy = strategy;
        }

        public void Destroy(KeyT key)
        {
            _strategy.Destroy(key);
        }

        public void Clear()
        {
            _strategy.Clear();
        }

        public int Capacity
        {
            get => _strategy.Capacity;
            set => _strategy.Capacity = value;
        }

        public void BeforeTake(KeyT key)
        {
            _strategy.BeforeTake(key);
            _loadTotalTime++;
            if (!_record.Remove(key))
            {
                _missCount++;
            }
        }

        public void AfterTake(KeyT key)
        {
            _strategy.AfterTake(key);
        }

        public void AfterPut(KeyT key)
        {
            _strategy.AfterPut(key);
            _record.Add(key);
        }

        public void Update(float elapsed)
        {
            _strategy.Update(elapsed);
        }

        public void Init()
        {
            _strategy.Init();
            _strategy.Eviction = s => Eviction?.Invoke(s);
            _record = new List<KeyT>();
            _loadTotalTime = 0;
            _missCount = 0;
        }

        /// <summary>
        /// 获取缓存命中
        /// </summary>
        /// <param name="loadTotalTime"></param>
        /// <returns></returns>
        public int GetCacheMiss(out int loadTotalTime)
        {
            loadTotalTime = _loadTotalTime;
            return _missCount;
        }

        public string GetScoreInfo()
        {
            if (_loadTotalTime == 0)
                return "nil";
            var percent = (_loadTotalTime - _missCount) * 1.0f / _loadTotalTime;
            var str =
                $"percent = {percent}, count = {_loadTotalTime}, hit = {_loadTotalTime - _missCount}";
            return str;
        }

        public Action<KeyT> Eviction { get; set; }
    }
}
