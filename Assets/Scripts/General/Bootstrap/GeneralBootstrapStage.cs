using Core.Bootstrap;
using MessagePipe;

namespace General
{
    public static class GeneralBootstrapStage
    {
        public static void Configure(CoreStartupContext context)
        {
            var options = context.MessagePipeOptions;
            if (options == null)
                throw new System.InvalidOperationException("MessagePipeOptions is missing. CoreBootstrapStage must run before GeneralBootstrapStage.");

            context.Builder.RegisterBusinessLayer(options, typeof(GeneralBootstrapStage).Assembly);
        }
    }
}
