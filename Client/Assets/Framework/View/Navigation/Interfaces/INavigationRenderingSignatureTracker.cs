namespace Framework.View.Navigation
{
    /// <summary>
    /// 导航渲染签名追踪器接口。
    /// <para>
    /// 职责：维护 "上一帧实际渲染画面" 的 View 构成快照，并提供跨帧变化/稳定性查询。
    /// </para>
    /// <para>
    /// 内置两种实现：
    /// <list type="bullet">
    /// <item><see cref="NavigationAutoRenderingSignatureTracker"/>：MonoBehaviour 实现，在
    ///     每帧 <c>WaitForEndOfFrame</c> 自动调用 <see cref="CaptureAllModes"/>。</item>
    /// <item><see cref="NavigationManualRenderingSignatureTracker"/>：普通 C# 类，由外部在适当时机
    ///     主动调用 <see cref="CaptureAllModes"/>，适用于需要精细控制捕获时机的场景。</item>
    /// </list>
    /// </para>
    /// </summary>
    public interface INavigationRenderingSignatureTracker
    {
        /// <summary>
        /// 判断当前 View 渲染构成是否与上一次 <see cref="CaptureAllModes"/> 时不同。
        /// <para>
        /// mode == <see cref="RenderingSignatureMode.None"/>、尚未完成首次捕获，
        /// 或当前无法形成有效渲染签名时返回 false。
        /// </para>
        /// </summary>
        bool HasRenderingSignatureChanged(RenderingSignatureMode mode);

        /// <summary>
        /// 判断当前是否仍存在稳定的渲染签名。
        /// <para>
        /// 仅当当前帧仍能采集到该 <see cref="RenderingSignatureMode"/> 对应的渲染签名，且其与上一次
        /// <see cref="CaptureAllModes"/> 时的快照完全一致时返回 true。
        /// 若当前帧已无法采集到该 mode 对应的渲染签名，则返回 false。
        /// </para>
        /// </summary>
        bool HasStableRenderingSignature(RenderingSignatureMode mode);

        /// <summary>
        /// 获取指定 mode 在指定帧来源中的渲染签名 View 列表。
        /// <para>
        /// 会先清空 <paramref name="output"/>，再填充结果并返回数量。
        /// </para>
        /// </summary>
        int GetRenderingSignatureViews(RenderingSignatureMode mode, System.Collections.Generic.List<ViewBase> output,
            RenderingSignatureFrame frame = RenderingSignatureFrame.Current);

        /// <summary>
        /// 获取指定 mode 在指定帧来源中的首个渲染签名 View。
        /// </summary>
        bool TryGetRenderingSignatureView(RenderingSignatureMode mode, out ViewBase view,
            RenderingSignatureFrame frame = RenderingSignatureFrame.Current);

        /// <summary>
        /// 捕获当前帧所有 <see cref="RenderingSignatureMode"/> 的渲染签名，作为下次比对的基准快照。
        /// <para>
        /// 自动实现（<see cref="NavigationAutoRenderingSignatureTracker"/>）会在每帧渲染完成后自动调用此方法；
        /// 手动实现（<see cref="NavigationManualRenderingSignatureTracker"/>）需由外部在渲染完成后主动调用。
        /// </para>
        /// </summary>
        void CaptureAllModes();

#if DEBUG_MODE
        /// <summary>
    /// 构建渲染签名调试文本（含前一帧快照 + 当前帧，按 mode 输出）。
        /// </summary>
    string GetRenderingSignatureDebugInfo(RenderingSignatureMode mode, bool richText = false);
#endif
    }
}
