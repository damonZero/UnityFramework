using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public static class Json2PrefabCommonPrefabPluginMgr
    {
        private static readonly Dictionary<string, IJson2PrefabCommonPrefabPlugin> _dictCommonPrefabPlugin;

        static Json2PrefabCommonPrefabPluginMgr()
        {
            _dictCommonPrefabPlugin = new Dictionary<string, IJson2PrefabCommonPrefabPlugin>();
            var iPsdPrefabCreates = Psd2UguiTool.FindInterfaceSubclass<IJson2PrefabCommonPrefabPlugin>();
            foreach (var subclass in iPsdPrefabCreates)
            {
                //抽象基类不实例化, 只实例化叶子插件类
                if (subclass.IsAbstract)
                    continue;

                var instance = (IJson2PrefabCommonPrefabPlugin)Activator.CreateInstance(subclass);
                foreach (var name in instance.Names)
                {
                    if (_dictCommonPrefabPlugin.TryAdd(name, instance)) continue;
                    Debug.LogError("重复的通用预制体插件名字: " + name + " 请检查");
                }
            }
        }

        /// <summary>
        /// 公共组件定制化解析入口(主路径, 实例化根节点)
        /// 设置尺寸/位置, 调用插件定制化逻辑, 并复用内嵌子公共组件的定制化解析
        /// </summary>
        /// <param name="node">PSD节点</param>
        /// <param name="instance">刚实例化的公共组件</param>
        public static bool CustomizedCheck(PsdNodeBase node, GameObject instance)
        {
            var prefabName = Psd2UguiTool.GetCommonPrefabName(node.name);
            var tr = instance.transform as RectTransform;
            if (!_dictCommonPrefabPlugin.TryGetValue(prefabName, out var plugin))
            {
                Json2PrefabFactory.SetPosWhByNode(tr, node);
                return Psd2UguiEditor._instance.replaceDel;
            }

            try
            {
                Psd2UguiStatistics.UseCommonPrefab(prefabName);
                Json2PrefabFactory.SetSizeBySelfLockEnum(tr, node, plugin.UseSelfSize);
                Json2PrefabFactory.SetPosBySelfLockEnum(tr, node, plugin.UseSelfPosition);
                plugin.CustomizedCheck(node, instance);

                //复用内嵌子公共组件的定制化解析(递归), 插件可覆写 AutoReuseEmbedded 关闭
                if (plugin.AutoReuseEmbedded)
                    ProcessEmbeddedCommonPrefabs(node, instance);

                return plugin.IsInterruption;
            }
            catch (Exception e)
            {
                //TODO 接飞书通知
                Debug.LogError(e);
                return false;
            }
        }

        /// <summary>
        /// 容器命名后缀约定: 名字以 _容器 结尾的 Group 视为布局容器
        /// 美术在 PSD 中建 XXX_容器 组自由摆放子节点, 预制体内放同名节点, 导出时自动填充
        /// </summary>
        private const string CONTAINER_SUFFIX = "_容器";

        /// <summary>
        /// 处理内嵌的公共组件子节点
        /// 遍历PSD子节点, 只有Group才可能是公共组件, 命中则复用其定制化解析并自然递归子孙,
        /// 未命中的普通分组沿名字定位继续深入(内部可能嵌套公共组件)
        /// </summary>
        /// <param name="node">PSD节点</param>
        /// <param name="instance">已实例化的外层预制体(Z)</param>
        private static void ProcessEmbeddedCommonPrefabs(PsdNodeBase node, GameObject instance)
        {
            if (node?.ChildrenNodes is not { Count: > 0 } || instance == null)
                return;

            foreach (var childNode in node.ChildrenNodes)
            {
                if (childNode == null) continue;

                //只有Group才可能是公共组件, 其他类型直接跳过
                if (childNode.NodeType != PsdNodeEnum.Group) continue;

                //容器识别: 名字以 _容器 结尾的 Group 视为布局容器, 填充其子节点
                if (childNode.name.EndsWith(CONTAINER_SUFFIX))
                {
                    Json2PrefabCommonPrefabAssembler.FillContainer(childNode, instance);
                    continue;
                }

                var childPrefabName = Psd2UguiTool.GetCommonPrefabName(childNode.name);
                if (!_dictCommonPrefabPlugin.ContainsKey(childPrefabName))
                {
                    //普通分组(非公共组件), 沿名字定位继续深入, 内部可能嵌套公共组件实例
                    var sub = instance.transform.Find(childNode.name);
                    if (sub != null)
                        ProcessEmbeddedCommonPrefabs(childNode, sub.gameObject);
                    continue;
                }

                //改名免疫匹配: 找 prefab 内来源名 == 公共组件名的实例根
                var childTr = Json2PrefabCommonPrefabAssembler.FindEmbeddedInstance(instance.transform, childNode.name);
                if (childTr != null)
                    RunPluginReuse(childNode, childTr.gameObject);
            }
        }

        /// <summary>
        /// 复用单个公共组件的定制化解析(不重新实例化, 不覆盖尺寸位置)
        /// 内嵌子组件(A/B/C)不自带缩放, 尺寸/位置/缩放保持预制体内部布局
        /// 复用后自然递归其子孙公共组件
        /// </summary>
        /// <param name="node">PSD节点</param>
        /// <param name="instance">预制体内已有的公共组件实例</param>
        private static void RunPluginReuse(PsdNodeBase node, GameObject instance)
        {
            var prefabName = Psd2UguiTool.GetCommonPrefabName(node.name);
            if (!_dictCommonPrefabPlugin.TryGetValue(prefabName, out var plugin))
                return;

            try
            {
                Psd2UguiStatistics.UseCommonPrefab(prefabName);
                //复用: 只执行定制化逻辑, 尺寸/位置/缩放保持预制体内部布局
                plugin.CustomizedCheck(node, instance);

                //递归子孙公共组件, 插件可覆写 AutoReuseEmbedded 关闭
                if (plugin.AutoReuseEmbedded)
                    ProcessEmbeddedCommonPrefabs(node, instance);
            }
            catch (Exception e)
            {
                //TODO 接飞书通知
                Debug.LogError(e);
            }
        }
    }
}
