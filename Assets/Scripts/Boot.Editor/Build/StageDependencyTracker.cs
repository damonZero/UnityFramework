using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 级联差量检测引擎。
    /// 对比标记文件时间戳与各 Stage 输入文件的修改时间，
    /// 自动判断哪些 Stage 需要重新运行，并级联传播下游依赖。
    /// </summary>
    public static class StageDependencyTracker
    {
        /// <summary> 共 10 个 Stage 的名称（P0~P9），与 BuildStageRegistry 中的顺序严格一致。 </summary>
        public static readonly string[] StageNames =
        {
            "P0.Plan",
            "P1.Preflight",
            "P2.Generate",
            "P3.HybridCLR",
            "P4.Assets",
            "P5.ApplyConfig",
            "P6.Player",
            "P7.Verify",
            "P8.Smoke",
            "P9.Report",
        };

        /// <summary> 各 Stage 监控的输入路径（文件或目录）。null 表示始终运行。 </summary>
        /// <remarks>
        /// S1/S2 仅监控热更程序集目录，排除 *.Editor 工具目录。
        /// 避免修改纯 Editor 工具代码时触发 MethodBridge 全量重生成（~20 分钟）。
        /// 若新增/修改热更程序集目录，需同步更新此列表。
        /// </remarks>
        private static readonly IReadOnlyDictionary<int, string[]> Inputs = new Dictionary<int, string[]>
        {
            [0] = null,                                           // S0 — always
            [1] = new[]                                            // S1 — hot-update C# only (no *.Editor)
            {
                "Assets/Scripts/Boot/",
                "Assets/Scripts/Core/",
                "Assets/Scripts/General/",
                "Assets/Scripts/Project/",
                "Assets/Framework/",
            },
            [2] = new[]                                            // S2 — hot-update C# only (no *.Editor)
            {
                "Assets/Scripts/Boot/",
                "Assets/Scripts/Core/",
                "Assets/Scripts/General/",
                "Assets/Scripts/Project/",
                "Assets/Framework/",
            },
            [3] = new[] { "HybridCLRData/HotUpdateDlls/" },       // S3 — S2 output
            [4] = new[] { "Assets/GameRes/HotUpdate/" },          // S4 — YooAsset resources
            [5] = new[] { "Assets/Resources/AssetConfig.asset" }, // S5 — config only
            [6] = null,                                           // S6 — always (cascaded)
            [7] = null,                                           // S7 — always
            [8] = null,                                           // S8 — optional
            [9] = null,                                           // S9 — always
        };

        // ===== 单 Stage 查询 =====

        /// <summary> 检查单个 Stage 的输入是否比标记文件更新、进而需要重跑。 </summary>
        public static bool NeedsRun(string stageName, BuildConfig config = null)
        {
            string markerPath = Path.Combine(GetMarkerDir(config), $".{stageName}.done");
            if (!File.Exists(markerPath))
                return true; // 从未跑过

            DateTime markerTime = File.GetLastWriteTime(markerPath);

            int idx = System.Array.IndexOf(StageNames, stageName);
            if (idx < 0) return true;
            return AnyInputNewerThan(idx, markerTime);
        }

        // ===== 全量差量检测（带级联） =====

        /// <summary>
        /// 返回 bool[10]，表示各 Stage 是否需要重新运行（已应用级联）。
        /// S0/S7/S9 始终 true；S8 可由外部设置。
        /// </summary>
        public static bool[] DetectChanges(bool includeSmoke = false, BuildConfig config = null)
        {
            bool[] raw = new bool[10];

            // S0 始终运行
            raw[0] = true;

            // S1~S6：检查输入是否比标记新
            for (int i = 1; i <= 6; i++)
            {
                string markerPath = Path.Combine(GetMarkerDir(config), $".{StageNames[i]}.done");
                if (!File.Exists(markerPath))
                {
                    raw[i] = true;
                    continue;
                }

                DateTime markerTime = File.GetLastWriteTime(markerPath);
                raw[i] = AnyInputNewerThan(i, markerTime);
            }

            // S7~S9
            raw[7] = true;
            raw[8] = includeSmoke;
            raw[9] = true;

            // === 级联传播 ===
            bool[] result = (bool[])raw.Clone();

            // S1 → S2 → S3 → S4 链式级联
            for (int i = 2; i <= 4; i++)
                if (result[i - 1]) result[i] = true;

            // S4 或 S5 变更 → S6 必须重跑（S5 独立，不触发 S2~S4）
            if (result[4] || result[5]) result[6] = true;

            // S6 的原始变更也可能需要 S6 本身重跑
            if (raw[6]) result[6] = true;

            return result;
        }

        /// <summary> 获取某个 Stage 需要重跑的原因描述（供 UI 展示）。 </summary>
        public static string GetReason(int stageIndex, BuildConfig config = null)
        {
            if (stageIndex < 0 || stageIndex >= StageNames.Length)
                return "未知";

            string name = StageNames[stageIndex];
            string markerPath = Path.Combine(GetMarkerDir(config), $".{name}.done");

            if (!File.Exists(markerPath))
                return "从未构建（无标记文件）";

            if (Inputs.TryGetValue(stageIndex, out string[] paths) && paths != null)
            {
                DateTime markerTime = File.GetLastWriteTime(markerPath);
                foreach (string path in paths)
                {
                    if (!File.Exists(path) && !Directory.Exists(path))
                        continue;

                    IEnumerable<string> files;
                    if (Directory.Exists(path))
                        files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                    else
                        files = new[] { path };

                    foreach (string f in files)
                    {
                        if (f.EndsWith(".meta")) continue;
                        // 排除 Editor-only 目录（如 Boot.Editor/、Core.Editor/），
                        // 改 Editor 工具不应该触发 MethodBridge 重生成。
                        if (IsInEditorDirectory(f)) continue;
                        if (File.GetLastWriteTime(f) > markerTime)
                            return $"检测到变更: {Path.GetFileName(f)}";
                    }
                }
            }

            // 可能是级联传播
            int[] cascadedFrom = GetCascadeSource(stageIndex);
            if (cascadedFrom != null && cascadedFrom.Length > 0)
                return $"级联: S{cascadedFrom[0]} 需要重跑";

            return "无需重跑";
        }

        // ===== 辅助 =====

        /// <summary>
        /// 解析 marker 目录：优先用传入 config（尊重 OutputDir 覆盖），否则退回默认（Build/{Platform}/.markers）。
        /// </summary>
        private static string GetMarkerDir(BuildConfig config)
        {
            return (config ?? new BuildConfig()).GetMarkerDir();
        }

        private static bool AnyInputNewerThan(int stageIndex, DateTime markerTime)
        {
            if (!Inputs.TryGetValue(stageIndex, out string[] paths) || paths == null)
                return false;

            foreach (string path in paths)
            {
                if (!File.Exists(path) && !Directory.Exists(path))
                    continue;

                IEnumerable<string> files;
                if (Directory.Exists(path))
                    files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                else
                    files = new[] { path };

                foreach (string f in files)
                {
                    if (f.EndsWith(".meta")) continue;
                    // 排除 Editor-only 目录文件
                    if (IsInEditorDirectory(f)) continue;
                    if (File.GetLastWriteTime(f) > markerTime)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查文件路径是否落在 *.Editor 目录下。
        /// 改纯 Editor 工具代码不应触发热更相关的 Stage 重跑。
        /// </summary>
        private static bool IsInEditorDirectory(string filePath)
        {
            // 规范化分隔符后检查是否包含 "/Editor." 或 "/Editor/" 或 "\Editor." 等
            string normalized = filePath.Replace('\\', '/');
            // *.Editor/ 和 *.Editor 程序集目录
            return normalized.Contains("/Boot.Editor/")
                || normalized.Contains("/Core.Editor/")
                || normalized.Contains("/General.Editor/")
                || normalized.Contains("/Project.Editor/")
                || normalized.Contains("/Framework.Asset.Editor/")
                || normalized.Contains("/Framework.Log.Editor/");
        }

        /// <summary> 返回导致 stageIndex 被级联触发的前置 Stage 索引列表。 </summary>
        private static int[] GetCascadeSource(int stageIndex)
        {
            // 级联链: 1→2→3→4→6，5→6
            if (stageIndex == 2 && NeedsRun(StageNames[1])) return new[] { 1 };
            if (stageIndex == 3 && NeedsRun(StageNames[2])) return new[] { 2 };
            if (stageIndex == 4 && NeedsRun(StageNames[3])) return new[] { 3 };
            if (stageIndex == 6)
            {
                var sources = new List<int>();
                if (NeedsRun(StageNames[4])) sources.Add(4);
                if (NeedsRun(StageNames[5])) sources.Add(5);
                if (sources.Count > 0) return sources.ToArray();
            }
            return null;
        }
    }
}
