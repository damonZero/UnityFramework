using System.Collections.Generic;
using Package.PSD2UGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Package.PSD2UGUI.Plugins
{
    public abstract class Json2PrefabNodeBase : Json2PrefabCommonPrefabPluginBase
    {
        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
        public bool IsInterruption => true;
    }

    public class Json2PrefabNumSelectNode : Json2PrefabNodeBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            ""
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            //不需要做处理，固定宽高
        }
    }

}