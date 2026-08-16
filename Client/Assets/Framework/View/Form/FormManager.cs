// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2019/03/06
// **************************************************************************************

using Framework.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;
using Framework.ViewCache;

namespace Framework.View
{
    /// <summary>
    /// 界面管理器
    ///
    /// 1、界面的开启、缓存功能
    /// 2、界面的层级管理功能
    /// 3、界面的相关事件统一监听接口
    /// </summary>
    public abstract class FormManager : IViewManager
    {
        #region private: 成员变量

        // UI根节点
        private RectTransform _uiRoot;

        // 所有当前开启的界面
        private readonly List<BaseForm> _forms = new();

        // 默认的界面缓存数量
        private const int DEFAULT_CACHE_CAPACITY = 20;

        // 每个层级可容纳的界面数量
        private const int LAYER_CAPACITY = 1000;

        // 界面缓存
        public Cache<BaseForm> Cache { get; private set; }

        /// <summary>
        /// 此控制器用于实现功能：隐藏指定Layer之下的所有界面
        /// </summary>
        private static readonly VisibleController _hideLayerController = new(
            $"{nameof(FormManager)}.{nameof(HideLayer)}", FormVisibleStrategyByCanvas.Shared);

        #endregion

        #region public：属性

        public int CacheCapacity
        {
            get => Cache.Capacity;
            set => Cache.Capacity = value;
        }

        /// <summary>
        /// 小于此层级的界面，打开时会被设置隐藏状态（如果HideLayerState为true），或者解除隐藏状态（如果HideLayerState为false）
        /// </summary>
        public int HideLayer { get; private set; }

        /// <summary>
        /// true表示隐藏
        /// </summary>
        public bool HideLayerState { get; private set; }

        /// <summary>
        /// 只读的界面列表
        /// </summary>
        public ReadOnlyCollection<BaseForm> Forms => _forms.AsReadOnly();

        #endregion

        #region public: 界面事件

        /// <summary>
        /// Form初始化前触发
        /// </summary>
        public event Action<BaseForm> FormPreAwake;

        /// <summary>
        /// Form初始化后触发
        /// </summary>
        public event Action<BaseForm> FormPostAwake;

        /// <summary>
        /// Form销毁前触发
        /// </summary>
        public event Action<BaseForm> FormPreDestroy;

        /// <summary>
        /// Form销毁后触发
        /// </summary>
        public event Action<BaseForm> FormPostDestroy;

        /// <summary>
        /// Form开启前触发，生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPreOpen;

        /// <summary>
        /// Form开启后触发，生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPostOpen;

        /// <summary>
        /// Form关闭前触发,生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPreClose;

        /// <summary>
        /// Form关闭后触发,生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPostClose;

        /// <summary>
        /// Form关闭后触发,生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPreShow;

        /// <summary>
        /// Form显示前触发,生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPostShow;

        /// <summary>
        /// Form显示后触发,生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPreHide;

        /// <summary>
        /// Form隐藏前触发,生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm> FormPostHide;

        /// <summary>
        /// Form隐藏后触发,生命周期内可能有多次
        /// </summary>
        public event Action<BaseForm, int, int> FormLayerChanged; // 界面层级改变时触发的事件
        public event Action<BaseForm> FormRenderingChanged;

        #endregion

        #region public: 开启/关闭接口

        /// <summary>
        /// 打开界面
        ///
        /// 实现 IViewManager 接口的开启方法
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async UniTask<ViewBase> OpenAsync(IViewOptions options, CancellationToken cancellationToken = default)
        {
            if (options is FormOptions formOptions)
            {
                return await Open(formOptions, cancellationToken);
            }

            throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 开启界面
        /// </summary>
        /// <param name="options">界面打开的参数项</param>
        /// <param name="cancellationToken">可选参数，用于中止开启流程的token</param>
        /// <returns>界面上挂载的Form脚本对象</returns>
        public async UniTask<BaseForm> Open(FormOptions options,
            CancellationToken cancellationToken = default)
        {
            var layer = options.Layer;
            var name = options.AssetName;
            // layer 的合法范围由 GetUniqueLayer 决定：> 0 按分桶自动分配唯一层级，< 0 表示外部传入的精确层级（取 -layer），
            // 0 是分桶 0 的起点。FormOptions.Layer 默认 -1（外部层级 1），因此这里不能拒绝 <= 0。

            // 1. 获取form实例：从缓存获取，或者实例化一个新的
            // ReSharper disable once MethodHasAsyncOverload
            var form = Cache.Take(name);
            if (form == null)
            {
                form = await LoadForm(options, cancellationToken);
                if (form == null) return null;
            }
            else
            {
                var rt = form.transform as RectTransform;
                rt.SetParent(_uiRoot, false);
            }

            // 2. 设置参数
            form.Layer = GetUniqueLayer(layer);
            form.LifeCycleExecutor = options.LifeCycleExecutor ?? this;

            GameLog.Debug($"添加到forms中， {form.AssetName}", module: "Framework.ViewCache");

            // 3. 添加组件
            if (options.Components != null)
            {
                foreach (var component in options.Components)
                {
                    form.AddViewComponent(component);
                }
            }

            // 4. 加入到界面列表中，并根据层级排序
            _forms.Add(form);
            ReorderForms(form);

            // 5. 执行Open生命周期流程
            try
            {
                var param = new LifeCycleArgs(LifeCycleCause.Open,
                    options.Data, cancellationToken);
                await ((IViewLifeCycle)form).ExecuteOpen(param);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
            }

            // 6. 如果低于隐藏层级，则设置隐藏
            if (form.Layer < HideLayer)
            {
                await form.SetVisibleState(_hideLayerController, !HideLayerState);
            }

            return form;
        }

        /// <summary>
        /// 隐藏或者显示指定Layer以下的界面
        /// </summary>
        /// <param name="layer">指定的层级</param>
        /// <param name="hide">true表示隐藏</param>
        public void SetHideBeneath(int layer, bool hide)
        {
            var oldLayer = HideLayer;
            var oldState = HideLayerState;

            if (layer == oldLayer)
            {
                if (hide == oldState) return;
            }
            else
            {
                // 重置旧的层级所关联的状态
                SetHideBeneath(oldLayer, false);

                if (!hide)
                {
                    HideLayer = layer;
                    HideLayerState = false;
                    return;
                }
            }

            HideLayer = layer;
            HideLayerState = hide;
            foreach (var form in _forms)
            {
                if (form.Layer < layer)
                {
                    form.SetVisibleState(_hideLayerController, !hide);
                }
            }
        }

        #endregion

        #region public: 获取界面接口

        /// <summary>
        /// 查找指定类型的界面（如果有多个则返回找到的第一个）
        /// </summary>
        /// <param name="assetName">【可选参数】预制体资源名字，不带.prefab后缀</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        
        public BaseForm FindForm<T>(string assetName = null) where T : BaseForm
        {
            foreach (var form in _forms)
            {
                if (form is T tFrom)
                {
                    if (assetName == null || tFrom.AssetName == assetName) return tFrom;
                }
            }

            return null;
        }

        /// <summary>
        /// 查找指定类型的多个界面
        /// </summary>
        /// <param name="assetName">【可选参数】预制体资源名字，不带.prefab后缀</param>
        public IEnumerable<T> FindForms<T>(string assetName = null) where T : BaseForm
        {
            foreach (var form in _forms)
            {
                if (form is T tFrom)
                {
                    if (assetName == null || tFrom.AssetName == assetName) yield return tFrom;
                }
            }
        }

        /// <summary>
        /// 根据层级获取界面（如果有多个则返回找到的第一个）
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        
        public BaseForm FindForm(int layer)
        {
            foreach (var form in _forms)
            {
                if (form.Layer == layer) return form;
            }

            return null;
        }

        #endregion

        #region public: 生命周期

        protected void Init(RectTransform uiRoot)
        {
            _uiRoot = uiRoot;

            Cache = CacheFactory.CreateCache(DEFAULT_CACHE_CAPACITY,
                new FormResContainer(_uiRoot),
                new FIFOCacheStrategy());

            ViewBase.StaticLifeCycleEvents.PreAwake.Add(OnStaticEventPreAwake);
            ViewBase.StaticLifeCycleEvents.PostAwake.Add(OnStaticEventPostAwake);

            ViewBase.StaticLifeCycleEvents.PreDestroy.Add(OnStaticEventPreDestroy);
            ViewBase.StaticLifeCycleEvents.PostDestroy.Add(OnStaticEventPostDestroy);

            ViewBase.StaticLifeCycleEvents.PreOpen.Add(OnStaticEventPreOpen);
            ViewBase.StaticLifeCycleEvents.PostOpen.Add(OnStaticEventPostOpen);
            ViewBase.StaticLifeCycleEvents.PreClose.Add(OnStaticEventPreClose);
            ViewBase.StaticLifeCycleEvents.PostClose.Add(OnStaticEventPostClose);
            ViewBase.StaticLifeCycleEvents.PreShow.Add(OnStaticEventPreShow);
            ViewBase.StaticLifeCycleEvents.PostShow.Add(OnStaticEventPostShow);
            ViewBase.StaticLifeCycleEvents.PreHide.Add(OnStaticEventPreHide);
            ViewBase.StaticLifeCycleEvents.PostHide.Add(OnStaticEventPostHide);
        }

        public virtual void Update(float elapsed)
        {
            Cache.Update(elapsed);
        }

        public virtual void Shutdown()
        {
            try
            {
                // 清理所有form
                foreach (var form in SafeEnumerateForms())
                {
                    var args = new LifeCycleArgs(LifeCycleCause.Close);
                    // 异步 Close 无法在同步 Shutdown 中 await，显式 Forget 避免异常沦为「未观察任务异常」。
                    var executor = form.LifeCycleExecutor;
                    if (executor != null)
                    {
                        executor.LifeCycleExecuteClose(form, args).Forget();
                    }
                }
            }
            finally
            {
                // 清理事件监听器
                // CleanupEventListeners();

                ViewBase.StaticLifeCycleEvents.PreAwake.Remove(OnStaticEventPreAwake);
                ViewBase.StaticLifeCycleEvents.PostAwake.Remove(OnStaticEventPostAwake);
                ViewBase.StaticLifeCycleEvents.PreDestroy.Remove(OnStaticEventPreDestroy);
                ViewBase.StaticLifeCycleEvents.PostDestroy.Remove(OnStaticEventPostDestroy);

                ViewBase.StaticLifeCycleEvents.PreOpen.Remove(OnStaticEventPreOpen);
                ViewBase.StaticLifeCycleEvents.PostOpen.Remove(OnStaticEventPostOpen);
                ViewBase.StaticLifeCycleEvents.PreClose.Remove(OnStaticEventPreClose);
                ViewBase.StaticLifeCycleEvents.PostClose.Remove(OnStaticEventPostClose);
                ViewBase.StaticLifeCycleEvents.PreShow.Remove(OnStaticEventPreShow);
                ViewBase.StaticLifeCycleEvents.PostShow.Remove(OnStaticEventPostShow);
                ViewBase.StaticLifeCycleEvents.PreHide.Remove(OnStaticEventPreHide);
                ViewBase.StaticLifeCycleEvents.PostHide.Remove(OnStaticEventPostHide);

                FormPreAwake = null;
                FormPostAwake = null;
                FormPreDestroy = null;
                FormPostDestroy = null;

                FormPreOpen = null;
                FormPostOpen = null;
                FormPreClose = null;
                FormPostClose = null;
                FormPreShow = null;
                FormPostShow = null;
                FormPreHide = null;
                FormPostHide = null;
                FormLayerChanged = null;
                FormRenderingChanged = null;

                _uiRoot = null;

                // 不再同步清空 _forms：异步 Close 的后续（LifeCycleExecuteClose）需要从 _forms 中移除自己，
                // 同步 Clear 会导致 Remove 失败并产生误导性错误日志；_forms 会随所有 Close 完成自然排空。

                // Cache 未实现 IDisposable，直接清空缓存（销毁缓存的界面实例）后置空引用。
                Cache?.Clear();
                Cache = null;
            }
        }

        #endregion

        #region private: 界面事件监听

        private void OnStaticEventPreAwake(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPreAwake?.Invoke(form);
            }
        }

        private void OnStaticEventPostAwake(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPostAwake?.Invoke(form);
            }
        }

        private void OnStaticEventPreDestroy(ViewBase view)
        {
            if (view is BaseForm form)
            {

// #if UNITY_EDITOR
//                 if (!form.Running)
//                 {
//                     GameLog.Error($"Do not destroy '{form.AssetName}' while it's running, close first!", module: "Framework.View");
//                 }
// #endif
                FormPreDestroy?.Invoke(form);
            }
        }

        private void OnStaticEventPostDestroy(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPostDestroy?.Invoke(form);
            }
        }

        private void OnStaticEventPreOpen(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPreOpen?.Invoke(form);
            }
        }

        private void OnStaticEventPostOpen(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPostOpen?.Invoke(form);
            }
        }

        private void OnStaticEventPreClose(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPreClose?.Invoke(form);
            }
        }

        private void OnStaticEventPostClose(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPostClose?.Invoke(form);
            }
        }

        private void OnStaticEventPreShow(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPreShow?.Invoke(form);
            }
        }

        private void OnStaticEventPostShow(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPostShow?.Invoke(form);
            }
        }

        private void OnStaticEventPreHide(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPreHide?.Invoke(form);
            }
        }

        private void OnStaticEventPostHide(ViewBase view)
        {
            if (view is BaseForm form)
            {
                FormPostHide?.Invoke(form);
            }
        }

        private void OnLayerChanged(BaseForm form, int oldLayer, int newLayer)
        {
            ReorderForms(form);
            if (oldLayer < HideLayer && newLayer >= HideLayer)
            {
                // 解除隐藏
                if (HideLayerState) form.SetVisibleState(_hideLayerController, true);
            }
            else if (oldLayer >= HideLayer && newLayer < HideLayer)
            {
                form.SetVisibleState(_hideLayerController, !HideLayerState);
            }

            FormLayerChanged?.Invoke(form, oldLayer, newLayer);
        }

        private void OnFormRenderingChanged(ViewBase form)
        {
            FormRenderingChanged?.Invoke(form as BaseForm);
        }

        #endregion

        #region private: 私有方法

        /// <summary>
        /// 根据分层获取唯一layer值
        /// （如果极限情况下层级界面满了，则返回最大layer值）
        /// </summary>
        /// <param name="layer">层级</param>
        /// <returns>唯一层级</returns>
        private int GetUniqueLayer(int layer)
        {
            //当layer小于0时，表示使用外部传入层级
            if (layer < 0) return -layer;

            var baseLayer = layer - (layer % LAYER_CAPACITY);
            var layerMax = baseLayer + LAYER_CAPACITY;

            //计算当前层级中所有界面的最大layer值
            var currMax = baseLayer;
            var count = _forms.Count;
            for (var i = 0; i < count; i++)
            {
                var form = _forms[i];
                var formLayer = form.Layer;
                if (formLayer < baseLayer || formLayer >= layerMax) continue;
                if (formLayer > currMax)
                    currMax = formLayer;
            }

            var uniqueLayer = currMax + 1;
            return uniqueLayer < layerMax ? uniqueLayer : currMax;
        }

        public override string ToString()
        {
            return $"[{nameof(FormManager)}] cacheInfo:【\n{Cache}\n】";
        }

        /// <summary>
        /// 加载一个新的界面实例
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async UniTask<BaseForm> LoadForm(FormOptions options, CancellationToken cancellationToken)
        {
            var assetName = options.AssetName + ".prefab";

            GameObject go = null;
            try
            {
                // go = await AssetUtil.InstantiateAsync<GameObject>(assetName, _uiRoot);
                go = await InstantiateForm(assetName, _uiRoot, cancellationToken);

                if (go == null)
                {
                    // 资源加载失败（IAssetSystem 返回 null），避免下面 go.activeSelf 空引用
                    GameLog.Error($"LoadForm 加载失败，资源不存在: {assetName}", module: "Framework.View");
                    return null;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Object.Destroy(go);
                    return null;
                }

                if (!go.activeSelf)
                {
                    // 没有激活时不会执行Awake()，所以必须激活
                    GameLog.Error($"{go} is not active in prefab!", module: "Framework.View");
                    go.SetActive(true);
                }
            }
            catch (Exception e)
            {
                if (go != null) Object.Destroy(go);
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                return null;
            }

            try
            {
                var form = go.GetComponent<BaseForm>();
                OnFormLoaded(form, options);
                return form;
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "View lifecycle exception", module: "Framework.View");
                return null;
            }
        }

        protected abstract UniTask<GameObject> InstantiateForm(string assetName, Transform parent,
            CancellationToken cancellationToken);

        /// <summary>
        /// 加载新form时对其进行监听等处理
        /// </summary>
        /// <param name="form"></param>
        /// <param name="options"></param>
        private void OnFormLoaded(BaseForm form, FormOptions options)
        {
            form.AssetName = options.AssetName;
            form.LayerChanged += OnLayerChanged;
            form.RenderingChanged += OnFormRenderingChanged;
        }

        /// <summary>
        /// 迭代界面列表
        /// </summary>
        /// <returns></returns>
        private IEnumerable<BaseForm> SafeEnumerateForms()
        {
            var list = UnityEngine.Pool.ListPool<BaseForm>.Get();
            list.AddRange(_forms);

            try
            {
                foreach (var form in list)
                {
                    yield return form;
                }
            }
            finally
            {
                UnityEngine.Pool.ListPool<BaseForm>.Release(list);
            }
        }

        /// <summary>
        /// 对界面进行重新排序
        /// </summary>
        private void ReorderForms(BaseForm changedForm)
        {
            var formsCount = _forms.Count;
            if (formsCount <= 1)
            {
                return;
            }

            var lastIndex = formsCount - 1;
            if (_forms[lastIndex] == changedForm)
            {
                // 大部分情况下，一次打开一个界面
                // 这里优化处理（依次比较，在合适处插入）
                var i = lastIndex;
                for (--i; i >= 0; --i)
                {
                    var form = _forms[i];
                    if (form.Layer > changedForm.Layer)
                    {
                        // 层级大的往后移动一位
                        _forms[i + 1] = form;
                    }
                    else
                    {
                        // 遇到层级小的（或相等）则结束
                        break;
                    }
                }

                // 把新界面放到之前停下的位置
                _forms[i + 1] = changedForm;
            }
            else
            {
                // 全部按层级大小重排序
                Algorithm.StableSort(_forms, (a, b) => a.Layer - b.Layer);
            }

            // 遍历设置每个界面的排列顺序
            for (var i = 0; i < formsCount; ++i)
            {
                var form = _forms[i];
                //编辑器方便查看顺序
#if UNITY_EDITOR
                form.transform.SetSiblingIndex(i);
#endif
                form.SortingOrder = i * 1000;
            }
        }

        /// <summary>
        /// 断言函数
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                GameLog.Error(message, module: "Framework.View");
            }
        }

        #endregion


        #region IViewLifeCycleExecutor implementation

        async UniTask IViewLifeCycleExecutor.LifeCycleExecuteClose(IViewLifeCycle view, LifeCycleArgs args)
        {
            if (view is not BaseForm form)
            {
                GameLog.Error($"Failed to close view, {view} is not {nameof(BaseForm)}", module: "Framework.View");
                return;
            }

            await view.ExecuteClose(args);

            if (!_forms.Remove(form))
            {
                GameLog.Error($"Failed to remove {form.AssetName} from {nameof(FormManager)}", module: "Framework.View");
            }
            else
            {
                GameLog.Debug($"移除forms : {form.AssetName}", module: "Framework.ViewCache");
            }

            // 关闭流程中表单可能已被销毁（如 Shutdown 中 Unity 销毁 UI 根），此时不再缓存；
            // 且 Shutdown 已清空并置空 Cache，异步 Close 后续执行到这里时 Cache 可能为 null，需判空避免 NRE。
            var cache = Cache;
            if (form != null && cache != null)
            {
                cache.Put(form, form.AssetName);
            }
        }

        #endregion
    }
}
