//**************************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description 基础滑动组件
//**************************************************************************************

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public class BaseSlip : BaseDrag
    {
        // 事件代理
        public SlipEventHandler HorizontalSlip;
        public SlipEventHandler VerticalSlip;

        [Header("滑动阈值(需大于拖动阈值才有效)")]
        public float slipThreshold = 0f;
        [Header("偏移角度")]
        public float offsetAngle = 0f;
        [Header("是否透传拖动")]
        public bool isPassDrag = false;

        // 开始位置
        private Vector2 _startPos;

        private void Awake()
        {
            // 设置回调，用于判定滑动方向
            InitializePotentialDrag = InitializePotentialDragCb;
            BeginDrag = BeginDragCb;

            // 设置拖动透传
            IsDragPass = isPassDrag;
        }

        // 点击时记录下起始位置
        private void InitializePotentialDragCb(GameObject obj, PointerEventData eventData)
        {
            _startPos = eventData.position;
        }

        // 开始拖动时根据当前位置与记录的起始位置判定滑动方向，并根据方向调用相应回调
        private void BeginDragCb(GameObject obj, PointerEventData eventData)
        {
            var dir = eventData.position - _startPos;
            if (slipThreshold > 0f && dir.sqrMagnitude < slipThreshold * slipThreshold)
                return;

            var angle = Vector2.Angle(dir, Vector2.right);
            if (dir.y < 0)
                angle = 360 - angle;

            var realAngle = angle - offsetAngle;
            var isUp = realAngle > 45 && realAngle < 135;
            var isDown = realAngle > 225 && realAngle < 315;
            if (isUp || isDown)
                VerticalSlip?.Invoke(obj, eventData, isUp);
            else
            {
                var isLeft = realAngle >= 135 && realAngle <= 225;
                HorizontalSlip?.Invoke(obj, eventData, !isLeft);
            }
        }

        public override void Clear()
        {
            base.Clear();

            HorizontalSlip = null;
            VerticalSlip = null;
        }
    }
}
