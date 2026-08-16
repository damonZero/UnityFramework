//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 显示对象接口
//**************************************************************************************

using System.Collections.Generic;
using Framework.Coverage;

namespace Framework.Coverage
{

    /// <summary>
    /// 遮挡/显示单位
    /// </summary>
    public interface ICoverage
    {
        /// <summary>
        /// 当前Coverage自身的Visible状态
        /// </summary>
        bool CoverageVisible { get; }

        /// <summary>
        /// 所属对象是否真正被渲染
        /// </summary>
        bool ActiveAndRendering { get; }

        /// <summary>
        /// 获取显示矩形列表
        /// </summary>
        /// <returns></returns>
        IEnumerable<IntRect> ShowRectList { get; }

        /// <summary>
        /// 获取遮挡矩形列表
        /// </summary>
        /// <returns></returns>
        IEnumerable<IntRect> CoverRectList { get; }
    }
}
