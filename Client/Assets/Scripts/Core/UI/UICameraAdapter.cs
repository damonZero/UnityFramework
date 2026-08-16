using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// UI 相机分辨率自适应组件：屏幕尺寸变化时重设相机位置 / orthographicSize / 平面距离，
    /// 使 UI 相机按「1 世界单位 = 1 屏幕像素」铺满屏幕。
    /// 对应参考项目 ScriptsC#/Core/UI/Util/UICameraAdapter.cs；去掉 [ExecuteAlways]（KJ 相机运行时创建）。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class UICameraAdapter : MonoBehaviour
    {
        [SerializeField] private Canvas _uiRootCanvas;

        /// <summary>绑定的 UI 根画布；赋值时立即应用相机（避免创建后首帧位置/尺寸错误）。</summary>
        public Canvas uiRootCanvas
        {
            get => _uiRootCanvas;
            set
            {
                _uiRootCanvas = value;
                ApplyCamera();
            }
        }

        private Camera _camera;
        private int _screenW = -1;
        private int _screenH = -1;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _screenW = Screen.width;
            _screenH = Screen.height;
            ApplyCamera();
        }

        private void Update()
        {
            if (_uiRootCanvas == null || _camera == null)
            {
                return;
            }

            if (_screenW == Screen.width && _screenH == Screen.height)
            {
                return;
            }

            _screenW = Screen.width;
            _screenH = Screen.height;
            ApplyCamera();
        }

        private void ApplyCamera()
        {
            if (_uiRootCanvas == null || _camera == null)
            {
                return;
            }

            if (_screenW <= 0 || _screenH <= 0)
            {
                return;
            }

            _uiRootCanvas.planeDistance = UICamera.DefaultPlaneDistance;
            var t = _camera.transform;
            t.position = new Vector3(_screenW / 2f, _screenH / 2f, -_uiRootCanvas.planeDistance);
            _camera.orthographic = true;
            _camera.orthographicSize = _screenH / 2f;
        }
    }
}
