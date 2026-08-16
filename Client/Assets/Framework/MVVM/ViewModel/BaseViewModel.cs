using Framework.Log;
using System;

namespace Framework.MVVM
{
    public abstract class BaseViewModel : MvvmBaseModel, IAutoInjectVm
    {
        //注意： 仅提供给《动态类型》调用，确定类型直接构造函数注入
        /// <summary>
        /// 获取指定类型的BaseViewModel实例
        /// </summary>
        /// <param name="type">BaseViewModel的子类</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static BaseViewModel GetInjectViewModel(Type type)
        {
            if (!typeof(BaseViewModel).IsAssignableFrom(type))
                throw new ArgumentException($"{type.FullName} is not a subclass of {nameof(BaseViewModel)}");

            var flag = Dependencies.Resolver.TryResolve(type, out var instance);
            return flag ? (BaseViewModel)instance : null;
        }

        /// <summary>
        /// 提供给并未注入到容器中的类型进行使用，外部注意不要控制其生命周期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetInjectViewModel<T>() where T : BaseViewModel
        {
            return GetInjectModel<T>();
        }

    }
}
