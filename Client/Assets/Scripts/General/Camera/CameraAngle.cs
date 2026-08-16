using System;
using UnityEngine;

namespace General
{
    /// <summary>按角度换算 FOV + 底部适配。对应参考项目 Framework/Package/CameraTool/CameraAngle.cs（落 General/Camera，未移植自定义 Editor，字段改为默认 Inspector 可见）。</summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class CameraAngle : MonoBehaviour
    {
        [SerializeField] private float _angle = 30;

        public Camera TargetCam;

        /// <summary>最大 fieldOfView（大于 0 则限制）。</summary>
        public float FieldOfViewMax = 0;

        /// <summary>是否底部适配（底部可见内容不变）。</summary>
        public bool IsAdapterBottom = false;

        public Vector3 OriginalPos = Vector3.zero;
        public float OriginalFieldOfView = 0;
        public float AdapterParam = 5.0f;

        /// <summary>Canvas 适配开关（编辑器下用）。</summary>
        public bool IsAdapterCanvas = false;

        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                SetFov();
            }
        }

        private void OnEnable()
        {
            TargetCam = GetComponent<Camera>();
            SetFov();
        }

#if UNITY_EDITOR
        private void Update()
        {
            // 编辑器上方便调试
            if (!Application.isPlaying) SetFov();
        }
#endif

        public void SetFov()
        {
            float w = Mathf.Tan(_angle * Mathf.Deg2Rad / 2);
            float h = w / TargetCam.aspect;
            if (FieldOfViewMax > 0)
                TargetCam.fieldOfView = Math.Min(Mathf.Atan(h) * Mathf.Rad2Deg * 2, FieldOfViewMax);
            else
                TargetCam.fieldOfView = Mathf.Atan(h) * Mathf.Rad2Deg * 2;

            if (IsAdapterBottom && ((!Application.isPlaying && IsAdapterCanvas) || Application.isPlaying))
            {
                float scaleFactor = TargetCam.fieldOfView - OriginalFieldOfView;
                TargetCam.transform.position = OriginalPos - TargetCam.transform.up * scaleFactor * AdapterParam;
            }
        }
    }
}
