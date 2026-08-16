using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Framework.View.Navigation;
namespace Framework.View.Navigation.Editor
{
    public class NavigationTreeWindow
    {
        private readonly List<NavigationTreeGroupShow> _groupShows = new List<NavigationTreeGroupShow>();
        private readonly List<NavigationTreeGroupShow> _groupShowsTmp = new List<NavigationTreeGroupShow>();
        private readonly List<NavigationTreeGroupShow> _searchGroupShows = new List<NavigationTreeGroupShow>();

        private readonly NavigationGUIObjectPool<NavigationTreeGroupShow> _groupShowPool =
            new NavigationGUIObjectPool<NavigationTreeGroupShow>();

        private readonly NavigationGUIObjectPool<NavigationTreeLoaderShow> _loaderShowPool =
            new NavigationGUIObjectPool<NavigationTreeLoaderShow>();

        private string _searchText = "";

        private GUIStyle _grayStyle = NavigationViewKit.GetColoredBoxStyle(Color.gray);
        private GUIStyle _greyStyle = NavigationViewKit.GetColoredBoxStyle(Color.grey);

        private NavigateContainer _lastShowGroup;

        public void Refresh(NavigateContainer root)
        {
            _lastShowGroup = root;
            _groupShowsTmp.Clear();
            _groupShowsTmp.AddRange(_groupShows);
            _groupShows.Clear();

            foreach (var group in root.ForeachContainers(TraversalOrder.Forward))
            {
                NavigationTreeGroupShow groupShow = _groupShowPool.Get();
                groupShow.group = group;
                groupShow.fold = _groupShowsTmp.Find(show => show.group.Name == group.Name)?.fold ?? true;
                _groupShows.Add(groupShow);
                for (int i = groupShow.loaderShows.Count - 1; i >= 0; i--)
                {
                    _loaderShowPool.Put(groupShow.loaderShows[i]);
                }

                groupShow.loaderShows.Clear();

                foreach (var loader in group.ForeachLoaders(TraversalOrder.Reverse))
                {
                    NavigationTreeLoaderShow loaderShow = _loaderShowPool.Get();
                    loaderShow.loader = loader;
                    loaderShow.groupShow = groupShow;
                    groupShow.loaderShows.Add(loaderShow);
                }
            }
            for (int i = _groupShowsTmp.Count - 1; i >= 0; i--)
            {
                _groupShowPool.Put(_groupShowsTmp[i]);
            }

            _groupShowsTmp.Clear();
            if (!string.IsNullOrEmpty(_searchText))
                Search(_searchText);
        }

        private Vector2 _dragTree;

        public void OnGUI(Rect rootRect, float treeWindowRect)
        {
            string searchTmp = NavigationViewKit.DrawSearch(_searchText, treeWindowRect);
            if (searchTmp != _searchText)
                Search(searchTmp);
            _searchText = searchTmp;

            NavigationTreeWidth width = new NavigationTreeWidth
            {
                titleWidth = 250,
                stateWidth = 50,
                layerWidth = 80,
                entranceWidth = 80,
                logicalVisibleWidth = 80,
                transitionWidth = 80,
                lockTypeWidth = 500
            };

            _dragTree = GUILayout.BeginScrollView(_dragTree);
            GUILayout.BeginHorizontal();
            GUILayout.Label("导航组", EditorStyles.boldLabel, GUILayout.Width(width.titleWidth));
            GUILayout.Label("|状态", EditorStyles.boldLabel, GUILayout.Width(width.stateWidth));
            GUILayout.Label("|层级", EditorStyles.boldLabel, GUILayout.Width(width.layerWidth));
            GUILayout.Label("|入口", EditorStyles.boldLabel, GUILayout.Width(width.entranceWidth));
            GUILayout.Label("|逻辑显隐", EditorStyles.boldLabel, GUILayout.Width(width.logicalVisibleWidth));
            GUILayout.Label("|转场中", EditorStyles.boldLabel, GUILayout.Width(width.transitionWidth));
            GUILayout.Label("|锁类型", EditorStyles.boldLabel, GUILayout.Width(width.lockTypeWidth));
            GUILayout.EndHorizontal();

            List<NavigationTreeGroupShow> shows = _searchGroupShows.Count == 0 ? _groupShows : _searchGroupShows;
            foreach (var groupShow in shows)
            {
                GUILayout.Label("---------------------------------------------------------------------------------------------------------------------------------------------------");
                groupShow.GUI(new Rect(), width);
            }

            GUILayout.EndScrollView();
        }

        private void Search(string searchText)
        {
            _searchGroupShows.Clear();
            searchText = searchText.ToLower();
            foreach (var groupShow in _groupShows)
            {
                if (groupShow.group.Name.ToLower().Contains(searchText))
                {
                    _searchGroupShows.Add(groupShow);
                    continue;
                }

                foreach (var loaderShow in groupShow.loaderShows)
                {
                    if (!loaderShow.loader.Name.ToLower().Contains(searchText)) continue;
                    _searchGroupShows.Add(groupShow);
                    break;
                }
            }
        }
    }
}
