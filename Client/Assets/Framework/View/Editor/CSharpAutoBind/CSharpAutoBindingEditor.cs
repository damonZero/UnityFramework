// CSharp 自动绑定注入配置编辑器

using System;
using UnityEditor;
using UnityEngine;

namespace Framework.View.Editor
{
    [CustomEditor(typeof(CSharpAutoBindingConfig))]
    public class CSharpAutoBindingEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 前缀->类型映射的注册入口，由应用层（Core.Editor）注入实现。
        /// </summary>
        public static IAutoBindingRegister Register { get; set; }

        public override void OnInspectorGUI()
        {
            var config = target as CSharpAutoBindingConfig;

            if (config == null)
            {
                Debug.LogError($"目标对象不是 {nameof(CSharpAutoBindingConfig)} 类型！");
                return;
            }

            var helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            var registerType = Register != null ? Register.GetType() : typeof(IAutoBindingRegister);
            GUILayout.Label(
                $"请通过 {registerType} 进行新类型注册！！！\n" +
                "并通过下方的 'Update Bindings' 按钮更新绑定信息，避免手动输入错误。",
                helpBoxStyle
            );

            EditorGUILayout.LabelField("Bindings (Read-Only):");
            foreach (var binding in config.Bindings)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Prefix  :   {binding.Prefix}", GUILayout.Width(150));
                GUILayout.Label($"Type  :   {binding.FullTypeName}");
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Update Bindings"))
            {
                UpdateBindings();
            }
        }

        private void UpdateBindings()
        {
            if (Register == null)
            {
                Debug.LogError($"未注册，请注入实例：{nameof(CSharpAutoBindingEditor)}.{nameof(Register)}！");
                return;
            }

            var config = target as CSharpAutoBindingConfig;

            if (config == null)
            {
                Debug.LogError("目标对象不是 CSharpAutoBindingConfig 类型！");
                return;
            }

            config.Bindings.Clear();

            foreach (var (prefix, type) in Register.PrefixTypeDict)
            {
                var exists = config.Bindings.Exists(b => b.Prefix == prefix);
                if (exists)
                {
                    Debug.LogError($"前缀 {prefix} 已存在，跳过更新！");
                    continue;
                }

                if (type == null)
                {
                    Debug.LogError($"类型为 null：{prefix}，跳过！");
                    continue;
                }

                var fullTypeName = type.FullName;
                if (string.IsNullOrEmpty(fullTypeName))
                {
                    Debug.LogError($"无法获取类型的完整命名空间：{type.Name}，跳过！");
                    continue;
                }

                config.Bindings.Add(new CSharpAutoBindingConfig.BindingCfg
                {
                    Prefix = prefix,
                    FullTypeName = fullTypeName
                });
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
