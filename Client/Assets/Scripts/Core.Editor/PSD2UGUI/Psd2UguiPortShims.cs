//*****************************************************************************
// PSD2UGUI 移植适配垫片
// 本工程没有 P33 的 Core 工具类(GetAddComponent / UIModelExtensionEditor 空白画布模板)，
// 在此提供等价的最小实现，仅输出基础节点。
//*****************************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// P33 Core 工具类的本地等价实现
    /// </summary>
    internal static class Psd2UguiPortShims
    {
        /// <summary>
        /// 等价 P33 的 GetAddComponent&lt;T&gt;() 扩展：取组件，没有则添加
        /// </summary>
        public static T GetAddComponent<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        /// <summary>
        /// 等价 P33 的 GetAddComponent&lt;T&gt;() 扩展：取组件，没有则添加
        /// </summary>
        public static T GetAddComponent<T>(this Component c) where T : Component
        {
            return c.GetComponent<T>() ?? c.gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 等价 P33 UIModelExtensionEditor.CreatePrefab("blankCanvas")：创建一个空白画布
        /// </summary>
        public static GameObject CreateBlankCanvas()
        {
            var go = new GameObject("Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Psd2UguiRule.UI_STANDARD_SIZE_NEW;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        /// <summary>
        /// 等价 P33 Core.RectTransformEditorExtension.ResetRectTransform：重置 RectTransform 预制属性覆盖
        /// </summary>
        public static void ResetRectTransform(this RectTransform rectTransform)
        {
            using (var serializedObject = new SerializedObject(rectTransform))
            {
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_AnchoredPosition"), InteractionMode.UserAction);
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_LocalPosition"), InteractionMode.UserAction);
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_SizeDelta"), InteractionMode.UserAction);
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_AnchorMin"), InteractionMode.UserAction);
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_AnchorMax"), InteractionMode.UserAction);
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_Pivot"), InteractionMode.UserAction);
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_LocalRotation"), InteractionMode.UserAction);
                PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty("m_LocalScale"), InteractionMode.UserAction);

                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 等价 P33 Core 的 RectTransform.SetRectFull：锚点全拉伸铺满父节点
        /// </summary>
        public static void SetRectFull(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
