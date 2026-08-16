//**************************************************************************************
// Auto Rendering Signature Tracker — MonoBehaviour implementation.
// 自动渲染签名追踪器——MonoBehaviour 实现，每帧 WaitForEndOfFrame 自动捕获快照。
//**************************************************************************************

using System.Collections;
using UnityEngine;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 自动渲染签名追踪器（MonoBehaviour 实现）。
    /// <para>
    /// 在每一帧 <c>WaitForEndOfFrame</c>（渲染完成后）自动调用
    /// <see cref="INavigationRenderingSignatureTracker.CaptureAllModes"/>，
    /// 维护 "上一帧实际渲染画面" 的 View 构成快照，供跨帧变化/稳定性检测使用。
    /// </para>
    /// <para>
    /// 由 <see cref="NavigationManager"/> 在 Init 时自动创建并绑定，外部通常无需手动管理。
    /// </para>
    /// </summary>
    internal sealed class NavigationAutoRenderingSignatureTracker : MonoBehaviour,
        INavigationRenderingSignatureTracker
    {
        private readonly NavigationRenderingSignatureSnapshot _snapshot = new();
        private Coroutine _captureCoroutine;

        /// <summary>
        /// 创建默认自动渲染签名追踪器，并完成宿主 GameObject 的隐藏与跨场景保活配置。
        /// </summary>
        internal static NavigationAutoRenderingSignatureTracker Create(out GameObject host)
        {
            host = new GameObject(nameof(NavigationAutoRenderingSignatureTracker))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            DontDestroyOnLoad(host);
            return host.AddComponent<NavigationAutoRenderingSignatureTracker>();
        }

        #region INavigationRenderingSignatureTracker

        /// <inheritdoc/>
        public bool HasRenderingSignatureChanged(RenderingSignatureMode mode)
            => _snapshot.HasRenderingSignatureChanged(mode);

        /// <inheritdoc/>
        public bool HasStableRenderingSignature(RenderingSignatureMode mode)
            => _snapshot.HasStableRenderingSignature(mode);

        /// <inheritdoc/>
        public int GetRenderingSignatureViews(RenderingSignatureMode mode, System.Collections.Generic.List<ViewBase> output,
            RenderingSignatureFrame frame = RenderingSignatureFrame.Current)
            => _snapshot.GetRenderingSignatureViews(mode, output, frame);

        /// <inheritdoc/>
        public bool TryGetRenderingSignatureView(RenderingSignatureMode mode, out ViewBase view,
            RenderingSignatureFrame frame = RenderingSignatureFrame.Current)
            => _snapshot.TryGetRenderingSignatureView(mode, out view, frame);

        /// <inheritdoc/>
        public void CaptureAllModes() => _snapshot.CaptureAllModes();

#if DEBUG_MODE
        /// <inheritdoc/>
    public string GetRenderingSignatureDebugInfo(RenderingSignatureMode mode, bool richText = false)
        => _snapshot.GetRenderingSignatureDebugInfo(mode, richText);
#endif

        #endregion

        #region Unity lifecycle

        private void OnEnable()
        {
            _captureCoroutine ??= StartCoroutine(CaptureLoop());
        }

        private void OnDisable()
        {
            if (_captureCoroutine != null)
            {
                StopCoroutine(_captureCoroutine);
                _captureCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            _snapshot.Dispose();
        }

        #endregion

        #region private

        private IEnumerator CaptureLoop()
        {
            var waitForEndOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return waitForEndOfFrame;
                _snapshot.CaptureAllModes();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        #endregion
    }
}
