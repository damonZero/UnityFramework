using Framework.Asset;
using MessagePipe;
using VContainer;

namespace Core.Bootstrap
{
    public sealed class CoreStartupContext
    {
        public CoreStartupContext(IContainerBuilder builder)
        {
            Builder = builder;
        }

        public IContainerBuilder Builder { get; }
        public MessagePipeOptions MessagePipeOptions { get; set; }
        public IAssetRuntime AssetRuntime { get; set; }
    }
}
