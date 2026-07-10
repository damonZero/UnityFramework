using System;
using System.Collections.Generic;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Plan;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 策略标志 —— 描述 Stage 的执行特性。
    /// </summary>
    [Flags]
    public enum BuildStagePolicy
    {
        None = 0,

        /// <summary>必须执行，不可被用户跳过</summary>
        Required = 1 << 0,

        /// <summary>可选执行</summary>
        Optional = 1 << 1,

        /// <summary>始终运行（忽略指纹检查）</summary>
        AlwaysRun = 1 << 2,

        /// <summary>不允许跳过（即使指纹匹配，Formal/Audit 用）</summary>
        NoSkip = 1 << 3,

        /// <summary>需要事务系统保护（修改项目状态前 snapshot）</summary>
        Transactional = 1 << 4,

        /// <summary>产出产物（需要写入 artifact manifest）</summary>
        ProducesArtifacts = 1 << 5,

        /// <summary>需要 Unity 主线程</summary>
        RequiresUnityMainThread = 1 << 6,
    }
}
