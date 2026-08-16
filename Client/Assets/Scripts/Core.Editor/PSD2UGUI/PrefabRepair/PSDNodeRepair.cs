//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description Prefab修复节点类
//*****************************************************************************
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD节点修复类
    /// </summary>
    public class PsdNodeRepair : PsdPrefabBase, IPsdPrefabRepair
    {
        PsdNodeType IPsdPrefabRepair.NodeType => PsdNodeType.CommonNode;

        bool IPsdPrefabRepair.IsSameNode(PsdNodeInfo psdNode, RectTransform currentUINode, bool preciseMatching)
        {
            return psdNode.nodeName == currentUINode.name;
        }

        Transform IPsdPrefabRepair.PrefabNodeRepair(PsdNodeInfo nodeInfo, RectTransform repairNode, Transform root, int hierarchy, out bool isContinue)
        {
            isContinue = true;

            //锚点自适应修复
            if (hierarchy == Psd2UguiRule.ALIGN_HIERARCHY_INDEX)
                AnchorAutoFit(repairNode);

            return repairNode;
        }
    }

}
