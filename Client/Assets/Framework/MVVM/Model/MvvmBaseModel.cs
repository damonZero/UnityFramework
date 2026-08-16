using Framework.DependencyInjection;
using Framework.Log;
using System;
using R3;
using VContainer;

namespace Framework.MVVM
{
    public abstract class MvvmBaseModel :  ICompositeDisposable
    {
        #region Static

        //注意： 仅提供给《动态类型》调用，确定类型直接构造函数注入
        /// <summary>
        /// 获取指定类型的BaseModel实例
        /// </summary>
        /// <param name="type">BaseModel的子类</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static MvvmBaseModel GetInjectModel(Type type)
        {
            if (!typeof(MvvmBaseModel).IsAssignableFrom(type))
                throw new ArgumentException($"{type.FullName} is not a subclass of {nameof(MvvmBaseModel)}");

            var flag = Dependencies.TryResolve(type, out var instance);
            return flag ? (MvvmBaseModel)instance : null;
        }

        /// <summary>
        /// 提供给并未注入到容器中的类型进行使用，外部注意不要控制其声明周期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetInjectModel<T>() where T : MvvmBaseModel
        {
            return Dependencies.ResolveOrDefault<T>();
        }


        #endregion


        #region ICompositeDisposable

        private CompositeDisposable _disposables;
        public CompositeDisposable DefaultDisposables => _disposables ??= new CompositeDisposable();

        #endregion

        #region IDisposable

        public virtual void Dispose()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        #endregion
    }
}
