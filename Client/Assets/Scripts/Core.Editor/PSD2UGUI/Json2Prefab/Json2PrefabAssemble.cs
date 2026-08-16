//组装预制体

using UnityEngine;

namespace Package.PSD2UGUI
{
    public static class Json2PrefabAssemble
    {
        /// <summary>
        /// 组装预制体
        /// </summary>
        /// <param name="rootNode"></param>
        public static void Assemble(PsdNodeRoot rootNode)
        {
            AutoAddComponentsTool.AutoAddComponents(AssembleRecursion(rootNode, null));
        }

        public static Transform AssembleRecursion(PsdNodeBase node, Transform parentTr)
        {
            var nodeTr = CreatePrefab(node, parentTr);
            if (nodeTr == null)
                return null;

            var children = node.ChildrenNodes;
            if (children is not { Count: > 0 })
                return nodeTr;

            foreach (var child in children)
            {
                AssembleRecursion(child, nodeTr);
            }

            return nodeTr;
        }


        private static Transform CreatePrefab(PsdNodeBase node, Transform parent)
        {
            switch (node.NodeType)
            {
                case PsdNodeEnum.Root:
                    return Json2PrefabFactory.CreateRoot(node as PsdNodeRoot);
                case PsdNodeEnum.Group:
                    return Json2PrefabFactory.CreateGroup(node as PsdNodeGroup, parent);
                case PsdNodeEnum.Image:
                    return Json2PrefabFactory.CreateImage(node as PsdNodeImage, parent);
                case PsdNodeEnum.Text:
                    return Json2PrefabFactory.CreateTMP(node as PsdNodeText, parent);
                default:
                    Debug.LogError($"err type:{node.NodeType} node.name: {node.name}");
                    break;
            }

            return null;
        }
    }
}