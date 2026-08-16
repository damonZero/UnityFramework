//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description Prefab修复Image类
//*****************************************************************************

using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// Prefab修复Image类
    /// </summary>
    public class PsdImageRepair : PsdPrefabBase, IPsdPrefabRepair
    {
        PsdNodeType IPsdPrefabRepair.NodeType => PsdNodeType.Image;

        bool IPsdPrefabRepair.IsSameNode(PsdNodeInfo psdNode, RectTransform currentUINode, bool preciseMatching)
        {
            Image findImage = currentUINode.GetComponent<Image>();
            if (!findImage || !findImage.sprite) return false;
            bool sameImage = psdNode.nodeName == findImage.sprite.name;
            bool precise = psdNode.nodeName == currentUINode.name;
            return sameImage && (!preciseMatching || precise);
        }

        Transform IPsdPrefabRepair.PrefabNodeRepair(PsdNodeInfo nodeInfo, RectTransform repairNode, Transform root,
            int hierarchy, out bool isContinue)
        {
            isContinue = false;

            //获取图片
            Image repairImage = repairNode.GetComponent<Image>();
            if (repairImage == null)
                return null;

            //设置图片属性
            repairImage.sprite = nodeInfo.nodeImage.sprite;
            return repairNode;
        }
    }
}