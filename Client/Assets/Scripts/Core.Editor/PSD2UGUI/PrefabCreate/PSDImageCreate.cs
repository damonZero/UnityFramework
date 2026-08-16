//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description PSD创建Prefab的Image类
//*****************************************************************************

using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD创建Prefab的Image类
    /// </summary>
    public class PsdImageCreate : PsdPrefabBase, IPsdPrefabCreate
    {
        PsdNodeType IPsdPrefabCreate.NodeType => PsdNodeType.Image;

        public bool IsContinue(PsdNodeInfo nodeInfo, int hierarchy)
        {
            return true;
        }

        Transform IPsdPrefabCreate.PrefabNodeCreate(PsdNodeInfo nodeInfo, Transform parent, Transform root,
            int hierarchy, out bool isRecursion, StringBuilder warnInfo)
        {
            isRecursion = true;
            //通过图片找项目中存在的预制体
            GameObject prefab = FindPrefab(nodeInfo.nodeName);
            RectTransform prefabTrans = null;
            if (prefab)
            {
                prefab = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
                prefabTrans = prefab.GetComponent<RectTransform>();
                SetRect(prefabTrans, nodeInfo, parent, root);
            }

            if (!prefab || !Psd2UguiEditor._instance.replaceDel)
            {
                //生成图片节点
                Transform addRect = CreateEmptyNode(nodeInfo, parent, root);

                //设置图片属性
                Image addImage = addRect.gameObject.AddComponent<Image>();
                addImage.sprite = nodeInfo?.nodeImage?.sprite;
                addImage.raycastTarget = false;
                if (addImage.sprite && addImage.sprite.border.sqrMagnitude > 0)
                    addImage.type = Image.Type.Sliced;

                //处理对称和全屏背景图
                SymmetryImageHandle((RectTransform) addRect);
                FullScreenImageHandle((RectTransform) addRect, nodeInfo);

                //警告处理
                TransformWarn(warnInfo, prefabTrans, addRect as RectTransform);
            }

            return parent;
        }


        //对称图片处理
        private void SymmetryImageHandle(RectTransform rect)
        {
            if (!rect.name.EndsWith(Psd2UguiRule.IMAGE_SYMMETRY_KEY))
                return;
            rect.localScale = new Vector3(-1, 1, 1);
        }

        //全屏背景图处理
        private void FullScreenImageHandle(RectTransform rect, PsdNodeInfo info)
        {
            if (!IsFullBgImage(info))
                return;
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.position = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Psd2UguiRule.FULL_SCREEN_IMAGE_SIZE;
            //todo c.c.
            // Core.FullBgScaler fullBgScaler = rect.gameObject.AddComponent<Core.FullBgScaler>();
            // fullBgScaler.ReFit();
        }
    }
}