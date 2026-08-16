using System;

namespace Framework.Timer
{
    /// <summary>
    /// Timer 对外部能力的静态委托注入点（由 Core 层在启动时赋值）。
    /// Timer 是零依赖纯 C# 模块，不引用 Log / UnityEngine / UniTask。
    /// </summary>
    public static class TimerDependencies
    {
        /// <summary>
        /// 计时器回调抛出异常时调用。未赋值时异常被静默忽略。
        /// </summary>
        public static Action<Exception>? LogError;
    }
}
