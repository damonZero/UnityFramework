//*****************************************************************************
//Created By huangjj
//
//@Description 
//*****************************************************************************

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.WSA;

namespace Package.PSD2UGUI
{
    public class TempPrefabEvent : AssetModificationProcessor
    {
        private const string TEMP_FOLDER = "_Temp";
        private const string TEMP_IMG_FOLDER = "Assets\\GameRes\\UI\\ResPool\\_Temp";

        public static bool GlobalInable = true; //全局开关

        [InitializeOnLoadMethod]
        static void EditorApplication_projectChanged()
        {
            //--projectWindowChanged已过时
            //--全局监听Project视图下的资源是否发生变化（添加 删除 移动等）
            // EditorApplication.projectChanged += delegate() { Debug.Log("资源状态发生变化！"); };
            PrefabStage.prefabSaving += instance =>
            {
                if (!GlobalInable) return;
                try
                {
                    string savePath = GetObjPrefabPath(instance);
                    ImageHandle(savePath, instance);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            };
        }

        //--监听“资源即将被创建”事件
        public static void OnWillCreateAsset(string savePath)
        {
            if (!GlobalInable) return;
            if (!savePath.EndsWith(".prefab")) return;
            try
            {
                var delayCall = new Action(async () =>
                {
                    //此时还资源未储存 延迟1s执行
                    await Task.Delay(System.TimeSpan.FromSeconds(1));
                    var instance = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
                    ImageHandle(savePath, instance);
                });
                delayCall();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        /// <summary>
        /// 根据物体获得预制体的路径
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private static string GetObjPrefabPath(GameObject instance)
        {
            string savePath = "";
            if (PrefabUtility.IsPartOfPrefabAsset(instance))
            {
                // 预制体资源就是自身
                savePath = AssetDatabase.GetAssetPath(instance);
            }

            // Scene中的Prefab Instance是Instance不是Asset
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
            {
                // 获取预制体资源
                var prefabAsset =
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);

                savePath = AssetDatabase.GetAssetPath(prefabAsset);
            }

            // PrefabMode中的GameObject既不是Instance也不是Asset
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // 预制体资源：prefabAsset = prefabStage.prefabContentsRoot
                savePath = prefabStage.assetPath;
            }

            return savePath;
        }


        /// <summary>
        /// 临时图片处理
        /// </summary>
        private static void ImageHandle(string savePath, GameObject instance)
        {
            if (!string.IsNullOrEmpty(savePath))
            {
                savePath = Path.GetDirectoryName(savePath);
            }

            if (instance)
            {
                var images = instance.GetComponentsInChildren<Image>(true);
                foreach (var image in images)
                {
                    if (!image.sprite) continue;
                    string imagePath = AssetDatabase.GetAssetPath(image.sprite);
                    if (imagePath.Contains(TEMP_FOLDER))
                    {
                        Debug.LogError("当前预制体中包含临时资源，物体名字：" + image.name + "，资源路径：" + imagePath);
                    }

                    //如果图片在临时资源目录，这个预制体放在临时目录下,则移到和预制体一起
                    if (!string.IsNullOrEmpty(savePath) && savePath.Contains(TEMP_FOLDER) &&
                        Path.GetDirectoryName(imagePath) == TEMP_IMG_FOLDER)
                    {
                        var fileName = Path.GetFileName(imagePath);
                        var newImgPath = savePath + "\\" + fileName;
                        if (!File.Exists(newImgPath))
                        {
                            FileUtil.MoveFileOrDirectory(imagePath, newImgPath);
                            FileUtil.MoveFileOrDirectory(imagePath + ".meta", newImgPath + ".meta");
                            Debug.Log($"将文件：{imagePath} 移动到了{newImgPath}");
                        }

                        var newImg = (Sprite) AssetDatabase.LoadAssetAtPath(newImgPath, typeof(Sprite));
                        if (newImg)
                        {
                            image.sprite = newImg;
                        }
                    }
                }
            }
        }


        //--监听“资源即将被保存”事件
        // public static string[] OnWillSaveAssets(string[] paths)
        // {
        //     if (paths != null)
        //     {
        //         Debug.Log("资源即将被保存 path :" + string.Join(",", paths));
        //         Debug.Log(paths.Length);
        //         foreach (var path in paths)
        //         {
        //             Debug.Log(path);
        //         }
        //     }
        //
        //     return paths;
        // }

        // //--监听“资源即将被移动”事件
        // public static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        // {
        //     Debug.Log("资源即将被移动 form:" + oldPath + " to:" + newPath);
        //     return AssetMoveResult.DidNotMove;
        // }
        //
        // //--监听“资源即将被删除”事件
        // public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        // {
        //     Debug.Log("资源即将被删除 : " + assetPath);
        //     return AssetDeleteResult.DidNotDelete;
        // }


        // AssetDatabase.LoadAssetAtPath(newImgPath, typeof(Texture2D));
        // TextureImporter textureImporter = AssetImporter.GetAtPath(newImgPath) as TextureImporter;
        // if (textureImporter)
        // {
        //     textureImporter.textureType = TextureImporterType.Sprite;
        //     textureImporter.spriteImportMode = SpriteImportMode.Single;
        //     textureImporter.spritePivot = new Vector2(0.5f, 0.5f);
        //     textureImporter.spritePixelsPerUnit = 100;
        // }
        // AssetDatabase.ImportAsset(newImgPath, ImportAssetOptions.ForceUpdate);

    }
}