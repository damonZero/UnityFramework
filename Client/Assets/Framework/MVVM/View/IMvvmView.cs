using Framework.Log;
using R3;

namespace Framework.MVVM
{
    public interface IMvvmView : ICompositeDisposable
    {
        #region 生命周期清理

        /// <summary>
        /// OnDisable时，需要执行的清理操作
        /// </summary>
        CompositeDisposable DisableDisposables { get; }

        /// <summary>
        /// OnDestroy时，需要执行的清理操作
        /// </summary>
        CompositeDisposable DestroyDisposables { get; }

        #endregion

        #region mvvm绑定清理

        CompositeDisposable AutoR3Disposables { get; }

        #endregion
    }
}
