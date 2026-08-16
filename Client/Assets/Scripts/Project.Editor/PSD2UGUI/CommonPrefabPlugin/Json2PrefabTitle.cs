using System.Collections.Generic;
using Package.PSD2UGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Editor
{
    public class Json2PrefabTitle
    {
        public abstract class Json2PrefabTitleBase : Json2PrefabCommonPrefabPluginBase
        {
            public override Json2PrefabEnum UseSelfSize => Json2PrefabEnum.WidthHeight;
        }

        public class CmnFunctionTitle : Json2PrefabTitleBase
        {
            public override Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.XY;
            public override IEnumerable<string> Names => new[]
            {
                "CmnFunctionTitle"
            };

            // 标题文字图在 PSD 组（CmnFunctionTitle）中的固定子节点索引（命名不稳定，按层级取）
            private const int TITLE_WORD_INDEX = 3;

            // 标题文字图在预制体中的节点全名（预制体资源不会改名，按全名匹配）
            private const string TITLE_WORD_NODE_NAME = "word_fuctiontitle";

            // 底图在 PSD 组（CmnFunctionTitle）中的固定子节点索引（与标题文字图一致，按层级取）
            private const int BOTTOM_IMG_INDEX = 0;

            // 底图在预制体中的节点全名（预制体资源不会改名，按全名匹配）
            private const string BOTTOM_IMG_NODE_NAME = "img_fuctiontitle_di";

            public override void CustomizedCheck(PsdNodeBase node, GameObject instance)
            {
                // 底图透明度：按 PSD 图层不透明度同步到预制体底图 Image 的 alpha（仅处理这一张）
                SetBottomImgAlpha(node, instance);

                // 标题文字图：PSD 侧命名不稳定按固定索引取，预制体侧按全名匹配
                SetTitleWordSprite(node, instance);
            }

            /// <summary>
            /// 底图透明度：读取 PSD 底图图层不透明度，设置预制体底图 Image 的 alpha
            /// </summary>
            private void SetBottomImgAlpha(PsdNodeBase node, GameObject instance)
            {
                if (node.ChildrenNodes.Count <= BOTTOM_IMG_INDEX)
                    return;
                if (node.ChildrenNodes[BOTTOM_IMG_INDEX] is not PsdNodeImage bottomNode)
                    return;

                Image img = null;
                foreach (var image in instance.GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject.name != BOTTOM_IMG_NODE_NAME)
                        continue;
                    img = image;
                    break;
                }

                if (img == null)
                    return;

                var color = img.color;
                color.a = bottomNode.opacity;
                img.color = color;
                EditorUtility.SetDirty(img);
            }

            /// <summary>
            /// 标题文字图：PSD 侧命名不稳定按固定索引取，预制体侧按全名匹配
            /// </summary>
            private void SetTitleWordSprite(PsdNodeBase node, GameObject instance)
            {
                if (node.ChildrenNodes.Count <= TITLE_WORD_INDEX)
                    return;
                if (node.ChildrenNodes[TITLE_WORD_INDEX] is not PsdNodeImage imgNode || string.IsNullOrEmpty(imgNode.assetPath))
                    return;

                Image img = null;
                foreach (var image in instance.GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject.name != TITLE_WORD_NODE_NAME)
                        continue;
                    img = image;
                    break;
                }

                if (img == null)
                    return;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imgNode.assetPath);
                if (sprite == null)
                    return;

                img.sprite = sprite;
                img.SetNativeSize();
                EditorUtility.SetDirty(img);
            }
        }
    }
}
