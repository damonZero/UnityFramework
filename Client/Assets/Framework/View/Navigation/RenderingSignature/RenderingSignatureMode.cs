namespace Framework.View.Navigation
{
    /// <summary>
    /// 渲染签名采样帧来源。
    /// </summary>
    public enum RenderingSignatureFrame
    {
        /// <summary>
        /// 当前帧实时采集。
        /// </summary>
        Current = 0,

        /// <summary>
        /// 上一帧实际渲染画面（最近一次 CaptureAllModes 快照）。
        /// </summary>
        Snapshot = 1,
    }

    /// <summary>
    /// 渲染签名模式：控制 <see cref="INavigationRenderingSignatureTracker"/> 采集与比对
    /// "参与渲染画面的 View 集合" 时所使用的策略。
    /// Rendering signature mode: controls which Views are included when
    /// <see cref="INavigationRenderingSignatureTracker"/> captures and compares frame snapshots.
    /// </summary>
    public enum RenderingSignatureMode
    {
        /// <summary>
        /// 禁用渲染签名检测。
        /// 当前不会采集或比对任何渲染签名；
        /// <see cref="INavigationRenderingSignatureTracker.HasRenderingSignatureChanged"/> 与
        /// <see cref="INavigationRenderingSignatureTracker.HasStableRenderingSignature"/> 都会返回 false。
        /// Rendering signature detection disabled: no signature is collected or compared.
        /// </summary>
        None = 0,

        /// <summary>
        /// 仅对比 "最顶层全屏渲染 View" 是否变化。
        /// Only compare the identity of the top-most full-screen rendering View.
        /// 覆盖典型闪烁场景：顶层全屏 View 被关闭后，底层另一个全屏 View 被暴露出来。
        /// </summary>
        TopmostFullScreen = 1,

        /// <summary>
        /// 对比"最顶层全屏渲染 View 及其 Layer 之上的所有渲染 View"是否变化。
        /// Compare the top-most full-screen rendering View plus all rendering Views stacked above it.
        /// 覆盖更全面的场景：顶层叠加的半屏/悬浮 View 的变化也会影响可见画面构成。
        /// </summary>
        AboveTopmostFullScreen = 2,
    }
}
