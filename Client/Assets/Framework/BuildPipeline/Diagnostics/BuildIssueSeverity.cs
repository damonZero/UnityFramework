namespace Framework.BuildPipeline.Diagnostics
{
    /// <summary>
    /// 构建问题的严重级别。
    /// </summary>
    public enum BuildIssueSeverity
    {
        /// <summary>信息提示，不影响构建结果</summary>
        Info = 0,

        /// <summary>警告，构建可继续但结果可能不符合预期</summary>
        Warning = 1,

        /// <summary>错误，当前 Stage 失败</summary>
        Error = 2,
    }
}
