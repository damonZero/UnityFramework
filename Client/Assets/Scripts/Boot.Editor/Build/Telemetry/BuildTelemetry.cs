using System;
using Framework.Aop;

namespace Boot.Editor.Build.Telemetry
{
    public static class BuildTelemetry
    {
        public static void Measure(string name, string category, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using var span = AopRuntime.StartSpan(name, category);
            try
            {
                action();
            }
            catch (Exception ex)
            {
                span.Fail(ex);
                throw;
            }
        }

        public static T Measure<T>(string name, string category, Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using var span = AopRuntime.StartSpan(name, category);
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                span.Fail(ex);
                throw;
            }
        }
    }
}
