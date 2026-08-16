//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 画布显示对象
//**************************************************************************************

using System;
using System.Collections.Generic;
using Framework.Coverage;
using UnityEngine;

namespace Framework.Coverage
{
    /// <summary>
    /// 画布显示对象
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CanvasCoverage : BaseCoverage
    {
        private Canvas _canvas;
        private UICoverageAreaCollect _showAreas = new UICoverageAreaCollect();
        private UICoverageAreaCollect _coverAreas = new UICoverageAreaCollect();
        private List<RectSide> _verticalSides = new List<RectSide>();
        public bool _rectHasInit = false;

        [SerializeField] private AreaInfo[] _showAreaInfos; //显示区域数据列表
        [SerializeField] private AreaInfo[] _coverAreaInfos; //遮挡区域数据列表

        /// <summary>
        /// 显示矩形列表
        /// </summary>
        public override IEnumerable<IntRect> ShowRectList => _showAreas;

        /// <summary>
        /// 遮挡矩形列表
        /// </summary>
        public override IEnumerable<IntRect> CoverRectList => _coverAreas;

        /// <summary>
        /// 画布对象
        /// </summary>
        public Canvas Canvas
        {
            get
            {
                if (_canvas == null)
                    _canvas = GetComponent<Canvas>();
                return _canvas;
            }
        }
#if UNITY_EDITOR

        public AreaInfo[] ShowArenaInfos
        {
            get => _showAreaInfos ?? new AreaInfo[0];
            set => _showAreaInfos = value;
        }

        public AreaInfo[] CoverArenaInfos
        {
            get => _coverAreaInfos ?? new AreaInfo[0];
            set => _coverAreaInfos = value;
        }

        public AreaInfo SelectedArenaInfo { get; set; }
#endif


        public override IList<RectSide> VerticalSideList => _verticalSides;

        public override IList<RectSide> HorizontalSideList => throw new Exception("暂时只使用竖直方向的边");


        protected override bool ActualRendering => Canvas.enabled;


        protected override void DoSetVisible(bool visible)
        {
            Canvas.enabled = visible;
        }

        /// <summary>
        /// 重新计算边列表
        /// </summary>
        public void ReCalcSide()
        {
            CalcSide();
            InvokeNeedAdjustEvt();
        }

        protected override bool RegisterOnStart => true;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override bool Init()
        {
            if (_initSuccess)
                return true;


            // 初始化显示列表的所有矩形
            if (_showAreaInfos != null)
                _showAreas.Init(_showAreaInfos, this, UICoverageArea.CoverageType.Show);

            if (_showAreaInfos == null || _showAreaInfos.Length < 1)
                Debug.LogError($"coverage 未指定任何显示区域, name：{gameObject.name}");

            // 初始化遮挡列表的所有矩形
            if (_showAreaInfos != null)
                _coverAreas.Init(_coverAreaInfos, this, UICoverageArea.CoverageType.Cover);

            CalcSide();
            return true;
        }

        /// <summary>
        /// 调试信息
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string DebugInfo()
        {
            return $"画布[{gameObject.name}]->  是否可见:{CoverageVisible}";
        }

        /// <summary>
        /// 计算边列表
        /// </summary>
        private void CalcSide()
        {
            foreach (var side in _verticalSides)
                RectSide.Cache(side);
            _verticalSides.Clear();

            foreach (var rect in _coverAreas)
            {
                //顶边
                var side1 = RectSide.Take();
                side1.Pos = rect.Y + rect.Height;
                side1.Flag = 1;
                side1.Start = rect.X;
                side1.End = side1.Start + rect.Width;

                //底边
                var side2 = RectSide.Take();
                side2.Pos = rect.Y;
                side2.Flag = 0;
                side2.Start = side1.Start;
                side2.End = side1.End;

                _verticalSides.Add(side1);
                _verticalSides.Add(side2);
            }
        }
    }
}
