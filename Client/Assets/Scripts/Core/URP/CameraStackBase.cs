using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core.URP
{
    /// <summary>
    /// URP 相机栈基类：管理「1 个 Base 主相机 + N 个 Overlay 叠加相机」。
    /// 对应参考项目 Boot/Update/View/BootBaseCamera.cs，但按 KJ 分层落到 Core（KJ 的 Boot 是 AOT 壳，不引用 URP）。
    /// 37 依赖的私有 URP 字段（ShotInUI/isUICamera）已按 Phase 0 决策用标准 URP API（renderType/cameraStack）替代，无需 fork。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraStackBase : MonoBehaviour
    {
        /// <summary>Overlay 相机的排序信息。</summary>
        private sealed class CameraInfo
        {
            public int index;
            public UniversalAdditionalCameraData data;
        }

        private static readonly Dictionary<Camera, CameraInfo> CameraCache = new Dictionary<Camera, CameraInfo>();

        /// <summary>相机列表（Base 在首位，供截屏/遍历使用）。引用固定、内容可变，故 static readonly。</summary>
        public static readonly List<Camera> Stack = new List<Camera>();

        /// <summary>Base 主相机（相机栈首位）。</summary>
        public static Camera MainCamera { get; private set; }

        /// <summary>Base 主相机的 URP 附加数据；无 Base 时回退 Camera.main。</summary>
        public static UniversalAdditionalCameraData MainData
        {
            get
            {
                var cam = MainCamera != null ? MainCamera : Camera.main;
                return cam != null ? cam.GetComponent<UniversalAdditionalCameraData>() : null;
            }
        }

        /// <summary>注册相机到栈（按 order 升序，首位即 Base）。</summary>
        public static void AddOverlay(Camera camera, int order)
        {
            if (camera == null) return;

            CacheCamera(camera, order);
            if (Stack.Contains(camera)) return;

            Stack.Add(camera);
            SortStack();
            ResetStack();
        }

        /// <summary>从栈移除相机。</summary>
        public static void RemoveOverlay(Camera camera)
        {
            if (camera == null) return;

            CameraCache.Remove(camera);
            Stack.Remove(camera);
            ResetStack();
        }

        /// <summary>清空相机栈（相机栈销毁 / 软重启时调用，避免 readonly 集合跨重启泄漏）。</summary>
        public static void ClearStack()
        {
            Stack.Clear();
            CameraCache.Clear();
            MainCamera = null;
        }

        private static void CacheCamera(Camera camera, int order)
        {
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            CameraCache[camera] = new CameraInfo { index = order, data = data };
        }

        private static void SortStack()
        {
            Stack.Sort((a, b) =>
            {
                if (!CameraCache.TryGetValue(a, out var orderA)) return 1;
                if (!CameraCache.TryGetValue(b, out var orderB)) return -1;
                return orderA.index.CompareTo(orderB.index);
            });
        }

        /// <summary>栈首位设为 Base，其余设为 Overlay 并挂到 Base 的 cameraStack。</summary>
        private static void ResetStack()
        {
            if (Stack.Count == 0 || !CameraCache.TryGetValue(Stack[0], out var baseInfo))
            {
                MainCamera = null;
                return;
            }

            var baseData = baseInfo.data;
            if (baseData == null)
            {
                MainCamera = null;
                return;
            }

            baseData.renderType = CameraRenderType.Base;
            baseData.cameraStack.Clear();
            MainCamera = Stack[0];

            for (int i = 1; i < Stack.Count; i++)
            {
                if (!CameraCache.TryGetValue(Stack[i], out var temp)) continue;
                if (temp.data == null) continue;
                temp.data.renderType = CameraRenderType.Overlay;
                baseData.cameraStack.Add(Stack[i]);
            }
        }

        private void OnDestroy()
        {
            // 相机栈根销毁时清空全局状态；各 Overlay 相机已通过 RemoveOverlay 单独退出。
            ClearStack();
        }
    }
}
