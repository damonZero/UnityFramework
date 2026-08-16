using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core.URP
{
    /// <summary>
    /// 按需开启颜色纹理（供热扰动等效果，性能开销高，按需挂载，引用计数归零时关闭）。
    /// 对应参考项目 Core/URP/CameraColorTexture.cs；37 的私有 showColorTextrue 字段按 Phase 0 决策改为标准 requiresColorTexture。
    /// </summary>
    [ExecuteAlways]
    public class CameraColorTexture : MonoBehaviour
    {
        public enum CameraEnum
        {
            Scene,
            UI,
            Other
        }

        private static readonly Dictionary<UniversalAdditionalCameraData, int> _cache = new();

        [Header("效果类型")] public CameraEnum type = CameraEnum.Scene;
        [Header("绑定开启相机(Other模式生效)")] public Camera bindCamera;

        private UniversalAdditionalCameraData _cameraData;
        private bool _isInit;

        private void OnEnable() => Init();

        private void Update()
        {
            if (!_isInit) Init();
        }

        private void OnDisable()
        {
            RemoveTexture(_cameraData);
            _isInit = false;
        }

        /// <summary>更新效果类型与绑定相机（参数改变后重新初始化）。</summary>
        public void UpdateData(CameraEnum cameraType, Camera cam)
        {
            if (_isInit) RemoveTexture(_cameraData);

            type = cameraType;
            bindCamera = cam;
            Init();
        }

        private void Init()
        {
            _cameraData = type switch
            {
                CameraEnum.Scene => CameraStackBase.MainData,
                CameraEnum.UI => GetUiData(),
                CameraEnum.Other => bindCamera == null
                    ? null
                    : bindCamera.GetComponent<UniversalAdditionalCameraData>(),
                _ => _cameraData
            };

#if UNITY_EDITOR
            if (!Application.isPlaying) _cameraData = GetCameraData();
#endif
            if (_cameraData == null) return;

            AddTexture(_cameraData);
            _isInit = true;
        }

        private static UniversalAdditionalCameraData GetUiData()
        {
            var uiCam = Core.UI.UICamera.Camera;
            return uiCam == null ? null : uiCam.GetComponent<UniversalAdditionalCameraData>();
        }

        private static void AddTexture(UniversalAdditionalCameraData data)
        {
            if (data == null) return;
            _cache.TryGetValue(data, out var count);
            _cache[data] = count + 1;
            data.requiresColorTexture = true;
        }

        private static void RemoveTexture(UniversalAdditionalCameraData data)
        {
            if (data == null) return;

            if (!_cache.TryGetValue(data, out var count))
            {
                data.requiresColorTexture = false;
                return;
            }

            var newCount = count - 1;
            if (newCount > 0)
            {
                _cache[data] = newCount;
                return;
            }

            _cache.Remove(data);
            data.requiresColorTexture = false;
        }

        // 编辑器预览：运行前取对应相机（运行中走相机栈/UICamera）
        private UniversalAdditionalCameraData GetCameraData()
        {
            if (type == CameraEnum.Other)
                return bindCamera == null ? null : bindCamera.GetComponent<UniversalAdditionalCameraData>();

            if (type == CameraEnum.Scene)
                return Camera.main != null ? Camera.main.GetComponent<UniversalAdditionalCameraData>() : null;

            return GetUiData();
        }
    }
}
