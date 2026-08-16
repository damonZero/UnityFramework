//**************************************************************************************
//Create By szx on 2022/04/19.
//
//@Description 通用拖拽节点槽，和DragItem配合使用
//**************************************************************************************

using System;
using UnityEngine;

namespace Framework.Touch
{
    public class UIDragItemSlot : MonoBehaviour
    {
        /// <summary>
        /// 拖拽节点放入槽中事件
        /// 参数1：DragItem的gameObject
        /// 参数2: DragItem的PassParam
        /// </summary>
        public Action<GameObject, object> OnReceiveItem;
        public Action<GameObject, object> OnReceiveItemDragCover;

        /// <summary>
        /// 标记类型，避免多个不同类型槽位混淆
        /// </summary>
        public int Tag { get; set; }

        public void HandleReceiveItem(UIDragItem item)
        {
            if (item.Tag != Tag) return;
            OnReceiveItem?.Invoke(item.gameObject, item.PassParam);
        }

        public void HandleReceiveItemDragCover(UIDragItem item)
        {
            if (item.Tag != Tag) return;
            OnReceiveItemDragCover?.Invoke(item.gameObject, item.PassParam);
        }

        private void OnDestroy()
        {
            OnReceiveItem = null;
            OnReceiveItemDragCover = null;
        }
    }
}
