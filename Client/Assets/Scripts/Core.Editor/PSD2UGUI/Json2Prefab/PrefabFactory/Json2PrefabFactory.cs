//较为简单，没必要做真工厂

using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    public static class Json2PrefabFactory
    {
        public static Transform CreateRoot(PsdNodeRoot node)
        {
            var root = Psd2UguiPortShims.CreateBlankCanvas();

            FillParent(root.transform as RectTransform);
            root.gameObject.name = node.name;

            return root.transform;
        }

        public static Transform CreateGroup(PsdNodeGroup node, Transform parent)
        {
            //目前公共组件在psd中都是以组的形式存在
            Json2PrefabAssembleTool.CommonPrefabHandler(node, parent,
                out var commonPrefab, out var isInterruption);

            if (isInterruption && commonPrefab != null)
                return null; //为了阻断递归生成子节点

            return CreatGameObject(node, parent, false);
        }

        public static Transform CreateImage(PsdNodeImage node, Transform parent)
        {
            var tr = CreatGameObject(node, parent, false);
            var image = tr.gameObject.AddComponent<Image>();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(node.assetPath);
            image.sprite = sprite;

            // 应用图层不透明度(color.a 0-1)
            var color = image.color;
            color.a = node.opacity;
            image.color = color;

            image.raycastTarget = false;
            if (image.sprite && image.sprite.border.sqrMagnitude > 0)
                image.type = Image.Type.Sliced;
            tr.name = Psd2UguiTool.StayOnlyEndOfChinese(node.name, new char[] { '_' });
            return tr;
        }

        public static Transform CreateTMP(PsdNodeText node, Transform parent)
        {
            var tr = CreatGameObject(node, parent, false);
            var text = tr.gameObject.AddComponent<TextMeshProUGUI>();

            text.fontSize = node.fontSize;
            text.color = new Color(node.color[0], node.color[1], node.color[2], node.color[3]);

            SetTMPContent(text, node);

            if (node.italic)
                text.fontStyle |= FontStyles.Italic;

            if (node.bold)
                text.fontStyle |= FontStyles.Bold;

            if (node.underline)
                text.fontStyle |= FontStyles.Underline;

            //间距 TODO (临时推断 ，待后续检验)
            text.characterSpacing = node.letterSpacing * 0.1f;

            //对齐方式
            text.alignment = TextAlignmentOptions.Midline;
            if (node.alignment == "TopLeft")
                text.alignment = TextAlignmentOptions.TopLeft;
            else if (node.alignment == "Top")
                text.alignment = TextAlignmentOptions.Top;
            else if (node.alignment == "TopRight")
                text.alignment = TextAlignmentOptions.TopRight;
            else if (node.alignment == "Left")
                text.alignment = TextAlignmentOptions.Left;
            else if (node.alignment == "Center")
                text.alignment = TextAlignmentOptions.Center;
            else if (node.alignment == "Right")
                text.alignment = TextAlignmentOptions.Right;
            else if (node.alignment == "BottomLeft")
                text.alignment = TextAlignmentOptions.BottomLeft;
            else if (node.alignment == "Bottom")
                text.alignment = TextAlignmentOptions.Bottom;
            else if (node.alignment == "BottomRight")
                text.alignment = TextAlignmentOptions.BottomRight;

            tr.name = Psd2UguiTool.StayOnlyEndOfChinese(node.name, new char[] { '@', '_' });
            return tr;
        }

        public static void SetTMPContent(TextMeshProUGUI text, PsdNodeText node, bool unSetSize = false)
        {
            if (node == null)
            {
                return;
            }

            // 分段颜色富文本
            text.text = Json2PrefabAssembleTool.BuildRichTextContent(node);
            if (!unSetSize)
            {
                var tr = text.transform as RectTransform;
                var deltaSize = tr.sizeDelta;
                deltaSize.x = (float)Math.Round(deltaSize.x * 1.2f);
                tr.sizeDelta = deltaSize;
            }
        }

        public static void SetTMPContentBottomPageButton(TextMeshProUGUI text, PsdNodeText[] nodes,
            bool unSetSize = false)
        {
            if (nodes == null || nodes.Length < 2)
            {
                return;
            }

            string str = string.Empty;
            if (nodes.Length >= 2)
            {
                str = string.Format("<size=52><line-indent=-21>{0}</size><line-indent=40><line-height=58%>{1}",
                    nodes[0].content.Replace("\r", ""), nodes[1].content.Replace("\r", ""));
            }
            else if (nodes.Length > 0)
            {
                str = nodes[0].content.Replace("\r", "");
            }

            text.text = str;
            if (!unSetSize)
            {
                var tr = text.transform as RectTransform;
                var deltaSize = tr.sizeDelta;
                deltaSize.x = (float)Math.Round(deltaSize.x * 1.2f);
                tr.sizeDelta = deltaSize;
            }
        }

        public static void SetPosWhByNode(RectTransform tr, PsdNodeBase node)
        {
            SetPositionByNode(tr, node);
            SetWidthHeightByNode(tr, node);
        }

        public static void SetWidthHeightByNode(RectTransform tr, PsdNodeBase node)
        {
            var sizeDelta = tr.sizeDelta;
            // stretch 锚点(anchorMin != anchorMax)下, 该轴尺寸由父节点拉伸决定,
            // 若仍用 PSD 宽高覆盖 sizeDelta 会破坏拉伸效果, 故仅对非 stretch 轴应用 PSD 尺寸
            if (!IsStretchWidth(tr))
                sizeDelta.x = node.size[0];
            if (!IsStretchHeight(tr))
                sizeDelta.y = node.size[1];
            tr.sizeDelta = sizeDelta;
        }

        public static void SetSizeBySelfLockEnum(RectTransform tr, PsdNodeBase node, Json2PrefabEnum type)
        {
            switch (type)
            {
                case Json2PrefabEnum.None:
                    SetWidthHeightByNode(tr, node);
                    return;
                case Json2PrefabEnum.WidthHeight:
                    return;
                case Json2PrefabEnum.Width:
                    // 锁定宽度(保留预制体自身), 高度用 PSD 尺寸; 高度为 stretch 时跳过
                    if (!IsStretchHeight(tr))
                        tr.sizeDelta = new Vector2(tr.sizeDelta.x, node.size[1]);
                    return;
                case Json2PrefabEnum.Height:
                    // 锁定高度(保留预制体自身), 宽度用 PSD 尺寸; 宽度为 stretch 时跳过
                    if (!IsStretchWidth(tr))
                        tr.sizeDelta = new Vector2(node.size[0], tr.sizeDelta.y);
                    return;
            }
        }

        /// <summary>
        /// 水平方向(x/宽度)是否使用 stretch 锚点(anchorMin.x != anchorMax.x)
        /// </summary>
        private static bool IsStretchWidth(RectTransform tr)
        {
            return !Mathf.Approximately(tr.anchorMin.x, tr.anchorMax.x);
        }

        /// <summary>
        /// 垂直方向(y/高度)是否使用 stretch 锚点(anchorMin.y != anchorMax.y)
        /// </summary>
        private static bool IsStretchHeight(RectTransform tr)
        {
            return !Mathf.Approximately(tr.anchorMin.y, tr.anchorMax.y);
        }

        public static void SetPositionByNode(RectTransform tr, PsdNodeBase node)
        {
            var parentTr = tr.parent as RectTransform;

            // stretch 锚点(anchorMin != anchorMax)该轴位置由父节点拉伸决定,
            // 若重置为中心锚点会破坏拉伸, 故仅对非 stretch 轴重置锚点并按 PSD 位置摆放
            var stretchX = !Mathf.Approximately(tr.anchorMin.x, tr.anchorMax.x);
            var stretchY = !Mathf.Approximately(tr.anchorMin.y, tr.anchorMax.y);

            var anchorMin = tr.anchorMin;
            var anchorMax = tr.anchorMax;
            if (!stretchX)
            {
                anchorMin.x = 0.5f;
                anchorMax.x = 0.5f;
            }

            if (!stretchY)
            {
                anchorMin.y = 0.5f;
                anchorMax.y = 0.5f;
            }

            tr.anchorMin = anchorMin;
            tr.anchorMax = anchorMax;

            // 两个方向都 stretch 时, 位置完全由拉伸锚点决定, 保留预制体布局
            if (stretchX && stretchY)
                return;

            var pos = parentTr.InverseTransformPoint(
                new Vector2(node.pos[0], node.pos[1]));
            pos.z = 0;

            // stretch 轴保留预制体 offset, 仅非 stretch 轴覆盖 PSD 位置
            var localPos = tr.localPosition;
            if (!stretchX)
                localPos.x = pos.x;
            if (!stretchY)
                localPos.y = pos.y;
            tr.localPosition = localPos;
        }

        public static void SetPosBySelfLockEnum(RectTransform tr, PsdNodeBase node, Json2PrefabEnum type)
        {
            switch (type)
            {
                case Json2PrefabEnum.None:
                    SetPositionByNode(tr, node);
                    return;
                case Json2PrefabEnum.XY:
                    tr.ResetRectTransform();
                    return;
                case Json2PrefabEnum.X:
                    break;
                case Json2PrefabEnum.Y:
                    break;
            }

            var parentTr = tr.parent as RectTransform;
            var localPos = tr.localPosition;
            var pos = parentTr.InverseTransformPoint(
                new Vector2(node.pos[0], node.pos[1]));
            if (Json2PrefabEnum.X == type)
                pos.x = localPos.x;
            else
                pos.y = localPos.y;

            pos.z = 0;
            tr.localPosition = pos;
        }

        private static RectTransform CreatGameObject(PsdNodeBase node, Transform parent, bool convChi = true)
        {
            var go = new GameObject(Psd2UguiTool.AdjustName(node.name, "", convChi));
            var tr = go.AddComponent<RectTransform>();
            tr.parent = parent;
            SetPosWhByNode(tr, node);
            return tr;
        }

        private static void FillParent(RectTransform transform)
        {
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.pivot = new Vector2(0.5f, 0.5f);
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
            var newPosition = transform.anchoredPosition3D;
            newPosition.z = 0f;
            transform.anchoredPosition3D = newPosition;
        }
    }
}
