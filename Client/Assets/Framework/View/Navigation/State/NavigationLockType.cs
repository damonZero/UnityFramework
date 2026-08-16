//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航系统锁类型
//@Description 根据操作类型定义出的位枚举锁类型，用于控制导航对象的操作权限
//**************************************************************************************

using System;
namespace Framework.View.Navigation
{
    [Flags]
    public enum NavigationLockType
    {
        /// <summary>
        /// 可以操作单个元素
        /// </summary>
        Single = 1 << 0,

        /// <summary>
        /// 没锁(可以进行所有操作)
        /// </summary>
        None = 1 << NavigationStateType.None,

        /// <summary>
        /// 清理锁(不能进行清理操作)
        /// </summary>
        Clear = 1 << NavigationStateType.Clear,

        /// <summary>
        /// 打开锁(不能进行打开操作)
        /// </summary>
        Open = 1 << NavigationStateType.Open,

        /// <summary>
        /// 关闭锁(不能进行关闭操作)
        /// </summary>
        Close = 1 << NavigationStateType.Close,

        /// <summary>
        /// 设置逻辑可见性
        /// </summary>
        SetLogicalVisible = Close << 1,

        /// <summary>
        /// 全部锁(不能进行所有操作)
        /// </summary>
        All = Open | Close | Clear | SetLogicalVisible,

        /// <summary>
        /// 可打开全部锁(不能进行除Open外的操作)
        /// </summary>
        AllExceptOpen = Close | Clear | SetLogicalVisible
    }
}
