using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Package.PSD2UGUI.Plugins
{
    //通用背景, 可替换图片
    public class Json2PrefabBg : Json2PrefabCommonPrefabPluginBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "ndFullBg"
        };

        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;
        public bool IsInterruption => true;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            if (node.name.Equals("ndClickClose"))
                return;

            var imgNode = node.ChildrenNodes[0] as PsdNodeImage;
            instance.GetComponentInChildren<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imgNode.assetPath);
            instance.GetComponentInChildren<Image>().SetNativeSize();

        }
    }
}