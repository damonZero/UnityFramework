using System;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class Psd2UguiImageReference : EditorWindow
    {
        public Action<EditorWindow> drawCb;

        public static EditorWindow OpenWindow(string path)
        {
            // 原 P33 依赖 Core.AssetReference 图片引用查找窗口, 本工程剥离
            Psd2UguiImageReference window = GetWindow<Psd2UguiImageReference>();
            window.titleContent = new GUIContent {text = "图片引用查找"};
            return window;
        }

        public void OnGUI()
        {
            drawCb?.Invoke(this);
        }
    }
}