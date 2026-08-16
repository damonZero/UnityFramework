// =================================
// 作者：WangXing-汪兴
// 创建时间：2026-04-22
// =================================

using System.Collections.Generic;
namespace Framework.View
{
    /// <summary>
    /// 可视化场景/界面的基础参数接口
    /// </summary>
    public interface IViewOptions
    {
        /// <summary>
        /// 不带后缀的资产名字
        /// </summary>
        string AssetName { get; set; }

        /// <summary>
        /// 【可选参数】开启数据
        /// </summary>
        object Data { get; set; }

        /// <summary>
        /// 【可选参数】打开(Open)时，要添加的组件
        /// </summary>
        List<IViewComponent> Components { get; set; }

        /// <summary>
        /// 【可选参数】生命周期执行器
        /// </summary>
        IViewLifeCycleExecutor LifeCycleExecutor { get; set; }
    }
}
