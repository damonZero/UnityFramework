using System.IO;
using Framework.Touch;
using Project.Demo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Editor.Demo
{
    /// <summary>
    /// 生成 DemoForm 预制体到 Assets/GameRes/UI/Project/DemoForm.prefab。
    /// 编辑器加载后若不存在则自动生成；也可通过菜单「KJ/Demo/重新生成 DemoForm 预制体」手动重建。
    /// </summary>
    public static class DemoFormPrefabCreator
    {
        private const string PrefabPath = "Assets/GameRes/UI/Project/DemoForm.prefab";

        [InitializeOnLoadMethod]
        private static void EnsurePrefab()
        {
            if (File.Exists(PrefabPath))
            {
                return;
            }

            // 延迟到编辑器初始化完成后执行，避免 domain reload 期间创建资源。
            EditorApplication.delayCall += CreatePrefab;
        }

        [MenuItem("KJ/Demo/重新生成 DemoForm 预制体")]
        public static void CreatePrefab()
        {
            var dir = Path.GetDirectoryName(PrefabPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 根节点：DemoForm（BaseForm 要求 Canvas）+ RectTransform + Canvas + GraphicRaycaster
            var root = new GameObject("DemoForm",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(DemoForm));
            Stretch(root.GetComponent<RectTransform>());

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 0;

            // 背景
            var bg = CreateUiObject("_imgBg", root.transform, typeof(Image));
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 1f);

            // 标题（命名前缀 _txt 供 VarBind 自动绑定识别）
            var title = CreateUiObject("_txtTitle", root.transform, typeof(Text));
            Anchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 0.8f), new Vector2(600f, 80f));
            var titleText = title.GetComponent<Text>();
            titleText.text = "KJ View 框架 Demo";
            titleText.fontSize = 44;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // 关闭按钮（命名前缀 _btn 供 VarBind 自动绑定识别；BaseButton 需 Image 提供视觉 + raycast）
            var btn = CreateUiObject("_btnClose", root.transform, typeof(Image), typeof(BaseButton));
            Anchor(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.2f), new Vector2(320f, 100f));
            btn.GetComponent<Image>().color = new Color(0.85f, 0.32f, 0.32f, 1f);

            // 按钮文字
            var btnLabel = CreateUiObject("Text", btn.transform, typeof(Text));
            Stretch(btnLabel.GetComponent<RectTransform>());
            var btnText = btnLabel.GetComponent<Text>();
            btnText.text = "关闭";
            btnText.fontSize = 32;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[DemoForm] 预制体已生成: {PrefabPath}");
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
