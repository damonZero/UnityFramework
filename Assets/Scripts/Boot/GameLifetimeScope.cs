using KJ.Core;
using VContainer;
using VContainer.Unity;

namespace KJ.Boot
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterCoreServices();
        }
    }
}
