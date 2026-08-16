//**************************************************************************************
// Create By Copilot on 2026/03/05
// 转场效果接口
// 统一定义导航转场生命周期，支持业务自定义扩展
//**************************************************************************************

using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework.View.Navigation
{
    public interface ITransition
    {
        bool IsNoOp { get; }

        /// <summary>
        /// 是否处于转场过程中
        /// 整体“导航转场生命周期”是否在进行（更宏观）
        /// </summary>
        bool IsTransitioning { get; }

        /// <summary>
        /// 具体“转场效果本身”（如动画、遮罩、特效）是否仍在播放（更微观）
        /// </summary>
        bool IsEffectRunning { get; }

        /// <summary>
        /// 开始
        /// </summary>
        void Start();

        /// <summary>
        /// 等待转场效果结束
        /// </summary>
        UniTask WaitEffectFinished(CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止
        /// </summary>
        void Stop();

        /// <summary>
        /// 回收到所属对象池。
        /// </summary>
        void RecycleToPool();
    }
}
