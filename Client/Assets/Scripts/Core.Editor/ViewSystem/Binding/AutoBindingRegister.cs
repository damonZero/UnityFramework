using System;
using System.Collections.Generic;
using UnityEditor;

namespace Core.Editor
{
    /// <summary>
    /// 自动绑定前缀注册表（KJ 适配版）。
    /// 在编辑器启动时注入到 Framework.View.Editor.CSharpAutoBindingEditor.Register。
    /// 仅登记 KJ 已具备的类型；随 Touch / 自研 UI 组件 / Spine 等模块落地后逐步扩充。
    /// </summary>
    public class AutoBindingRegister : Framework.View.Editor.IAutoBindingRegister
    {
        [InitializeOnLoadMethod]
        private static void InjectRegister()
        {
            var register = new AutoBindingRegister();
            Framework.View.Editor.CSharpAutoBindingEditor.Register = register;
        }

        public Dictionary<string, Type> PrefixTypeDict { get; } = new()
        {
            // Object / 资源类型
            { "_obj", typeof(UnityEngine.Object) },
            { "_go", typeof(UnityEngine.GameObject) },
            { "_mat", typeof(UnityEngine.Material) },
            { "_spr", typeof(UnityEngine.Sprite) },
            { "_sha", typeof(UnityEngine.Shader) },
            { "_mesh", typeof(UnityEngine.Mesh) },
            { "_rdt", typeof(UnityEngine.RenderTexture) },
            { "_anc", typeof(UnityEngine.AnimationClip) },
            { "_adc", typeof(UnityEngine.AudioClip) },

            // 通用 Component 类型
            { "_tr", typeof(UnityEngine.Transform) },
            { "_rt", typeof(UnityEngine.RectTransform) },
            { "_ca", typeof(UnityEngine.Camera) },
            { "_am", typeof(UnityEngine.Animator) },
            { "_ps", typeof(UnityEngine.ParticleSystem) },
            { "_lgt", typeof(UnityEngine.Light) },
            { "_sre", typeof(UnityEngine.SpriteRenderer) },
            { "_lr", typeof(UnityEngine.LineRenderer) },
            { "_cl", typeof(UnityEngine.Collider) },

            // UGUI 类型
            { "_tf", typeof(UnityEngine.UI.Text) },
            { "_img", typeof(UnityEngine.UI.Image) },
            { "_rmg", typeof(UnityEngine.UI.RawImage) },
            { "_tg", typeof(UnityEngine.UI.Toggle) },
            { "_sd", typeof(UnityEngine.UI.Slider) },
            { "_sb", typeof(UnityEngine.UI.Scrollbar) },
            { "_dd", typeof(UnityEngine.UI.Dropdown) },
            { "_ipf", typeof(UnityEngine.UI.InputField) },
            { "_usr", typeof(UnityEngine.UI.ScrollRect) },
            { "_hlg", typeof(UnityEngine.UI.HorizontalLayoutGroup) },
            { "_vlg", typeof(UnityEngine.UI.VerticalLayoutGroup) },
            { "_csf", typeof(UnityEngine.UI.ContentSizeFitter) },

            // Touch 交互类型（Framework.Touch 已移植）
            { "_btn", typeof(Framework.Touch.BaseButton) },
            { "_bd", typeof(Framework.Touch.BaseDrag) },
            { "_bm", typeof(Framework.Touch.BaseMove) },
            { "_bst", typeof(Framework.Touch.BaseSelect) },
            { "_bsp", typeof(Framework.Touch.BaseSlip) },
            { "_di", typeof(Framework.Touch.UIDragItem) },
            { "_dis", typeof(Framework.Touch.UIDragItemSlot) },

            // TextMeshPro 类型（com.unity.textmeshpro 已引入）
            { "_tmp", typeof(TMPro.TMP_Text) },
            { "_t2d", typeof(TMPro.TextMeshPro) },
            { "_t3d", typeof(TMPro.TextMeshProUGUI) },
            { "_tdd", typeof(TMPro.TMP_Dropdown) },
            { "_tipf", typeof(TMPro.TMP_InputField) },
        };
    }
}
