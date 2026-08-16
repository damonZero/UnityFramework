using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 预制体校准工具
    /// </summary>
    public class PrefabAdjustUtil
    {


        /// <summary>
        /// 自适应子节点大小
        /// </summary>
        /// <param name="go"></param>
        public static void FitChildSize(RectTransform parentRT)
        {
            if (parentRT == null) return;
            
            List<RectTransform> children = new List<RectTransform>();

            // 记录原始世界坐标
            Dictionary<RectTransform, Vector3> originalPositions = new Dictionary<RectTransform, Vector3>();
        
            for (var i = 0; i < parentRT.childCount; i++)
            {
                var child = parentRT.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf) continue;
                children.Add(child);
                originalPositions[child] = child.position; // 记录原始世界坐标
            }

            if (children.Count == 0) return;

            // 计算包围盒
            Vector3[] corners = new Vector3[4];
            children[0].GetWorldCorners(corners);
            Rect totalRect = GetWorldRect(corners);

            foreach (RectTransform child in children)
            {
                child.GetWorldCorners(corners);
                totalRect = Encapsulate(totalRect, GetWorldRect(corners));
            }

            // 获取子节点包围盒中心
            Vector3 childrenCenter = totalRect.center;

            // 移动父节点到中心点，需要考虑父节点的对齐方式
            parentRT.position = new Vector3(
                childrenCenter.x + parentRT.rect.width * (parentRT.pivot.x - 0.5f),
                childrenCenter.y + parentRT.rect.height * (parentRT.pivot.y - 0.5f),
                parentRT.position.z
                );
            parentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalRect.width);
            parentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalRect.height);

            // 恢复子节点原始世界坐标
            foreach (var kvp in originalPositions)
            {
                kvp.Key.position = kvp.Value;
            }
            
            // 强制重建布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
        }

        static Rect GetWorldRect(Vector3[] corners)
        {
            return new Rect(corners[0], corners[2] - corners[0]);
        }

        static Rect Encapsulate(Rect a, Rect b)
        {
            Vector2 min = Vector2.Min(a.min, b.min);
            Vector2 max = Vector2.Max(a.max, b.max);
            return new Rect(min, max - min);
        }
        
        
        
    }
}