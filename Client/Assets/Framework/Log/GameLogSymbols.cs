namespace Framework.Log
{
    /// <summary>
    /// Central compile symbols used by log call-site pruning.
    /// </summary>
    public static class GameLogSymbols
    {
        public const string UnityEditor = "UNITY_EDITOR";
        public const string DevelopmentBuild = "DEVELOPMENT_BUILD";
        public const string Trace = "KJ_LOG_TRACE";
        public const string Debug = "KJ_LOG_DEBUG";
        public const string Information = "KJ_LOG_INFORMATION";
        public const string Warning = "KJ_LOG_WARNING";
        public const string Error = "KJ_LOG_ERROR";
        public const string Critical = "KJ_LOG_CRITICAL";
    }
}
