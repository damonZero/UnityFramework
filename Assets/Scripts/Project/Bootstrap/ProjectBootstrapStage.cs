using Core.Bootstrap;
using MessagePipe;

namespace Project.Bootstrap
{
    public static class ProjectBootstrapStage
    {
        public static void Configure(CoreStartupContext context)
        {
            var options = context.MessagePipeOptions;
            if (options == null)
                throw new System.InvalidOperationException("MessagePipeOptions is missing. CoreBootstrapStage must run before ProjectBootstrapStage.");

            ProjectBootstrapper.Configure(context.Builder, options);
        }
    }
}
