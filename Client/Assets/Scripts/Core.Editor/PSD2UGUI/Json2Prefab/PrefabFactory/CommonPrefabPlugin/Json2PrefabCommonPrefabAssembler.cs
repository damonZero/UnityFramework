using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 公共组件内嵌内容组装器
    /// 负责公共组件预制体内部节点的填充与查找等纯节点操作:
    ///   - 布局容器填充(XXX_容器)
    ///   - 内嵌公共组件实例查找(改名免疫)
    ///   - 布局控制宽高时为非公共节点添加 LayoutElement
    /// 插件调度与递归编排仍由 Json2PrefabCommonPrefabPluginMgr 负责
    /// </summary>
    public static class Json2PrefabCommonPrefabAssembler
    {
        /// <summary>
        /// 填充布局容器
        /// PSD的 XXX_容器 组内容通过 AssembleRecursion 生成到预制体内同名容器节点下,
        /// 子节点若为公共组件则自动实例化并走定制化解析(含递归), 非公共组件生成普通节点.
        /// 容器是否使用布局由预制体决定:
        ///   - 带 LayoutGroup: 子节点位置由布局接管排列
        ///   - 纯空节点: 子节点按 PSD 位置摆放
        /// </summary>
        /// <param name="containerNode">PSD容器组节点</param>
        /// <param name="instance">已实例化的外层预制体(Z)</param>
        public static void FillContainer(PsdNodeBase containerNode, GameObject instance)
        {
            if (containerNode?.ChildrenNodes is not { Count: > 0 } || instance == null)
                return;

            //在预制体内找同名容器节点
            var containerTr = FindChildByName(instance.transform, containerNode.name);
            if (containerTr == null)
            {
                Debug.LogWarning($"[容器填充] 预制体'{instance.name}'中未找到同名容器节点'{containerNode.name}', 跳过填充");
                return;
            }

            //检测容器布局是否控制了子节点宽高
            var layout = containerTr.GetComponent<HorizontalOrVerticalLayoutGroup>();
            var controlWidth = layout != null && layout.childControlWidth;
            var controlHeight = layout != null && layout.childControlHeight;

            //将容器内每个子节点递归组装到容器下
            //子节点位置由 AssembleRecursion 按 PSD 数据设置:
            //  - 容器带布局: LayoutGroup 会接管排列, 覆盖位置
            //  - 容器不带布局: 保留 PSD 位置, 依赖美术摆放
            foreach (var childNode in containerNode.ChildrenNodes)
            {
                if (childNode == null) continue;

                var childTr = Json2PrefabAssemble.AssembleRecursion(childNode, containerTr);

                //容器布局控制子节点宽高时, 非公共预制体节点无 LayoutElement 会被布局归零,
                //故添加 LayoutElement 并按 PSD 尺寸设置 preferred 宽高
                if (childTr != null && (controlWidth || controlHeight)
                    && !Json2PrefabAssembleTool.IsCommonPrefab(childNode.name))
                {
                    AddLayoutElement(childTr, childNode, controlWidth, controlHeight);
                }
            }
        }

        /// <summary>
        /// 改名免疫匹配: 找 prefab 内来源名 == 公共组件名的实例根
        /// </summary>
        /// <param name="root">外层实例根</param>
        /// <param name="nodeName">PSD节点名</param>
        public static Transform FindEmbeddedInstance(Transform root, string nodeName)
        {
            var commonName = Psd2UguiTool.GetCommonPrefabName(nodeName);
            foreach (var t in CollectPrefabInstanceRoots(root))
            {
                if (SourcePrefabName(t) == commonName)
                    return t;
            }
            return null;
        }

        /// <summary>
        /// 为容器内非公共预制体节点添加 LayoutElement, 按 PSD 尺寸设置 preferred 宽高,
        /// 避免容器布局(childControlWidth/Height)将子节点宽高归零
        /// </summary>
        private static void AddLayoutElement(Transform childTr, PsdNodeBase node,
            bool controlWidth, bool controlHeight)
        {
            var layoutElement = childTr.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = childTr.gameObject.AddComponent<LayoutElement>();

            if (controlWidth)
                layoutElement.preferredWidth = node.size[0];
            if (controlHeight)
                layoutElement.preferredHeight = node.size[1];
        }

        /// <summary>递归查找指定名称的子节点</summary>
        private static Transform FindChildByName(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindChildByName(parent.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// 收集指定节点下的所有预制体实例根节点
        /// 前置过滤: 非实例的普通节点直接跳过, 深入子树覆盖嵌套实例
        /// </summary>
        /// <param name="root">起始节点</param>
        private static List<Transform> CollectPrefabInstanceRoots(Transform root)
        {
            var result = new List<Transform>();
            if (root == null) return result;
            CollectInstanceRootRecursive(root, result);
            return result;
        }

        private static void CollectInstanceRootRecursive(Transform parent, List<Transform> result)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                //前置过滤: 只保留预制体实例根节点
                if (Psd2UguiTool.IsPrefabRoot(child.gameObject))
                    result.Add(child);
                //深入子树, 覆盖嵌套实例
                CollectInstanceRootRecursive(child, result);
            }
        }

        /// <summary>
        /// 取预制体实例的来源预制体资产名(文件名, 不含扩展名)
        /// 优先用原始来源: 嵌套实例才能追踪到真正的 A.prefab 资产, 改名不影响
        /// </summary>
        /// <param name="t">预制体实例根节点</param>
        private static string SourcePrefabName(Transform t)
        {
            var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
            if (original != null) return original.name;

            var source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            return source != null ? source.name : null;
        }
    }
}
