using System;
using Cysharp.Threading.Tasks;

namespace Framework.ViewCache
{
    /// <summary>
    /// 缓存资源容器，管理资源的加载、卸载、保存等
    /// </summary>
    /// <typeparam name="KeyT"></typeparam>
    public interface ICacheResContainer<KeyT, T>
    {
        //将资源加入缓存中
        bool Put(T instance, KeyT assetKey);

        //直接获取缓存资源,没有则返回null
        T Take(KeyT assetKey);

        //是否存在资源
        bool TryGet(KeyT assetKey, out T form);

        //获取缓存资源，没有则Instance
        UniTask<T> TakeAsync(KeyT assetKey);

        //初始化资源
        UniTask<T> InstanceAsync(KeyT assetKey);

        //删除缓存资源
        void Destroy(KeyT assetKey);

        //直接销毁资源
        void DestroyObj(T form);

        //清理
        void Clear();

        //返回总大小
        int GetCount();
    }
}
