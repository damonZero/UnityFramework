using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public class NavigationTreeGroupShow : INavigationTreeShowContent, INavigationGUIObjectPool
    {
        public NavigateContainer group;

        public readonly List<NavigationTreeLoaderShow> loaderShows = new();

        internal bool fold = true;

        public void GUI(Rect rect, NavigationTreeWidth width)
        {
            GUILayout.BeginHorizontal();
            GUITitle(width.titleWidth);
            GUIState(width.stateWidth);
            GUILayer(width.layerWidth);
            GUIEntrance(width.entranceWidth);
            GUILogicalVisible(width.logicalVisibleWidth);
            GUITransition(width.transitionWidth);
            GUILockType(width.lockTypeWidth);
            GUILayout.EndHorizontal();
            Rect lastRect = GUILayoutUtility.GetLastRect();
            Rect topRect = new Rect(0, 0, rect.width, lastRect.y + lastRect.height);
            NavigationViewKit.DrawBorderedRect(topRect, Color.gray, 1);

            if (!fold) return;

            GUILayout.BeginVertical();
            foreach (var loaderShow in loaderShows)
            {
                loaderShow.GUI(rect, width);
            }

            GUILayout.EndVertical();
        }

        public void GUITitle(int width)
        {
            string nameType = NavigationViewKit.GetBehaviourDes(group);
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            fold = EditorGUILayout.Foldout(fold, $"{nameType}[{group.Name}]");
            GUILayout.EndHorizontal();
        }

        public void GUIState(int width)
        {
            GUIContent guiContent = NavigationViewKit.GetStateGUIContent(group.CurrentState);
            GUILayout.Label(guiContent, GUILayout.Width(width), GUILayout.Height(20));
        }

        public void GUILayer(int width)
        {
            GUILayout.Space(width + 4);
        }

        public void GUIEntrance(int width)
        {
            GUILayout.Label(group.HasEntrance().ToString(), GUILayout.Width(width));
        }

        public void GUILogicalVisible(int width)
        {
            GUILayout.Space(width + 4);
        }

        public void GUITransition(int width)
        {
            GUILayout.Label(group.Transitioning.ToString(), GUILayout.Width(width));
        }

        public void GUILockType(int width)
        {
            string lockDis = NavigationViewKit.GetLockDescribe(group.LockType);
            GUILayout.Label(lockDis, GUILayout.Width(width));
        }

        public void Reset()
        {
            group = null;
            fold = true;
            loaderShows.Clear();
        }

        private GUIStyle GetFoldoutStyle(bool isExpanded)
        {
            GUIStyle style = new GUIStyle(EditorStyles.foldout);
            if (isExpanded)
                style.normal = style.onActive;
            return style;
        }
    }
}