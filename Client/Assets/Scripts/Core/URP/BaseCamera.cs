using UnityEngine;

namespace Core.URP
{
    /// <summary>
    /// URP Base 主相机（tag MainCamera）。挂场景主相机上，注册为相机栈首位（Base）。
    /// 对应参考项目 Core/URP/BaseCamera.cs。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BaseCamera : CameraStackBase
    {
        private const int BaseOrder = int.MinValue;

        private Camera _camera;

        protected virtual void OnEnable()
        {
            _camera = GetComponent<Camera>();
            AddOverlay(_camera, BaseOrder);

#if UNITY_EDITOR
            CheckOverlayCamera();
#endif
        }

        protected virtual void OnDisable()
        {
            RemoveOverlay(_camera);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器校验：主相机 cullingMask 不得包含 UI 层（UI 由专用 UICamera 渲染）。
        /// 37 的完整版还校验「场景相机漏挂 OverlayCamera」，依赖其 SceneSubSystem.SceneActive 事件与 CopyUI/UIModel 层，KJ 暂缺对应钩子，留待 Phase 7。
        /// </summary>
        private void CheckOverlayCamera()
        {
            var cam = GetComponent<Camera>();
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0 && (cam.cullingMask & (1 << uiLayer)) != 0)
            {
                Framework.Log.GameLog.Error(
                    $"BaseCamera 的 cullingMask 不应包含 UI 层: {cam.name}",
                    nameof(BaseCamera));
            }
        }
#endif
    }
}
