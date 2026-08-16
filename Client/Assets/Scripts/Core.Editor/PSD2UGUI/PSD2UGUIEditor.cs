//*****************************************************************************
//Created By Liangc on 2019/6/3
//PSD转UGUI窗口绘制类
//@Description 集成了所有功能的调用接口
//*****************************************************************************

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using Object = System.Object;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD2UGUI窗口绘制类
    /// </summary>
    public class Psd2UguiEditor : EditorWindow
    {
        #region 局部变量

        //工具模式
        private enum ModeType
        {
            Create = 0, //创建模式
            Repair = 1 //修复模式
        }

        //当前模式
        private ModeType _currentMode = ModeType.Create;

        //当前选择的创建的PSD文件缩略图
        private Texture2D _choiceCreatePsdThumb;

        //当前选择的生成预制体节点
        private GameObject _choiceCreateObj;

        //当前选择创建的PSD文件路径
        private string _createPsdPath;

        //选择路径临时变量
        private string _createPsdPathTemp;

        //解析出的创建PSD文件信息
        private PsdNodeInfo _psdCreateNodeInfo;

        //解析出的PSD选择数据集合
        private List<Psd2UguiChoiceData> _choices;

        //当前选择的修复的PSD文件
        private Texture2D _choiceRepairPsdThumb;

        //当前选择修复Image的文件路径
        private string _repairFolderPath;

        //选择关联文件夹
        private string _imageFolderPath;

        //选择生成资源临时文件夹
        private string _imageFolderTempPath;

        //遍历文件夹路径数组
        private List<string> _folderPaths;

        //当前选择的修复的预制体节点
        private GameObject _choiceRepairObj;

        //当前选择修复的PSD文件路径
        private string _repairPsdPath;

        //当前选择路径临时变量
        private string _repairPsdPathTemp;

        //解析出的修复PSD文件信息
        private PsdNodeInfo _psdRepairNodeInfo;

        //修复选项(位置/图片/文字)
        public bool _repairRect = true, _repairImage = true, _repairText = true, _preciseMatching = true;

        //分辨率模式
        public int _resolutionIndex;

        //替换节点时是否删除元素
        public bool replaceDel = true;

        //是否隐藏的psd节点不导出
        public bool _hideLayerNotExport;

        //导出图片自动剪裁九宫格
        public bool sliceClip = false;

        //检测UI文件夹下所有prefab
        private const string FOLDER_PATH = "Assets\\GameRes\\UI\\General";

        // 用于记录已经检测过的Texture2D
        private Dictionary<Texture2D, bool> textureCheckedMap;
        private Dictionary<Texture2D, Color32> textureCheckedColorMap;

        //单例
        public static Psd2UguiEditor _instance;

        #endregion

        #region 绘制样式

        //logo显示GUIContent
        public GUIContent logoGUIContent;

        //title标题GUIStyle
        public GUIStyle titleTextGUIStyle;

        //logo显示GUIStyle
        public GUIStyle logoGUIStyle;

        //路径提示文字GUIStyle
        public GUIStyle pathTextGUIStyle;

        //标题文字位置
        private readonly Rect _explainPositionRect = new Rect(new Vector2(10, 30), new Vector2(580, 40));

        //logo图片位置
        private readonly Rect _logoPositionRect = new Rect(new Vector2(10, 600), new Vector2(580, 30));

        //功能框位置
        private readonly Rect _functionFramePosition = new Rect(new Vector2(10, 100), new Vector2(580, 490));

        //创建模式按钮位置
        private readonly Rect _createModePosition = new Rect(new Vector2(10, 100), new Vector2(290, 30));

        //修改模式按钮位置
        private readonly Rect _repairModePosition = new Rect(new Vector2(300, 100), new Vector2(290, 30));

        //------------------------------------创建模式----------------------------------------------
        //PSD文件选择边框
        private readonly Rect _choicePsdFrameRect = new Rect(new Vector2(10, 130), new Vector2(580, 150));

        //标准分辨率选择框
        private readonly Rect _standardChoiceRect = new Rect(new Vector2(130, 160), new Vector2(300, 20));

        //PSD文件选择提示
        private readonly Rect _hintChoicePsdTextRect = new Rect(new Vector2(130, 200), new Vector2(300, 20));

        //PSD文件选择框
        private readonly Rect _choicePsdFileFrameRect = new Rect(new Vector2(455, 135), new Vector2(70, 140));

        //导出图片功能边框
        private readonly Rect _exportImageFrameRect = new Rect(new Vector2(10, 280), new Vector2(580, 150));

        //图片导出文件夹路径
        private readonly Rect _exportImageDirePath = new Rect(new Vector2(130, 320), new Vector2(300, 20));

        //临时图片导出文件夹路径
        private readonly Rect _exportImageTempDirePath = new Rect(new Vector2(130, 340), new Vector2(300, 20));

        //导出图片提示
        private readonly Rect _exportImageHintTextRect = new Rect(new Vector2(130, 360), new Vector2(300, 20));

        //九宫格剪裁勾选框
        private readonly Rect _sliceToggleRect = new Rect(new Vector2(320, 360), new Vector2(100, 20));

        //导出图片按钮位置
        private readonly Rect _exportImageBtnRect = new Rect(new Vector2(440, 320), new Vector2(100, 60));

        //生成预制体功能边框
        private readonly Rect _createFrameRect = new Rect(new Vector2(10, 430), new Vector2(580, 160));

        //生成预制体拖动选择边框
        // private readonly Rect _createChoiceRect = new Rect(new Vector2(130, 515), new Vector2(295, 20));

        //生成预制体提示文字
        private readonly Rect _createHintTextRect = new Rect(new Vector2(130, 525), new Vector2(300, 20));

        //是否替换时删除选项位置
        private readonly Rect _replaceDelRect = new Rect(new Vector2(300, 525), new Vector2(130, 20));

        //是否隐藏的psd节点不导出
        private readonly Rect _hideLayerNotExportRect = new Rect(new Vector2(300, 545), new Vector2(130, 20));

        //生成预制体按钮位置
        private readonly Rect _createBtnRect = new Rect(new Vector2(440, 510), new Vector2(100, 60));
        //------------------------------------创建模式----------------------------------------------

        //------------------------------------生成模式----------------------------------------------
        //PSD文件选择边框
        private readonly Rect _choiceRepairFrameRect = new Rect(new Vector2(10, 130), new Vector2(580, 150));

        //PSD文件路径选择提示
        private readonly Rect _choicePsdFileHintTextRect = new Rect(new Vector2(130, 190), new Vector2(300, 20));

        //PSD文件选择框位置
        private readonly Rect _choicePsdFileRect = new Rect(new Vector2(455, 135), new Vector2(70, 140));

        //预制体修复模式边框
        private readonly Rect _repairModeFrameRect = new Rect(new Vector2(10, 280), new Vector2(580, 150));

        //预制体修复开关(图片/位置/文字)
        private readonly Rect _choiceImageRepairToggleRect = new Rect(new Vector2(100, 380), new Vector2(50, 20));
        private readonly Rect _choicePosRepairToggleRect = new Rect(new Vector2(220, 380), new Vector2(50, 20));
        private readonly Rect _choiceTextRepairToggleRect = new Rect(new Vector2(340, 380), new Vector2(50, 20));

        private readonly Rect _preciseMatchingToggleRect = new Rect(new Vector2(460, 380), new Vector2(80, 20));

        //预制体修复开关提示
        private readonly Rect _choiceToggleHintTextRect = new Rect(new Vector2(130, 340), new Vector2(300, 20));

        //预制体节点修复边框
        private readonly Rect _repairFrameRect = new Rect(new Vector2(10, 430), new Vector2(580, 160));

        //图片导出文件夹路径
        private readonly Rect _prefabRepairImageDirPathRect = new Rect(new Vector2(130, 485), new Vector2(300, 20));

        //预制体修改提示
        private readonly Rect _prefabRepairHintTextRect = new Rect(new Vector2(130, 515), new Vector2(300, 20));

        //预制体修改按钮位置
        private readonly Rect _prefabRepairChoiceRect = new Rect(new Vector2(270, 515), new Vector2(150, 20));

        //预制体节点选择
        private readonly Rect _repairBtnRect = new Rect(new Vector2(440, 480), new Vector2(100, 60));
        //------------------------------------生成模式----------------------------------------------

        #endregion

        #region 工具初始化

        [MenuItem("GameObject/UI制作/PSD转UGUI工具", false, priority = -100)]
        [MenuItem("Core/PSD转UGUI工具")]
        [MenuItem("美术工具/UI专用工具/PSD转UGUI工具", false, 1)]
        public static void OpenPsd2UguiWindow()
        {
            //初始化窗口
            Psd2UguiEditor animationPanel = EditorWindow.GetWindow(typeof(Psd2UguiEditor)) as Psd2UguiEditor;
            _instance = animationPanel;
            animationPanel.Init();
        }

        [MenuItem("GameObject/UI制作/保存导出预制", false, priority = -300)]
        public static void SavePsd2UguiPrefab()
        {
            bool isSave = true;
            if (!Psd2UguiTool.HasChinese(Selection.activeGameObject.name))
            {
                string ioMessage = $"检测到预制体命名似乎不像是美术导入的预制，请确认是否是美术导出的预制体资源？";
                isSave = EditorUtility.DisplayDialog("确认提示", ioMessage, "头铁导入", "我再看看");
            }

            if (isSave)
            {
                var path = Psd2UguiRule.EXPORT_IMAGE_UI_TEMP_PREFAB_PATH + "/" + Selection.activeGameObject.name;
                if (!Directory.Exists(path))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("创建目标目录失败：" + ex.Message);
                    }
                }

                var savePatch = path + "/" + Selection.activeGameObject.name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(Selection.activeGameObject, savePatch);
                var parent = Selection.activeGameObject.transform.parent;
                GameObject.DestroyImmediate(Selection.activeGameObject);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(savePatch);
                PrefabUtility.InstantiatePrefab(obj, parent);
            }
        }

        private bool _hasInit;

        private void Init()
        {
            if (_hasInit) return;
            _hasInit = true;
            _choices = new List<Psd2UguiChoiceData>();
            //图片生成文件夹
            _imageFolderPath = Psd2UguiRule.EXPORT_IMAGE_PATH;
            _imageFolderTempPath = Psd2UguiRule.EXPORT_IMAGE_TEMP_PATH;
            _repairFolderPath = Psd2UguiRule.EXPORT_IMAGE_PATH;
            //遍历文件夹路径
            _folderPaths = new List<string> { Psd2UguiRule.EXPORT_IMAGE_PATH, Psd2UguiRule.EXPORT_IMAGE_UI_BG_PATH };
            if (Directory.Exists(Psd2UguiRule.CONFIG_IMAGE_PATH))
            {
                DirectoryInfo[] configDir = (new DirectoryInfo(Psd2UguiRule.CONFIG_IMAGE_PATH)).GetDirectories();
                foreach (var dir in configDir)
                {
                    int index = dir.FullName.IndexOf("Assets", StringComparison.Ordinal);
                    if (index != 0)
                    {
                        _folderPaths.Add(dir.FullName.Remove(0, index).Replace('\\', '/'));
                    }
                }
            }

            // 递归遍历文件夹 UIResPool
            GetFolderPaths(Psd2UguiRule.EXPORT_IMAGE_PATH, _folderPaths);

            //加载Logo
            Texture2D logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Psd2UguiRule.LOGO_PATH);
            //初始化窗口名
            titleContent = new GUIContent("PSD转UGUI", logoTexture);
            //初始化窗口大小
            minSize = new Vector2(600, 640);
            maxSize = new Vector2(600, 640);
            //设置logo显示
            GUIContent logoGuiContent = new GUIContent("清理");
            logoGUIContent = logoGuiContent;
            //标题文字样式
            GUIStyle titleTextGuiStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
            titleTextGuiStyle.normal.textColor = new Color(1.0f, 160 / 255.0f, 50 / 255.0f);
            titleTextGUIStyle = titleTextGuiStyle;
            //路径提示文字样式
            GUIStyle pathTextGuiStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
            if (EditorGUIUtility.isProSkin)
            {
                pathTextGuiStyle.normal.textColor = Color.white;
            }

            pathTextGUIStyle = pathTextGuiStyle;
            //图片样式
            GUIStyle logoGuiStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
            logoGuiStyle.CalcScreenSize(new Vector2(128, 64));
            logoGUIStyle = logoGuiStyle;
        }

        // 传入文件夹路径，及列表， 递归遍历获取返回文件夹下的所有子文件夹路径列表
        private void GetFolderPaths(string folderPath, List<string> folderPaths)
        {
            // 目录不存在时直接返回，避免抛 DirectoryNotFoundException
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            DirectoryInfo[] dirs = (new DirectoryInfo(folderPath)).GetDirectories();
            foreach (var dir in dirs)
            {
                int index = dir.FullName.IndexOf("Assets", StringComparison.Ordinal);
                if (index != 0)
                {
                    folderPaths.Add(dir.FullName.Remove(0, index).Replace('\\', '/'));
                    GetFolderPaths(dir.FullName, folderPaths);
                }
            }
        }

        #endregion

        #region 界面绘制

        public void OnGUI()
        {
            if (Application.isPlaying)
            {
                _hasInit = false;
                return;
            }

            Init();

            DrawMainWindow();
            if (_currentMode == ModeType.Create)
                DrawCreatePrefabWindow();
            else
                DrawRepairPrefabWindow();
        }

        //绘制主功能区
        private void DrawMainWindow()
        {
            //绘制说明和Logo
            GUI.Box(_explainPositionRect, "PSD转UGUI", titleTextGUIStyle);
            GUI.Box(_logoPositionRect, logoGUIContent, logoGUIStyle);
            //清理按钮
            if (GUI.Button(_logoPositionRect, logoGUIContent))
                ClearRecord();
            // if (GUILayout.Button("纯色图清理"))
            //     ClearSolidColorImg();
            // if (GUILayout.Button("替换纯色图"))
            //     ReplaceSolidColorImg();

            //功能切换
            GUI.Box(_functionFramePosition, "");
            if (GUI.Button(_createModePosition, "创建模式"))
                _currentMode = ModeType.Create;
            if (GUI.Button(_repairModePosition, "修复模式"))
                _currentMode = ModeType.Repair;
        }

        //绘制创建预制体窗口
        private void DrawCreatePrefabWindow()
        {
            //PSD文件选择边框
            GUI.Box(_choicePsdFrameRect, "");
            //标准分辨率选择
            _resolutionIndex = EditorGUI.Popup(_standardChoiceRect, _resolutionIndex, Psd2UguiRule.RESOLUTION_CHOICE);
            //PSD选择提示
            GUI.Label(_hintChoicePsdTextRect, "①选择PSD文件==>", pathTextGUIStyle);
            DragPath(_choicePsdFileFrameRect, ref _createPsdPath,
                savePath => _choiceCreatePsdThumb = CreateThumb(savePath));
            _choiceCreatePsdThumb = (Texture2D)EditorGUI.ObjectField(_choicePsdFileFrameRect, _choiceCreatePsdThumb,
                typeof(Texture2D), true);
            _createPsdPathTemp = AssetDatabase.GetAssetPath(_choiceCreatePsdThumb);
            _createPsdPath = _createPsdPathTemp == "" ? _createPsdPath : _createPsdPathTemp;

            //导出图片边框
            GUI.Box(_exportImageFrameRect, "");
            //导出图片路径
            DragPath(_exportImageDirePath, ref _imageFolderPath);
            _imageFolderPath = GUI.TextField(_exportImageDirePath, _imageFolderPath);
            DragPath(_exportImageTempDirePath, ref _imageFolderTempPath);
            _imageFolderTempPath = GUI.TextField(_exportImageTempDirePath, _imageFolderTempPath);
            //点击导出图片提示
            GUI.Label(_exportImageHintTextRect, "②点击导出图片==>", pathTextGUIStyle);
            //自动九宫格剪裁
            sliceClip = GUI.Toggle(_sliceToggleRect, sliceClip, "自动剪裁九宫格?");
            //导出图片按钮
            if (GUI.Button(_exportImageBtnRect, "导出图片"))
                ExportImage();

            //生成节点
            GUI.Box(_createFrameRect, "");
            //选择路径提示
            GUI.Label(_createHintTextRect, "③选择节点生成预制体==>", pathTextGUIStyle);
            //替换时产出选项
            // replaceDel = GUI.Toggle(_replaceDelRect, replaceDel, "找到通用节点后删除?");
            _hideLayerNotExport = GUI.Toggle(_hideLayerNotExportRect, _hideLayerNotExport, "隐藏节点不导出?");
            //选择生成节点
            // _choiceCreateObj =
            //     FilterChoiceObj((GameObject) EditorGUI.ObjectField(_createChoiceRect, _choiceCreateObj,
            //         typeof(GameObject), true));
            //生成重写状态机按钮
            if (GUI.Button(_createBtnRect, "生成"))
                CreatePrefab(true);

            if (GUI.Button(new Rect(10, 560, 100, 20), "急救"))
                CreatePrefab();
        }

        //绘制预制体修复窗口
        private void DrawRepairPrefabWindow()
        {
            //PSD文件选择
            GUI.Box(_choiceRepairFrameRect, "");
            //点击导出图片提示
            GUI.Label(_choicePsdFileHintTextRect, "①选择PSD文件==>", pathTextGUIStyle);
            //PSD获取
            DragPath(_choicePsdFileRect, ref _repairPsdPath, savePath => _choiceRepairPsdThumb = CreateThumb(savePath));
            _choiceRepairPsdThumb =
                (Texture2D)EditorGUI.ObjectField(_choicePsdFileRect, _choiceRepairPsdThumb, typeof(Texture2D), true);
            _repairPsdPathTemp = AssetDatabase.GetAssetPath(_choiceRepairPsdThumb);
            _repairPsdPath = _repairPsdPathTemp == "" ? _repairPsdPath : _repairPsdPathTemp;

            //修复模式边框绘制
            GUI.Box(_repairModeFrameRect, "");
            //修复模式选择提示
            GUI.Label(_choiceToggleHintTextRect, "②选择预制体修复模式==>", pathTextGUIStyle);
            _repairImage = EditorGUI.ToggleLeft(_choiceImageRepairToggleRect, "图片", _repairImage);
            _repairRect = EditorGUI.ToggleLeft(_choicePosRepairToggleRect, "位置", _repairRect);
            _repairText = EditorGUI.ToggleLeft(_choiceTextRepairToggleRect, "文字", _repairText);
            _preciseMatching = EditorGUI.ToggleLeft(_preciseMatchingToggleRect, "精准匹配", _preciseMatching);


            //修复边框绘制
            GUI.Box(_repairFrameRect, "");
            //修复选择提示
            GUI.Label(_prefabRepairHintTextRect, "③选择修复节点==>", pathTextGUIStyle);
            //导出图片位置
            DragPath(_prefabRepairImageDirPathRect, ref _repairFolderPath);
            _repairFolderPath = GUI.TextField(_prefabRepairImageDirPathRect, _repairFolderPath);
            //选择生成节点
            _choiceRepairObj = FilterChoiceObj((GameObject)EditorGUI.ObjectField(_prefabRepairChoiceRect,
                _choiceRepairObj, typeof(GameObject), true));
            //点击生成按钮
            if (GUI.Button(_repairBtnRect, "修改"))
                RepairPrefab();
        }

        //导出图片
        private void ExportImage()
        {
            if (!IsPsdFile(_createPsdPath))
            {
                EditorUtility.DisplayDialog("PSD文件路径错误", "请先选择PSD文件再进行导出", "确认");
                return;
            }

            _psdCreateNodeInfo =
                Psd2UguiParse.Instance.PSDParseByPath(_createPsdPath, _imageFolderPath, _folderPaths, _choices);

            //打开选择面板
            Psd2UguiChoice.OpenChoiceWindows(_psdCreateNodeInfo, _choices, _imageFolderPath, _imageFolderTempPath,
                sliceClip, false, null,
                _createPsdPath, _folderPaths, (newPsdInfoNode) => { _psdCreateNodeInfo = newPsdInfoNode; });
        }

        //是否是PSD文件
        private bool IsPsdFile(string path)
        {
            Debug.Log(Path.GetExtension(path).ToUpper());
            Debug.Log(path.ToUpper());
            return Path.GetExtension(path).ToUpper().Contains(".PSD");
        }

        //生成预制体
        private void CreatePrefab(bool isNew = false)
        {
            if (_psdCreateNodeInfo == null)
            {
                EditorUtility.DisplayDialog("PSD信息错误", "PSD解析信息为空,点击'导出图片'重新解析", "确认");
                return;
            }

            if (isNew)
            {
                Psd2UguiStatistics.Reset(_createPsdPath);
                Json2Prefab.CreatePrefab(_createPsdPath, _hideLayerNotExport);
                Psd2UguiStatistics.Statistics();
                return;
            }

            Psd2UguiStatistics.Reset(_createPsdPath);

            if (_choiceCreateObj == null)
            {
                _choiceCreateObj = Psd2UguiPortShims.CreateBlankCanvas();
                if (_choiceCreateObj == null)
                {
                    EditorUtility.DisplayDialog("选择节点错误", "没有选中生成节点,请重新选择", "确认");
                    return;
                }

                GameObject go = null;
                var allObj = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var obj in allObj)
                {
                    if (obj.name == "UIRoot")
                    {
                        go = obj;
                        break;
                    }
                }

                if (go)
                {
                    go.SetActive(true);
                    _choiceCreateObj.transform.SetParent(go.transform, true);
                    RectTransform rect = _choiceCreateObj.GetComponent<RectTransform>();
                    rect.SetRectFull();
                }
            }

            StringBuilder warnInfo =
                PsdPrefabCreate.Instance.CreatePrefab(_psdCreateNodeInfo, _choiceCreateObj.transform);
            EditorSceneManager.MarkAllScenesDirty();
            if (warnInfo.Length != 0)
                EditorUtility.DisplayDialog("注意!", warnInfo.ToString(), "确认--Console控制台可查看详细信息");

            Psd2UguiStatistics.Statistics();
        }

        //修复预制体
        private void RepairPrefab()
        {
            if (!IsPsdFile(_repairPsdPath))
            {
                EditorUtility.DisplayDialog("PSD文件路径错误", "请先选择PSD文件再进行导出", "确认");
                return;
            }

            _psdRepairNodeInfo =
                Psd2UguiParse.Instance.PSDParseByPath(_repairPsdPath, _repairFolderPath, _folderPaths, _choices);

            if (_choiceRepairObj == null)
            {
                EditorUtility.DisplayDialog("选择节点错误", "没有选中生成节点,请重新选择", "确认");
                return;
            }

            //图片修复模式打开,打开选择面板,选好导出后执行预制体修复
            if (_repairImage)
                Psd2UguiChoice.OpenChoiceWindows(_psdCreateNodeInfo, _choices, _repairFolderPath, _imageFolderTempPath,
                    sliceClip, false,
                    () =>
                    {
                        Undo.RegisterCreatedObjectUndo(_choiceRepairObj, _choiceRepairObj.name);
                        PsdPrefabRepair.Instance.SetRepairOption(_repairRect, _repairImage, _repairText,
                            _preciseMatching);
                        PsdPrefabRepair.Instance.RepairPrefab(_psdRepairNodeInfo, _choiceRepairObj.transform);
                        EditorUtility.SetDirty(_choiceRepairObj);
                    }, _repairPsdPath, _folderPaths, null);
            //图片修复模式关闭,直接进行预制体修复
            else
            {
                Undo.RegisterCreatedObjectUndo(_choiceRepairObj, _choiceRepairObj.name);
                PsdPrefabRepair.Instance.SetRepairOption(_repairRect, _repairImage, _repairText, _preciseMatching);
                PsdPrefabRepair.Instance.RepairPrefab(_psdRepairNodeInfo, _choiceRepairObj.transform);
                EditorUtility.SetDirty(_choiceRepairObj);
            }
        }

        //清理记录
        private void ClearRecord()
        {
            _choiceCreatePsdThumb = null;
            _choiceCreateObj = null;
            _createPsdPath = null;
            _psdCreateNodeInfo = null;
            _choiceRepairPsdThumb = null;
            _choiceRepairObj = null;
            _repairPsdPath = null;
            _repairRect = true;
            _repairImage = true;
            _repairText = true;
            _psdRepairNodeInfo = null;
            _imageFolderPath = Psd2UguiRule.EXPORT_IMAGE_PATH;
            _repairFolderPath = Psd2UguiRule.EXPORT_IMAGE_PATH;

            EditorUtility.DisplayDialog("清空选择", "已清空选项", "确认");
        }

        //过滤选择的PSD文件
        private Texture2D FilterChoicePsd(Texture2D image)
        {
            if (!image)
                return null;

            string path = AssetDatabase.GetAssetPath(image);
            if (path.ToUpper().EndsWith(".PSD"))
                return image;
            EditorUtility.DisplayDialog("选择错误", "选择图片不是PSD文件", "重新选择");
            return null;
        }

        //过滤选择的GameObject
        private GameObject FilterChoiceObj(GameObject obj)
        {
            if (!obj)
                return null;

            if (!(obj.transform is RectTransform))
            {
                EditorUtility.DisplayDialog("选择节点错误", "请选择带Canvas的节点", "确认");
                return null;
            }

            string objPath = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(objPath))
                return obj;
            else
            {
                EditorUtility.DisplayDialog("选择错误", "请选择场景中的节点", "重新选择");
                return null;
            }
        }

        //拖动路径
        private void DragPath(Rect dragRect, ref string savePath, Action<string> endDragCb = null)
        {
            if (!dragRect.Contains(Event.current.mousePosition))
                return;

            //改变鼠标的外表
            if (Event.current.type == EventType.DragUpdated)
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

            if ((Event.current.type == EventType.DragExited)
                && (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0))
            {
                savePath = DragAndDrop.paths[0];
                endDragCb?.Invoke(savePath);
            }
        }

        //生成缩略图
        private Texture2D CreateThumb(string path)
        {
            PhotoshopFile.PsdFile psd = new PhotoshopFile.PsdFile(path, Encoding.Default);
            return PhotoshopFile.PSDEditorWindow.CreateTextureThumb(psd.BaseLayer);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        void ClearSolidColorImg()
        {
            string path;
            Texture2D t2d;
            string[] assets = AssetDatabase.FindAssets("t:Texture2D");
            int count = assets.Length;
            for (int i = 0; i < count; i++)
            {
                path = AssetDatabase.GUIDToAssetPath(assets[i]);
                t2d = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Texture2D)) as Texture2D;
                if (t2d == null || t2d.width == 0 || t2d.height == 0 || (t2d.width <= 2 && t2d.height <= 2))
                {
                    continue;
                }

                Color32[] colors = t2d.isReadable
                    ? t2d.GetPixels32()
                    : Psd2UguiChoice.ConvertTexture(t2d).GetPixels32();
                bool isDiff = false;
                foreach (var c in colors)
                {
                    if (!colors[0].Compare(c))
                    {
                        isDiff = true;
                        break;
                    }
                }

                if (!isDiff)
                {
                    SaveNTexture(colors[0], path);
                }
            }
        }

        void SaveNTexture(Color32 color, string path)
        {
            Texture2D t2d = new Texture2D(2, 2);
            t2d.SetPixels32(new[] { color, color, color, color });
            path = Application.dataPath.Replace("Assets", "") + path;
            byte[] textureByte = t2d.EncodeToPNG();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (FileStream f = new FileStream(path, FileMode.Create))
            {
                f.Write(textureByte);
                f.Flush();
            }
        }

        void ReplaceSolidColorImg()
        {
            DateTime startTime = DateTime.Now; // 开始时间
            textureCheckedMap = new Dictionary<Texture2D, bool>();
            textureCheckedColorMap = new Dictionary<Texture2D, Color32>();

            string[] prefabFiles = AssetDatabase.FindAssets("t:Prefab", new[] { FOLDER_PATH });
            GameObject[] prefabs = new GameObject[prefabFiles.Length];
            for (int i = 0; i < prefabFiles.Length; i++)
            {
                string guid = prefabFiles[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            int count = 0;
            StringBuilder str = new StringBuilder();
            ;
            foreach (var prefab in prefabs)
            {
                bool isAdd = false;
                //获取prefab下所有Image的节点
                var imageComponents = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var imageComponent in imageComponents)
                {
                    if (imageComponent.sprite != null)
                    {
                        Texture2D texture = (Texture2D)imageComponent.sprite.texture;
                        bool result;
                        //之前是否已经检测过该图片了
                        if (!textureCheckedMap.ContainsKey(texture))
                        {
                            result = CheckIsSolidColor(texture);
                            textureCheckedMap[texture] = result;
                        }
                        else
                            result = textureCheckedMap[texture];

                        //如果是纯色图,那么就去掉图片，修改颜色
                        if (result)
                        {
                            Color32 color = textureCheckedColorMap[texture];
                            imageComponent.color = color;
                            imageComponent.sprite = null;
                            if (!isAdd)
                            {
                                str.Append("Prefab Name: " + prefab.name + " -->  [ " + imageComponent.name);
                            }
                            else
                            {
                                str.Append("  |  " + imageComponent.name);
                            }

                            // Debug.Log("Prefab Name: " + prefab.name);
                            // Debug.Log("imageComponent Name: " + imageComponent.name);
                            // Debug.Log("Image Component Texture: " + texture.name);
                            isAdd = true;
                        }
                    }
                }

                if (isAdd)
                {
                    count += 1;
                    PrefabUtility.SavePrefabAsset(prefab);
                    str.Append(" ]" + Environment.NewLine + Environment.NewLine);
                }
            }

            TimeSpan elapsedTime = DateTime.Now - startTime; // 计算时间差
            Debug.Log("修复执行时间：" + elapsedTime.TotalMilliseconds + "(ms)");
            Debug.Log("修改的预制体数量：" + count);
            Debug.Log(str.ToString());
        }

        bool CheckIsSolidColor(Texture2D t2d)
        {
            if (t2d == null || t2d.width == 0 || t2d.height == 0)
                return false;

            Color32[] colors = t2d.isReadable
                ? t2d.GetPixels32()
                : Psd2UguiChoice.ConvertTexture(t2d).GetPixels32();
            foreach (var c in colors)
            {
                if (!colors[0].Compare(c))
                    return false;
            }

            textureCheckedColorMap[t2d] = colors[0];
            return true;
        }

        #endregion
    }
}