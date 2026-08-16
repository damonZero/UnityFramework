//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航对象状态类型
//@Description 一部分状态是可以外部操作的,一部分状态是导航系统控制的,与生命周期相关
//**************************************************************************************
namespace Framework.View.Navigation
{
    public enum NavigationStateType
    {
        /// <summary>
        /// 空状态
        /// </summary>
        None = 1,

        /// <summary>
        /// 清理
        /// </summary>
        Clear = 2,

        /// <summary>
        /// 打开
        /// </summary>
        Open = 3,

        /// <summary>
        /// 关闭
        /// </summary>
        Close = 6,

        /// <summary>
        /// 跳转中
        /// </summary>
        Jumping = 7,
    }
}
