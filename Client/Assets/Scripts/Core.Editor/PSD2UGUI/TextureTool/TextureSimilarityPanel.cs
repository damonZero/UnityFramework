//*****************************************************************************
//Created By cd_liangc
//
//@Description 纹理相似度查找面板
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class TextureSimilarityPanel : EditorWindow
    {
        //遍历处理文件数量
        private int _handleCount;

        //所有相似纹理数据
        private List<TextureSimilarityData> _allTexture;

        //处理纹理后缀
        private string[] _extensions = new string[] {".PNG", ".JPG"};

        //最小相似度
        private float _minSimilar = 0.98f;

        //滑动位置缓存
        private Vector2 _scrollV2;

        //导入图片绘制
        private readonly GUIContent _importContent = new GUIContent();

        //图片显示风格
        private GUIStyle _imageStyle;

        [MenuItem("Assets/资源相关工具/纹理冗余查找替换")]
        public static void CompareTextureSimilarity()
        {
            TextureSimilarityPanel window = GetWindow<TextureSimilarityPanel>();
            window.Init();
        }

        private void Init()
        {
            RecursiveProcess(null);
            _allTexture = new List<TextureSimilarityData>(_handleCount);
            RecursiveProcess(InitTextureSimilarity);
            CollectTextureSimilarity();
            _allTexture.Sort((texData1, texData2) =>
            {
                int count1 = texData1.simTextures?.Count ?? 0;
                int count2 = texData2.simTextures?.Count ?? 0;
                return -count1.CompareTo(count2);
            });
        }

        private void OnGUI()
        {
            if (_allTexture == null || _allTexture.Count == 0)
                return;
            _imageStyle ??= GetGuiStyle();
            _scrollV2 = GUILayout.BeginScrollView(_scrollV2);
            EditorGUILayout.BeginVertical();
            foreach (var texData in _allTexture)
            {
                DrawTextureData(texData);
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        //绘制纹理数据
        private void DrawTextureData(TextureSimilarityData data)
        {
            EditorGUILayout.BeginHorizontal();
            data.expand = EditorGUILayout.Toggle(data.expand);
            int simCount = data.simTextures?.Count ?? 0;
            GUILayout.Label($"相似纹理:{simCount}个");
            GUILayout.Label(data.path);
            GUILayout.Label(data.tex, _imageStyle);
            EditorGUILayout.EndHorizontal();

            //相似纹理绘制
            if (!data.expand || simCount == 0) return;
            EditorGUILayout.BeginVertical();
            GUILayout.Label(
                "相似纹理↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("一键替换"))
                TextureSimilarityTool.ReplaceSameTexture(data);
            if (GUILayout.Button("全选"))
                AllChoiceHandle(data, true);
            if (GUILayout.Button("取消全选"))
                AllChoiceHandle(data, false);
            EditorGUILayout.EndHorizontal();

            foreach (var simTexture in data.simTextures)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(30);
                simTexture.choice = EditorGUILayout.Toggle(simTexture.choice);
                GUILayout.Label($"相似度:{simTexture.similarValue:P}");
                GUILayout.Label(simTexture.data.path);
                GUILayout.Label(simTexture.data.tex, _imageStyle);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Label(
                "↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑");

            EditorGUILayout.EndVertical();
        }

        //全选处理
        private void AllChoiceHandle(TextureSimilarityData texData, bool choice)
        {
            foreach (var simTexture in texData.simTextures)
            {
                simTexture.choice = choice;
            }
        }

        //计算纹理相似度数据
        private void InitTextureSimilarity(string path)
        {
            Texture2D tex = TextureSimilarityTool.ReadTexture(path);
            string hash = TextureSimilarityTool.CalculateHash(tex);
            TextureSimilarityData simData = new TextureSimilarityData
            {
                tex = tex,
                hash = hash,
                path = path,
            };
            _allTexture.Add(simData);
        }

        //计算相似纹理,并记录
        private void CollectTextureSimilarity()
        {
            foreach (var texData in _allTexture)
            {
                foreach (var compareData in _allTexture)
                {
                    if (texData == compareData) continue;
                    float similar = TextureSimilarityTool.CalculateSimilarValue(texData.hash, compareData.hash);
                    if (similar < _minSimilar) continue;
                    TextureSimilarityCompare simCompareData = new TextureSimilarityCompare
                    {
                        data = compareData,
                        choice = false,
                        similarValue = similar
                    };
                    texData.simTextures ??= new List<TextureSimilarityCompare>();
                    texData.simTextures.Add(simCompareData);
                }
            }
        }

        //递归处理当前选中目录
        private void RecursiveProcess(Action<string> handler)
        {
            var path = Selection.assetGUIDs;
            var handleDirectory = new DirectoryInfo(AssetDatabase.GUIDToAssetPath(path[0]));

            _handleCount = 0;
            RecursionDirectory(handleDirectory, handler);

            Debug.Log($"总共遍历:{_handleCount} 个文件");
        }

        //递归处理文件夹
        private void RecursionDirectory(DirectoryInfo curDirectory, Action<string> handler)
        {
            //处理当前文件夹下所有指定后缀文件
            var allFiles = curDirectory.GetFiles();
            var fileLength = allFiles.Length;
            for (var i = 0; i < fileLength; ++i)
            {
                var handleFile = allFiles[i];
                if (!SameExtension(handleFile.Extension)) continue;
                handler?.Invoke(handleFile.FullName);
                ++_handleCount;
            }

            //递归处理当前文件夹下的其他文件夹
            var allDirectories = curDirectory.GetDirectories();
            var direLength = allDirectories.Length;
            for (var i = 0; i < direLength; ++i)
            {
                var handleDire = allDirectories[i];
                RecursionDirectory(handleDire, handler);
            }
        }

        //是否为遍历纹理路径
        private bool SameExtension(string extension)
        {
            string upper = extension.ToUpper();
            foreach (var ex in _extensions)
            {
                if (upper.Equals(ex))
                    return true;
            }

            return false;
        }

        //获取格子绘制风格
        private GUIStyle GetGuiStyle()
        {
            GUIStyle skin = GUI.skin.box;
            skin.normal.textColor = Color.white;
            GUIStyle guiStyle = new GUIStyle(skin)
            {
                fixedWidth = 100,
                fixedHeight = 50
            };
            return guiStyle;
        }
    }
}