using System.Collections.Generic;
using TMPro;
using Package.PSD2UGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Editor
{
    public abstract class Json2PrefabButtonBase : Json2PrefabCommonPrefabPluginBase
    {
        public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
    }

    /// <summary>
    /// 关闭按钮——固定位置 + 解析__N后缀设置UIState状态
    /// </summary>
    public class CmnBtnClose : Json2PrefabButtonBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnBtnClose"
        };

        public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置状态, 已剥离
        }
    }

    /// <summary>
    /// 基础按钮——固定位置 + 解析__N后缀设置UIState状态
    /// </summary>
    public class CmnBtn : Json2PrefabButtonBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnBtnBig",
            "CmnBtnMid",
            "CmnBtnSmall",
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置状态, 已剥离

            // 解析文本
            var t2d = instance.transform.Find("文本")?.GetComponent<TextMeshProUGUI>();
            var t2dNode = node.ChildrenNodes[1];
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
        }
    }

    /// <summary>
    /// 双按钮布局——固定位置 + 解析__N后缀设置UIState状态
    /// </summary>
    public class CmnDoubleBtn : Json2PrefabButtonBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnDoubleBtnMid",
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
        }
    }

    /// <summary>
    /// 底部按钮布局——固定位置 + 解析__N后缀设置UIState状态
    /// </summary>
    public class CmnBottomButtonLayout : Json2PrefabButtonBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnBottomButtonLayout",
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置状态, 已剥离
        }
    }

    /// <summary>
    /// 功能按钮——固定位置 + 解析__N后缀设置UIState状态
    /// </summary>
    public class CmnFuncBtnVerticalLayout : Json2PrefabButtonBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnFuncBtnVerticalLayout"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 原 P33 依赖 UIState 组件设置状态, 已剥离
        }
    }

    /// <summary>
    /// 功能按钮布局——固定位置 + 解析__N后缀设置UIState状态
    /// </summary>
    public class CmnFuncBtn : Json2PrefabButtonBase
    {
        public override IEnumerable<string> Names => new[]
        {
            "CmnFuncBtnStyle1",
            "CmnFuncBtnStyle2",
            "CmnFuncBtnStyle3"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            // 解析图标
            var icon = instance.transform.Find("图标")?.GetComponent<Image>();
            var iconNode = node.ChildrenNodes[1] as PsdNodeImage;
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconNode.assetPath);
            // 解析文本
            var t2d = instance.transform.Find("文本/按钮文本")?.GetComponent<TextMeshProUGUI>();
            var t2dNode = node.ChildrenNodes[3];
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
        }
    }
}
