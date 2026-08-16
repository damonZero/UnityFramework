//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航异常处理管理
//@Description 负责收集和处理导航系统框架层和业务层使用的各种异常，让框架更稳定
//**************************************************************************************

using System;
using System.Collections.Generic;
namespace Framework.View.Navigation
{
    public class NavigationExceptionMgr
    {
        //导航容器列表
        private readonly INavigateContainer _rootContainer;

        //常规操作记录
        private int _lastOperateFrame;

        //常规操作检查帧数
        private readonly int _checkOperateCount = 60;

        public NavigationExceptionMgr(INavigateContainer rootContainer)
        {
            _rootContainer = rootContainer;
            _instance = this;
        }

        private class ExceptionData
        {
            internal NavigationBehaviour behaviour;
            internal Exception exception;
        }

        //异常列表容量上限，超出时丢弃最早的异常
        private const int MAX_EXCEPTIONS = 100;

        //异常
        private readonly List<ExceptionData> _exceptions = new();

        //单例(方便全局调用)
        private static NavigationExceptionMgr _instance;

        //是否为空
        public bool IsEmpty => _exceptions.Count == 0;

        //异常处理事件
        public readonly NavigationEvent<NavigationBehaviour, Exception> onException = new(false);

        /// <summary>
        /// 添加异常
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="exception"></param>
        public static void AddException(INavigateBehaviour behaviour, Exception exception)
        {
            if (_instance == null)
            {
                Log.Error($"NavigationExceptionMgr not initialized, cannot add exception: {exception}");
                return;
            }

            if (_instance._exceptions.Count >= MAX_EXCEPTIONS)
            {
                _instance._exceptions.RemoveAt(0);
            }
            var addData = new ExceptionData()
            {
                behaviour = behaviour as NavigationBehaviour,
                exception = exception
            };
            _instance._exceptions.Add(addData);
        }

        /// <summary>
        /// 记录操作
        /// </summary>
        public static void RecordOperate()
        {
            if (_instance == null) return;
            _instance._lastOperateFrame = UnityEngine.Time.frameCount;
        }

        /// <summary>
        /// 是否存在异常
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static bool HasException(NavigationBehaviour behaviour)
        {
            if (_instance == null) return false;
            return _instance._exceptions.Exists(errData => errData.behaviour == behaviour);
        }

        /// <summary>
        /// 获取异常
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static Exception GetException(NavigationBehaviour behaviour)
        {
            if (_instance == null) return null;
            ExceptionData errData = _instance._exceptions.Find(errData => errData.behaviour == behaviour);
            return errData?.exception;
        }

        public void Update()
        {
#if UNITY_EDITOR
            NoEntrance(_rootContainer);
#endif
            HandleException();
            CheckOperate();
        }

        //施放
        public void Dispose()
        {
            _exceptions.Clear();
            _instance = null;
        }

        //当资源加载错误
        public void OnAssetLoadError(string assetName)
        {
            var lastContainer = _rootContainer.LastContainer();
            NavigationException err;
            if (lastContainer.IsFullScreen())
                err = new NavigationOpenException($"[Navigation]:{lastContainer.Name}加载资源:{assetName}导致显示异常!!");
            else
                err = new NavigationAssetLoadException($"[Navigation]:{lastContainer.Name}加载资源:{assetName}异常!!");
            AddException(lastContainer, err);
        }

        //处理错误
        private void HandleException()
        {
            if (_exceptions.Count == 0)
                return;
            for (int i = _exceptions.Count - 1; i >= 0; i--)
            {
                if (i >= _exceptions.Count) break;
                ExceptionData errData = _exceptions[i];
                //留给业务层处理,方便扩展 //先注释,想清楚各种情况后再处理
                onException.Invoke(errData.behaviour, errData.exception);
            }

            _exceptions.Clear();
        }

        //检查普通操作
        private void CheckOperate()
        {
            //未有操作记录 或 操作间隔小于60帧
            if (onException.Count == 0 || _lastOperateFrame == 0 ||
                UnityEngine.Time.frameCount - _lastOperateFrame < _checkOperateCount)
                return;

            var lastContainer = _rootContainer.LastContainer();
            //不能直接判断最后一个组是否为空,因为前面的组可能显示着
            if (lastContainer.Empty || lastContainer.CurrentState != NavigationStateType.Open)
            {
                Log.Error($"[Navigation]{lastContainer.Name}检查错误,触发保底机制!!");
                onException.Invoke(null, null);
            }

            _lastOperateFrame = UnityEngine.Time.frameCount;
        }

#if UNITY_EDITOR
        //无入口时
        private void NoEntrance(INavigateContainer navigateContainer)
        {
            // Fixme by fred 启用这个代码
            // navigateContainer.ForwardTraversal(group =>
            // {
            //     if (group.LockType != NavigationLockType.None
            //         || group.CurState == NavigationStateType.Show)
            //         return true;
            //     NavigationClearType clearType = group.Cache.ClearType;
            //     if (clearType == NavigationClearType.AllRecover ||
            //         clearType == NavigationClearType.EntranceRecover)
            //     {
            //         //TODO 这里考虑独立在Editor的代码中去写,搞个状态监控模块
            //         if (group.HasEntrance())
            //             return true;
            //         NavigateLog.LogError(
            //             $"【NavigationMgr】:'{group.Name}'没有入口,可能在还原时出问题,请检查你的逻辑");
            //     }
            //
            //     return true;
            // }, false);
        }
#endif
    }
}
