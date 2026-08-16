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
    /// 基础显示类（所有显示组件的基类）
    /// 提供组件绑定、异步delay操作支持
    /// </summary>
    public abstract class ViewObject : MonoBehaviour
    {

        #region public: 成员属性

        /// <summary>
        /// 不带后缀的资产名字
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// 是否处于正常打开状态（运行中）
        /// </summary>
        public virtual bool Running => gameObject.activeInHierarchy;

        #endregion

        #region private&protected：成员字段/属性

        private GameObject _gameObject;
        private Transform _transform;

        protected GameObject SelfGo => _gameObject ??= gameObject;
        protected Transform SelfTrans => _transform ??= transform;

        #endregion

        #region 生命周期

        protected virtual void Awake()
        {

        }


        protected virtual void OnEnable()
        {

            var components = CopyViewComponents<IViewActiveComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    try
                    {
                        component.OnViewEnable();
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }
                RecycleComponentList(components);
            }

        }

        /// <summary>
        /// 当组件被禁用时调用
        /// 负责清理异步操作的取消令牌，确保异步操作在组件禁用时能够正确取消
        /// </summary>
        protected virtual void OnDisable()
        {

            var components = CopyViewComponents<IViewActiveComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    try
                    {
                        component.OnViewDisable();
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }
                RecycleComponentList(components);
            }

        }

        /// <summary>
        /// 当组件被销毁时调用
        /// </summary>
        protected virtual void OnDestroy()
        {
            //清理持有对象
            _gameObject = null;
            _transform = null;

            // 清理组件
            var components = CopyViewComponents<IViewDestroyComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    try
                    {
                        component.OnViewDestroy();
                    }
                    catch (Exception e)
                    {
                        GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                    }
                }
                RecycleComponentList(components);
                RecycleComponentList(_viewComponents);
                _viewComponents = null;
            }

        }

        #endregion


        #region 自动绑定相关

        /// <summary>
        /// 自动绑定变量，序列化存储
        /// </summary>
        [HideInInspector] [SerializeField] public SerializationDictionary<string, VarBindData> bindData = new();

        private Dictionary<string, object> _bindCache;
        private Dictionary<string, object> BindCache => _bindCache ??= new Dictionary<string, object>();

        /// <summary>
        /// 获取自动绑定字段
        /// </summary>
        /// <param name="fieldName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T GetBindField<T>(string fieldName)
        {
            if (BindCache.TryGetValue(fieldName, out var cached) && cached != null)
            {
                if (cached is T tField) return tField;
                if (cached.GetType() == typeof(UnityEngine.Object))
                {
                    GameLog.Error($"{SelfGo.name} 获取绑定字段失败，'{fieldName}'不存在了，请检查绑定信息", module: "Framework.View");
                    GameLog.Error($"{SelfGo.name} {nameof(GetBindField)} error: missing bind field '{fieldName}' ", module: "Framework.View");
                }
                else
                {
                    GameLog.Error($"{SelfGo.name} 获取绑定字段错误，'{fieldName}'类型{cached.GetType().FullName}" +
                                   $"不是预期的{typeof(T)}，请检查绑定信息", module: "Framework.View");
                    GameLog.Error($"{SelfGo.name} {nameof(GetBindField)} error: field '{fieldName}' type mismatch." +
                                   $"Expected {typeof(T)}, but got {cached.GetType().FullName}", module: "Framework.View");
                }

                return default;
            }

            if (bindData.TryGetValue(fieldName, out var bindItem))
            {
                object result = null;

                if (bindItem.IsMultiple)
                {
                    if (!typeof(T).IsArray)
                    {
                        GameLog.Error($"{SelfGo.name} {nameof(GetBindField)} error: Field '{fieldName}' is bound as multiple objects," +
                                       $" but type {typeof(T)} is not an array type", module: "Framework.View");
                        return default;
                    }

                    var objs = bindItem.BindObjects();
                    var elementType = typeof(T).GetElementType();
                    if (elementType != null)
                    {
                        var typedArray = Array.CreateInstance(elementType, objs.Length);
                        for (var i = 0; i < objs.Length; i++)
                            typedArray.SetValue(objs[i], i);
                        result = typedArray;
                    }
                }
                else
                {
                    result = bindItem.BindObject();
                }

                BindCache[fieldName] = result;

                if (result != null)
                {
                    if (result is T tField) return tField;
                    if (result.GetType() == typeof(UnityEngine.Object))
                    {
                        GameLog.Error($"{SelfGo.name} 获取绑定字段失败，'{fieldName}'不存在了，请检查绑定信息", module: "Framework.View");
                        GameLog.Error($"{SelfGo.name} {nameof(GetBindField)} error: missing bind field '{fieldName}' ", module: "Framework.View");
                    }
                    else
                    {
                        GameLog.Error($"{SelfGo.name} 获取绑定字段错误，" +
                                       $"'{fieldName}'类型{result.GetType().FullName}不是预期的{typeof(T)}，请检查绑定信息", module: "Framework.View");
                        GameLog.Error($"{SelfGo.name} {nameof(GetBindField)} error: field '{fieldName}' type mismatch." +
                                       $"Expected {typeof(T)}, but got {result.GetType().FullName}", module: "Framework.View");
                    }

                    return default;
                }
            }

            GameLog.Error($"{SelfGo.name} cant find auto bind field: '{fieldName}'", module: "Framework.View");
            return default;
        }

        public void UpdateBinding(List<VarBindData> bindList)
        {
            bindData.Clear();
            foreach (var bindItem in bindList)
            {
                bindData.Add(bindItem.Name, bindItem);
            }
        }

        public void ClearBinding()
        {
            bindData.Clear();
            _bindCache?.Clear();
        }

        #endregion

        #region 组件功能

        private List<IViewComponent> _viewComponents;

        protected List<IViewComponent> ViewComponents {
            get
            {
                _viewComponents ??= UnityEngine.Pool.ListPool<IViewComponent>.Get();
                return _viewComponents;
            }
        }

        public T GetViewComponent<T>() where T : IViewComponent
        {
            foreach (var component in ViewComponents)
            {
                if (component is T tComponent)
                {
                    return tComponent;
                }
            }
            return default;
        }

        public void AddViewComponent(IViewComponent component)
        {
            if (!ViewComponents.Contains(component))
            {
                ViewComponents.Add(component);
            }
        }

        public void RemoveViewComponent(IViewComponent component)
        {
            if (ViewComponents.Contains(component))
            {
                ViewComponents.Remove(component);
            }
        }

        public T GetOrAddViewComponent<T>() where T : IViewComponent, new()
        {
            var existingComponent = GetViewComponent<T>();
            if (existingComponent != null)
            {
                return existingComponent;
            }

            var newComponent = new T();
            AddViewComponent(newComponent);
            return newComponent;
        }

        protected List<T> CopyViewComponents<T>() where T : IViewComponent
        {
            if (_viewComponents == null) return null;

            var copy = UnityEngine.Pool.ListPool<T>.Get();
            foreach (var t in _viewComponents)
            {
                if (t is T tComponent) copy.Add(tComponent);
            }
            return copy;
        }

        protected void RecycleComponentList<T>(List<T> components) where T : IViewComponent
        {
            UnityEngine.Pool.ListPool<T>.Release(components);
        }

        #endregion


        #region 异步延迟功能

        /// <summary>
        /// 获取异步延迟功能组件的实例
        /// 如果实例不存在，会自动创建
        /// </summary>
        protected AsyncDelayComponent AsyncDelay
        {
            get
            {
                var component = GetViewComponent<AsyncDelayComponent>();
                if (component == null)
                {
                    component = new AsyncDelayComponent(this);
                    AddViewComponent(component);
                }
                return component;
            }
        }

        /// <summary>
        /// 延迟指定帧数帧末执行
        /// </summary>
        /// <param name="delayFrameCount">要延迟的帧数，必须大于0</param>
        /// <param name="disableCancel">是否在组件禁用时取消延迟，默认为true
        /// <para>- true: 组件禁用时会取消延迟</para>
        /// <para>- false: 组件禁用时不会取消延迟，但组件销毁时仍会取消</para>
        /// </param>
        /// <param name="cancelImmediately">取消时是否立即结束，默认为false
        /// <para>- true: 取消时立即结束当前等待</para>
        /// <para>- false: 取消时等待当前帧完成后再结束</para>
        /// </param>
        /// <returns>延迟是否成功完成，如果被取消则返回false</returns>
        /// <example>
        /// <code>
        /// // 等待5帧
        /// bool completed = await DelayFrame(5);
        /// if (completed)
        /// {
        ///     // 延迟成功完成后的操作
        /// }
        /// </code>
        /// </example>
        protected UniTask<bool> DelayFrame(int delayFrameCount, bool disableCancel = true,
            bool cancelImmediately = false) =>
            AsyncDelay.DelayFrame(delayFrameCount, disableCancel, cancelImmediately);

        /// <summary>
        /// 延迟指定毫秒数后继续执行
        /// </summary>
        /// <param name="millisecondsDelay">要延迟的毫秒数，必须大于0</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放，默认为false
        /// <para>- true: 使用真实时间计时，不受Time.timeScale影响</para>
        /// <para>- false: 使用游戏时间计时，受Time.timeScale影响</para>
        /// </param>
        /// <param name="disableCancel">是否在组件禁用时取消延迟，默认为true
        /// <para>- true: 组件禁用时会取消延迟</para>
        /// <para>- false: 组件禁用时不会取消延迟，但组件销毁时仍会取消</para>
        /// </param>
        /// <param name="delayTiming">延迟执行的时机，默认为Update
        /// <para>决定在Unity生命周期中的哪个阶段继续执行</para>
        /// </param>
        /// <param name="cancelImmediately">取消时是否立即结束，默认为false
        /// <para>- true: 取消时立即结束当前等待</para>
        /// <para>- false: 取消时等待当前帧完成后再结束</para>
        /// </param>
        /// <returns>延迟是否成功完成，如果被取消则返回false</returns>
        /// <exception cref="ArgumentException">当millisecondsDelay小于0时抛出</exception>
        /// <example>
        /// <code>
        /// // 等待1秒，忽略时间缩放
        /// bool completed = await Delay(1000, ignoreTimeScale: true);
        /// if (completed)
        /// {
        ///     // 延迟成功完成后的操作
        /// }
        /// </code>
        /// </example>
        protected UniTask<bool> Delay(int millisecondsDelay, bool ignoreTimeScale = false, bool disableCancel = true,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update, bool cancelImmediately = false) =>
            AsyncDelay.Delay(millisecondsDelay, ignoreTimeScale, disableCancel, delayTiming, cancelImmediately);

        #endregion



    }
}
