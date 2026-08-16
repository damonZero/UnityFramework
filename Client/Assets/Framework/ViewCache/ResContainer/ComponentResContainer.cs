
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

using Framework.Log;

namespace Framework.ViewCache
{
    /// <summary>
    /// 缓存指定Component的组件资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ComponentResContainer<T> : AbstractResContainer<T> where T : Component
    {
        public ComponentResContainer(Transform cacheRootParent) : base(cacheRootParent)
        {
        }

        protected override Transform GetTransform(T instance)
        {
            return instance.transform;
        }

        protected override T Instance(string assetName)
        {
            return null;
        }


        public override async UniTask<T> InstanceAsync(string assetName)
        {
            GameLog.Debug($"缓存 中没有，触发加载 {assetName} ", module: CacheFactory.CACHER_LOG_DEBUG);

            var resName = assetName + ".prefab";
            var instance = await CacheDependencies.InstantiateGameObject(resName, _cacheRoot);
            if (instance == null) return null;

            var component = instance.GetComponent<T>();
            if (component == null)
            {
                GameLog.Error($"实例化对象上未找到组件 {typeof(T).Name}: {assetName}", module: CacheFactory.CACHER_LOG_DEBUG);
                Object.Destroy(instance);
                return null;
            }
            return component;
        }

        public override void DestroyObj(T component)
        {
            if (component == null) return;
            // 修复：InstanceAsync 实例化的是整个 prefab（GameObject），这里只 Destroy 组件会泄漏宿主 GameObject 及其子节点。
            Object.Destroy(component.gameObject);
        }
    }
}
