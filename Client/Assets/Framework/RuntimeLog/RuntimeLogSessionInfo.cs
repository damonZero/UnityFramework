using System;
using System.Collections.Generic;

namespace Framework.RuntimeLog
{
    public sealed class RuntimeLogSessionInfo
    {
        public string SessionId { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
        public DateTimeOffset? EndTimeUtc { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string UnityVersion { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string ApplicationVersion { get; set; } = string.Empty;
        public string BuildGuid { get; set; } = string.Empty;
        public string GitCommit { get; set; } = string.Empty;
        public string LogProfile { get; set; } = string.Empty;
        public string AssetPlayMode { get; set; } = string.Empty;
        public string AssetPackageName { get; set; } = string.Empty;
        public List<string> HotUpdateAssemblies { get; } = new();
        public List<string> AotMetadataAssemblies { get; } = new();
        public Dictionary<string, string> Context { get; } = new(StringComparer.Ordinal);
    }
}
