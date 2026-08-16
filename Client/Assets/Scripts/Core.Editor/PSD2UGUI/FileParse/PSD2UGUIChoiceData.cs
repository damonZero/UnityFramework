//*****************************************************************************
//Created By Liangc on 2019/8/29
//
//@Description PSD选择窗口数据类
//*****************************************************************************

using System;
using UnityEngine;
using Package.PSD2UGUI;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

//图片导入类型
public enum PsdImgInputType
{
    正式导入,
    临时导入
}

public class Psd2UguiChoiceData
{
    /// <summary>
    /// psd layer图层顺序
    /// </summary>
    public int layerIndex = -1;
    /// <summary>
    /// 是否生成
    /// </summary>
    public bool isCreate;

    /// <summary>
    /// 是否为临时导入图片
    /// </summary>
    public PsdImgInputType inputType = PsdImgInputType.正式导入;

    /// <summary>
    /// 是否裁剪
    /// </summary>
    public bool isSlice = false;

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool isShow = true;

    /// <summary>
    /// PSD节点
    /// </summary>
    public PsdNodeInfo node;

    /// <summary>
    /// 原始图片
    /// </summary>
    public Texture2D originalImage;


    /// <summary>
    /// 展示图片
    /// </summary>
    public Texture2D sliceImage;

    /// <summary>
    /// 项目中的图片
    /// </summary>
    public Texture2D oldImage;

    /// <summary>
    /// 项目图片地址
    /// </summary>
    public string oldImagePath;

    /// <summary>
    /// 相关忽略数据
    /// </summary>
    public List<Psd2UguiChoiceData> ignoreData;

    /// <summary>
    /// 相似度记录
    /// </summary>
    public float similarity = 0;

    /// <summary>
    /// 图片资源名
    /// </summary>
    public string assetName = "";
    
    /// <summary>
    /// 差异图片
    /// </summary>
    public Texture2D DiffImage
    {
        get
        {
            if (!_diffImage)
                _diffImage = GetDifferenceImage();
            return _diffImage;
        }
    }

    private Texture2D _diffImage;

    public override bool Equals(object obj)
    {
        if (obj is Psd2UguiChoiceData compareData)
        {
            bool sameName = compareData.node.nodeName == node.nodeName;
            bool sameHash = compareData.originalImage.imageContentsHash == originalImage.imageContentsHash;
            return sameName && sameHash;
        }

        return false;
    }

    public override int GetHashCode()
    {
        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        return base.GetHashCode();
    }

    //异步初始化差异图 (原依赖 EditorCoroutines 包, 移植后改为同步)
    public void InitDifferenceImage(EditorWindow window)
    {
        if (_diffImage) return;
        _diffImage = GetDifferenceImage();
    }

    private readonly Color _specialColor = new Color(1, 1, 1, 0);

    //获取对比图片
    private Texture2D GetDifferenceImage()
    {
        if (!originalImage) return default;

        int diffWidth, diffHeight;
        Color[] diffColors;
        if (oldImage)
        {
            //先保存到本地，再用本地的两张图片去比较，保存到本地可以消除差异
            string path = Psd2UguiRule.EXPORT_COMPARE_IMAGE_TEMP_PATH;
            if(File.Exists(path))
                File.Delete(path);

            //优先选择裁剪图片进行比较。
            Texture2D selectImage = originalImage;
            if (isSlice && sliceImage != null)
            {
                selectImage = sliceImage;
            }
            else
            {
                selectImage = originalImage;
            }
            //如果宽高不一致，不做相似度比较，没意义
            if (selectImage.width != oldImage.width || selectImage.height != oldImage.height)
            {
                return default;
            }

            var sprite = PhotoshopFile.PSDEditorWindow.SaveAsset(selectImage,
                path, PhotoshopFile.PSDEditorWindow.pixelsToUnitSize);

            Color[] newColors = sprite.texture.GetPixels();
            similarity = 0;

            Color[] oldColors = ReadProImagePixels(oldImage);
            int oldColorLength = oldColors.Length;
            int oldWidth = oldImage.width, oldHeight = oldImage.height;
            int newColorLength = newColors.Length;
            int newWidth = selectImage.width, newHeight = selectImage.height;

            int diffLength = newColorLength > oldColorLength ? newColorLength : oldColorLength;
            diffWidth = oldWidth > newWidth ? oldWidth : newWidth;
            diffHeight = oldHeight > newHeight ? oldHeight : newHeight;
            diffColors = new Color[diffLength];


            var similarityCount = 0;
            for (int i = 0; i < diffLength; i++)
            {
                Color oldColor = i < oldColorLength ? oldColors[i] : Color.magenta;
                Color newColor = i < newColorLength ? newColors[i] : Color.magenta;
                if (newColor == _specialColor || (oldColor == newColor && oldColor != Color.magenta))
                {
                    diffColors[i] = Color.green;
                    similarityCount++;
                }
                else
                    diffColors[i] = Color.magenta;
            }

            similarity = similarityCount / diffLength * 100;
        }
        else
        {
            Color[] newColors = originalImage.GetPixels();

            diffWidth = originalImage.width;
            diffHeight = originalImage.height;
            diffColors = new Color[newColors.Length];
            for (int i = 0; i < newColors.Length; i++)
            {
                diffColors[i] = Psd2UguiTool.GrayColor(newColors[i]);
            }
        }

        Texture2D retTexture = new Texture2D(diffWidth, diffHeight);
        retTexture.SetPixels(diffColors);
        retTexture.Apply();
        return retTexture;
    }

    //获取项目图片像素
    private Color[] ReadProImagePixels(Texture2D readImage)
    {
        RenderTexture renTmp = RenderTexture.GetTemporary(readImage.width, readImage.height);
        Graphics.Blit(oldImage, renTmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renTmp;
        Texture2D retTexture2D = new Texture2D(readImage.width, readImage.height);
        retTexture2D.ReadPixels(new Rect(0, 0, renTmp.width, renTmp.height), 0, 0);
        retTexture2D.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renTmp);
        return retTexture2D.GetPixels();
    }

    public void SetSlice(bool flag)
    {
        if (isSlice != flag)
        {
            isSlice = flag;
            _diffImage = null;
        }
    }
}