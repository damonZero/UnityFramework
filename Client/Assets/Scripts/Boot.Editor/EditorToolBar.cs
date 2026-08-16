using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Boot.GameLife;

namespace Boot.Editor
{
    /// <summary>
    /// 编辑器顶部工具栏「软重启」按钮（参考 37 项目 Core.Editor/Tools/EditorToolBar.cs）。
    /// 通过反射定位 UnityEditor.Toolbar，在右侧注入一个 IMGUI 按钮：
    /// 运行中点击触发 <see cref="GameRestart.SoftRestart"/>，未运行时点击进入 Play 模式。
    /// 图标用 Unity 内置 <c>d_Refresh</c>（与参考项目一致）。
    /// </summary>
    [InitializeOnLoad]
    public static class EditorToolBar
    {
        private static ScriptableObject _currentToolbar;

        static EditorToolBar()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnGUI()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("d_Refresh"), "软重启")))
            {
                if (Application.isPlaying)
                {
                    // 延迟一帧再执行，避免在 GUI 事件 / 卸载场景回调里直接拆 UI 报错（对齐参考项目）。
                    EditorApplication.delayCall += () => GameRestart.SoftRestart();
                }
                else
                {
                    EditorApplication.isPlaying = true;
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void OnUpdate()
        {
            if (_currentToolbar != null)
                return;

            var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            if (toolbarType == null)
                return;

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (_currentToolbar == null)
                return;

            var rootField = _currentToolbar.GetType().GetField("m_Root",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (rootField == null)
                return;

            var concreteRoot = rootField.GetValue(_currentToolbar) as VisualElement;
            if (concreteRoot == null)
                return;

            var toolbarZone = concreteRoot.Q("ToolbarZoneRightAlign");
            if (toolbarZone == null)
                return;

            var parent = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                }
            };
            var container = new IMGUIContainer();
            container.onGUIHandler += OnGUI;
            parent.Add(container);
            toolbarZone.Add(parent);
        }
    }
}
