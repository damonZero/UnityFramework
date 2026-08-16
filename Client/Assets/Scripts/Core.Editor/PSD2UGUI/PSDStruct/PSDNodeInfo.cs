//*****************************************************************************
//Created By Liangc on 2019/6/3
//PSD文件信息类
//@Description 包含了PSD文件的layer中能解析到的所有有用信息
//*****************************************************************************

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD节点信息
    /// </summary>
    public class PsdNodeInfo
    {
        /// <summary>
        /// 节点类型
        /// </summary>
        public PsdNodeType nodeType;

        /// <summary>
        /// 父节点
        /// </summary>
        public readonly PsdNodeInfo parentNode;

        /// <summary>
        /// 子节点
        /// </summary>
        public List<PsdNodeInfo> childNodes;

        /// <summary>
        /// PSD文件图层
        /// </summary>
        public PhotoshopFile.Layer layer;

        /// <summary>
        /// 节点名
        /// </summary>
        public string nodeName;

        /// <summary>
        /// 节点Rect
        /// </summary>
        public Rect nodeRect;

        /// <summary>
        /// 节点图片
        /// </summary>
        public PsdImage nodeImage;

        /// <summary>
        /// 节点文字
        /// </summary>
        public PsdText nodeText;

        public PsdNodeInfo(PsdNodeType nodeType, string name, PsdNodeInfo parentNode = null, Rect rect = default,
            PsdImage image = null, PsdText text = null, PhotoshopFile.Layer layer = null)
        {
            this.nodeType = nodeType;
            this.nodeName = name;
            this.parentNode = parentNode;
            this.nodeRect = rect;
            this.nodeImage = image;
            this.nodeText = text;
            this.layer = layer;
        }

        /// <summary>
        /// 是否有子节点
        /// </summary>
        /// <returns></returns>
        public bool HasChildNode => childNodes != null && childNodes.Count != 0;

        /// <summary>
        /// 添加子节点到集合头部
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void AddChildNodeFirst(PsdNodeInfo nodeInfo)
        {
            if (childNodes == null)
                childNodes = new List<PsdNodeInfo>();

            childNodes.Insert(0, nodeInfo);
        }

        /// <summary>
        /// 添加子节点到集合末尾
        /// </summary>
        /// <param name="nodeInfo"></param>
        public void AddChildNodeLast(PsdNodeInfo nodeInfo)
        {
            if (childNodes == null)
                childNodes = new List<PsdNodeInfo>();

            childNodes.Add(nodeInfo);
        }

        /// <summary>
        /// 节点前序遍历
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="handleNode"></param>
        /// <param name="continueJudge"></param>
        /// <param name="currentLayer"></param>
        public static void SpreorderTraverseNode(PsdNodeInfo nodeInfo, Action<PsdNodeInfo, int> handleNode,
            Func<PsdNodeInfo, int, bool> continueJudge, int currentLayer = 1)
        {
            if (nodeInfo == null || handleNode == null || continueJudge == null)
                return;

            handleNode(nodeInfo, currentLayer);

            if (nodeInfo.childNodes == null)
                return;

            int childCount = nodeInfo.childNodes.Count;
            if (continueJudge(nodeInfo, currentLayer) && childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    SpreorderTraverseNode(nodeInfo.childNodes[i], handleNode, continueJudge, currentLayer + 1);
                }
            }
        }

        /// <summary>
        /// 节点后续遍历
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="handleNode"></param>
        /// <param name="currentLayer"></param>
        public static void SubsequentTraversalNode(PsdNodeInfo nodeInfo,
            Func<PsdNodeInfo, int, bool> handleNode, int currentLayer = 1)
        {
            if (nodeInfo == null || handleNode == null)
                return;

            bool isRecursion = false;
            try
            {
                isRecursion = handleNode(nodeInfo, currentLayer);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (nodeInfo.childNodes == null)
                return;

            int childCount = nodeInfo.childNodes.Count;
            if (isRecursion && childCount > 0)
            {
                for (int i = childCount - 1; i >= 0; --i)
                {
                    SubsequentTraversalNode(nodeInfo.childNodes[i], handleNode, currentLayer + 1);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            SpreorderTraverseNode(this, (nodeInfo, currentLayer) =>
                {
                    for (int i = 0; i < currentLayer; i++)
                    {
                        stringBuilder.Append("    ");
                    }

                    stringBuilder.Append(nodeInfo.nodeName + "(" + nodeInfo.nodeType.ToString() + ")");
                    stringBuilder.Append("\n");
                },
                (nodeInfo, currentLayer) => true);

            return stringBuilder.ToString();
        }
    }
}