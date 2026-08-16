//*****************************************************************************
//Created By Liangc on 2019/8/28
//
//@Description PSD创建Prefab的终止节点类
//*****************************************************************************

using System.Text;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class PsdOverNodeCreate : IPsdPrefabCreate
    {
        public PsdNodeType NodeType => PsdNodeType.OverNode;

        public bool IsContinue(PsdNodeInfo nodeInfo, int hierarchy)
        {
            return true;
        }

        public Transform PrefabNodeCreate(PsdNodeInfo nodeInfo, Transform parent, Transform root,
            int hierarchy, out bool isRecursion, StringBuilder warnInfo)
        {
            isRecursion = true;
            if (parent == null)
                Debug.LogError($"层级错误!!!请美术检查PSD文件中'{nodeInfo?.parentNode.nodeName}'附近文件夹命名");
            return parent.parent;
        }
    }
}