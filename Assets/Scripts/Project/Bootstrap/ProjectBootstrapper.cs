using System.Reflection;
using Framework.Log;
using General;
using MessagePipe;
using VContainer;

namespace Project.Bootstrap
{
    /// <summary>
    /// Project layer container hook. It only depends on General and external DI packages.
    /// </summary>
    public static class ProjectBootstrapper
    {
        public static void Configure(IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterBusinessLayer(options, Assembly.GetExecutingAssembly());
            GameLog.Info("[ProjectBootstrapper] Project layer container registration ready");
        }
    }
}
