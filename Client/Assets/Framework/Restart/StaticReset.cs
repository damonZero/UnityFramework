using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Framework.Restart
{
    /// <summary>
    /// 软重启静态变量重置执行器（纯 C# 反射，零 Unity 依赖，可直接单测）。
    ///
    /// 规则（约定 + 强制检查）：
    /// <list type="bullet">
    /// <item><c>const</c>（IsLiteral）→ 跳过。</item>
    /// <item><c>static readonly</c>（IsInitOnly）→ 跳过（基础设施）。</item>
    /// <item>可变 <c>static</c> → 重置为 <c>default</c>（引用类型 null，值类型零值）。</item>
    /// <item><c>[SoftRestartField(SoftRestartAction.DoNotReset)]</c> → 跳过。</item>
    /// <item><c>[SoftRestartField(initialValue: x)]</c> → 重置为 x。</item>
    /// </list>
    /// </summary>
    public static class StaticReset
    {
        /// <summary>重置单个类型的所有可变静态字段（供单测直接调用）。</summary>
        public static void Reset(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // 类型级跳过：[SoftRestartClass(DoNotReset)] 表示该类型的静态实例跨重启保留，不枚举其任何字段。
            var classAttr = type.GetCustomAttribute<SoftRestartClassAttribute>();
            if (classAttr != null && classAttr.StaticInstanceAction == SoftRestartAction.DoNotReset)
                return;

            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsLiteral) continue;   // const
                if (field.IsInitOnly) continue;  // static readonly（基础设施，自动保留）

                var attr = field.GetCustomAttribute<SoftRestartFieldAttribute>();
                if (attr != null)
                {
                    if (attr.Action == SoftRestartAction.DoNotReset) continue;
                    if (attr.HasInitialValue)
                    {
                        SetField(field, attr.InitialValue);
                        continue;
                    }
                }

                // 默认：重置为 default。
                SetField(field, field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null);
            }
        }

        /// <summary>
        /// 重置 KJ 全部热更游戏层的可变静态字段。扫描范围由命名空间决定（KJ「命名空间=目录路径」约定）：
        /// Framework./Core/General/Project，天然排除 Boot（重启协调器自身）、Launcher（AOT）、第三方与 Unity 内置。
        /// </summary>
        public static void ResetAll()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ShouldSkipAssembly(assembly.GetName().Name))
                    continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    if (type.IsDefined(typeof(CompilerGeneratedAttribute), false)) continue;
                    if (!IsTargetNamespace(type.Namespace)) continue;
                    Reset(type);
                }
            }
        }

        /// <summary>性能黑名单：跳过明显非 KJ 的程序集，避免枚举其海量类型（正确性由命名空间过滤兜底）。</summary>
        private static bool ShouldSkipAssembly(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            if (name.Contains("Editor")) return true;
            if (name.Contains("TestKit")) return true;

            return name.StartsWith("UnityEngine", StringComparison.Ordinal)
                || name.StartsWith("UnityEditor", StringComparison.Ordinal)
                || name.StartsWith("Unity.", StringComparison.Ordinal)
                || name.StartsWith("System", StringComparison.Ordinal)
                || name == "mscorlib"
                || name.StartsWith("netstandard", StringComparison.Ordinal)
                || name.StartsWith("Mono.", StringComparison.Ordinal)
                || name.StartsWith("Microsoft", StringComparison.Ordinal)
                || name.StartsWith("Cysharp", StringComparison.Ordinal)
                || name.StartsWith("VContainer", StringComparison.Ordinal)
                || name.StartsWith("MessagePipe", StringComparison.Ordinal)
                || name.StartsWith("UniTask", StringComparison.Ordinal)
                || name.StartsWith("ZLinq", StringComparison.Ordinal)
                || name.StartsWith("ZString", StringComparison.Ordinal)
                || name.StartsWith("ZLogger", StringComparison.Ordinal)
                || name.StartsWith("YooAsset", StringComparison.Ordinal)
                || name.StartsWith("HybridCLR", StringComparison.Ordinal)
                || name.StartsWith("Google", StringComparison.Ordinal)
                || name.StartsWith("Newtonsoft", StringComparison.Ordinal)
                || name.StartsWith("Sirenix", StringComparison.Ordinal)
                || name.StartsWith("E7.", StringComparison.Ordinal)
                || name.StartsWith("Odin", StringComparison.Ordinal)
                || name.StartsWith("NUnit", StringComparison.Ordinal)
                || name.StartsWith("nunit", StringComparison.Ordinal)
                || name.StartsWith("YamlDotNet", StringComparison.Ordinal);
        }

        private static bool IsTargetNamespace(string ns)
        {
            if (string.IsNullOrEmpty(ns)) return false;
            return ns == "Core" || ns.StartsWith("Core.", StringComparison.Ordinal)
                || ns == "General" || ns.StartsWith("General.", StringComparison.Ordinal)
                || ns == "Project" || ns.StartsWith("Project.", StringComparison.Ordinal)
                || ns == "Framework" || ns.StartsWith("Framework.", StringComparison.Ordinal);
        }

        private static void SetField(FieldInfo field, object value)
        {
            field.SetValue(null, value);
        }
    }
}
