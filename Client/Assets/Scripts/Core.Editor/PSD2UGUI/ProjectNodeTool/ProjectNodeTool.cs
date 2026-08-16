//************************************************************************
//Create by Liangc on 2019/10/30
//
//@Description  项目节点工具类
//************************************************************************

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public static class ProjectNodeTool
    {
        /// <summary>
        /// 解析预制体节点信息
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static ProjectNodeInfo ParseNodeInfo(FileInfo node)
        {
            //基础信息
            var parseNode = new ProjectNodeInfo
            {
                path = GetRelativeAssetsPath(node.FullName),
                name = Path.GetFileNameWithoutExtension(node.Name)
            };

            //缩略图
            var nodeObj = AssetDatabase.LoadAssetAtPath<GameObject>(parseNode.path);
            parseNode.prefabObj = nodeObj;
            parseNode.thumbnail = PrefabPreview.GetPrefabPreview(nodeObj);
            parseNode.content = new GUIContent()
            {
                text = parseNode.name,
                image = parseNode.thumbnail
            };

            // 作者信息: 原 P33 依赖 LuaBaseBehaviour + LuaVarBindTool 解析 Lua 脚本注释, 本工程已剥离

            return parseNode;
        }

        //本地路径转项目资源路径
        private static string GetRelativeAssetsPath(string path)
        {
            return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "")
                .Replace('\\', '/');
        }
    }
}
