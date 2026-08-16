//**************************************************************************************
//Create By wensx on 2020/03/05
//
//@Description  负责多点触控和事件穿透
//**************************************************************************************

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public class PassTrigger : BaseTrigger
    {
        // 相关参数
        public PassTriggerParam passParam = new PassTriggerParam();

        // Id，用于定点透传
        public int TriggerId
        {
            get => passParam.id;
            set => passParam.id = value;
        }

        // 是否直接透传点击
        public bool IsClickPass
        {
            get => passParam.isClickPass;
            set => passParam.isClickPass = value;
        }

        //是否直接透传拖动
        public bool IsDragPass
        {
            get => passParam.isDragPass;
            set => passParam.isDragPass = value;
        }

        // 处理需要透传的事件，方便子类使用
        protected void Handle<T>(PointerEventData eventData, Action<PointerEventData> cb,
            ExecuteEvents.EventFunction<T> function, bool isClick) where T : IEventSystemHandler
        {
            Handle(baseParam, passParam, eventData, cb, function, isClick);
        }

        /// <summary>
        /// 处理多点触控、禁用与事件透传。
        /// 穿透判定仅由 passParam 决定：点击透传 isClickPass、拖拽透传 isDragPass（事件回调不再返回穿透 bool）。
        /// </summary>
        /// <param name="baseParam">BaseTrigger相关参数</param>
        /// <param name="passParam">PassTrigger相关参数</param>
        /// <param name="eventData">事件数据</param>
        /// <param name="cb">事件处理回调</param>
        /// <param name="function">ExecuteEvents中的对应事件函数</param>
        /// <param name="isClick">是否为点击相关事件</param>
        /// <typeparam name="T">事件处理接口</typeparam>
        public static void Handle<T>(BaseTriggerParam baseParam, PassTriggerParam passParam, PointerEventData eventData,
            Action<PointerEventData> cb, ExecuteEvents.EventFunction<T> function, bool isClick)
            where T : IEventSystemHandler
        {
            // 多点触控处理
            if (!passParam.dealMultiTouch && eventData != null && eventData.pointerId > 0)
                return;

            // 未禁用时执行回调（回调不再返回穿透信号）
            if (!baseParam.isDisable)
                cb(eventData);

            // 穿透仅由 passParam 决定
            var isPass = isClick ? passParam.isClickPass : passParam.isDragPass;

            if (isPass && eventData != null)
            {
                PassEvent(passParam, eventData, function, isClick ? eventData.pointerPress : eventData.pointerDrag);
            }
        }


        // 将事件透传给下一个对象
        private static void PassEvent<T>(PassTriggerParam param, PointerEventData data,
            ExecuteEvents.EventFunction<T> function, GameObject exceptObj) where T : IEventSystemHandler
        {
            // 获取下一个射线检测结果
            var nextResult = StandaloneAdvInputModule.GetNextRaycastResult(data.pointerId, function, param.id, exceptObj);
            var obj = nextResult.gameObject;

            // 有下一个对象的话，将事件传递给它
            if (obj != null)
            {
                // 点击事件还需判断当前对象是否为原点击对象
                if (typeof(T) == typeof(IPointerClickHandler))
                {
                    var canClick = StandaloneAdvInputModule.GetCurPassClickObj(data.pointerId) == obj;
                    if (!canClick)
                    {
                        var customButton = obj.GetComponent<ICustomClick>();
                        if (customButton != null)
                            canClick = customButton.CanTriggerClick(data);
                    }

                    if (!canClick)
                        return;
                }

                data.pointerCurrentRaycast = nextResult;
                ExecuteEvents.ExecuteHierarchy(obj, data, function);
            }
        }

        // PassTrigger所需参数
        [Serializable]
        public class PassTriggerParam
        {
            // Trigger的Id，用于定点透传
            [Tooltip("Trigger的Id，用于定点透传")]
            public int id = 0;

            // 是否处理多点触控
            [Tooltip("是否处理多点触控")]
            public bool dealMultiTouch;

            // 是否自动透传(1. 点击透传给拖拽层 2. 拖拽透传给点击层)
            [Tooltip("是否自动透传(1. 点击透传给拖拽层 2. 拖拽透传给点击层)")]
            public bool isAutoPass;

            [Tooltip("是否直接透传点击(点击透传给点击)")]
            public bool isClickPass;

            [Tooltip("是否直接透传拖动(拖拽透传给拖拽)")]
            public bool isDragPass;
        }
    }
}
