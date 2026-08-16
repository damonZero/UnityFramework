using System;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 导航模式
    /// </summary>
    [Flags]
    public enum NavigationMode
    {
        /// <summary>
        /// 查找已打开实例，若不存在则打开新实例
        /// </summary>
        FindOrNew = 1 << 0,

        /// <summary>
        /// 总是打开新实例，不检查是否存在已打开的实例
        /// </summary>
        AlwaysNew = 1 << 1,

        /// <summary>
        /// 跳转到已打开实例所在容器
        /// </summary>
        JumpToContainer = 1 << 2,


        // ======= 以下是组合模式 =======

        /// <summary>
        /// 检查是否存在已打开的实例:
        ///     若存在, 则跳转到已打开的实例所在容器
        ///     不存在, 否则打开新实例并跳转
        /// </summary>
        OpenAndJump = FindOrNew | JumpToContainer,

        /// <summary>
        /// 打开纯新实例，不检查是否存在已打开的实例
        /// 打开后，跳转到新实例所在容器
        /// </summary>
        NewAndJump = AlwaysNew | JumpToContainer,

        /// <summary>
        /// 检查是否已打开，若已打开则忽略，否则打开新实例
        /// 不跳转
        /// </summary>
        OpenOnly = FindOrNew,

        /// <summary>
        /// 打开纯新实例，不检查是否存在已打开的实例
        /// 不跳转
        /// </summary>
        NewOnly = AlwaysNew,
    }
}
