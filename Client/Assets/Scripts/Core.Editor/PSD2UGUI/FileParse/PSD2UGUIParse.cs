//*****************************************************************************
//Created By Liangc on 2019/6/3
//PSD文件解析类
//@Description 在Editor下解析并导出PSD信息类
//*****************************************************************************

using System;
using System.IO;
using Aspose.PSD;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Aspose.PSD.FileFormats.Psd.Layers;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD文件解析
    /// </summary>
    public class Psd2UguiParse
    {
        private static Psd2UguiParse _instance;
        public static Psd2UguiParse Instance => _instance ?? (_instance = new Psd2UguiParse());

        /// <summary>
        /// PSD文件解析
        /// </summary>
        /// <param name="path">PSD文件路径</param>
        /// <param name="directoryPath"></param>
        /// <param name="findPaths"></param>
        /// <param name="choices"></param>
        /// <returns>PSD信息节点</returns>
        public PsdNodeInfo PSDParseByPath(string path, string directoryPath,
            List<string> findPaths, List<Psd2UguiChoiceData> choices)
        {
            choices.Clear();
            //获取PSD路径
            string fileName = Path.GetFileNameWithoutExtension(path);
            PsdNodeInfo rootNode;

            //创建文件夹来存储导出的图片
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            //PSDImporter解析PSD文件,用来导出图片的原因是Aspose导出的图片有水印
            PhotoshopFile.PsdFile psd = new PhotoshopFile.PsdFile(path, Encoding.Default);

            //Aspose解析PSD文件
            using (Image image = Image.Load(path))
            {
                Rect rootRect = new Rect(0, 0, image.Width, image.Height);
                rootNode = new PsdNodeInfo(PsdNodeType.CommonNode, fileName, null, rootRect);

                Type type = image.GetType();
                PropertyInfo propertyInfo = type.GetProperty("Layers"); //获取指定名称的属性
                Layer[] layers = (Layer[])propertyInfo.GetValue(image, null); //获取所有层信息,根据PSD层级的倒序遍历排布
                int layersLength = layers.Length;

                PsdNodeInfo currentNode = rootNode;
                for (int i = layersLength - 1; i >= 0; --i)
                {
                    EditorUtility.DisplayProgressBar("解析PSD文件", "正在解析和导出PSD文件",
                        (layersLength - i) / (float)layersLength);
                    Layer layer = layers[i];
                    Rect rect = new Rect(layer.Left, -layer.Top, layer.Width, layer.Height);
                    PsdNodeInfo addNode =
                        new PsdNodeInfo(PsdNodeType.CommonNode, psd.Layers[i].Name, currentNode, rect);

                    //节点(PSD中的文件夹,包括普通节点和按钮节点)
                    if (layer is LayerGroup)
                        currentNode = ParseNode(layer, addNode, currentNode);
                    //元素(PSD中的文字和图片)
                    else
                    {
                        if (layer is TextLayer textLayer)
                            //文字节点
                            currentNode = ParseText(textLayer, addNode, currentNode);
                        else
                            //图片节点
                            currentNode = ParseImage(addNode, currentNode, psd.Layers[i], findPaths, choices, i);
                    }
                }

                EditorUtility.ClearProgressBar();
            }

            choices.Sort((a, b)
                => string.Compare(a.originalImage.name, b.originalImage.name, StringComparison.Ordinal));

            Debug.Log(rootNode.ToString());

            return rootNode;
        }

        //解析节点
        private PsdNodeInfo ParseNode(Layer layer, PsdNodeInfo addNode, PsdNodeInfo currentNode)
        {
            //节点开始
            if (!layer.DisplayName.StartsWith("<End of layer group>"))
            {
                addNode.nodeType = Psd2UguiRule.JudgePsdNodeType(layer.Name);
                currentNode.AddChildNodeLast(addNode);
                currentNode = addNode;
            }
            //节点结束
            else
            {
                addNode.nodeType = PsdNodeType.OverNode;
                currentNode.AddChildNodeFirst(addNode);
                currentNode = currentNode.parentNode;
            }

            string addPrefix = addNode.nodeType == PsdNodeType.CommonNode ? Psd2UguiRule.PER_KEY_ND : "";
            addNode.nodeName = Psd2UguiTool.AdjustName(addNode.nodeName, addPrefix, true);
            return currentNode;
        }

        //文字信息解析
        private PsdNodeInfo ParseText(TextLayer layer, PsdNodeInfo addNode, PsdNodeInfo currentNode)
        {
            PsdText retText = new PsdText { text = layer.Text };

            Aspose.PSD.Color readColor = layer.TextColor;
            retText.color = new UnityEngine.Color(readColor.R / 255.0f,
                readColor.G / 255.0f, readColor.B / 255.0f, readColor.A / 255.0f);

            Aspose.PSD.Font readFont = layer.Font;
            retText.fontName = readFont.Name;
            retText.fontSize = readFont.Size;

            addNode.nodeType = PsdNodeType.Text;
            addNode.nodeText = retText;
            currentNode.AddChildNodeLast(addNode);
            addNode.nodeName = Psd2UguiTool.AdjustName(addNode.nodeName, Psd2UguiRule.PER_KEY_T_2D + "@", true, 64);
            return currentNode;
        }


        /// <summary>
        /// 解析图片
        /// </summary>
        /// <param name="findPaths"></param>
        /// <param name="choices"></param>
        /// <param name="layerIndex">psd layer的顺序</param>
        /// <returns>PSD信息节点</returns>
        private PsdNodeInfo ParseImage(PsdNodeInfo addNode, PsdNodeInfo currentNode,
            PhotoshopFile.Layer layer, List<string> findPaths, List<Psd2UguiChoiceData> choices, int layerIndex)
        {
            addNode.nodeType = PsdNodeType.Image;
            addNode.layer = layer;
            addNode.nodeName = Psd2UguiTool.AdjustName(addNode.nodeName, "", false);
            Psd2UguiChoiceData choiceData = ParseChoiceData(addNode, findPaths, false, choices);

            if (choiceData != null)
            {
                choiceData.layerIndex = layerIndex;
                choices.Add(choiceData);
            }

            currentNode.AddChildNodeLast(addNode);
            return currentNode;
        }

        //解析出选择数据
        private static Psd2UguiChoiceData ParseChoiceData(PsdNodeInfo psdNode,
            List<string> directoryPaths, bool isCover, List<Psd2UguiChoiceData> choices)
        {
            Texture2D newImage = PhotoshopFile.PSDEditorWindow.CreateTexture(psdNode.layer);
            if (newImage == null) return null;

            //相同图片检测
            Psd2UguiChoiceData sameHashData =
                choices.Find(data => data.originalImage.imageContentsHash == newImage.imageContentsHash);
            if (sameHashData != null)
                return new Psd2UguiChoiceData
                {
                    isCreate = sameHashData.isCreate, node = psdNode, originalImage = sameHashData.originalImage,
                    oldImage = sameHashData.oldImage,assetName = sameHashData.assetName
                };

            //生成解析信息
            newImage.name = Psd2UguiTool.ExcludeEndOfChinese(psdNode.nodeName, new[] { '_' });
            Psd2UguiChoiceData retData = new Psd2UguiChoiceData
            {
                isCreate = isCover, node = psdNode, originalImage = newImage,
                assetName = newImage.name
            };
            foreach (var dir in directoryPaths)
            {
                //背景图片会有一层同名文件夹
                string findPath = Psd2UguiTool.FilePathToUnityAssetPath(dir + "/" + retData.assetName);
                if (Directory.Exists(findPath))
                {
                    findPath = findPath + "/" + retData.assetName + ".png";
                }
                else
                {
                    findPath = Psd2UguiTool.FilePathToUnityAssetPath(dir + "/" + retData.assetName + ".png");
                }
                if (File.Exists(findPath))
                {
                    Sprite loadSprite = AssetDatabase.LoadAssetAtPath(findPath, typeof(Sprite)) as Sprite;
                    // ReSharper disable once Unity.NoNullPropagation
                    retData.oldImage = loadSprite?.texture;
                    retData.oldImagePath = findPath;
                    break;
                }
            }

            return retData;
        }
    }
}