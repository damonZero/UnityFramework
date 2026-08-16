using Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UnityExtension
{
    /// <summary>
    /// 相机坐标换算扩展。对应参考项目 ScriptsC#/Core/UnityExtension/CameraExtension.cs。
    /// 世界 / 屏幕 / 视口 / UI 坐标互转，及水平地面取点。
    /// </summary>
    public static class CameraExtension
    {
        /// <summary>世界坐标转 UI 世界坐标。</summary>
        public static void WorldToUIPointXYZ(this Camera camera, Vector3 v, out float x, out float y, out float z)
        {
            var pos = ScreenHelper.WorldPointToUIPoint(camera, v);
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>世界坐标转 UI 世界坐标。</summary>
        public static void WorldToUIPointXYZ(this Camera camera, float x1, float y1, float z1,
            out float x, out float y, out float z)
        {
            var pos = ScreenHelper.WorldPointToUIPoint(camera, new Vector3(x1, y1, z1));
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>世界坐标转屏幕坐标。</summary>
        public static void WorldToScreenPointXYZ(this Camera camera, Vector3 v, out float x, out float y, out float z)
        {
            var pos = camera.WorldToScreenPoint(v);
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>世界坐标转屏幕坐标。</summary>
        public static void WorldToScreenPointXYZ(this Camera camera, float x1, float y1, float z1,
            out float x, out float y, out float z)
        {
            var pos = camera.WorldToScreenPoint(new Vector3(x1, y1, z1));
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>UI 世界坐标转世界坐标。</summary>
        public static void UIToWorldPointXYZ(this Camera camera, float x1, float y1, float z1,
            out float x, out float y, out float z)
        {
            var pos = ScreenHelper.UIPointToWorldPoint(camera, new Vector3(x1, y1, z1));
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>世界坐标转视口坐标。</summary>
        public static void WorldToViewportPointXYZ(this Camera camera, float x1, float y1, float z1,
            out float x, out float y, out float z)
        {
            var pos = camera.WorldToViewportPoint(new Vector3(x1, y1, z1));
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>视口坐标转屏幕坐标。</summary>
        public static void ViewportToScreenPointXYZ(this Camera camera, float x1, float y1, float z1,
            out float x, out float y, out float z)
        {
            var pos = camera.ViewportToScreenPoint(new Vector3(x1, y1, z1));
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// 世界坐标转 UGUI 本地坐标。
        /// </summary>
        /// <param name="uiCamera">UI 相机</param>
        /// <param name="worldCamera">世界相机</param>
        /// <param name="canvasRectTransform">目标 Canvas 的 RectTransform</param>
        /// <param name="pos">世界坐标</param>
        /// <param name="x">输出 UGUI 本地 X</param>
        /// <param name="y">输出 UGUI 本地 Y</param>
        public static void World2UGUIPosXY(this Camera uiCamera, Camera worldCamera, RectTransform canvasRectTransform,
            Vector3 pos, out float x, out float y)
        {
            Vector2 world2ScreenPos = worldCamera.WorldToScreenPoint(pos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, world2ScreenPos, uiCamera,
                out var uiPos);
            x = uiPos.x;
            y = uiPos.y;
        }

        /// <summary>
        /// 通过相机射线取水平地面上（y = groundY 平面）的点击世界坐标。
        /// </summary>
        /// <param name="sceneCamera">场景相机</param>
        /// <param name="screenClickX">点击屏幕位置 X</param>
        /// <param name="screenClickY">点击屏幕位置 Y</param>
        /// <param name="groundY">地面高度 Y</param>
        /// <param name="wx">输出世界 X</param>
        /// <param name="wy">输出世界 Y</param>
        /// <param name="wz">输出世界 Z</param>
        public static void GetSceneGroundPosXYZ(this Camera sceneCamera,
            float screenClickX, float screenClickY,
            float groundY,
            out float wx, out float wy, out float wz)
        {
            var ray = sceneCamera.ScreenPointToRay(new Vector2(screenClickX, screenClickY));
            var distance = (groundY - ray.origin.y) / ray.direction.y;
            var worldClickPosition = ray.origin + ray.direction * distance;
            wx = worldClickPosition.x;
            wy = worldClickPosition.y;
            wz = worldClickPosition.z;
        }
    }
}
