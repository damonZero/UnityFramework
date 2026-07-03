using System;
using System.Linq;
using System.Reflection;
using Framework.Event;
using MessagePipe;
using VContainer;

namespace Core.Architecture
{
    /// <summary>
    /// Reflection is limited to registration-time identity discovery.
    /// Runtime dependencies still have to be expressed through constructors.
    /// </summary>
    public static class ArchitectureContainerRegistration
    {
        private static readonly MethodInfo RegisterMessageBrokerMethod =
            typeof(MessagePipe.ContainerBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m =>
                    m.Name == "RegisterMessageBroker" &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1);

        public static void RegisterArchitecture(this IContainerBuilder builder, MessagePipeOptions options, params Assembly[] assemblies)
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
                return new[] { typeof(ArchitectureContainerRegistration).Assembly };

            return assemblies.Where(a => a != null).Distinct().ToArray();
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
                         .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<CoreSystemAttribute>() != null))
            {
                ValidateSystem(type);
                builder.Register(type, Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
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

        private static void ValidateSystem(Type type)
        {
            if (!typeof(ISystem).IsAssignableFrom(type))
                throw new InvalidOperationException($"[CoreSystem] type must implement ISystem: {type.FullName}");

            if (type.Namespace == null || !type.Namespace.StartsWith("Core", StringComparison.Ordinal))
                throw new InvalidOperationException($"[CoreSystem] is reserved for Core architecture types: {type.FullName}");
        }
    }
}
