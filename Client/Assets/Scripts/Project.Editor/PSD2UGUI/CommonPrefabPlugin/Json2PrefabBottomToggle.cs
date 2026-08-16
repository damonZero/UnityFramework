using System.Collections.Generic;
using System.Linq;
using TMPro;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    public class Json2PrefabBottomToggle : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnBottomToggle"
        };

        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var instTrans = instance.transform;
            var groupNdList = node.ChildrenNodes.Where(n => n.NodeType == PsdNodeEnum.Group).ToList();
            // 原 P33 依赖 UIState 组件设置组数量状态, 已剥离
            for (var i = 0; i < groupNdList.Count; i++)
            {
                var groupNd = groupNdList[i];
                foreach (var groupChildNode in groupNd.ChildrenNodes)
                {
                    if (groupChildNode is not PsdNodeText textNode) continue;
                    var childTrans = instTrans.GetChild(i);
                    var t2d = childTrans.Find("On/t2d@fswb").GetComponent<TextMeshProUGUI>();
                    if (t2d != null)
                    {
                        t2d.SetText(textNode.content);
                    }
                }
            }
        }
    }
}
