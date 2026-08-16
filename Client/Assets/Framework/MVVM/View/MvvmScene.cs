using Framework.Log;
using System;
using Framework.View;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework.MVVM
{
    public abstract class MvvmScene : BaseScene, IMvvmView
    {
        #region ICompositeDisposable

        /// <summary>
        /// 默认使用的CompositeDisposable
        /// </summary>
        public virtual CompositeDisposable DefaultDisposables => CloseDisposable;

        #endregion

        #region 生命周期清理

        /// <summary>
        /// OnDisable时，需要执行的清理操作
        ///
        /// 这里没用对象池，因为场景/界面本身就会应该会有缓存机制，这里再用对象池必要性不大
        /// </summary>
        public CompositeDisposable DisableDisposables => _disableDisposables ??= new CompositeDisposable();

        protected CompositeDisposable _disableDisposables;

        /// <summary>
        /// OnClose时，需要执行的清理操作
        /// </summary>
        public CompositeDisposable CloseDisposable => _closeDisposable ??= new CompositeDisposable();

        protected CompositeDisposable _closeDisposable;


        /// <summary>
        /// OnDestroy时，需要执行的清理操作
        /// </summary>
        public CompositeDisposable DestroyDisposables => _destroyDisposables ??= new CompositeDisposable();

        protected CompositeDisposable _destroyDisposables;


        #endregion

        #region 生命周期

        // protected override void Awake()
        // {
        //     base.Awake();
        // }

        protected override void OnDisable()
        {
            base.OnDisable();

            try
            {
                _disableDisposables?.Clear();
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "MVVM lifecycle exception", module: "Framework.MVVM");
            }
            _disableDisposables = null;
        }

        protected override void OnClose(LifeCycleArgs args)
        {
            base.OnClose(args);


            try
            {
                _closeDisposable?.Clear();
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "MVVM lifecycle exception", module: "Framework.MVVM");
            }
            _closeDisposable = null;
        }

        /// <summary>
        /// 当组件被销毁时调用
        /// 确保所有异步操作被取消，防止内存泄漏
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            try
            {
                _destroyDisposables?.Clear();
            }
            catch (Exception e)
            {
                GameLog.Exception(e, "MVVM lifecycle exception", module: "Framework.MVVM");
            }
            _destroyDisposables = null;

            //清理DI数据绑定
            if (_lifetimeScope)
                _lifetimeScope.Dispose();
        }


        #endregion


        #region mvvm绑定清理

        public CompositeDisposable AutoR3Disposables => DefaultDisposables;

        #endregion

        #region DIContainer

        /// <summary>
        /// 获取一个注入的实例，外部缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected static T GetInjectable<T>() where T : class, DependencyInjection.IInjectable
        {
            try
            {
                var success = Dependencies.Resolver.TryResolve<T>(out var result);
                if (!success)
                {
                    // FIXME by fred
                    GameLog.Error(
                        $"GetInjectable<T> Error, <{typeof(T).Name}> Maybe not implement autoInject interface ," +
                        $" if is BaseLogicModule must config in 模块表", module: "Framework.MVVM");
                }

                return result;
            }
            catch (Exception e)
            {
                GameLog.Error($"GetInjectable<T> Error, {e.Message}", module: "Framework.MVVM");
                return null;
            }
        }

        /// <summary>
        /// 获取指定类型的ViewModel实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected BaseViewModel GetViewModel<T>() where T : BaseViewModel
        {
            return GetInjectable<T>();
        }

        /// <summary>
        /// 获取指定类型的Model实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected BaseModel GetModel<T>() where T : BaseModel
        {
            return GetInjectable<T>();
        }

        /// <summary>
        /// DI容器，生命周期
        /// </summary>
        private LifetimeScope _lifetimeScope;

        /// <summary>
        /// 创建一个DI容器,只有界面和场景才有资格创建
        /// </summary>
        /// <param name="installation"></param>
        /// <typeparam name="TParent"></typeparam>
        protected void CreateContainer<TParent>(Action<IContainerBuilder> installation)
            where TParent : LifetimeScope
        {
            if (_lifetimeScope)
            {
                _lifetimeScope.Dispose();
                GameLog.Error($"Container already exists , {GetType()}", module: "Framework.MVVM");
            }

            _lifetimeScope = Dependencies.Scope.CreateChild(installation);
            _lifetimeScope.Container.Inject(this);
        }

        #endregion
    }
}
