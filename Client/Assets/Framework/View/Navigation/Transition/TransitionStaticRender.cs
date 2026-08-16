using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 静态渲染转场：Begin停止渲染到屏幕，End恢复渲染到屏幕
    /// </summary>
    public sealed class TransitionStaticRender : TransitionBase
    {
        /// <summary>
        /// 此转场没有持续性表现效果，所以这个属性固定为false
        /// </summary>
        public override bool IsEffectRunning => false;


        public override void Start()
        {
            base.Start();
            DisableRenderToScreen();
        }

        public override void Stop()
        {
            EnableRenderToScreen();
            base.Stop();
        }

        #region 停止相机渲染的静态控制逻辑

        private static List<Camera> DisableCameras { get; set; }

        internal static void DisableRenderToScreen()
        {
            // 禁用事件系统,避免转场中的意外点击
            if (!NavigationManager.Instance.DisableEventSystem())
            {
                Log.Debug($"{nameof(DisableRenderToScreen)} : already disabled event system, skip disabling camera render");
                return;
            }

            DisableCameras ??= CollectionPool<List<Camera>, Camera>.Get();
            DisableCameras.Clear();

            var texture = GetStopCameraTexture();
            foreach (var cam in Camera.allCameras)
            {
                if (cam.targetTexture != null) continue;

                cam.targetTexture = texture;
                DisableCameras.Add(cam);

                Log.Debug($"{nameof(DisableRenderToScreen)} : camera('{cam}').targetTexture = {texture}");
            }

            Log.Debug($"{nameof(DisableRenderToScreen)} : disable {DisableCameras.Count} cameras render to screen");

            //将渲染帧间隔设置为最大,在停止相机渲染的瞬间让Unity不渲染新的画面
            // FIXME by fred 临时屏蔽，解决无法完成登录界面加载问题（导致游戏循环Update不执行了）
            OnDemandRendering.renderFrameInterval = int.MaxValue;
        }


        internal static void EnableRenderToScreen()
        {
            // 启用事件系统
            if (!NavigationManager.Instance.EnableEventSystem())
            {
                Log.Debug($"{nameof(EnableRenderToScreen)} : event system still disabled by other requests, skip enabling camera render");
                return;
            }

            foreach (var cam in DisableCameras)
            {
                if (!cam) continue;
                cam.targetTexture = null;

                Log.Debug($"{nameof(EnableRenderToScreen)} : camera('{cam}').targetTexture = null");
            }

            Log.Debug($"{nameof(EnableRenderToScreen)} : enable {DisableCameras.Count} cameras render to screen");

            ReleaseStopCameraTexture();

            CollectionPool<List<Camera>, Camera>.Release(DisableCameras);
            DisableCameras = null;

            //将渲染帧间隔设置为正常值,配合StopCameraRender()方法使用
            OnDemandRendering.renderFrameInterval = 1;
        }

        #endregion

        #region 用于停止相机渲染到屏幕的纹理

        private static RenderTexture _stopCameraTex;
        private static Vector2Int _stopCameraTexSize;

        private static RenderTexture GetStopCameraTexture()
        {
            var currentScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (_stopCameraTexSize == currentScreenSize)
            {
                return _stopCameraTex;
            }

            _stopCameraTexSize = currentScreenSize;

            if (_stopCameraTex != null)
            {
                RenderTexture.ReleaseTemporary(_stopCameraTex);
            }

            _stopCameraTex = RenderTexture.GetTemporary(
                _stopCameraTexSize.x, _stopCameraTexSize.y, 0, RenderTextureFormat.R8);

            return _stopCameraTex;
        }

        private static void ReleaseStopCameraTexture()
        {
            if (_stopCameraTex != null)
            {
                RenderTexture.ReleaseTemporary(_stopCameraTex);
                _stopCameraTex = null;
                _stopCameraTexSize = Vector2Int.zero;
            }
        }

        #endregion
    }
}
