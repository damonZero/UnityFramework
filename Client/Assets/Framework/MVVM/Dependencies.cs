using Framework.Log;
using VContainer;
using VContainer.Unity;

namespace Framework.MVVM
{
    public static class Dependencies
    {
        public static LifetimeScope Scope => DependencyInjection.Dependencies.Scope;

        public static IObjectResolver Resolver => Scope.Container;
    }
}
