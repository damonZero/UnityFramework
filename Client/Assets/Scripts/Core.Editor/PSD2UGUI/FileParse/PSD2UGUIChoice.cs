//*****************************************************************************
//Created By Liangc on 2019/8/29
//PSD文件解析类
//@Description 在Editor下PSD导出选择导出类
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhotoshopFile;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class Psd2UguiChoice : EditorWindow
    {
        //滑动位置缓存
        private Vector2 _scrollV2;

        //图片显示风格
        private GUIStyle _imageStyle;

        //选中图片显示风格
        private GUIStyle _hintStyle;

        //警告文字风格
        private GUIStyle _warnTextStyle;

        //Psd文件信息
        private PsdNodeInfo _psdCreateNodeInfo;

        //绘制选择PsdData列表
        private List<Psd2UguiChoiceData> _choicePsdData;

        //窗口GUI
        private GUIContent _guiTitle;

        //导出路径
        private string _directoryPath;

        //临时图片导出路径
        private string _directoryTempPath;

        //九宫格剪裁
        private bool _sliceClip;

        //导出后的回调
        private Action _exportCb;

        //导出提示
        private string _exportHint;

        //导出警告
        private bool _exportWarn;

        //隐藏相同图片的标记
        private bool _hideSamePic;

        //psd文件 方便修改图片名后写回PSD
        private PsdFile _psd;

        //psd文件路径
        private string _psdPath;

        //psd文件中layer名字映射
        private Dictionary<string, string> _psdLayerNameDic;

        //名字缓存映射
        private Dictionary<string, string> _nameTempDic;

        //修改名字标记
        private Dictionary<int, string> _modifyNameDic;

        //UI文件夹目录列表
        private List<string> _folderPaths;

        //PSD文件修改后的回调，用来重新刷新生成预制体的数据
        private Action<PsdNodeInfo> _psdChangedCallBack;

        //按钮宽度
        private const int BUTTON_WIDTH = 80;

        //按钮高度
        private const int BUTTON_HEIGHT = 20;

        private bool _defaultCreate;

        private enum NameModifyMode
        {
            SingleMode=1,
            BatchMode=2,
        }
        /// <summary>
        /// 批量修改字符串标识
        /// </summary>
        private  const string BATCH_MODIFY_STR = "batch_modify_flag";
        private  const string SINGLE_MODIFY_STR = "single_modify_flag";

        /// <summary>
        /// 打开选择窗口
        /// </summary>
        /// <param name="psdCreateNodeInfo"></param>
        /// <param name="choicePsdData"></param>
        /// <param name="directoryPath"></param>
        /// <param name="directoryTempPath"></param>
        /// <param name="sliceClip"></param>
        /// <param name="defaultCreate"></param>
        /// <param name="exportCb"></param>
        /// <param name="psdPath"></param>  psd文件路径
        /// <param name="folderPaths"></param>
        /// <param name="psdChangedCallBack"></param>
        public static void OpenChoiceWindows(PsdNodeInfo psdCreateNodeInfo ,List<Psd2UguiChoiceData> choicePsdData, string directoryPath,
            string directoryTempPath, bool sliceClip, bool defaultCreate, Action exportCb, string psdPath,
            List<string> folderPaths, Action<PsdNodeInfo> psdChangedCallBack)
        {
            Psd2UguiChoice choiceWindows = GetWindow(typeof(Psd2UguiChoice)) as Psd2UguiChoice;

            //GUIStyle设置
            choiceWindows._guiTitle = new GUIContent {text = "选择导入覆盖的图片"};
            choiceWindows.titleContent = choiceWindows._guiTitle;
            choiceWindows._imageStyle = GetGuiStyle();
            choiceWindows._hintStyle = GetHintGuiStyle();
            choiceWindows._warnTextStyle = new GUIStyle();
            choiceWindows._warnTextStyle.normal.textColor = Color.red;
            choiceWindows._warnTextStyle.fixedWidth = 40;

            choiceWindows.minSize = new Vector2(800, 800);
            choiceWindows._exportHint = "导出图片 + 建立索引 = 必须点一下!";

            //数据赋值
            choiceWindows._psdCreateNodeInfo = psdCreateNodeInfo;
            choiceWindows._choicePsdData = choicePsdData;
            choiceWindows._directoryPath = directoryPath;
            choiceWindows._directoryTempPath = directoryTempPath;
            choiceWindows._sliceClip = sliceClip;
            choiceWindows._exportCb = exportCb;
            choiceWindows._psdChangedCallBack = psdChangedCallBack;

            choiceWindows._hideSamePic = false;
            choiceWindows._defaultCreate = defaultCreate;

            //初始状态设置
            InitDatas(choiceWindows._choicePsdData, defaultCreate, sliceClip);
            TrimChoiceData(choiceWindows._choicePsdData);


            choiceWindows._folderPaths = folderPaths;
            choiceWindows._psdPath = psdPath;
            choiceWindows._psd = new PsdFile(psdPath, Encoding.Default);
            // 初始化跟改名有关的数据
            choiceWindows.InitLayerDic();
        }

        //导入图片绘制
        private readonly GUIContent _importContent = new GUIContent();

        //导出图片绘制
        private readonly GUIContent _exportContent = new GUIContent();

        public void OnGUI()
        {
            if (_choicePsdData == null)
                return;

            int choiceLength = _choicePsdData.Count;

            //批量覆盖状态修改
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全选覆盖"))
            {
                ChangeChoiceData(_choicePsdData, true);
            }

            if (GUILayout.Button("取消全选"))
            {
                ChangeChoiceData(_choicePsdData, false);
            }

            GUILayout.EndHorizontal();
            _hideSamePic = GUILayout.Toggle(_hideSamePic, "隐藏完全相同的图片（谨慎使用）");

            //文件提示
            GUILayout.BeginHorizontal();
            GUILayout.Label("导入文件");
            GUILayout.Label("项目文件");
            GUILayout.EndHorizontal();

            //绘制所有数据
            GUILayout.BeginVertical();

            //绘制拖动条
            _exportWarn = false;
            var nameInvalid = false;
            var invalidnName = "";
            _scrollV2 = GUILayout.BeginScrollView(_scrollV2);
            for (int i = 0; i < choiceLength; i++)
            {
                // 纵向的间隔
                float vSpace = 15;
                Psd2UguiChoiceData psdData = _choicePsdData[i];
                if (!psdData.isShow) continue;
                var newImg = psdData.isSlice ? psdData.sliceImage : psdData.originalImage;
                // 如果隐藏相同图片的话就跳过
                if (!psdData.isCreate && _hideSamePic && CheckSamePic(newImg, psdData))
                {
                    continue;
                }

                string layerName = psdData.node.layer.Name;

                //绘制单个数据
                GUILayout.BeginHorizontal();
                //绘制导入图片
                _importContent.text =
                    $"{layerName} [{newImg.width}×{newImg.height}]";
                _importContent.image = newImg;

                // GUILayout.BeginVertical();
                GUILayout.Box(_importContent, _imageStyle);

                // 图片改名功能
                if (!_nameTempDic.ContainsKey(layerName))
                {
                    _nameTempDic.Add(layerName, layerName);
                }

                if (!_modifyNameDic.ContainsKey(psdData.layerIndex))
                {
                    _modifyNameDic.Add(psdData.layerIndex, String.Empty);
                }

                Rect boxRect = GUILayoutUtility.GetLastRect();
                // 改名按钮和编辑框
                Rect btnRect = new Rect(boxRect.x, boxRect.y + boxRect.height + 5, BUTTON_WIDTH, BUTTON_HEIGHT);
                Rect btnBatchRect = new Rect(boxRect.x+100, boxRect.y + boxRect.height + 5, BUTTON_WIDTH, BUTTON_HEIGHT);
                Rect btnPreviewRect = new Rect(boxRect.x+200, boxRect.y + boxRect.height + 5, BUTTON_WIDTH, BUTTON_HEIGHT);
                // 名字编辑框
                Rect textFieldRect = new Rect(btnRect.x, btnRect.y + btnRect.height + 5, boxRect.width , btnRect.height);
                // 取消按钮
                Rect cancelBtnRect = new Rect(btnRect.x + btnRect.width + 5, btnRect.y, btnRect.width, btnRect.height);

                vSpace += btnRect.height;

                if (_modifyNameDic[psdData.layerIndex] != string.Empty)
                {
                    _nameTempDic[layerName] = GUI.TextField(textFieldRect, _nameTempDic[layerName]);
                    if (GUI.Button(btnRect, "确定"))
                    {
                        //相同名称不做处理
                        if (_nameTempDic[layerName].Equals(layerName))
                        {
                            _modifyNameDic[psdData.layerIndex] = string.Empty;
                        }
                        else
                        {
                            ModifyName(psdData, layerName);
                            _nameTempDic[layerName] = layerName;
                        }
                    }

                    if (GUI.Button(cancelBtnRect, "取消"))
                    {
                        _modifyNameDic[psdData.layerIndex] = string.Empty;
                        _nameTempDic[layerName] = layerName;
                    }
                    vSpace += textFieldRect.height;
                }
                else
                {
                    int sameCount = CheckSameNameCount(layerName);
                    if (GUI.Button(btnRect, "改名"))
                    {
                        _modifyNameDic[psdData.layerIndex] = SINGLE_MODIFY_STR;
                    }
                    //只有多个同名的才会显示
                    if (sameCount > 1)
                    {
                        if (GUI.Button(btnBatchRect, "批量修改"))
                        {
                            _modifyNameDic[psdData.layerIndex] = BATCH_MODIFY_STR;
                        }

                        if (GUI.Button(btnPreviewRect, "同名预览"))
                        {
                            Psd2UguiModifyListShow.ShowWindow(_choicePsdData, layerName);
                        }
                    }
                }

                // if (psdData.originalImage != null && psdData.oldImage != null)
                // {
                //     var resizeOldImg = Psd2UguiTool.ResizeTexture(psdData.originalImage, psdData.oldImage.width, psdData.oldImage.height);
                //     var oldImageReadable = Psd2UguiTool.ReadProImagePixels(psdData.oldImage);
                //     GUILayout.Label("相似度:" + Psd2UguiTool.CompareTextures(resizeOldImg, oldImageReadable).ToString());
                // }
                // CompareTextures

                //中文命名+空格提示,比较按钮
                GUILayout.Space(100f);
                GUILayout.BeginVertical();

                if (Psd2UguiTool.HasSpace(layerName) ||
                    (psdData.oldImage != null && Psd2UguiTool.HasSpace(psdData.oldImage.name)))
                {
                    GUILayout.Label("包含空格图片!!!", _warnTextStyle);
                    if (psdData.isCreate)
                        _exportWarn = true;
                }


                //检查名字中是否包含一些禁用的符号
                if (Psd2UguiTool.CheckNameInValid(layerName))
                {
                    GUILayout.Label("包含禁用符号!!!", _warnTextStyle);
                    if (psdData.isCreate)
                    {
                        _exportWarn = true;
                        nameInvalid = true;
                        invalidnName = layerName;
                    }

                }

                //分辨率超出提示
                if (ExceedResolution(psdData.originalImage))
                {
                    GUILayout.Label("分辨率超过\n750*1624!!!", _warnTextStyle);
                    if (psdData.isCreate)
                        _exportWarn = true;
                }

                if (GUILayout.Button("比较差异", new[] {GUILayout.Width(BUTTON_WIDTH)}))
                    Psd2UguiDiffShow.InitShow(_choicePsdData, i);
                if (psdData.oldImage)
                {
                    if (GUILayout.Button("查找引用", new[] {GUILayout.Width(BUTTON_WIDTH)}))
                        Psd2UguiImageReference.OpenWindow(psdData.oldImagePath);
                }

                if (psdData.oldImage)
                {
                    if (GUILayout.Button("选中旧图片", new[] {GUILayout.Width(BUTTON_WIDTH)}))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture>(psdData.oldImagePath);
                    }
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("裁剪：", new[] {GUILayout.Width(40)});
                psdData.SetSlice(GUILayout.Toggle(psdData.isSlice, "", new[] {GUILayout.Width(40)}));

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                //绘制已有图片
                bool isSlice = false;
                if (psdData.oldImage)
                {
                    TextureImporter importer = AssetImporter.GetAtPath(psdData.oldImagePath) as TextureImporter;
                    if (importer)
                    {
                        //isSlice = importer.spriteBorder.Equals(Vector4.zero);//存在精度问题
                        isSlice = importer.spriteBorder.x >= 1 || importer.spriteBorder.y >= 1 ||
                                  importer.spriteBorder.z >= 1 || importer.spriteBorder.w >= 1;
                    }
                }
                _exportContent.text = psdData.oldImage != null
                    ? $"{psdData.oldImage.name} [{psdData.oldImage.width}×{psdData.oldImage.height}]" : null;
                if (isSlice)
                {
                    _exportContent.text = _exportContent.text + "【九宫格】";
                }
                _exportContent.image = psdData.oldImage;
                GUILayout.Box(_exportContent, psdData.isCreate ? _hintStyle : _imageStyle);
                GUILayout.BeginVertical();
                //绘制导出选择勾选
                psdData.isCreate = GUILayout.Toggle(psdData.isCreate, "",
                    new[] {GUILayout.MinWidth(140)});
                //不是中文的可以选择正式导入/临时导入  中文只能临时导入
                if (psdData.isCreate && !Psd2UguiTool.HasChinese(Psd2UguiTool.ExcludeEndOfChinese(layerName,new []{'_'})))
                {
                    GUILayout.Label("导入类型：", new[] {GUILayout.Width(60)});
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    psdData.inputType = (PsdImgInputType) EditorGUI.EnumPopup(
                        new Rect(lastRect.x + 60, lastRect.y, 80, 60),
                        psdData.inputType);
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(vSpace);

            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();


            //确定按钮
            if (GUILayout.Button(_exportHint, GUILayout.Height(50)))
            {
                if (nameInvalid)
                {
                    Debug.LogError($"名字包含异常符号，请检查导入清单:{invalidnName}");
                    return;
                }
                if (_exportWarn)
                {
                    int retId = EditorUtility.DisplayDialogComplex(
                        "警告!",
                        "即将导入不符合项目规范的图片!!!\n",
                        "头铁导入",
                        "和美术核对",
                        default);
                    if (retId == 0)
                    {
                        ExportImageBatch();
                        _exportCb?.Invoke();
                    }
                }
                else
                {
                    ExportImageBatch();
                    _exportCb?.Invoke();
                }
            }
        }

        //批量设置选择数据列表覆盖
        private static void ChangeChoiceData(List<Psd2UguiChoiceData> psdData, bool isCreate)
        {
            foreach (var data in psdData)
            {
                data.isCreate = isCreate;
            }
        }

        //批量设置选择数据列表覆盖,忽略不存在
        private static void InitDatas(List<Psd2UguiChoiceData> psdData, bool isCreate, bool isSlice)
        {
            foreach (var data in psdData)
            {
                //1.新图片与项目图片不是相同图片 2.项目图片为空 时选择默认导入
                data.isCreate = data.oldImage == null || isCreate;
                //中文默认不裁剪
                data.isSlice = !Psd2UguiTool.HasChinese(data.node.layer.Name) && isSlice;
                data.sliceImage = Slice(data, out TextureSliceData sliceData);
            }
        }

        //整理选择数据
        private static void TrimChoiceData(List<Psd2UguiChoiceData> psdData)
        {
            int initSize = psdData.Count;
            for (int i = 0; i < initSize; i++)
            {
                if (i >= psdData.Count) return;
                Psd2UguiChoiceData dataTmp = psdData[i];
                for (int j = psdData.Count - 1; j > i; j--)
                {
                    Psd2UguiChoiceData dataTmp1 = psdData[j];
                    if (dataTmp.originalImage.imageContentsHash != dataTmp1.originalImage.imageContentsHash) continue;
                    dataTmp.ignoreData ??= new List<Psd2UguiChoiceData>();
                    dataTmp1.isShow = false;
                    dataTmp1.isCreate = false;
                    dataTmp.ignoreData.Add(dataTmp1);
                }
            }
        }

        //批量导出图片
        private void ExportImageBatch()
        {
            try
            {
                //AssetDatabase.StartAssetEditing();
                int length = _choicePsdData.Count;
                for (int i = 0; i < length; i++)
                {
                    EditorUtility.DisplayProgressBar("解析PSD文件", "正在解析和导出PSD文件", i / (float) length);
                    Psd2UguiChoiceData psdData = _choicePsdData[i];
                    var exportPath = GetExportPath(psdData);
                    ExportImage(psdData, exportPath, psdData.isCreate);
                }
            }
            finally
            {
                //AssetDatabase.StopAssetEditing();
            }
            

            EditorUtility.ClearProgressBar();
            Close();
        }

        //获得导入图片路径
        private string GetExportPath(Psd2UguiChoiceData psdData)
        {
            if (psdData.node.parentNode !=null && psdData.node.parentNode.nodeName ==  "ndFullBg")
            {
                return Psd2UguiRule.EXPORT_IMAGE_UI_BG_PATH;
            }
            
            //如果有中文，只能临时导入
            if (Psd2UguiTool.HasChinese(psdData.assetName) || psdData.inputType == PsdImgInputType.临时导入)
            {
                var parentPath = Psd2UguiRule.EXPORT_IMAGE_UI_TEMP_PREFAB_PATH;
                var targetPath = parentPath + "/" + _psdCreateNodeInfo.nodeName;
                if (!Directory.Exists(targetPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("创建目标目录失败：" + ex.Message);
                    }
                }

                return targetPath;
            }
            else
            {
                return _directoryPath;
            }
        }


        //导出图片
        private void ExportImage(Psd2UguiChoiceData psdData, string directoryPath, bool isCreate)
        {
            //获取导出节点layer
            PhotoshopFile.Layer exportLayer = psdData.node.layer;
            PsdImage pSdImage = new PsdImage();
            string picName = exportLayer.Name;

            //对称图片特殊处理,不单独导出(这里)
            picName = picName.Replace(Psd2UguiRule.IMAGE_SYMMETRY_KEY, "");
            //背景图片的名字处理
            picName = picName.Replace(Psd2UguiRule.IMAGE_BG_KEY, "");
            //图片路径
            string spritePath = Psd2UguiTool.FilePathToUnityAssetPath(directoryPath + "/" + psdData.assetName + ".png");
            string spriteOldPath = AssetDatabase.GetAssetPath(psdData.oldImage);
            if (!string.IsNullOrEmpty(spriteOldPath))
                spritePath = spriteOldPath;

            //生成新图片
            if (isCreate)
            {
                if (string.IsNullOrEmpty(psdData.assetName))
                {
                    Debug.LogError("尝试导出名称为空的图片");
                    return;
                }
                //九宫格剪裁
                if (psdData.isSlice)
                {
                    Texture2D clipTexture = Slice(psdData, out TextureSliceData sliceData);
                    pSdImage.sprite = PhotoshopFile.PSDEditorWindow.SaveAsset(clipTexture,
                        spritePath, PhotoshopFile.PSDEditorWindow.pixelsToUnitSize);
                    if (clipTexture != psdData.originalImage)
                    {
                        psdData.originalImage = clipTexture;
                        TextureSliceTool.SetSliceBorder(spritePath, clipTexture,
                            Psd2UguiRule.SLICE_RESERVED_NUM - 1, Psd2UguiRule.SLICE_CLIP_SIMILARITY,
                            Psd2UguiRule.SLICE_CLIP_LOW_SIMILARITY, Psd2UguiRule.SLICE_CLIP_AVE_SIMILARITY, sliceData);
                    }
                }
                else
                {
                    pSdImage.sprite = PhotoshopFile.PSDEditorWindow.SaveAsset(psdData.originalImage,
                        spritePath, PhotoshopFile.PSDEditorWindow.pixelsToUnitSize);
                }

                psdData.oldImage = psdData.originalImage;
            }
            //不生成图片
            else
            {
                UnityEngine.Object existPng = AssetDatabase.LoadAssetAtPath(spritePath, typeof(Sprite));
                pSdImage.sprite = (Sprite) existPng;
            }

            pSdImage.spritePath = spritePath;
            psdData.node.nodeImage = pSdImage;

            //关联Data赋值
            if (psdData.ignoreData == null) return;
            foreach (var data in psdData.ignoreData)
            {
                data.node.nodeImage = pSdImage;
            }
        }


        /// <summary>
        /// 裁剪图片
        /// </summary>
        /// <param name="psdData"></param>
        /// <param name="sliceData"></param>
        /// <returns></returns>
        private static Texture2D Slice(Psd2UguiChoiceData psdData, out TextureSliceData sliceData)
        {
            return TextureSliceTool.SliceClipTexture(psdData.originalImage,
                Psd2UguiRule.SLICE_CONTINUE_NUM, Psd2UguiRule.SLICE_RESERVED_NUM,
                Psd2UguiRule.SLICE_CLIP_SIMILARITY, Psd2UguiRule.SLICE_CLIP_LOW_SIMILARITY,
                Psd2UguiRule.SLICE_CLIP_AVE_SIMILARITY, out sliceData);
        }


        //获取格子绘制风格
        private static GUIStyle GetGuiStyle()
        {
            GUIStyle skin = GUI.skin.box;
            skin.normal.textColor = Color.white;
            GUIStyle guiStyle = new GUIStyle(skin)
            {
                alignment = TextAnchor.LowerCenter,
                imagePosition = ImagePosition.ImageAbove,
                fixedWidth = 250,
                fixedHeight = 70
            };
            return guiStyle;
        }

        //获取选中格子绘制风格
        private static GUIStyle GetHintGuiStyle()
        {
            GUIStyle skin = GUI.skin.box;
            skin.normal.textColor = Color.red;
            GUIStyle guiStyle = new GUIStyle(skin)
            {
                alignment = TextAnchor.LowerCenter,
                imagePosition = ImagePosition.ImageAbove,
                fixedWidth = 250,
                fixedHeight = 70
            };
            return guiStyle;
        }

        //分辨率是否超出
        private static bool ExceedResolution(Texture2D texture)
        {
            Vector2 size = texture.texelSize;
            if (size == Psd2UguiRule.FULL_SCREEN_IMAGE_SIZE)
                return false;
            if (size.x > Psd2UguiRule.RESOLUTION_WIDTH || size.y > Psd2UguiRule.RESOLUTION_HEIGHT)
                return true;
            return false;
        }

        /// <summary>
        /// 将导入的图与项目中已有的图是否相同（命名和尺寸）
        /// </summary>
        /// <param name="texture">导入的图片</param>
        /// <param name="psdData">单个PSD数据</param>
        /// <returns></returns>
        private static bool CheckSamePic(Texture2D texture, Psd2UguiChoiceData psdData)
        {
            if (psdData.oldImage)
            {
                Texture2D oldImage = psdData.oldImage;
                Color32[] oColor32 = ConvertTexture(oldImage).GetPixels32();
                Color32[] cColor32 = texture.GetPixels32();
                if (psdData.assetName == oldImage.name
                    && texture.width == oldImage.width
                    && texture.height == oldImage.height && oColor32.Equals(cColor32))
                {
                    return true;
                }
            }
            return false;
        }

        public static Texture2D ConvertTexture(Texture2D source)
        {
            byte[] pix = source.GetRawTextureData();
            Texture2D t = new Texture2D(source.width, source.height, source.format, false);
            t.LoadRawTextureData(pix);
            t.Apply();
            return t;
        }

        static Texture2D duplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
        /// <summary>
        /// 修改并重置PSD数据
        /// </summary>
        /// <returns></returns>
        private PsdNodeInfo ModifyReimportPSD()
        {
            _choicePsdData = new List<Psd2UguiChoiceData>();
            //数据赋值
            PsdNodeInfo tempInfo = Psd2UguiParse.Instance.PSDParseByPath(_psdPath, _directoryPath, _folderPaths, _choicePsdData);

            InitDatas(_choicePsdData, _defaultCreate, _sliceClip);
            TrimChoiceData(_choicePsdData);
            InitLayerDic();
            return tempInfo;
        }

        /// <summary>
        /// 判断PSD文件是否有修改并写回
        /// </summary>
        /// <para>layerIndex 图层顺序</para>
        /// <returns></returns>
        private void CheckAndSavePSD(NameModifyMode modifyType, int layerIndex)
        {
            // 基础校验
            if (layerIndex == -1)
            {
                return;
            }
            // PSD文件写回
            bool psdModify = false;

            if (modifyType == NameModifyMode.SingleMode)
            {
                var layer = _psd.Layers[layerIndex];
                if (layer.Name != "</Layer set>" && layer.Name != "</Layer group>")
                {
                    if (_psdLayerNameDic.ContainsKey(layer.Name))
                    {
                        if (_psdLayerNameDic[layer.Name] != layer.Name)
                        {
                            layer.Name = _psdLayerNameDic[layer.Name];
                            psdModify = true;
                        }
                    }
                }
            }
            else if (modifyType == NameModifyMode.BatchMode)
            {
                foreach (Layer layer in _psd.Layers)
                {
                    if (layer.Name != "</Layer set>" && layer.Name != "</Layer group>")
                    {
                        if (_psdLayerNameDic.ContainsKey(layer.Name))
                        {
                            if (_psdLayerNameDic[layer.Name] != layer.Name)
                            {
                                layer.Name = _psdLayerNameDic[layer.Name];
                                psdModify = true;
                            }
                        }
                    }
                }
            }


            if (psdModify)
            {
                Debug.Log("PSD：【" + _psdPath +"】 has been modified and reimport");
                _psd.Save(_psdPath, Encoding.Default);
                PsdNodeInfo newPsdNodeInfo = ModifyReimportPSD();
                _psdChangedCallBack?.Invoke(newPsdNodeInfo);
            }
        }

        /// <summary>
        /// 获取图层同名数量
        /// </summary>
        /// <param name="checkName"></param>
        /// <returns></returns>
        private int CheckSameNameCount(string checkName)
        {
            int count = 0;
            foreach (Layer layer in _psd.Layers)
            {
                if (layer.Name != "</Layer set>" && layer.Name != "</Layer group>")
                {
                    if (checkName.Equals(layer.Name))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void ModifyName(Psd2UguiChoiceData psdData, string layerName)
        {
            // 还原修改标记
            string modifyTypeStr = _modifyNameDic[psdData.layerIndex];
            _modifyNameDic[psdData.layerIndex] = string.Empty;
            // 如果新的名字非空 则对psd文件中的名字赋值
            if (!string.IsNullOrEmpty(_nameTempDic[layerName]))
            {
                //单独修改
                NameModifyMode modifyType = NameModifyMode.SingleMode;
                if (modifyTypeStr == BATCH_MODIFY_STR)
                {
                    //批量修改
                    modifyType = NameModifyMode.BatchMode;
                }else if (modifyTypeStr == SINGLE_MODIFY_STR)
                {
                    modifyType = NameModifyMode.SingleMode;
                }
                _psdLayerNameDic[layerName] = _nameTempDic[layerName];
                CheckAndSavePSD(modifyType, psdData.layerIndex);
            }
            else
            {
                // 否则就还原成默认的名字
                _nameTempDic[layerName] = layerName;
            }
        }

        /// <summary>
        /// 初始化节点名的映射
        /// </summary>
        private void InitLayerDic()
        {
            _nameTempDic = new Dictionary<string, string>();
            _modifyNameDic = new Dictionary<int, string>();
            _psdLayerNameDic = new Dictionary<string, string>();

            foreach (Layer layer in _psd.Layers)
            {
                if (layer.Name != "</Layer set>" && layer.Name != "</Layer group>")
                {
                    string layerName = layer.Name;
                    if (!_psdLayerNameDic.ContainsKey(layerName))
                    {
                        _psdLayerNameDic.Add(layerName, layerName);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            //删除临时图片
            string path = Psd2UguiRule.EXPORT_COMPARE_IMAGE_TEMP_PATH;
            if(File.Exists(path))
                File.Delete(path);
        }
    }
}