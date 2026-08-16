using System;
using VContainer;
using VContainer.Unity;

namespace Framework.DependencyInjection
{
    /// <summary>
    /// 环境容器门面（服务定位器）：仅用于「非 DI 管理对象」的按需解析 —— 如 MVVM 反射型动态注入、
    /// Form/Node/Scene 的 ViewModel/Model 解析与子 scope 创建。
    ///
    /// 持有「叶子 scope」：分层启动链 Core → General → Project 依次赋值，最终停在 Project 叶子
    /// （VContainer 子 scope 向上回溯父链，叶子可解析全量注册）。因此调用方无需关心「当前属于哪一层」，
    /// 一律以叶子为父容器解析 / 建子 scope 即可；编译期 asmdef 已隔离层级，不会越层解析。
    ///
    /// 外部一律走 Resolve / TryResolve / ResolveOrDefault，不要直接读 Scope。
    /// </summary>
    public static class Dependencies
    {
        /// <summary>
        /// 叶子 scope（环境容器）。仅由分层启动链 Entrypoint 赋值（Core/General/Project 各一次）；
        /// 外部不要直接读，用下面的封装方法。
        /// </summary>
        public static LifetimeScope Scope { get; set; }

        /// <summary>解析指定类型（动态类型）。容器未就绪时抛异常（表示用早了）。</summary>
        public static object Resolve(Type type) => Scope.Container.Resolve(type);

        /// <summary>解析指定类型。容器未就绪时抛异常（表示用早了）。</summary>
        public static T Resolve<T>() => Scope.Container.Resolve<T>();

        /// <summary>尝试解析指定类型（动态类型）。容器未就绪或未注册时返回 false。</summary>
        public static bool TryResolve(Type type, out object instance)
        {
            if (Scope == null) { instance = null; return false; }
            return Scope.Container.TryResolve(type, out instance);
        }

        /// <summary>尝试解析指定类型。容器未就绪或未注册时返回 false。</summary>
        public static bool TryResolve<T>(out T instance)
        {
            if (Scope == null) { instance = default; return false; }
            return Scope.Container.TryResolve<T>(out instance);
        }

        /// <summary>解析指定类型，未注册时返回 default（容器未就绪也返回 default）。</summary>
        public static T ResolveOrDefault<T>()
        {
            return Scope == null ? default : Scope.Container.ResolveOrDefault<T>();
        }

        /// <summary>
        /// 以叶子 scope 为父创建子容器（Form/Node/Scene 各自的 scope）。父容器恒为叶子：
        /// 叶子可解析全量注册，且编译期 asmdef 已隔离层级，故无需按层指定父容器。
        /// </summary>
        public static LifetimeScope CreateChild(Action<IContainerBuilder> installation)
            => Scope.CreateChild(installation);
    }
}
