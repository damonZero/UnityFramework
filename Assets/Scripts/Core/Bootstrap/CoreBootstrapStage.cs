using Boot;
using UnityEngine.Scripting;

namespace Core.Bootstrap
{
    [Preserve]
    public sealed class CoreBootstrapStage : IBootstrapStage
    {
        public int Priority => 100;
        public string StageName => "Core";

        public void Configure(BootstrapContext context)
        {
            var options = context.Builder.RegisterCoreServices();
            context.Set(options);
        }
    }
}
