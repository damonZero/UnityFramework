//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description coverage相关扩展方法定义
//**************************************************************************************

using System.Collections.Generic;
using Framework.Coverage;
using UnityEngine;

namespace Framework.Coverage
{
    public static class CoverageExtension
    {
        /// <summary>
        /// 判断一个显示对象是否被其他显示对象组成的区域所包围
        /// </summary>
        /// <param name="coverage">显示对象</param>
        /// <param name="others">其他显示对象</param>
        /// <param name="range">总检查区域</param>
        /// <returns></returns>
        public static bool IsCoveredByOthers(this ICoverage coverage, IList<ICoverage> others, IntRect range)
        {
            var ctx = RectCheckContext.Take(range);
            var isIn = coverage.IsCoveredByCtx( others,ctx);
            RectCheckContext.Cache(ctx);
            return isIn;
        }

        /// <summary>
        /// 通过当前的检测区域和新增的显示对象组成的区域来判断显示对象是否被包围
        /// </summary>
        /// <param name="coverage">显示对象</param>
        /// <param name="additionalOther">新增显示对象</param>
        /// <param name="ctx">当前检测信息</param>
        /// <returns></returns>
        public static bool IsCoveredByCtx(this ICoverage coverage, ICoverage additionalOther,RectCheckContext ctx)
        {
            ctx.AddSide(CoverageUtil.GenerateSideList(additionalOther.CoverRectList,true));
            return coverage.IsCoveredByCtx(ctx);
        }

        /// <summary>
        /// 通过当前的检测区域和新增的显示对象列表组成的区域来判断显示对象是否被包围
        /// </summary>
        /// <param name="coverage">显示对象</param>
        /// <param name="additionalOthers">新增显示对象列表</param>
        /// <param name="ctx">当前检测信息</param>
        /// <returns></returns>
        public static bool IsCoveredByCtx(this ICoverage coverage, IList<ICoverage> additionalOthers,RectCheckContext ctx)
        {
            var otherRectList = new List<IntRect>();
            foreach (var cov in additionalOthers)
                otherRectList.AddRange(cov.CoverRectList);
            ctx.AddSide(CoverageUtil.GenerateSideList(otherRectList,true));
//            Profiler.BeginSample("Coverage Covered Check");
            var isIn = coverage.IsCoveredByCtx(ctx);
//            Profiler.EndSample();
            return isIn;
        }

        /// <summary>
        /// 通过当前的检测区域来判断显示对象是否被包围
        /// </summary>
        /// <param name="coverage">显示对象</param>
        /// <param name="ctx">当前检测信息</param>
        /// <returns></returns>
        public static bool IsCoveredByCtx(this ICoverage coverage, RectCheckContext ctx)
        {
            foreach (var rect in coverage.ShowRectList)
            {
                if (!CoverageUtil.RectIsCoveredByCtx(rect, ctx))
                    return false;
            }
            return true;
        }
    }
}
