using System;
using System.Collections.Generic;
using System.Reflection;
using Framework.Log;

namespace General
{
    /// <summary>
    /// 扫描程序集中带 <see cref="ModelAttribute"/> 且实现 <see cref="IModel"/> 的类型。
    /// 只负责"注册源"的筛选；运行期模型实例的解析由 <see cref="ModelLifecycle"/>
    /// 通过 scoped 类型契约完成（见分层启动计划 §0.2），禁止用
    /// <see cref="System.Linq.Enumerable"/> 聚合注入。
    /// </summary>
    public static class ModelScanner
    {
        public static Type[] ScanModelTypes(Assembly assembly)
        {
            if (assembly == null)
                return Array.Empty<Type>();

            // 一次性启动路径，非热路径（General 未引用 Pool，直接 new 可接受）。
            var result = new List<Type>();
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract &&
                        type.GetCustomAttribute<ModelAttribute>() != null &&
                        typeof(IModel).IsAssignableFrom(type))
                    {
                        result.Add(type);
                    }
                }
            }
            catch (ReflectionTypeLoadException e)
            {
                // 汇总并记录加载失败的具体异常，便于排查被跳过的类型（LoaderExceptions
                // 才是失败根因；e.Types 中失败的项为 null，仅靠 null 过滤看不到原因）。
                Exception firstLoaderEx = null;
                var loaderExCount = 0;
                if (e.LoaderExceptions != null)
                {
                    foreach (var loaderEx in e.LoaderExceptions)
                    {
                        if (loaderEx == null)
                            continue;

                        if (firstLoaderEx == null)
                            firstLoaderEx = loaderEx;
                        loaderExCount++;
                    }
                }

                if (loaderExCount > 0)
                {
                    GameLog.Warn(
                        $"[ModelScanner] 类型扫描失败，跳过不可加载类型（{loaderExCount} 个加载异常，首个: {firstLoaderEx.GetType().Name}: {firstLoaderEx.Message}）",
                        "General.ModelScanner");
                }

                foreach (var type in e.Types)
                {
                    if (type == null || !type.IsClass || type.IsAbstract ||
                        type.GetCustomAttribute<ModelAttribute>() == null ||
                        !typeof(IModel).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    result.Add(type);
                }
            }

            return result.ToArray();
        }
    }
}
