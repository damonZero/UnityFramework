using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Framework.RuntimeLog;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.Logging
{
    public static class RuntimeLogEditorTools
    {
        private const string MenuRoot = "KJ/Runtime Logs/";

        [MenuItem(MenuRoot + "Open Latest Log")]
        public static void OpenLatestLog()
        {
            OpenFile(RuntimeLogConstants.LatestLogFileName);
        }

        [MenuItem(MenuRoot + "Open Latest Session")]
        public static void OpenLatestSession()
        {
            OpenFile(RuntimeLogConstants.LatestSessionFileName);
        }

        [MenuItem(MenuRoot + "Reveal Log Folder")]
        public static void RevealLogFolder()
        {
            EnsureDirectory();
            EditorUtility.RevealInFinder(GetRuntimeLogDirectory());
        }

        [MenuItem(MenuRoot + "Generate Latest Summary")]
        public static void GenerateLatestSummary()
        {
            var logPath = Path.Combine(GetRuntimeLogDirectory(), RuntimeLogConstants.LatestLogFileName);
            if (!File.Exists(logPath))
            {
                EditorUtility.DisplayDialog("Runtime Logs", "latest.jsonl does not exist.", "OK");
                return;
            }

            var summaryPath = Path.Combine(GetRuntimeLogDirectory(), "latest.summary.md");
            File.WriteAllText(summaryPath, RuntimeLogSummary.Generate(logPath), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(summaryPath);
        }

        [MenuItem(MenuRoot + "Export Diagnostic Package")]
        public static void ExportDiagnosticPackage()
        {
            var directory = GetRuntimeLogDirectory();
            if (!Directory.Exists(directory))
            {
                EditorUtility.DisplayDialog("Runtime Logs", "Runtime log directory does not exist.", "OK");
                return;
            }

            var exportRoot = Path.Combine(directory, "Exports");
            Directory.CreateDirectory(exportRoot);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var staging = Path.Combine(exportRoot, "diagnostic_" + stamp);
            Directory.CreateDirectory(staging);

            CopyIfExists(RuntimeLogConstants.LatestLogFileName, staging);
            CopyIfExists(RuntimeLogConstants.LatestSessionFileName, staging);

            var logPath = Path.Combine(directory, RuntimeLogConstants.LatestLogFileName);
            if (File.Exists(logPath))
                File.WriteAllText(Path.Combine(staging, "summary.md"), RuntimeLogSummary.Generate(logPath), Encoding.UTF8);

            var zipPath = staging + ".zip";
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            try
            {
                ZipFile.CreateFromDirectory(staging, zipPath);
                Directory.Delete(staging, true);
                EditorUtility.RevealInFinder(zipPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Runtime Logs",
                    "Failed to create zip package. The diagnostic folder was kept.",
                    "OK");
                EditorUtility.RevealInFinder(staging);
            }
        }

        [MenuItem(MenuRoot + "Clear Runtime Logs")]
        public static void ClearRuntimeLogs()
        {
            var directory = GetRuntimeLogDirectory();
            if (!Directory.Exists(directory))
                return;

            if (!EditorUtility.DisplayDialog("Runtime Logs", "Delete all runtime log files?", "Delete", "Cancel"))
                return;

            Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        private static void OpenFile(string fileName)
        {
            var path = Path.Combine(GetRuntimeLogDirectory(), fileName);
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Runtime Logs", fileName + " does not exist.", "OK");
                return;
            }

            EditorUtility.OpenWithDefaultApp(path);
        }

        private static void CopyIfExists(string fileName, string destinationDirectory)
        {
            var source = Path.Combine(GetRuntimeLogDirectory(), fileName);
            if (File.Exists(source))
                File.Copy(source, Path.Combine(destinationDirectory, fileName), true);
        }

        private static void EnsureDirectory()
        {
            Directory.CreateDirectory(GetRuntimeLogDirectory());
        }

        private static string GetRuntimeLogDirectory()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, "Logs", "Runtime");
        }
    }

    internal static class RuntimeLogSummary
    {
        public static string Generate(string logPath)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Trace"] = 0,
                ["Debug"] = 0,
                ["Information"] = 0,
                ["Warning"] = 0,
                ["Error"] = 0,
                ["Critical"] = 0
            };
            var firstProblems = new List<string>();
            var total = 0;

            foreach (var line in File.ReadLines(logPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                total++;
                var level = ExtractJsonString(line, "level");
                if (!string.IsNullOrEmpty(level) && counts.ContainsKey(level))
                    counts[level]++;

                if (level == "Warning" || level == "Error" || level == "Critical")
                {
                    if (firstProblems.Count < 20)
                    {
                        var time = ExtractJsonString(line, "timeUtc");
                        var module = ExtractJsonString(line, "module");
                        var phase = ExtractJsonString(line, "phase");
                        var message = ExtractJsonString(line, "message");
                        firstProblems.Add("- " + time + " [" + level + "] [" + phase + "] " + module + ": " + message);
                    }
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("# Runtime Log Summary");
            builder.AppendLine();
            builder.AppendLine("- Source: `" + logPath.Replace('\\', '/') + "`");
            builder.AppendLine("- Generated: `" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC`");
            builder.AppendLine("- Entries: `" + total + "`");
            builder.AppendLine();
            builder.AppendLine("## Levels");
            builder.AppendLine();
            foreach (var pair in counts)
            {
                builder.AppendLine("- " + pair.Key + ": `" + pair.Value + "`");
            }

            builder.AppendLine();
            builder.AppendLine("## First Problems");
            builder.AppendLine();
            if (firstProblems.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var item in firstProblems)
                {
                    builder.AppendLine(item);
                }
            }

            return builder.ToString();
        }

        private static string ExtractJsonString(string line, string propertyName)
        {
            var needle = "\"" + propertyName + "\":";
            var index = line.IndexOf(needle, StringComparison.Ordinal);
            if (index < 0)
                return string.Empty;

            index += needle.Length;
            if (index >= line.Length || line[index] != '"')
                return string.Empty;

            index++;
            var builder = new StringBuilder();
            while (index < line.Length)
            {
                var ch = line[index++];
                if (ch == '"')
                    break;

                if (ch == '\\' && index < line.Length)
                {
                    var escaped = line[index++];
                    if (escaped == 'u' && index + 4 <= line.Length)
                    {
                        var hex = line.Substring(index, 4);
                        if (ushort.TryParse(
                                hex,
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var code))
                        {
                            builder.Append((char)code);
                            index += 4;
                            continue;
                        }
                    }

                    builder.Append(escaped switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        '/' => '/',
                        'b' => '\b',
                        'f' => '\f',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => escaped
                    });
                    continue;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
