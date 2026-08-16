// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using Cysharp.Threading.Tasks;
namespace Framework.View
{
    public interface IViewHideComponent : IViewComponent
    {
        #region Hide生命周期

        /// <summary>
        /// View隐藏前的异步方法
        /// </summary>
        UniTask OnPreHideAsync(LifeCycleArgs args);

        /// <summary>
        /// View隐藏时的同步方法
        /// </summary>
        void OnViewHide(LifeCycleArgs args);

        /// <summary>
        /// View隐藏后的异步方法
        /// </summary>
        UniTask OnPostHideAsync(LifeCycleArgs args);

        #endregion
    }
}
