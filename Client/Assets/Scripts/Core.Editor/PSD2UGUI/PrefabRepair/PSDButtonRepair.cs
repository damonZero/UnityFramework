//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description Prefab修复Button类
//*****************************************************************************
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// Prefab修复Button类
    /// </summary>
    public class PsdButtonRepair : PsdPrefabBase, IPsdPrefabRepair
    {
        PsdNodeType IPsdPrefabRepair.NodeType => PsdNodeType.ButtonNode;

        bool IPsdPrefabRepair.IsSameNode(PsdNodeInfo psdNode, RectTransform currentUINode,bool preciseMatching)
        {
            return psdNode.nodeName == currentUINode.name;
        }

        Transform IPsdPrefabRepair.PrefabNodeRepair(PsdNodeInfo nodeInfo, RectTransform repairNode, Transform root, int hierarchy, out bool isContinue)
        {
            isContinue = true;

            //按钮点击范围修复
            ButtonMinSizeLimit(repairNode);

            return repairNode;
        }
    }

}
