using System.Collections.Generic;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class Json2PrefabListView : Json2PrefabCommonPrefabPluginBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "lvVertical", "lvHorizontal", "lvGrid"
        };

        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;
        public bool IsInterruption => true;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIListView 组件, 本工程剥离; 列表定制化解析留待后续补充
        }
    }
}
