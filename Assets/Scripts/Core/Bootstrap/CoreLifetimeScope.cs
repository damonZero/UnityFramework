using Core.Bootstrap;
using Framework.Asset;
using VContainer;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 层根 LifetimeScope（分层启动链 Phase 1）。
    /// 应用生命周期内只存在一个 Core root；General/Project 为其 child scope。
    /// Boot 创建的 <see cref="IAssetRuntime"/> 所有权只交接给 Core，Core scope 销毁时由
    /// <see cref="Core.Asset.AssetSystem.Shutdown"/> 关闭。
    /// </summary>
    public sealed class CoreLifetimeScope : LifetimeScope
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
        }
    }
}
