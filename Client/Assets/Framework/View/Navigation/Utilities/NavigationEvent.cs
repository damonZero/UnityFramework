//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航系统事件类
//@Description 实现事件优先级区分，执行过程中容错等机制
//**************************************************************************************

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.Pool;
namespace Framework.View.Navigation
{
    public class NavigationEvent<TParamA, TParamB>
    {
        #region public属性

        /// <summary>
        /// 是否允许异步事件，默认允许
        /// </summary>
        public bool AllowAsync { get; }

        /// <summary>
        /// 事件数量
        /// </summary>
        public int Count => _delegates.Count;

        #endregion

        #region private字段

        /// <summary>
        /// 所有注册委托（同步和异步统一存储）
        /// </summary>
        private readonly List<NavigationDelegateWrapper> _delegates = new();

        #endregion

        #region public方法


        public NavigationEvent(bool allowAsync = true)
        {
            AllowAsync = allowAsync;
        }

        /// <summary>
        /// 添加同步委托
        /// </summary>
        /// <param name="handler">委托</param>
        /// <param name="order">委托执行顺序,小的先执行</param>
        public bool Add(Action<TParamA, TParamB> handler, uint order = 999)
        {
            if (handler == null || Exists(handler))
            {
                return false;
            }

            var wrapper = NavigationFactory.Instance.Get<NavigationDelegateWrapper>();
            wrapper.syncDelegate = handler;
            wrapper.order = order;
            InsertSorted(wrapper);

            return true;
        }

        /// <summary>
        /// 添加异步委托
        /// </summary>
        /// <param name="handler">异步委托</param>
        /// <param name="order">委托执行顺序,小的先执行</param>
        public bool AddAsync(Func<TParamA, TParamB, UniTask> handler, uint order = 999)
        {
            if (!AllowAsync)
            {
                throw new Exception($"Can't Add Async Event, since {nameof(AllowAsync)} is false");
            }

            if (handler == null || ExistsAsync(handler))
            {
                return false;
            }

            var wrapper = NavigationFactory.Instance.Get<NavigationDelegateWrapper>();
            wrapper.asyncDelegate = handler;
            wrapper.order = order;
            InsertSorted(wrapper);

            return true;
        }

        /// <summary>
        /// 异步执行所有事件（同步和异步委托按order混合排序后统一执行）
        /// 注意：此方法仅适用于AllowAsync=true的情况
        /// </summary>
        /// <param name="paramA">调用参数a</param>
        /// <param name="paramB">调用参数b</param>
        public async UniTask InvokeAsync(TParamA paramA, TParamB paramB)
        {
            // 不允许异步时，必须使用Invoke，抛出异常提示调用方
            if (!AllowAsync)
            {
                throw new Exception($"Use {nameof(Invoke)} instead, since {nameof(AllowAsync)} is false");
            }

            if (_delegates.Count == 0) return;

            // 复制到临时列表避免遍历时修改
            var tempList = UnityEngine.Pool.ListPool<NavigationDelegateWrapper>.Get();
            tempList.AddRange(_delegates);

            foreach (var wrapper in tempList)
            {
                try
                {
                    if (AllowAsync && wrapper.asyncDelegate != null)
                    {
                        await wrapper.asyncDelegate.Invoke(paramA, paramB);
                    }
                    else
                    {
                        wrapper.syncDelegate?.Invoke(paramA, paramB);
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    var eventException = NavigationException.Convert<NavigationEventException>(e);
                    NavigationExceptionMgr.AddException(null, eventException);
                }
            }

            UnityEngine.Pool.ListPool<NavigationDelegateWrapper>.Release(tempList);
        }

        /// <summary>
        /// 同步执行事件
        /// 注意：此方法仅适用于AllowAsync=false的情况
        /// </summary>
        /// <param name="paramA">调用参数a</param>
        /// <param name="paramB">调用参数b</param>
        public void Invoke(TParamA paramA, TParamB paramB)
        {
            // 允许异步时，必须调用InvokeAsync，抛出异常提示调用方
            if (AllowAsync)
            {
                throw new Exception($"Use {nameof(InvokeAsync)} instead, since {nameof(AllowAsync)} is true");
            }

            if (_delegates.Count == 0) return;

            var tempList = UnityEngine.Pool.ListPool<NavigationDelegateWrapper>.Get();
            tempList.AddRange(_delegates);

            foreach (var wrapper in tempList)
            {
                if (wrapper.IsAsync) continue; // 跳过异步委托

                try
                {
                    wrapper.syncDelegate?.Invoke(paramA, paramB);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    var eventException = NavigationException.Convert<NavigationEventException>(e);
                    NavigationExceptionMgr.AddException(null, eventException);
                }
            }

            UnityEngine.Pool.ListPool<NavigationDelegateWrapper>.Release(tempList);
        }

        /// <summary>
        /// 检查同步委托是否存在
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public bool Exists(Action<TParamA, TParamB> handler)
        {
            return _delegates.Exists(d => d.syncDelegate == handler);
        }

        /// <summary>
        /// 检查异步委托是否存在
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public bool ExistsAsync(Func<TParamA, TParamB, UniTask> handler)
        {
            return _delegates.Exists(d => d.asyncDelegate == handler);
        }

        /// <summary>
        /// 移除同步委托
        /// </summary>
        /// <param name="handlerToRemove"></param>
        public void RemoveAll(Action<TParamA, TParamB> handlerToRemove)
        {
            for (int i = _delegates.Count - 1; i >= 0; i--)
            {
                if (_delegates[i].syncDelegate == handlerToRemove)
                {
                    NavigationFactory.Instance.Recycle(_delegates[i]);
                    _delegates.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 移除同步委托
        /// </summary>
        /// <param name="handlerToRemove"></param>
        public void Remove(Action<TParamA, TParamB> handlerToRemove)
        {
            for (var i = 0; i < _delegates.Count; i++)
            {
                if (handlerToRemove == _delegates[i].syncDelegate)
                {
                    NavigationFactory.Instance.Recycle(_delegates[i]);
                    _delegates.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 移除异步委托
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveAsync(Func<TParamA, TParamB, UniTask> handler)
        {
            for (int i = _delegates.Count - 1; i >= 0; i--)
            {
                if (_delegates[i].asyncDelegate == handler)
                {
                    NavigationFactory.Instance.Recycle(_delegates[i]);
                    _delegates.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 清理事件
        /// </summary>
        public void Clear()
        {
            foreach (var wrapper in _delegates)
            {
                NavigationFactory.Instance.Recycle(wrapper);
            }
            _delegates.Clear();
        }

        #endregion

        #region wrapper


        /// <summary>
        /// 统一委托包装类（同时支持同步和异步）
        /// </summary>
        private class NavigationDelegateWrapper
        {
            public Action<TParamA, TParamB> syncDelegate;      // 同步委托
            public Func<TParamA, TParamB, UniTask> asyncDelegate; // 异步委托
            public uint order;
            public bool IsAsync => asyncDelegate != null;

            public void Reset()
            {
                syncDelegate = null;
                asyncDelegate = null;
                order = 0;
            }
        }

        /// <summary>
        /// 插入排序：找到正确位置插入，保持列表有序
        /// </summary>
        private void InsertSorted(NavigationDelegateWrapper wrapper)
        {
            var index = _delegates.BinarySearch(wrapper, WrapperComparer.instance);
            if (index < 0) index = ~index;
            _delegates.Insert(index, wrapper);
        }

        /// <summary>
        /// Wrapper比较器，用于二分查找
        /// </summary>
        private class WrapperComparer : IComparer<NavigationDelegateWrapper>
        {
            public static readonly WrapperComparer instance = new();
            public int Compare(NavigationDelegateWrapper x, NavigationDelegateWrapper y)
            {
                if (x == null || y == null) return 0;
                return x.order.CompareTo(y.order);
            }
        }

        #endregion
    }
}
