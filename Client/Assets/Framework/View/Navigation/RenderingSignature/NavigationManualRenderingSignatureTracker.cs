//**************************************************************************************
// Manual Rendering Signature Tracker — plain C# class implementation.
// 手动渲染签名追踪器——普通 C# 类，由外部主动调用 CaptureAllModes。
//**************************************************************************************

using System;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 手动渲染签名追踪器（普通 C# 类实现）。
    /// <para>
    /// 不内置任何驱动机制，完全由外部在适当时机调用 <see cref="CaptureAllModes"/>。
    /// 适用于需要精细控制捕获时机的场景（如在特定系统的 LateUpdate 或自定义渲染管线回调中捕获）。
    /// </para>
    /// <para>
    /// 使用示例：
    /// <code>
    /// var tracker = new NavigationManualRenderingSignatureTracker();
    /// NavigationManager.Instance.SetRenderingSignatureTracker(tracker);
    /// // 在每帧渲染完成后调用：
    /// tracker.CaptureAllModes();
    /// // 查询：
    /// bool changed = tracker.HasRenderingSignatureChanged(RenderingSignatureMode.TopmostFullScreen);
    /// // 生命周期结束时：
    /// tracker.Dispose();
    /// </code>
    /// </para>
    /// </summary>
    public sealed class NavigationManualRenderingSignatureTracker : INavigationRenderingSignatureTracker, IDisposable
    {
        private readonly NavigationRenderingSignatureSnapshot _snapshot = new();

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
        /// <remarks>
        /// 应在每帧渲染完成（等价于 <c>WaitForEndOfFrame</c>）后调用，
        /// 以确保写入的快照严格对应“上一帧实际渲染画面”。
        /// </remarks>
        public void CaptureAllModes() => _snapshot.CaptureAllModes();

#if DEBUG_MODE
        /// <inheritdoc/>
    public string GetRenderingSignatureDebugInfo(RenderingSignatureMode mode, bool richText = false)
        => _snapshot.GetRenderingSignatureDebugInfo(mode, richText);
#endif

        #endregion

        /// <summary>
        /// 释放所有池化资源。生命周期结束时应调用此方法。
        /// </summary>
        public void Dispose() => _snapshot.Dispose();
    }
}
