// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using Framework.Log;
using System;
using System.Collections.Generic;
using UnityEngine;

using Cysharp.Threading.Tasks;
namespace Framework.View
{
    /// <summary>
    /// View生命周期事件集合
    ///
    /// 包含各生命周期阶段的事件节点，支持持久回调和一次性回调（Once）
    ///
    /// 用法：
    ///   持久回调：node.Add(callback) / node.Remove(callback)
    ///   一次性回调：node.AddOnce(callback)  （触发一次后自动移除）
    ///   两种回调均支持带/不带 ViewBase 参数的重载
    /// </summary>
    public class ViewLifeCycleEvents
    {
        /// <summary>
        /// 生命周期事件节点
        ///
        /// 统一管理持久和一次性回调，触发时每个回调独立 try-catch
        /// </summary>
        public class EventNode
        {
            /// <summary>
            /// 回调条目
            /// </summary>
            private struct Entry
            {
                /// <summary>回调委托（Action / Action&lt;ViewBase&gt; / Func&lt;UniTask&gt; / Func&lt;ViewBase, UniTask&gt;）</summary>
                public Delegate callback;

                /// <summary>true = 一次性回调，触发后自动移除</summary>
                public bool once;

                /// <summary>true = 带 ViewBase 参数；false = 无参数</summary>
                public bool withView;

                /// <summary>true = 异步回调（返回 UniTask）</summary>
                public bool isAsync;
            }

            private List<Entry> _entries;

            // ---- 持久回调 ----

            /// <summary>
            /// 添加持久回调（带 ViewBase 参数），等同于 event +=
            /// </summary>
            public void Add(Action<ViewBase> callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = false, withView = true });
            }

            /// <summary>
            /// 添加持久回调（无参数），等同于 event +=
            /// </summary>
            public void Add(Action callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = false, withView = false });
            }

            /// <summary>
            /// 添加持久回调（带 ViewBase 参数），确保唯一性（先尝试移除已存在的相同回调）
            /// </summary>
            public void AddUnique(Action<ViewBase> callback)
            {
                Remove(callback);
                Add(callback);
            }

            /// <summary>
            /// 添加持久回调（无参数），确保唯一性（先尝试移除已存在的相同回调）
            /// </summary>
            public void AddUnique(Action callback)
            {
                Remove(callback);
                Add(callback);
            }

            /// <summary>
            /// 移除回调（带 ViewBase 参数），等同于 event -=
            /// </summary>
            public void Remove(Action<ViewBase> callback)
            {
                if (_entries == null) return;
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    if (_entries[i].callback == (Delegate)callback)
                    {
                        _entries.RemoveAt(i);
                        return;
                    }
                }
            }

            /// <summary>
            /// 移除回调（无参数），等同于 event -=
            /// </summary>
            public void Remove(Action callback)
            {
                if (_entries == null) return;
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    if (_entries[i].callback == (Delegate)callback)
                    {
                        _entries.RemoveAt(i);
                        return;
                    }
                }
            }

            // ---- 一次性回调 ----

            /// <summary>
            /// 注册一次性回调（带 ViewBase 参数），触发一次后自动移除，支持注册多个
            /// </summary>
            public void AddOnce(Action<ViewBase> callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = true, withView = true });
            }

            /// <summary>
            /// 注册一次性回调（无参数），触发一次后自动移除，支持注册多个
            /// </summary>
            public void AddOnce(Action callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = true, withView = false });
            }

            /// <summary>
            /// 注册一次性回调（带 ViewBase 参数），确保唯一性（先尝试移除已存在的相同回调）
            /// </summary>
            public void AddOnceUnique(Action<ViewBase> callback)
            {
                Remove(callback);
                AddOnce(callback);
            }

            /// <summary>
            /// 注册一次性回调（无参数），确保唯一性（先尝试移除已存在的相同回调）
            /// </summary>
            public void AddOnceUnique(Action callback)
            {
                Remove(callback);
                AddOnce(callback);
            }

            // ---- 异步回调 ----

            /// <summary>
            /// 添加持久异步回调（带 ViewBase 参数）
            /// </summary>
            public void Add(Func<ViewBase, UniTask> callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = false, withView = true, isAsync = true });
            }

            /// <summary>
            /// 添加持久异步回调（无参数）
            /// </summary>
            public void Add(Func<UniTask> callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = false, withView = false, isAsync = true });
            }

            /// <summary>
            /// 移除异步回调（带 ViewBase 参数）
            /// </summary>
            public void Remove(Func<ViewBase, UniTask> callback)
            {
                if (_entries == null) return;
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    if (_entries[i].callback == (Delegate)callback)
                    {
                        _entries.RemoveAt(i);
                        return;
                    }
                }
            }

            /// <summary>
            /// 移除异步回调（无参数）
            /// </summary>
            public void Remove(Func<UniTask> callback)
            {
                if (_entries == null) return;
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    if (_entries[i].callback == (Delegate)callback)
                    {
                        _entries.RemoveAt(i);
                        return;
                    }
                }
            }

            /// <summary>
            /// 注册一次性异步回调（带 ViewBase 参数），触发一次后自动移除
            /// </summary>
            public void AddOnce(Func<ViewBase, UniTask> callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = true, withView = true, isAsync = true });
            }

            /// <summary>
            /// 注册一次性异步回调（无参数），触发一次后自动移除
            /// </summary>
            public void AddOnce(Func<UniTask> callback)
            {
                if (callback == null) return;
                _entries ??= new List<Entry>();
                _entries.Add(new Entry { callback = callback, once = true, withView = false, isAsync = true });
            }

            // ---- 触发 ----

            /// <summary>
            /// 触发所有回调：
            ///   - 按注册顺序执行
            ///   - 每个回调独立 try-catch，单个异常不影响其余
            ///   - 一次性回调全部触发完毕后自动移除
            /// </summary>
            public void Invoke(ViewBase view)
            {
                if (_entries == null || _entries.Count == 0) return;

                // 缓存一份当前的 entries 到 pooledList 进行遍历，防止在回调过程中发生移除/添加导致集合被修改引发异常
                var pooledList = UnityEngine.Pool.ListPool<Entry>.Get();
                if (pooledList.Capacity < _entries.Count)
                {
                    pooledList.Capacity = _entries.Count;
                }
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    var entry = _entries[i];
                    pooledList.Add(entry);

                    // 提前将其中的 once 条从原始列表中移除。
                    // 这样即使在下面的执行循环中新添加了 Once 回调，它们也会安全地保留在 _entries 中，不会被误删。
                    if (entry.once) _entries.RemoveAt(i);
                }

                var count = pooledList.Count;
                // 从_entries复制到pooledList是倒序的，所以再次倒序遍历pooledList才能保证回调的执行顺序与注册顺序一致。
                for (var i = count - 1; i >= 0; i--)
                {
                    var entry = pooledList[i];
                    try
                    {
                        if (entry.withView)
                            ((Action<ViewBase>)entry.callback).Invoke(view);
                        else
                            ((Action)entry.callback).Invoke();
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle callback failed", module: "Framework.View");
                    }
                }

                UnityEngine.Pool.ListPool<Entry>.Release(pooledList);
            }

            // FIXME by fred 增加cancellationToken参数，调用方传入
            /// <summary>
            /// 触发所有回调（含异步回调）：
            ///   - 同步回调按注册顺序依次执行
            ///   - 异步回调收集后并行执行（UniTask.WhenAll）
            ///   - 每个回调独立 try-catch，单个异常不影响其余
            ///   - 一次性回调触发后自动移除
            /// </summary>
            public async UniTask InvokeAsync(ViewBase view)
            {
                if (_entries == null || _entries.Count == 0) return;

                var pooledList = UnityEngine.Pool.ListPool<Entry>.Get();
                if (pooledList.Capacity < _entries.Count)
                {
                    pooledList.Capacity = _entries.Count;
                }
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    var entry = _entries[i];
                    pooledList.Add(entry);
                    if (entry.once) _entries.RemoveAt(i);
                }

                List<UniTask> asyncTasks = null;
                var count = pooledList.Count;
                for (var i = count - 1; i >= 0; i--)
                {
                    var entry = pooledList[i];
                    try
                    {
                        if (entry.isAsync)
                        {
                            UniTask task;
                            if (entry.withView)
                                task = ((Func<ViewBase, UniTask>)entry.callback).Invoke(view);
                            else
                                task = ((Func<UniTask>)entry.callback).Invoke();
                            asyncTasks ??= UnityEngine.Pool.ListPool<UniTask>.Get();
                            asyncTasks.Add(task);
                        }
                        else
                        {
                            if (entry.withView)
                                ((Action<ViewBase>)entry.callback).Invoke(view);
                            else
                                ((Action)entry.callback).Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle callback failed", module: "Framework.View");
                    }
                }

                UnityEngine.Pool.ListPool<Entry>.Release(pooledList);

                if (asyncTasks != null)
                {
                    try
                    {
                        await UniTask.WhenAll(asyncTasks);
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle callback failed", module: "Framework.View");
                    }
                    UnityEngine.Pool.ListPool<UniTask>.Release(asyncTasks);
                }
            }
        }

        // ---- 各生命周期阶段（惰性初始化，只有首次访问时才 new EventNode）----

        private EventNode _preAwake;
        /// <summary>View初始化前（Awake之前）</summary>
        public EventNode PreAwake => _preAwake ??= new EventNode();

        private EventNode _postAwake;
        /// <summary>View初始化后（Awake之后）</summary>
        public EventNode PostAwake => _postAwake ??= new EventNode();

        private EventNode _preOpen;
        /// <summary>View打开前（Open之前）</summary>
        public EventNode PreOpen => _preOpen ??= new EventNode();

        private EventNode _postOpen;
        /// <summary>View打开后（Open之后）</summary>
        public EventNode PostOpen => _postOpen ??= new EventNode();

        private EventNode _preShow;
        /// <summary>View显示前（Show之前）</summary>
        public EventNode PreShow => _preShow ??= new EventNode();

        private EventNode _postShow;
        /// <summary>View显示后（Show之后）</summary>
        public EventNode PostShow => _postShow ??= new EventNode();

        private EventNode _preHide;
        /// <summary>View隐藏前（Hide之前）</summary>
        public EventNode PreHide => _preHide ??= new EventNode();

        private EventNode _postHide;
        /// <summary>View隐藏后（Hide之后）</summary>
        public EventNode PostHide => _postHide ??= new EventNode();

        private EventNode _preClose;
        /// <summary>View关闭前（Close之前）</summary>
        public EventNode PreClose => _preClose ??= new EventNode();

        private EventNode _postClose;
        /// <summary>View关闭后（Close之后）</summary>
        public EventNode PostClose => _postClose ??= new EventNode();

        private EventNode _preDestroy;
        /// <summary>View销毁前（Destroy之前）</summary>
        public EventNode PreDestroy => _preDestroy ??= new EventNode();

        private EventNode _postDestroy;
        /// <summary>View销毁后（Destroy之后）</summary>
        public EventNode PostDestroy => _postDestroy ??= new EventNode();
    }
}
