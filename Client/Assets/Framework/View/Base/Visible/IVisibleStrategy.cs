// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

namespace Framework.View
{
    /// <summary>
    /// View隐藏和显示的处理策略（比如enable/disable Camera或GameObject等）
    /// </summary>
    public interface IVisibleStrategy
    {
        void SetVisible(ViewBase view, bool visible);
    }
}
