using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 导航容器：
    ///     可以容纳多个导航加载器Loader
    ///     可以容纳多个子导航容器Container，形成树形结构
    ///
    /// 提供打开、关闭、跳转View等导航功能
    /// </summary>
    public interface INavigateContainer : INavigateBehaviour
    {
        #region 适用于界面的简化调用API

        /// <summary>
        /// 打开界面
        /// </summary>
        /// <param name="layer">界面显示层级</param>
        /// <param name="data">附带的数据，可选参数</param>
        /// <param name="formName">界面预制体名字，可选参数</param>
        /// <typeparam name="TForm">界面脚本类型</typeparam>
        /// <returns></returns>
        UniTask<TForm> OpenFormAsync<TForm>(int layer, object data = null, string formName = null)
            where TForm : BaseForm;

        #endregion

        #region 适用于场景的简化调用API

        /// <summary>
        /// 打开场景
        /// </summary>
        /// <param name="data">数据，可选参数</param>
        /// <param name="sceneName">场景名称，可选参数</param>
        /// <typeparam name="TScene"></typeparam>
        /// <returns></returns>
        UniTask<TScene> OpenSceneAsync<TScene>(object data = null, string sceneName = null)
            where TScene : BaseScene;

        #endregion

        #region 同时适用于场景和界面的API

        /// <summary>
        /// 查找加载器
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <returns></returns>
        INavigateLoader FindLoader<TView>() where TView : ViewBase;

        /// <summary>
        /// 查找或创建加载器
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <returns></returns>
        INavigateLoader FindOrCreateLoader<TView>(string viewName = null) where TView : ViewBase;

        /// <summary>
        /// 添加一个View到导航容器中，不执行打开动作
        /// 若View已存在，如果已经存在，则仅设置options
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <returns>返回View对应的加载器</returns>
        INavigateLoader AddView<TView>(INavigateOptions options) where TView : ViewBase;

        /// <summary>
        /// 打开View，支持全部参数项
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="TView"></typeparam>
        /// <returns></returns>
        UniTask<TView> OpenViewAsync<TView>(INavigateOptions options) where TView : ViewBase;

        /// <summary>
        /// 查找View
        /// </summary>
        /// <typeparam name="TView">View类型</typeparam>
        /// <param name="viewName">View名称，可选参数</param>
        TView FindView<TView>(string viewName = null) where TView : ViewBase;

        #endregion

        #region 容器操作

        /// <summary>
        /// 是否为空（不含任何子容器和loader）
        /// </summary>
        bool Empty { get; }

        /// <summary>
        /// 获取最后一个操作的导航容器
        /// </summary>
        /// <param name="firstLayer">只在首层子节点查找</param>
        /// <returns></returns>
        INavigateContainer LastContainer(bool firstLayer = false);

        #endregion
    }
}
