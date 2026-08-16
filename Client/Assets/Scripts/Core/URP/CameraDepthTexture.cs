using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core.URP
{
    /// <summary>
    /// 按需开启深度纹理（供水面/雾等需要深度图的效果，性能开销高，按需挂载，引用计数归零时关闭）。
    /// 对应参考项目 Core/URP/CameraDepthTexture.cs；37 的私有 showDepthTextrue 字段按 Phase 0 决策改为标准 requiresDepthTexture。
    /// </summary>
    [ExecuteAlways]
    public class CameraDepthTexture : MonoBehaviour
    {
        private static readonly Dictionary<UniversalAdditionalCameraData, int> _cache = new();

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

        private void Init()
        {
            _cameraData = CameraStackBase.MainData;
            if (_cameraData == null) return;

            AddTexture(_cameraData);
            _isInit = true;
        }

        private static void AddTexture(UniversalAdditionalCameraData data)
        {
            if (data == null) return;
            _cache.TryGetValue(data, out var count);
            _cache[data] = count + 1;
            data.requiresDepthTexture = true;
        }

        private static void RemoveTexture(UniversalAdditionalCameraData data)
        {
            if (data == null) return;

            if (!_cache.TryGetValue(data, out var count))
            {
                data.requiresDepthTexture = false;
                return;
            }

            var newCount = count - 1;
            if (newCount > 0)
            {
                _cache[data] = newCount;
                return;
            }

            _cache.Remove(data);
            data.requiresDepthTexture = false;
        }
    }
}
