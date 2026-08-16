//******************************************************************************************
//Created By Liangc on 2021/12/30
// PSD2UGUI图片工具
//@Description
//******************************************************************************************

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Package.PSD2UGUI
{
    public class TextureSliceTool
    {
        private const int DEFAULT = -1;

        /// <summary>
        /// 纹理行像素是否相等
        /// </summary>
        /// <param name="colors">颜色集合</param>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        /// <param name="row1">行1</param>
        /// <param name="row2">行2</param>
        /// <param name="similarity">单个像素相似度</param>
        /// <param name="lowSimilarity">最低单个像素相似度</param>
        /// <param name="rowSimilarity">整行相似度</param>
        /// <returns></returns>
        public static bool EqualRowPixel(Color32[] colors, int width, int height,
            int row1, int row2, float similarity, float lowSimilarity, float rowSimilarity)
        {
            if (row1 < 1 || row1 > height || row2 < 1 || row2 > height) return false;
            float similarityTmp = 0;
            for (int i = 1; i <= width; i++)
            {
                int index1 = (height - row1) * width + i - 1;
                int index2 = (height - row2) * width + i - 1;
                float sim = GetColorSimilarity(colors[index1], colors[index2]);
                similarityTmp += sim;
                if (sim < similarity)
                {
                    if (sim < lowSimilarity)
                        return false;
                    float averageSim = (similarityTmp + (width - i) * 100) / width;
                    if (averageSim < rowSimilarity)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 纹理列像素是否相等
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        /// <param name="similarity"></param>
        /// <param name="lowSimilarity"></param>
        /// <param name="rowSimilarity"></param>
        /// <returns></returns>
        public static bool EqualColumnPixel(Color32[] colors, int width, int height,
            int col1, int col2, float similarity, float lowSimilarity, float rowSimilarity)
        {
            if (col1 < 1 || col1 > width || col2 < 1 || col2 > width) return false;
            float similarityTmp = 0;
            for (int i = 1; i <= height; i++)
            {
                int index1 = (height - i) * width + col1 - 1;
                int index2 = (height - i) * width + col2 - 1;
                float sim = GetColorSimilarity(colors[index1], colors[index2]);
                similarityTmp += sim;
                if (sim < similarity)
                {
                    if (sim < lowSimilarity)
                        return false;
                    float averageSim = (similarityTmp + (height - i) * 100) / height;
                    if (averageSim < rowSimilarity)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 纹理水平方向是否可九宫格切割
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="continueNum"></param>
        /// <param name="colStart"></param>
        /// <param name="colEnd"></param>
        /// <param name="similarity"></param>
        /// <param name="lowSimilarity"></param>
        /// <param name="rowSimilarity"></param>
        /// <returns></returns>
        public static bool HorizontalSlice(Texture2D texture, int continueNum, out int colStart,
            out int colEnd, float similarity, float lowSimilarity, float rowSimilarity)
        {
            colStart = DEFAULT;
            colEnd = DEFAULT;

            //查找相同像素区间
            int tmpStart = 0, tmpEnd = 0;
            List<int[]> sections = new List<int[]>();
            Color32[] colors = texture.GetPixels32();
            for (int i = 1; i < texture.width - 1; i++)
            {
                int compareIdx = tmpStart == 0 ? i + 1 : tmpStart;
                if (EqualColumnPixel(colors, texture.width, texture.height,
                    i, compareIdx, similarity, lowSimilarity, rowSimilarity))
                {
                    tmpStart = tmpStart == 0 ? i : tmpStart;
                    tmpEnd = i;
                }
                else
                {
                    if (tmpEnd - tmpStart >= continueNum)
                        sections.Add(new[] {tmpStart, tmpEnd});
                    tmpStart = 0;
                    tmpEnd = 0;
                }
            }

            //对比像素区间长度
            int sectionsCount = sections.Count;
            if (sectionsCount == 0)
                return false;

            int[] ret = sections[0];
            for (int i = 1; i < sectionsCount; i++)
            {
                int[] secTmp = sections[i];
                if (secTmp[1] - secTmp[0] <= ret[1] - ret[0])
                    continue;
                ret = secTmp;
            }

            colStart = ret[0];
            colEnd = ret[1];
            return true;
        }

        /// <summary>
        /// 纹理水平竖直是否可九宫格切割
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="continueNum"></param>
        /// <param name="rowStart"></param>
        /// <param name="rowEnd"></param>
        /// <param name="similarity"></param>
        /// <param name="lowSimilarity"></param>
        /// <param name="colSimilarity"></param>
        /// <returns></returns>
        public static bool VerticalSlice(Texture2D texture, int continueNum, out int rowStart,
            out int rowEnd, float similarity, float lowSimilarity, float colSimilarity)
        {
            rowStart = DEFAULT;
            rowEnd = DEFAULT;

            //查找相同像素区间
            int tmpStart = 0, tmpEnd = 0;
            List<int[]> sections = new List<int[]>();
            Color32[] colors = texture.GetPixels32();
            for (int i = 1; i < texture.height - 1; i++)
            {
                int compareIdx = tmpStart == 0 ? i + 1 : tmpStart;
                if (EqualRowPixel(colors, texture.width, texture.height,
                    i, compareIdx, similarity, lowSimilarity, colSimilarity))
                {
                    tmpStart = tmpStart == 0 ? i : tmpStart;
                    tmpEnd = i;
                }
                else
                {
                    if (tmpEnd - tmpStart >= continueNum)
                        sections.Add(new[] {tmpStart, tmpEnd});
                    tmpStart = 0;
                    tmpEnd = 0;
                }
            }

            //对比像素区间长度
            int sectionsCount = sections.Count;
            if (sectionsCount == 0)
                return false;

            int[] ret = sections[0];
            for (int i = 1; i < sectionsCount; i++)
            {
                int[] secTmp = sections[i];
                if (secTmp[1] - secTmp[0] <= ret[1] - ret[0])
                    continue;
                ret = secTmp;
            }

            rowStart = ret[0];
            rowEnd = ret[1];
            return true;
        }

        /// <summary>
        /// 九宫格剪裁纹理
        /// </summary>
        /// <param name="texture">纹理</param>
        /// <param name="continueNum">剪裁判断持续像素数量</param>
        /// <param name="reserveNum">剪裁预留像素</param>
        /// <param name="similarity">单个像素相似度</param>
        /// <param name="lowSimilarity">单个相似最低相似度</param>
        /// <param name="aveSimilarity">平均相似度</param>
        /// <param name="sliceData">裁剪数据</param>
        /// <returns></returns>
        public static Texture2D SliceClipTexture(Texture2D texture, int continueNum, int reserveNum,
            float similarity, float lowSimilarity, float aveSimilarity, out TextureSliceData sliceData)
        {
            sliceData = null;
            if (continueNum <= 0 || reserveNum <= 0 || continueNum <= reserveNum) return texture;
            //获得剪裁开始结束位置
            int oldWidth = texture.width;

            int oldHeight = texture.height;
            bool verticalSlice = VerticalSlice(texture, continueNum,
                out int rowStart, out int rowEnd, similarity, lowSimilarity, aveSimilarity);
            bool horizontalSlice = HorizontalSlice(texture, continueNum,
                out int colStart, out int colEnd, similarity, lowSimilarity, aveSimilarity);
            //判断剪裁位置是否超出旧尺寸
            if (verticalSlice && (rowStart + reserveNum >= oldHeight || rowStart + reserveNum >= rowEnd))
            {
                verticalSlice = false;
                rowStart = DEFAULT;
                rowEnd = DEFAULT;
            }

            if (horizontalSlice && (colStart + reserveNum >= oldWidth || colStart + reserveNum >= colEnd))
            {
                horizontalSlice = false;
                colStart = DEFAULT;
                colEnd = DEFAULT;
            }

            if (!verticalSlice && !horizontalSlice) return texture;

            //计算新纹理尺寸
            rowStart = verticalSlice ? rowStart + reserveNum : rowStart;
            colStart = horizontalSlice ? colStart + reserveNum : colStart;
            int newWidth = horizontalSlice ? oldWidth - (colEnd - colStart + 1) : oldWidth;
            int newHeight = verticalSlice ? oldHeight - (rowEnd - rowStart + 1) : oldHeight;
            Color[] newColors = new Color[newWidth * newHeight];
            Color[] oldColors = texture.GetPixels();
            sliceData = new TextureSliceData
            {
                //rowStart/colStart时也会被剪裁,剪裁后的值减1
                rowStart = rowStart == DEFAULT ? DEFAULT : rowStart - 1,
                colStart = colStart == DEFAULT ? DEFAULT : colStart - 1,
                reserveNum = reserveNum - 1
            };

            //旧纹理数据映射到新纹理
            int newIndex = 0;
            for (int i = 0; i < oldColors.Length; i++)
            {
                int rowTmp = oldHeight - (i + 1) / oldWidth;
                int colTmp = (i + 1) % oldWidth;
                if (verticalSlice && rowTmp >= rowStart && rowTmp <= rowEnd)
                    continue;
                if (horizontalSlice && colTmp >= colStart && colTmp <= colEnd)
                    continue;
                newColors[newIndex] = oldColors[i];
                ++newIndex;
            }

            //保存新纹理数据
            Texture2D newTexture = new Texture2D(newWidth, newHeight, TextureFormat.ARGB32, false);
            newTexture.SetPixels(newColors);
            newTexture.Apply();
            return newTexture;
        }

        /// <summary>
        /// 设置九宫格边界
        /// </summary>
        /// <param name="path">图片路径</param>
        /// <param name="texture">图片纹理</param>
        /// <param name="continueNum">设置九宫格连续像素</param>
        /// <param name="similarity">设置九宫格连续像素</param>
        /// <param name="lowSimilarity">设置九宫格连续像素</param>
        /// <param name="aveSimilarity">设置九宫格连续像素</param>
        /// <param name="sliceData">裁剪数据</param>
        public static void SetSliceBorder(string path, Texture2D texture, int continueNum,
            float similarity, float lowSimilarity, float aveSimilarity, TextureSliceData sliceData)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer)
                return;
            bool verticalSlice, horizontalSlice;
            int rowStart, rowEnd, colStart, colEnd;

            if (sliceData == null)
            {
                //通过计算获得连续像素位置
                verticalSlice = VerticalSlice(texture, continueNum, out rowStart,
                    out rowEnd, similarity, lowSimilarity, aveSimilarity);
                horizontalSlice = HorizontalSlice(texture, continueNum, out colStart,
                    out colEnd, similarity, lowSimilarity, aveSimilarity);
                if (!verticalSlice && !horizontalSlice) return;
            }
            else
            {
                //通过裁剪数据获得连续像素位置
                verticalSlice = sliceData.rowStart != DEFAULT;
                rowEnd = sliceData.rowStart;
                rowStart = sliceData.rowStart - sliceData.reserveNum;
                horizontalSlice = sliceData.colStart != DEFAULT;
                colEnd = sliceData.colStart;
                colStart = sliceData.colStart - sliceData.reserveNum;
            }

            //设置边界
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            Vector4 oldBorder = importer.spriteBorder;
            float top = verticalSlice ? rowStart - 1 : oldBorder.y;
            float bottom = verticalSlice ? texture.height - rowEnd : oldBorder.w;
            float left = horizontalSlice ? colStart - 1 : oldBorder.x;
            float right = horizontalSlice ? texture.width - colEnd : oldBorder.z;
            top = top < 0 ? 0 : top;
            bottom = bottom < 0 ? 0 : bottom;
            left = left < 0 ? 0 : left;
            right = right < 0 ? 0 : right;
            importer.spriteBorder = new Vector4(left, bottom, right, top);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// 颜色是否相等
        /// </summary>
        /// <param name="a">颜色a</param>
        /// <param name="b">颜色b</param>
        /// <param name="similarity">相似度</param>
        /// <returns></returns>
        public static bool ColorEqual(Color32 a, Color32 b, float similarity)
        {
            if (a.Equals(b))
                return true;
            if (similarity >= 100)
                return false;
            float colorSimilarity = GetColorSimilarity(a, b);
            return colorSimilarity >= similarity;
        }

        /// <summary>
        /// 获取颜色相似度
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static float GetColorSimilarity(Color32 a, Color32 b)
        {
            float difR = (a.r - b.r) * 1.0f / 255;
            float difG = (a.g - b.g) * 1.0f / 255;
            float difB = (a.b - b.b) * 1.0f / 255;
            float difA = (a.a - b.a) * 1.0f / 255;
            float dif = Mathf.Sqrt(difR * difR + difG * difG + difB * difB + difA * difA);
            return (1 - dif) * 100;
        }
    }
}