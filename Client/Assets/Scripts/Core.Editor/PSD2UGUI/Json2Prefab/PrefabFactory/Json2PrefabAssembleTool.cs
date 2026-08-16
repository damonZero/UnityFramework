using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public static class Json2PrefabAssembleTool
    {
        private const string PREFAB_FIND_PATH = "Assets/GameRes/UI/General";

        private static Dictionary<string, string> _dictCommonPrefabGuid;

        static Json2PrefabAssembleTool()
        {
            InitCommonPrefab();
        }

        public static void ParseTextTid(PsdNodeText node, TextMeshProUGUI t2d)
        {
            if (node == null)
                return;

            // 基础节点：直接用 PSD 文本颜色
            t2d.color = new Color(node.color[0], node.color[1], node.color[2], node.color[3]);
        }

        /// <summary>
        /// 判断节点名是否对应一个公共预制体(按预制体名匹配, 忽略状态/缩放后缀)
        /// </summary>
        public static bool IsCommonPrefab(string nodeName)
        {
            var prefabName = Psd2UguiTool.GetCommonPrefabName(nodeName);
            return _dictCommonPrefabGuid.ContainsKey(prefabName);
        }

        /// <summary>
        /// 根据 PSD 分段颜色构建富文本
        /// 整体颜色 = 第一个字的颜色, 与其一致的字一律不加标签;
        /// 不同颜色按色号生成 color 标签
        /// </summary>
        public static string BuildRichTextContent(PsdNodeText node)
        {
            var content = node.content.Replace("\r", "");
            if (node.colorRuns == null || node.runLengths == null
                || node.colorRuns.Length == 0 || node.runLengths.Length == 0)
            {
                return content;
            }

            // 整体颜色 = PSD 第一个字的颜色
            var first = node.colorRuns[0];
            var overallHex = ToHex(new Color(first[0] / 255f, first[1] / 255f, first[2] / 255f));

            var sb = new StringBuilder(content.Length + 64);
            var charIndex = 0;
            for (var i = 0; i < node.runLengths.Length && charIndex < content.Length; i++)
            {
                var runLen = Math.Min(node.runLengths[i], content.Length - charIndex);
                if (runLen <= 0)
                    break;

                var runText = content.Substring(charIndex, runLen);
                charIndex += runLen;

                if (i >= node.colorRuns.Length)
                {
                    sb.Append(runText);
                    continue;
                }

                var rgba = node.colorRuns[i];
                var runHex = ToHex(new Color(rgba[0] / 255f, rgba[1] / 255f, rgba[2] / 255f));

                if (runHex == overallHex)
                    sb.Append(runText); // 与整体颜色一致, 不加富文本
                else
                    sb.Append("<color=").Append(runHex).Append('>').Append(runText).Append("</color>");
            }

            return sb.ToString();
        }

        /// <summary>颜色转十六进制(#RRGGBB), ToHtmlStringRGB 不带 # 需手动补</summary>
        private static string ToHex(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }

        public static void CommonPrefabHandler(PsdNodeBase node, Transform parent,
            out Transform tr, out bool isInterruption)
        {
            tr = null;
            isInterruption = false;

            //解析并应用公共组件缩放标记 @Sxx, 只设置根节点缩放, 组件内部UI缩放已在预制制作时处理
            //应用后剥离该后缀更新node.name, 后续方法不再关心 @Sxx
            var scale = Psd2UguiTool.ParseCommonPrefabScale(node.name);
            if (scale.HasValue)
            {
                node.name = Psd2UguiTool.StripCommonPrefabScaleSuffix(node.name);
                node.scale = scale.Value;
            }

            //到这里，缩放等特殊后缀已被剥离
            var prefabName = Psd2UguiTool.GetCommonPrefabName(node.name);
            if (!_dictCommonPrefabGuid.TryGetValue(prefabName, out var guid))
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (!prefab)
            {
                Debug.LogError($"{prefabName}预制体不存在");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"{prefabName}预制体实例化失败");
                return;
            }

            tr = instance.transform;
            tr.localScale *= node.scale;

            isInterruption = Json2PrefabCommonPrefabPluginMgr.CustomizedCheck(node, instance);
        }

        private static void InitCommonPrefab()
        {
            var findDir = new DirectoryInfo(PREFAB_FIND_PATH);
            if (!findDir.Exists)
            {
                Debug.LogError($"路径{PREFAB_FIND_PATH}不存在");
                return;
            }

            _dictCommonPrefabGuid = new Dictionary<string, string>();

            TraverseUIPrefab(findDir, file =>
            {
                var guid = AssetDatabase.AssetPathToGUID(Json2PrefabParseTool.FullPath2AssetPath(file.FullName));
                var name = Path.GetFileNameWithoutExtension(file.Name);
                _dictCommonPrefabGuid.Add(name, guid);
            });
        }

        private static void TraverseUIPrefab(DirectoryInfo directoryInfo, Action<FileInfo> fileHandle)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                if (file.FullName.EndsWith(".prefab"))
                    fileHandle(file);
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                TraverseUIPrefab(dir, fileHandle);
            }
        }
    }
}
