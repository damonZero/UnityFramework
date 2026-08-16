using Framework.Pool;
using Framework.View.Navigation;
namespace Framework.View.Navigation
{
    public class NavigateFormOptions : FormOptions, INavigateOptions
    {
        /// <summary>
        /// 【可选参数】导航容器名称，不传时使用最后一个导航容器
        /// </summary>
        public string Container { get; set; }

        /// <summary>
        /// 【可选参数】导航模式，默认 OpenAndJump
        /// </summary>
        public NavigationMode Mode { get; set; }

        /// <summary>
        /// 【可选参数】转场效果
        /// </summary>
        public ITransition Transition { get; set; }

        #region ObjectPool 对象池相关

        public static ObjectPool<NavigateFormOptions> Pool { get; } =
            NavigationFactory.Instance.CreatePool(() => new NavigateFormOptions(), 32);

        public void RecycleToPool()
        {
            Pool.Return(this);
        }

        public override void Reset()
        {
            base.Reset();
            Container = null;
            Mode = NavigationMode.OpenAndJump;

            Transition?.RecycleToPool();
            Transition = TransitionFactory.CreateDefault?.Invoke();
        }

        #endregion

        #region private

        private NavigateFormOptions()
        {
        }

        #endregion
    }
}
