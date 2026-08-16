using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public class RecordStackWindow : EditorWindow
    {
        private enum RecordStackType
        {
            None,
            CSharp
        }

        //记录堆栈数据
        private class RecordStackData
        {
            internal readonly string stack;
            internal readonly RecordStackType recordStackType;
            internal readonly string skipIdeFileName;
            internal readonly int line;

            private readonly Regex _regexCSharp = new(@"\\([\w.]+)\.cs:(\d+)");

            public RecordStackData(string stack)
            {
                if (string.IsNullOrEmpty(stack))
                    return;
                this.stack = stack.Trim();
                if (_regexCSharp.IsMatch(this.stack))
                {
                    recordStackType = RecordStackType.CSharp;
                    string[] splits = _regexCSharp.Split(this.stack);
                    skipIdeFileName = $"{splits[1]}.cs";
                    line = Convert.ToInt32(splits[2]);
                }
            }

            //是否能跳转
            public bool CanSkip()
            {
                return recordStackType != RecordStackType.None;
            }
        }

        //描述
        private string _describe;

        //记录数据
        private readonly List<RecordStackData> _recordStacks = new();

        //解析数据
        private void ParseRecordStack(string stack)
        {
            if (string.IsNullOrEmpty(stack))
                return;
            _recordStacks.Clear();
            var stackArray = stack.Split('\n');
            foreach (var stackLine in stackArray)
            {
                if (string.IsNullOrEmpty(stackLine))
                    continue;
                _recordStacks.Add(new RecordStackData(stackLine));
            }
        }

        //绘制一条堆栈
        private void DrawOneStack(RecordStackData recordStackData)
        {
            GUILayout.BeginHorizontal();
            //绘制跳转按钮
            if (recordStackData.CanSkip())
            {
                // if (GUILayout.Button("跳转IDE", GUILayout.Width(70)))
                // {
                //     if (recordStackData.recordStackType == RecordStackType.Lua)
                //         LuaToIdea.OnOpenLuaAsset(recordStackData.skipIdeFileName, recordStackData.line);
                //     if (recordStackData.recordStackType == RecordStackType.CSharp)
                //         LuaToIdea.OnOpenCSharpAsset(recordStackData.skipIdeFileName, recordStackData.line);
                // }
            }
            else
            {
                GUILayout.Space(77);
            }

            //绘制堆栈内容
            GUILayout.Label(recordStackData.stack);

            GUILayout.EndHorizontal();
        }

        private Vector2 _scrollPos;

        private void OnGUI()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos,
                GUILayout.Width(position.width), GUILayout.Height(position.height));
            GUILayout.BeginVertical();
            GUILayout.Label(_describe);
            GUILayout.Space(10);

            foreach (var recordStack in _recordStacks)
            {
                DrawOneStack(recordStack);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        //创建窗口
        public static RecordStackWindow CreateWindow(string stack, string describe)
        {
            var window = GetWindow<RecordStackWindow>(false, describe);
            // window.titleContent = new GUIContent($"堆栈详情");
            window._describe = describe;
            window.ParseRecordStack(stack);
            window.Show();
            return window;
        }
    }
}
