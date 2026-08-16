//**************************************************************************************
//Create By wensx on 2020/03/30
//
//@Description  对Unity原生StandaloneInputModule的重写，以更满足项目的需求
//**************************************************************************************

using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Touch
{
    public class StandaloneAdvInputModule : StandaloneBaseInputModule
    {
        // 是否处理鼠标右键、中键
        public static bool isHandleRightAndMiddleButton = false;
        // 连续点击间隔时间（秒）
        public static readonly float continuousClickInterval = 0.3f;

        // 射线检测结果缓存字典，每帧更新
        private static readonly Dictionary<int, List<RaycastResult>> _raycastResultDic = new Dictionary<int, List<RaycastResult>>();
        // 透传列表缓存字典，用于透传
        private static readonly Dictionary<int, List<RaycastResult>> _passDic = new Dictionary<int, List<RaycastResult>>();

        // 初始索引字典，用于指定不同事件的初始索引
        private static readonly Dictionary<int, Dictionary<object, int>> _initIndexDic = new Dictionary<int, Dictionary<object, int>>();
        // 索引缓存字典，用于分隔不同事件的透传，每帧都会重置
        private static readonly Dictionary<int, Dictionary<object, int>> _indexDic = new Dictionary<int, Dictionary<object, int>>();
        // 点击事件索引字典，用于判定当前透传点击对象是否为原透传点击对象
        private static readonly Dictionary<int, int> _clickIndexDic = new Dictionary<int, int>();

        // 鼠标状态，声明为字段以重复使用，避免频繁创建与销毁
        private readonly MouseState _mouseState = new MouseState();

        /// <summary>
        /// 获取下一个射线检测结果
        /// </summary>
        /// <param name="pointerId">事件Id</param>
        /// <param name="function">事件函数</param>
        /// <param name="passId">透传Id，表示要定点透传的Id</param>
        /// <param name="exceptObj">排除的节点，如果不排除可能会触发多次</param>
        /// <returns>下一个射线检测结果</returns>
        public static RaycastResult GetNextRaycastResult(int pointerId, object function, int passId, GameObject exceptObj)
        {
            _passDic.TryGetValue(pointerId, out var resultList);
            if (resultList == null || resultList.Count <= 0)
                return new RaycastResult();

            var hasIndex = true;
            if (!_indexDic.TryGetValue(pointerId, out var curIndexDic))
            {
                if (!_initIndexDic.TryGetValue(pointerId, out curIndexDic))
                    hasIndex = false;
            }

            if (!hasIndex || curIndexDic == null)
            {
                curIndexDic = IndexDictionaryPool.Get();
                _indexDic[pointerId] = curIndexDic;
            }

            curIndexDic.TryGetValue(function, out var index);

            if (index >= resultList.Count)
                return new RaycastResult();

            // 寻找第一个gameObject有值的结果
            GameObject obj = null;
            for (; index < resultList.Count; ++index)
            {
                obj = resultList[index].gameObject;
                if (obj != null && obj != exceptObj)
                    break;
            }

            // 没找到则返回一个空结果
            if (index >= resultList.Count)
                return new RaycastResult();

            // 若是定点透传，还需判断该对象的TriggerId是否为透传Id
            if (passId > 0)
            {
                var passTrigger = obj.GetComponent<PassTrigger>();
                if (passTrigger == null || passTrigger.passParam.id != passId)
                    return new RaycastResult();
            }

            // 满足条件则返回结果
            curIndexDic[function] = index + 1;
            return resultList[index];
        }

        /// <summary>
        /// 获取当前透传点击对象
        /// </summary>
        /// <returns>点击透传对象</returns>
        public static GameObject GetCurPassClickObj(int pointerId)
        {
            var hasResult = _raycastResultDic.TryGetValue(pointerId, out var raycastList);
            if (!hasResult || raycastList == null || raycastList.Count == 0)
                return null;

            _clickIndexDic.TryGetValue(pointerId, out var clickIndex);
            if (clickIndex >= raycastList.Count)
                return null;

            for (; clickIndex < raycastList.Count; ++clickIndex)
            {
                var obj = raycastList[clickIndex].gameObject;
                if (obj != null)
                {
                    _clickIndexDic[pointerId] = clickIndex + 1;
                    return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取下一个处理对象
        /// </summary>
        /// <param name="pointerId">事件Id</param>
        /// <param name="isClick">是否点击</param>
        /// <returns>下一个处理对象</returns>
        private static GameObject GetNextHandler(int pointerId, bool isClick)
        {
            _raycastResultDic.TryGetValue(pointerId, out var resultList);
            if (resultList == null || resultList.Count <= 0)
                return null;

            // 寻找第一个能处理的对象
            for (var index = 0; index < resultList.Count; ++index)
            {
                var obj = resultList[index].gameObject;
                if (obj == null) continue;

                var pointerPress = ExecuteEvents.GetEventHandler<IPointerDownHandler>(obj);
                if (pointerPress == null)
                    pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(obj);

                var pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(obj);

                var hasPress = pointerPress != null;
                var hasDrag = pointerDrag != null;

                if (!hasPress && !hasDrag)
                    return null;

                var canPress = isClick && hasPress;
                var canDrag = !isClick && hasDrag;
                if (canPress || canDrag)
                {
                    var initIndex = index + 1;
                    var hasIndex = _initIndexDic.TryGetValue(pointerId, out var curIndexDic);
                    if (!hasIndex || curIndexDic == null)
                    {
                        curIndexDic = IndexDictionaryPool.Get();
                        _initIndexDic[pointerId] = curIndexDic;
                    }

                    if (canPress)
                    {
                        curIndexDic[ExecuteEvents.pointerDownHandler] = initIndex;
                        curIndexDic[ExecuteEvents.pointerClickHandler] = initIndex;
                        curIndexDic[ExecuteEvents.pointerUpHandler] = initIndex;

                        return pointerPress;
                    }

                    curIndexDic[ExecuteEvents.initializePotentialDrag] = initIndex;
                    curIndexDic[ExecuteEvents.beginDragHandler] = initIndex;
                    curIndexDic[ExecuteEvents.dragHandler] = initIndex;
                    curIndexDic[ExecuteEvents.dropHandler] = initIndex;
                    curIndexDic[ExecuteEvents.endDragHandler] = initIndex;

                    return pointerDrag;
                }

                var passTrigger = obj.GetComponent<PassTrigger>();
                if (passTrigger != null && !passTrigger.passParam.isAutoPass)
                    return null;
            }

            return null;
        }

        // 重写的方法，修改了射线检测处的逻辑，对射线检测结果进行了缓存
        protected override PointerEventData GetTouchPointerEventData(UnityEngine.Touch inputTouch, out bool pressed, out bool released)
        {
            var created = GetPointerData(inputTouch.fingerId, out var pointerData, true);
            pointerData.Reset();

            pressed = created || (inputTouch.phase == TouchPhase.Began);
            released = (inputTouch.phase == TouchPhase.Canceled) || (inputTouch.phase == TouchPhase.Ended);

            if (created)
                pointerData.position = inputTouch.position;

            pointerData.delta = pressed ? Vector2.zero : inputTouch.position - pointerData.position;
            pointerData.position = inputTouch.position;
            pointerData.button = PointerEventData.InputButton.Left;
            pointerData.pointerCurrentRaycast = inputTouch.phase == TouchPhase.Canceled ? new RaycastResult() : GetRaycastResult(pointerData);

            return pointerData;
        }

        // 重写的方法，修改了获取pointerDrag、pointerPress的逻辑，添加了自动透传机制
        protected override void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var pointerId = pointerEvent.pointerId;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            if (pressed)
            {
                // 初始化透传相关数据
                InitPass(pointerId);

                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                pointerEvent.pointerPress = newPressed;
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                // 自动透传处理
                HandleAutoPass(currentOverGo, pointerEvent, newPressed,
                    ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo),
                    out var realPressed, out var realDrag);
                pointerEvent.pointerPress = realPressed;
                pointerEvent.pointerDrag = realDrag;

                var time = Time.unscaledTime;
                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.rawPointerPress = currentOverGo;
                pointerEvent.clickTime = time;

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

                InputPointerEvent = pointerEvent;
            }

            if (released)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                if (CanClick(currentOverGo, pointerEvent))
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;

                InputPointerEvent = pointerEvent;

                // 清理透传相关数据
                ClearPass(pointerId);
            }
        }

        // 判断能否触发点击
        private static bool CanClick(GameObject currentGo, PointerEventData eventData)
        {
            var canClick = false;
            if (eventData.eligibleForClick && eventData.pointerPress != null)
            {
                var clickGo = GetClickHandler(eventData.pointerId, currentGo, 0);
                canClick = eventData.pointerPress == clickGo;

                // eventData.pointerPress 是按下那一刻的 GameObject，currentGo 是当前位置的 GameObject；
                // 若按下后未抬起前又打开了新界面，两者不是同一对象，则不应触发点击。
                // （ICustomClick 的自定义点击判定已在 PassEvent 中处理，此处不再重复。）
            }

            return canClick;
        }

        // 初始化透传相关数据
        private static void InitPass(int pointerId)
        {
            // 更新透传字典
            var passList = RaycastResultListPool.Get();
            if (_raycastResultDic.TryGetValue(pointerId, out var resultList))
                passList.AddRange(resultList);
            _passDic[pointerId] = passList;
        }

        // 清理透传相关数据
        private static void ClearPass(int pointerId)
        {
            // 清理初始索引
            if (_initIndexDic.TryGetValue(pointerId, out var indexDic))
            {
                _initIndexDic.Remove(pointerId);
                IndexDictionaryPool.Release(indexDic);
            }
            // 清理透传列表
            if (_passDic.TryGetValue(pointerId, out var passList))
            {
                _passDic.Remove(pointerId);
                RaycastResultListPool.Release(passList);
            }
        }

        // 重写的方法，修改了射线检测处的逻辑，对射线检测结果进行了缓存
        protected override MouseState GetMousePointerEventData(int id)
        {
            var created = GetPointerData(kMouseLeftId, out var leftData, true);
            leftData.Reset();

            if (created)
                leftData.position = input.mousePosition;

            var inputCache = input;
            var pos = inputCache.mousePosition;
            var isLock = Cursor.lockState == CursorLockMode.Locked;
            leftData.delta = isLock ? Vector2.zero : pos - leftData.position;
            leftData.position = isLock ? new Vector2(-1.0f, -1.0f) : pos;

            leftData.scrollDelta = inputCache.mouseScrollDelta;
            leftData.button = PointerEventData.InputButton.Left;
            leftData.pointerCurrentRaycast = GetRaycastResult(leftData);

            _mouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);

            if (isHandleRightAndMiddleButton)
            {
                GetPointerData(kMouseRightId, out var rightData, true);
                CopyFromTo(leftData, rightData);
                rightData.button = PointerEventData.InputButton.Right;

                GetPointerData(kMouseMiddleId, out var middleData, true);
                CopyFromTo(leftData, middleData);
                middleData.button = PointerEventData.InputButton.Middle;

                _mouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
                _mouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);
            }

            return _mouseState;
        }

        // 重写的方法，添加了右键、中键事件处理开关
        protected override void ProcessMouseEvent(int id)
        {
            var mouseData = GetMousePointerEventData(id);
            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

            CurrentFocusedGameObject = leftButtonData.buttonData.pointerCurrentRaycast.gameObject;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            if (isHandleRightAndMiddleButton)
            {
                ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
                ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
                ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
                ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);
            }

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
        }

        // 重写的方法，修改了获取pointerDrag、pointerPress的逻辑，添加了自动透传机制
        protected override void ProcessMousePress(MouseButtonEventData data)
        {
            var pointerEvent = data.buttonData;
            var pointerId = pointerEvent.pointerId;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            if (data.PressedThisFrame())
            {
                // 初始化透传相关数据
                InitPass(pointerId);

                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // 自动透传处理
                HandleAutoPass(currentOverGo, pointerEvent, newPressed,
                    ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo),
                    out var realPressed, out var realDrag);
                pointerEvent.pointerPress = realPressed;
                pointerEvent.pointerDrag = realDrag;

                var time = Time.unscaledTime;
                if (pointerEvent.pointerPress == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < continuousClickInterval)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.rawPointerPress = currentOverGo;
                pointerEvent.clickTime = time;

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

                InputPointerEvent = pointerEvent;
            }

            if (data.ReleasedThisFrame())
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                if (CanClick(currentOverGo, pointerEvent))
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                if (currentOverGo != pointerEvent.pointerEnter)
                {
                    HandlePointerExitAndEnter(pointerEvent, null);
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                }

                InputPointerEvent = pointerEvent;

                // 清理透传相关数据
                ClearPass(pointerId);
            }
        }


        // 获取PassTrigger
        private static PassTrigger GetPassTrigger(GameObject go)
        {
            if (go == null)
                return null;
            return go.GetComponent<PassTrigger>();
        }

        // 处理自动透传
        /*
        2025年7月24日16:40:39 修复：
        1. 整理透传逻辑，确认isAutoPass作用：1.点击透传给拖拽层，2.拖拽透传给点击层
        2. 当前传入的press和drag代表系统当前选中的事件处理对象，根据当前对象来判断是否需要透传，不是currentGo(鼠标点击到的对象，只是raycast为true)
        */
        private static void HandleAutoPass(GameObject currentGo, PointerEventData eventData,
            GameObject press, GameObject drag, out GameObject newPress, out GameObject newDrag)
        {
            newPress = press;
            newDrag = drag;

            if (press != null && drag != null)
            {
                return;
            }

            var passTrigger = GetPassTrigger(press) ?? GetPassTrigger(drag);
            if (passTrigger == null)
                return;

            if (passTrigger.baseParam.isDisable)
            {
                return;
            }

            if (!passTrigger.passParam.isAutoPass)
            {
                return;
            }

            // 点击
            if (drag != null)
            {
                newPress = GetNextHandler(eventData.pointerId, true);
                if (newPress != null)
                    ExecuteEvents.Execute(newPress, eventData, ExecuteEvents.pointerDownHandler);
            }
            else if (press != null)
            {
                newDrag = GetNextHandler(eventData.pointerId, false);
            }
        }

        // 获取对象的实际处理点击的对象
        private static GameObject GetClickHandler(int pointerId, GameObject obj, int index)
        {
            var clickComp = ExecuteEvents.GetEventHandler<IPointerClickHandler>(obj);
            if (clickComp != null)
                return clickComp;

            var dragComp = ExecuteEvents.GetEventHandler<IDragHandler>(obj);
            if (dragComp == null)
                return null;

            _raycastResultDic.TryGetValue(pointerId, out var resultList);
            if (resultList == null || resultList.Count <= index)
                return null;

            for (; index < resultList.Count; ++index)
            {
                var resultObj = resultList[index].gameObject;
                if (resultObj != null)
                    return GetClickHandler(pointerId, resultObj, index + 1);
            }

            return null;
        }

        // 重写的方法，取消了点击对象和拖动对象不是同一个对象时才取消点击的判断
        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            if (!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
                return;

            if (!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            if (pointerEvent.dragging)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        // 直接搬过来的方法，并未修改逻辑，因为其是私有的，这里访问不到
        private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        // 进行射线检测，并将检测结果列表以事件参数为Key进行缓存
        private RaycastResult GetRaycastResult(PointerEventData eventData)
        {
            var resultCache = RaycastResultListPool.Get();
            eventSystem.RaycastAll(eventData, resultCache);
            _raycastResultDic[eventData.pointerId] = resultCache;

            for (var i = 0; i < resultCache.Count; ++i)
            {
                if (resultCache[i].gameObject == null) continue;

                var result = resultCache[i];
                resultCache.RemoveRange(0, i + 1);
                return result;
            }

            return new RaycastResult();
        }

        // 清理索引
        private void LateUpdate()
        {
            foreach (var resultCache in _raycastResultDic.Values)
                RaycastResultListPool.Release(resultCache);
            foreach (var resultCache in _indexDic.Values)
                IndexDictionaryPool.Release(resultCache);
            _raycastResultDic.Clear();
            _indexDic.Clear();
            _clickIndexDic.Clear();
        }

        /// <summary>
        /// 清理环境（释放静态缓存，避免场景卸载后残留）
        /// </summary>
        public static void ShutDown()
        {
            foreach (var resultCache in _raycastResultDic.Values)
                RaycastResultListPool.Release(resultCache);
            foreach (var indexDic in _indexDic.Values)
                IndexDictionaryPool.Release(indexDic);
            foreach (var indexDic in _initIndexDic.Values)
                IndexDictionaryPool.Release(indexDic);
            foreach (var passList in _passDic.Values)
                RaycastResultListPool.Release(passList);

            _raycastResultDic.Clear();
            _passDic.Clear();
            _initIndexDic.Clear();
            _indexDic.Clear();
            _clickIndexDic.Clear();
        }
    }
}
