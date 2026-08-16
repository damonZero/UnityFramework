//**************************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description 索引字典池
//**************************************************************************************

using System.Collections.Generic;

namespace Framework.Touch
{
    public static class IndexDictionaryPool
    {
        // 索引字典缓存堆栈
        private static readonly Stack<Dictionary<object, int>> _cacheStack = new Stack<Dictionary<object, int>>();

        /// <summary>
        /// 获取一个索引字典
        /// </summary>
        /// <returns>一个索引字典</returns>
        public static Dictionary<object, int> Get()
        {
            if (_cacheStack.Count > 0)
            {
                return _cacheStack.Pop();
            }

            return new Dictionary<object, int>();
        }

        /// <summary>
        /// 回收一个索引字典
        /// </summary>
        /// <param name="toRelease">要回收的字典</param>
        public static void Release(Dictionary<object, int> toRelease)
        {
            if (toRelease == null) return;

            toRelease.Clear();
            _cacheStack.Push(toRelease);
        }
    }
}


