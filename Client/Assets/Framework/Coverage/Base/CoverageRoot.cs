//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 显示对象根节点
//**************************************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Framework.Log;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

namespace Framework.Coverage
{
    /// <summary>
    /// 重排方式
    /// </summary>
    public enum AdjustType
    {
        Later = 1, //延迟重排
        Immediately = 2 //立即重排
    }

    /// <summary>
    /// 显示单位根节点
    /// </summary>
    public class CoverageRoot : UIBehaviour
    {
        /// <summary>
        /// 开关状态本地存储的key(仅对Editor有效，真实环境都会开启)
        /// </summary>
#if UNITY_EDITOR
        public const string TOGGLE_SAVE_KEY = "TOGGLE_COVERAGE";
#endif
        public IntRect Range { get; private set; } //显示范围

        public RectTransform root; //根节点

        /// <summary>
        /// 获取所有的Coverage
        /// </summary>
        public List<BaseCoverage> CoverageList => _coverageList;

        [Header("重排方式")] public AdjustType adjustType = AdjustType.Later;

        private readonly List<BaseCoverage> _coverageList = new(); //显示对象列表
        private RectCheckContext _checkCtx; //检测遮挡的上下文对象
        private bool _happenedError = false; //是否发生过错误
        private bool _needResizeLater = false;
        private bool _needAdjust = false;
        private Vector3 _oldScale;
        private Vector3 _oldPosition;

        /// <summary>
        /// 重排事件
        /// </summary>
        public event Action OnAdjust;


        /// <summary>
        /// 全局根节点
        /// </summary>
        public static CoverageRoot Global { get; private set; }

        /// <summary>
        /// 使用上一次的计算结果来判断是否占满整个屏幕
        /// </summary>
        /// <returns></returns>
        public static bool IsFullScreen()
        {
            var global = Global;
            if (global == null) return false;

            return CoverageUtil.RectIsCoveredByCtx(global.Range, global._checkCtx);
        }

        /// <summary>
        /// 获取屏蔽渲染当前的状态打印，用于调试信息
        /// </summary>
        /// <returns></returns>
        public string DebugInfo()
        {
            var sb = new StringBuilder();
            sb.Append($"当前帧数:{Time.frameCount}  当前检测区域:{Range}\n");
            for (int i = _coverageList.Count - 1; i >= 0; --i)
            {
                sb.Append(_coverageList[i].DebugInfo());
                sb.Append("\n\n");
            }

            return sb.ToString();
        }


        protected override void Awake()
        {
            Global = this;
        }

        protected override void Start()
        {
#if UNITY_EDITOR
            // var value = EditorPrefs.GetString(TOGGLE_SAVE_KEY);
            // var isOpen = string.IsNullOrEmpty(value) || value == "open";
            // Debug.Log($"Editor下UI屏蔽渲染是否启用: {isOpen}");
#endif
            Resize();
        }


        private void Update()
        {
            if (_needResizeLater || _oldScale != transform.lossyScale)
                _needResizeLater = true;
            if (_needResizeLater || _oldPosition != transform.position)
                _needResizeLater = true;

            if (_needResizeLater)
            {
                Resize();
                _needResizeLater = false;
            }
        }

        private void LateUpdate()
        {
            if (_needAdjust)
            {
                AdjustCoverageList();
                _needAdjust = false;
            }
        }

        protected override void OnDestroy()
        {
            Global = null;
            for (int i = _coverageList.Count - 1; i >= 0; --i)
            {
                UnRegister(_coverageList[i], false);
            }
        }

        /// <summary>
        /// 该节点的尺寸、缩放改变回调
        /// 加该回调是因为 UICamera.lua在修改UI像机的orthographicSize,会导致该节点的缩放发生变化。需要重新计算一次Range
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _needResizeLater = true;
        }


        /// <summary>
        /// 重新计算检查区域，并重新初始化已注册的coverage
        /// </summary>
        public void Resize()
        {
            var lossyScale = transform.lossyScale;
            _oldScale = lossyScale;
            _oldPosition = transform.position;
            var rect = root.rect;
            var realWidth = rect.width * lossyScale.x;
            var realHeight = rect.height * lossyScale.y;
            var pos = CoverageUtil.RectTransToUIPointWithoutAnchor(transform as RectTransform);
            var newRange = new IntRect(pos.x, pos.y, realWidth, realHeight);
            if (newRange == Range)
                return;
            Range = newRange;
            _checkCtx = RectCheckContext.Take(Range);
            foreach (var coverage in _coverageList)
                coverage.ReInit();
            AdjustCoverageList();
            // Debug.Log($"刷新UI屏蔽渲染检测区域信息：{newRange}");
            // Debug.Log($"刷新根节点尺寸{Time.frameCount}  {transform.lossyScale}  {transform.position})");
        }

        /// <summary>
        /// 是否包含某个coverage
        /// </summary>
        /// <param name="coverage"></param>
        public bool ContainsCoverage(BaseCoverage coverage)
        {
            return _coverageList.Contains(coverage);
        }

        /// <summary>
        /// 注册显示单位
        /// </summary>
        /// <param name="coverage"></param>
        public bool Register(BaseCoverage coverage)
        {
            if (_coverageList.Contains(coverage)) return false;

            coverage.OnCoverageEnable += OnCoverageEnable;
            coverage.OnCoverageDisable += OnCoverageDisable;
            coverage.OnCoverageDestroy += OnCoverageDestroy;
            coverage.OnLayerChange += OnLayerChange;
            coverage.OnNeedAdjust += OnNeedAdjust;
            _coverageList.Add(coverage);
            _coverageList.Sort(BaseCoverage.Compare);

            if (adjustType == AdjustType.Later)
                _needAdjust = true;
            else
                AdjustCoverageList(_coverageList.IndexOf(coverage));
            return true;
        }

        /// <summary>
        /// 取消注册
        /// </summary>
        /// <param name="coverage"></param>
        public bool UnRegister(BaseCoverage coverage, bool adjust = true)
        {
            var idx = _coverageList.IndexOf(coverage);
            if (idx < 0) return false;

            _coverageList.RemoveAt(idx);
            coverage.OnCoverageEnable -= OnCoverageEnable;
            coverage.OnCoverageDisable -= OnCoverageDisable;
            coverage.OnCoverageDestroy -= OnCoverageDestroy;
            coverage.OnLayerChange -= OnLayerChange;
            coverage.OnNeedAdjust -= OnNeedAdjust;

            if (adjust)
            {
                if (adjustType == AdjustType.Later)
                    _needAdjust = true;
                else
                    AdjustCoverageList(idx - 1);
            }

            return true;
        }

        /// <summary>
        /// 显示单位激活
        /// </summary>
        protected virtual void OnCoverageEnable(BaseCoverage coverage)
        {
            if (adjustType == AdjustType.Later)
                _needAdjust = true;
            else
                AdjustCoverageList(_coverageList.IndexOf(coverage));
        }

        /// <summary>
        /// 显示单位隐藏
        /// </summary>
        protected virtual void OnCoverageDisable(BaseCoverage coverage)
        {
            if (adjustType == AdjustType.Later)
                _needAdjust = true;
            else
                AdjustCoverageList(_coverageList.IndexOf(coverage) - 1);
        }

        /// <summary>
        /// 层级改变事件
        /// </summary>
        protected virtual void OnLayerChange(BaseCoverage coverage)
        {
            _coverageList.Sort(BaseCoverage.Compare);
            if (adjustType == AdjustType.Later)
                _needAdjust = true;
            else
                AdjustCoverageList();
        }

        /// <summary>
        /// 需要调整时的事件监听
        /// </summary>
        /// <param name="coverage"></param>
        protected virtual void OnNeedAdjust(BaseCoverage coverage)
        {
            if (adjustType == AdjustType.Later)
                _needAdjust = true;
            else
                AdjustCoverageList(_coverageList.IndexOf(coverage));
        }

        /// <summary>
        /// 显示单位销毁
        /// </summary>
        protected virtual void OnCoverageDestroy(BaseCoverage coverage)
        {
            UnRegister(coverage);
        }

        /// <summary>
        /// 调整所有显示对象的显示隐藏
        /// </summary>
        protected void AdjustCoverageList(int start = int.MaxValue)
        {
            if (_happenedError)
                return;
#if UNITY_EDITOR
            var value = EditorPrefs.GetString(TOGGLE_SAVE_KEY);
            if (!string.IsNullOrEmpty(value) && value != "open")
                return;
#endif
            if (start < 0)
                return;

//            var sw = new Stopwatch();
//            sw.Start();
            _checkCtx.Reset();
            try
            {
                for (int i = _coverageList.Count - 1; i >= 0; --i)
                {
                    var curCoverage = _coverageList[i];

                    if (!curCoverage.InitSuccess)
                        continue;

                    if (i <= start && !curCoverage.Ignore)
                    {
                        var covered = curCoverage.IsCoveredByCtx(_checkCtx);
                        curCoverage.SetVisible(!covered);
                    }

                    if (curCoverage.ActiveAndRendering)
                    {
                        _checkCtx.AddSide(curCoverage.VerticalSideList);
                    }
                }

                OnAdjust?.Invoke();
            }
            catch (Exception e)
            {
                for (int i = _coverageList.Count - 1; i >= 0; --i)
                {
                    var coverage = _coverageList[i];
                    coverage.SetVisible(true);
                    UnRegister(coverage, false);
                }

                _happenedError = true;
                GameLog.Exception(e, "coverage adjust error", module: "Framework.Coverage");
            }


//            sw.Stop();
//            Debug.LogFormat("调整遮挡关系耗时耗时：{0}毫秒", sw.ElapsedMilliseconds);
        }
    }
}
