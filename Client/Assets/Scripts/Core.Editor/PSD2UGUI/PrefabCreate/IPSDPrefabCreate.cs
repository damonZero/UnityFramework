//*****************************************************************************
//Created By Liangc on 2019/6/3
//PSD创建Prefab接口类
//@Description 创建预制体时调用接口,插件移植时,根据当前项目需求,自行修改继承此接口的类
//*****************************************************************************

using System.Text;
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD预制体生成接口
    /// </summary>
    public interface IPsdPrefabCreate
    {
        /// <summary>
        /// 节点类型
        /// </summary>
        PsdNodeType NodeType { get; }

        /// <summary>
        /// 预制体节点创建
        /// </summary>
        /// <param name="nodeInfo">PSD节点信息</param>
        /// <param name="parent">Prefab父节点</param>
        /// <param name="hierarchy">PSD节点层级</param>
        /// <param name="root">画布根节点</param>
        /// <param name="isRecursion">是否递归</param>
        /// <param name="warnInfo">警告信息</param>
        /// <returns></returns>
        Transform PrefabNodeCreate(PsdNodeInfo nodeInfo, Transform parent, Transform root,
            int hierarchy, out bool isRecursion, StringBuilder warnInfo);

        /// <summary>
        /// 是否继续的回调
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="hierarchy"></param>
        /// <returns></returns>
        bool IsContinue(PsdNodeInfo nodeInfo, int hierarchy);
    }
}