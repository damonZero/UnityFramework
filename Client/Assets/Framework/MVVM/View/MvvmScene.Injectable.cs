using Framework.Log;
using Framework.DependencyInjection;

namespace Framework.MVVM
{
    public abstract class MvvmScene<TInjectable> : MvvmScene where TInjectable : class, IInjectable
    {
        private TInjectable _injectable;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _injectable = null;
        }

        /// <summary>
        /// 泛型ViewModel实例（懒加载）
        /// </summary>
        protected TInjectable Model => _injectable ??= GetInjectable<TInjectable>();
    }
}
