using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Framework.Log;

namespace Framework.ViewCache
{
    public abstract class AbstractCommonContainer<KeyT, T> : ICacheResContainer<KeyT, T>
    {
        /// <summary>
        /// 创建资源
        /// </summary>
        /// <param name="assetKey"></param>
        /// <returns></returns>
        protected abstract T Instance(KeyT assetKey);

        /// <summary>
        /// 异步创建资源
        /// </summary>
        /// <param name="assetKey"></param>
        /// <returns></returns>
        public abstract UniTask<T> InstanceAsync(KeyT assetKey);

        /// <summary>
        /// 销毁资源
        /// </summary>
        /// <param name="instance"></param>
        public abstract void DestroyObj(T instance);

        /// <summary>
        /// 缓存字典
        /// </summary>
        protected readonly Dictionary<KeyT, List<T>> _cacheDict = new();



        /// <summary>
        /// 获取缓存数量
        /// </summary>
        /// <returns></returns>
        public virtual int GetCount()
        {
            return _cacheDict.Count;
        }


        /// <summary>
        /// 放入缓存
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="assetKey"></param>
        /// <returns></returns>
        public virtual bool Put(T instance, KeyT assetKey)
        {
            if (!_cacheDict.TryGetValue(assetKey, out var list))
            {
                list = new List<T>();
                _cacheDict.Add(assetKey, list);
            }

            list.Add(instance);
            OnPutInContainer(instance);
            return true;
        }


        /// <summary>
        /// 放入缓存后调用(可选)
        /// </summary>
        /// <param name="instance"></param>
        protected virtual void OnPutInContainer(T instance)
        {
        }

        /// <summary>
        /// 同步从缓存中获取资源
        /// </summary>
        /// <param name="assetKey"></param>
        /// <returns></returns>
        public virtual T Take(KeyT assetKey)
        {
            if (!_cacheDict.TryGetValue(assetKey, out var list))
                return Instance(assetKey);

            var count = list.Count;
            if (count <= 0)
                return Instance(assetKey);

            GameLog.Debug($"从缓存中获取 {assetKey} 成功", module: CacheFactory.CACHER_LOG_DEBUG);
            var ret = list[count - 1];
            list.RemoveAt(count - 1);
            return ret;
        }

        /// <summary>
        /// 尝试从缓存中获取资源, 如果缓存中没有, 则返回false
        /// </summary>
        /// <param name="assetKey"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public bool TryGet(KeyT assetKey, out T instance)
        {
            instance = default;
            if (!_cacheDict.TryGetValue(assetKey, out var list))
                return false;
            if (list.Count == 0)
                return false;
            instance = list[0];
            return true;
        }


        /// <summary>
        /// 异步从缓存中获取资源
        /// </summary>
        /// <param name="assetKey"></param>
        /// <returns></returns>
        public virtual async UniTask<T> TakeAsync(KeyT assetKey)
        {
            if (!_cacheDict.TryGetValue(assetKey, out var list))
                return await InstanceAsync(assetKey);

            var count = list.Count;
            if (count <= 0)
                return await InstanceAsync(assetKey);

            GameLog.Debug($"从缓存中获取 {assetKey} 成功", module: CacheFactory.CACHER_LOG_DEBUG);
            var ret = list[count - 1];
            list.RemoveAt(count - 1);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (count == 1)
                _cacheDict.Remove(assetKey);
#endif
            return ret;
        }


        /// <summary>
        /// 从缓存中销毁资源
        /// </summary>
        /// <param name="assetKey"></param>
        public virtual void Destroy(KeyT assetKey)
        {
            GameLog.Debug($"Destroy GameObject: {assetKey}", module: CacheFactory.CACHER_LOG_DEBUG);
            if (_cacheDict.TryGetValue(assetKey, out var list))
            {
                var len = list.Count;
                if (len > 0)
                {
                    var go = list[len - 1];
                    list.RemoveAt(len - 1);
                    DestroyObj(go);
                    return;
                }
            }

            var str = $"当前缓存中 = [[";
            foreach (var pair in _cacheDict)
            {
                if (pair.Value.Count > 0)
                    str += pair.Key + ",\n";
            }

            str += "]]";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameLog.Error($"GameObject Destroy error [{assetKey}, \n cur{str}]", module: "Framework.ViewCache");
#endif
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public virtual void Clear()
        {
            GameLog.Debug($"{this.GetType().FullName} Clear!!!!", module: CacheFactory.CACHER_LOG_DEBUG);
            // Destroy 每次只移除一个实例，需要循环直到该 key 下的所有实例都被销毁
            foreach (var pair in _cacheDict)
            {
                while (pair.Value.Count > 0)
                    Destroy(pair.Key);
            }

            _cacheDict.Clear();
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var str = "";
            foreach (var pair in _cacheDict)
            {
                var count = pair.Value.Count;
                if (count > 0)
                    str += $"{pair.Key}={count};\n";
            }

            return str;
        }
    }
}
