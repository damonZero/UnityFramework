using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class ProjectNodeSimilarEditor : EditorWindow
    {
        private Dictionary<GameObject, List<GameObject>> _similars =
            new Dictionary<GameObject, List<GameObject>>();

        [MenuItem("GameObject/UI制作/通用预制体查找", false, priority = -300)]
        public static EditorWindow OpenShowWindow()
        {
            ProjectNodeSimilarEditor window = GetWindow<ProjectNodeSimilarEditor>();
            GameObject findObj = Selection.gameObjects[0];
            if (findObj == null)
                Debug.Log("请选中单个节点!");
            window.TraverseNode(findObj.transform, window.Collect);
            return window;
        }

        /// <summary>
        /// 收集相似预制体
        /// </summary>
        /// <param name="node"></param>
        private void Collect(Transform node)
        {
            ProjectNodeSimilarInfo test = new ProjectNodeSimilarInfo(node.gameObject);
            List<GameObject> similarObjs = test.GetSimilarPrefabs();
            if (similarObjs.Count == 0) return;
            _similars.Add(node.gameObject, similarObjs);
        }

        /// <summary>
        /// 遍历节点
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="cb"></param>
        private void TraverseNode(Transform tr, Action<Transform> cb)
        {
            int childCount = tr.childCount;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(tr.gameObject) || childCount == 0)
                return;
            cb(tr);
            for (int i = 0; i < childCount; i++)
            {
                TraverseNode(tr.GetChild(i), cb);
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            foreach (var similar in _similars)
            {
                EditorGUILayout.ObjectField(similar.Key, typeof(GameObject), true);
                foreach (var sim in similar.Value)
                {
                    EditorGUILayout.ObjectField(sim, typeof(GameObject), true);
                }

                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndVertical();
        }
    }
}