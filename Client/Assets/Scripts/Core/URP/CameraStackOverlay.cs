using UnityEngine;

namespace Core.URP
{
    /// <summary>
    /// URP Overlay 相机基类：OnEnable 挂栈、OnDisable 出栈。
    /// 对应参考项目 Boot/Update/View/BootOverlayCamera.cs（去掉 37 私有 shotInUI 字段，按 Phase 0 决策）。
    /// </summary>
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    public class CameraStackOverlay : MonoBehaviour
    {
        [Header("顺序")] public int order;

        private Camera _camera;

        protected virtual void OnEnable()
        {
            _camera = GetComponent<Camera>();
            CameraStackBase.AddOverlay(_camera, order);
        }

        protected virtual void OnDisable()
        {
            CameraStackBase.RemoveOverlay(_camera);
        }
    }
}
