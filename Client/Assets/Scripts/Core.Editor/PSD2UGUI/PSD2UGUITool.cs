//******************************************************************************************
//Created By Liangc on 2019/6/3
//PSD转UGUI工具类
//PhotoShop坐标系采用左上角坐标系(X向右,Y向下),PSD文件读取采取PhotoShop坐标系
//UGUI采用中心坐标系(X向右,Y向上),工具中涉及到所有UGUI的坐标系均将PhotoShop坐标系映射到中心点坐标系
//@Description 
//******************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;
using Microsoft.International.Converters.PinYinConverter;
using Debug = System.Diagnostics.Debug;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD转UGUI工具类
    /// </summary>
    public static class Psd2UguiTool
    {
        /// <summary>
        /// 公共组件缩放标记正则, 精准匹配 @S 或 @s 后跟数字(不分大小写), 如 @S70/@s80
        /// </summary>
        private static readonly Regex CommonPrefabScaleRegex = new(@"@[Ss]\d+", RegexOptions.Compiled);

        /// <summary>
        /// 公共组件状态后缀分隔符，PSD节点名格式: {PrefabName}__{StateIndex}
        /// </summary>
        public const string COMMON_PREFAB_STATE_SEPARATOR = "__";

        //置灰向量
        private static readonly Vector4 _grayVec = new Vector4(0.2125f, 0.7154f, 0.0721f, 0);

        /// <summary>
        /// 查找继承接口的所有子类
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <returns></returns>
        public static Type[] FindInterfaceSubclass<TInterface>()
        {
            Type[] interfaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(TInterface))))
                .ToArray();
            return interfaces;
        }

        /// <summary>
        /// 文件夹下所有文件遍历
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <param name="fileHandle"></param>
        public static void TraverseFolder(DirectoryInfo directoryInfo, Func<FileInfo, bool> fileHandle)
        {
            if (directoryInfo == null || fileHandle == null)
                return;

            FileInfo[] childFiles = directoryInfo.GetFiles();
            DirectoryInfo[] directories = directoryInfo.GetDirectories();

            bool isContinue = true;
            foreach (var file in childFiles)
            {
                if (!file.FullName.EndsWith(".meta"))
                    isContinue = fileHandle(file);
            }

            if (directories.Length == 0 || !isContinue)
                return;

            foreach (var dir in directories)
            {
                TraverseFolder(dir, fileHandle);
            }
        }

        /// <summary>
        /// 系统资源路径转Unity资源路径
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string FilePathToUnityAssetPath(string filePath)
        {
            int startIndex = filePath.IndexOf("Assets", StringComparison.Ordinal);
            string sub = filePath.Substring(startIndex);
            return sub.Replace("\\", "/");
        }

        /// <summary>
        /// 是否是配置图片
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsConfigImage(string path)
        {
            return path.Contains(Psd2UguiRule.CONFIG_IMAGE_KEY);
        }

        /// <summary>
        /// 判断是否是根预制体
        /// </summary>
        /// <param name="go">判断对象</param>
        /// <returns></returns>
        public static bool IsPrefabRoot(GameObject go)
        {
            return UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(go);
        }

        /// <summary>
        /// 颜色置灰
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color GrayColor(Color color)
        {
            var grey = Vector4.Dot(color, _grayVec);
            return new Color(grey, grey, grey, color.a);
        }

        /// <summary>
        /// 字符串是否包含中文
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool HasChinese(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }

        /// <summary>
        /// 字符串是否包含空格
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool HasSpace(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            return str.Contains(" ");
        }

        /// <summary>
        /// 中文转拼音
        /// </summary>
        /// <param name="chinese">需要转换的字符串</param>
        /// <returns></returns>
        public static string Chinese2PinYin(string chinese)
        {
            StringBuilder result = new StringBuilder();
            foreach (var chi in chinese)
            {
                try
                {
                    ChineseChar c = new ChineseChar(chi);
                    if (c.Pinyins.Count > 0 && c.Pinyins[0].Length > 0)
                    {
                        string pinyin = c.Pinyins[0].ToLower();
                        result.Append(Regex.Replace(pinyin, @"\d", ""));
                    }
                }
                catch (Exception e)
                {
                    result.Append(chi.ToString());
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 校正名字
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <param name="convChi"></param>
        /// <param name="excludeUni"></param>
        /// <returns></returns>
        public static string AdjustName(string name,
            string prefix, bool convChi, int excludeUni = -1)
        {
            name = ExceptionCharHandle(name, excludeUni);
            if (convChi && HasChinese(name))
                name = Chinese2PinYin(name);
            if (!name.StartsWith(prefix))
                name = prefix + name;
            return name;
        }

        /// <summary>
        /// 异常字符处理
        /// </summary>
        /// <param name="name"></param>
        /// <param name="excludeUni"></param>
        /// <returns></returns>
        public static string ExceptionCharHandle(string name, int excludeUni)
        {
            name = name.Replace(" ", "");
            char[] tmp = name.ToCharArray();
            for (int i = 0; i < tmp.Length; i++)
            {
                char t = tmp[i];
                if (t > 255) continue;
                if (t == excludeUni
                    || (t >= 48 && t <= 57)
                    || (t >= 65 && t <= 90)
                    || (t >= 97 && t <= 122))
                    continue;
                tmp[i] = '_';
            }

            return new string(tmp);
        }

        /// <summary>
        /// 检查命名是否异常
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>是否有异常</returns>
        public static bool CheckNameInValid(string name)
        {
            name = name.Replace(" ", "");
            var str = name;
            char[] tmp = name.ToCharArray();
            for (int i = 0; i < tmp.Length; i++)
            {
                var t = (int)tmp[i];
                if ((t >= 33 && t <= 47) || (t >= 58 && t <= 64) || (t >= 91 && t <= 94) || (t >= 123 && t <= 126) ||
                    t == 96)
                    return true;
            }

            return false;
        }


        /// <summary>
        /// 检查是否分割出中文字符，如果有，把中文字符去掉
        /// </summary>
        /// <param name="str"></param>
        /// <param name="splits"></param>
        /// <returns></returns>
        public static string ExcludeEndOfChinese(string str, char[] splits)
        {
            var nameSplit = str.Split(splits);
            var lastSplit = nameSplit.Last();
            if (nameSplit.Length > 1 && HasChinese(lastSplit))
            {
                return str.Substring(0, str.Length - lastSplit.Length - 1);
            }

            return str;
        }

        /// <summary>
        /// 检查是否分割出中文字符，如果是，只保留最后一部分
        /// </summary>
        /// <param name="str"></param>
        /// <param name="splits"></param>
        /// <returns></returns>
        public static string StayOnlyEndOfChinese(string str, char[] splits)
        {
            var nameSplit = str.Split(splits);
            var lastSplit = nameSplit.Last();
            if (nameSplit.Length > 1 && HasChinese(lastSplit))
            {
                return lastSplit;
            }

            return str;
        }

        /// <summary>
        /// 从PSD节点名中提取公共组件名称，如 CmnBtnClose__0 → CmnBtnClose
        /// </summary>
        public static string GetCommonPrefabName(string nodeName)
        {
            var idx = nodeName.LastIndexOf(COMMON_PREFAB_STATE_SEPARATOR, StringComparison.Ordinal);
            return idx > 0 ? nodeName[..idx] : nodeName;
        }

        /// <summary>
        /// 从PSD节点名中提取公共组件状态后缀，如 CmnBtnClose__one@S70 → one (剥离缩放标记)
        /// </summary>
        public static string GetCommonPrefabNameSuffix(string nodeName)
        {
            var idx = nodeName.LastIndexOf(COMMON_PREFAB_STATE_SEPARATOR, StringComparison.Ordinal);
            if (idx <= 0) return "";
            //先剥离缩放标记段, 再取状态后缀
            return StripCommonPrefabScaleSuffix(nodeName[(idx + COMMON_PREFAB_STATE_SEPARATOR.Length)..]);
        }

        /// <summary>
        /// 从PSD节点名中解析公共组件缩放百分比, 如 CmnBtnClose__0@S70 → 0.7
        /// 无缩放标记返回 null(不缩放)
        /// </summary>
        public static float? ParseCommonPrefabScale(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName)) return null;
            var match = CommonPrefabScaleRegex.Match(nodeName);
            if (!match.Success) return null;

            //去掉 @S/@s 前缀取数字
            var scalePercent = float.Parse(match.Value.Substring(2));
            return scalePercent / 100f;
        }

        /// <summary>
        /// 从PSD节点名中剥离公共组件缩放标记段, 如 CmnBtnClose__0@S70 → CmnBtnClose__0
        /// 精准匹配 @S+数字, 只删缩放标记段
        /// </summary>
        public static string StripCommonPrefabScaleSuffix(string nodeName)
        {
            return string.IsNullOrEmpty(nodeName) ? nodeName : CommonPrefabScaleRegex.Replace(nodeName, "");
        }

        /// <summary>
        /// 从PSD节点名中提取公共组件状态序号，如 CmnBtnClose__1 → 1
        /// </summary>
        public static int GetCommonPrefabStateIndex(string nodeName)
        {
            var suffix = GetCommonPrefabNameSuffix(nodeName);
            return string.IsNullOrEmpty(suffix) ? 0 : int.Parse(suffix);
        }
    }
}