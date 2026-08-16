using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Framework.Log;

namespace Framework.ViewCache
{
    /// <summary>
    /// 简单工厂
    /// </summary>
    public static class CacheFactory
    {
        public const string CACHER_LOG_DEBUG = "Framework.ViewCache";

        /// <summary>
        /// 创建缓存
        /// 缓存默认用string作为key
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="container"></param>
        /// <param name="strategy"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Cache<T> CreateCache<T>(int capacity, ICacheResContainer<string, T> container, IStrategy<string> strategy)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            strategy = new StrategyDecorate<string>(strategy);
#endif
            var cache = new Cache<T>(capacity, container, strategy);
            return cache;
        }

        /// <summary>
        /// 创建缓存
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="container"></param>
        /// <param name="strategy"></param>
        /// <typeparam name="KeyT"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Cache<KeyT, T> CreateCache<KeyT, T>(int capacity, ICacheResContainer<KeyT, T> container, IStrategy<KeyT> strategy)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            strategy = new StrategyDecorate<KeyT>(strategy);
#endif
            var cache = new Cache<KeyT, T>(capacity, container, strategy);
            return cache;
        }
    }
}
