using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.ViewCache
{
    /// <summary>
    /// 依赖的外部方法，需要外部进行依赖注入
    /// </summary>
    public static class CacheDependencies
    {
        public delegate UniTask<T> InstantiateDelegate<T>(string asset, Transform parent) where T : UnityEngine.Object;

        /// <summary>
        /// 异步实例化资源GameObject
        /// </summary>
        public static InstantiateDelegate<GameObject> InstantiateGameObject { get; set; }

        /// <summary>
        /// 获取资源内存占用大小
        /// </summary>
        public static Func<string, int> GetMemory { get; set; }
    }
}
