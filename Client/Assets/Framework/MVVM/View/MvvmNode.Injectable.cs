using Framework.Log;
using Framework.DependencyInjection;

namespace Framework.MVVM
{
    public class MvvmNode<TInjectable> : MvvmNode where TInjectable : class, IInjectable
    {
        /// <summary>
        /// 泛型 ViewModel实例（懒加载）
        /// </summary>
        private TInjectable _injectable;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _injectable = null;
        }

        /// <summary>
        /// 泛型 ViewModel实例（懒加载）
        /// </summary>
        protected TInjectable ViewModel => _injectable ??= GetInjectable<TInjectable>();
    }
}
