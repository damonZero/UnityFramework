namespace Framework.BuildPipeline.Environment
{
    /// <summary>
    /// 构建环境类型 —— 决定宏定义、日志级别、调试开关、签名策略。
    /// </summary>
    public enum BuildEnvironment
    {
        /// <summary>本地开发 — Development、Debug/Info 日志、GM/Debug UI 可启用</summary>
        Dev = 0,

        /// <summary>QA 测试 — 可按需开启调试</summary>
        QA = 1,

        /// <summary>性能分析 — Profiler 启用、符号保留、Smoke 必须跑</summary>
        Profiling = 2,

        /// <summary>审核包 — 禁用 GM/Debug UI、Development=false、Smoke 必须跑且不可 Skip</summary>
        Audit = 3,

        /// <summary>正式发布 — 全禁用调试、签名必需、Smoke 必须跑且不可 Skip</summary>
        Formal = 4,

        /// <summary>预发布 — 接近 Formal，可按需保留有限诊断</summary>
        Pre = 5,
    }
}
