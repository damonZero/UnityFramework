using E7.NotchSolution;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    /// <summary>
    /// 屏幕 / 安全区 / 分辨率适配。
    /// 对应参考项目 ScriptsC#/Core/UI/Screen/ScreenHelper + CanvasScalerHelper。
    /// 安全区边距由 E7 Notch Solution 的 <see cref="SafePadding"/> 组件驱动（ViewSystem 创建 UIRoot 时挂载），
    /// 此处只读 <see cref="SafePadding.Instance"/> 的 FinalPaddingsLdur；SafePadding 未就绪时回退手写 Screen.safeArea。
    /// </summary>
    public static class ScreenHelper
    {
        /// <summary>设计分辨率宽度（参考项目 ScreenConfig.STANDARD_WIDTH）。</summary>
        public const int StandardWidth = 750;

        /// <summary>设计分辨率高度（参考项目 ScreenConfig.STANDARD_HEIGHT）。</summary>
        public const int StandardHeight = 1624;

        /// <summary>设计宽高比。</summary>
        public static float StandardAspect => StandardWidth * 1f / StandardHeight;

        /// <summary>UI 根画布。</summary>
        public static Canvas RootCanvas { get; private set; }

        /// <summary>UI 根 RectTransform。</summary>
        public static RectTransform UIRoot { get; private set; }

        /// <summary>安全区内的根节点（已内缩避开刘海/底部指示条），Form 应挂在此节点下。</summary>
        public static RectTransform SafeUIRoot { get; private set; }

        /// <summary>设计画布逻辑宽。</summary>
        public static int CanvasWidth { get; private set; }

        /// <summary>设计画布逻辑高。</summary>
        public static int CanvasHeight { get; private set; }

        /// <summary>横向适配比例：屏幕像素宽 / 标准宽。</summary>
        public static float StandardRate => Screen.width * 1f / StandardWidth;

        /// <summary>安全区边距（左 下 上 右，Canvas 逻辑单位）。由 SafePadding 组件驱动，未就绪时回退手写计算。</summary>
        public static Vector4 SafePaddingLdur => SafePadding.Instance != null
            ? SafePadding.Instance.FinalPaddingsLdur
            : FallbackSafePaddingLdur();

        /// <summary>CanvasScaler 的 matchWidthOrHeight 取值（0=宽度适配，1=高度适配）。</summary>
        public static float MatchWidthOrHeight =>
            Screen.width * 1f / Screen.height < StandardAspect ? 0f : 1f;

        public static void Init(Canvas canvas, RectTransform uiRoot, RectTransform safeUIRoot)
        {
            RootCanvas = canvas;
            UIRoot = uiRoot;
            SafeUIRoot = safeUIRoot;
            Refresh();
        }

        /// <summary>
        /// 刷新画布逻辑尺寸。分辨率变化时调用（安全区边距由 SafePadding 组件自行驱动，无需在此应用）。
        /// </summary>
        public static void Refresh()
        {
            RefreshCanvasSize();
        }

        private static void RefreshCanvasSize()
        {
            var deviceAspect = Screen.width * 1f / Screen.height;
            if (deviceAspect <= 0f)
            {
                CanvasWidth = StandardWidth;
                CanvasHeight = StandardHeight;
                return;
            }

            if (deviceAspect < StandardAspect)
            {
                // 宽度适配：宽度固定，高度随比例增大
                CanvasWidth = StandardWidth;
                CanvasHeight = Mathf.RoundToInt(StandardWidth / deviceAspect);
            }
            else
            {
                // 高度适配：高度固定，宽度随比例增大
                CanvasHeight = StandardHeight;
                CanvasWidth = Mathf.RoundToInt(StandardHeight * deviceAspect);
            }
        }

        /// <summary>
        /// 安全区边距兜底：SafePadding 未就绪（如测试环境 / 异常）时直接用 Screen.safeArea 计算。
        /// 正常运行时安全区由 SafePadding 组件驱动其自身 RectTransform，此方法不改变任何状态。
        /// </summary>
        private static Vector4 FallbackSafePaddingLdur()
        {
            var safeArea = Screen.safeArea;
            var screenW = Screen.width;
            var screenH = Screen.height;
            if (screenW <= 0 || screenH <= 0)
            {
                return Vector4.zero;
            }

            var left = safeArea.xMin / screenW * CanvasWidth;
            var right = (screenW - safeArea.xMax) / screenW * CanvasWidth;
            var bottom = safeArea.yMin / screenH * CanvasHeight;
            var top = (screenH - safeArea.yMax) / screenH * CanvasHeight;

            return new Vector4(left, bottom, top, right);
        }

        /// <summary>
        /// 适配 RectTransform 分辨率（对应参考项目 ScreenHelper.AdaptResolution）。
        /// 将界面 RectTransform 归一化到父节点，并去掉嵌套 Canvas 的 CanvasScaler（避免 scaleFactor 被重置）。
        /// </summary>
        public static void AdaptResolution(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            var scaler = rt.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                var canvas = rt.GetComponent<Canvas>();
                var scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
                Object.Destroy(scaler);
                if (canvas != null)
                {
                    canvas.scaleFactor = scaleFactor;
                }
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;
            rt.sizeDelta = Vector2.zero;
        }

        /// <summary>世界坐标转 UI 世界坐标（对应参考项目 ScreenHelper.WorldPointToUIPoint(Camera, Vector3)）。</summary>
        public static Vector3 WorldPointToUIPoint(Camera worldCamera, Vector3 pos)
        {
            pos = worldCamera.WorldToScreenPoint(pos);
            var uiCamera = UICamera.Camera;
            if (uiCamera != null)
            {
                pos = uiCamera.ScreenToWorldPoint(pos);
                pos.z = 0f;
            }

            return pos;
        }

        /// <summary>UI 世界坐标转世界坐标（对应参考项目 ScreenHelper.UIPointToWorldPoint(Camera, Vector3)）。</summary>
        public static Vector3 UIPointToWorldPoint(Camera uiCamera, Vector3 pos)
        {
            if (uiCamera == null)
            {
                return pos;
            }

            return uiCamera.ScreenToWorldPoint(pos);
        }

        public static void Cleanup()
        {
            RootCanvas = null;
            UIRoot = null;
            SafeUIRoot = null;
            CanvasWidth = 0;
            CanvasHeight = 0;
        }
    }
}
