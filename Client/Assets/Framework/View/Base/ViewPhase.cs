// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

namespace Framework.View
{
    /// <summary>
    /// View 生命周期阶段（稳态）
    ///
    /// 仅表示已完成的稳定状态，不含过渡态。
    /// 过渡状态通过 ViewBase.PendingPhase 字段跟踪。
    ///
    /// ═══════════════════  状态流转图  ═══════════════════
    ///
    ///   主线性流程（不可逆）：
    ///
    ///     None ───► Opened ───► Shown ───► Hidden ───► Closed
    ///                            ▲            │
    ///                            │            │
    ///                            └────────────┘
    ///                          可见性循环（可逆）
    ///
    ///   规则：
    ///     · Shown ↔ Hidden  可双向切换（显示/隐藏循环）
    ///     · Hidden → Closed 仅从 Hidden 进入关闭
    ///     · 其余转换均为单向，不可回退
    ///
    /// ═══════════════  过渡过程（PendingPhase）  ═══════════════
    ///
    ///   CurrentPhase  │  PendingPhase  │  完成后 CurrentPhase  │  说明
    ///  ───────────────┼────────────────┼──────────────────────┼────────
    ///   None          │  Opened        │  Opened              │  打开
    ///   Opened        │  Shown         │  Shown               │  首次显示
    ///   Shown         │  Hidden        │  Hidden              │  隐藏
    ///   Hidden        │  Shown         │  Shown               │  重新显示
    ///   Hidden        │  Closed        │  Closed              │  关闭
    ///
    /// ═══════════════  关键属性语义  ═══════════════
    ///
    ///   IsPhaseChanging : PendingPhase != None（正在过渡中）
    ///   Running         : CurrentPhase ∈ {Opened, Shown, Hidden}
    ///   Rendering       : CurrentPhase == Shown
    ///
    /// </summary>
    public enum ViewPhase : byte
    {
        /// <summary>未激活（初始状态 / 已回收）</summary>
        None = 0,

        /// <summary>Open 流程已完成，等待首次显示</summary>
        Opened = 1,

        /// <summary>显示中（正常运行，可见）</summary>
        Shown = 2,

        /// <summary>已隐藏（运行中，不可见）</summary>
        Hidden = 3,

        /// <summary>已关闭，等待销毁</summary>
        Closed = 4,
    }
}
