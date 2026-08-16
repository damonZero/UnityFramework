using System;

namespace Framework.ViewCache
{

    /// <summary>
    ///
    /// </summary>
    public interface ICacheStatistics : IDisposable
    {
        void BeforeTake(string key);
        void AfterTake(string key);
        void Put(string key);
        long GetScore(string key);
        void Clear();
    }
}
