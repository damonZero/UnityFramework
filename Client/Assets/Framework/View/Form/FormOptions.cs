// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// ********************************************************************

using System.Collections.Generic;
namespace Framework.View
{
    public class FormOptions : IViewOptions
    {
        /// <summary>
        /// 【必填参数】界面层级
        /// </summary>
        public int Layer { get; set; } = -1;

        /// <summary>
        /// 【可选参数】不带后缀的资产名字
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


        public virtual void Reset()
        {
            AssetName = null;
            Data = null;
            Layer = -1;
            Components?.Clear();
            LifeCycleExecutor = null;
        }
    }
}
