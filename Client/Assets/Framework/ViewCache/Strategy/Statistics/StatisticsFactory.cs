using System;
using System.Collections.Generic;

namespace Framework.ViewCache
{
    /// <summary>
    /// 这个模块存在的原因是 有些Statistics永远只存在一份，通过这个模块来管理单例的Statistics
    /// </summary>
    public static class StatisticsFactory
    {
        /// <summary>
        /// 如果是单例的分析类，需要在这边进行注册一下 FIXME by liangc:这里没看懂
        /// </summary>
        private static Dictionary<Type, ICacheStatistics> _staticContainer =
            new Dictionary<Type, ICacheStatistics>()
            {
                {
                    typeof(MemoryStatistics), new MemoryStatistics()
                }
            };

        public static ICacheStatistics Get<T>()
        {
            var type = typeof(T);
            return Get(type);
        }

        public static ICacheStatistics Get(Type type)
        {
            if (_staticContainer.TryGetValue(type, out var value))
            {
                return value;
            }

            return (ICacheStatistics) Activator.CreateInstance(type);
        }
    }
}
