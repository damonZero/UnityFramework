//**************************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description 按钮基类，提供了对点击的相关支持
//**************************************************************************************

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Touch
{
    [DisallowMultipleComponent]
    public class BaseButton : PassTrigger, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
    {
        // 事件代理
        public PointerEventHandler Click { get; set; }
        public PointerEventHandler Up { get; set; }
        public PointerEventHandler Down { get; set; }
        public EventHandler LongPress { get; set; }
        public EventHandler LongPressUpdate { get; set; }
        public EventHandler LongPressStop { get; set; }
        public EventHandler LongPressBegin { get; set; }

        // 点击移动容差，大于0且小于拖动阈值才有效
        public float moveDelta = 0;

        // 长按响应时间
        public float longPressTime = 1;

        // 长按持续更新时间
        public float longPressUpdateTime = 0;

        // 长按开始响应时间
        public float longPressBeginTime = 0.1f;

        // 是否长按
        [HideInInspector] public bool isLongPress = false;

        // 开始按压时间
        private float _startDownTime = 0;

        // 长按协程
        protected Coroutine _longPressCor;

        // 停止长按
        protected bool _isStopLongPress = false;

        // 缓存 WaitForSeconds，复用避免每次按压产生 GC 分配
        private WaitForSeconds _longPressBeginWait;
        private WaitForSeconds _longPressWait;
        private WaitForSeconds _longPressUpdateWait;

        /// <summary>
        /// 设置事件禁用状态
        /// </summary>
        /// <param name="isDisable">是否禁用</param>
        public virtual void SetEventDisable(bool isDisable)
        {
            SetDisable(isDisable);
        }

        /// <summary>
        /// 点击回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            // 若已经触发长按了，则不再触发点击
            if (isLongPress)
                return;

            Handle(eventData, PointerClickCb, ExecuteEvents.pointerClickHandler, true);
        }

        // 点击处理
        private void PointerClickCb(PointerEventData eventData)
        {
            if (Click == null)
                return;

            // 移动容差判定
            if (moveDelta > 0)
            {
                var dis = Vector3.Distance(eventData.pressPosition, eventData.position);
                if (dis > moveDelta)
                    return;
            }

            // 穿透由 IsClickPass 属性控制，Click 委托不返回穿透信号
            Click(gameObject, eventData);
        }

        /// <summary>
        /// 按下回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            Handle(eventData, PointerDownCb, ExecuteEvents.pointerDownHandler, true);
        }

        // 按下处理
        protected void PointerDownCb(PointerEventData eventData)
        {
            isLongPress = false;
            _isStopLongPress = false;

            // 启用协程判定是否长按及长按更新
            //fix add (_longPressCor == null),透传时，如果按钮下方存在多个射线接收，
            //会存在同时开启多个协程，但只会关闭最后赋值的一个协程
            if ((LongPress != null || LongPressUpdate != null || LongPressBegin != null) && gameObject.activeInHierarchy)
            {
                _startDownTime = Time.time;
                //fix 存在可能上一个协程未销毁，没有停止的情况，这里做个检测
                if (_longPressCor != null)
                {
                    StopCoroutine(_longPressCor);
                    _longPressCor = null;
                }

                _longPressCor = StartCoroutine(LongPressJudge());
            }

            Down?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 弹起回调
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            // 中途停止的话则已经执行过了，不用再处理
            if (_isStopLongPress) return;

            // 停止长按判定协程
            if (_longPressCor != null)
            {
                if (isLongPress)
                    LongPressStop?.Invoke(gameObject, eventData);
                StopCoroutine(_longPressCor);
                _longPressCor = null;
                isLongPress = false;
            }

            Handle(eventData, PointerUpCb, ExecuteEvents.pointerUpHandler, true);
        }

        // 弹起处理
        protected void PointerUpCb(PointerEventData eventData)
        {
            Up?.Invoke(gameObject, eventData);
        }

        /// <summary>
        /// 停止长按，并立即触发 Up 事件（该事件不会透传）。
        /// 仅当存在进行中的按压/长按时才触发，避免对未按下的按钮误触发 Up(null)。
        /// </summary>
        public void StopLongPress()
        {
            if (_longPressCor == null)
                return;

            _isStopLongPress = true;

            if (isLongPress)
                LongPressStop?.Invoke(gameObject, null);
            StopCoroutine(_longPressCor);
            _longPressCor = null;
            isLongPress = false;

            Up?.Invoke(gameObject, null);
        }

        /// <summary>
        /// 长按判定协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator LongPressJudge()
        {
            // 复用缓存的 WaitForSeconds，避免每次按压产生 GC 分配
            if (_longPressBeginWait == null)
                _longPressBeginWait = new WaitForSeconds(longPressBeginTime);
            yield return _longPressBeginWait;

            // 有则执行长按开始回调
            if (LongPressBegin != null)
            {
                LongPressBegin(gameObject, Time.time - _startDownTime);
            }

            // 若开始回调时间早于响应时间，则补足剩余等待，使从按下到触发长按的总时长 = longPressTime
            if (longPressBeginTime < longPressTime)
            {
                if (_longPressWait == null)
                    _longPressWait = new WaitForSeconds(longPressTime - longPressBeginTime);
                yield return _longPressWait;
            }

            // 有则执行长按回调
            if (LongPress != null)
            {
                isLongPress = true;
                LongPress(gameObject, Time.time - _startDownTime);
            }

            if (LongPressUpdate != null)
            {
                while (true)
                {
                    // 长按更新回调
                    isLongPress = true;
                    LongPressUpdate(gameObject, Time.time - _startDownTime);

                    // 等待长按更新时间
                    if (_longPressUpdateWait == null)
                        _longPressUpdateWait = new WaitForSeconds(longPressUpdateTime);
                    yield return _longPressUpdateWait;
                }
            }

            _longPressCor = null;
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        public override void Clear()
        {
            Click = null;
            Up = null;
            Down = null;
            LongPress = null;
            LongPressUpdate = null;
            LongPressBegin = null;
            LongPressStop = null;
            isLongPress = false;
            _isStopLongPress = false;
            StopLongPressCor();
        }

        public void StopLongPressCor()
        {
            if (_longPressCor != null)
            {
                StopCoroutine(_longPressCor);
                _longPressCor = null;
            }
        }
    }
}
