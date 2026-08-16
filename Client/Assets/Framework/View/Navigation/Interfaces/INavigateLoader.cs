using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 导航加载器
    /// </summary>
    public interface INavigateLoader : IViewLifeCycleExecutor
    {
        /// <summary>
        /// 打开视图（界面或场景）
        /// </summary>
        /// <returns></returns>
        UniTask<TView> OpenViewAsync<TView>() where TView : ViewBase;
    }
}
