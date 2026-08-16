using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI.Plugins
{
    public abstract class Json2PrefabButtonBase : Json2PrefabCommonPrefabPluginBase
    {
        public Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
        public bool IsInterruption => true;
    }

    /// <summary>
    /// 解析按钮 仅替换文本
    /// </summary>
    public class Json2PrefabButtonText : Json2PrefabButtonBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "_btnPrimaryBlack", "_btnBlackNormal", "_btnNormalA",
            "_btnNormalB", "_btnNormalC", "_btnNormalD","_btnPrimary",
            "_btnPrimarycost", "_btnSecond", "_btnThrid"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            if (node.isFixedPosition)
            {
                instance.GetComponent<RectTransform>().ResetRectTransform();
            }
            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            PsdNodeBase t2dNode = null;
            if (node.ChildrenNodes.Count > 0)
            {
                if (node.ChildrenNodes[0].ChildrenNodes.Count > 0)
                {
                    t2dNode = node.ChildrenNodes[0].ChildrenNodes[1];
                }
                else
                {
                    t2dNode = node.ChildrenNodes[1];
                }
            }
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
        }
    }

    /// <summary>
    /// 解析按钮 替换文本和图片
    /// </summary>
    public class Json2PrefabButtonIconText : Json2PrefabButtonBase, IJson2PrefabCommonPrefabPlugin
    {
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;
        public override IEnumerable<string> Names => new[]
        {
            "btnCommon_text"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var icon = instance.GetComponentInChildren<Image>(true);
            var iconNode = node.ChildrenNodes[0] as PsdNodeImage;
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconNode.assetPath);

            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            var t2dNode = node.ChildrenNodes[1];
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
            // 解析文本属性
            Json2PrefabAssembleTool.ParseTextTid(t2dNode as PsdNodeText, t2d);
        }
    }

    /// <summary>
    /// 解析按钮 替换文本和图片
    /// </summary>
    public class Json2PrefabButtonIconTextBg : Json2PrefabButtonBase, IJson2PrefabCommonPrefabPlugin
    {
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;
        public override IEnumerable<string> Names => new[]
        {
             "_btnFunction"
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var icon = instance.GetComponentInChildren<Image>(true);
            var iconNode = node.ChildrenNodes[0] as PsdNodeImage;
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconNode.assetPath);

            var t2d = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            var t2dNode = node.ChildrenNodes[2];
            Json2PrefabFactory.SetTMPContent(t2d, t2dNode as PsdNodeText);
            // 解析文本属性
            Json2PrefabAssembleTool.ParseTextTid(t2dNode as PsdNodeText, t2d);
        }
    }

    /// <summary>
    /// 解析按钮 替换文本
    /// </summary>
    public class Json2PrefabButtonIcon : Json2PrefabButtonBase, IJson2PrefabCommonPrefabPlugin
    {
        public Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;
        public override IEnumerable<string> Names => new[]
        {
            "btnCommon",
        };

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var icon = instance.GetComponentsInChildren<Image>(true)[0];
            var iconNode = node.ChildrenNodes[0] as PsdNodeImage;
            iconNode.assetPath = Json2PrefabParseTool.ParseImgPath(iconNode,node);
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconNode.assetPath);
        }
    }

    /// <summary>
    /// 固定按钮位置
    /// </summary>
    public class Json2PrefabButtonFixedPos : Json2PrefabButtonBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "_btnClose"
        };

        public new Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            //不需要解析 主要为了固定大小和位置
        }
    }

    /// <summary>
    /// 底部按钮布局
    /// </summary>
    public class Json2PrefabBottomButtonLayoutFixedPos : Json2PrefabButtonBase, IJson2PrefabCommonPrefabPlugin
    {
        public override IEnumerable<string> Names => new[]
        {
            "BottomButton"
        };

        public new Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;

        public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            //解析节点子节点数量
            var childCount = node.ChildrenNodes.Count;
            if (childCount == 0)
            {
                return;
            }

            // 将子节点拉入对应的子节点下(原 P33 依赖 UIState 组件控制状态数量, 已剥离)
            for (int i = 0; i < childCount; i++)
            {
                var parent = instance.transform.GetChild(i);
                if (!parent)
                {
                    return;
                }
                var childNode = node.ChildrenNodes[i];
                childNode.isFixedPosition = true;
                Json2PrefabAssemble.AssembleRecursion(childNode, parent);
                parent.GetComponent<RectTransform>().ResetRectTransform();
            }
            instance.GetComponent<RectTransform>().ResetRectTransform();
        }
    }
}
