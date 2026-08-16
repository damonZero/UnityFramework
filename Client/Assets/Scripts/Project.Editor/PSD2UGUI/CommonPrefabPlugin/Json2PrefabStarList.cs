using System.Collections.Generic;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// 星级列表节点——解析后缀设置"布局状态"状态机（居左 Left / 居中 Center / 自定义 Custom）
    /// </summary>
    public class Json2PrefabStarList : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnBigStarListNode",
            "CmnSmallStarListNode"
        };

        // 列表宽高由星级数量动态驱动，保留预制体自身尺寸，不被 PSD 像素覆盖
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;

        // 摆放位置由 PSD 决定
        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置布局状态, 已剥离
        }
    }
}
