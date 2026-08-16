//************************************************************************
//Create by CH on 2024/4/18
//
//@Description  项目通用节点替换工具(移植后剥离 P33 的 CoreEditor Odin 窗口框架,
//              改为标准 EditorWindow; 保留替换选中对象的核型逻辑)
//************************************************************************

using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class ProjectNodeReplace : EditorWindow
    {
        private GameObject _prefab;

        [MenuItem("GameObject/UI制作/替换通用预制", false, priority = -200)]
        public static void OpenShowWindow()
        {
            ProjectNodeReplace window = GetWindow<ProjectNodeReplace>();
            window.titleContent = new GUIContent("通用预制替换");
        }

        private void OnGUI()
        {
            _prefab = EditorGUILayout.ObjectField("通用预制体", _prefab, typeof(GameObject), false) as GameObject;

            EditorGUI.BeginDisabledGroup(_prefab == null || Selection.activeGameObject == null);
            if (GUILayout.Button("替换选中对象", GUILayout.Height(40)))
            {
                ReplaceSelectObject();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ReplaceSelectObject()
        {
            GameObject selectObject = Selection.activeGameObject;
            if (selectObject == null)
            {
                Debug.LogError("请选中需要替换的对象");
                return;
            }

            //获取选中对象的根节点
            Transform selectRoot = selectObject.transform;
            var objPrefab = PrefabUtility.InstantiatePrefab(_prefab) as GameObject;
            if (objPrefab == null)
            {
                Debug.LogErrorFormat("无法创建该模板,检查模板预制体:{0}", AssetDatabase.GetAssetPath(_prefab));
                return;
            }

            //将当前对象放到到选中对象同级下一个索引处，并同步选中对象的名称以及RectTransform信息
            objPrefab.transform.SetParent(selectRoot.parent, false);
            objPrefab.transform.SetSiblingIndex(selectRoot.GetSiblingIndex() + 1);
            objPrefab.name = selectRoot.name;
            RectTransform rectTransform = objPrefab.GetComponent<RectTransform>();
            rectTransform.pivot = selectRoot.GetComponent<RectTransform>().pivot;
            rectTransform.anchorMax = selectRoot.GetComponent<RectTransform>().anchorMax;
            rectTransform.anchorMin = selectRoot.GetComponent<RectTransform>().anchorMin;
            rectTransform.anchoredPosition = selectRoot.GetComponent<RectTransform>().anchoredPosition;

            //根据文件目录类型定制化替换内容(按钮/标题: 同步子节点文本组件名称及内容)
            string assetPath = AssetDatabase.GetAssetPath(_prefab).Replace("\\", "/");
            if (assetPath.Contains("Prefab/button") || assetPath.Contains("Prefab/title"))
            {
                var text = objPrefab.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    var srcText = selectRoot.GetComponentInChildren<TextMeshProUGUI>();
                    if (srcText != null)
                    {
                        text.text = srcText.text;
                        text.name = srcText.name;
                    }
                }
            }

            //将原预制体节点下非预制体的节点移动到替换的预制体节点下
            Recursion(selectRoot, objPrefab.transform);

            //删除选中对象
            GameObject.DestroyImmediate(selectObject);
            EditorUtility.SetDirty(objPrefab);
        }

        // 递归遍历 将原预制体节点下非预制体的节点移动到替换的预制体节点下
        private void Recursion(Transform selectRoot, Transform objPrefab)
        {
            for (int i = 0; i < selectRoot.childCount; i++)
            {
                var child = selectRoot.GetChild(i);
                if (PrefabUtility.GetPrefabInstanceStatus(child) == PrefabInstanceStatus.NotAPrefab)
                {
                    child.SetParent(objPrefab, true);
                    //创建一个空对象，对原对象占位，放在原对象的位置
                    var emptyObj = new GameObject();
                    emptyObj.transform.SetParent(selectRoot, false);
                    emptyObj.transform.SetSiblingIndex(i);
                }
                else
                {
                    Recursion(child, objPrefab);
                }
            }
        }
    }
}
