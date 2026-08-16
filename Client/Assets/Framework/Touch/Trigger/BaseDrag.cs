//**************************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description 拖拽事件处理组件
//**************************************************************************************

using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public class BaseDrag : PassTrigger, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler
    {
        // 事件代理
        public PointerEventHandler InitializePotentialDrag;
        public PointerEventHandler BeginDrag;
        public PointerEventHandler Drag;
        public PointerEventHandler Drop;
        public PointerEventHandler EndDrag;

        /// <summary>
        /// 初始化潜在拖拽回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            Handle(eventData, InitializePotentialDragCb, ExecuteEvents.initializePotentialDrag, false);
        }

        // 初始化潜在拖拽处理
        private void InitializePotentialDragCb(PointerEventData eventData)
        {
            InitializePotentialDrag?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 开始拖拽回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            Handle(eventData, BeginDragCb, ExecuteEvents.beginDragHandler, false);
        }

        // 开始拖拽处理
        private void BeginDragCb(PointerEventData eventData)
        {
            BeginDrag?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 拖拽回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnDrag(PointerEventData eventData)
        {
            Handle(eventData, DragCb, ExecuteEvents.dragHandler, false);
        }

        // 拖拽处理
        private void DragCb(PointerEventData eventData)
        {
            Drag?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 丢下回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnDrop(PointerEventData eventData)
        {
            Handle(eventData, DropCb, ExecuteEvents.dropHandler, false);
        }

        // 丢下处理
        private void DropCb(PointerEventData eventData)
        {
            Drop?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 结束拖拽回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            Handle(eventData, EndDragCb, ExecuteEvents.endDragHandler, false);
        }

        // 结束拖拽处理
        private void EndDragCb(PointerEventData eventData)
        {
            EndDrag?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        public override void Clear()
        {
            InitializePotentialDrag = null;
            BeginDrag = null;
            Drag = null;
            Drop = null;
            EndDrag = null;
        }
    }
}
