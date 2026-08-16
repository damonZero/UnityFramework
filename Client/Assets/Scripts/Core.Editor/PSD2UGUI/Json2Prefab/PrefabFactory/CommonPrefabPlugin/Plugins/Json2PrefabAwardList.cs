using System.Collections.Generic;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class Json2PrefabAwardList : Json2PrefabCommonPrefabPluginBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "AwardListNode", "AwardListNode_80", "AwardListNode_70", "AwardListNode_50", "AwardListNode_40"
        };

        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;
        public bool IsInterruption => true;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var children = node.ChildrenNodes;
            if (children is not { Count: > 0 })
                return;

            var prefab = instance.transform.Find("_srAward/Viewport/_goAwardItem");
            var parent = instance.transform.Find("_srAward/Viewport/_trAwardContent");
            foreach (var child in children)
            {
                // 生成新的子节点
                var obj = GameObject.Instantiate(prefab.gameObject, parent);
                obj.SetActive(true);
                obj.name = prefab.name;
            }
        }
    }
}
