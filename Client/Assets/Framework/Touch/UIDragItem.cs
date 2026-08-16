//**************************************************************************************
//Create By szx on 2022/04/19.
//
//@Description 通用UI拖拽节点, 和UIDragItemSlot配合使用
//**************************************************************************************

using System;
using Framework.Log;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Touch
{
    [RequireComponent(typeof(BaseDrag))]
    public class UIDragItem : MonoBehaviour
    {
        [Header("克隆原物体进行拖拽")] public bool isCloneDrag = true;

        [Header("拖拽时是否显示到最上层")] public bool isTopDrag = true;

        [Header("指定拖拽物体父节点")] public Transform appointParent = null;

        [Header("拖拽限定范围")] public RectTransform dragRange;

        [Header("拖拽结束后是否恢复原位(克隆拖拽不生效)")] public bool isRecoverPos = false;

        [Header("是否拖拽透传下去（默认透传）")] public bool isPassDrag = true;

        /// <summary>
        /// 自身RectTransform
        /// </summary>
        private RectTransform _rt;

        /// <summary>
        /// 拖拽节点在拖拽过程中的父节点
        /// </summary>
        private RectTransform _dragParent;

        /// <summary>
        /// 被拖拽的节点的RectTransform
        /// </summary>
        private RectTransform _dragRt;

        /// <summary>
        /// 拖拽开始前，原始的父节点
        /// </summary>
        private RectTransform _originalParent;

        /// <summary>
        /// 拖拽开始前，原始索引
        /// </summary>
        private int _originalSlibling;

        /// <summary>
        /// 拖拽开始前，原始位置
        /// </summary>
        private Vector3 _originalPos;

        /// <summary>
        /// 外部传递参数，可自由设置，最终会传入DragItemSlot的OnItemDrop事件中
        /// </summary>
        public object PassParam { get; set; }

        /// <summary>
        /// 标记，避免同一个界面，多组拖拽节点混淆
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// 开始拖拽事件
        /// </summary>
        public PointerEventHandler BeginDrag;

        /// <summary>
        /// 拖拽覆盖事件
        /// </summary>
        public PointerEventHandler DragCover;

        /// <summary>
        /// 拖拽放下事件
        /// </summary>
        public PointerEventHandler OnDrop;

        private void Awake()
        {
            _rt = transform as RectTransform;
            var drag = GetComponent<BaseDrag>();
            drag.BeginDrag = OnBeginDrag;
            drag.Drag = OnDrag;
            drag.EndDrag = OnEndDrag;
            // 穿透由 IsDragPass 属性控制（事件回调不再返回穿透 bool）
            drag.IsDragPass = isPassDrag;
        }

        private void OnDestroy()
        {
            PassParam = null;
        }

        private void OnBeginDrag(GameObject go, PointerEventData eventData)
        {
            _originalSlibling = _rt.GetSiblingIndex();
            _originalParent = _rt.parent as RectTransform;
            _originalPos = _rt.localPosition;


            if (isCloneDrag)
            {
                var cloneGo = Instantiate(gameObject, transform.parent);
                _dragRt = cloneGo.GetComponent<RectTransform>();
                _dragRt.position = _rt.position;
                _dragRt.SetSiblingIndex(_originalSlibling + 1);
            }
            else
            {
                _dragRt = _rt;
            }

            if (appointParent != null)
            {
                //如果指定了拖拽节点的父节点，则进行设置
                _dragParent = appointParent as RectTransform;
                _dragRt.SetParent(_dragParent);
            }
            else if (isTopDrag)
            {
                //如果拖拽时需要显示到最上层，则将拖拽父节点设为上层的canvas
                //通常项目中是一个界面一个Canvas，当然也有例外，如粒子UI混排的时候，遇到了再处理 TODO
                var canvas = GetComponentInParent<Canvas>();
                _dragParent = canvas.transform as RectTransform;
                _dragRt.SetParent(_dragParent);
            }
            else
            {
                _dragParent = _rt.parent as RectTransform;
            }

            BeginDrag?.Invoke(gameObject, eventData);
        }

        private void OnDrag(GameObject go, PointerEventData eventData)
        {
            if (_dragRt == null || _dragParent == null)
                return;

            Vector2 pos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragParent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pos))
            {
                var size = _rt.sizeDelta;
                var pivot = _rt.pivot;
                var offset = new Vector2(size.x * (pivot.x - 0.5f), size.y * (pivot.y - 0.5f));
                _dragRt.localPosition = pos + offset;
                _LimitDragRange();
                // 检测当前首个可替换的对象
                var results = RaycastResultListPool.Get();
                try
                {
                    EventSystem.current.RaycastAll(eventData, results);
                    GameObject coverObj = null;
                    foreach (var result in results)
                    {
                        var slot = result.gameObject.GetComponent<UIDragItemSlot>();
                        if (slot != null && slot.Tag == Tag)
                        {
                            coverObj = slot.gameObject;
                            slot.HandleReceiveItemDragCover(this);
                            break;
                        }
                    }

                    DragCover?.Invoke(coverObj, eventData);
                }
                finally
                {
                    RaycastResultListPool.Release(results);
                }
            }
        }


        private void OnEndDrag(GameObject go, PointerEventData eventData)
        {
            if (_dragRt == null)
                return;
            _LimitDragRange();
            try
            {
                var results = RaycastResultListPool.Get();
                EventSystem.current.RaycastAll(eventData, results);
                foreach (var result in results)
                {
                    var slot = result.gameObject.GetComponent<UIDragItemSlot>();
                    if (slot != null)
                    {
                        slot.HandleReceiveItem(this);
                        break;
                    }
                }

                RaycastResultListPool.Release(results);
                OnDrop?.Invoke(gameObject, eventData);
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "UIDragItem.OnEndDrag failed", module: "Framework.Touch");
            }

            if (isCloneDrag)
            {
                Destroy(_dragRt.gameObject);
            }
            else
            {
                if (isTopDrag)
                {
                    // Debug.Log($"drag end{gameObject.name}");
                    _dragRt.SetParent(_originalParent);
                    _dragRt.SetSiblingIndex(_originalSlibling);
                }

                if (isRecoverPos)
                    _dragRt.localPosition = _originalPos;
            }

            _dragRt = null;
            _dragParent = null;
        }

        /// <summary>
        /// 将拖拽节点限制在拖拽范围内
        /// </summary>
        private void _LimitDragRange()
        {
            if (dragRange == null)
                return;

            // 将拖拽节点的世界坐标转换到 dragRange 的本地坐标系
            Vector3 localPos = dragRange.InverseTransformPoint(_dragRt.position);

            // 使用 rect 属性获取限制范围（rect 总是最新的）
            var rect = dragRange.rect;
            var min = rect.min;
            var max = rect.max;

            // 限制位置
            localPos.x = Mathf.Clamp(localPos.x, min.x, max.x);
            localPos.y = Mathf.Clamp(localPos.y, min.y, max.y);

            // 转换回世界坐标
            _dragRt.position = dragRange.TransformPoint(localPos);
        }

        /// <summary>
        /// 设置拖拽节点禁用状态
        /// </summary>
        /// <param name="isDisable">是否禁用</param>
        public void SetDisable(bool isDisable)
        {
            var drag = GetComponent<BaseDrag>();
            drag.SetDisable(isDisable);
            drag.IsDragPass = isDisable;
            drag.IsClickPass = isDisable;
        }

        /// <summary>
        /// 拖拽是否禁用
        /// </summary>
        /// <returns></returns>
        public bool IsDisable()
        {
            var drag = GetComponent<BaseDrag>();
            return drag.IsDisable();
        }


    }
}
