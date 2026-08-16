using System.Collections.Generic;
using TMPro;
using Package.PSD2UGUI;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// 道具节点，定制化解析
    /// </summary>
    public class Json2PrefabItem
    {
        public abstract class Json2PrefabItemBase : Json2PrefabCommonPrefabPluginBase
        {
            public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;

            /// <summary>
            /// 文本类型
            /// </summary>
            public enum TextKind
            {
                Count = 0,
                Cost = 1
            }

            private static readonly (float min, float max, float maxSize, float minSize)[] countRanges =
            {
                (0.8f, 1.0f, 35f, 28f),
                (0.7f, 0.8f, 40f, 35f),
                (0.6f, 0.7f, 44f, 40f),
                (0.5f, 0.6f, 48f, 44f),
            };

            private static readonly (float min, float max, float maxSize, float minSize)[] costRanges =
            {
                (0.8f, 1.0f, 35f, 28f),
                (0.7f, 0.8f, 40f, 35f),
                (0.6f, 0.7f, 44f, 40f),
                (0.5f, 0.6f, 48f, 44f),
            };

            /// <summary>
            /// 设置道具字体大小，根据缩放比例区间动态调整
            /// </summary>
            /// <param name="text">文本组件</param>
            /// <param name="scale">缩放比例（@Sxx，如 @S70 = 0.7）</param>
            public static void SetTextFont(TextMeshProUGUI text, float scale, TextKind kind = TextKind.Count)
            {
                if (text == null) return;

                text.fontSize = GetFontSizeByScale(scale, kind);
            }

            /// <summary>
            /// 根据缩放比例计算对应字号，scale 越小字号越大以保证可读性
            /// </summary>
            private static float GetFontSizeByScale(float scale, TextKind kind = TextKind.Count)
            {
                // (minScale, maxScale, maxFontSize, minFontSize)
                var ranges = kind == TextKind.Count ? countRanges : costRanges;
                foreach (var (min, max, maxSize, minSize) in ranges)
                {
                    if (scale >= min && scale <= max)
                        return Mathf.Lerp(maxSize, minSize, Mathf.InverseLerp(min, max, scale));
                }

                // 超出全部区间时返回边界字号
                return scale > 1.0f ? 28f : 48f;
            }
        }

        //递归查找指定名称的子节点
        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindDeepChild(parent.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        public class CmnItemWithNameNode : Json2PrefabButtonBase
        {
            public override IEnumerable<string> Names => new[]
            {
                "CmnItemWithNameNode"
            };

            public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
            {
                // 原 P33 依赖 UIState 组件设置状态, 已剥离
                var numText = FindDeepChild(instance.transform, "通用_数量文本").GetComponent<TextMeshProUGUI>();
                var scale = node.scale;
                Json2PrefabItemBase.SetTextFont(numText, scale);
            }
        }

        public class CmnItemNode : Json2PrefabButtonBase
        {
            public override IEnumerable<string> Names => new[]
            {
                "CmnItemNode"
            };

            public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
            {
                var numText = FindDeepChild(instance.transform, "通用_数量文本").GetComponent<TextMeshProUGUI>();
                var scale = node.scale;
                Json2PrefabItemBase.SetTextFont(numText, scale);
            }
        }

        public class CmnCostItemNode : Json2PrefabButtonBase
        {
            public override IEnumerable<string> Names => new[]
            {
                "CmnCostItemNode"
            };

            public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
            {
                var numText = FindDeepChild(instance.transform, "通用_数量文本").GetComponent<TextMeshProUGUI>();
                var scale = node.scale;
                Json2PrefabItemBase.SetTextFont(numText, scale);
            }
        }
    }
}
