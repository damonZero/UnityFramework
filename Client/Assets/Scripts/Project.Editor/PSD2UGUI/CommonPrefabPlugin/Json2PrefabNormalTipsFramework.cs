using System.Collections.Generic;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    public class Json2PrefabNormalTipsFramework : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnNormalTipsFramework"
        };

        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 LuaForm + UIPositionAdapterClick 组件做位置适配, 本工程剥离
        }
    }
}
