using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 节点身份组件编辑器工具
    /// </summary>
    public class NodeIdentityEditor : EditorWindow
    {
        [MenuItem("开发中/节点身份管理")]
        public static void ShowWindow()
        {
            NodeIdentityEditor window = GetWindow<NodeIdentityEditor>();
            window.titleContent = new GUIContent("节点身份管理");
            window.Show();
        }

        private GameObject targetPrefab;
        private Vector2 scrollPosition;
        private bool showDebugInfo = false;

        public void OnGUI()
        {
            GUILayout.Space(20);
            GUILayout.Label("节点身份组件管理", EditorStyles.boldLabel);
            
            targetPrefab = EditorGUILayout.ObjectField("目标预制体", targetPrefab, typeof(GameObject), false) as GameObject;
            
            GUILayout.Space(10);
            
            if (targetPrefab != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("添加身份组件"))
                {
                    SmartNodeMatcher.AddIdentityComponents(targetPrefab);
                    EditorUtility.SetDirty(targetPrefab);
                }
                
                if (GUILayout.Button("更新身份组件"))
                {
                    SmartNodeMatcher.UpdateIdentityComponents(targetPrefab);
                    EditorUtility.SetDirty(targetPrefab);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("分析身份组件"))
                {
                    AnalyzeIdentityComponents();
                }
                
                showDebugInfo = EditorGUILayout.Toggle("显示调试信息", showDebugInfo);
                
                if (showDebugInfo)
                {
                    ShowDebugInfo();
                }
            }
            else
            {
                GUILayout.Label("请选择一个预制体", EditorStyles.helpBox);
            }
        }
        
        /// <summary>
        /// 分析身份组件
        /// </summary>
        private void AnalyzeIdentityComponents()
        {
            if (targetPrefab == null) return;
            
            var allNodes = GetAllNodes(targetPrefab);
            int totalNodes = allNodes.Count;
            int identityNodes = 0;
            int validIdentityNodes = 0;
            
            foreach (var node in allNodes)
            {
                var identity = node.GetComponent<NodeIdentityComponent>();
                if (identity != null)
                {
                    identityNodes++;
                    if (identity.NodeFileID != 0)
                    {
                        validIdentityNodes++;
                    }
                }
            }
            
            Debug.Log($"身份组件分析结果：");
            Debug.Log($"总节点数: {totalNodes}");
            Debug.Log($"有身份组件的节点数: {identityNodes}");
            Debug.Log($"有效身份组件节点数: {validIdentityNodes}");
            Debug.Log($"覆盖率: {(float)identityNodes / totalNodes * 100:F1}%");
        }
        
        /// <summary>
        /// 显示调试信息
        /// </summary>
        private void ShowDebugInfo()
        {
            if (targetPrefab == null) return;
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var allNodes = GetAllNodes(targetPrefab);
            foreach (var node in allNodes)
            {
                var identity = node.GetComponent<NodeIdentityComponent>();
                if (identity != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(node.name, GUILayout.Width(150));
                    GUILayout.Label($"FileID: {identity.NodeFileID}", GUILayout.Width(100));
                    GUILayout.Label($"Level: {identity.HierarchyLevel}", GUILayout.Width(50));
                    GUILayout.Label($"Path: {identity.NodePath}", GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(node.name, GUILayout.Width(150));
                    GUILayout.Label("无身份组件", GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndScrollView();
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
    }
} 