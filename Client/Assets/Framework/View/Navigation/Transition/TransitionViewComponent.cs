using System.Threading;
using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 作为View的附加组件，驱动转场效果（ITransition实例）
    /// </summary>
    public class TransitionViewComponent : IViewOpenComponent, IViewShowComponent
    {
        public ITransition Transition { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public TransitionViewComponent(ITransition transition, CancellationToken cancellationToken)
        {
            Transition = transition;
            CancellationToken = cancellationToken;
        }

        public void Init(ITransition transition, CancellationToken cancellationToken)
        {
            Transition = transition;
            CancellationToken = cancellationToken;
        }

        public void Reset()
        {
            Transition = null;
            CancellationToken = CancellationToken.None;
        }

        /// <summary>
        /// 手动开始转场效果
        /// </summary>
        public void ManualStart()
        {
            if (!Transition.IsTransitioning)
            {
                Transition.Start();
            }
        }

        public UniTask OnPreOpenAsync(LifeCycleArgs args)
        {
            Log.Debug($"Transition.IsTransitioning = {Transition.IsTransitioning}");
            if (!Transition.IsTransitioning)
            {
                Transition.Start();
            }

            return UniTask.CompletedTask;
        }

        public void OnViewOpen(LifeCycleArgs args)
        {
        }

        public UniTask OnPostOpenAsync(LifeCycleArgs args)
        {
            return UniTask.CompletedTask;
        }

        public UniTask OnPreShowAsync(LifeCycleArgs args)
        {
            Log.Debug($"Transition.IsTransitioning = {Transition.IsTransitioning}");
            if (!Transition.IsTransitioning)
            {
                Transition.Start();
            }

            return UniTask.CompletedTask;
        }

        public void OnViewShow(LifeCycleArgs args)
        {
        }

        public async UniTask OnPostShowAsync(LifeCycleArgs args)
        {
            Log.Debug($"Transition.IsTransitioning = {Transition.IsTransitioning}");

            Log.Debug("waiting for transition effect finished...");
            // 等待转场效果播放完成
            await Transition.WaitEffectFinished(CancellationToken);

            Log.Debug("transition effect finished.");
            Transition.Stop();
        }
    }

}
