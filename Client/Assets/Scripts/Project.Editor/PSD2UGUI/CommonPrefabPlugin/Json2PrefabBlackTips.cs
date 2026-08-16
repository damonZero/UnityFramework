using System.Collections.Generic;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    public class Json2PrefabBlackTips : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnBlackTips"
        };

        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置弹窗尺寸状态, 已剥离
        }
    }
}
