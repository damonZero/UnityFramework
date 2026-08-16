using System.Collections.Generic;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// 空节点组件——解析 __N 后缀设置 bgState 状态机状态
    /// </summary>
    public class Json2PrefabEmptyNode : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnEmptyNode"
        };

        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置 bgState 状态, 已剥离
        }
    }
}
