using System;
using System.Collections.Generic;
using System.Reflection;
using ZLinq;

namespace Framework.Event
{
    public static class GameEventTypeScanner
    {
        public static IReadOnlyList<Type> FindGameEventTypes(params Assembly[] assemblies)
        {
            var result = new List<Type>();
            foreach (var type in GetLoadableTypes(assemblies?.AsValueEnumerable().Where(a => a != null).Distinct().ToArray() ?? Array.Empty<Assembly>()))
            {
                if (type.GetCustomAttribute<GameEventAttribute>() == null)
                    continue;

                if (!type.IsValueType || type.IsEnum)
                    throw new InvalidOperationException($"[GameEvent] must be applied to a struct: {type.FullName}");

                result.Add(type);
            }

            return result;
        }

        private static IEnumerable<Type> GetLoadableTypes(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.AsValueEnumerable().Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                    yield return type;
            }
        }
    }
}
