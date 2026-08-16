using System.Collections.Generic;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// 时间倒计时节点——解析 __N 后缀设置"时钟状态"状态机状态（Show 显示 / Hide 隐藏）
    /// </summary>
    public class Json2PrefabTimeDownNode : Json2PrefabCommonPrefabPluginBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnTimeDownNode"
        };

        // 根节点宽高由 ContentSizeFitter + 自定义 LayoutGroup 驱动，保留预制体自身尺寸，不被 PSD 像素覆盖
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;

        // 倒计时节点摆放位置由 PSD 决定
        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置时钟状态, 已剥离
        }
    }
}
