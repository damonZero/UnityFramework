using Framework.Log;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.Pool;
using UnityEngine;
namespace Framework.View.Navigation
{
    public abstract class TransitionBase : ITransition
    {

        /// <summary>
        /// 转场效果最长持续时间
        ///
        /// 超过最长帧数 && 超过最长时间 => 会强制结束转场效果，避免转场效果异常导致界面无法操作
        /// </summary>
        public static float MaxDurationSeconds { get; set; } = 5f;

        /// <summary>
        /// 转场效果最长持续帧数
        ///
        /// 超过最长帧数 && 超过最长时间 => 会强制结束转场效果，避免转场效果异常导致界面无法操作
        ///
        /// 为什么除了时间还需要用帧数？
        ///     因为Editor环境（以及断点调试）可能会卡顿、反应慢，导致转场效果时间变长，更容易超过最长时间
        /// </summary>
        public static int MaxDurationFrames { get; set; } = 150;

        public virtual bool IsNoOp => false;

        /// <summary>
        /// 是否处于转场过程中
        /// 整体“导航转场生命周期”是否在进行（更宏观）
        /// </summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// 具体“转场效果本身”（如动画、遮罩、特效）是否仍在播放（更微观）
        /// </summary>
        public abstract bool IsEffectRunning { get; }

        /// <summary>
        /// 转场效果开始时间
        /// </summary>
        private float _beginSeconds;

        /// <summary>
        /// 转场效果开始帧
        /// </summary>
        private int _beginFrame;

        public virtual void Start()
        {
            if (IsTransitioning)
            {
                GameLog.Error($"Transition {GetType().Name} is already started. ", module: "Framework.View.Navigation");
                return;
            }
            _beginFrame = Time.frameCount;
            _beginSeconds = Time.realtimeSinceStartup;
            IsTransitioning = true;
        }

        public virtual async UniTask WaitEffectFinished(CancellationToken cancellationToken = default)
        {
            while (IsEffectRunning)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    GameLog.Warn($"Transition {GetType().Name} was cancelled. Forcing completion.", module: "Framework.View.Navigation");
                    return;
                }

                // 容错处理：IsEffectRunning如果一直不结束，可能是转场效果异常了，这时强制结束转场，避免卡死
                if (_beginFrame + MaxDurationFrames < Time.frameCount &&
                    _beginSeconds + MaxDurationSeconds < Time.realtimeSinceStartup)
                {
                    Log.Error($"Transition({GetType().Name}, {nameof(_beginFrame)}:{_beginFrame}, {nameof(_beginSeconds)}:{_beginSeconds}) " +
                              $"exceeded max duration of {MaxDurationFrames} frames (current:{Time.frameCount}) and " +
                              $"{MaxDurationFrames} seconds (current:{Time.realtimeSinceStartup}). Forcing completion.");
                    return;
                }

                await UniTask.Yield();
            }
        }

        public virtual void Stop()
        {
            IsTransitioning = false;
        }

        #region ObjectPool

        public virtual void Reset()
        {
            IsTransitioning =  false;
        }

        public void RecycleToPool()
        {
            NavigationFactory.Instance.Recycle(this);
        }


        #endregion
    }
}
