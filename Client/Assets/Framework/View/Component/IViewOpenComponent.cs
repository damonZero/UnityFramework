// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using Cysharp.Threading.Tasks;
namespace Framework.View
{
    public interface IViewOpenComponent : IViewComponent
    {
        #region Open生命周期

        /// <summary>
        /// View打开前的异步方法
        /// </summary>
        UniTask OnPreOpenAsync(LifeCycleArgs args);

        /// <summary>
        /// View打开时的同步方法
        /// </summary>
        void OnViewOpen(LifeCycleArgs args);

        /// <summary>
        /// View打开后的异步方法
        /// </summary>
        UniTask OnPostOpenAsync(LifeCycleArgs args);

        #endregion
    }
}
