// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

namespace Framework.View
{
    public interface IViewActiveComponent : IViewComponent
    {
        void OnViewEnable();

        void OnViewDisable();
    }
}
