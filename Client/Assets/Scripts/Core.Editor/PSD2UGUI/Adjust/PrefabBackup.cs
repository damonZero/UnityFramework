using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 预制体备份类
    /// 用于保存和还原预制体的状态
    /// </summary>
    [System.Serializable]
    public class PrefabBackup
    {
        /// <summary>
        /// 节点备份信息
        /// </summary>
        [System.Serializable]
        public class NodeBackup
        {
            public string nodePath;
            public Vector3 position;
            public Vector3 localPosition;
            public Vector3 localScale;
            public Quaternion localRotation;
            public Vector2 sizeDelta;
            public Vector2 anchoredPosition;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public bool activeSelf;
            
            // 布局组件备份
            public LayoutGroupBackup layoutGroupBackup;
            public ContentSizeFitterBackup contentSizeFitterBackup;
            public AspectRatioFitterBackup aspectRatioFitterBackup;
            
            public NodeBackup(GameObject node)
            {
                nodePath = GetNodePath(node);
                activeSelf = node.activeSelf;
                
                var rectTransform = node.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    position = rectTransform.position;
                    localPosition = rectTransform.localPosition;
                    localScale = rectTransform.localScale;
                    localRotation = rectTransform.localRotation;
                    sizeDelta = rectTransform.sizeDelta;
                    anchoredPosition = rectTransform.anchoredPosition;
                    anchorMin = rectTransform.anchorMin;
                    anchorMax = rectTransform.anchorMax;
                    pivot = rectTransform.pivot;
                }
                
                // 备份布局组件
                var layoutGroup = node.GetComponent<LayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroupBackup = new LayoutGroupBackup(layoutGroup);
                }
                
                var contentSizeFitter = node.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    contentSizeFitterBackup = new ContentSizeFitterBackup(contentSizeFitter);
                }
                
                var aspectRatioFitter = node.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitterBackup = new AspectRatioFitterBackup(aspectRatioFitter);
                }
            }
            
            private string GetNodePath(GameObject node)
            {
                string path = node.name;
                Transform current = node.transform.parent;
                
                while (current != null)
                {
                    path = current.name + "/" + path;
                    current = current.parent;
                }
                
                return path;
            }
            
            /// <summary>
            /// 还原节点状态
            /// </summary>
            public void Restore(GameObject node)
            {
                if (node == null) return;
                
                node.SetActive(activeSelf);
                
                var rectTransform = node.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.position = position;
                    rectTransform.localPosition = localPosition;
                    rectTransform.localScale = localScale;
                    rectTransform.localRotation = localRotation;
                    rectTransform.sizeDelta = sizeDelta;
                    rectTransform.anchoredPosition = anchoredPosition;
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    rectTransform.pivot = pivot;
                }
                
                // 还原布局组件
                if (layoutGroupBackup != null)
                {
                    var layoutGroup = node.GetComponent<LayoutGroup>();
                    if (layoutGroup != null)
                    {
                        layoutGroupBackup.Restore(layoutGroup);
                    }
                }
                
                if (contentSizeFitterBackup != null)
                {
                    var contentSizeFitter = node.GetComponent<ContentSizeFitter>();
                    if (contentSizeFitter != null)
                    {
                        contentSizeFitterBackup.Restore(contentSizeFitter);
                    }
                }
                
                if (aspectRatioFitterBackup != null)
                {
                    var aspectRatioFitter = node.GetComponent<AspectRatioFitter>();
                    if (aspectRatioFitter != null)
                    {
                        aspectRatioFitterBackup.Restore(aspectRatioFitter);
                    }
                }
            }
        }
        
        /// <summary>
        /// 布局组件备份
        /// </summary>
        [System.Serializable]
        public class LayoutGroupBackup
        {
            public float spacing;
            public RectOffset padding;
            public TextAnchor childAlignment;
            public bool childControlWidth;
            public bool childControlHeight;
            public bool childForceExpandWidth;
            public bool childForceExpandHeight;
            
            public LayoutGroupBackup(LayoutGroup layoutGroup)
            {
                // spacing = layoutGroup.spacing;
                padding = new RectOffset(layoutGroup.padding.left, layoutGroup.padding.right, 
                                       layoutGroup.padding.top, layoutGroup.padding.bottom);
                childAlignment = layoutGroup.childAlignment;
                // childControlWidth = layoutGroup.childControlWidth;
                // childControlHeight = layoutGroup.childControlHeight;
                // childForceExpandWidth = layoutGroup.childForceExpandWidth;
                // childForceExpandHeight = layoutGroup.childForceExpandHeight;
            }
            
            public void Restore(LayoutGroup layoutGroup)
            {
                // layoutGroup.spacing = spacing;
                layoutGroup.padding = padding;
                layoutGroup.childAlignment = childAlignment;
                // layoutGroup.childControlWidth = childControlWidth;
                // layoutGroup.childControlHeight = childControlHeight;
                // layoutGroup.childForceExpandWidth = childForceExpandWidth;
                // layoutGroup.childForceExpandHeight = childForceExpandHeight;
            }
        }
        
        /// <summary>
        /// ContentSizeFitter备份
        /// </summary>
        [System.Serializable]
        public class ContentSizeFitterBackup
        {
            public ContentSizeFitter.FitMode horizontalFit;
            public ContentSizeFitter.FitMode verticalFit;
            
            public ContentSizeFitterBackup(ContentSizeFitter fitter)
            {
                horizontalFit = fitter.horizontalFit;
                verticalFit = fitter.verticalFit;
            }
            
            public void Restore(ContentSizeFitter fitter)
            {
                fitter.horizontalFit = horizontalFit;
                fitter.verticalFit = verticalFit;
            }
        }
        
        /// <summary>
        /// AspectRatioFitter备份
        /// </summary>
        [System.Serializable]
        public class AspectRatioFitterBackup
        {
            public AspectRatioFitter.AspectMode aspectMode;
            public float aspectRatio;
            
            public AspectRatioFitterBackup(AspectRatioFitter fitter)
            {
                aspectMode = fitter.aspectMode;
                aspectRatio = fitter.aspectRatio;
            }
            
            public void Restore(AspectRatioFitter fitter)
            {
                fitter.aspectMode = aspectMode;
                fitter.aspectRatio = aspectRatio;
            }
        }
        
        public string prefabName;
        public List<NodeBackup> nodeBackups = new List<NodeBackup>();
        public System.DateTime backupTime;
        
        public PrefabBackup(GameObject prefab)
        {
            prefabName = prefab.name;
            backupTime = System.DateTime.Now;
            
            // 收集所有节点
            var allNodes = GetAllNodes(prefab);
            
            foreach (var node in allNodes)
            {
                nodeBackups.Add(new NodeBackup(node));
            }
            
            Debug.Log($"创建预制体备份: {prefabName}，包含 {nodeBackups.Count} 个节点");
        }
        
        /// <summary>
        /// 还原预制体
        /// </summary>
        public void Restore(GameObject prefab)
        {
            if (prefab == null) return;
            
            Debug.Log($"开始还原预制体: {prefabName}");
            
            // 创建节点路径映射
            var nodePathMap = CreateNodePathMap(prefab);
            
            int restoredCount = 0;
            foreach (var nodeBackup in nodeBackups)
            {
                if (nodePathMap.TryGetValue(nodeBackup.nodePath, out GameObject node))
                {
                    nodeBackup.Restore(node);
                    restoredCount++;
                }
                else
                {
                    Debug.LogWarning($"找不到节点: {nodeBackup.nodePath}");
                }
            }
            
            Debug.Log($"预制体还原完成: {prefabName}，还原了 {restoredCount} 个节点");
            
            // 标记预制体为已修改
            EditorUtility.SetDirty(prefab);
        }
        
        /// <summary>
        /// 获取所有节点
        /// </summary>
        private List<GameObject> GetAllNodes(GameObject root)
        {
            var nodes = new List<GameObject>();
            GetAllNodesRecursive(root, nodes);
            return nodes;
        }
        
        /// <summary>
        /// 递归获取所有节点
        /// </summary>
        private void GetAllNodesRecursive(GameObject obj, List<GameObject> nodes)
        {
            nodes.Add(obj);
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                GetAllNodesRecursive(obj.transform.GetChild(i).gameObject, nodes);
            }
        }
        
        /// <summary>
        /// 创建节点路径映射
        /// </summary>
        private Dictionary<string, GameObject> CreateNodePathMap(GameObject root)
        {
            var pathMap = new Dictionary<string, GameObject>();
            CreateNodePathMapRecursive(root, pathMap, "");
            return pathMap;
        }
        
        /// <summary>
        /// 递归创建节点路径映射
        /// </summary>
        private void CreateNodePathMapRecursive(GameObject node, Dictionary<string, GameObject> pathMap, string currentPath)
        {
            string fullPath = string.IsNullOrEmpty(currentPath) ? node.name : currentPath + "/" + node.name;
            pathMap[fullPath] = node;
            
            for (int i = 0; i < node.transform.childCount; i++)
            {
                CreateNodePathMapRecursive(node.transform.GetChild(i).gameObject, pathMap, fullPath);
            }
        }
    }
} 