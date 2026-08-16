using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.Pool;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 空转场（Null Object）
    /// </summary>
    public sealed class TransitionNoOp : ITransition
    {
        public bool IsNoOp => true;

        public bool IsTransitioning => false;

        public bool IsEffectRunning => false;

        public void Start()
        {
        }

        public UniTask WaitEffectFinished(CancellationToken cancellationToken = default)
        {
            return UniTask.CompletedTask;
        }

        public void Stop()
        {
        }

        public void Reset()
        {
        }

        public void RecycleToPool()
        {
        }
    }
}
