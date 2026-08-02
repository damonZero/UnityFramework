using System;
using System.Reflection;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// 层间反射启动辅助（分层启动链）。
    /// 封装"按程序集限定名反射调用静态 <c>Start(LifetimeScope)</c>"的通用流程：
    /// 类型解析 → 方法查找 → 签名校验 → 调用 → TargetInvocationException 解包。
    /// 供 Core/General 层入口复用，避免重复反射代码；错误通过回调交给调用方记录本层日志。
    /// </summary>
    public static class LayerStartupReflector
    {
        public enum InvokeResult
        {
            Ok,
            TypeNotFound,
            MethodNotFound,
            SignatureUnsupported,
            InvokeFailed
        }

        /// <summary>
        /// 反射调用目标层 <c>XxxStartup.Start(LifetimeScope)</c>。
        /// </summary>
        /// <param name="typeName">程序集限定名，如 "General.Bootstrap.GeneralStartup, General"。</param>
        /// <param name="parentScope">父 scope，作为参数传给 Start。</param>
        /// <param name="onError">失败回调：(result, typeName, realException)。realException 已解包。</param>
        public static void InvokeStart(
            string typeName,
            LifetimeScope parentScope,
            Action<InvokeResult, string, Exception> onError)
        {
            if (parentScope == null)
                throw new ArgumentNullException(nameof(parentScope));

            try
            {
                var type = Type.GetType(typeName, throwOnError: false);
                if (type == null)
                {
                    onError?.Invoke(InvokeResult.TypeNotFound, typeName, null);
                    return;
                }

                var method = type.GetMethod("Start",
                    BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    onError?.Invoke(InvokeResult.MethodNotFound, typeName, null);
                    return;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(LifetimeScope))
                {
                    onError?.Invoke(InvokeResult.SignatureUnsupported, typeName, null);
                    return;
                }

                method.Invoke(null, new object[] { parentScope });
            }
            catch (Exception e)
            {
                // 反射调用会包 TargetInvocationException，解包记录真实内部异常。
                var real = e is TargetInvocationException tie && tie.InnerException != null
                    ? tie.InnerException
                    : e;
                onError?.Invoke(InvokeResult.InvokeFailed, typeName, real);
            }
        }
    }
}
