// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Framework.View
{
    /// <summary>
    /// View的生命周期循环流程接口，仅提供给流程控制/管理器调用
    ///
    /// 注意：应用层不要直接调用这些接口
    /// </summary>
    public interface IViewLifeCycle
    {
        UniTask ExecuteOpen(LifeCycleArgs args);
        UniTask ExecuteClose(LifeCycleArgs args);
        object ExecuteSave();
        void ExecuteClear();
    }

    /// <summary>
    /// 视图View所属的生命周期驱动器，用于驱动执行Close/Hide/Show等行为
    /// </summary>
    public interface IViewLifeCycleExecutor
    {
        /// <summary>
        /// 关闭View
        /// </summary>
        UniTask LifeCycleExecuteClose(IViewLifeCycle view, LifeCycleArgs args);
    }


    /// <summary>
    /// 引起生命周期改变的原因
    /// </summary>
    [Flags]
    public enum LifeCycleCause
    {
        None = 0,
        Open = 1,       // View未打开时：执行打开View
        Close = 2,      // View处于打开状态：执行关闭View
        Requested = 4,  // 应用层主动请求的操作（比如主动请求关闭View，区分于系统层因为关闭/重启等原因触发的操作）

        RequestedClose = Close | Requested, // 应用层主动请求的关闭View
    }

    public struct LifeCycleArgs
    {
        public CancellationToken CancelToken { get; }

        public LifeCycleCause Cause { get; }

        public object Data { get; set; }

        public bool IsOpen => Cause.HasFlag(LifeCycleCause.Open);
        public bool IsClose => Cause.HasFlag(LifeCycleCause.Close);

        /// <summary>
        /// 是否为应用层主动请求执行的操作
        ///
        /// 比如主动请求关闭View，区分于系统层因为关闭/重启等原因触发的操作
        /// </summary>
        public bool IsRequested => Cause.HasFlag(LifeCycleCause.Requested);

        public LifeCycleArgs(LifeCycleCause cause,
            object data = null, CancellationToken cancelToken = default)
        {
            Data = data;
            CancelToken = cancelToken;
            Cause = cause;
        }

        public override string ToString()
        {
            return $"Cause:{Cause}, Data:{Data ?? "null"}";
        }
    }
}
