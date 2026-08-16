//************************************************************************
//Create by Liangc on 2019/10/30
//
//@Description  项目节点文件夹类
//************************************************************************
using System.Collections.Generic;
using System.IO;

namespace Package.PSD2UGUI
{
    public class ProjectNodeInfoFolder
    {
        //父文件夹
        public ProjectNodeInfoFolder parentFolder;

        //文件夹信息
        public readonly DirectoryInfo directoryInfo;

        //子node集合
        public readonly List<ProjectNodeInfo> childNodes;

        //子文件夹集合
        public readonly List<ProjectNodeInfoFolder> childFolders;

        //是否展开
        public bool isOpen;

        //是否为空
        public bool Empty => (childNodes == null || childNodes.Count == 0) &&
                             (childFolders == null || childFolders.Count == 0);

        public ProjectNodeInfoFolder(ProjectNodeInfoFolder parent, DirectoryInfo directory)
        {
            parentFolder = parent;
            directoryInfo = directory;
            childNodes = new List<ProjectNodeInfo>();
            childFolders = new List<ProjectNodeInfoFolder>();
        }

        //查找符合条件的节点
        public void SearchNode(Dictionary<ProjectNodeInfo, ProjectNodeInfoFolder> collect,
            string keyword, bool searchName, bool searchAut, bool searchDes)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            foreach (var node in childNodes)
            {
                if (node.Search(keyword, searchName, searchAut, searchDes))
                    collect.Add(node, this);
            }

            foreach (var folder in childFolders)
            {
                folder.SearchNode(collect, keyword, searchName, searchAut, searchDes);
            }
        }

        //打开关闭批量处理
        public void OpenOrClose(bool open)
        {
            OpenOrClose(this, open);
        }

        //打开处理
        private static void OpenOrClose(ProjectNodeInfoFolder folder, bool open)
        {
            folder.isOpen = open;
            foreach (var childFolder in folder.childFolders)
            {
                OpenOrClose(childFolder, open);
            }
        }

        //打开所有父节点
        public static void OpenParent(ProjectNodeInfoFolder folder)
        {
            folder.isOpen = true;
            if (folder.parentFolder != null)
                OpenParent(folder.parentFolder);
        }
    }
}