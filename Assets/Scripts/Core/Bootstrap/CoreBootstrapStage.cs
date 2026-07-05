namespace Core.Bootstrap
{
    public static class CoreBootstrapStage
    {
        public static void Configure(CoreStartupContext context)
        {
            context.MessagePipeOptions = context.Builder.RegisterCoreServices(context.AssetRuntime);
        }
    }
}
