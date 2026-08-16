//************************************************************************
//Create by Liangc on 2022/1/25
// 项目节点相似度信息类
//@Description 计算节点相似度,查找通用节点
//************************************************************************

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    public class ProjectNodeSimilarInfo
    {
        //所有图片
        private List<Image> _images;

        //所有t2d组件
        private List<TextMeshProUGUI> _t2ds;

        public ProjectNodeSimilarInfo(GameObject go)
        {
            _images = new List<Image>();
            Image[] findImages = go.GetComponentsInChildren<Image>();
            foreach (var image in findImages)
            {
                if (image.sprite)
                    _images.Add(image);
            }

            _t2ds = new List<TextMeshProUGUI>(go.GetComponentsInChildren<TextMeshProUGUI>());
        }

        /// <summary>
        /// Jaccard计算相似度
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private float CalculateSimilar(GameObject go)
        {
            ProjectNodeSimilarInfo compare = new ProjectNodeSimilarInfo(go);
            //交集数量
            int intersection = 0;
            for (int i = 0; i < _images.Count; i++)
            {
                for (int j = compare._images.Count - 1; j >= 0; j--)
                {
                    if (_images[i].sprite.name != compare._images[j].sprite.name)
                        continue;
                    compare._images.RemoveAt(j);
                    intersection++;
                    break;
                }
            }

            for (int i = 0; i < _t2ds.Count; i++)
            {
                for (int j = compare._t2ds.Count - 1; j >= 0; j--)
                {
                    //todo c.c.
                    // if (_t2ds[i].textTid != compare._t2ds[j].textTid)
                    //     continue;
                    compare._t2ds.RemoveAt(j);
                    intersection++;
                    break;
                }
            }

            //并集数量
            int union = compare._images.Count + _images.Count + compare._t2ds.Count + _t2ds.Count;
            return intersection * 1.0f / union * 100;
        }

        /// <summary>
        /// 获取对比相似度
        /// </summary>
        /// <param name="elementCount"></param>
        /// <returns></returns>
        private float GetCompareSimilar(int elementCount)
        {
            if (elementCount <= 2)
                return 100;
            if (elementCount <= 4)
                return 80;
            if (elementCount <= 6)
                return 60;
            return 50;
        }

        /// <summary>
        /// 获取相似预制体
        /// </summary>
        /// <returns></returns>
        public List<GameObject> GetSimilarPrefabs()
        {
            //查找有图片引用的预制体
            List<GameObject> imageReference = new List<GameObject>();
            // 原 P33 依赖 Core.AssetReference.Instance.GetPrefabObj 查找图片引用预制体, 本工程剥离

            //获取相似度较高的预制体
            int elementCount = _images.Count + _t2ds.Count;
            float compareSimilar = GetCompareSimilar(elementCount);
            for (int i = imageReference.Count - 1; i >= 0; i--)
            {
                GameObject compareObj = imageReference[i];
                if (CalculateSimilar(compareObj) < compareSimilar)
                    imageReference.RemoveAt(i);
            }

            return imageReference;
        }

        // [MenuItem("Assets/测试预制体相似度查找")]
        // public static void TestFind()
        // {
        //     GameObject findObj = Selection.gameObjects[0];
        //     ProjectNodeSimilarInfo test = new ProjectNodeSimilarInfo(findObj);
        //     List<GameObject> similarObjs = test.GetSimilarPrefabs();
        //     foreach (var similarObj in similarObjs)
        //     {
        //         Debug.Log(similarObj.name, similarObj);
        //     }
        // }
    }
}