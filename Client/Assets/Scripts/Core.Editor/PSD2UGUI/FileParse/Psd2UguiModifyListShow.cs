//*****************************************************************************
//Created By Liangc on 2021/5/11
//
//@Description PSD节点差异展示界面
//*****************************************************************************

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class Psd2UguiModifyListShow : EditorWindow
    {
        private List<Texture2D> textureList = new List<Texture2D>();
        private Vector2 scrollPosition = Vector2.zero;
        /// <summary>
        /// 列表图片大小设置
        /// </summary>
        private float imageWith = 100;
        private float imageHeight = 100;


        /// <summary>
        /// 同名预览窗口
        /// </summary>
        /// <param name="list"></param>
        /// <param name="showName"></param>
        public static void ShowWindow(List<Psd2UguiChoiceData> list, string showName)
        {
            Psd2UguiModifyListShow window = EditorWindow.GetWindow<Psd2UguiModifyListShow>("同名预览列表");
            window.textureList.Clear();
            foreach (var item in list)
            {
                if (item.node.layer.Name.Equals(showName))
                {
                    window.textureList.Add(item.originalImage);
                }
            }
            window.Show();
            window.minSize = new Vector2(600, 600);
            window.Repaint();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);


            // 绘制图片列表
            GUILayout.BeginHorizontal();
            GUILayout.Space(10f);
            GUILayout.BeginVertical();

            for (int i = 0; i < textureList.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10f);
                GUILayout.Label(string.Format("{0} - {1}【{2}x{3}】", i + 1, textureList[i].name, textureList[i].width, textureList[i].height), GUILayout.Width(250), GUILayout.Height(50));
                GUILayout.Label(textureList[i], GUILayout.Width(imageWith), GUILayout.Height(imageHeight));
                GUILayout.Space(10f);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.Space(10f);
            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }
    }
}