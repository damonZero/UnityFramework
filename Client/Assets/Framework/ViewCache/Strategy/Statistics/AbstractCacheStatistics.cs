using System;
using System.Collections.Generic;

namespace Framework.ViewCache
{
    //FIXME by liangc:这个抽象类存在的意义是?
    public abstract class AbstractCacheStatistics : ICacheStatistics
    {
        private Dictionary<string, double> _recordDict; //FIXME by liangc:未使用

        public AbstractCacheStatistics()
        {
            _recordDict = new Dictionary<string, double>();
        }




        public abstract void BeforeTake(string key);
        public abstract void AfterTake(string key);
        public abstract long GetScore(string key);

        public void Put(string key)
        {

        }

        public virtual void Clear()
        {
        }

        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }

        ~AbstractCacheStatistics()
        {
            Clear();
        }
    }
}
