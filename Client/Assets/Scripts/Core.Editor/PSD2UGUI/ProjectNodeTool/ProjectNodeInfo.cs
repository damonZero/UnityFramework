//************************************************************************
//Create by Liangc on 2019/10/30
//
//@Description  项目节点信息类
//************************************************************************

using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 项目节点信息类
    /// </summary>
    public class ProjectNodeInfo
    {
        //预制体节点
        public GameObject prefabObj;

        //节点名
        public string name;

        //节点路径
        public string path;

        //节点缩略图
        public Texture thumbnail;

        //节点详细图
        public Texture detailImage;

        //节点绘制信息
        public GUIContent content;

        //作者
        public string author;

        //说明
        public string description;

        //时间
        public string time;

        //搜索关键字
        public bool Search(string keyword, bool searchName, bool searchAut, bool searchDes)
        {
            bool hasName = false, hasAut = false, hasDes = false;
            if (searchName && !string.IsNullOrEmpty(name))
                hasName = name.ToLower().Contains(keyword.ToLower());
            if (searchAut && !string.IsNullOrEmpty(author))
                hasAut = author.ToLower().Contains(keyword.ToLower());
            if (searchDes && !string.IsNullOrEmpty(description))
                hasDes = description.ToLower().Contains(keyword.ToLower());
            return hasName || hasAut || hasDes;
        }
    }
}