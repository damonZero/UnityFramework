using System;
using Framework.Log;

namespace Framework.RuntimeLog
{
    public sealed class RuntimeLogSessionOptions
    {
        public string DirectoryPath { get; set; }
        public string FilePrefix { get; set; }
        public bool MaintainLatest { get; set; }
        public GameLogLevel MinimumLevel { get; set; } = GameLogLevel.Trace;
        public RuntimeLogSessionInfo SessionInfo { get; set; }
        public Func<DateTimeOffset> UtcNow { get; set; } = () => DateTimeOffset.UtcNow;
        public Func<int?> FrameProvider { get; set; }
    }
}
