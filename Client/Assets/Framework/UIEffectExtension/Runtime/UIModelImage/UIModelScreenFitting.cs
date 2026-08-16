using System;
using UnityEngine;

namespace Framework.UIEffectExtensions
{
    /// <summary>
    /// 3D 模型屏幕适配：根据 UI 相机与设计分辨率的差异缩放模型，使其在不同屏幕比例下保持正确大小。
    /// 对应参考项目 Package/UIEffectExtension/Runtime/UIModelImage/UIModelScreenFitting.cs（原类名 UIModeScreenFitting 为拼写笔误，此处修正）。
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class UIModelScreenFitting : MonoBehaviour
    {
        private Camera _uiCamera;

        private void Start()
        {
            Canvas canvas = transform.GetComponentInParent<Canvas>();
            _uiCamera = canvas.worldCamera;
            if (_uiCamera != null)
            {
                Vector2 scale = GetRate(_uiCamera);
                var transform1 = transform;
                Vector3 nowScale = transform1.localScale;
                transform1.localScale = new Vector3(nowScale.x * scale.x, nowScale.y * scale.y, nowScale.z);
            }
        }

        // 垂直缩放
        private static float _hRate;
        // 水平缩放
        private static float _wRate;

        // 获取相机的横纵比变化
        public static Vector2 GetRate(Camera camera)
        {
            if (Math.Abs(_hRate) <= 0 || Math.Abs(_wRate) <= 0)
            {
                var transform1 = camera.transform;
                var distance = transform1.position.sqrMagnitude;
                var originCorners = GetCorners(transform1, camera.fieldOfView, 0.5622189f, distance);
                var corners = GetCorners(camera.transform, camera.fieldOfView, camera.aspect, distance);
                if (Mathf.Abs(originCorners[3].x - originCorners[0].x) <= 0)
                {
                    _hRate = 1;
                }
                else
                {
                    _hRate = Mathf.Abs(originCorners[3].x - originCorners[0].x) / Mathf.Abs(corners[3].x - corners[0].x);
                }

                if (Mathf.Abs(originCorners[3].y - originCorners[0].y) <= 0)
                {
                    _wRate = 1;
                }
                else
                {
                    _wRate = Mathf.Abs(originCorners[3].y - originCorners[0].y) / Mathf.Abs(corners[3].y - corners[0].y);
                }
            }

            return new Vector2(_wRate, _hRate);
        }

        // 计算视窗大小
        public static Vector3[] GetCorners(Transform cameraTransform, float fieldOfView, float aspect, float distance)
        {
            var corners = new Vector3[4];
            var halfFov = (fieldOfView * 0.5f) * Mathf.Deg2Rad;
            var height = distance * Mathf.Tan(halfFov);
            var width = height * aspect;

            var position = cameraTransform.position;
            var right = cameraTransform.right;
            corners[0] = position - (right * width);
            var up = cameraTransform.up;
            corners[0] = corners[0] + up * height;
            var forward = cameraTransform.forward;
            corners[0] = corners[0] + forward * distance;

            corners[1] = position + (right * width);
            corners[1] = corners[1] + up * height;
            corners[1] = corners[1] + forward * distance;

            corners[2] = position - (right * width);
            corners[2] = corners[2] - up * height;
            corners[2] = corners[2] + forward * distance;

            corners[3] = position + (right * width);
            corners[3] = corners[3] - up * height;
            corners[3] = corners[3] + forward * distance;
            return corners;
        }
    }
}
