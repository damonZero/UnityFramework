//************************************************************************
//Create by Liangc on 2021/4/9
//
//@Description  项目通用节点详细信息展示
//************************************************************************

using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class ProjectNodeDetailShow : EditorWindow
    {
        private ProjectNodeInfo _showNode;
        private readonly Vector2 _windowSize = new Vector2(375, 667);
        private readonly Rect _imageRect = new Rect(0, 0, 375, 667);

        public static ProjectNodeDetailShow Show(ProjectNodeInfo showNode, Vector2 pos)
        {
            ProjectNodeDetailShow window = GetWindow<ProjectNodeDetailShow>();
            window.Init(showNode, pos);
            window.titleContent = new GUIContent("节点详细信息展示");
            return window;
        }

        public void Init(ProjectNodeInfo showNode, Vector2 pos)
        {
            Focus();
            _showNode = showNode;
            position = new Rect(pos, _windowSize);
            int x = (int) _windowSize.x;
            int y = (int) _windowSize.y;
            if (!_showNode.detailImage)
            {
                _showNode.detailImage =
                    PrefabPreview.GetPrefabPreview(_showNode.prefabObj, x, y);
            }
        }

        private void OnGUI()
        {
            GUI.Box(_imageRect, _showNode.detailImage);
            GUILayout.BeginVertical();
            GUILayout.Label($"描述：{_showNode.description}");
            GUILayout.Label($"作者：{_showNode.author}");
            GUILayout.Label($"时间：{_showNode.time}");
            GUILayout.EndVertical();
        }
    }
}