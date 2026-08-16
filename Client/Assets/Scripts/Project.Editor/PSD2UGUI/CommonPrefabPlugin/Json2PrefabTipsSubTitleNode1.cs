using System.Collections.Generic;
using TMPro;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    public class Json2PrefabTipsSubTitleNode1 : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnTipsSubTitleNode1"
        };
        
        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var instTrans = instance.transform;
            var t2d = instTrans.GetChild(0).GetComponent<TextMeshProUGUI>();
            var nd = node.ChildrenNodes[1];
            if (nd is PsdNodeText textNode)
            {
                t2d.SetText(textNode.content);
            }
        }
    }
}