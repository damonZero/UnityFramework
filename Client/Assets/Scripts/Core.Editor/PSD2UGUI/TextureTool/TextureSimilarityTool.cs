//*****************************************************************************
//Created By cd_liangc
//
//@Description 纹理相似度对比工具
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class TextureSimilarityTool
    {
        private const int D_BIT_COUNT = 8;
        private const int BIT_COUNT = 64;

        // [MenuItem("Assets/图片相似度检查测试", false, 100)]
        // public static void Test()
        // {
        // UnityEngine.Object[] selects = Selection.objects;
        //
        // foreach (var sel in selects)
        // {
        //     Texture2D t1 = ReadTexture(GetWindowPath(sel));
        //
        //     string tempPath = GetWindowPath(sel);
        //     string fileTempPath = tempPath.Replace(".png", "") + "_WindowOpen.png";
        //     PhotoshopFile.PSDEditorWindow.SaveAsset(t1,
        //         fileTempPath, PhotoshopFile.PSDEditorWindow.pixelsToUnitSize);
        //
        //     string hash1 = CalculateHash(t1);
        //     Debug.Log(sel.name);
        //     Debug.Log(hash1);
        //     Debug.Log(ToPrint(hash1));
        // }

        // Texture2D t1 = ReadTexture(GetWindowPath(selects[0]));
        // Texture2D t2 = ReadTexture(GetWindowPath(selects[1]));
        // string hash1 = CalculateHash(t1);
        // string hash2 = CalculateHash(t2);
        // float dis = CalculateDistance(hash1, hash2);
        // float similar = dis / BIT_COUNT;
        // Debug.Log($"{t1.name}:{ToPrint(hash1)}");
        // Debug.Log($"{t2.name}:{ToPrint(hash2)}");
        // Debug.Log($"{t1.name}<==>{t2.name} : {dis},{similar}");
        // }

        private static string ToPrint(string hash)
        {
            StringBuilder ret = new StringBuilder();
            ret.Append("\n");
            for (int i = 0; i < hash.Length; i++)
            {
                if (i % D_BIT_COUNT == 0)
                {
                    ret.Append("\n");
                }

                ret.Append(hash[i]);
            }

            return ret.ToString();
        }

        public static string GetWindowPath(UnityEngine.Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj).Replace("Assets", "");
            path = Application.dataPath + path;
            return path.Replace("/", "\\");
        }

        /// <summary>
        /// 读取纹理
        /// </summary>
        /// <param name="path">window路径</param>
        /// <returns></returns>
        public static Texture2D ReadTexture(string path)
        {
            FileStream fs = File.OpenRead(path);
            fs.Seek(0, SeekOrigin.Begin);
            byte[] image = new byte[(int) fs.Length];
            fs.Read(image, 0, (int) fs.Length);

            var index = path.IndexOf("Asset", StringComparison.Ordinal);
            if (index != 0)
                path = path.Remove(0, index);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var texCopy = new Texture2D(tex.width, tex.height);
            texCopy.LoadImage(image);
            fs.Dispose();
            return texCopy;
        }

        /// <summary>
        /// 计算纹理p-hash值
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        public static string CalculateHash(Texture2D tex)
        {
            Texture2D reduceTex = ReduceSize(tex, D_BIT_COUNT);
            GrayTexture(reduceTex);
            float[,] dct = TextureDct(reduceTex);
            float average = AverageDct(dct);
            return CalculateHash(dct, average);
        }

        /// <summary>
        /// 计算2个P-hash值的相似度
        /// </summary>
        /// <param name="hash1"></param>
        /// <param name="hash2"></param>
        /// <returns></returns>
        public static float CalculateSimilarValue(string hash1, string hash2)
        {
            return CalculateDistance(hash1, hash2) / BIT_COUNT;
        }

        /// <summary>
        /// 计算2个P-hash值之间的汉明距离
        /// </summary>
        /// <param name="hash1"></param>
        /// <param name="hash2"></param>
        /// <returns></returns>
        public static float CalculateDistance(string hash1, string hash2)
        {
            float dis = 0;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] == hash2[i])
                    dis++;
            }

            return dis;
        }

        /// <summary>
        /// 压缩纹理
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        static Texture2D ReduceSize(Texture2D tex, int size)
        {
            if (!tex || size <= 0)
                return null;

            Texture2D reduceTex = new Texture2D(size, size, TextureFormat.RGB24, false);
            Color[] colors = tex.GetPixels();
            int width = tex.width;
            int height = tex.height;
            float rateX = width * 1.0f / size;
            float rateY = height * 1.0f / size;
            for (int i = 0; i < size; i++)
            {
                int idxY = Mathf.RoundToInt(i * rateY);
                if (idxY >= height) break;
                for (int j = 0; j < size; j++)
                {
                    int idxX = Mathf.RoundToInt(j * rateX);
                    if (idxX >= width) break;
                    Color color = colors[idxY * width + idxX];
                    reduceTex.SetPixel(j, i, color);
                }
            }

            return reduceTex;
        }


        //dct矩阵
        private static float[,] _dctMatrix;

        //dct转置矩阵
        private static float[,] _dctMatrixT;

        /// <summary>
        /// 纹理DCT变换
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        static float[,] TextureDct(Texture2D tex)
        {
            _dctMatrix ??= CreateDctMatrix(D_BIT_COUNT);
            _dctMatrixT ??= Transpose(_dctMatrix);
            float[,] tex1F = Texture2F(tex);
            return Multiply(Multiply(_dctMatrix, tex1F), _dctMatrixT);
        }

        /// <summary>
        /// 灰度纹理
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        static void GrayTexture(Texture2D tex)
        {
            for (int i = 0; i < tex.height; i++)
            {
                for (int j = 0; j < tex.width; j++)
                {
                    Color color = tex.GetPixel(j, i);
                    float gray = (color.r * 30 + color.g * 59 + color.b * 11) / 100;
                    tex.SetPixel(j, i, new Color(gray, gray, gray));
                }
            }
        }

        /// <summary>
        /// 纹理转float矩阵
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        static float[,] Texture2F(Texture2D tex)
        {
            int size = tex.width;
            float[,] f = new float[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    f[i, j] = tex.GetPixel(i, j).r;
                }
            }

            return f;
        }

        /// <summary>
        /// 创建DCT矩阵
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        static float[,] CreateDctMatrix(int size)
        {
            float[,] ret = new float[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float angle = (y + 0.5f) * Mathf.PI * x / size;
                    float cTemp = x == 0 ? Mathf.Sqrt(1.0f / size) : Mathf.Sqrt(2.0f / size);
                    ret[x, y] = cTemp * Mathf.Cos(angle);
                }
            }

            return ret;
        }

        /// <summary>
        /// 矩阵转置
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        static float[,] Transpose(float[,] c)
        {
            int size = c.GetLength(0);
            float[,] ret = new float[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    ret[y, x] = c[x, y];
                }
            }

            return ret;
        }

        /// <summary>
        /// 矩阵相乘
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        static float[,] Multiply(float[,] c1, float[,] c2)
        {
            int size = c1.GetLength(0);
            float[,] ret = new float[size, size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum1 = 0;
                    for (int k = 0; k < size; k++)
                    {
                        sum1 += c1[x, k] * c2[k, y];
                    }

                    ret[x, y] = sum1;
                }
            }

            return ret;
        }

        /// <summary>
        /// 计算矩阵均值
        /// </summary>
        /// <param name="dct"></param>
        /// <returns></returns>
        static float AverageDct(float[,] dct)
        {
            int size = dct.GetLength(0);
            float aver = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    aver += dct[i, j];
                }
            }

            return aver / (size * size);
        }

        /// <summary>
        /// 计算P-Hash值
        /// </summary>
        /// <param name="dct"></param>
        /// <param name="aver"></param>
        /// <returns></returns>
        static string CalculateHash(float[,] dct, float aver)
        {
            string hash = string.Empty;
            for (int i = 0; i < D_BIT_COUNT; i++)
            {
                for (int j = 0; j < D_BIT_COUNT; j++)
                {
                    hash += dct[i, j] >= aver ? "1" : "0";
                }
            }

            return hash;
        }

        /// <summary>
        /// 替换相似纹理
        /// </summary>
        /// <param name="texData"></param>
        public static int ReplaceSameTexture(TextureSimilarityData texData)
        {
            // 原 P33 依赖 Core.AssetReference 替换引用纹理, 本工程剥离
            Debug.LogWarning("[TextureSimilarityTool] 纹理引用替换依赖 P33 的 AssetReference, 已剥离");
            return 0;
        }

        //资源加载路径转换
        public static string SwitchPath(string path)
        {
            return "Assets" + Path.GetFullPath(path).Replace(
                Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
        }
    }
}