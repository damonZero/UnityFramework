using System.Collections.Generic;
using UnityEngine;

namespace Framework.Pool
{
    /// <summary>
    /// 实例回收策略：决定一个被 <see cref="GameObjectPool.Recycle"/> 的 idle 实例
    /// 是否应保留入库存（idle 栈），还是应直接 Destroy（容量超限 / 过期 / 常驻否决等）。
    ///
    /// 这是 §7 G2 / §8.2-④「实例库存策略化」的最小切口：把原来写死的 maxIdle 判断
    /// 抽象为可插拔策略，让「池组合缓存策略」真正落地。
    /// 注意 YAGNI（§8）：实例库存是「每 prefab 计数上限」，用本策略即可，
    /// 不强行把 keyed 的 <see cref="Framework.Cache.BoundedStore{TKey,TValue}"/> 套到实例库存上。
    /// </summary>
    public interface IInstanceRecyclePolicy
    {
        /// <summary>
        /// 返回 true 表示实例应保留入 idle 库存；false 表示应直接 Destroy。
        /// </summary>
        /// <param name="prefabPath">实例所属 prefab 路径。</param>
        /// <param name="currentIdleCount">该 prefab 当前 idle 库存数量（入栈前）。</param>
        /// <param name="instance">待回收实例。</param>
        bool ShouldRetain(string prefabPath, int currentIdleCount, GameObject instance);
    }

    /// <summary>
    /// 默认实现：容量策略。当 <paramref name="currentIdleCount"/> &lt; <paramref name="maxIdle"/> 时保留。
    /// maxIdle &lt;= 0 等价于「无上限」（保留全部），与 ObjectPool 的 capacity=0 语义一致（§3 信息项）。
    /// </summary>
    public sealed class CapacityInstancePolicy : IInstanceRecyclePolicy
    {
        private readonly int _maxIdle;

        public CapacityInstancePolicy(int maxIdle)
        {
            _maxIdle = maxIdle < 0 ? 0 : maxIdle;
        }

        public bool ShouldRetain(string prefabPath, int currentIdleCount, GameObject instance)
        {
            if (_maxIdle <= 0)
            {
                return true;
            }

            return currentIdleCount < _maxIdle;
        }
    }

    /// <summary>
    /// 常驻保护：若 <paramref name="prefabPath"/> 命中 <paramref name="persistentPaths"/> 则永远保留
    /// （借鉴 ETPro persistentPathCache 常驻保护），否则委托 <paramref name="inner"/> 决策。
    /// </summary>
    public sealed class PersistentInstancePolicy : IInstanceRecyclePolicy
    {
        private readonly HashSet<string> _persistentPaths;
        private readonly IInstanceRecyclePolicy _inner;

        public PersistentInstancePolicy(HashSet<string> persistentPaths, IInstanceRecyclePolicy inner = null)
        {
            _persistentPaths = persistentPaths ?? new HashSet<string>();
            _inner = inner ?? new CapacityInstancePolicy(int.MaxValue);
        }

        public bool ShouldRetain(string prefabPath, int currentIdleCount, GameObject instance)
        {
            if (_persistentPaths.Contains(prefabPath))
            {
                return true;
            }

            return _inner.ShouldRetain(prefabPath, currentIdleCount, instance);
        }
    }
}
