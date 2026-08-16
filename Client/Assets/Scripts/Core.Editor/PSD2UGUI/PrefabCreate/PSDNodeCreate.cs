//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description PSD创建Prefab的节点类
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD创建Prefab的节点类
    /// </summary>
    public class PsdNodeCreate : PsdPrefabBase, IPsdPrefabCreate
    {
        PsdNodeType IPsdPrefabCreate.NodeType => PsdNodeType.CommonNode;

        public bool IsContinue(PsdNodeInfo nodeInfo, int hierarchy)
        {
            return !FindPrefab(nodeInfo.nodeName);
        }

        Transform IPsdPrefabCreate.PrefabNodeCreate(PsdNodeInfo nodeInfo, Transform parent, Transform root,
            int hierarchy, out bool isRecursion, StringBuilder warnInfo)
        {
            // Debug.Log("create node:" + nodeInfo.nodeName);
            isRecursion = true;
            Transform retRect = default;
            GameObject prefabInstance = default;

            //查找项目中是否有预制体节点
            GameObject prefab = FindPrefab(nodeInfo.nodeName);

            //匹配已存在预制体节点
            if (prefab)
            {
                prefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
                Psd2UguiStatistics.UseCommonPrefab(nodeInfo.nodeName);
            }


            //有预制体节点,直接处理实例化对象
            RectTransform prefabTrans = null;
            if (prefabInstance)
            {
                isRecursion = false;
                GetMaxRectByChildren(nodeInfo);
                prefabTrans = prefabInstance.GetComponent<RectTransform>();

                //处理公共预制本身已经做了适配的就不需要再设置了
                if(prefabTrans.anchorMin == new Vector2(0.5f, 0.5f) && prefabTrans.anchorMax == new Vector2(0.5f, 0.5f))
                {
                    SetRect(prefabTrans, nodeInfo, parent, root);
                }
                else
                {
                    //锚点居中的只需要做一次基础设置即可
                    prefabTrans.SetParent(root, false);
                    prefabTrans.localScale = Vector3.one;
                }

                retRect = parent;
                // 原 P33 依赖 UIListView 组件处理列表公共组件, 本工程剥离
            }

            //根据选择决定是否创建新节点
            if (!prefabInstance || !Psd2UguiEditor._instance.replaceDel)
            {
                isRecursion = true;
                //直接将画布作为首层节点,画布不需要锚点自适应,其他层级正常创建
                if (IsRootNode(hierarchy))
                {
                    retRect = root;
                    HandleRootNode(nodeInfo, retRect);
                }
                else
                {
                    retRect = CreateEmptyNode(nodeInfo, parent, root);

                    //暂只自适应2级节点,深层次节点若找到更合适的规则也可自适应
                    if (hierarchy == Psd2UguiRule.ALIGN_HIERARCHY_INDEX)
                        AnchorAutoFit((RectTransform) retRect);

                    //警告处理
                    TransformWarn(warnInfo, prefabTrans, retRect as RectTransform);
                }
            }

            return retRect;
        }

        //根节点处理
        private void HandleRootNode(PsdNodeInfo node, Transform rect)
        {
            Vector2 standardSize = Psd2UguiRule.GetStandardResolution(
                Psd2UguiEditor._instance._resolutionIndex);
            node.nodeRect.size = standardSize;
            rect.name = node.nodeName;
            ((RectTransform) rect).anchoredPosition = Vector2.zero;
            ((RectTransform) rect).sizeDelta = standardSize;
        }

        //是否是根节点
        private bool IsRootNode(int hierarchy)
        {
            return hierarchy == 1;
        }
    }
}