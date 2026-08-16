using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public class NavigationTreeLoaderShow : INavigationTreeShowContent, INavigationGUIObjectPool
    {
        public NavigationLoader loader;

        public NavigationTreeGroupShow groupShow;

        private readonly float _defaultWidth = 30;

        public void GUI(Rect rect, NavigationTreeWidth width)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(_defaultWidth);

            GUITitle(width.titleWidth);
            GUIState(width.stateWidth);
            GUILayer(width.layerWidth);
            GUIEntrance(width.entranceWidth);
            GUILogicalVisible(width.logicalVisibleWidth);
            GUITransition(width.transitionWidth);
            GUILockType(width.lockTypeWidth);

            GUILayout.EndHorizontal();
        }

        public void GUITitle(int width)
        {
            string nameType = $"{NavigationViewKit.GetBehaviourDes(loader)}[{loader.Name}]";
            GUIStyle titleStyle = NavigationViewKit.GetLeftAlignedButtonStyle();
            if (GUILayout.Button(nameType, titleStyle, GUILayout.Width(width - _defaultWidth)))
                NavigationViewKit.SelectLoader(loader);
        }

        public void GUIState(int width)
        {
            GUIContent guiContent = NavigationViewKit.GetStateGUIContent(loader.CurrentState);
            GUILayout.Label(guiContent, GUILayout.Width(width), GUILayout.Height(20));
        }

        public void GUILayer(int width)
        {
            string layer = loader is NavigationFormLoader formLoader ? formLoader.Layer.ToString() : "";
            GUILayout.Label(layer, GUILayout.Width(width));
        }

        public void GUIEntrance(int width)
        {
            GUILayout.Label(loader.Entrance.ToString(), GUILayout.Width(width));
        }

        public void GUILogicalVisible(int width)
        {
            string logicalVisible = loader is NavigationFormLoader formLoader
                ? formLoader.Form.LogicalVisible.ToString()
                : "";
            GUILayout.Label(logicalVisible, GUILayout.Width(width));
        }

        public void GUITransition(int width)
        {
            GUILayout.Label(loader.Transitioning.ToString(), GUILayout.Width(width));
        }

        public void GUILockType(int width)
        {
            string lockDis = NavigationViewKit.GetLockDescribe(loader.LockType);
            GUILayout.Label(lockDis, GUILayout.Width(width));
        }

        public void Reset()
        {
            loader = null;
            groupShow = null;
        }
    }
}