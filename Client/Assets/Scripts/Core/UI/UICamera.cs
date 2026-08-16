using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    /// <summary>
    /// UI 相机静态门面：持有专用 UI 相机与根 Canvas 引用。
    /// 对应参考项目 ScriptsC#/Core/UI/Util/UICamera.cs。
    /// KJ 由 ViewSystem 在创建 UI 根后调用 <see cref="Bind"/> 注入（分层启动链下不依赖场景单例 StartScreen），
    /// 去掉了 37 对 EventManager / ScreenHelper 的依赖（屏幕适配由 ScreenHelper 单独负责）。
    /// </summary>
    public static class UICamera
    {
        /// <summary>UI 相机的默认平面距离（与参考项目 UI.unity 场景 m_PlaneDistance 一致）。</summary>
        public const float DefaultPlaneDistance = 500f;

        private static Camera _camera;
        private static Canvas _rootCanvas;

        /// <summary>专用 UI 相机（正交、只渲染 UI 层）。</summary>
        public static Camera Camera => _camera;

        /// <summary>UI 根画布。</summary>
        public static Canvas RootCanvas => _rootCanvas;

        /// <summary>绑定 UI 相机与根画布（ViewSystem 创建 UI 根后调用）。</summary>
        public static void Bind(Camera uiCamera, Canvas rootCanvas)
        {
            _camera = uiCamera;
            _rootCanvas = rootCanvas;
        }

        /// <summary>解绑（Shutdown 时调用）。</summary>
        public static void Unbind()
        {
            _camera = null;
            _rootCanvas = null;
        }
    }
}
