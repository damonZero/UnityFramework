using Framework.Log;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Editor 构建管线统一日志输出——桥接到 GameLog（带 module）并同步输出到 Unity Console。
    /// </summary>
    internal static class BuildLogger
    {
        private const string Module = "Build";

        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warn(string message) => Log(LogLevel.Warn, message);
        public static void Error(string message) => Log(LogLevel.Error, message);

        private static void Log(LogLevel level, string message)
        {
            // 写入 GameLog（落 .jsonl 供 AI 分析）
            switch (level)
            {
                case LogLevel.Info:
                    GameLog.Info(message, module: Module);
                    break;
                case LogLevel.Warn:
                    GameLog.Warn(message, module: Module);
                    break;
                case LogLevel.Error:
                    GameLog.Error(message, module: Module);
                    break;
            }
            // 同步到 Unity Console（人看）
            switch (level)
            {
                case LogLevel.Info:
                    Debug.Log(message);
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
            }
        }

        private enum LogLevel { Info, Warn, Error }
    }
}
