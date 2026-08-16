namespace Framework.View.Navigation
{
    public interface INavigateBehaviour
    {
        /// <summary>
        /// 名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 当前状态
        /// </summary>
        NavigationStateType CurrentState { get; }

        /// <summary>
        /// 锁类型
        /// </summary>
        NavigationLockType LockType { get; set; }

        /// <summary>
        /// 是否为全屏
        /// </summary>
        /// <returns></returns>
        bool IsFullScreen();
    }
}
