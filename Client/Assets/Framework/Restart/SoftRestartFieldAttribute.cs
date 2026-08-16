using System;

namespace Framework.Restart
{
    /// <summary>
    /// 标记静态字段的软重启重置行为（仅标注字段级例外，默认无需标注）。
    ///
    /// 约定（默认，零标注）：
    /// <list type="bullet">
    /// <item><c>const</c> / <c>static readonly</c> → 自动跳过（不变值 / 基础设施）。</item>
    /// <item>可变 <c>static</c> → 自动重置为 <c>default</c>。</item>
    /// </list>
    ///
    /// 本特性仅用于两种罕见场景：
    /// <list type="bullet">
    /// <item>可变 static 带非默认起始值，需重置回指定目标值 → <c>[SoftRestartField(initialValue: true)]</c>。</item>
    /// <item>可变 static 必须跨重启保留 → <c>[SoftRestartField(SoftRestartAction.DoNotReset)]</c>。</item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class SoftRestartFieldAttribute : Attribute
    {
        /// <summary>重置动作。</summary>
        public SoftRestartAction Action { get; }

        /// <summary>目标值（仅 <see cref="HasInitialValue"/> 为 true 时有效）。</summary>
        public object InitialValue { get; }

        /// <summary>是否显式指定了目标值（区分「按动作」与「按目标值」两种构造）。</summary>
        public bool HasInitialValue { get; }

        /// <summary>按动作重置（如 <see cref="SoftRestartAction.DoNotReset"/>）。</summary>
        public SoftRestartFieldAttribute(SoftRestartAction action)
        {
            Action = action;
        }

        /// <summary>按目标值重置（可变 static 带非默认起始值的罕见场景）。</summary>
        public SoftRestartFieldAttribute(object initialValue)
        {
            Action = SoftRestartAction.ResetToDefault;
            InitialValue = initialValue;
            HasInitialValue = true;
        }
    }
}
