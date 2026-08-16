//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description PSD创建Prefab的Text类
//*****************************************************************************

using System.Text;
using TMPro;
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD创建Prefab的Text类
    /// </summary>
    public class PsdTextCreate : PsdPrefabBase, IPsdPrefabCreate
    {
        PsdNodeType IPsdPrefabCreate.NodeType => PsdNodeType.Text;

        public bool IsContinue(PsdNodeInfo nodeInfo, int hierarchy)
        {
            return true;
        }

        Transform IPsdPrefabCreate.PrefabNodeCreate(PsdNodeInfo nodeInfo, Transform parent, Transform root,
            int hierarchy, out bool isRecursion, StringBuilder warnInfo)
        {
            isRecursion = true;
            //生成文本节点            
            GetMaxRectByChildren(nodeInfo);

            //生成字体节点
            Transform addRect = CreateEmptyNode(nodeInfo, parent, root);
            addRect.gameObject.name = nodeInfo.nodeName;

            //挂载t2d组件,挂载t2d组件时会自动设置为(50,200)
            TextMeshProUGUI addText = addRect.gameObject.AddComponent<TextMeshProUGUI>();
            //默认加宽一点,避免频繁换行(这样会导致文字不方便对齐,所以不放大,需要放大的手动设置)
            // addText.rectTransform.sizeDelta = addText.rectTransform.sizeDelta * 1.5f;

            addText.color = nodeInfo.nodeText.color;
            //FIXME 名字太长了读取不到后面的字号大小
            string[] nameParse = nodeInfo.nodeName.Split('@');
            if (nameParse.Length > 1)
            {
                int.TryParse(nameParse[nameParse.Length - 1], out var sizeTemp);
                addText.fontSize = sizeTemp;
            }
            else
                addText.fontSize = nodeInfo.nodeText.fontSize;

            addText.raycastTarget = false;

            //判断PSD解析的高度是否,超过字体高度的一半,超过则可换行,没超过默认不换行
            //不换行原因:PSD解析高度和文本属性表配置高度,有细微差异,换行很容易触发,需要手动调节
            if (nodeInfo.nodeRect.height > addText.fontSize * 1.5f)
            {
                //多行时默认"上+左"
                addText.enableWordWrapping = true;
                addText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            }
            else
            {
                addText.enableWordWrapping = false;
                //单行时(单个字符:居中;多个字符:"中+左")
                addText.alignment = nodeInfo.nodeRect.width < addText.fontSize
                    ? TMPro.TextAlignmentOptions.Midline
                    : TMPro.TextAlignmentOptions.MidlineLeft;
            }

            //本地自定义属性赋值(字号在配置表中未配置为0时用之前的字号)
            //因为动态生成字体的原因,通过文本属性设置字体必须在text赋值之前
            TextPropertyRead(addText, nodeInfo.nodeName);
            //文本属性初次赋值,描边信息无法读取,建议走本地属性配置
            addText.SetText(nodeInfo.nodeText.text.Replace("\r", ""));
            //todo c.c.
            // TextMeshProEditorEx.AdjustTmpOrgText(addText);

            return parent;
        }
    }
}