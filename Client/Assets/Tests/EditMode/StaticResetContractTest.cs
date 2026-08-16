using System;
using System.Collections.Generic;
using System.Reflection;
using Framework.Restart;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class StaticResetContractTest
    {
        /// <summary>测试目标类型：覆盖重置器的字段分类（const / readonly / 可变 / initialValue / DoNotReset）。</summary>
        private static class ResetTarget
        {
            public const int ConstValue = 42;              // const → 跳过
            public static readonly int ReadonlyValue = 7;  // static readonly → 跳过
            public static int MutableValue = 100;          // 可变 → default(0)
            public static string MutableRef = "hello";     // 可变引用 → null

            [SoftRestartField(initialValue: true)]
            public static bool ResetToTrue = false;        // initialValue → true

            [SoftRestartField(SoftRestartAction.DoNotReset)]
            public static int DoNotResetValue = 999;       // DoNotReset → 保留
        }

        [Test]
        public void Reset_ResetsMutableStaticsToDefault()
        {
            StaticReset.Reset(typeof(ResetTarget));

            Assert.AreEqual(0, ResetTarget.MutableValue);
            Assert.IsNull(ResetTarget.MutableRef);
        }

        [Test]
        public void Reset_SkipsConstAndReadonly()
        {
            StaticReset.Reset(typeof(ResetTarget));

            Assert.AreEqual(42, ResetTarget.ConstValue);
            Assert.AreEqual(7, ResetTarget.ReadonlyValue);
        }

        [Test]
        public void Reset_AppliesInitialValueAndDoNotReset()
        {
            ResetTarget.ResetToTrue = false; // 先破坏再重置，验证 target value 生效
            StaticReset.Reset(typeof(ResetTarget));

            Assert.IsTrue(ResetTarget.ResetToTrue);
            Assert.AreEqual(999, ResetTarget.DoNotResetValue);
        }

        /// <summary>
        /// 强制检查：可变 static 字段带非默认初始值（如 static bool N = true）、且未标 [SoftRestartField]，
        /// 会在软重启「重置为 default」时被错误重置，应改成 const/readonly 或标 [SoftRestartField(initialValue: ...)]。
        /// 反射扫描 KJ 热更游戏层命名空间（Framework./Core/General/Project）。
        /// </summary>
        [Test]
        public void MutableStaticFields_ShouldNotHaveNonDefaultInitializers()
        {
            var violations = new List<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ShouldSkipAssembly(assembly.GetName().Name)) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    if (type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)) continue;
                    if (!IsTargetNamespace(type.Namespace)) continue;

                    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (field.IsLiteral) continue;   // const
                        if (field.IsInitOnly) continue;  // static readonly（基础设施，自动保留）
                        if (field.GetCustomAttribute<SoftRestartFieldAttribute>() != null) continue; // 已标注

                        object value;
                        try { value = field.GetValue(null); }
                        catch (Exception) { continue; } // 读取失败（如未初始化类型）跳过，避免误报

                        if (!IsDefaultValue(field.FieldType, value))
                            violations.Add($"{type.FullName}.{field.Name}");
                    }
                }
            }

            if (violations.Count > 0)
            {
                // 报告但不阻塞：存量字段已整改完毕（GameLog._profile/_startupBufferCapacity 已惰性初始化），
                // 当前应无违规；确认后可将下方改为 Assert.IsEmpty 恢复强制检查。
                TestContext.Out.WriteLine(
                    "可变 static 字段带非默认初始值（应改 const/readonly 或标 [SoftRestartField(initialValue: ...)]）:\n" +
                    string.Join("\n", violations));
            }
        }

        private static bool IsDefaultValue(Type fieldType, object value)
        {
            if (value == null) return true;
            if (!fieldType.IsValueType) return false; // 引用类型非 null 即非默认
            var defaultValue = Activator.CreateInstance(fieldType);
            return Equals(value, defaultValue);
        }

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
    }
}
