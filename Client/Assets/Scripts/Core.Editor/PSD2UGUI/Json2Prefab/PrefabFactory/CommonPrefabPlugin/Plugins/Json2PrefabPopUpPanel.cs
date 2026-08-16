
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Package.PSD2UGUI.Plugins
{
    
    public class Json2PrefabPopUpPanel : Json2PrefabCommonPrefabPluginBase,IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "LargePopupPanel", "MiddlePopupPanel","SmallPopupPanel","InterruptPopupPanel"
        };
        
        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
        public bool IsInterruption => true;
        
        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            PsdNodeBase firstNode = node.ChildrenNodes[0];
            var t2dNode = FindTextChildNode(firstNode);
            if (t2dNode != null)
            {
                Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText); 
            }
            
            Transform otherNodeParent = instance.transform.Find("main/panel/content");
            if (!otherNodeParent)
            {
                return;
            }
            
            // 解析除了第一个节点的其他节点内容
            for (int i = 1; i < node.ChildrenNodes.Count; i++)
            {
                PsdNodeBase childNode = node.ChildrenNodes[i];
                Json2PrefabAssemble.AssembleRecursion(childNode, otherNodeParent);
            }
        }

        // 递归遍历所有类型为PsdNodeGroup的子节点，找到类型为PsdNodeText的节点
        private PsdNodeBase FindTextChildNode(PsdNodeBase node)
        {
            if (node.NodeType == PsdNodeEnum.Text)
            {
                return node;
            }
            else if (node.NodeType == PsdNodeEnum.Group)
            {
                foreach (var childNode in node.ChildrenNodes)
                {
                    PsdNodeBase findNode = FindTextChildNode(childNode);
                    if (findNode != null)
                    {
                        return findNode;
                    }
                }
            }
            return null;
        }

    }
}