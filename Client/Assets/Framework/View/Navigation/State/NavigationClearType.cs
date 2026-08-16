//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航容器清理类型
//@Description 根据业务需求定义出的各种清理类型
//**************************************************************************************
namespace Framework.View.Navigation
{
    public enum NavigationClearType
    {
        /// <summary>
        /// 完整状态
        /// </summary>
        Complete = 1,

        /// <summary>
        /// 清理内存
        /// </summary>
        ClearMemory = 2,

        /// <summary>
        /// 全部还原
        /// </summary>
        AllRecover = 3,

        /// <summary>
        /// 入口还原类型
        /// </summary>
        EntranceRecover = 4,

        /// <summary>
        /// 不还原类型
        /// </summary>
        NoRecover = 5
    }
}
