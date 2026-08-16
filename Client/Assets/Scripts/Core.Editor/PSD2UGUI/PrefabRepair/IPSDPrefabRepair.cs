//*****************************************************************************
//Created By Liangc on 2019/6/3
//Prefab修复接口
//@Description 修复预制体时调用接口,插件移植时,根据当前项目需求,自行修改继承此接口的类
//*****************************************************************************
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 预制体修复接口
    /// </summary>
    public interface IPsdPrefabRepair
    {
        /// <summary>
        /// 节点类型
        /// </summary>
        PsdNodeType NodeType { get; }

        /// <summary>
        /// 预制体节点修复
        /// </summary>
        /// <param name="nodeInfo">PSD节点信息</param>
        /// <param name="repairNode">Prefab父节点</param>
        /// <param name="hierarchy">PSD节点层级</param>
        /// <param name="root">画布根节点</param>
        /// <param name="isContinue">是否继续遍历</param>
        /// <returns></returns>
        Transform PrefabNodeRepair(PsdNodeInfo nodeInfo, RectTransform repairNode, Transform root, int hierarchy, out bool isContinue);

        /// <summary>
        /// 是否是相同节点
        /// 用来判断PSD节点和UI节点是否是同一个节点
        /// </summary>
        /// <param name="psdNode">PSD节点</param>
        /// <param name="currentUINode">UI节点</param>
        /// <param name="preciseMatching"></param>
        /// <returns></returns>
        bool IsSameNode(PsdNodeInfo psdNode, RectTransform currentUINode,bool preciseMatching);
    }

}
