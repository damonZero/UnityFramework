//**************************************************************************************
//Create By wensx on 2020/03/30
//
//@Description 射线检测结果列表的一个缓存池，因为射线检测很频繁，用缓存池可以很好的避免列表的重复创建与销毁
//**************************************************************************************

using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public static class RaycastResultListPool
    {
        // 射线检测结果列表缓存堆栈
        private static readonly Stack<List<RaycastResult>> _cacheStack = new Stack<List<RaycastResult>>();

        /// <summary>
        /// 获取一个射线检测结果列表
        /// </summary>
        /// <returns>一个射线检测结果列表</returns>
        public static List<RaycastResult> Get()
        {
            if (_cacheStack.Count > 0)
            {
                return _cacheStack.Pop();
            }

            return new List<RaycastResult>();
        }

        /// <summary>
        /// 回收一个射线检测结果列表
        /// </summary>
        /// <param name="toRelease">要回收的列表</param>
        public static void Release(List<RaycastResult> toRelease)
        {
            if (toRelease == null) return;

            toRelease.Clear();
            _cacheStack.Push(toRelease);
        }
    }
}
