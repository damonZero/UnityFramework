using Boot;
using MessagePipe;
using UnityEngine.Scripting;

namespace Project.Bootstrap
{
    [Preserve]
    public sealed class ProjectBootstrapStage : IBootstrapStage
    {
        public int Priority => 300;
        public string StageName => "Project";

        public void Configure(BootstrapContext context)
        {
            var options = context.GetRequired<MessagePipeOptions>();
            ProjectBootstrapper.Configure(context.Builder, options);
        }
    }
}
