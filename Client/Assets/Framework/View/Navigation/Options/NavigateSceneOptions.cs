
using System.Collections.Generic;
using Framework.Pool;
using Framework.View.Navigation;
namespace Framework.View.Navigation
{
    public class NavigateSceneOptions : INavigateOptions
    {
        /// <summary>
        /// 导航容器名称
        /// </summary>
        public string Container { get; set; }

        /// <summary>
        /// 视图名称
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// 【可选参数】开启数据
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// 【可选参数】要添加的组件
        /// </summary>
        public List<IViewComponent> Components { get; set; }

        public IViewLifeCycleExecutor LifeCycleExecutor { get; set; }

        /// <summary>
        /// 导航模式，默认 OpenAndJump
        /// </summary>
        public NavigationMode Mode { get; set; }

        /// <summary>
        /// 【可选参数】转场效果
        /// </summary>
        public ITransition Transition { get; set; }

        #region ObjectPool 对象池相关

        public static ObjectPool<NavigateSceneOptions> Pool { get; } =
            NavigationFactory.Instance.CreatePool(() => new NavigateSceneOptions(), 32);

        public void RecycleToPool()
        {
            Pool.Return(this);
        }

        public void Reset()
        {
            AssetName = null;
            Data = null;
            Components?.Clear();
            LifeCycleExecutor = null;

            Container = null;
            Mode = NavigationMode.OpenAndJump;

            Transition?.RecycleToPool();
            Transition = TransitionFactory.CreateDefault?.Invoke();
        }

        #endregion

        #region private

        private NavigateSceneOptions()
        {
        }

        #endregion
    }
}
