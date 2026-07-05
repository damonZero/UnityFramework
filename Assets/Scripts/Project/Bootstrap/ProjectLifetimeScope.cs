using Core.Bootstrap;
using Framework.Asset;
using General;
using VContainer;
using VContainer.Unity;

namespace Project.Bootstrap
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        internal static IAssetRuntime PendingBootAssetRuntime { get; set; }

        protected override void Configure(IContainerBuilder builder)
        {
            var context = new CoreStartupContext(builder)
            {
                AssetRuntime = PendingBootAssetRuntime
            };
            PendingBootAssetRuntime = null;

            CoreBootstrapStage.Configure(context);
            GeneralBootstrapStage.Configure(context);
            ProjectBootstrapStage.Configure(context);
        }
    }
}
