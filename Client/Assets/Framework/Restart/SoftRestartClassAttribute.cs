using System;

namespace Framework.Restart
{
    /// <summary>
    /// 标记类型的软重启重置行为（类型级，控制「该类型的静态实例字段」如何重置）。
    /// 例如 [SoftRestartClass(SoftRestartAction.DoNotReset)] 表示该类型的静态实例跨重启保留。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SoftRestartClassAttribute : Attribute
    {
        /// <summary>该类型静态实例的重置动作。</summary>
        public SoftRestartAction StaticInstanceAction { get; }

        public SoftRestartClassAttribute(SoftRestartAction staticInstanceAction)
        {
            StaticInstanceAction = staticInstanceAction;
        }
    }
}
