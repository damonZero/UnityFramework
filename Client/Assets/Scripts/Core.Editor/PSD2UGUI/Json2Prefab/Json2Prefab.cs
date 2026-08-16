using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public static class Json2Prefab
    {
        // 颜色错误收集(非标准色号), 导出结束后统一 LogError 到控制台
        private static readonly List<string> _colorErrors = new List<string>();

        // System.Text.Json 反序列化配置: 节点数据类均为 public 字段, 需开启 IncludeFields
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            // Node.js 输出的 lineSpace/letterSpacing 可能是字符串, 允许字符串转数字
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static void ClearColorErrors()
        {
            _colorErrors.Clear();
        }

        public static void AddColorError(string message)
        {
            _colorErrors.Add(message);
        }

        private static void ShowColorErrors()
        {
            if (_colorErrors.Count == 0)
                return;
            // 弹窗提示
            EditorUtility.DisplayDialog("颜色警告", string.Join("\n", _colorErrors), "确认");
            // 同时输出到控制台
            foreach (var error in _colorErrors)
                Debug.LogError(error);
        }

        //[MenuItem("美术工具/UI专用工具/测试JSON2PREFAB", false, 2000)]
        public static void Test()
        {
            CreatePrefab("F:\\int\\tools\\PSD\\航会试炼_失败.psd", false);
        }

        public static void CreatePrefab(string psdPath, bool ignoreHideLayer)
        {
            ClearColorErrors();
            var jsonPath = Psd2Json.Psd2JsonByNodeJs(psdPath, ignoreHideLayer);

            //必须要先设置临时图片目录 在解析
            Json2PrefabParseTool.SetTmpImgPoolPath(jsonPath);

            var jsonInfo = File.ReadAllText(jsonPath);
            var rootNode = ParseJsonInfo(jsonInfo);

            Json2PrefabAssemble.Assemble(rootNode);
            ShowColorErrors();
        }

        /// <summary>
        /// 解析Json信息
        /// </summary>
        /// <param name="jsonStr"></param>
        private static PsdNodeRoot ParseJsonInfo(string jsonStr)
        {
            var rootNode = JsonSerializer.Deserialize<PsdNodeRoot>(jsonStr, _jsonOptions);
            var childJArray = rootNode.childNodes;
            if (childJArray == null)
            {
                Debug.LogError("childJArray == null");
                return null;
            }

            ParseChildrenJArray(rootNode, childJArray);

            return rootNode;
        }

        private static void ParseChildrenJArray(PsdNodeBase parentNode, JsonArray childNodes)
        {
            foreach (var childNode in childNodes)
            {
                var psdNode = ParseChildJToken(childNode,parentNode);

                psdNode.Parent = parentNode;
                parentNode.AddChildPsdNode(psdNode);

                if (psdNode.childNodes == null)
                    continue;
                ParseChildrenJArray(psdNode, psdNode.childNodes);
            }
        }

        private static PsdNodeBase ParseChildJToken(JsonNode jsonToken,PsdNodeBase parentNode)
        {
            var type = (PsdNodeEnum)jsonToken["type"].GetValue<int>();
            switch (type)
            {
                case PsdNodeEnum.Group:
                    return JsonSerializer.Deserialize<PsdNodeGroup>(jsonToken, _jsonOptions);
                case PsdNodeEnum.Image:
                    var imageNode = JsonSerializer.Deserialize<PsdNodeImage>(jsonToken, _jsonOptions);
                    imageNode.assetPath = Json2PrefabParseTool.ParseImgPath(imageNode,parentNode);
                    return imageNode;
                case PsdNodeEnum.Text:
                    return JsonSerializer.Deserialize<PsdNodeText>(jsonToken, _jsonOptions);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
