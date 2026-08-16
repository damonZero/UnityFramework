//**************************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description 移动事件处理组件
//**************************************************************************************

using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public class BaseMove : BaseTrigger, IMoveHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // 事件代理
        public AxisEventHandler Move;
        public PointerEventHandler Enter;
        public PointerEventHandler Exit;

        /// <summary>
        /// 进入回调
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsDisable()) return;

            Enter?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 移动回调
        /// </summary>
        /// <param name="eventData"></param>
        public void OnMove(AxisEventData eventData)
        {
            if (IsDisable()) return;

            Move?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 离开回调
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsDisable()) return;

            Exit?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        public override void Clear()
        {
            Move = null;
            Enter = null;
            Exit = null;
        }
    }
}
