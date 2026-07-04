using Boot;
using MessagePipe;
using UnityEngine.Scripting;

namespace General
{
    [Preserve]
    public sealed class GeneralBootstrapStage : IBootstrapStage
    {
        public int Priority => 200;
        public string StageName => "General";

        public void Configure(BootstrapContext context)
        {
            var options = context.GetRequired<MessagePipeOptions>();
            context.Builder.RegisterBusinessLayer(options, typeof(GeneralBootstrapStage).Assembly);
        }
    }
}
