//**************************************************************************************
// Rendering Signature Snapshot — shared logic core for Navigation signature trackers.
// 渲染签名快照——Navigation 签名追踪器的共享逻辑核心。
// 不依赖 MonoBehaviour，由两种追踪器实现通过组合使用。
//**************************************************************************************

using System.Collections.Generic;
using UnityEngine.Pool;
#if DEBUG_MODE
using System.Text;
using Unity.Profiling;
#endif
namespace Framework.View.Navigation
{
    /// <summary>
    /// 渲染签名快照核心：封装快照存储、捕获、比对逻辑。
    /// <para>
    /// 通过 <see cref="CaptureAllModes"/> 写入 "上一帧实际渲染画面" 的快照，
    /// 并通过 <see cref="HasRenderingSignatureChanged"/> / <see cref="HasStableRenderingSignature"/>
    /// 查询当前状态与快照之间的关系。
    /// 由 <see cref="NavigationAutoRenderingSignatureTracker"/> 和 <see cref="NavigationManualRenderingSignatureTracker"/> 持有并委托调用。
    /// </para>
    /// </summary>
    internal sealed class NavigationRenderingSignatureSnapshot
    {
#if DEBUG_MODE
        private static readonly ProfilerMarker _markerCaptureAllModes =
            new("NavigationRenderingSignatureSnapshot.CaptureAllModes");
        private static readonly ProfilerMarker _markerEvaluateSignature =
            new("NavigationRenderingSignatureSnapshot.EvaluateRenderingSignature");
#endif

        /// <summary>
        /// 需要追踪的所有 <see cref="RenderingSignatureMode"/>（排除 None）。
        /// </summary>
        private static readonly RenderingSignatureMode[] _allModes =
        {
            RenderingSignatureMode.TopmostFullScreen,
            RenderingSignatureMode.AboveTopmostFullScreen,
        };

        /// <summary>
        /// 最近一次 <see cref="CaptureAllModes"/> 时捕获的快照（按 mode 分开存储）。
        /// </summary>
        private readonly Dictionary<RenderingSignatureMode, List<ViewBase>> _snapshots = new();

        /// <summary>
        /// 标记是否已完成过至少一次快照。
        /// 首帧快照尚未执行时，变化检测与稳定性检测都返回 false，避免误判。
        /// </summary>
        private bool _hasSnapshot;

        /// <summary>
        /// 判断当前 View 渲染构成是否与上次 <see cref="CaptureAllModes"/> 时的快照不同。
        /// <para>
        /// mode == <see cref="RenderingSignatureMode.None"/>、尚未建立首个快照，
        /// 或当前无法形成有效渲染签名时返回 false。
        /// </para>
        /// </summary>
        public bool HasRenderingSignatureChanged(RenderingSignatureMode mode)
        {
            EvaluateRenderingSignature(mode, out _, out var changed);
            return changed;
        }

        /// <summary>
        /// 判断当前是否仍存在稳定的渲染签名。
        /// <para>
        /// 返回 true 表示：当前仍能采集到该 mode 对应的渲染签名，且与上一次快照完全一致。
        /// 若当前帧已没有该 mode 对应的渲染签名，则返回 false。
        /// </para>
        /// </summary>
        public bool HasStableRenderingSignature(RenderingSignatureMode mode)
        {
            EvaluateRenderingSignature(mode, out var hasRenderingSignature, out var changed);
            return hasRenderingSignature && !changed;
        }

        /// <summary>
        /// 获取指定 mode 在指定帧来源中的渲染签名 View 列表。
        /// </summary>
        public int GetRenderingSignatureViews(RenderingSignatureMode mode, List<ViewBase> output,
            RenderingSignatureFrame frame = RenderingSignatureFrame.Current)
        {
            output.Clear();
            if (mode == RenderingSignatureMode.None) return 0;

            switch (frame)
            {
                case RenderingSignatureFrame.Snapshot:
                    if (!_hasSnapshot) return 0;
                    if (!_snapshots.TryGetValue(mode, out var snapshot) || snapshot == null) return 0;

                    for (var i = 0; i < snapshot.Count; i++)
                    {
                        var view = snapshot[i];
                        if (view != null)
                        {
                            output.Add(view);
                        }
                    }

                    return output.Count;

                case RenderingSignatureFrame.Current:
                default:
                    var root = NavigationManager.Instance?.Root;
                    if (root == null) return 0;

                    var raw = ListPool<ViewBase>.Get();
                    try
                    {
                        root.CollectRenderingSignature(mode, raw);
                        for (var i = 0; i < raw.Count; i++)
                        {
                            var view = raw[i];
                            if (view != null)
                            {
                                output.Add(view);
                            }
                        }

                        return output.Count;
                    }
                    finally
                    {
                        ListPool<ViewBase>.Release(raw);
                    }
            }
        }

        /// <summary>
        /// 获取指定 mode 在指定帧来源中的首个渲染签名 View。
        /// </summary>
        public bool TryGetRenderingSignatureView(RenderingSignatureMode mode, out ViewBase view,
            RenderingSignatureFrame frame = RenderingSignatureFrame.Current)
        {
            view = null;

            var views = ListPool<ViewBase>.Get();
            try
            {
                GetRenderingSignatureViews(mode, views, frame);
                if (views.Count <= 0) return false;

                view = views[0];
                return view != null;
            }
            finally
            {
                ListPool<ViewBase>.Release(views);
            }
        }

        /// <summary>
        /// 捕获当前帧所有 Mode 的渲染签名快照。
        /// </summary>
        public void CaptureAllModes()
        {
#if DEBUG_MODE
            using var _ = _markerCaptureAllModes.Auto();
#endif
            var root = NavigationManager.Instance?.Root;
            if (root == null) return;

            var firstCapture = !_hasSnapshot;

            foreach (var mode in _allModes)
            {
                if (!_snapshots.TryGetValue(mode, out var list) || list == null)
                {
                    list = ListPool<ViewBase>.Get();
                    _snapshots[mode] = list;
                }
                list.Clear();
                root.CollectRenderingSignature(mode, list);
            }
            _hasSnapshot = true;

            if (firstCapture)
            {
                Log.Debug("[NavigationRenderingSignatureSnapshot] First rendering signature snapshot captured.");
            }
        }

#if DEBUG_MODE
        /// <summary>
        /// 构建渲染签名调试文本（含前一帧快照 + 当前帧，按 mode 输出）。
        /// </summary>
        public string GetRenderingSignatureDebugInfo(RenderingSignatureMode mode, bool richText = false)
        {
            if (mode == RenderingSignatureMode.None)
                return "mode=None (signature disabled)";

            var prevLabel = richText ? "<color=#8BC34AFF>▶ 前一帧</color>" : "▶ 前一帧";
            var currLabel = richText ? "<color=#FFD54FFF>▶ 当前帧</color>" : "▶ 当前帧";
            var separator = richText
                ? "<color=#66FFFFFF>═══════════════════</color>"
                : "═══════════════════";

            var sb = new StringBuilder(512);
            var prev = ListPool<ViewBase>.Get();
            var curr = ListPool<ViewBase>.Get();
            try
            {
                var prevCount = GetRenderingSignatureViews(mode, prev, RenderingSignatureFrame.Snapshot);
                var currCount = GetRenderingSignatureViews(mode, curr, RenderingSignatureFrame.Current);

                sb.Append(prevLabel).Append('\n');
                sb.Append($"snapshot: mode={mode}, count={prevCount}");
                AppendViews(sb, prev);

                sb.Append('\n').Append(separator).Append('\n');

                sb.Append(currLabel).Append('\n');
                sb.Append($"current: mode={mode}, count={currCount}");
                AppendViews(sb, curr);

                return sb.ToString();
            }
            finally
            {
                ListPool<ViewBase>.Release(prev);
                ListPool<ViewBase>.Release(curr);
            }
        }

        private static void AppendViews(StringBuilder sb, List<ViewBase> views)
        {
            for (var i = 0; i < views.Count; i++)
            {
                var view = views[i];
                sb.Append(view == null ? $"\n  [{i}] <null>" : $"\n  [{i}] {view.GetType().Name}({view.name})");
            }
        }
#endif

        /// <summary>
        /// 释放所有池化列表，应在追踪器销毁时调用。
        /// </summary>
        public void Dispose()
        {
            foreach (var kv in _snapshots)
            {
                if (kv.Value != null) ListPool<ViewBase>.Release(kv.Value);
            }
            _snapshots.Clear();
            _hasSnapshot = false;
        }

        private static bool SignatureEquals(List<ViewBase> a, List<ViewBase> b)
        {
            if (a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!ReferenceEquals(a[i], b[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// 统一评估当前渲染签名状态。
        /// <para>
        /// 一次采集同时得到两个结果：
        /// 当前帧是否仍存在该 mode 对应的有效渲染签名，以及它相较快照是否发生变化。
        /// </para>
        /// </summary>
        private void EvaluateRenderingSignature(RenderingSignatureMode mode, out bool hasRenderingSignature,
            out bool changed)
        {
#if DEBUG_MODE
            using var _ = _markerEvaluateSignature.Auto();
#endif
            hasRenderingSignature = false;
            changed = false;

            if (mode == RenderingSignatureMode.None) return;
            if (!_hasSnapshot) return;
            if (!_snapshots.TryGetValue(mode, out var snapshot)) return;

            var root = NavigationManager.Instance?.Root;
            if (root == null)
            {
                changed = snapshot.Count > 0;
                return;
            }

            var current = ListPool<ViewBase>.Get();
            try
            {
                root.CollectRenderingSignature(mode, current);
                hasRenderingSignature = current.Count > 0;
                changed = !SignatureEquals(current, snapshot);

                if (changed)
                {
                    Log.Debug($"[NavigationRenderingSignatureSnapshot] Signature changed. mode={mode}, currentCount={current.Count}, snapshotCount={snapshot.Count}");
                }
            }
            finally
            {
                ListPool<ViewBase>.Release(current);
            }
        }
    }
}
