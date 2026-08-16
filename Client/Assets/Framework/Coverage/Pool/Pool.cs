//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 对象池定义
//**************************************************************************************

using System.Collections.Generic;

namespace Framework.Coverage
{
    public interface IPool
    {
        void OnTake();
        void OnCache();
    }

    /// <summary>
    /// 对象池基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pool<T>:IPool
        where T : class, IPool,new()
    {
        private static readonly Queue<T> _pool = new Queue<T>();

        public static T Take()
        {
            T item;
            if (_pool.Count > 0)
                item = _pool.Dequeue();
            else
                item = new T();
            item.OnTake();
            return item;
        }

        public static void Cache(T item)
        {
            item.OnCache();
            _pool.Enqueue(item);
        }

        public virtual void OnTake()
        {

        }

        public virtual void OnCache()
        {

        }
    }
}
