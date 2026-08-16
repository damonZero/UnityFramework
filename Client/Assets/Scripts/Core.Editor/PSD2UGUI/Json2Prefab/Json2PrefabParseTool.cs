using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Package.PSD2UGUI
{
    public static class Json2PrefabParseTool
    {
        //图片导出路径
        private const string IMG_POOL_PATH = Psd2UguiRule.EXPORT_IMAGE_PATH;
        private const string IMG_CFG_PATH = Psd2UguiRule.CONFIG_IMAGE_PATH;

        //中文图片临时目录存放地方不一样
        private static string _curTmpImgPath;

        public static void SetTmpImgPoolPath(string jsonPath)
        {
            var dir = Path.GetDirectoryName(jsonPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _curTmpImgPath = FullPath2AssetPath(dir);
        }

        public static string ParseImgPath(PsdNodeImage node,PsdNodeBase parentNode)
        {
            var imgName = Psd2UguiTool.ExcludeEndOfChinese(node.name,new []{'_'});

            var dir = GetImagePath(parentNode);

            if (Regex.IsMatch(imgName, @"[^a-zA-Z0-9_-]"))
                dir = _curTmpImgPath;

            var assetPath = Path.Combine(dir, $"{imgName}.png");

            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
            {
                var path = AssetDatabase.FindAssets(imgName).Select(AssetDatabase.GUIDToAssetPath);
                assetPath = path.FirstOrDefault(p =>
                {
                    //如果是个文件夹就跳过
                    if (Directory.Exists(p))
                    {
                        return false;
                    }
                    
                    if (!Path.GetFileNameWithoutExtension(p).Equals(imgName))
                        return false;

                    if (!p.Contains(IMG_CFG_PATH))
                        return false;

                    return !p.Split(Path.DirectorySeparatorChar).Any(d => d.StartsWith("_"));
                });
            }
            
            //dir 目录下的子文件夹目录找不到，需要遍历子文件夹目录查找
            if (string.IsNullOrEmpty(assetPath))
            {
                var dirs = Directory.GetDirectories(dir);
                foreach (var d in dirs)
                {
                    var path = Path.Combine(d, $"{imgName}.png");
                    if (File.Exists(path))
                    {
                        assetPath = path;
                        break;
                    }
                }
            }
            

            return assetPath;
        }

        private static string GetImagePath(PsdNodeBase node)
        {
            if (node !=null && node.name == "ndFullBg")
            {
                return Psd2UguiRule.EXPORT_IMAGE_UI_BG_PATH;
            }
            else
            {
                return IMG_POOL_PATH;
            }
        }

        /// <summary>
        ///  全路径转Unity资源路径
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static string FullPath2AssetPath(string fullPath)
        {
            return fullPath[fullPath.IndexOf("Assets", StringComparison.Ordinal)..];
        }
    }
}