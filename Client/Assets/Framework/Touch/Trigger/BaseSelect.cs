//**************************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description  选中事件处理组件
//**************************************************************************************

using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public class BaseSelect : BaseTrigger, ISelectHandler, IUpdateSelectedHandler, IDeselectHandler
    {
        // 事件代理
        public BaseEventHandler Select;
        public BaseEventHandler UpdateSelect;
        public BaseEventHandler Deselect;

        /// <summary>
        /// 选中回调
        /// </summary>
        /// <param name="eventData"></param>
        public void OnSelect(BaseEventData eventData)
        {
            if (IsDisable()) return;

            Select?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 选中更新回调
        /// </summary>
        /// <param name="eventData"></param>
        public void OnUpdateSelected(BaseEventData eventData)
        {
            if (IsDisable()) return;

            UpdateSelect?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 取消选中回调
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDeselect(BaseEventData eventData)
        {
            if (IsDisable()) return;

            Deselect?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        public override void Clear()
        {
            Select = null;
            UpdateSelect = null;
            Deselect = null;
        }
    }
}
