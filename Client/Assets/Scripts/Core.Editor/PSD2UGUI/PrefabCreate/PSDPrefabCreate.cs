//*****************************************************************************
//Created By Liangc on 2019/6/3
//PSD创建Prefab类
//@Description 负责遍历PSD节点信息,分发对应的事件
//*****************************************************************************

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD创建预制体类
    /// </summary>
    public class PsdPrefabCreate
    {
        private static PsdPrefabCreate _instance;

        public static PsdPrefabCreate Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new PsdPrefabCreate();
                _instance.InterfaceInjection();
                return _instance;
            }
        }

        //预制体创建接口集合
        private List<IPsdPrefabCreate> _prefabCreates;

        /// <summary>
        /// 接口注入
        /// </summary>        
        private void InterfaceInjection()
        {
            Type[] iPsdPrefabCreates = Psd2UguiTool.FindInterfaceSubclass<IPsdPrefabCreate>();
            foreach (Type subclass in iPsdPrefabCreates)
            {
                if (_prefabCreates == null)
                    _prefabCreates = new List<IPsdPrefabCreate>();
                _prefabCreates.Add((IPsdPrefabCreate) Activator.CreateInstance(subclass));
            }
        }

        /// <summary>
        /// 获取预制体生成接口
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        private IPsdPrefabCreate GetPsdPrefabInterface(PsdNodeInfo nodeInfo)
        {
            if (_prefabCreates == null)
                InterfaceInjection();

            foreach (var create in _prefabCreates)
            {
                if (create.NodeType == nodeInfo.nodeType)
                    return create;
            }

            return null;
        }

        /// <summary>
        /// 创建预制体
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <param name="rootNode"></param>
        /// <param name="layerIndex"></param>
        /// <param name="cb">Action</param>
        public StringBuilder CreatePrefab(PsdNodeInfo nodeInfo, Transform rootNode, int layerIndex = 1,
            Action<RectTransform> cb = null)
        {
            if (nodeInfo == null || rootNode == null || !(rootNode is RectTransform))
                return null;
            //当前处理节点
            Transform currentNode = rootNode;
            //当前处理接口实例
            IPsdPrefabCreate iPrefabCreate;
            //警告收集
            StringBuilder warnInfo = new StringBuilder();

            //PS和Unity的显示与层级关系相反,采取倒序遍历
            PsdNodeInfo.SubsequentTraversalNode(nodeInfo,
                (node, layerIdx) =>
                {
                    //获取分发事件的接口
                    iPrefabCreate = null;
                    iPrefabCreate = GetPsdPrefabInterface(node);
                    if (iPrefabCreate == null)
                        return true;

                    //是否继续递归
                    bool isRecursion = true;
                    //根据创建的节点获得下次创建的父节点
                    Transform retNode =
                        iPrefabCreate.PrefabNodeCreate(node, currentNode,
                            rootNode, layerIdx, out isRecursion, warnInfo);
                    //Debug.LogFormat("在:{0}层级下的:{1}节点下生成节点:{2}节点,返回下下级节点是{3}", layerIndex, currentNode.name, node.nodeName, retNode.name);

                    currentNode = retNode;
                    cb?.Invoke(currentNode as RectTransform);
                    return isRecursion;
                }, layerIndex);
            if (currentNode)
            {
                (currentNode as RectTransform).SetRectFull();
            }
            return warnInfo;
        }
    }
}