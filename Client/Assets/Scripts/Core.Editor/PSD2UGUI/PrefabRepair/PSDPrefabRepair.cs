//*****************************************************************************
//Created By Liangc on 2019/6/3
//Prefab修复类
//@Description 负责遍历PSD节点信息,并分发修复事件
//*****************************************************************************
using System;
using UnityEngine;
using System.Collections.Generic;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD修复预制体类
    /// </summary>
    public class PsdPrefabRepair : PsdPrefabBase
    {
        private static PsdPrefabRepair _instance;
        public static PsdPrefabRepair Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new PsdPrefabRepair();
                _instance.InterfaceInjection();
                return _instance;
            }
        }

        //修复选项(位置/图片/文字)
        private bool _repairRect = true, _repairImage = true, _repairText = true, _preciseMatching = true;

        //预制体修复接口集合
        private List<IPsdPrefabRepair> _prefabRepairs;

        /// <summary>
        /// 接口注入
        /// </summary>        
        public void InterfaceInjection()
        {
            Type[] iPsdPrefabRepairs = Psd2UguiTool.FindInterfaceSubclass<IPsdPrefabRepair>();
            foreach (Type subclass in iPsdPrefabRepairs)
            {
                if (_prefabRepairs == null)
                    _prefabRepairs = new List<IPsdPrefabRepair>();

                _prefabRepairs.Add((IPsdPrefabRepair)Activator.CreateInstance(subclass));
            }
        }

        /// <summary>
        /// 获取预制体修复接口
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        public IPsdPrefabRepair GetPsdPrefabInterface(PsdNodeInfo nodeInfo)
        {
            if (_prefabRepairs == null)
                InterfaceInjection();

            foreach (var repair in _prefabRepairs)
            {
                if (repair.NodeType == nodeInfo.nodeType)
                    return repair;
            }

            return null;
        }

        /// <summary>
        /// 设置修复模式
        /// </summary>
        /// <param name="repairRect"></param>
        /// <param name="repairImage"></param>
        /// <param name="repairText"></param>
        /// <param name="preciseMatching"></param>
        public void SetRepairOption(bool repairRect,
            bool repairImage, bool repairText, bool preciseMatching)
        {
            _repairRect = repairRect;
            _repairImage = repairImage;
            _repairText = repairText;
            _preciseMatching = preciseMatching;
        }

        /// <summary>
        /// 预制体修复
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="rootNode"></param>
        public void RepairPrefab(PsdNodeInfo nodeInfo, Transform rootNode)
        {
            //修复位置/引用的图片/文字属性
            if (nodeInfo == null || rootNode == null || !(rootNode is RectTransform))
                return;

            RectTransform rectRootNode = (RectTransform) rootNode;

            PsdNodeInfo.SubsequentTraversalNode(nodeInfo,
                (currentPsdNode, currentPsdIndex) =>
                {
                    bool isContinue = true;

                    //查找修复接口
                    IPsdPrefabRepair prefabRepair = GetPsdPrefabInterface(currentPsdNode);
                    if (prefabRepair == null)
                        return true;

                    //匹配UI预制体相同的节点
                    RectTransform findRectTrans = FindUINode(currentPsdNode, rectRootNode, prefabRepair.IsSameNode, _preciseMatching);
                    if (!findRectTrans)
                        return true;

                    //修复位置
                    if (_repairRect && findRectTrans)
                        RepairRect(currentPsdNode, findRectTrans, rectRootNode);

                    //修复
                    if (findRectTrans)
                    {
                        //图片/文字修复模式限制
                        if ((prefabRepair.NodeType == PsdNodeType.Image && _repairImage) ||
                        (prefabRepair.NodeType == PsdNodeType.Text && _repairText))
                            prefabRepair.PrefabNodeRepair(currentPsdNode, findRectTrans, rectRootNode, currentPsdIndex, out isContinue);

                        //节点/按钮修复
                        if (prefabRepair.NodeType == PsdNodeType.CommonNode || prefabRepair.NodeType == PsdNodeType.ButtonNode)
                            prefabRepair.PrefabNodeRepair(currentPsdNode, findRectTrans, rectRootNode, currentPsdIndex, out isContinue);
                    }

                    //非首层如果是嵌套预制体,停止遍历
                    if (currentPsdIndex != 1 && Psd2UguiTool.IsPrefabRoot(findRectTrans.gameObject))
                        isContinue = false;

                    return true;
                });

        }


    }

}
