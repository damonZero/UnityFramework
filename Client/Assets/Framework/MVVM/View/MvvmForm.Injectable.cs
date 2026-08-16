using Framework.Log;
using Framework.DependencyInjection;

namespace Framework.MVVM
{
    public abstract class MvvmForm<TInjectable> : MvvmForm where TInjectable : class, IInjectable
    {
        /// <summary>
        /// 泛型ViewModel实例（懒加载）
        /// </summary>
        private TInjectable _injectable;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _injectable = null;
        }

        /// <summary>
        /// 泛型ViewModel实例（懒加载）
        /// </summary>
        protected TInjectable ViewModel => _injectable ??= GetInjectable<TInjectable>();
    }
}
