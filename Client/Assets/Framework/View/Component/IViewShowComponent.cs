// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using Cysharp.Threading.Tasks;
namespace Framework.View
{
    public interface IViewShowComponent : IViewComponent
    {
        #region Show生命周期

        /// <summary>
        /// View显示前的异步方法
        /// </summary>
        UniTask OnPreShowAsync(LifeCycleArgs args);

        /// <summary>
        /// View显示时的同步方法
        /// </summary>
        void OnViewShow(LifeCycleArgs args);

        /// <summary>
        /// View显示后的异步方法
        /// </summary>
        UniTask OnPostShowAsync(LifeCycleArgs args);

        #endregion
    }
}
