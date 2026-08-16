//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description Prefab修复Text类
//*****************************************************************************
using TMPro;
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// Prefab修复Text类
    /// </summary>
    public class PsdTextRepair : PsdPrefabBase, IPsdPrefabRepair
    {
        PsdNodeType IPsdPrefabRepair.NodeType => PsdNodeType.Text;

        bool IPsdPrefabRepair.IsSameNode(PsdNodeInfo psdNode, RectTransform currentUINode, bool preciseMatching)
        {
            TextMeshProUGUI findT2d = currentUINode.GetComponent<TextMeshProUGUI>();
            if (!findT2d) return false;
            bool sameText = psdNode.nodeText.text == findT2d.text;
            bool precise = psdNode.nodeName == currentUINode.name;
            return sameText && (!preciseMatching || precise);
        }

        Transform IPsdPrefabRepair.PrefabNodeRepair(PsdNodeInfo nodeInfo, RectTransform repairNode, Transform root, int hierarchy, out bool isContinue)
        {
            isContinue = false;

            //获取文本
            TextMeshProUGUI repairText = repairNode.GetComponent<TextMeshProUGUI>();
            if (repairText == null)
                return null;

            //设置属性
            repairText.SetText(nodeInfo.nodeText.text);            
            repairText.color = nodeInfo.nodeText.color;
            string[] nameParse = nodeInfo.nodeName.Split('@');
            if (nameParse.Length >= 3)
            {
                int.TryParse(nameParse[2], out var sizeTemp);
                repairText.fontSize = sizeTemp;
            }
            else
                repairText.fontSize = nodeInfo.nodeText.fontSize;
            
            repairText.alignment = TMPro.TextAlignmentOptions.Center;
            repairText.alignment = TMPro.TextAlignmentOptions.Midline;
            repairText.raycastTarget = false;

            //文本属性配置表读取
            TextPropertyRead(repairText, nodeInfo.nodeName);

            return repairNode;
        }
    }

}
