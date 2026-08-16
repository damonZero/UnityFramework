//**************************************************************************************
//Create By szx on 2020/12/3
//
//@Description Coverage UI区域定义
//**************************************************************************************

using Framework.Coverage;
using Framework.Log;
using UnityEngine;

namespace Framework.Coverage
{
    public class UICoverageArea
    {
        /// <summary>
        /// 区域类型
        /// </summary>
        public enum CoverageType
        {
            Show = 1, //显示区域
            Cover = 2 //遮挡区域
        }

        public IntRect Rect { get; set; }


        public RectTransform Trans => AreaInfo.anchorTrans;

        public AreaInfo AreaInfo { get; private set; }

        public CoverageType CovType { get; private set; }

        private CanvasCoverage _canvasCov;

        private UICoverageAreaCtrl _ctrl; // 区域控制组件，可以为null

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool Available
        {
            get
            {
                if (Trans == null || Trans.Equals(null))
                    return false;
                if (_ctrl != null && !_ctrl.Equals(null))
                    return _ctrl.GetAvailable(CovType);
                return true;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="areaInfo"></param>
        /// <param name="canvasCov"></param>
        internal void Init(AreaInfo areaInfo, CanvasCoverage canvasCov, CoverageType type)
        {
            AreaInfo = areaInfo;
            var trans = areaInfo.anchorTrans;
            if (trans == null || trans.Equals(null))
                GameLog.Error($"Coverage显示或遮挡区域丢失 : {canvasCov.name}", module: "Framework.Coverage");
            CovType = type;
            if (_ctrl != null && !_ctrl.Equals(null))
                _ctrl.OnAvailableChange -= OnAvailableChange;
            _ctrl = trans.GetComponent<UICoverageAreaCtrl>();
            if (_ctrl != null)
                _ctrl.OnAvailableChange += OnAvailableChange;
            _canvasCov = canvasCov;
            CalcRect();
        }

        /// <summary>
        /// 计算矩形区域
        /// </summary>
        private void CalcRect()
        {
            Rect = CalcRect(AreaInfo);
        }

        private void OnAvailableChange(CoverageType type, bool available)
        {
            if (CovType != type)
                return;
            _canvasCov.ReCalcSide();
        }

        public static IntRect CalcRect(AreaInfo info)
        {
            var trans = info.anchorTrans;
            var pos = CoverageUtil.RectTransToUIPointWithoutAnchor(trans);
            var rect = trans.rect;
            var scale = trans.lossyScale;
            var newRect = new Rect(pos.x, pos.y, rect.width * scale.x,
                rect.height * scale.y);
            newRect.x += info.offsetLeft * scale.x;
            newRect.y += info.offsetDown * scale.y;
            newRect.width -= info.offsetRight * scale.x;
            newRect.height -= info.offsetUp * scale.y;
            return new IntRect(newRect.x, newRect.y, newRect.width, newRect.height);
        }
    }
}
