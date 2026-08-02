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

        /// <summary>
        /// 注册本层消息事件 Broker。注意：不调用 <see cref="MessagePipe.ContainerBuilderExtensions.RegisterMessagePipe"/>
        /// —— 消息域由 Core scope 统一建立（分层启动计划 §0.1），本层只注册事件。
        /// </summary>
        public static void RegisterBusinessEvents(this IContainerBuilder builder, MessagePipeOptions options, params Assembly[] assemblies)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var scanAssemblies = assemblies?.AsValueEnumerable().Where(a => a != null).Distinct().ToArray() ?? Array.Empty<Assembly>();
            foreach (var type in GameEventTypeScanner.FindGameEventTypes(scanAssemblies))
            {
                RegisterMessageBrokerMethod.MakeGenericMethod(type).Invoke(null, new object[] { builder, options });
            }
        }

        /// <summary>
        /// 注册本层模型（scoped 类型契约，分层启动计划 §0.2）。
        /// 只扫本层程序集；扫描结果注册为 <see cref="IReadOnlyList{Type}"/>，子 scope 注册覆盖父 scope，
        /// 保证每层 ModelLifecycle 只管理本层模型。模型实例注册为 <see cref="IModel"/> + <see cref="AsSelf"/>，
        /// 由 ModelLifecycle 通过 <see cref="VContainer.IObjectResolver"/> 惰性解析。
        /// </summary>
        public static void RegisterModels(this IContainerBuilder builder, params Assembly[] assemblies)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var scanAssemblies = assemblies?.AsValueEnumerable().Where(a => a != null).Distinct().ToArray() ?? Array.Empty<Assembly>();
            var modelTypes = new List<Type>();
            foreach (var type in GetLoadableModelTypes(scanAssemblies))
            {
                modelTypes.Add(type);
                if (!builder.Exists(type))
                    builder.Register(type, Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            }

            // RegisterInstance 默认 Singleton，但注册在各自 scope 的 registry 中，
            // 子 scope 解析优先看自己的注册，所以 General/Project 各自拿到本层的 modelTypes。
            builder.RegisterInstance(modelTypes.ToArray())
                .As<IReadOnlyList<Type>>();
        }

        /// <summary>
        /// 注册 <see cref="ModelLifecycle"/>（每层独立，只管理本层模型）。
        /// </summary>
        public static void RegisterModelLifecycle(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!builder.Exists(typeof(ModelLifecycle)))
                builder.Register<ModelLifecycle>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        }

        private static Type[] GetLoadableModelTypes(Assembly[] assemblies)
        {
            var result = new List<Type>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in ModelScanner.ScanModelTypes(assembly))
                {
                    result.Add(type);
                }
            }

            return result.ToArray();
        }
    }
}
