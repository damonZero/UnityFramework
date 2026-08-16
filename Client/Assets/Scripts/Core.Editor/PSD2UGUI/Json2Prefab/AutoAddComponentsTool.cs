using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    public static class AutoAddComponentsTool
    {
        private const string BUTTON_SUFFIX = "按钮";

        /// <summary>
        /// 自动挂上对应的组件
        /// </summary>
        /// <param name="tr"></param>
        public static void AutoAddComponents(Transform tr)
        {
            for (int i = 0; i < tr.childCount; i++)
            {
                var childTr = tr.GetChild(i);
                AddComponentsBySuffix(childTr);
                AutoAddComponents(childTr);
            }
        }

        private static void AddComponentsBySuffix(Transform tr)
        {
            if (tr.name.EndsWith(BUTTON_SUFFIX))
            {
                AutoAddButtonComponent(tr);
            }
        }

        private static void AutoAddButtonComponent(Transform tr)
        {
            var button = tr.GetAddComponent<Button>();
            button.transition = Selectable.Transition.None;
            // 基础节点：确保存在可点击的 Graphic
            var graphic = tr.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;
        }
    }
}
