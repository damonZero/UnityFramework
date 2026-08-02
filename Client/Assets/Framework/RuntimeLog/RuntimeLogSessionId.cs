using System;
using System.Globalization;

namespace Framework.RuntimeLog
{
    public static class RuntimeLogSessionId
    {
        public static string Create(DateTimeOffset startTimeUtc)
        {
            var prefix = startTimeUtc.ToUniversalTime().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            return prefix + "-" + suffix;
        }
    }
}
