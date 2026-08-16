//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航工厂类
//@Description 各种对象的创建，以及Runtime和Editor下的不同创建方式
//**************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Framework.Pool;

namespace Framework.View.Navigation
{
    public class NavigationFactory
    {
        private static NavigationFactory _instance;

        public static NavigationFactory Instance
        {
            get => _instance ??= new NavigationFactory();
            set => _instance = value;
        }

        /// <summary>
        /// 类型 → 回收委托，用于非泛型 Recycle(object) 按运行时类型归还对象。
        /// </summary>
        private readonly ConcurrentDictionary<Type, Action<object>> _recyclers = new();

        /// <summary>
        /// 类型 → Reset 方法缓存，供泛型 Get/CreatePool 的 reset 回调使用（约定对象有 public Reset()）。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, MethodInfo> ResetMethods = new();

        /// <summary>
        /// 按类型获取对象（要求有公开无参构造）。
        /// </summary>
        public virtual T Get<T>() where T : class, new()
        {
            var pool = TypePool.GetOrCreate<T>(reset: Reset<T>, maxIdle: 64);
            _recyclers.TryAdd(typeof(T), obj => pool.Return((T)obj));
            return pool.Rent();
        }

        /// <summary>
        /// 主动创建指定类型的池，工厂用于支持 private 构造等无法 new() 的类型。
        /// </summary>
        public ObjectPool<T> CreatePool<T>(Func<T> factory, int capacity = 64) where T : class
        {
            var pool = TypePool.Register<T>(factory, Reset<T>, capacity);
            _recyclers.TryAdd(typeof(T), obj => pool.Return((T)obj));
            return pool;
        }

        /// <summary>
        /// 回收对象到其所属类型的池中。
        /// </summary>
        public void Recycle(object obj)
        {
            if (obj == null) return;
            if (_recyclers.TryGetValue(obj.GetType(), out var recycler))
            {
                recycler(obj);
            }
        }

        private static void Reset<T>(T obj) where T : class
        {
            if (ResetMethods.GetOrAdd(typeof(T), t => t.GetMethod("Reset", Type.EmptyTypes)) is { } m)
            {
                m.Invoke(obj, null);
            }
        }

        /// <summary>
        /// 导航容器缓存池
        /// </summary>
        private ObjectPool<NavigateContainer> _containerPool =
            new(() => new NavigateContainer(), static c => c.Reset(), 32);

        /// <summary>
        /// 场景加载器缓存池
        /// </summary>
        private ObjectPool<NavigationSceneLoader> _sceneLoaderPool =
            new(() => new NavigationSceneLoader(), static s => s.Reset(), 16);

        // 释放缓存对象（KJ ObjectPool 无 Clear，进程级一次性释放靠引用置空 + GC）
        public void Release()
        {
            _containerPool = null;
            _sceneLoaderPool = null;
        }

        #region static API

        /// <summary>
        /// 获取加载器列表
        /// </summary>
        public static List<NavigationLoader> GetLoaderList()
        {
            return UnityEngine.Pool.ListPool<NavigationLoader>.Get();
        }

        /// <summary>
        /// 回收加载器列表
        /// </summary>
        public static void ReleaseLoaderList(List<NavigationLoader> loaderList)
        {
            UnityEngine.Pool.ListPool<NavigationLoader>.Release(loaderList);
        }

        /// <summary>
        /// 获取导航容器列表
        /// </summary>
        public static List<NavigateContainer> GetContainerList()
        {
            return UnityEngine.Pool.ListPool<NavigateContainer>.Get();
        }

        /// <summary>
        /// 回收导航容器列表
        /// </summary>
        public static void ReleaseContainerList(List<NavigateContainer> containerList)
        {
            UnityEngine.Pool.ListPool<NavigateContainer>.Release(containerList);
        }

        #endregion
    }
}
