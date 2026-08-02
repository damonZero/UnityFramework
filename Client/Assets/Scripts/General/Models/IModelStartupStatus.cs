using System.Collections.Generic;

namespace General
{
    /// <summary>
    /// 模型层启动状态查询接口。由 <see cref="ModelLifecycle"/> 实现。
    /// 分层启动时，上层（General/Project）以本接口结果作为"本层是否启动成功"依据，
    /// 失败即阻断下一层创建。
    /// </summary>
    public interface IModelStartupStatus
    {
        /// <summary>本层模型是否已全部加载成功。</summary>
        bool IsLoaded { get; }

        /// <summary>是否存在加载失败的模型。</summary>
        bool HasFailures { get; }

        /// <summary>加载失败的模型类型名列表。</summary>
        IReadOnlyList<string> FailedModelNames { get; }
    }
}
