using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public interface INavigationTreeShowContent
    {
        void GUI(Rect rect,NavigationTreeWidth width);

        void GUITitle(int width);

        void GUIState(int width);

        void GUILayer(int width);

        void GUIEntrance(int width);

        void GUILogicalVisible(int width);

        void GUITransition(int width);

        void GUILockType(int width);
    }
}