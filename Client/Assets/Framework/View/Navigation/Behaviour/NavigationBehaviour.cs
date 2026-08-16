//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航行为基类
//@Description 抽象和封装Container/Form/Scene通用行为的基类,包括"状态、锁、事件"等
//**************************************************************************************

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.Pool;
using Framework.View;
using Framework.View.Navigation;
using UnityEngine;
namespace Framework.View.Navigation
{
    public abstract class NavigationBehaviour : INavigateBehaviour
    {
        #region 常量

        /// <summary>
        /// 等待 PendingState 清零的最大帧数，超时后强制继续
        /// </summary>
        private const int PENDING_STATE_WAIT_MAX_FRAMES = 120; // ~2秒@60fps

        #endregion

        #region public: 属性

        /// <summary>
        /// 当前状态
        /// </summary>
        public NavigationStateType CurrentState { get; private set; } = NavigationStateType.None;

        /// <summary>
        /// 即将改变为的状态，用于在改变过程中记录目标状态、标识处于改变过程中
        /// </summary>
        public NavigationStateType PendingState { get; private set; } = NavigationStateType.None;

        /// <summary>
        /// 锁类型
        /// </summary>
        public NavigationLockType LockType { get; set; } = NavigationLockType.None;

        /// <summary>
        /// 状态变化前监听器
        /// </summary>
        public NavigationEvent<NavigationBehaviour, NavigationStateType> BeforeStateChangeEvent { get; } = new ();

        /// <summary>
        /// 状态变化后监听器
        /// </summary>
        public NavigationEvent<NavigationBehaviour, NavigationStateType> AfterStateChangeEvent { get; } = new ();

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;

#if DEBUG_MODE || UNITY_EDITOR
                if (value != null) _lastNameForDebug = value;
#endif
            }
        }

        /// <summary>
        /// 场景/界面占用内存
        /// </summary>
        public abstract int Memory { get; protected set; }

        /// <summary>
        /// 是否逻辑可见
        /// </summary>
        public abstract bool LogicalVisible { get; }

        /// <summary>
        /// 是否被渲染
        /// </summary>
        public abstract bool Rendering { get; }

        /// <summary>
        /// 是否在转场中
        /// </summary>
        public abstract bool Transitioning { get; }

        #endregion

        #region protected: 属性

        protected CancellationTokenSource OpenCancellation { get; set; }

        protected Queue<StateWaiter> StateWaiters { get; } = new();

        protected readonly struct StateWaiter
        {
            public int Id { get; }
            public NavigationStateType TargetState { get; }

#if UNITY_EDITOR
            /// <summary>
            /// Editor下记录创建时的调用堆栈，用于调试定位来源
            /// </summary>
            public string StackTrace { get; }
#endif

            public StateWaiter(int id, NavigationStateType targetState)
            {
                Id = id;
                TargetState = targetState;
#if UNITY_EDITOR
                StackTrace = new System.Diagnostics.StackTrace(2, true).ToString();
#endif
            }
        }

        #endregion

        #region private: 字段

        /// <summary>
        /// 自增的等待者ID，用于区分同一目标状态的不同请求
        /// </summary>
        private int _nextWaiterId;

        /// <summary>
        /// 代次计数器，每次CancelAllWaiters自增，用于通知正在轮询的等待者退出
        /// </summary>
        private int _waiterGeneration;

        /// <summary>
        /// 当前behaviour的名字
        /// </summary>
        private string _name;

#if DEBUG_MODE || UNITY_EDITOR
        /// <summary>
        /// debug模式下记录名字的最后一次非null赋值，避免被重置为null后日志中丢失关键信息
        /// </summary>
        private string _lastNameForDebug;
#endif

        #endregion

        #region public: 方法

        /// <summary>
        /// 主动关闭
        /// </summary>
        /// <returns>是否关闭成功</returns>
        public virtual UniTask<bool> Close(CancellationToken cancellationToken = default)
        {
            var param = new LifeCycleArgs(LifeCycleCause.Close, cancelToken: cancellationToken);
            return LifeCycleExecuteCloseState(param);
        }

        /// <summary>
        /// 设置可见状态（隐藏/显示）
        /// </summary>
        /// <param name="visible"></param>
        /// <returns></returns>
        public abstract UniTask SetLogicalVisible(bool visible);

        /// <summary>
        /// 是否为全屏
        /// </summary>
        /// <returns></returns>
        public abstract bool IsFullScreen();

        /// <summary>
        /// 是否全屏且逻辑可见
        /// </summary>
        public abstract bool FullScreenAndLogicalVisible();

        /// <summary>
        /// 是否全屏且正在渲染
        /// </summary>
        public abstract bool FullScreenAndRendering();

        /// <summary>
        /// 重置
        /// </summary>
        public virtual void Reset()
        {
            Name = null;
            AfterStateChangeEvent.Clear();
            BeforeStateChangeEvent.Clear();
            CurrentState = NavigationStateType.None;
            PendingState = NavigationStateType.None;
            LockType = NavigationLockType.None;
            CancelAllWaiters();
        }

        /// <summary>
        /// 检查是否可以切换到目标状态（综合锁和状态验证）
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <returns></returns>
        public virtual bool CanChangeTo(NavigationStateType targetState)
        {
            if (!IsUnlocked(targetState)) return false;
            return IsStateValid(targetState);
        }

        /// <summary>
        /// 检查针对目标状态是否未被锁定
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <returns></returns>
        public virtual bool IsUnlocked(NavigationStateType targetState)
        {
            //锁状态验证
            if (LockType.HasFlag(NavigationLockType.None))
                return true;

            //包含哪个锁的状态,就不能进行对应的操作
            var passVerify = !LockType.HasFlag(ConvertLockType(targetState));
            if (!passVerify)
            {
                var info = $"当前{GetType()}对象:'{Name}'被锁定:'{LockType}',不允许进行操作:{targetState}";
                Log.Debug(info);
            }

            return passVerify;
        }

        /// <summary>
        /// 状态类型和锁类似转换
        /// </summary>
        /// <param name="stateType"></param>
        /// <returns></returns>
        public NavigationLockType ConvertLockType(NavigationStateType stateType)
        {
            return (NavigationLockType)(1 << (int)stateType);
        }

        /// <summary>
        /// 检查当前状态是否允许切换到目标状态
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <returns></returns>
        public virtual bool IsStateValid(NavigationStateType targetState)
        {
            return true;
        }

        #endregion

        #region protected: 方法

        //添加错误
        protected virtual void OnError(Exception e)
        {
            NavigationExceptionMgr.AddException(this, e);
            Log.Exception(e);
        }

        /// <summary>
        /// 改变状态前
        /// </summary>
        /// <param name="targetState">目标状态</param>
        /// <param name="forceReentry">
        /// 是否允许强制重入。
        /// 若为 true，即使当前状态已是目标状态，依然会执行状态切换流程（设置 PendingState、触发事件），
        /// 适用于需要重复执行某状态下的初始化逻辑且需要状态锁保护的场景（如：重复打开已打开的界面）。
        /// </param>
        protected virtual async UniTask<bool> PreChangeStateAsync(NavigationStateType targetState, bool forceReentry = false)
        {
            // 等待当前状态改变完成（队列化轮询，零GC Alloc，带超时保护）
            // 当PendingState不为None 或 队列中有等待者时，必须排队（防止新调用者插队）
            if (PendingState != NavigationStateType.None || StateWaiters.Count > 0)
            {
                var waiterId = ++_nextWaiterId;
                var generation = _waiterGeneration;
                StateWaiters.Enqueue(new StateWaiter(waiterId, targetState));

                Log.Debug($"{GetType()}对象:'{Name}', targetState:{targetState}, " +
                          $"等待PendingState({PendingState})完成, 队列位置:{StateWaiters.Count}" +
#if UNITY_EDITOR
                          $"\n--- Waiter StackTrace ---\n{StateWaiters.Peek().StackTrace}" +
#endif
                          "");

                var waitFrames = 0;
                while (true)
                {
                    // 被外部取消（如Reset），直接返回
                    if (generation != _waiterGeneration)
                    {
                        return false;
                    }

                    // 当前无进行中的状态变更，且轮到自己
                    if (PendingState == NavigationStateType.None &&
                        StateWaiters.Count > 0 &&
                        StateWaiters.Peek().Id == waiterId)
                    {
                        StateWaiters.Dequeue();
                        break;
                    }

                    await UniTask.Yield();

                    if (++waitFrames >= PENDING_STATE_WAIT_MAX_FRAMES)
                    {
                        // 只有队首等待者才能触发异常机制，避免多个等待者同时触发
                        if (StateWaiters.Count > 0 && StateWaiters.Peek().Id == waiterId)
                        {
                            var errorMsg = $"{GetType()}对象:'{Name}', targetState:{targetState}, " +
                                           $"等待PendingState({PendingState})完成超时({PENDING_STATE_WAIT_MAX_FRAMES}帧)！已死锁，直接抛出异常中断流转。" +
#if UNITY_EDITOR
                                           $"\n--- Waiter StackTrace ---\n{StateWaiters.Peek().StackTrace}" +
#endif
                                           "";
                            Log.Error(errorMsg);
                            Log.Error($"请检查 '{Name}' 相关的业务调用流程是否存在死循环！！！");
                            PendingState = NavigationStateType.None;
                            StateWaiters.Dequeue();
                            throw new NavigationException(errorMsg);
                        }

                        // 非队首等待者：重置计时器，继续等待队首超时后逐个传导
                        waitFrames = 0;
                    }
                }
            }

            if (!CanChangeTo(targetState))
            {
                return false;
            }

            if (CurrentState == targetState && !forceReentry)
            {
                var msg = $"{GetType()}对象:'{Name}'，targetState:{targetState} -> 忽略, " +
                          $"因为CurrentState已经是'{CurrentState}'";
                Log.Debug(msg);
                return false;
            }

            try
            {
                PendingState = targetState;
                await BeforeStateChangeEvent.InvokeAsync(this, targetState);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            return true;
        }

        /// <summary>
        /// 改变状态后
        /// </summary>
        /// <param name="targetState"></param>
        protected virtual async UniTask PostChangeStateAsync(NavigationStateType targetState)
        {
            if (PendingState != targetState)
            {
                string error;

                if (PendingState == NavigationStateType.None)
                {
                    error = $"当前{GetType()}对象:'{Name}'状态:'{CurrentState}', " +
                            $"不允许改变状态为->{targetState}，因为没有执行{nameof(PreChangeStateAsync)}";
                }
                else
                {
                    error = $"当前{GetType()}对象:'{Name}'正在改变状态:'{CurrentState}'->'{PendingState}'," +
                            $"不允许改变状态为->{targetState}";
                }
                Log.Error(error);
                return;
            }

            CurrentState = targetState;
            PendingState = NavigationStateType.None;

            try
            {
                await AfterStateChangeEvent.InvokeAsync(this, targetState);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// 取消所有等待中的状态切换请求
        /// </summary>
        private void CancelAllWaiters()
        {
            _waiterGeneration++;
            StateWaiters.Clear();
        }

        #endregion

        #region protected: 生命周期流程


        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns>是否关闭成功</returns>
        public virtual async UniTask<bool> LifeCycleExecuteCloseState(LifeCycleArgs args)
        {
            var ok = await PreChangeStateAsync(NavigationStateType.Close);
            if (!ok) return false;

            var result = true;
            try
            {
                await LifeCycleDoClose(args);
            }
            catch (Exception e)
            {
                OnError(e);
                result = false;
            }

            await PostChangeStateAsync(NavigationStateType.Close);

            return result;
        }

        protected abstract UniTask LifeCycleDoClose(LifeCycleArgs args);

        /// <summary>
        /// 清理
        /// </summary>
        /// <returns>是否清理成功</returns>
        public virtual async UniTask<bool> Clear()
        {
            var ok = await PreChangeStateAsync(NavigationStateType.Clear);
            if (!ok) return false;

            var result = true;
            try
            {
                DoClear();
            }
            catch (Exception e)
            {
                OnError(e);
                result = false;
            }

            await PostChangeStateAsync(NavigationStateType.Clear);
            return result;
        }

        protected abstract void DoClear();

        #endregion

        public override string ToString()
        {
            // ReSharper disable once RedundantAssignment
            var name = Name;

#if DEBUG_MODE || UNITY_EDITOR
            name = $"{{lastName:{_lastNameForDebug}}}";
#endif
            return
                $"[{name}] ==> " +
                $"Type:{GetType()},LockType:{LockType},Rendering:{Rendering},FullScreen:{IsFullScreen()}";
        }

    }
}
