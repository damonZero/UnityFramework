using System.Collections.Generic;
using TMPro;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    public class Json2PrefabTipsContentItem : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnTipsContentItem"
        };
        
        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var instTrans = instance.transform;
            var t2d = instTrans.Find("内容").GetComponent<TextMeshProUGUI>();
            var nd = node.ChildrenNodes[0];
            if (nd is PsdNodeText textNode)
            {
                t2d.SetText(textNode.content);
            }
        }
    }
}