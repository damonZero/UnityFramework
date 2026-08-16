//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 矩形检测相关上下文
//**************************************************************************************

using System.Collections.Generic;
using Framework.Coverage;

namespace Framework.Coverage
{
    /// <summary>
    /// 矩形检测上下文
    /// 用于保存线段树，线段等信息
    /// 用于增量检测，而不用每次去重新构造线段树
    /// </summary>
    public class RectCheckContext : Pool<RectCheckContext>
    {
        /// <summary>
        ///线段树
        /// </summary>
        public SegmentTree SegmentTree { get; }

        /// <summary>
        /// 检测范围
        /// </summary>
        public IntRect Range { get; private set; }

        /// <summary>
        /// 最小边界
        /// </summary>
        public int Start { get; private set; }

        /// <summary>
        /// 最大边界
        /// </summary>
        public int End { get; private set; }

        /// <summary>
        /// 边列表
        /// </summary>
        public List<RectSide> SideList { get; }


        public RectCheckContext()
        {
            SegmentTree = new SegmentTree();
            SideList = new List<RectSide>();
        }

        public static RectCheckContext Take(IntRect range)
        {
            var ctx = Take();
            ctx.SetRange(range);
            return ctx;
        }

        public override void OnCache()
        {
            Reset();
        }

        /// <summary>
        /// 重置上下文
        /// </summary>
        public void Reset()
        {
            SegmentTree.Reset();
            SideList.Clear();
        }


        /// <summary>
        /// 设置范围
        /// 该方法会重设线段树，只建议在初始化的时候使用
        /// </summary>
        /// <param name="range"></param>
        public void SetRange(IntRect range)
        {
            Range = range;
            SegmentTree.Build(range.X, range.X + range.Width);
        }

        /// <summary>
        /// 添加矩形边
        /// </summary>
        /// <param name="side"></param>
        public void AddSide(RectSide side)
        {
            SideList.Add(side);
            SideList.Sort(RectSide.Comparer);
            CalcBorder();
        }

        /// <summary>
        /// 添加矩形边
        /// </summary>
        public void AddSide(IList<RectSide> sideList)
        {
            SideList.AddRange(sideList);
            SideList.Sort(RectSide.Comparer);
            CalcBorder();
        }

        /// <summary>
        /// 通过边的位置移除边
        /// </summary>
        /// <param name="pos"></param>
        public void RemoveSideByPos(int pos)
        {
            for (int i = 0; i < SideList.Count; ++i)
            {
                if (SideList[i].Pos == pos)
                {
                    SideList.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 计算边界
        /// </summary>
        private void CalcBorder()
        {
            for (int i = 0; i < SideList.Count; ++i)
            {
                var side = SideList[i];
                if (i == 0)
                {
                    Start = side.Start;
                    End = side.End;
                    continue;
                }

                if (side.Start < Start)
                    Start = side.Start;
                if (side.End > End)
                    End = side.End;
            }
        }
    }
}
