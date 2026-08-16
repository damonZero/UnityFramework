//*****************************************************************************
//Created By Liangc on 2019/6/3
//
//@Description PSD创建Prefab基类
//*****************************************************************************

using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;
using TMPro;
using Object = UnityEngine.Object;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD创建Prefab基类
    /// </summary>
    public class PsdPrefabBase
    {
        //UI层级
        private readonly int _uiLayer = LayerMask.NameToLayer("UI");

        /// <summary>
        /// 在首层节点下查找指定类型节点
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="findType"></param>
        /// <returns></returns>
        public PsdNodeInfo FindChildNode(PsdNodeInfo nodeInfo, PsdNodeType findType)
        {
            if (nodeInfo == null || !nodeInfo.HasChildNode || findType == PsdNodeType.Error)
                return null;

            PsdNodeInfo findRet = default;
            foreach (var info in nodeInfo.childNodes)
            {
                if (info.nodeType != findType) continue;
                findRet = info;
                break;
            }

            return findRet;
        }

        /// <summary>
        /// 在画布上添加UI节点
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="parent"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        protected Transform CreateEmptyNode(PsdNodeInfo nodeInfo,
            Transform parent, Transform root)
        {
            GetMaxRectByChildren(nodeInfo);
            GameObject addNode = new GameObject(nodeInfo.nodeName);
            RectTransform addRect = addNode.AddComponent<RectTransform>();
            SetRect(addRect, nodeInfo, parent, root);
            addNode.layer = _uiLayer;
            return addNode.transform;
        }

        /// <summary>
        /// 设置锚点位置
        /// </summary>
        /// <param name="addRect"></param>
        /// <param name="nodeInfo"></param>
        /// <param name="parent"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        protected Transform SetRect(RectTransform addRect,
            PsdNodeInfo nodeInfo, Transform parent, Transform root)
        {
            //PhotoShop中没有父节点对齐概念,都是以首层对齐,所以直接对齐画布,再设置父节点
            addRect.SetParent(root);
            addRect.localScale = Vector3.one;
            //设置节点的位置和大小
            Vector2 rootSize = ((RectTransform) root).sizeDelta;
            addRect.sizeDelta = nodeInfo.nodeRect.size;
            //获取中心点相对于根节点左上角的坐标
            Vector2 nodePos = new Vector2(nodeInfo.nodeRect.position.x + addRect.sizeDelta.x / 2,
                nodeInfo.nodeRect.position.y - addRect.sizeDelta.y / 2);
            //转换到根节点中心点坐标
            addRect.anchoredPosition = new Vector2(nodePos.x - rootSize.x / 2, nodePos.y + rootSize.y / 2);
            //设置好位置后再设置父节点
            addRect.SetParent(parent);
            addRect.localScale = Vector3.one;

            return addRect;
        }

        /// <summary>
        /// 通过所有子物体获取到父物体最大Rect
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        protected void GetMaxRectByChildren(PsdNodeInfo nodeInfo)
        {
            if (!nodeInfo.HasChildNode)
                return;

            List<Vector4> childRects = new List<Vector4>();
            PsdNodeInfo.SubsequentTraversalNode(nodeInfo, (currentNode, currentIndex) =>
            {
                if (currentNode == nodeInfo)
                    return true;

                Rect currentRect = currentNode.nodeRect;
                if (currentRect != Rect.zero && !IsFullBgImage(currentNode))
                    childRects.Add(new Vector4(currentRect.x, currentRect.y, currentRect.x + currentRect.width,
                        currentRect.y - currentRect.height));

                return true;
            });

            float xMax = float.MinValue, xMin = float.MaxValue, yMax = float.MaxValue, yMin = float.MinValue;
            foreach (var rect in childRects)
            {
                xMin = rect.x < xMin ? rect.x : xMin;
                yMin = rect.y > yMin ? rect.y : yMin;
                xMax = rect.z > xMax ? rect.z : xMax;
                yMax = rect.w < yMax ? rect.w : yMax;
            }

            Rect newRect = new Rect(xMin, yMin, xMax - xMin, yMin - yMax);
            nodeInfo.nodeRect = newRect;
        }

        /// <summary>
        /// 修复UI节点位置
        /// </summary>
        /// <param name="nodeInfo">修复节点PSD信息</param>
        /// <param name="repairNode">修复节点</param>
        /// <param name="alignNode">对齐节点(根节点)</param>
        protected void RepairRect(PsdNodeInfo nodeInfo, RectTransform repairNode, RectTransform alignNode)
        {
            //计算Rect
            GetMaxRectByChildren(nodeInfo);
            //通过生成新节点计算位置
            Transform oldParent = repairNode.parent;
            GameObject addNode = new GameObject(nodeInfo.nodeName);
            RectTransform addRect = addNode.AddComponent<RectTransform>();
            SetRect(addRect, nodeInfo, oldParent, alignNode);
            //还原锚点对齐点
            repairNode.anchorMax = new Vector2(0.5f, 0.5f);
            repairNode.anchorMin = new Vector2(0.5f, 0.5f);
            //位置修复
            repairNode.anchoredPosition = addRect.anchoredPosition;
            Object.DestroyImmediate(addRect.gameObject);
        }

        /// <summary>
        /// 通过自身节点或父节点判断是否是相同节点
        /// </summary>
        /// <param name="nodeInfo">psd节点</param>
        /// <param name="repairNode">UI节点</param>
        /// <param name="depth">判断层级深度</param>
        /// <returns></returns>
        public bool IsSameNode(PsdNodeInfo nodeInfo, RectTransform repairNode, int depth)
        {
            bool isSame = true;
            PsdNodeInfo psdParent = nodeInfo;
            RectTransform rectTransParent = repairNode;

            while (depth > 0 && psdParent.parentNode != null && rectTransParent.parent != null)
            {
                if (psdParent.nodeName != rectTransParent.name)
                {
                    isSame = false;
                    break;
                }

                psdParent = psdParent.parentNode;
                rectTransParent = (RectTransform) rectTransParent.parent;
                --depth;
            }

            return isSame;
        }

        //锚点数据
        private readonly struct AnchorData
        {
            public readonly Vector2 anchorMin; //锚点的anchorMin值
            public readonly Vector2 anchorMax; //锚点的anchorMax值
            public readonly Vector2 anchorPos; //锚点相对于中心点坐标系的坐标

            public AnchorData(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchorPos)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.anchorPos = anchorPos;
            }
        }

        /// <summary>
        /// 锚点自适应
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        protected Vector2 AnchorAutoFit(RectTransform child)
        {
            if (child == null || child.parent == null || !(child.parent.transform is RectTransform))
                return Vector2.zero;

            RectTransform parent = (RectTransform) child.parent;

            //用节点自身中心点去对比父节点上9常用个对齐点,找到最近点作为锚点
            //父节点锚点顺序按照顺时针排序,0-3号为角点,4-7号为边上中点,8号为中心点
            //整个计算采用与父节点中心点的坐标系
            Vector2 parentSize = parent.rect.size;
            AnchorData[] parentPoints = new AnchorData[9];
            parentPoints[0] = new AnchorData(new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(-parentSize.x / 2, parentSize.y / 2));
            parentPoints[1] = new AnchorData(new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(parentSize.x / 2, parentSize.y / 2));
            parentPoints[2] = new AnchorData(new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(parentSize.x / 2, -parentSize.y / 2));
            parentPoints[3] = new AnchorData(new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(-parentSize.x / 2, -parentSize.y / 2));
            parentPoints[4] =
                new AnchorData(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, parentSize.y / 2));
            parentPoints[5] =
                new AnchorData(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(parentSize.x / 2, 0));
            parentPoints[6] = new AnchorData(new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, -parentSize.y / 2));
            parentPoints[7] = new AnchorData(new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(-parentSize.x / 2, 0));
            parentPoints[8] = new AnchorData(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0));

            //得到子节点相对于父节点中心点坐标的位置
            Vector2 childPos = child.localPosition;

            //找到子节点中心点相对于父节点所有节点最近锚点
            float minDis = float.MaxValue;
            AnchorData minDisAnchor = default;
            foreach (var point in parentPoints)
            {
                float pointDis = Vector2.Distance(point.anchorPos, childPos);
                if (pointDis < minDis)
                {
                    minDis = pointDis;
                    minDisAnchor = point;
                }
            }

            //坐标系平移后的锚点坐标系下的坐标
            Vector2 anchorRelativePos = childPos - minDisAnchor.anchorPos;
            //设置坐标
            child.anchorMax = minDisAnchor.anchorMax;
            child.anchorMin = minDisAnchor.anchorMin;
            child.anchoredPosition = anchorRelativePos;

            return minDisAnchor.anchorPos;
        }

        /// <summary>
        /// 查找已存在预制体
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        protected GameObject FindPrefab(string prefabName)
        {
            GameObject retObj = default;

            if (!Directory.Exists(Psd2UguiRule.PREFAB_FIND_PATH))
                return null;

            DirectoryInfo findDir = new DirectoryInfo(Psd2UguiRule.PREFAB_FIND_PATH);
            Psd2UguiTool.TraverseFolder(findDir,
                fileInfo =>
                {
                    if (Path.GetFileNameWithoutExtension(fileInfo.Name) == prefabName)
                    {
                        string findPath = Psd2UguiTool.FilePathToUnityAssetPath(fileInfo.FullName);
                        retObj = AssetDatabase.LoadAssetAtPath<GameObject>(findPath);
                        return false;
                    }

                    return true;
                });

            return retObj;
        }

        /// <summary>
        /// PSD节点查找相同的UI节点
        /// </summary>
        /// <param name="findPsdNode"></param>
        /// <param name="rootNode"></param>
        /// <param name="equalJudge"></param>
        /// <param name="preciseMatching"></param>
        /// <returns></returns>
        protected static RectTransform FindUINode(PsdNodeInfo findPsdNode, RectTransform rootNode,
            Func<PsdNodeInfo, RectTransform, bool, bool> equalJudge, bool preciseMatching)
        {
            if (findPsdNode == null || rootNode == null || equalJudge == null)
                return null;

            RectTransform findRet = null;
            PreorderTraverseUINode(rootNode,
                currentRectTrans =>
                {
                    if (equalJudge(findPsdNode, currentRectTrans, preciseMatching))
                        findRet = currentRectTrans;
                },
                currentRectTrans => findRet == null);

            return findRet;
        }

        /// <summary>
        /// 前序遍历UI节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="traverseHandle"></param>
        /// <param name="continueJudge"></param>
        public static void PreorderTraverseUINode(RectTransform node,
            Action<RectTransform> traverseHandle, Func<RectTransform, bool> continueJudge)
        {
            if (node == null || traverseHandle == null)
                return;

            traverseHandle(node);

            int childCount = node.childCount;
            if (childCount == 0 || !continueJudge(node))
                return;

            for (int i = 0; i < childCount; i++)
            {
                PreorderTraverseUINode(node.GetChild(i) as RectTransform, traverseHandle, continueJudge);
            }
        }

        /// <summary>
        /// 是否是全屏背景图
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected bool IsFullBgImage(PsdNodeInfo node)
        {
            return node.nodeType == PsdNodeType.Image
                   && (node.nodeName.Contains(Psd2UguiRule.IMAGE_BG_KEY) ||
                       node.layer.Rect.size == Psd2UguiRule.FULL_SCREEN_IMAGE_SIZE);
        }

        /// <summary>
        /// 文本属性配置读取
        /// </summary>
        /// <param name="t2d"></param>
        /// <param name="keyword"></param>
        protected void TextPropertyRead(TextMeshProUGUI t2d, string keyword)
        {
            //格式判断
            if (!keyword.Contains("@"))
                return;

            keyword = keyword.Replace("t2d@", "");
            string[] splits = keyword.Split('@');
            if (splits.Length < 2)
                return;

            //文本属性Tid提取
            int.TryParse(splits[splits.Length - 2], out var propertyTid);

            if (propertyTid <= 0)
                return;

            // 原 P33 依赖 TextMeshPro2D 的 useTextProperty/textTid 文本属性系统, 本工程剥离
        }

        /// <summary>
        /// 按钮最小尺寸限制,方便能正常点击到
        /// </summary>
        /// <param name="rect"></param>
        protected void ButtonMinSizeLimit(RectTransform rect)
        {
            Vector2 size = rect.sizeDelta;
            if (!(size.x < Psd2UguiRule.BUTTON_MIN_SIZE.x) &&
                !(size.y < Psd2UguiRule.BUTTON_MIN_SIZE.y)) return;
            float changeX = size.x < Psd2UguiRule.BUTTON_MIN_SIZE.x ? Psd2UguiRule.BUTTON_MIN_SIZE.x : size.x;
            float changeY = size.y < Psd2UguiRule.BUTTON_MIN_SIZE.y ? Psd2UguiRule.BUTTON_MIN_SIZE.y : size.y;
            rect.sizeDelta = new Vector2(changeX, changeY);
        }

        //警告处理
        protected void TransformWarn(StringBuilder warnInfo, RectTransform prefabInstance, RectTransform psdInstance)
        {
            if (prefabInstance == null || psdInstance == null) return;
            Vector2 prefabSize = prefabInstance.sizeDelta * prefabInstance.localScale;
            Vector2 psdSize = psdInstance.sizeDelta * psdInstance.localScale;
            //小于2个像素,x/y各1个像素
            if (Vector2.Distance(prefabSize, psdSize) < 2)
                return;
            string warn = $"\n通用节点与PSD导入节点尺寸有差异:{prefabInstance.name}";
            warnInfo.Append(warn);
            Debug.Log(warn, psdInstance);
        }
    }
}