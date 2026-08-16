using System.Collections.Generic;
using UnityEngine;

namespace Package.PSD2UGUI.Plugins
{
    public class Json2PrefabCommonFormRegion : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CommonFormRegion"
        };

        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;

        //区域面板自行管理子节点生成, 避免自动复用叠加
        public override bool AutoReuseEmbedded => false;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            if (node.ChildrenNodes is not { Count: > 0 })
            {
                return;
            }

            for (int i = 0; i < node.ChildrenNodes.Count; i++)
            {
                var psdRegionNode = node.ChildrenNodes[i];
                if (psdRegionNode == null || string.IsNullOrEmpty(psdRegionNode.name))
                {
                    continue;
                }

                var regionParent = instance.transform.Find(psdRegionNode.name);
                if (!regionParent)
                {
                    Debug.LogError($"CommonFormRegion预制体下未找到同名节点: {psdRegionNode.name}");
                    continue;
                }

                var regionChildren = psdRegionNode.ChildrenNodes;
                if (regionChildren is not { Count: > 0 })
                {
                    continue;
                }

                for (int j = 0; j < regionChildren.Count; j++)
                {
                    Json2PrefabAssemble.AssembleRecursion(regionChildren[j], regionParent);
                }
            }
        }
    }
}
