//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description PSD创建Prefab的Button类
//*****************************************************************************

using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// Button创建类
    /// </summary>
    public class PsdButtonCreate : PsdPrefabBase, IPsdPrefabCreate
    {
        public PsdNodeType NodeType => PsdNodeType.ButtonNode;

        public bool IsContinue(PsdNodeInfo nodeInfo, int hierarchy)
        {
            return true;
        }

        public Transform PrefabNodeCreate(PsdNodeInfo nodeInfo, Transform parent, Transform root,
            int hierarchy, out bool isRecursion, StringBuilder warnInfo)
        {
            isRecursion = true;
            Transform retRect = default;
            GameObject prefabInstance = default;

            //查找项目中是否有预制体节点
            GameObject prefab = FindPrefab(nodeInfo.nodeName);

            //匹配已存在预制体节点
            if (prefab)
            {
                prefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
                Psd2UguiStatistics.UseCommonPrefab(nodeInfo.nodeName);
            }

            //有预制体节点,直接处理实例化对象
            RectTransform prefabTrans = null;
            if (prefabInstance)
            {
                isRecursion = false;
                GetMaxRectByChildren(nodeInfo);
                prefabTrans = prefabInstance.GetComponent<RectTransform>();
                SetRect(prefabTrans, nodeInfo, parent, root);
                retRect = parent;
            }

            //根据选择决定是否创建新节点
            if (!prefabInstance || !Psd2UguiEditor._instance.replaceDel)
            {
                isRecursion = true;
                retRect = CreateEmptyNode(nodeInfo, parent, root);
                // 原 P33 依赖 EmptyRaycast 组件, 本工程剥离
                ButtonMinSizeLimit((RectTransform) retRect);
                retRect.name = retRect.name.Replace(Psd2UguiRule.PER_KEY_BTN, "ctn");
                //警告处理
                TransformWarn(warnInfo, prefabTrans, retRect as RectTransform);
            }

            return retRect;
        }
    }
}