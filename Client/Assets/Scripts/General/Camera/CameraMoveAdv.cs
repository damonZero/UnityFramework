using System;
using Framework.Touch;
using UnityEngine;
using UnityEngine.EventSystems;

namespace General
{
    /// <summary>
    /// 场景相机控制（精简拆分版）。拖拽/滚轮操控相机，支持平移/环视/旋转/环绕/缩放/惯性/定位移动。
    /// 对应参考项目 Core/UI/Util/CameraMoveAdv.cs，但：
    /// 1. 按功能拆分为多个 partial 文件，每个功能一个 [SerializeField] bool 开关，Inspector 勾选启用；
    /// 2. 精简去掉多边形限位（LimitPosList）、分辨率适配、Ground 射线等 37 游戏特定逻辑。
    /// 本文件为「核心」：输入分发、公共字段、轴定义与公共辅助。
    /// </summary>
    public partial class CameraMoveAdv : BaseDrag, IScrollHandler
    {
        public static CameraMoveAdv Inst { get; set; }

        public delegate void MoveCallBack(Vector3 pos);

        /// <summary>移动轴类型。</summary>
        public enum Axis
        {
            X = 0,        // 世界 X
            Y = 1,        // 世界 Y
            Z = 2,        // 世界 Z
            LocalX = 3,   // 本地 X
            LocalY = 4,   // 本地 Y
            LocalZ = 5,   // 本地 Z
            WorldLocalZ = 6 // 世界水平面上与 localX 垂直的 Z
        }

        [Header("相机")]
        [SerializeField] private GameObject cam;
        [SerializeField] private float camDistance = 93f;
        [SerializeField] private GameObject lookCenter;

        [Header("拖动轴")]
        [SerializeField] private Axis touchX = Axis.LocalX;
        [SerializeField] private Axis touchY = Axis.LocalY;
        [SerializeField] private bool reverseRotate;

        [Header("倍率")]
        [SerializeField] private float boostX = 2f;
        [SerializeField] private float boostY = 2f;
        [SerializeField] private float boostZoom = 1f;

        [Header("拖动限位")]
        [SerializeField] private float minOffsetX = -50f;
        [SerializeField] private float maxOffsetX = 50f;
        [SerializeField] private float minOffsetY = -50f;
        [SerializeField] private float maxOffsetY = 50f;

        [Header("缩放限位")]
        [SerializeField] private float minZoom = -50f;
        [SerializeField] private float maxZoom = 50f;

        [Header("输入")]
        [SerializeField] private bool allowAll = true;
        [SerializeField] private bool allowTouchX = true;
        [SerializeField] private bool allowTouchY = true;

        [Header("定位移动")]
        [SerializeField] private float fixedMoveSpeed = 200f;

        // 世界水平面上与 localX 垂直的 Z
        private Vector3 worldLocalZ;
        private bool mouseMode;
        private Camera cameraComp;

        // 拖动/缩放累计值（与限位配合）
        private float offsetX;
        private float offsetY;
        private float offsetZoom;
        private float touchDistance;

        // 惯性状态
        private bool underInertia;
        private float inertiaTime;
        private Vector3 velocity;

        private const float TOUCH_SCALE = 0.05f;
        private const float MOUSE_SCALE = 0.05f;
        private const float ZOOM_SCALE = 0.03f;
        private const float ZOOM_SCALE_MOUSE = 10f;

        private MoveCallBack moveCallBack;
        private MoveCallBack beginDragCallBack;
        private MoveCallBack endDragCallBack;
        private MoveCallBack zoomChangeCallBack;

        public GameObject Cam => cam;
        public Camera MainCamera => Camera.main ? Camera.main : cameraComp;
        public bool IsMoving { get; protected set; }
        public Vector3 LastLoc { get; set; }

        public void SetMoveCallBack(MoveCallBack cb) => moveCallBack += cb;
        public void RemoveMoveCallback(MoveCallBack cb) => moveCallBack -= cb;
        public void SetBeginDragCallBack(MoveCallBack cb) => beginDragCallBack = cb;
        public void SetEndDragCallBack(MoveCallBack cb) => endDragCallBack = cb;
        public void SetZoomChangeCallBack(MoveCallBack cb) => zoomChangeCallBack = cb;

        // 各功能分发（partial void：功能文件实现；未实现则调用被编译器消除）
        partial void DoCameraMove(float deltaX, float deltaY);
        partial void DoCamLookAround(float deltaX, float deltaY);
        partial void DoCamRotate(float deltaX, float deltaY);
        partial void DoCamAround(float deltaX, float deltaY);
        partial void DoCameraZoom(float deltaZoom);
        partial void OnUpdateInertia();

        private void Awake()
        {
            BeginDrag = OnBeginDragCb;
            Drag = OnDragCb;
            EndDrag = OnEndDragCb;
            if (cam != null) cameraComp = cam.GetComponent<Camera>();
        }

        private void OnEnable() => Inst = this;

        private void OnDisable() => IsMoving = false;

        private void Start()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            mouseMode = true;
#endif
            if (lookCenter == null)
                lookCenter = new GameObject("LOOK_CENTER");

            if (cam != null)
                worldLocalZ = (cam.transform.forward +
                    Mathf.Tan(cam.transform.rotation.eulerAngles.x / 180 * Mathf.PI) * cam.transform.up).normalized;
        }

        private void Update()
        {
            OnUpdateInertia();
        }

        // ============ 输入 ============

        public void OnScroll(PointerEventData data)
        {
            if (!allowAll || cam == null) return;
            DoCameraZoom(data.scrollDelta.y * ZOOM_SCALE_MOUSE);
        }

        private void OnBeginDragCb(GameObject obj, PointerEventData data)
        {
            underInertia = false;
            beginDragCallBack?.Invoke(data.position);
        }

        private void OnDragCb(GameObject obj, PointerEventData data)
        {
            if (!allowAll || cam == null) return;

            var scale = mouseMode ? MOUSE_SCALE : TOUCH_SCALE;
            var delta = data.delta;
            var inputX = allowTouchX ? (reverseRotate ? delta.x : -delta.x) * scale : 0;
            var inputY = allowTouchY ? (reverseRotate ? delta.y : -delta.y) * scale : 0;

            HandleMove(inputX, inputY);

            // 触摸双指缩放
            if (!mouseMode)
                DoCameraZoom(GetZoomGesture() * ZOOM_SCALE);
        }

        private void OnEndDragCb(GameObject obj, PointerEventData data)
        {
            underInertia = true;
            inertiaTime = 0;
            endDragCallBack?.Invoke(data.position);
        }

        /// <summary>计算双指缩放手势变化量（触摸端）。</summary>
        private float GetZoomGesture()
        {
            if (Input.touchCount != 2) return 0;

            if (Input.GetTouch(1).phase == TouchPhase.Began)
                touchDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);

            float newDistance = touchDistance;
            float oldDistance = touchDistance;
            if (Input.GetTouch(1).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Moved)
                newDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);

            touchDistance = newDistance;
            return newDistance - oldDistance;
        }

        // ============ 分发 ============

        private void HandleMove(float deltaX, float deltaY)
        {
            DoCameraMove(deltaX, deltaY);
            DoCamLookAround(deltaX, deltaY);
            DoCamRotate(deltaX, deltaY);
            DoCamAround(deltaX, deltaY);
        }

        // ============ 公共辅助 ============

        private Vector3 GetAxis(Axis type)
        {
            switch (type)
            {
                case Axis.X: return Vector3.right;
                case Axis.Y: return Vector3.up;
                case Axis.Z: return Vector3.forward;
                case Axis.LocalX: return cam.transform.right;
                case Axis.LocalY: return cam.transform.up;
                case Axis.LocalZ: return cam.transform.forward;
                case Axis.WorldLocalZ: return worldLocalZ;
                default: return Vector3.zero;
            }
        }

        /// <summary>修正变化值，使 oldValue + delta 限制在 [min, max]。</summary>
        private static float FixDelta(float oldValue, float delta, float min, float max)
        {
            var newValue = oldValue + delta;
            if (newValue > max) return max - oldValue;
            if (newValue < min) return min - oldValue;
            return delta;
        }

        /// <summary>重置拖动/缩放累计值（用于自动恢复位置）。</summary>
        public void ResetOffset()
        {
            offsetX = 0;
            offsetY = 0;
            offsetZoom = 0;
        }

        /// <summary>触发移动回调（简化：直接传相机位置，去掉 37 的 Ground 射线）。</summary>
        private void TriggerMoveCb()
        {
            if (moveCallBack == null || cam == null) return;
            moveCallBack.Invoke(cam.transform.position);
        }

        private void TriggerZoomCb()
        {
            zoomChangeCallBack?.Invoke(cam.transform.position);
        }
    }
}
