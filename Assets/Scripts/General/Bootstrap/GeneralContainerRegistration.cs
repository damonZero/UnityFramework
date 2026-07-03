using System;
using System.Linq;
using System.Reflection;
using Framework.Event;
using MessagePipe;
using VContainer;

namespace General
{
    public static class GeneralContainerRegistration
    {
        private static readonly MethodInfo RegisterMessageBrokerMethod =
            typeof(MessagePipe.ContainerBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m =>
                    m.Name == "RegisterMessageBroker" &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1);

        public static void RegisterBusinessLayer(this IContainerBuilder builder, MessagePipeOptions options, params Assembly[] assemblies)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var scanAssemblies = assemblies?.Where(a => a != null).Distinct().ToArray() ?? Array.Empty<Assembly>();
            RegisterBusinessEvents(builder, options, scanAssemblies);
            RegisterModels(builder, scanAssemblies);
            builder.Register<ModelLifecycle>(Lifetime.Singleton);
        }

        private static void RegisterBusinessEvents(IContainerBuilder builder, MessagePipeOptions options, Assembly[] assemblies)
        {
            foreach (var type in GameEventTypeScanner.FindGameEventTypes(assemblies))
            {
                RegisterMessageBrokerMethod.MakeGenericMethod(type).Invoke(null, new object[] { builder, options });
            }
        }

        private static void RegisterModels(IContainerBuilder builder, Assembly[] assemblies)
        {
            foreach (var type in GetLoadableTypes(assemblies)
                         .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<ModelAttribute>() != null))
            {
                if (!typeof(IModel).IsAssignableFrom(type))
                    throw new InvalidOperationException($"[Model] type must implement IModel: {type.FullName}");

                builder.Register(type, Lifetime.Singleton).AsSelf().As<IModel>();
            }
        }

        private static Type[] GetLoadableTypes(Assembly[] assemblies)
        {
            return assemblies.SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.Where(t => t != null);
                }
            }).ToArray();
        }
    }
}
