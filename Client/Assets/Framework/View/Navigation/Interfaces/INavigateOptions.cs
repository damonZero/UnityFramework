using Framework.View.Navigation;

namespace Framework.View.Navigation
{
    /// <summary>
    /// 进行视图导航（创建导航器、跳转界面/场景）的参数项
    /// </summary>
    public interface INavigateOptions : IViewOptions
    {
        /// <summary>
        /// 导航容器名称
        /// </summary>
        string Container { get; set; }

        /// <summary>
        /// 导航模式，默认 OpenAndJump
        /// </summary>
        NavigationMode Mode { get; set; }

        /// <summary>
        /// 转场效果
        /// </summary>
        ITransition Transition { get; set; }

        /// <summary>
        /// 重置为初始状态（对象池复用前）。
        /// </summary>
        void Reset();

        /// <summary>
        /// 回收到所属对象池。
        /// </summary>
        void RecycleToPool();
    }
}
