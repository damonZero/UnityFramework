//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 基础显示对象，所有脚本类型的显示对象都必须继承于这个类
//**************************************************************************************

using System;
using System.Collections.Generic;
using Framework.Log;
using UnityEngine;

namespace Framework.Coverage
{
    /// <summary>
    /// 显示单位脚本的基类
    /// </summary>
    public abstract class BaseCoverage : MonoBehaviour, ICoverage
    {
        private bool _ignore = false;
        private bool _hasEnabled = false;
        private bool _visible = true;
        protected bool _refreshChildrenWhenVisibleChanged = true;
        private readonly List<CoverageChild> _children = new();

        /// <summary>
        /// 根节点
        /// </summary>
        public CoverageRoot Holder { get; protected set; }

        /// <summary>
        /// 在显示对象中的排列顺序，值越大越靠前
        /// </summary>
        public virtual int CoverageIdx => transform.GetSiblingIndex();

        /// <summary>
        /// 是否成功初始化
        /// </summary>
        public bool InitSuccess
        {
            get
            {
                if (!_initSuccess)
                    _initSuccess = Init();
                return _initSuccess;
            }
        }

        protected bool _initSuccess = false;

        /// <summary>
        /// 是否忽略，忽略即不参与遮挡关系计算
        /// </summary>
        public virtual bool Ignore
        {
            get => _ignore;
            set
            {
                if (_ignore != value)
                {
                    _ignore = value;
                    if (_ignore)
                    {
                        SetVisible(true);
                    }
                    InvokeNeedAdjustEvt();
                }
            }
        }

        /// <summary>
        /// 当前Coverage自身的Visible状态
        /// </summary>
        public virtual bool CoverageVisible => _visible;

        /// <summary>
        /// Coverage所属对象是否处于active且真实可见（会被渲染）
        /// </summary>
        public virtual bool ActiveAndRendering => ActualRendering && gameObject.activeSelf;

        /// <summary>
        /// Coverage所属对象是否真的会被渲染
        /// </summary>
        protected abstract bool ActualRendering { get; }

        public abstract IEnumerable<IntRect> ShowRectList { get; }
        public abstract IEnumerable<IntRect> CoverRectList { get; }


        /// <summary>
        /// 竖直方向的边的列表
        /// </summary>
        public abstract IList<RectSide> VerticalSideList { get; }

        /// <summary>
        /// 水平方向的边的列表
        /// </summary>
        public abstract IList<RectSide> HorizontalSideList { get; }


        /// <summary>
        /// 显示单位激活事件
        /// </summary>
        public event Action<BaseCoverage> OnCoverageEnable;

        /// <summary>
        /// 显示单位隐藏事件
        /// </summary>
        public event Action<BaseCoverage> OnCoverageDisable;

        /// <summary>
        /// 显示单位销毁事件
        /// </summary>
        public event Action<BaseCoverage> OnCoverageDestroy;

        /// <summary>
        /// 层级改变事件
        /// </summary>
        public event Action<BaseCoverage> OnLayerChange;

        /// <summary>
        /// 扩展事件，当需要调整遮挡关系时除了上面定义的事件，还可以派发这个事件
        /// </summary>
        public event Action<BaseCoverage> OnNeedAdjust;

        protected abstract bool RegisterOnStart { get; }


        protected abstract bool Init();

        /// <summary>
        /// 执行显示或隐藏的具体操作，由子类实现
        /// </summary>
        /// <param name="visible"></param>
        protected abstract void DoSetVisible(bool visible);

        /// <summary>
        /// 屏蔽渲染元素调试信息
        /// </summary>
        /// <returns></returns>
        public abstract string DebugInfo();


        /// <summary>
        /// 通过在显示对象的排列顺序进行比较
        /// </summary>
        /// <param name="cov1"></param>
        /// <param name="cov2"></param>
        /// <returns></returns>
        public static int Compare(BaseCoverage cov1, BaseCoverage cov2)
        {
            return cov1.CoverageIdx - cov2.CoverageIdx;
        }

        /// <summary>
        /// 重新初始化
        /// </summary>
        public void ReInit()
        {
            _initSuccess = false;
            _initSuccess = Init();
        }


        /// <summary>
        /// 是否填充满整个检测区域（全屏）
        /// </summary>
        /// <returns></returns>
        public bool IsFull()
        {
            if (!InitSuccess)
            {
                GameLog.Error($"Coverage: {gameObject.name} have not init success!", module: "Framework.Coverage");
                return false;
            }

            return CoverageUtil.RectIsCoveredByOthers(Holder.Range, CoverRectList, Holder.Range);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;

            DoSetVisible(visible);

            if (_refreshChildrenWhenVisibleChanged)
            {
                RefreshChildren(visible);
            }
        }


        /// <summary>
        /// 注册coverage 子节点
        /// </summary>
        /// <param name="child"></param>
        public void RegisterChild(CoverageChild child)
        {
            if (_children.Contains(child))
                return;
            _children.Add(child);

            if (CoverageVisible)
            {
                child.OnShow();
            }
            else
            {
                child.OnHide();
            }
        }

        /// <summary>
        /// 刷新coverage 子节点的显隐
        /// </summary>
        protected void RefreshChildren(bool visible)
        {
            for (var i = _children.Count - 1; i >= 0; i--)
            {
                var child = _children[i];
                if (child == null)
                {
                    _children.RemoveAt(i);
                    continue;
                }

                if (visible)
                {
                    child.OnShow();
                }
                else
                {
                    child.OnHide();
                }
            }
        }


        /// <summary>
        /// 触发层级改变事件，因为event事件无法在子类触发，所以写个方法给子类调用
        /// </summary>
        protected void InvokeLayerChangeEvt()
        {
            OnLayerChange?.Invoke(this);
        }

        protected void InvokeNeedAdjustEvt()
        {
            OnNeedAdjust?.Invoke(this);
        }


        protected virtual void Awake()
        {
            Holder = FindHolder();
        }

        protected virtual void Start()
        {
            if (RegisterOnStart)
            {
                InitAndRegister();
            }
        }

        protected void InitAndRegister()
        {
            _initSuccess = Init();
            _hasEnabled = true;
            if (Holder != null)
            {
                if (!Holder.Register(this))
                {
                    OnNeedAdjust?.Invoke(this);
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (_hasEnabled)
                OnCoverageEnable?.Invoke(this);
        }

        protected virtual void OnDisable()
        {
            OnCoverageDisable?.Invoke(this);
        }

        protected void OnDestroy()
        {
            OnCoverageDestroy?.Invoke(this);
        }

        /// <summary>
        /// 寻找根节点
        /// </summary>
        /// <returns></returns>
        protected virtual CoverageRoot FindHolder()
        {
            return CoverageRoot.Global;
        }
    }
}
