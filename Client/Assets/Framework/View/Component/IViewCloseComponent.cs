// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using Cysharp.Threading.Tasks;
namespace Framework.View
{
    public interface IViewCloseComponent : IViewComponent
    {
        #region Close生命周期

        /// <summary>
        /// View关闭前的异步方法
        /// </summary>
        UniTask OnPreCloseAsync(LifeCycleArgs args);

        /// <summary>
        /// View关闭时的同步方法
        /// </summary>
        void OnViewClose(LifeCycleArgs args);

        /// <summary>
        /// View关闭后的异步方法
        /// </summary>
        UniTask OnPostCloseAsync(LifeCycleArgs args);

        #endregion
    }
}
