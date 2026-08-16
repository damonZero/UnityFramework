// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

namespace Framework.View
{
    public interface IViewDestroyComponent : IViewComponent
    {
        /// <summary>
        /// 视图销毁时调用
        /// </summary>
        void OnViewDestroy();
    }
}
