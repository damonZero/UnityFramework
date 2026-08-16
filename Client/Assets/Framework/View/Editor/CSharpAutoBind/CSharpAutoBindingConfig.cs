// 自动绑定，对应关系配置

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.View.Editor
{
    [CreateAssetMenu(fileName = "CSharpAutoBindingConfig", menuName = "ScriptableObjects/CSharpAutoBindingConfig")]
    public class CSharpAutoBindingConfig : ScriptableObject
    {
        [Serializable]
        public struct BindingCfg
        {
            [Tooltip("前缀，例如：_go")] public string Prefix;

            [Tooltip("带命名空间的完整类型，例如：UnityEngine.GameObject")]
            public string FullTypeName;
        }

        public List<BindingCfg> Bindings = new();

        public Dictionary<string, Type> GetPrefixTypeDict()
        {
            var dict = new Dictionary<string, Type>();
            foreach (var pair in Bindings)
            {
                var fullTypeName = pair.FullTypeName;
                var type = Type.GetType(fullTypeName);
                if (type == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var temp = assembly.GetType(fullTypeName);
                        if (temp != null)
                        {
                            type = temp;
                        }
                    }
                }

                if (type == null)
                {
                    Debug.LogError($"Can't find type {fullTypeName}");
                    continue;
                }

                dict[pair.Prefix] = type;
            }

            return dict;
        }
    }
}
