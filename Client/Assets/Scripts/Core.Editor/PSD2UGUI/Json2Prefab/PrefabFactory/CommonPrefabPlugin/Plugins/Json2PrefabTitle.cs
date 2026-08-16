using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Package.PSD2UGUI.Plugins
{
    public abstract class Json2PrefabTitleBase : Json2PrefabCommonPrefabPluginBase
    {

        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
        public bool IsInterruption => true;
    }
    
    public class Json2PrefabTitle : Json2PrefabTitleBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "TopTitle", "TopTitleBlack"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            //标题
            var t2dTitle = instance.GetComponentInChildren<TextMeshProUGUI>();
            var t2dNode = node.ChildrenNodes[1];
            Json2PrefabFactory.SetTMPContent(t2dTitle, t2dNode as PsdNodeText);
        }
    }
    
    // 左上角标题 第一个字尺寸要放大
    public class Json2PrefabTitleLeft : Json2PrefabTitleBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "LeftTopTitle"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            //标题
            var t2dTitle = instance.GetComponentInChildren<TextMeshProUGUI>();
            PsdNodeText t2dNode = node.ChildrenNodes[1] as PsdNodeText;
            PsdNodeText t2dFirstCharNode = node.ChildrenNodes[2] as PsdNodeText;
            t2dNode.content = $"<size=68>{t2dFirstCharNode.content}</size>{t2dNode.content}";
            Json2PrefabFactory.SetTMPContent(t2dTitle, t2dNode);
        }
    }
    
    public class Json2PrefabFunctionTitle : Json2PrefabTitleBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "FunctionTilte"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            //标题
            var t2dTitle = instance.GetComponentInChildren<TextMeshProUGUI>();
            var t2dNode = node.ChildrenNodes[2];
            Json2PrefabFactory.SetTMPContent(t2dTitle, t2dNode as PsdNodeText);
        }
    }
}