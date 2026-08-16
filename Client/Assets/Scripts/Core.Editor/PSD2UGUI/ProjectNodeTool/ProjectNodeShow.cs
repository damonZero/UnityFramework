//************************************************************************
//Create by Liangc on 2021/4/9
//
//@Description  项目通用节点展示类
//************************************************************************

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Package.PSD2UGUI
{
    public class ProjectNodeShow : EditorWindow
    {
        //预制体节点查找根目录
        private const string NODE_ROOT_PATH = "Assets/GameRes/UI/General";

        //特殊目录路径
        private readonly string[] _nodeSpecialPath = new string[]
        {
            // "Assets/GameRes/UI/FunctionUI/RedDot",
            // "Assets/GameRes/UI/FunctionUI/Base",
            // "Assets/GameRes/UI/FunctionUI/Award",
            // "Assets/GameRes/UI/FunctionUI/PSDCommon"  // 原 P33 特殊目录, 本工程无对应
        };

        //文件夹通用节点查找标识
        private const string FIND_SIGN = "Common";

        //通用节点数据
        private ProjectNodeInfoFolder _rootFolder;
        private Dictionary<ProjectNodeInfo, ProjectNodeInfoFolder> _searchCollect;

        //窗口绘制rect
        private Rect NodePreviewRect => new Rect(210, 30, position.width - 210, position.height - 30);

        //窗口绘制GUIStyle
        private GUIStyle _treeViewStyle;
        private GUIStyle _nodePreviewStyle;
        private GUIStyle _nodeStyle;
        private GUIStyle _nodeCheck;
        private GUIStyle _searchButtonStyle;
        private MethodInfo _searchInfo;

        //窗口绘制临时变量
        private bool _hasInit;
        private Vector2 _nodeTreeScrollPos;
        private Vector2 _nodePreviewScrollPos;
        private string _searchKeyword;
        private int _drawNodeCount;
        private bool _hierarchyCb;
        private ProjectNodeDetailShow _stayWindow;
        private ProjectNodeInfo _checkNode;
        private bool _searchName = true;
        private bool _searchAut = true;
        private bool _searchDes = true;


        [MenuItem("GameObject/通用UI面板", false, priority = -40)]
        public static EditorWindow OpenShowWindow()
        {
            ProjectNodeShow window = GetWindow<ProjectNodeShow>();
            window.titleContent = new GUIContent("通用UI面板");
            window.maxSize = new Vector2(1500, 800);
            window.minSize = new Vector2(855, 400);
            window.maximized = true;
            return window;
        }

        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                _hasInit = false;
                return;
            }

            InitData();

            //整体水平布局
            EditorGUILayout.BeginHorizontal();

            //绘制节点,垂直布局
            _nodeTreeScrollPos = EditorGUILayout.BeginScrollView(_nodeTreeScrollPos, _treeViewStyle);
            DrawNodeFolder(_rootFolder, 20, 0);
            EditorGUILayout.EndScrollView();

            // 右侧节点预览框垂直布局
            EditorGUILayout.BeginVertical();
            //搜索框和搜索按钮的水平布局
            EditorGUILayout.BeginHorizontal();
            string keywordTmp = DrawSearchField(_searchKeyword);
            if (_searchKeyword != keywordTmp || GUILayout.Button("搜索", _searchButtonStyle))
            {
                _searchKeyword = keywordTmp;
                _searchCollect.Clear();
                _rootFolder.SearchNode(_searchCollect,
                    _searchKeyword, _searchName, _searchAut, _searchDes);
            }

            _searchName = GUILayout.Toggle(_searchName, "名字");
            _searchAut = GUILayout.Toggle(_searchAut, "作者");
            _searchDes = GUILayout.Toggle(_searchDes, "描述");

            //规范跳转
            if (GUILayout.Button("公共组件文档", _searchButtonStyle))
                Application.OpenURL("https://jzyxgames.feishu.cn/wiki/CJLCwH2Y5ifFPWkPqYbcZIG0nFd?sheet=vJy3kB");

            EditorGUILayout.EndHorizontal();

            // 绘制预览图
            GUILayout.BeginArea(NodePreviewRect);
            _nodePreviewScrollPos = GUILayout.BeginScrollView(_nodePreviewScrollPos, _nodePreviewStyle);

            //节点预览区域,总体垂直布局,每个节点根据节点行列数选择开始或结束水平布局
            GUILayout.BeginVertical();
            if (!string.IsNullOrEmpty(_searchKeyword))
            {
                GUILayout.BeginHorizontal();
                DrawNodePreview(_searchCollect, NodePreviewRect);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                _drawNodeCount = 1;
                DrawNodeFolderPreview(_rootFolder, ref _drawNodeCount, NodePreviewRect);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();


            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void InitData()
        {
            if (_hasInit) return;
            _hasInit = true;
            //初始化样式
            _treeViewStyle = new GUIStyle(GUI.skin.box)
            {
                fixedWidth = 200,
                stretchWidth = true
            };
            _nodePreviewStyle = new GUIStyle(GUI.skin.box)
            {
                stretchWidth = true
            };
            GUIStyle skin = GUI.skin.box;
            skin.active.textColor = Color.red;
            _nodeStyle = new GUIStyle(skin)
            {
                alignment = TextAnchor.LowerCenter,
                imagePosition = ImagePosition.ImageAbove,
                fixedWidth = 120,
                fixedHeight = 120
            };
            _nodeCheck = new GUIStyle(skin)
            {
                alignment = TextAnchor.LowerCenter,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold,
                fixedWidth = 120,
                fixedHeight = 120
            };
            _searchButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 100
            };
            _searchInfo = typeof(EditorGUILayout).GetMethod("ToolbarSearchField",
                BindingFlags.NonPublic | BindingFlags.Static, null,
                new[] {typeof(string), typeof(GUILayoutOption[])}, null);

            //初始化数据
            _searchCollect = new Dictionary<ProjectNodeInfo, ProjectNodeInfoFolder>();
            _rootFolder = new ProjectNodeInfoFolder(null, new DirectoryInfo(NODE_ROOT_PATH));
            FindAllNode(_rootFolder);
            _rootFolder.isOpen = true;
        }

        //绘制节点文件夹视图
        private void DrawNodeFolder(ProjectNodeInfoFolder folder, float width, int layer)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(width * layer);
            folder.isOpen = EditorGUILayout.Foldout(folder.isOpen, folder.directoryInfo.Name);
            GUILayout.EndHorizontal();

            if (!folder.isOpen) return;
            foreach (var childFolder in folder.childFolders)
            {
                DrawNodeFolder(childFolder, width, layer + 1);
            }

            GUILayout.BeginVertical();
            foreach (var childNode in folder.childNodes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(width * (layer + 1));
                if (childNode == _checkNode)
                {
                    EditorGUILayout.BeginVertical("button");
                    EditorGUILayout.ObjectField(childNode.prefabObj, typeof(GameObject), true);
                    EditorGUILayout.EndVertical();
                }
                else
                    EditorGUILayout.ObjectField(childNode.prefabObj, typeof(GameObject), true);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        //计算节点Rect
        private Rect CalculateNodeRect(int drawCount,
            out int row, out int column, Rect nodePreview)
        {
            int maxColumn = (int) (nodePreview.width / _nodeStyle.fixedWidth);
            maxColumn = maxColumn == 0 ? 1 : maxColumn;
            row = Mathf.CeilToInt((float) drawCount / maxColumn);
            column = drawCount % maxColumn;
            column = column == 0 ? maxColumn : column;
            float x = (column - 1) * _nodeStyle.fixedWidth;
            float y = (row - 1) * _nodeStyle.fixedHeight;
            return new Rect(x, y, _nodeStyle.fixedWidth, _nodeStyle.fixedHeight);
        }

        //绘制节点预览视图
        private void DrawNodeFolderPreview(ProjectNodeInfoFolder folder, ref int drawCount, Rect nodePreview)
        {
            foreach (var childNode in folder.childNodes)
            {
                Rect retRc = CalculateNodeRect(drawCount,
                    out var curRow, out var curColumn, nodePreview);
                //行第一个元素开始排列
                if (curRow != 1 && curColumn == 1)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                DrawNode(retRc, childNode, folder);
                drawCount++;
            }

            foreach (var childFolder in folder.childFolders)
            {
                DrawNodeFolderPreview(childFolder, ref drawCount, nodePreview);
            }
        }


        //绘制节点预览视图
        private void DrawNodePreview(Dictionary<ProjectNodeInfo, ProjectNodeInfoFolder> collect, Rect nodePreview)
        {
            int drawCount = 1;
            foreach (var kv in collect)
            {
                Rect retRc = CalculateNodeRect(drawCount,
                    out var curRow, out var curColumn, nodePreview);
                //行第一个元素开始排列
                if (curRow != 1 && curColumn == 1)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                DrawNode(retRc, kv.Key, kv.Value);
                drawCount++;
            }
        }

        //绘制节点
        private void DrawNode(Rect rc, ProjectNodeInfo nodeInfo, ProjectNodeInfoFolder folder)
        {
            //绘制节点
            GUILayout.Label(nodeInfo.content,
                nodeInfo == _checkNode ? _nodeCheck : _nodeStyle);

            //鼠标位置判断
            if (rc.Contains(Event.current.mousePosition))
            {
                //点击标记
                if (Event.current.type == EventType.MouseUp)
                {
                    _rootFolder.OpenOrClose(false);
                    ProjectNodeInfoFolder.OpenParent(folder);
                    _checkNode = nodeInfo;
                    StayNode(_checkNode);
                }
            }

            //执行拖动
            if (Event.current.type == EventType.MouseDrag && rc.Contains(Event.current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] {nodeInfo.prefabObj};
                DragAndDrop.StartDrag("");
            }

            //拖动到Hierarchy面板执行替换
            if (Event.current.type == EventType.DragExited)
            {
                EditorApplication.hierarchyWindowItemOnGUI = (instanceId, selectionRect) =>
                {
                    if (Event.current.type != EventType.DragExited) return;
                    if (!_hierarchyCb)
                    {
                        var replaceObj = Selection.activeGameObject;
                        if (!replaceObj) return;
                        replaceObj.transform.localPosition = Vector3.zero;
                        _hierarchyCb = true;
                    }
                    else
                    {
                        _hierarchyCb = false;
                        EditorApplication.hierarchyWindowItemOnGUI = null;
                    }
                };
            }
        }

        private void OnDestroy()
        {
            if (_stayWindow)
                _stayWindow.Close();
        }

        //节点悬停 (原依赖 EditorCoroutines 包, 移植后改为同步)
        private void StayNode(ProjectNodeInfo stayNode)
        {
            float x = position.position.x + position.width;
            float y = position.position.y;
            Vector2 stayPos = new Vector2(x, y);
            if (_stayWindow)
                _stayWindow.Init(stayNode, stayPos);
            else
                _stayWindow = ProjectNodeDetailShow.Show(stayNode, stayPos);
        }

        //绘制搜索框
        private string DrawSearchField(string input, params GUILayoutOption[] options)
        {
            if (_searchInfo == null) return default;
            return (string) _searchInfo.Invoke(null, new object[] {input, options});
        }

        //查找文件夹
        private void FindFolder(string sign, ProjectNodeInfoFolder folder)
        {
            if (folder?.directoryInfo == null || !Directory.Exists(folder.directoryInfo.FullName))
                return;

            DirectoryInfo[] childFolders = folder.directoryInfo.GetDirectories();
            FileInfo[] childFiles = folder.directoryInfo.GetFiles();
            //sign标记查找只生效一次,即在目录下找到的第一个sign标记目录
            if (sign != null)
            {
                foreach (var childFolder in childFolders)
                {
                    Debug.LogError("childFolder.Name=" + childFolder.Name);
                    if (childFolder.Name != sign) continue;
                    ProjectNodeInfoFolder nodeInfoFolder = new ProjectNodeInfoFolder(null, childFolder);
                    FindFolder(null, nodeInfoFolder);
                    if (nodeInfoFolder.Empty) continue;
                    nodeInfoFolder.parentFolder = folder;
                    folder.childFolders.Add(nodeInfoFolder);
                }
            }
            else
            {
                foreach (var childFile in childFiles)
                {
                    var fileInfo = childFile;
                    if (childFile.Extension != ".prefab") continue;
                    //try一下,加载出来的节点可能报错
                    try
                    {
                        folder.childNodes.Add(ProjectNodeTool.ParseNodeInfo(fileInfo));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                foreach (var childFolder in childFolders)
                {
                    var nodeInfoFolder = new ProjectNodeInfoFolder(null, childFolder);
                    FindFolder(null, nodeInfoFolder);
                    if (nodeInfoFolder.Empty) continue;
                    nodeInfoFolder.parentFolder = folder;
                    folder.childFolders.Add(nodeInfoFolder);
                }
            }
        }

        //查找出所有预制体节点 (原依赖 EditorCoroutines 包, 移植后改为同步)
        private void FindAllNode(ProjectNodeInfoFolder rootFolder)
        {
            //查找特殊目录
            foreach (var path in _nodeSpecialPath)
            {
                var directoryInfo = new DirectoryInfo(path);
                var findTemp = new ProjectNodeInfoFolder(null, directoryInfo);
                FindFolder(null, findTemp);
                if (findTemp.Empty) continue;
                findTemp.parentFolder = rootFolder;
                rootFolder.childFolders.Add(findTemp);
            }

            //查找根目录
            if (!Directory.Exists(NODE_ROOT_PATH))
                return;
            var rootFolders = new DirectoryInfo(NODE_ROOT_PATH).GetDirectories();
            foreach (var folder in rootFolders)
            {
                var findTemp = new ProjectNodeInfoFolder(null, folder);
                FindFolder(FIND_SIGN, findTemp);
                if (findTemp.Empty) continue;
                findTemp.parentFolder = rootFolder;
                rootFolder.childFolders.Add(findTemp);
            }
        }
    }
}