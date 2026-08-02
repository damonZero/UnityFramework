namespace Framework.BuildPipeline.CI
{
    /// <summary>
    /// CI 退出码 —— 稳定、可被 CI 系统解析。
    /// </summary>
    public enum BuildExitCode
    {
        /// <summary>成功</summary>
        Success = 0,

        /// <summary>参数/Profile 错误</summary>
        ConfigError = 10,

        /// <summary>Preflight 失败</summary>
        PreflightFailed = 20,

        /// <summary>Generate/HybridCLR 失败</summary>
        GenerateFailed = 30,

        /// <summary>YooAsset 失败</summary>
        AssetFailed = 40,

        /// <summary>Config 事务失败</summary>
        ConfigFailed = 50,

        /// <summary>BuildPlayer/平台工具链失败</summary>
        PlayerFailed = 60,

        /// <summary>Static Verify 失败</summary>
        VerifyFailed = 70,

        /// <summary>Runtime Smoke 失败</summary>
        SmokeFailed = 80,

        /// <summary>Report/Archive 失败</summary>
        ReportFailed = 90,

        /// <summary>未分类异常</summary>
        UnknownError = 99,
    }
}
