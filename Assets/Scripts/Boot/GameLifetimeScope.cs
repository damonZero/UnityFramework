using Core;
using VContainer;
using VContainer.Unity;

namespace Boot
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterCoreServices();
        }
    }
}
