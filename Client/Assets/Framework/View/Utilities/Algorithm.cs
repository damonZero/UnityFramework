// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2018/06/04
// ********************************************************************

using System;
using System.Collections.Generic;
namespace Framework.View
{
    /// <summary>
    /// 算法集合
    /// </summary>
    internal static class Algorithm
    {
        /// <summary>
        /// 稳定排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="comparison"></param>
        public static void StableSort<T>(List<T> list, Comparison<T> comparison)
        {
            BottomUpMergeSort(list, comparison);
        }

        /// <summary>
        /// 归并排序（自底向上）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="comparison"></param>
        public static void BottomUpMergeSort<T>(List<T> target, Comparison<T> comparison)
        {
            var n = target.Count;
            var assist = new T[n];

            IList<T> src = target;
            IList<T> dst = assist;
            for (var width = 1; width < n; width *= 2)
            {
                var step = width * 2;
                for (var i = 0; i < n; i += step)
                {
                    BottomUpMerge(src, i, Math.Min(i + width, n), Math.Min(i + width + width, n), dst, comparison);
                }

                // swap src & dst
                (src, dst) = (dst, src);
            }

            dst = src; // swap back

            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (dst != target)
            {
                // copy back
                for (var i = 0; i < n; ++i)
                {
                    target[i] = dst[i];
                }
            }
        }

        private static void BottomUpMerge<T>(IList<T> src,
            int left,
            int right,
            int end,
            IList<T> dst,
            Comparison<T> comparison)
        {
            var i = left;
            var j = right;
            var k = left;

            if (i < right && j < end)
            {
                while (true)
                {
                    if (comparison(src[i], src[j]) <= 0)
                    {
                        dst[k++] = src[i++];

                        if (i == right) break;
                    }
                    else
                    {
                        dst[k++] = src[j++];

                        if (j == end) break;
                    }
                }
            }

            while (i < right)
            {
                dst[k++] = src[i++];
            }

            while (j < end)
            {
                dst[k++] = src[j++];
            }
        }
    }
}
