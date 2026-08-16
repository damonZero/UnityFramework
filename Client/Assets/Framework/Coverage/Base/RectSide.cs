//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 矩形边定义
//**************************************************************************************

using System.Collections.Generic;
using Framework.Coverage;

namespace Framework.Coverage
{

    /// <summary>
    /// 矩形的边
    /// </summary>
    public class RectSide:Pool<RectSide>
    {
        public int Start { get; set; }

        public int End { get; set; }

        public int Pos { get; set; }

        /// <summary>
        /// 边的标记位
        /// 0：底/左边
        /// 1：顶/右边
        /// </summary>
        public byte Flag { get; set; }


        public static int Comparer(RectSide side1, RectSide side2)
        {
            return side1.Pos - side2.Pos;
        }


        private static readonly List<RectSide> _pool = new List<RectSide>();

        public override void OnCache()
        {
            Start = 0;
            End = 0;
            Pos = 0;
            Flag = 0;
        }



    }
}
