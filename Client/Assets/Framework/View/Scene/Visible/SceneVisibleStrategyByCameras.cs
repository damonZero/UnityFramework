// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// ********************************************************************

using Framework.Log;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.View
{
    /// <summary>
    /// 控制场景相机“隐藏时如何降级渲染”的字段开关。
    ///
    /// 说明：
    /// - 这是可组合的 Flags 枚举，可按位或组合。
    /// - 仅勾选的字段会在 Hide 时被修改，并在 Show 时恢复。
    /// </summary>
    [System.Flags]
    public enum CameraRenderControlFlags
    {
        /// <summary>
        /// 不修改任何相机字段。
        /// </summary>
        None = 0,

        /// <summary>
        /// 隐藏时设为 0（不渲染任何 Layer）；显示时恢复原值。
        /// </summary>
        CullingMask = 1 << 0,

        /// <summary>
        /// 隐藏时设为 Nothing（不清屏）；显示时恢复原值。
        /// </summary>
        ClearFlags = 1 << 1,

        /// <summary>
        /// 隐藏时压缩近远裁剪面；显示时恢复原值。
        /// 该方式不是严格“停渲染”，建议与 CullingMask 组合使用。
        /// </summary>
        ClipPlane = 1 << 2,

        /// <summary>
        /// 隐藏时直接禁用 Camera；显示时恢复 enabled。
        /// 注意：这会触发 Camera 的 OnEnable/OnDisable 生命周期。
        /// </summary>
        Enable = 1 << 3,

        /// <summary>
        /// 默认策略：
        /// 仅修改 CullingMask + ClearFlags，不修改 Camera.enabled。
        /// </summary>
        Default = CullingMask | ClearFlags,
    }

    /// <summary>
    /// BaseScene 的相机显隐策略。
    ///
    /// 目标：
    /// - 默认只影响“渲染输出”，尽量不影响相机逻辑生命周期；
    /// - 允许业务按需切换到更激进的控制方式（如 Enable/ClipPlane）。
    ///
    /// 设计：
    /// - 每个 BaseScene 持有独立策略实例（非共享），内部缓存该场景的 Camera 列表；
    /// - 第一次调用时收集相机并缓存，后续复用；
    /// - 仅对被 flags 命中的字段执行“隐藏修改 + 显示恢复”。
    /// </summary>
    public class SceneVisibleStrategyByCameras : IVisibleStrategy
    {
        private readonly struct CameraState
        {
            public readonly int cullingMask;
            public readonly CameraClearFlags clearFlags;
            public readonly float nearClipPlane;
            public readonly float farClipPlane;
            public readonly bool enabled;

            public CameraState(Camera cam)
            {
                cullingMask = cam.cullingMask;
                clearFlags = cam.clearFlags;
                nearClipPlane = cam.nearClipPlane;
                farClipPlane = cam.farClipPlane;
                enabled = cam.enabled;
            }
        }

        // 当前 scene 的相机缓存（懒初始化）。
        private List<Camera> _cameras;

        // 隐藏时缓存原始字段，显示时回滚。
        private readonly Dictionary<Camera, CameraState> _hiddenStates = new();

        private readonly CameraRenderControlFlags _controlFlags;

        /// <summary>
        /// 当前实例使用的控制 flags。
        /// </summary>
        public CameraRenderControlFlags ControlFlags => _controlFlags;

        /// <summary>
        /// 创建相机可见策略。
        /// </summary>
        /// <param name="controlFlags">控制字段组合。</param>
        public SceneVisibleStrategyByCameras(CameraRenderControlFlags controlFlags)
        {
            _controlFlags = controlFlags;
        }

        /// <summary>
        /// 预设：推荐默认模式（仅影响渲染，不改 Camera.enabled）。
        /// </summary>
        public static SceneVisibleStrategyByCameras CreateRenderSafePreset()
        {
            return new SceneVisibleStrategyByCameras(CameraRenderControlFlags.Default);
        }

        /// <summary>
        /// 预设：强制停机模式（直接改 Camera.enabled）。
        /// </summary>
        public static SceneVisibleStrategyByCameras CreateHardOffPreset()
        {
            return new SceneVisibleStrategyByCameras(CameraRenderControlFlags.Enable);
        }

        /// <summary>
        /// 预设：混合模式（CullingMask + ClearFlags + ClipPlane）。
        /// </summary>
        public static SceneVisibleStrategyByCameras CreateHybridPreset()
        {
            return new SceneVisibleStrategyByCameras(
                CameraRenderControlFlags.CullingMask |
                CameraRenderControlFlags.ClearFlags |
                CameraRenderControlFlags.ClipPlane);
        }

        /// <summary>
        /// 使相机缓存失效，下次 SetVisible 调用时重新收集。
        /// 仅在运行时动态增删了场景相机时需要主动调用。
        /// </summary>
        public void InvalidateCameraCache() => _cameras = null;

        public void SetVisible(ViewBase view, bool visible)
        {
            if (_controlFlags == CameraRenderControlFlags.None) return;

            if (view is not BaseScene scene)
            {
                GameLog.Error($"{view} is not a {nameof(BaseScene)}", module: "Framework.View");
                return;
            }

            EnsureCameras(scene);

            if (visible)
                ApplyVisible();
            else
                ApplyInvisible();
        }

        private void EnsureCameras(BaseScene scene)
        {
            if (_cameras != null) return;

            _cameras = new List<Camera>();
            CollectCameras(scene.UnityScene, _cameras);
        }

        /// <summary>
        /// 收集指定 scene 下的所有 Camera（包含 inactive GameObject）。
        ///
        /// 采用 FindObjectsByType：
        /// - FindObjectsInactive.Include：与 includeInactive: true 语义一致；
        /// - FindObjectsSortMode.None：跳过排序，降低额外开销。
        ///
        /// 参考文档：
        /// https://docs.unity3d.com/ScriptReference/Object.FindObjectsByType.html
        /// https://docs.unity3d.com/ScriptReference/FindObjectsSortMode.html
        /// https://docs.unity3d.com/ScriptReference/FindObjectsInactive.html
        /// </summary>
        private static void CollectCameras(Scene scene, List<Camera> output)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            // 返回所有已加载场景中的 Camera，再按 scene 过滤。
            var all = Object.FindObjectsByType(typeof(Camera),
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var obj in all)
            {
                if (obj is Camera cam && cam.gameObject.scene == scene)
                    output.Add(cam);
            }
        }

        private void ApplyInvisible()
        {
            foreach (var cam in _cameras)
            {
                if (cam == null) continue;

                // 仅首次隐藏时缓存，避免重复覆盖导致“恢复目标”漂移。
                if (!_hiddenStates.ContainsKey(cam))
                    _hiddenStates[cam] = new CameraState(cam);

                if ((_controlFlags & CameraRenderControlFlags.CullingMask) != 0)
                    cam.cullingMask = 0;

                if ((_controlFlags & CameraRenderControlFlags.ClearFlags) != 0)
                    cam.clearFlags = CameraClearFlags.Nothing;

                if ((_controlFlags & CameraRenderControlFlags.ClipPlane) != 0)
                {
                    // 压缩可见深度，尽量缩小可见体积。
                    cam.nearClipPlane = 0.01f;
                    cam.farClipPlane = 0.0101f;
                }

                if ((_controlFlags & CameraRenderControlFlags.Enable) != 0)
                    cam.enabled = false;
            }
        }

        private void ApplyVisible()
        {
            foreach (var cam in _cameras)
            {
                if (cam == null) continue;

                if (_hiddenStates.TryGetValue(cam, out var state))
                {
                    if ((_controlFlags & CameraRenderControlFlags.CullingMask) != 0)
                        cam.cullingMask = state.cullingMask;

                    if ((_controlFlags & CameraRenderControlFlags.ClearFlags) != 0)
                        cam.clearFlags = state.clearFlags;

                    if ((_controlFlags & CameraRenderControlFlags.ClipPlane) != 0)
                    {
                        cam.nearClipPlane = state.nearClipPlane;
                        cam.farClipPlane = state.farClipPlane;
                    }

                    if ((_controlFlags & CameraRenderControlFlags.Enable) != 0)
                        cam.enabled = state.enabled;

                    _hiddenStates.Remove(cam);
                }
                // 未命中缓存（从未隐藏过）则保持当前值。
            }
        }
    }
}
