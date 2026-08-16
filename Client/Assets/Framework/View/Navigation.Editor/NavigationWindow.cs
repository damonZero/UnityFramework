using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Framework.View.Navigation;
namespace Framework.View.Navigation.Editor
{
    public class NavigationWindow : EditorWindow
    {
        //显示的Group
        public NavigateContainer selectShowGroup;

        //根节点
        private NavigateContainer _root;

        //绘制操作历史区域宽度
        private const int ALL_OPERATE_WIDTH = 240;

        //圆形红点
        private Texture2D _circleRed;
        private Texture2D _circleGray;

        //导航总视图
        private NavigationTotalView _navigationTotalView;
        private VisualElement _navigationTotalViewElement;

        //导航树形结构视图
        private NavigationTreeWindow _navigationTreeWindow;
        private bool _showTreeWindow;
        private readonly int _showTreeWindowWidth = 300;
        private readonly int _showTreeWindowHeight = 20;

        //使用快捷键打开窗口
        [MenuItem("程序工具/导航Navigation %#D")]
        public static void EditorNavigationWindow()
        {
            var window = GetWindow<NavigationWindow>();
            window.titleContent = new GUIContent("Navigation");
        }

        private bool _hasInit;
        private bool _repaintError;

        private void OnEnable()
        {
            // 在窗口启用时添加监听
            EditorApplication.playModeStateChanged += ReInit;
        }

        private void OnDisable()
        {
            // 在窗口禁用时移除监听
            EditorApplication.playModeStateChanged -= ReInit;
        }

        private void ReInit(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _hasInit = false;
                _repaintError = false;
            }
        }

        private void OnEnteredPlayModeDelayed()
        {
            Debug.Log("Entered Play Mode (Delayed)");
            // 在进入播放模式后的下一帧执行的逻辑
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns>是否已经初始化成功</returns>
        private bool Init()
        {
            if (_root == null)
            {
                _root = NavigationManager.Instance?.Root;
                return _hasInit;
            }

            if (_hasInit)
                return _hasInit;

            _hasInit = true;


            //初始化红点
            _circleRed = NavigationViewKit.CreateDot(8, 8, Color.red);
            _circleGray = NavigationViewKit.CreateDot(8, 8, Color.gray);

            //初始化必要数据
            _navigationTotalView = new NavigationTotalView();
            _navigationTotalViewElement = new VisualElement();
            _navigationTotalViewElement.Add(_navigationTotalView);
            rootVisualElement.Add(_navigationTotalViewElement);

            _navigationTreeWindow = new NavigationTreeWindow();

            //初始化Record记录监听
            NavigationFactory.Instance = new NavigationEditorFactory();

            //监听选中节点
            _navigationTotalView.onNodeSelected = node =>
            {
                if (node is NavigationContainerNodeView containerNode)
                    selectShowGroup = containerNode.Container;
            };

            //读取选择记录
            _showTreeWindow = EditorPrefs.GetBool("NavigationWindowShowTree", false);
            return _hasInit;
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Init()) return;
            if (_root == null) return;

            bool showTreeWindowTmp = GUILayout.Toggle(_showTreeWindow, "显示树形结构",
                GUILayout.Width(_showTreeWindowWidth), GUILayout.Height(_showTreeWindowHeight));
            if (showTreeWindowTmp != _showTreeWindow)
            {
                _showTreeWindow = showTreeWindowTmp;
                EditorPrefs.SetBool("NavigationWindowShowTree", _showTreeWindow);
            }


            NavigateContainer showGroup = selectShowGroup ?? _root.GetLastContainer();
            Rect containerRect = DrawGroup(showGroup);
            DrawAllOperateHistory();

            rootVisualElement.visible = !_showTreeWindow;
            if (_showTreeWindow)
                DrawTreeView(containerRect, _showTreeWindowWidth);
            else
                DrawTotalTreeView(containerRect, _showTreeWindowHeight);
        }

        private void OnDestroy()
        {
            //还原Record监听设置
            NavigationFactory.Instance = new NavigationFactory();
            EditorApplication.playModeStateChanged -= ReInit;
        }

        //绘制导航组树形结构
        private Rect DrawTotalTreeView(Rect lowerRect, float space)
        {
            //绘制树形结构
            Rect rect = new Rect(0, space, position.width - ALL_OPERATE_WIDTH,
                position.height - lowerRect.height - space);
            NavigationViewKit.DrawBorderedRect(rect, Color.gray, 1);
            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical();
            _navigationTotalViewElement.style.left = rect.x;
            _navigationTotalViewElement.style.top = rect.y;
            _navigationTotalViewElement.style.height = rect.height;
            _navigationTotalViewElement.style.width = rect.width;

            GUILayout.EndVertical();
            GUILayout.EndArea();
            return rect;
        }

        //绘制树形结构
        private Rect DrawTreeView(Rect lowerRect, float treeWindowRect)
        {
            Rect rect = new Rect(0, 0, position.width - ALL_OPERATE_WIDTH, position.height - lowerRect.height);
            NavigationViewKit.DrawBorderedRect(rect, Color.gray, 1);
            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical();

            _navigationTreeWindow.Refresh(_root);
            _navigationTreeWindow.OnGUI(rect, treeWindowRect);

            GUILayout.EndVertical();
            GUILayout.EndArea();
            return rect;
        }

        //绘制一个导航组
        private Rect DrawGroup(NavigateContainer container)
        {
            //设置整个显示区域
            int height = CalculateGroupHeight(container);
            //左下角对齐
            Rect rect = new Rect(0, position.height - height, position.width - ALL_OPERATE_WIDTH, height);
            GUILayout.BeginArea(rect);
            NavigationViewKit.DrawBorderedRect(rect, Color.gray, 1);

            //整体水平布局
            GUILayout.BeginHorizontal();
            float onePart = (position.width - ALL_OPERATE_WIDTH) / 10;

            //绘制Loader信息(垂直布局)
            GUILayout.BeginVertical("box", GUILayout.Width(onePart * 4), GUILayout.Height(height));
            GUILayout.Space(5);
            GUILayout.Label($" 导航组：{container.Name}", EditorStyles.boldLabel);
            GUILayout.Space(10);
            int index = 1;
            foreach (var loader in container.ForeachLoaders(TraversalOrder.Forward))
            {
                DrawLoader(loader, index);
                index++;
            }
            GUILayout.EndVertical();

            //绘制堆栈显示(垂直布局)
            GUILayout.BeginVertical("box", GUILayout.Width(onePart * 3), GUILayout.Height(height));
            DrawGroupStack(container);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            //绘制子分组信息(垂直布局)
            GUILayout.BeginVertical("box", GUILayout.Width(onePart * 3), GUILayout.Height(height * 0.5f));
            GUILayout.Label("子导航组:");
            // container.ForwardTraversal(childGroup =>
            // {
            //     if (childGroup.Parent != container) return false;
            //     DrawChildGroup(childGroup);
            //     return true;
            // }, false);
            foreach (var child in container.ForeachContainers(TraversalOrder.Forward, false))
            {
                if (container.Parent != container) break;
                DrawChildGroup(child);
            }

            GUILayout.EndVertical();

            //绘制风险(垂直布局)
            GUILayout.BeginVertical("box", GUILayout.Width(onePart * 3), GUILayout.Height(height * 0.5f));
            DrawGroupRisk(container);
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            return rect;
        }

        //计算一个导航组的高度
        private int CalculateGroupHeight(NavigateContainer container)
        {
            int minGroupHeight = 200;
            int oneHeight = 30;
            int childHeight = (container.ContainerCount - 1) * oneHeight + oneHeight;
            int loaderCount = container.Loaders.Count;
            int loaderHeight = loaderCount * oneHeight + oneHeight;
            return Math.Max(minGroupHeight, Math.Max(childHeight, loaderHeight));
        }

        //绘制一个导航组的加载器
        private void DrawLoader(NavigationLoader loader, int index)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            string loaderType = NavigationViewKit.GetBehaviourDes(loader);
            GUILayout.Label($"{loaderType}{index}:", EditorStyles.boldLabel, GUILayout.Width(40));

            //绘制一个小红点,来显示加载器是否可见
            GUIContent guiContent = NavigationViewKit.GetStateGUIContent(loader.CurrentState);
            GUILayout.Label(guiContent, GUILayout.Width(20), GUILayout.Height(20));

            //点击跳转到面板
            GUIStyle titleStyle = NavigationViewKit.GetLeftAlignedButtonStyle();
            if (GUILayout.Button(loader.Name, titleStyle, GUILayout.Width(250)))
                NavigationViewKit.SelectLoader(loader);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        //绘制一个导航组的子分组
        private void DrawChildGroup(NavigateContainer container)
        {
            if (GUILayout.Button(container.Name))
            {
                Debug.Log($"选中子导航组:{container.Name}");
            }
        }


        private Vector2 _containerStackScrollPos;

        //绘制一个导航组的堆栈
        private void DrawGroupStack(NavigateContainer container)
        {
            _containerStackScrollPos = GUILayout.BeginScrollView(_containerStackScrollPos);
            GUILayout.BeginVertical();
            foreach (var record in NavigationRecordMgr.Instance.Records)
            {
                NavigationBehaviour stackObj = record.operateObj;
                if (stackObj is NavigationLoader loader && loader.ParentContainer != container)
                    continue;
                if (stackObj is NavigateContainer stackGroup && stackGroup != container)
                    continue;
                DrawOneRecord(record);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        //绘制一个导航组的风险
        private void DrawGroupRisk(NavigateContainer container)
        {
            GUILayout.BeginVertical();
            if (!container.HasEntrance())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("导航组没有入口!!", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (container.Empty)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("导航组为空!!", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }


        private Vector2 _operateHistoryScrollPos;
        private string _searchHistoryText = "";
        private List<NavigationOperateRecordData> _records;

        //绘制操作历史
        private Rect DrawAllOperateHistory()
        {
            Rect rect = new Rect(position.width - ALL_OPERATE_WIDTH, 0, ALL_OPERATE_WIDTH, position.height);
            GUILayout.BeginArea(rect);
            NavigationViewKit.DrawBorderedRect(rect, Color.gray, 1);
            GUILayout.BeginVertical();
            GUILayout.Space(5);

            //绘制一个搜索框
            GUILayout.BeginHorizontal();
            GUILayout.Label($"搜索:", GUILayout.Width(35));
            string newSearchHistoryText =
                GUILayout.TextField(_searchHistoryText, GUILayout.Width(190));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("清空堆栈"))
                NavigationRecordMgr.Instance.Records.Clear();
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(newSearchHistoryText))
            {
                if (newSearchHistoryText != _searchHistoryText)
                {
                    string findLower = newSearchHistoryText.ToLower();
                    _records = NavigationRecordMgr.Instance.Records.FindAll(recordData =>
                        recordData.operateObjName.ToLower().Contains(findLower));
                }
            }
            else
                _records = NavigationRecordMgr.Instance.Records;

            _searchHistoryText = newSearchHistoryText;

            //绘制操作历史
            _operateHistoryScrollPos = GUILayout.BeginScrollView(_operateHistoryScrollPos,
                GUILayout.Width(ALL_OPERATE_WIDTH), GUILayout.Height(position.height));


            GUILayout.Space(20);

            foreach (var record in _records)
            {
                DrawOneRecord(record);
                GUILayout.Space(10);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUILayout.EndVertical();
            return rect;
        }

        //绘制一条操作记录
        private void DrawOneRecord(NavigationOperateRecordData record)
        {
            GUILayout.BeginVertical();

            string type = NavigationViewKit.GetBehaviourDes(record.operateObjType);
            string operateType = NavigationViewKit.GetStateDescribe(record.operateType);
            string showOperate = $"{type}：{record.operateObjName} => {operateType}";
            GUILayout.Label(showOperate);

            //只显示时分秒
            GUILayout.BeginHorizontal();
            GUILayout.Label($"时间:{record.operateTime:HH:mm:ss}");
            GUILayout.Space(10);
            GUILayout.Label($"帧:{record.frame}");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //显示堆栈
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("C#堆栈:", GUILayout.Width(ALL_OPERATE_WIDTH * 0.5f)))
                RecordStackWindow.CreateWindow(record.operateCSharpStack, $"{showOperate} ==> C#堆栈:");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
