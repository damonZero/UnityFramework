//**************************************************************************************
//Create By fred on 2018/10/11
//
//@Description 触摸事件通用工具集
//**************************************************************************************

using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public static class TouchUtil
    {
        // TriggerId基值
        private static uint _triggerIdBase = 0;

        // 事件系统当前是否禁用
        private static bool _isEventSystemDisable = false;
        // 当前事件系统
        private static EventSystem _currentEventSystem;

        // 当前触摸位置
        public static Vector2 TouchPos
        {
            get
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                if (Input.touchCount == 0)
                    return Vector3.zero;
                return Input.touches[0].position;
#else
                return Input.mousePosition;
#endif
            }
        }

        #region 事件注册接口

        public delegate bool EventCallback(GameObject obj, object eventData, object userData);
        public delegate bool BaseEventCallback(GameObject obj, BaseEventData eventData, object userData);
        public delegate bool AxisEventCallback(GameObject obj, AxisEventData eventData, object userData);
        public delegate bool PointerEventCallback(GameObject obj, PointerEventData eventData, object userData);


        /// =============================================================
        /// 3D物体的事件注册有两点要求：
        ///     1、相机上要有UnityEngine.EventSystems.PhysicsRaycaster
        ///     2、此3D物体上要有Collider
        /// =============================================================

        /// <summary>
        /// 获取下一个TriggerId
        /// </summary>
        public static uint NextTriggerId()
        {
            return ++_triggerIdBase;
        }

        /// <summary>
        /// 给对象加上一个BaseButton
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>添加的BaseButton</returns>
        public static BaseButton AddBaseButton(GameObject gameObject)
        {
            var baseButton = gameObject.GetComponent<BaseButton>();
            if (baseButton == null)
                baseButton = gameObject.AddComponent<BaseButton>();

            return baseButton;
        }

        /// <summary>
        /// 给对象加上一个BaseDrag
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>添加的BaseDrag</returns>
        public static BaseDrag AddBaseDrag(GameObject gameObject)
        {
            var baseDrag = gameObject.GetComponent<BaseDrag>();
            if (baseDrag == null)
                baseDrag = gameObject.AddComponent<BaseDrag>();

            return baseDrag;
        }

        /// <summary>
        /// 设置对象上所有Trigger的禁用状态
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="isDisable">是否禁用</param>
        public static void SetAllEventDisable(GameObject gameObject, bool isDisable)
        {
            var baseTriggers = gameObject.GetComponents<BaseTrigger>();
            foreach (var baseTrigger in baseTriggers)
            {
                baseTrigger.SetDisable(isDisable);
            }
        }

        /// <summary>
        /// 设置事件系统禁用状态
        /// </summary>
        /// <param name="isDisable">是否禁用</param>
        public static void DisableEventSystem(bool isDisable)
        {
            if (isDisable == _isEventSystemDisable) return;

            _isEventSystemDisable = isDisable;
            if (_isEventSystemDisable)
            {
                _currentEventSystem = EventSystem.current;
                _currentEventSystem.enabled = false;
            }
            else
            {
                _currentEventSystem.enabled = true;
                EventSystem.current = _currentEventSystem;
            }
        }

        #endregion

        #region 触摸状态判断接口

        /// <summary>
        /// 判断是否处于触摸结束|鼠标左键抬起状态
        /// </summary>
        /// <returns></returns>
        public static bool IsTouchEnded()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return Input.GetMouseButtonUp(0);
#else
            return (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);
#endif
        }

        public static bool IsTouchPressed()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return Input.GetMouseButton(0);
#else
            return Input.touchCount > 0 &&
                (Input.GetTouch(0).phase == TouchPhase.Stationary || Input.GetTouch(0).phase == TouchPhase.Moved);
#endif
        }

        public static bool IsTouchBegan()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return Input.GetMouseButtonDown(0);
#else
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
        }

        /// <summary>
        /// 判断是否点击在了UI上
        ///
        /// 注意：此函数只有在触摸开始的状态下才能起效
        /// https://docs.unity3d.com/ScriptReference/EventSystems.EventSystem.IsPointerOverGameObject.html
        /// </summary>
        /// <returns></returns>
        public static bool IsTouchBeganOnUI()
        {
            if (!IsTouchBegan()) return false;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return EventSystem.current.IsPointerOverGameObject();
#else
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
        }

        /// <summary>
        /// 判断是否点击在了场景中（没点在UI上）
        ///
        /// 注意：此函数只有在触摸开始的状态下才能起效
        /// </summary>
        /// <returns></returns>
        public static bool IsTouchBeganIntoScene()
        {
            if (!IsTouchBegan()) return false;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return !EventSystem.current.IsPointerOverGameObject();
#else
            return !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
        }

        #endregion
    }

}
