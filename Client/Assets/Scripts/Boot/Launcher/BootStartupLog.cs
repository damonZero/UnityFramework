using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Boot
{
    public enum BootStartupLogLevel
    {
        Info,
        Warn,
        Error
    }

    public sealed class BootStartupLogEntry
    {
        public DateTime TimeUtc { get; init; } = DateTime.UtcNow;
        public BootStartupLogLevel Level { get; init; } = BootStartupLogLevel.Info;
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>
    /// AOT-side startup logger. Must not reference any hot-update Framework.* type.
    /// Writes human-readable lines to Logs/Runtime/boot.log and keeps an in-memory
    /// snapshot that is handed to the hot-update layer via <see cref="BootBridge"/>.
    /// </summary>
    public static class BootStartupLog
    {
        private static readonly List<BootStartupLogEntry> SnapshotList = new List<BootStartupLogEntry>();
        private static readonly object Gate = new object();

        public static IReadOnlyList<BootStartupLogEntry> Snapshot
        {
            get
            {
                lock (Gate)
                    return SnapshotList.ToArray();
            }
        }

        public static void Info(string message) => Write(BootStartupLogLevel.Info, message);
        public static void Warn(string message) => Write(BootStartupLogLevel.Warn, message);
        public static void Error(string message) => Write(BootStartupLogLevel.Error, message);

        public static void Write(BootStartupLogLevel level, string message)
        {
            var entry = new BootStartupLogEntry
            {
                TimeUtc = DateTime.UtcNow,
                Level = level,
                Message = message ?? string.Empty
            };

            lock (Gate)
                SnapshotList.Add(entry);

            WriteFileLine(entry);
        }

        private static void WriteFileLine(BootStartupLogEntry entry)
        {
            try
            {
#if UNITY_EDITOR
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                var dir = Path.Combine(projectRoot, "Logs", "Runtime");
#else
                var dir = Path.Combine(Application.persistentDataPath, "Logs", "Runtime");
#endif
                Directory.CreateDirectory(dir);
                var line = $"[{entry.TimeUtc:yyyy-MM-dd HH:mm:ss}][{entry.Level}] {entry.Message}";
                File.AppendAllText(Path.Combine(dir, "boot.log"), line + Environment.NewLine);
            }
            catch
            {
                // Logging must never throw during boot.
            }
        }
    }
}
