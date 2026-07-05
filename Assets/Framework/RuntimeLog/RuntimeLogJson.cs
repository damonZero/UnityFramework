using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Framework.Log;

namespace Framework.RuntimeLog
{
    public static class RuntimeLogJson
    {
        public static string SerializeEntry(RuntimeLogEntry entry, string sessionId)
        {
            var builder = new StringBuilder(512);
            builder.Append('{');
            AppendProperty(builder, "schema", RuntimeLogConstants.LogSchema);
            AppendComma(builder);
            AppendProperty(builder, "timeUtc", FormatUtc(entry.TimeUtc));
            AppendComma(builder);
            AppendProperty(builder, "sessionId", sessionId);
            AppendComma(builder);
            AppendProperty(builder, "seq", entry.Seq);
            AppendComma(builder);
            AppendNullableProperty(builder, "frame", entry.Frame);
            AppendComma(builder);
            AppendProperty(builder, "threadId", entry.ThreadId);
            AppendComma(builder);
            AppendProperty(builder, "level", entry.Level.ToString());
            AppendComma(builder);
            AppendProperty(builder, "module", Normalize(entry.Module, GameLog.DefaultModule));
            AppendComma(builder);
            AppendProperty(builder, "category", Normalize(entry.Category, GameLog.DefaultModule));
            AppendComma(builder);
            AppendProperty(builder, "phase", Normalize(entry.Phase, RuntimeLogPhaseResolver.DefaultPhase));
            AppendComma(builder);
            AppendProperty(builder, "message", entry.Message ?? string.Empty);
            AppendComma(builder);
            AppendNullableProperty(builder, "exceptionType", entry.ExceptionType);
            AppendComma(builder);
            AppendNullableProperty(builder, "exceptionMessage", entry.ExceptionMessage);
            AppendComma(builder);
            AppendNullableProperty(builder, "stackTrace", entry.StackTrace);

            if (entry.Context != null && entry.Context.Count > 0)
            {
                AppendComma(builder);
                AppendDictionary(builder, "context", entry.Context);
            }

            builder.Append('}');
            return builder.ToString();
        }

        public static string SerializeSession(RuntimeLogSessionInfo info)
        {
            var builder = new StringBuilder(512);
            builder.Append('{');
            AppendProperty(builder, "schema", RuntimeLogConstants.SessionSchema);
            AppendComma(builder);
            AppendProperty(builder, "sessionId", info.SessionId);
            AppendComma(builder);
            AppendProperty(builder, "startTimeUtc", FormatUtc(info.StartTimeUtc));
            AppendComma(builder);
            AppendNullableProperty(builder, "endTimeUtc", info.EndTimeUtc.HasValue ? FormatUtc(info.EndTimeUtc.Value) : null);
            AppendComma(builder);
            AppendProperty(builder, "projectName", info.ProjectName);
            AppendComma(builder);
            AppendProperty(builder, "unityVersion", info.UnityVersion);
            AppendComma(builder);
            AppendProperty(builder, "platform", info.Platform);
            AppendComma(builder);
            AppendProperty(builder, "applicationVersion", info.ApplicationVersion);
            AppendComma(builder);
            AppendProperty(builder, "buildGuid", info.BuildGuid);
            AppendComma(builder);
            AppendProperty(builder, "gitCommit", info.GitCommit);
            AppendComma(builder);
            AppendProperty(builder, "logProfile", info.LogProfile);
            AppendComma(builder);
            AppendProperty(builder, "assetPlayMode", info.AssetPlayMode);
            AppendComma(builder);
            AppendProperty(builder, "assetPackageName", info.AssetPackageName);
            AppendComma(builder);
            AppendArray(builder, "hotUpdateAssemblies", info.HotUpdateAssemblies);
            AppendComma(builder);
            AppendArray(builder, "aotMetadataAssemblies", info.AotMetadataAssemblies);

            if (info.Context != null && info.Context.Count > 0)
            {
                AppendComma(builder);
                AppendDictionary(builder, "context", info.Context);
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendProperty(StringBuilder builder, string name, string value)
        {
            AppendName(builder, name);
            AppendString(builder, value ?? string.Empty);
        }

        private static void AppendProperty(StringBuilder builder, string name, long value)
        {
            AppendName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendProperty(StringBuilder builder, string name, int value)
        {
            AppendName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendNullableProperty(StringBuilder builder, string name, int? value)
        {
            AppendName(builder, name);
            if (value.HasValue)
                builder.Append(value.Value.ToString(CultureInfo.InvariantCulture));
            else
                builder.Append("null");
        }

        private static void AppendNullableProperty(StringBuilder builder, string name, string value)
        {
            AppendName(builder, name);
            if (value == null)
                builder.Append("null");
            else
                AppendString(builder, value);
        }

        private static void AppendArray(StringBuilder builder, string name, IReadOnlyList<string> values)
        {
            AppendName(builder, name);
            builder.Append('[');
            if (values != null)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    if (i > 0)
                        AppendComma(builder);

                    AppendString(builder, values[i] ?? string.Empty);
                }
            }

            builder.Append(']');
        }

        private static void AppendDictionary(StringBuilder builder, string name, IReadOnlyDictionary<string, string> values)
        {
            AppendName(builder, name);
            builder.Append('{');
            var first = true;
            foreach (var pair in values)
            {
                if (!first)
                    AppendComma(builder);

                first = false;
                AppendName(builder, pair.Key);
                AppendString(builder, pair.Value ?? string.Empty);
            }

            builder.Append('}');
        }

        private static void AppendName(StringBuilder builder, string name)
        {
            AppendString(builder, name);
            builder.Append(':');
        }

        private static void AppendString(StringBuilder builder, string value)
        {
            builder.Append('"');
            Escape(builder, value);
            builder.Append('"');
        }

        private static void Escape(StringBuilder builder, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (ch < 32)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(ch);
                        }
                        break;
                }
            }
        }

        private static void AppendComma(StringBuilder builder) => builder.Append(',');

        private static string FormatUtc(DateTimeOffset value) =>
            value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

        private static string Normalize(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
