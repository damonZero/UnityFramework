//*****************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description 触摸事件处理组件基类，控制开关
//*****************************************************************************

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Touch
{
    public delegate void EventHandler(GameObject obj, object eventData);
    public delegate void BaseEventHandler(GameObject obj, BaseEventData eventData);
    public delegate void AxisEventHandler(GameObject obj, AxisEventData eventData);
    public delegate void PointerEventHandler(GameObject obj, PointerEventData eventData);
    public delegate void ToggleEventHandler(GameObject obj, bool isDisable);
    public delegate void SlipEventHandler(GameObject obj, PointerEventData eventData, bool dir);

    public class BaseTrigger : MonoBehaviour, IDisable
    {
        // 相关参数
        public BaseTriggerParam baseParam = new BaseTriggerParam();

        /// <summary>
        /// 设置Trigger禁用状态，会先设置状态，再将自身的射线检测关闭
        /// </summary>
        /// <param name="isDisable">是否禁用</param>
        public virtual void SetDisable(bool isDisable)
        {
            SetDisable(gameObject, baseParam, isDisable);
        }

        /// <summary>
        /// 获取Trigger是否禁用
        /// </summary>
        /// <returns></returns>
        public bool IsDisable()
        {
            return baseParam.isDisable;
        }

        private void OnDestroy()
        {
            Clear();
        }

        public virtual void Clear()
        {

        }

        /// <summary>
        /// BaseTrigger所需的参数
        /// </summary>
        [Serializable]
        public class BaseTriggerParam
        {
            // 禁用状态
            public bool isDisable;

            // 对象上接收射线检测的组件
            [HideInInspector]
            public Component raycastComp;
            // 接收射线检测的组件的类型
            [HideInInspector]
            public RaycastCompType raycastCompType;
            // 对象上是否有接收射线检测的组件
            [HideInInspector]
            public bool hasRaycastComp = true;

            // 是否已缓存原始射线状态（运行时缓存，不序列化）
            [NonSerialized] public bool hasCachedRaycastState;
            // 原始射线状态（首次禁用前记录，恢复时还原而非强制开启）
            [NonSerialized] public bool originalRaycastState;

            // 开关事件代理
            public ToggleEventHandler Toggle { get; set; }

            public enum RaycastCompType
            {
                Graphic,      // Graphic组件
                Collider2D,   // Collider2D组件
                Collider      // Collider组件
            };
        }

        /// <summary>
        /// 设置Trigger禁用状态，会先设置状态，再将自身的射线检测关闭
        /// </summary>
        /// <param name="gameObject">对象</param>
        /// <param name="param">参数</param>
        /// <param name="isDisable">是否禁用</param>
        public static void SetDisable(GameObject gameObject, BaseTriggerParam param, bool isDisable)
        {
            if (param.isDisable == isDisable) return;

            // 设置状态
            param.isDisable = isDisable;

            // 开关事件回调
            param.Toggle?.Invoke(gameObject, isDisable);

            // 禁用自身射线检测
            var comp = GetRaycastComp(gameObject, param);
            if (!param.hasRaycastComp)
                return;

            // 首次修改前缓存原始射线状态，恢复时还原而非强制开启
            if (!param.hasCachedRaycastState)
            {
                param.hasCachedRaycastState = true;
                param.originalRaycastState = GetRaycastEnabled(comp, param.raycastCompType);
            }

            SetRaycastEnabled(comp, param.raycastCompType, isDisable ? false : param.originalRaycastState);
        }

        // 获取当前射线接收状态
        private static bool GetRaycastEnabled(Component comp, BaseTriggerParam.RaycastCompType type)
        {
            switch (type)
            {
                case BaseTriggerParam.RaycastCompType.Graphic:
                    return ((Graphic)comp).raycastTarget;
                case BaseTriggerParam.RaycastCompType.Collider2D:
                    return ((Collider2D)comp).enabled;
                case BaseTriggerParam.RaycastCompType.Collider:
                    return ((Collider)comp).enabled;
                default:
                    return false;
            }
        }

        // 设置射线接收状态
        private static void SetRaycastEnabled(Component comp, BaseTriggerParam.RaycastCompType type, bool enabled)
        {
            switch (type)
            {
                case BaseTriggerParam.RaycastCompType.Graphic:
                    ((Graphic)comp).raycastTarget = enabled;
                    break;
                case BaseTriggerParam.RaycastCompType.Collider2D:
                    ((Collider2D)comp).enabled = enabled;
                    break;
                case BaseTriggerParam.RaycastCompType.Collider:
                    ((Collider)comp).enabled = enabled;
                    break;
            }
        }

        // 获取对象上接收射线检测的组件
        public static Component GetRaycastComp(GameObject gameObject, BaseTriggerParam param)
        {
            if (!param.hasRaycastComp)
                return null;

            var raycastComp = param.raycastComp;
            if (raycastComp != null)
                return raycastComp;

            // 依次尝试获取Graphic、Collider2D、Collider组件
            raycastComp = gameObject.GetComponent<Graphic>();
            if (raycastComp == null)
            {
                raycastComp = gameObject.GetComponent<Collider2D>();
                if (raycastComp == null)
                {
                    raycastComp = gameObject.GetComponent<Collider>();
                    if (raycastComp != null)
                        param.raycastCompType = BaseTriggerParam.RaycastCompType.Collider;
                }
                else
                    param.raycastCompType = BaseTriggerParam.RaycastCompType.Collider2D;
            }
            else
                param.raycastCompType = BaseTriggerParam.RaycastCompType.Graphic;

            if (raycastComp != null)
            {
                param.raycastComp = raycastComp;
                return raycastComp;
            }

            param.hasRaycastComp = false;

            return null;
        }
    }
}
