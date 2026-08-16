//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 显示对象区域判断公共接口
//**************************************************************************************

using System.Collections.Generic;
using Framework.Coverage;
using UnityEngine;

namespace Framework.Coverage
{
    /// <summary>
    /// Coverage 公共接口,工具类
    /// </summary>
    public static class CoverageUtil
    {
        /// <summary>
        /// 检测矩形是否被其他矩形区域所包围
        /// </summary>
        /// <param name="rect">待检测矩形</param>
        /// <param name="others">其他矩形</param>
        /// <param name="range">总范围</param>
        /// <returns></returns>
        public static bool RectIsCoveredByOthers(IntRect rect, IEnumerable<IntRect> others, IntRect range)
        {
            if (others == null)
                return false;
            var ctx = RectCheckContext.Take(range);
            var isIn = RectIsCoveredByCtx(rect, others, ctx);
            RectCheckContext.Cache(ctx);
            return isIn;
        }


        /// <summary>
        /// 检测矩形是否被当前检测区域和新增的矩形形成的区域所包围
        /// </summary>
        /// <param name="rect">待检测矩形</param>
        /// <param name="additionalOthers">新增矩形</param>
        /// <param name="ctx">当前检测上下文</param>
        /// <returns></returns>
        public static bool RectIsCoveredByCtx(IntRect rect, IEnumerable<IntRect> additionalOthers, RectCheckContext ctx)
        {
            if (additionalOthers == null)
                return RectIsCoveredByCtx(rect, ctx);
            ctx.AddSide(GenerateSideList(additionalOthers, true));
            return RectIsCoveredByCtx(rect, ctx);
        }

        /// <summary>
        /// 检测矩形是否被当前检测区域所包围
        /// </summary>
        /// <param name="rect">待检测矩形</param>
        /// <param name="ctx">当前检测上下文</param>
        /// <returns></returns>
        public static bool RectIsCoveredByCtx(IntRect rect, RectCheckContext ctx)
        {
            if (ctx.SideList.Count < 1)
                return false;

            var range = ctx.Range;
            var xStart = Mathf.Max(rect.X, 0);
            var yStart = Mathf.Max(rect.Y, 0);
            var xEnd = Mathf.Min(rect.X + rect.Width, range.X + range.Width);
            var yEnd = Mathf.Min(rect.Y + rect.Height, range.Y + range.Height);

            //将待检查的矩形尺寸限制到检查范围内,如果是完全和检查范围不相交，则返回false，否则只检测相交部分。
            if (xEnd < range.X || xStart > range.X + range.Width || yEnd < range.Y || yStart > range.Y + range.Height)
                return false; //完全不相交的情况
            if (xStart < range.X)
                xStart = range.X;
            if (xEnd > range.X + range.Width)
                xEnd = range.X + range.Width;
            if (yStart < range.Y)
                yStart = range.Y;
            if (yEnd > range.Y + range.Height)
                yEnd = range.Y + range.Height;

            if (yStart < ctx.SideList[0].Pos || yEnd > ctx.SideList[ctx.SideList.Count - 1].Pos)
                return false;

            //自下往上扫描
            var isIn = true;
            ctx.SegmentTree.Build(ctx.Range.X, ctx.Range.X + ctx.Range.Width);
            for (int i = 0; i < ctx.SideList.Count; i++)
            {
                var side = ctx.SideList[i];

                if (side.Pos >= yEnd)
                {
                    if (i == 0)
                        return false;

                    if (ctx.SideList[i - 1].Pos < yStart)
                    {
                        //如果直接一次性扫描过整个矩形，则至少需要判断一次
                        isIn = isIn && ctx.SegmentTree.CheckSegIsEnable(xStart, xEnd);
                    }

                    //如果扫描过了顶边，则不需要再继续扫描了
                    break;
                }

                ctx.SegmentTree.SetSegEnable(side.Start, side.End, side.Flag == 0);
                if (side.Pos >= yStart && side.Pos < yEnd)
                {
                    //这里必须用 side.Pos < yEnd 而不是≤，因为当yEnd刚好与某个矩形的顶边重合的时候，
                    //这个时候将线段树中该区间设为未激活，后续判断会出问题


                    //判断当前边的范围是否激活
                    if (!ctx.SegmentTree.CheckSegIsEnable(xStart, xEnd))
                    {
                        isIn = false;
                        break;
                    }
                }
            }

            ctx.SegmentTree.Reset();
            return isIn;
        }

        /// <summary>
        /// 构建多个矩形的所有水平/竖直边的列表
        /// </summary>
        /// <param name="rectList">矩形列表</param>
        /// <param name="isHorizontal">是否生成水平边的列表，如果为false，则生成竖直边的列表</param>
        /// <returns></returns>
        public static List<RectSide> GenerateSideList(IEnumerable<IntRect> rectList, bool isHorizontal)
        {
            List<RectSide> sideList = new List<RectSide>();
            foreach (var rect in rectList)
            {
                if (isHorizontal)
                {
                    // 构造顶边
                    var side = RectSide.Take();
                    side.Start = rect.X;
                    side.End = rect.X + rect.Width;
                    side.Pos = rect.Y + rect.Height;
                    side.Flag = 1;
                    sideList.Add(side);

                    //构造底边
                    side = RectSide.Take();
                    side.Start = rect.X;
                    side.End = rect.X + rect.Width;
                    side.Pos = rect.Y;
                    side.Flag = 0;
                    sideList.Add(side);
                }
                else
                {
                    //构造左边
                    var side = RectSide.Take();
                    side.Start = rect.Y;
                    side.End = rect.Y + rect.Height;
                    side.Pos = rect.X + rect.Width;
                    side.Flag = 0;
                    sideList.Add(side);

                    //构造右边
                    side = RectSide.Take();
                    side.Start = rect.Y;
                    side.End = rect.Y + rect.Height;
                    side.Pos = rect.X;
                    side.Flag = 1;
                    sideList.Add(side);
                }
            }

            return sideList;
        }


        /// <summary>
        /// 世界坐标转UI坐标 以屏幕的左下角为原点
        /// </summary>
        /// <param name="worldPoint"></param>
        /// <returns></returns>
        public static Vector2 WorldPointToUIPoint(Vector2 worldPoint, Canvas canvas, Vector2 desigonResolution)
        {
            Vector3 viewPortPoint;
            if (canvas.worldCamera != null)
            {
                viewPortPoint = canvas.worldCamera.WorldToViewportPoint(worldPoint);
                return new Vector2(desigonResolution.x * viewPortPoint.x, desigonResolution.y * viewPortPoint.y);
            }
            else
            {
                //如果是overlay模式，直接取世界坐标
                return worldPoint;
            }
        }

        /// <summary>
        /// 把RectTransform的世界坐标转UI坐标，并且不受锚点的影响（即：认为传入的RectTransform的中心点为左下角）
        /// 在我们游戏中，UI的世界坐标=屏幕坐标=UI坐标
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        public static Vector2 RectTransToUIPointWithoutAnchor(RectTransform trans)
        {
            var uiPos = trans.position;
            var lossyScale = trans.lossyScale;
            uiPos.x = uiPos.x - trans.rect.width * trans.pivot.x * lossyScale.x;
            uiPos.y = uiPos.y - trans.rect.height * trans.pivot.y * lossyScale.y;
            return uiPos;
        }
    }
}
