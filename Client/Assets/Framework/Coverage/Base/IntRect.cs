//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 整数矩形
//**************************************************************************************


using UnityEngine;

namespace Framework.Coverage
{
    /// <summary>
    /// 整型 rect  原点为左下角
    /// </summary>
    public struct IntRect
    {
        /// <summary>
        /// x坐标
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// y坐标
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public int Height { get; set; }

        public static IntRect Zero => new IntRect();


        public IntRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public IntRect(float x, float y, float width, float height)
        {
            X = Mathf.RoundToInt(x);
            Y = Mathf.RoundToInt(y);
            Width = Mathf.RoundToInt(width);
            Height = Mathf.RoundToInt(height);
        }

        public static bool operator ==(IntRect a, IntRect b)
        {
            return a.X == b.X &&
                   a.Y == b.Y &&
                   a.Width == b.Width &&
                   a.Height == b.Height;
        }

        public static bool operator !=(IntRect a, IntRect b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"IntRect:{{x={X}, y={Y}, width={Width}, height:{Height}}}";
        }

        /// <summary>
        /// 获取该对象的简单字符串
        /// </summary>
        /// <returns></returns>
        public string ToSimpleString()
        {
            return $"{{{X},{Y},{Width},{Height}}}";
        }
    }
}
