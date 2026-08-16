using Framework.Log;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Framework.View
{
    /// <summary>
    /// 异步延迟功能组件
    /// 提供可复用的异步延迟操作实现，支持帧延迟和时间延迟
    /// </summary>
    public class AsyncDelayComponent : IViewActiveComponent
    {
        /// <summary>
        /// 用于管理可取消异步操作的令牌源
        /// 当组件被禁用或销毁时，可以通过此令牌源取消正在进行的异步操作
        /// </summary>
        private CancellationTokenSource _disableCts;

        /// <summary>
        /// 所属的view对象
        /// </summary>
        private readonly ViewObject _owner;

        /// <summary>
        /// 构造异步延迟组件
        /// </summary>
        /// <param name="owner">拥有此组件的MonoBehaviour对象，不能为null</param>
        /// <exception cref="ArgumentNullException">当owner为null时抛出</exception>
        public AsyncDelayComponent(ViewObject owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// 获取用于控制异步操作的取消令牌
        /// 如果当前令牌源不存在或已被取消，会自动创建新的令牌源
        /// </summary>
        /// <remarks>
        /// 此属性用于支持disableCancel参数为true的异步操作
        /// 当组件被禁用时，这些操作可以被取消
        /// </remarks>
        private CancellationToken DisableToken
        {
            get
            {
                if (_disableCts == null || _disableCts.IsCancellationRequested)
                    _disableCts = new CancellationTokenSource();
                return _disableCts.Token;
            }
        }


        public void OnViewEnable()
        {

        }

        /// <summary>
        /// 当拥有者被禁用时调用
        /// 根据各异步操作的disableCancel参数决定是否取消对应的异步操作
        /// </summary>
        public void OnViewDisable()
        {
            if (_disableCts is { IsCancellationRequested: false })
            {
                _disableCts.Cancel();
                _disableCts.Dispose();
                _disableCts = null;
            }
        }

        /// <summary>
        /// 延迟指定帧数后继续执行
        /// </summary>
        /// <param name="delayFrameCount">要延迟的帧数，必须大于0</param>
        /// <param name="disableCancel">是否在组件禁用时取消延迟，默认为true
        /// <para>- true: 组件禁用时会取消延迟</para>
        /// <para>- false: 组件禁用时不会取消延迟，但组件销毁时仍会取消</para>
        /// </param>
        /// <param name="cancelImmediately">取消时是否立即结束，默认为false
        /// <para>- true: 取消时立即结束当前等待</para>
        /// <para>- false: 取消时等待当前帧完成后再结束</para>
        /// </param>
        /// <returns>延迟是否成功完成，如果被取消则返回false</returns>
        /// <example>
        /// <code>
        /// // 等待5帧
        /// bool completed = await DelayFrame(5);
        /// if (completed)
        /// {
        ///     // 延迟成功完成后的操作
        /// }
        /// </code>
        /// </example>
        public async UniTask<bool> DelayFrame(int delayFrameCount, bool disableCancel = true,
            bool cancelImmediately = false)
        {
            return await DelayFrameInternal(delayFrameCount, disableCancel, PlayerLoopTiming.Update, cancelImmediately);
        }

        /// <summary>
        /// 内部使用的帧延迟方法，支持自定义执行时机
        /// </summary>
        /// <param name="delayFrameCount">要延迟的帧数</param>
        /// <param name="disableCancel">是否在组件禁用时取消延迟</param>
        /// <param name="timing">延迟执行的时机，决定在Unity生命周期中的哪个阶段继续执行</param>
        /// <param name="cancelImmediately">取消时是否立即结束</param>
        /// <returns>延迟是否成功完成，如果被取消则返回false</returns>
        /// <exception cref="ArgumentException">当delayFrameCount小于等于0时抛出</exception>
        private async UniTask<bool> DelayFrameInternal(int delayFrameCount, bool disableCancel, PlayerLoopTiming timing,
            bool cancelImmediately)
        {
            if (disableCancel && !_owner.gameObject.activeInHierarchy)
            {
                GameLog.Error("AsyncDelayComponent Delay: owner is disabled", module: "Framework.View");
                return false;
            }

            if (delayFrameCount <= 0) throw new ArgumentException($"frameCount = {delayFrameCount}");

            var token = disableCancel ? DisableToken : _owner.destroyCancellationToken;
            await UniTask.DelayFrame(delayFrameCount, timing, token, cancelImmediately)
                .SuppressCancellationThrow();
            return !token.IsCancellationRequested;
        }

        /// <summary>
        /// 延迟指定毫秒数后继续执行
        /// </summary>
        /// <param name="millisecondsDelay">要延迟的毫秒数，必须大于0</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放，默认为false
        /// <para>- true: 使用真实时间计时，不受Time.timeScale影响</para>
        /// <para>- false: 使用游戏时间计时，受Time.timeScale影响</para>
        /// </param>
        /// <param name="disableCancel">是否在组件禁用时取消延迟，默认为true
        /// <para>- true: 组件禁用时会取消延迟</para>
        /// <para>- false: 组件禁用时不会取消延迟，但组件销毁时仍会取消</para>
        /// </param>
        /// <param name="delayTiming">延迟执行的时机，默认为Update
        /// <para>决定在Unity生命周期中的哪个阶段继续执行</para>
        /// </param>
        /// <param name="cancelImmediately">取消时是否立即结束，默认为false
        /// <para>- true: 取消时立即结束当前等待</para>
        /// <para>- false: 取消时等待当前帧完成后再结束</para>
        /// </param>
        /// <returns>延迟是否成功完成，如果被取消则返回false</returns>
        /// <exception cref="ArgumentException">当millisecondsDelay小于0时抛出</exception>
        /// <example>
        /// <code>
        /// // 等待1秒，忽略时间缩放
        /// bool completed = await Delay(1000, ignoreTimeScale: true);
        /// if (completed)
        /// {
        ///     // 延迟成功完成后的操作
        /// }
        /// </code>
        /// </example>
        public async UniTask<bool> Delay(int millisecondsDelay, bool ignoreTimeScale = false, bool disableCancel = true,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update, bool cancelImmediately = false)
        {
            if (disableCancel && !_owner.gameObject.activeInHierarchy)
            {
                GameLog.Error("AsyncDelayComponent Delay: owner is disabled", module: "Framework.View");
                return false;
            }

            if (millisecondsDelay <= 0) throw new ArgumentException($"millisecondsDelay = {millisecondsDelay}");

            var token = disableCancel ? DisableToken : _owner.destroyCancellationToken;
            await UniTask.Delay(millisecondsDelay, ignoreTimeScale, delayTiming, token, cancelImmediately)
                .SuppressCancellationThrow();
            return !token.IsCancellationRequested;
        }
    }
}
