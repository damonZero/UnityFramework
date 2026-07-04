using System;
using System.Collections.Generic;
using System.Reflection;
using Framework.Event;
using MessagePipe;
using VContainer;
using ZLinq;

namespace General
{
    public static class GeneralContainerRegistration
    {
        private static readonly MethodInfo RegisterMessageBrokerMethod =
            typeof(MessagePipe.ContainerBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .AsValueEnumerable()
                .First(m =>
                    m.Name == "RegisterMessageBroker" &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1);

        public static void RegisterBusinessLayer(this IContainerBuilder builder, MessagePipeOptions options, params Assembly[] assemblies)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var scanAssemblies = assemblies?.AsValueEnumerable().Where(a => a != null).Distinct().ToArray() ?? Array.Empty<Assembly>();
            RegisterBusinessEvents(builder, options, scanAssemblies);
            RegisterModels(builder, scanAssemblies);
            RegisterModelLifecycle(builder);
        }

        private static void RegisterModelLifecycle(IContainerBuilder builder)
        {
            if (!builder.Exists(typeof(ModelLifecycle)))
                builder.Register<ModelLifecycle>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
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
                         .AsValueEnumerable()
                         .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<ModelAttribute>() != null))
            {
                if (!typeof(IModel).IsAssignableFrom(type))
                    throw new InvalidOperationException($"[Model] type must implement IModel: {type.FullName}");

                if (!builder.Exists(type))
                    builder.Register(type, Lifetime.Singleton).AsSelf().As<IModel>();
            }
        }

        private static Type[] GetLoadableTypes(Assembly[] assemblies)
        {
            var result = new List<Type>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    result.AddRange(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var type in e.Types)
                    {
                        if (type != null)
                            result.Add(type);
                    }
                }
            }

            return result.ToArray();
        }
    }
}
