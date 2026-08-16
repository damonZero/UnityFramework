using Core.UI;
using Framework.Log;
using UnityEngine;
using UnityEngine.UI;

namespace General
{
    /// <summary>
    /// 摄像机相关工具：视口 / 屏幕 / UI 区域判定与坐标换算。
    /// 对应参考项目 ScriptsC#/General/Utils/CameraUtil.cs。
    /// 37 的 UtilUIKit.IsInScreen 依赖这里内联为「UI 元素包围盒与根画布包围盒相交」判定。
    /// </summary>
    public static class CameraUtil
    {
        private const string Module = nameof(CameraUtil);

        // 世界坐标角点缓存（与根画布求包围盒重叠用；引用固定、内容可变，故 static readonly）
        private static readonly Vector3[] Corners = new Vector3[4];
        private static readonly Vector3[] RootCorners = new Vector3[4];

        /// <summary>
        /// 判断游戏对象是否在摄像机视口范围内，自动识别场景对象和 UI 对象。
        /// </summary>
        public static bool IsInViewport(GameObject gameObject, Camera camera = null)
        {
            if (gameObject == null)
            {
                GameLog.Warn("CameraUtil: GameObject is null", Module);
                return false;
            }

            var rectTransform = gameObject.GetComponent<RectTransform>();
            return rectTransform != null
                ? IsUIObjectInViewport(rectTransform, camera)
                : IsSceneObjectInViewport(gameObject.transform, camera);
        }

        /// <summary>判断场景对象是否在摄像机视口范围内。</summary>
        public static bool IsSceneObjectInViewport(Transform target, Camera camera = null)
        {
            if (target == null)
            {
                GameLog.Warn("CameraUtil: Target transform is null", Module);
                return false;
            }

            if (!TryResolveCamera(camera, out camera))
                return false;

            Vector3 viewportPos = camera.WorldToViewportPoint(target.position);

            // z > 0（相机前方）且 xy 在 [0,1] 内（视野内）
            bool isInFront = viewportPos.z > 0;
            bool isInView = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                            viewportPos.y >= 0 && viewportPos.y <= 1;

            return isInFront && isInView;
        }

        /// <summary>判断场景对象是否在摄像机视口范围内（带视口坐标）。</summary>
        public static (bool isInView, Vector2 viewportPos) IsSceneObjectInViewportDetailed(Transform target,
            Camera camera = null)
        {
            if (target == null)
            {
                GameLog.Warn("CameraUtil: Target transform is null", Module);
                return (false, Vector2.zero);
            }

            if (!TryResolveCamera(camera, out camera))
                return (false, Vector2.zero);

            Vector3 viewportPos = camera.WorldToViewportPoint(target.position);

            bool isInFront = viewportPos.z > 0;
            bool isInView = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                            viewportPos.y >= 0 && viewportPos.y <= 1;

            return (isInFront && isInView, new Vector2(viewportPos.x, viewportPos.y));
        }

        /// <summary>
        /// 判断 UI 对象是否在屏幕范围内（其包围盒与根画布包围盒相交）。
        /// <paramref name="camera"/> 对 UI 对象无意义，仅为保持接口一致保留。
        /// </summary>
        public static bool IsUIObjectInViewport(RectTransform rectTransform, Camera camera = null)
        {
            if (rectTransform == null)
            {
                GameLog.Warn("CameraUtil: RectTransform is null", Module);
                return false;
            }

            var rootCanvas = ScreenHelper.RootCanvas;
            if (rootCanvas == null)
                return false;

            rectTransform.GetWorldCorners(Corners);
            ((RectTransform)rootCanvas.transform).GetWorldCorners(RootCorners);
            return BoundsOverlap(Corners, RootCorners);
        }

        /// <summary>判断屏幕坐标点是否在指定 UI 区域内。</summary>
        public static bool IsScreenPointInUIArea(RectTransform rectTransform, Vector2 screenPoint, Camera camera = null)
        {
            if (rectTransform == null)
            {
                GameLog.Warn("CameraUtil: RectTransform is null", Module);
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, camera);
        }

        /// <summary>获取场景对象在屏幕上的位置；对象在相机后方时返回 (-1, -1)。</summary>
        public static Vector2 GetScreenPosition(Transform target, Camera camera = null)
        {
            if (target == null)
            {
                GameLog.Warn("CameraUtil: Target transform is null", Module);
                return new Vector2(-1, -1);
            }

            if (!TryResolveCamera(camera, out camera))
                return new Vector2(-1, -1);

            Vector3 screenPos = camera.WorldToScreenPoint(target.position);
            return screenPos.z <= 0 ? new Vector2(-1, -1) : new Vector2(screenPos.x, screenPos.y);
        }

        /// <summary>获取场景对象在视口中的位置（0-1 范围）；对象在相机后方时返回 (-1, -1)。</summary>
        public static Vector2 GetViewportPosition(Transform target, Camera camera = null)
        {
            if (target == null)
            {
                GameLog.Warn("CameraUtil: Target transform is null", Module);
                return new Vector2(-1, -1);
            }

            if (!TryResolveCamera(camera, out camera))
                return new Vector2(-1, -1);

            Vector3 viewportPos = camera.WorldToViewportPoint(target.position);
            return viewportPos.z <= 0 ? new Vector2(-1, -1) : new Vector2(viewportPos.x, viewportPos.y);
        }

        /// <summary>
        /// 判断游戏对象是否在指定 UI 的 Rect 范围内，自动识别场景对象和 UI 对象。
        /// </summary>
        public static bool IsInUIRect(GameObject gameObject, RectTransform targetUIRect, Camera camera = null)
        {
            if (gameObject == null)
            {
                GameLog.Warn("CameraUtil: GameObject is null", Module);
                return false;
            }

            if (targetUIRect == null)
            {
                GameLog.Warn("CameraUtil: Target UI RectTransform is null", Module);
                return false;
            }

            var rectTransform = gameObject.GetComponent<RectTransform>();
            return rectTransform != null
                ? IsUIObjectInUIRect(rectTransform, targetUIRect)
                : IsSceneObjectInUIRect(gameObject.transform, targetUIRect, camera);
        }

        /// <summary>判断场景对象是否在指定 UI 的 Rect 范围内。</summary>
        public static bool IsSceneObjectInUIRect(Transform target, RectTransform targetUIRect, Camera camera = null)
        {
            if (target == null)
            {
                GameLog.Warn("CameraUtil: Target transform is null", Module);
                return false;
            }

            if (targetUIRect == null)
            {
                GameLog.Warn("CameraUtil: Target UI RectTransform is null", Module);
                return false;
            }

            if (!TryResolveCamera(camera, out camera))
                return false;

            Vector3 screenPos = camera.WorldToScreenPoint(target.position);
            if (screenPos.z <= 0)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(targetUIRect, screenPos, null);
        }

        /// <summary>判断 UI 对象中心点是否在指定 UI 的 Rect 范围内。</summary>
        public static bool IsUIObjectInUIRect(RectTransform sourceUIRect, RectTransform targetUIRect)
        {
            if (sourceUIRect == null)
            {
                GameLog.Warn("CameraUtil: Source UI RectTransform is null", Module);
                return false;
            }

            if (targetUIRect == null)
            {
                GameLog.Warn("CameraUtil: Target UI RectTransform is null", Module);
                return false;
            }

            Vector3 sourceScreenPos = RectTransformUtility.WorldToScreenPoint(null, sourceUIRect.position);
            return RectTransformUtility.RectangleContainsScreenPoint(targetUIRect, sourceScreenPos, null);
        }

        /// <summary>判断 UI 对象的边界是否与指定 UI 的 Rect 范围重叠。</summary>
        public static bool IsUIObjectOverlapWithUIRect(RectTransform sourceUIRect, RectTransform targetUIRect)
        {
            if (sourceUIRect == null)
            {
                GameLog.Warn("CameraUtil: Source UI RectTransform is null", Module);
                return false;
            }

            if (targetUIRect == null)
            {
                GameLog.Warn("CameraUtil: Target UI RectTransform is null", Module);
                return false;
            }

            Rect sourceRect = GetWorldRect(sourceUIRect);
            Rect targetRect = GetWorldRect(targetUIRect);
            return sourceRect.Overlaps(targetRect);
        }

        /// <summary>解析相机：显式传入优先，否则回退 CameraMoveAdv.Inst 的主相机。</summary>
        private static bool TryResolveCamera(Camera camera, out Camera resolved)
        {
            if (camera != null)
            {
                resolved = camera;
                return true;
            }

            resolved = CameraMoveAdv.Inst?.MainCamera;
            if (resolved == null)
                GameLog.Warn("CameraUtil: No camera available", Module);
            return resolved != null;
        }

        /// <summary>获取 RectTransform 在世界空间中的矩形边界。</summary>
        private static Rect GetWorldRect(RectTransform rectTransform)
        {
            rectTransform.GetWorldCorners(Corners);

            float minX = Corners[0].x;
            float maxX = Corners[0].x;
            float minY = Corners[0].y;
            float maxY = Corners[0].y;

            for (int i = 1; i < 4; i++)
            {
                if (Corners[i].x < minX) minX = Corners[i].x;
                if (Corners[i].x > maxX) maxX = Corners[i].x;
                if (Corners[i].y < minY) minY = Corners[i].y;
                if (Corners[i].y > maxY) maxY = Corners[i].y;
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>判断两组世界坐标角点（各 4 个）的包围盒是否重叠。</summary>
        private static bool BoundsOverlap(Vector3[] a, Vector3[] b)
        {
            float minX1 = float.MaxValue;
            float minY1 = float.MaxValue;
            float maxX1 = float.MinValue;
            float maxY1 = float.MinValue;
            float minX2 = float.MaxValue;
            float minY2 = float.MaxValue;
            float maxX2 = float.MinValue;
            float maxY2 = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                minX1 = Mathf.Min(minX1, a[i].x);
                minY1 = Mathf.Min(minY1, a[i].y);
                maxX1 = Mathf.Max(maxX1, a[i].x);
                maxY1 = Mathf.Max(maxY1, a[i].y);

                minX2 = Mathf.Min(minX2, b[i].x);
                minY2 = Mathf.Min(minY2, b[i].y);
                maxX2 = Mathf.Max(maxX2, b[i].x);
                maxY2 = Mathf.Max(maxY2, b[i].y);
            }

            return minX1 <= maxX2 && maxX1 >= minX2 && minY1 <= maxY2 && maxY1 >= minY2;
        }
    }
}
