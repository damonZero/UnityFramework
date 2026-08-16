//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 线段树
//**************************************************************************************

using Framework.Coverage;
using UnityEngine;


/*
 * 线段树用于对集合的查询操作
 *
 * SetSegEnable(100,200,true)   //添加线段
 *
 * CheckSegIsEnable(110,190)    //返回true
 * CheckSegIsEnable(110,201)    //返回false
 *
 * SetSegEnable(150,300,true)   //继续添加线段
 *
 * CheckSegIsEnable(110,201)    //返回true
 *
 * 删除线段现在有局限性，只能删除之前添加过的线段，不能随意删除某个区间，多次添加的线段区间可被多次删除
 * SetSegEnable(100,200,false)  //删除线段 正确操作
 * SetSegEnable(150,300,false)  //删除线段 正确操作
 * SetSegEnable(100,150,false)  //删除线段 错误操作 (未添加过100~150区间的线段)
 *
 */

namespace Framework.Coverage
{
    /// <summary>
    /// 线段树
    /// </summary>
    public class SegmentTree
    {
        /// <summary>
        /// 线段树节点
        /// </summary>
        private class SegTreeNode : Pool<SegTreeNode>
        {
            //线段范围: [start,end)   左闭右开

            public int Start { get; set; }
            public int End { get; set; }
            public int Count { get; set; }

            //这里线段树由于是动态生成，不采用数组存储，采用左右节点引用的形式
            public SegTreeNode Left { get; set; }
            public SegTreeNode Right { get; set; }

            public override void OnCache()
            {
                Start = 0;
                End = 0;
                Count = 0;
            }
        }

        private SegTreeNode _root; //线段树根节点  这里没有事先构建所有树节点，不是一颗完全二叉树，用节点的形式存储而不用数组

        /// <summary>
        /// 构造线段树 [start,end] 闭区间
        /// </summary>
        public void Build(int start, int end)
        {
            _root = SegTreeNode.Take();
            _root.Start = start;
            _root.End = end + 1;
        }

        /// <summary>
        /// 重置线段树，释放整棵树的节点，只保留根节点
        /// </summary>
        public void Reset()
        {
            Release(_root);
            _root = null;
        }

        /// <summary>
        /// 设置线段是否激活
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="enable"></param>
        public void SetSegEnable(int start, int end, bool enable)
        {
            if (start > end)
                return;
            SetSegEnable(start, end, enable, _root);
        }


        /// <summary>
        /// 检测线段是否激活
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public bool CheckSegIsEnable(int start, int end)
        {
            if (start > end)
                return false;
//            Profiler.BeginSample("SegmentTreeCheckEnable");
            var isEnable = CheckSegIsEnable(start, end, _root);
//            Profiler.EndSample();
            return isEnable;
        }

        /// <summary>
        /// 计算树节点的个数(用于调试)
        /// </summary>
        /// <returns></returns>
        public int CalcNodeCount()
        {
            return CalcNodeCount(_root);
        }

        private int CalcNodeCount(SegTreeNode node)
        {
            if (node == null)
                return 0;
            return CalcNodeCount(node.Left) + CalcNodeCount(node.Right) + 1;
        }


        /// <summary>
        /// 计算树高度用于调试
        /// </summary>
        /// <returns></returns>
        public int CalcTreeHeight()
        {
            return CalcTreeHeight(_root);
        }

        /// <summary>
        /// 计算线段树所占字节数
        /// </summary>
        /// <returns></returns>
        public int CalcMem()
        {
            var cnt = CalcNodeCount();
            return cnt * 40;
        }

        /// <summary>
        /// 设置线段是否激活
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="enable"></param>
        /// <param name="node"></param>
        private void SetSegEnable(int start, int end, bool enable, SegTreeNode node)
        {
            if (node == null)
                return;

            if (start > end)
            {
                start = start ^ end;
                end = start ^ end;
                start = start ^ end;
            }

            if (start < _root.Start)
                start = _root.Start;
            if (end > _root.End)
                end = _root.End;
            var mid = (int) Mathf.Ceil((node.Start + node.End) * 0.5f);
            if (start <= node.Start && end >= node.End - 1)
            {
                //如果节点的范围完全在设置范围内，则设置整个节点的计数
                if (!enable && node.Count == 0)
                    return;
                node.Count += enable ? 1 : -1;
            }
            else
            {
                if (start < mid && end > mid)
                {
                    SetSegEnable(start, mid, enable, GetOrCreateLeft(node));
                    SetSegEnable(mid, end, enable, GetOrCreateRight(node));
                }
                else
                {
                    // start>=mid || end<=mid
                    SetSegEnable(start, end, enable, start < mid ? GetOrCreateLeft(node) : GetOrCreateRight(node));
                }
            }
        }


        /// <summary>
        /// 获取左孩子节点，如果没有，则创建
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private SegTreeNode GetOrCreateLeft(SegTreeNode node)
        {
            if (node.Left == null)
            {
                var mid = (int) Mathf.Ceil((node.Start + node.End) * 0.5f);
                node.Left = SegTreeNode.Take();
                node.Left.Start = node.Start;
                node.Left.End = mid;
            }

            return node.Left;
        }

        /// <summary>
        /// 获取右孩子节点，如果没有，则创建
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private SegTreeNode GetOrCreateRight(SegTreeNode node)
        {
            if (node.Right == null)
            {
                var mid = (int) Mathf.Ceil((node.Start + node.End) * 0.5f);
                node.Right = SegTreeNode.Take();
                node.Right.Start = mid;
                node.Right.End = node.End;
            }

            return node.Right;
        }


        /// <summary>
        /// 检查线段是否激活
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool CheckSegIsEnable(int start, int end, SegTreeNode node)
        {
            if (node == null)
                return false;

            if (node.Count > 0)
                return true;

            if (start > end)
            {
                start = start ^ end;
                end = start ^ end;
                start = start ^ end;
            }

            var mid = (int) Mathf.Ceil((node.Start + node.End) * 0.5f);
            if (start < mid && end > mid)
                return CheckSegIsEnable(start, mid, node.Left) && CheckSegIsEnable(mid, end, node.Right);
            return CheckSegIsEnable(start, end, start < mid ? node.Left : node.Right);
        }


        /// <summary>
        /// 释放节点，回收到缓存池
        /// </summary>
        /// <param name="node"></param>
        private void Release(SegTreeNode node)
        {
            if (node == null)
                return;
            Release(node.Left);
            Release(node.Right);
            node.Left = null;
            node.Right = null;
            SegTreeNode.Cache(node);
        }

        /// <summary>
        /// 计算高度
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private int CalcTreeHeight(SegTreeNode node)
        {
            if (node == null)
                return 0;
            var height1 = CalcTreeHeight(node.Left) + 1;
            var height2 = CalcTreeHeight(node.Right) + 1;
            return height1 > height2 ? height1 : height2;
        }
    }
}
