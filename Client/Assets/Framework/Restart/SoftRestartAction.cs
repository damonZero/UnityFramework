namespace Framework.Restart
{
    /// <summary>
    /// 软重启时静态字段的重置动作。
    /// 默认行为（无任何标注）：可变 static 字段重置为 default（引用类型为 null，值类型为零值）。
    /// </summary>
    public enum SoftRestartAction
    {
        /// <summary>重置为 default。这是默认行为，通常无需显式指定。</summary>
        ResetToDefault = 0,

        /// <summary>跳过不重置。仅用于「可变 static 字段必须跨重启保留」的罕见场景。</summary>
        DoNotReset = 1,
    }
}
