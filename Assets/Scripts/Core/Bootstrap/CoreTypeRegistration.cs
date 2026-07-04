using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Systems;
using Core.Systems.Attributes;
using Framework.Event;
using MessagePipe;
using VContainer;
using ZLinq;

namespace Core.Bootstrap
{
    /// <summary>
    /// Reflection is limited to registration-time identity discovery.
    /// Runtime dependencies still have to be expressed through constructors.
    /// </summary>
    public static class CoreTypeRegistration
    {
        private static readonly MethodInfo RegisterMessageBrokerMethod =
            typeof(MessagePipe.ContainerBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .AsValueEnumerable()
                .First(m =>
                    m.Name == "RegisterMessageBroker" &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1);

        public static void RegisterCoreTypes(this IContainerBuilder builder, MessagePipeOptions options, params Assembly[] assemblies)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var scanAssemblies = NormalizeAssemblies(assemblies);
            RegisterGameEvents(builder, options, scanAssemblies);
            RegisterSystems(builder, scanAssemblies);
        }

        private static Assembly[] NormalizeAssemblies(Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return new[] { typeof(CoreTypeRegistration).Assembly };

            return assemblies.AsValueEnumerable().Where(a => a != null).Distinct().ToArray();
        }

        private static void RegisterGameEvents(IContainerBuilder builder, MessagePipeOptions options, Assembly[] assemblies)
        {
            foreach (var type in GameEventTypeScanner.FindGameEventTypes(assemblies))
            {
                RegisterMessageBrokerMethod.MakeGenericMethod(type).Invoke(null, new object[] { builder, options });
            }
        }

        private static void RegisterSystems(IContainerBuilder builder, Assembly[] assemblies)
        {
            foreach (var type in GetLoadableTypes(assemblies)
                         .AsValueEnumerable()
                         .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<CoreSystemAttribute>() != null))
            {
                ValidateSystem(type);
                builder.Register(type, Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
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

        private static void ValidateSystem(Type type)
        {
            if (!typeof(ISystem).IsAssignableFrom(type))
                throw new InvalidOperationException($"[CoreSystem] type must implement ISystem: {type.FullName}");

            if (type.Namespace == null || !type.Namespace.StartsWith("Core", StringComparison.Ordinal))
                throw new InvalidOperationException($"[CoreSystem] is reserved for Core systems: {type.FullName}");
        }
    }
}
