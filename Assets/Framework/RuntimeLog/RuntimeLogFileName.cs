using System;
using System.Globalization;
using System.Text;

namespace Framework.RuntimeLog
{
    public static class RuntimeLogFileName
    {
        public static string Create(DateTimeOffset startTimeUtc, string platform, string sessionId)
        {
            var time = startTimeUtc.ToUniversalTime().ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            return Sanitize(time + "_" + platform + "_" + sessionId);
        }

        public static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "runtime-log";

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.')
                    builder.Append(ch);
                else
                    builder.Append('_');
            }

            return builder.ToString();
        }
    }
}
