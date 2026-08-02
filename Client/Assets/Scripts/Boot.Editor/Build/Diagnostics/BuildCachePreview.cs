using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建缓存预检 —— 在不实际构建的情况下，判断每个 Stage 是否会命中缓存（可跳过）。
    /// 供 Dashboard 展示「缓存命中 / 需重跑」状态，帮助用户快速判断一次构建的成本。
    ///
    /// 判断逻辑与 <see cref="BuildPipelineRunner.GeneratePlan"/> 保持一致：
    /// 指纹对比（Profile/Inputs/Tools）+ 输出存在性 + 依赖级联。
    ///
    /// 性能：结果带缓存（<see cref="Inspect"/> 传入同一 Profile 且其哈希未变时返回缓存），
    /// 避免 Dashboard 每次 Repaint 都做全目录 SHA-256。Profile 变化或手动调用
    /// <see cref="Invalidate"/> 时重新计算。
    /// </summary>
    public static class BuildCachePreview
    {
        private static List<StageCacheStatus> _cached;
        private static BuildProfile _cachedProfile;
        private static string _cachedProfileHash;
        private static bool _dirty = true;

        /// <summary>单个 Stage 的缓存状态</summary>
        public sealed class StageCacheStatus
        {
            public string StageId;
            public string DisplayName;
            public int Order;
            public bool CacheHit;      // true=缓存命中，可跳过；false=需重跑
            public string Reason;      // 人类可读原因
            public string Category;

            /// <summary>true=该 Stage 策略为 AlwaysRun/NoSkip，总是执行（不适用缓存）。</summary>
            public bool AlwaysRuns;
        }

        /// <summary>Pipeline 版本（与 Runner 保持一致）</summary>
        private const string PipelineVersion = "1.1.0";

        /// <summary>使缓存失效（Profile 变更、构建完成后调用），下次 Inspect 重新计算。</summary>
        public static void Invalidate()
        {
            _cached = null;
            _cachedProfile = null;
            _cachedProfileHash = null;
            _dirty = true;
        }

        /// <summary>对当前 Profile 预检所有 Stage 的缓存状态（带缓存）。</summary>
        public static List<StageCacheStatus> Inspect(BuildProfile profile)
        {
            if (profile == null)
            {
                Invalidate();
                return new List<StageCacheStatus>();
            }

            string profileHash = SafeProfileHash(profile);

            // 缓存命中条件：Profile 引用相同且哈希未变（Profile 字段未改）
            if (!_dirty && ReferenceEquals(_cachedProfile, profile)
                && string.Equals(_cachedProfileHash, profileHash, StringComparison.Ordinal))
            {
                return _cached ?? new List<StageCacheStatus>();
            }

            _cachedProfile = profile;
            _cachedProfileHash = profileHash;
            _dirty = false;
            _cached = Compute(profile);
            return _cached;
        }

        private static string SafeProfileHash(BuildProfile profile)
        {
            try { return profile.ComputeProfileHash(); }
            catch { return string.Empty; }
        }

        private static List<StageCacheStatus> Compute(BuildProfile profile)
        {
            var result = new List<StageCacheStatus>();
            if (profile == null)
                return result;

            var ctx = new BuildContext { Profile = profile };
            ctx.Paths = new BuildPaths(profile);
            ctx.Paths.EnsureDirectories();

            var stages = BuildStageRegistry.GetAll();
            var willSkipById = new Dictionary<string, bool>();

            foreach (var stage in stages)
            {
                bool alwaysRuns = (stage.Policy & BuildStagePolicy.AlwaysRun) != 0
                                  || (stage.Policy & BuildStagePolicy.NoSkip) != 0;
                bool canSkip = TryDecideSkip(ctx, stage, willSkipById, out string reason);
                willSkipById[stage.Id] = canSkip;

                result.Add(new StageCacheStatus
                {
                    StageId = stage.Id,
                    DisplayName = stage.DisplayName,
                    Order = stage.Order,
                    CacheHit = canSkip,
                    Reason = alwaysRuns ? "策略总是执行" : reason,
                    Category = stage.Category,
                    AlwaysRuns = alwaysRuns,
                });
            }

            return result;
        }

        private static bool TryDecideSkip(BuildContext ctx, IBuildStage stage,
            IReadOnlyDictionary<string, bool> willSkipById, out string reason)
        {
            // 1. 基础 CanSkip（含策略检查）
            var previous = LoadPreviousFingerprint(ctx, stage.Id);
            var decision = ctx.ForceFullRebuild
                ? BuildSkipDecision.DoNotSkip("Manual full rebuild requested")
                : stage.CanSkip(ctx, previous);

            // 2. 指纹校验（防止缓存指纹过期但输出仍在）
            if (decision.CanSkip)
            {
                var current = ComputeStageFingerprint(ctx, stage, includeOutputs: true);
                if (!current.Matches(previous))
                {
                    decision = BuildSkipDecision.DoNotSkip("输入/工具/Profile 指纹变化");
                }
                else if (!string.Equals(current.OutputsHash, previous.OutputsHash, StringComparison.Ordinal))
                {
                    decision = BuildSkipDecision.DoNotSkip("缓存输出内容变化");
                }
            }

            // 3. 依赖级联：上游 ProducesArtifacts/Transactional 变化 → 下游必须重跑
            if (decision.CanSkip)
            {
                foreach (string dependencyId in stage.DependsOn)
                {
                    if (!willSkipById.TryGetValue(dependencyId, out bool depWillSkip))
                        continue;
                    var dependencyStage = FindStage(dependencyId);
                    bool changesDownstream = dependencyStage != null
                        && (dependencyStage.Policy.HasFlag(BuildStagePolicy.ProducesArtifacts)
                            || dependencyStage.Policy.HasFlag(BuildStagePolicy.Transactional));
                    if (!depWillSkip && changesDownstream)
                    {
                        decision = BuildSkipDecision.DoNotSkip($"依赖 {dependencyId} 将产生新输入");
                        break;
                    }
                }
            }

            reason = decision.CanSkip
                ? (decision.HumanReason ?? "缓存命中")
                : (decision.HumanReason ?? "需重跑");
            return decision.CanSkip;
        }

        private static IBuildStage FindStage(string id)
        {
            foreach (var stage in BuildStageRegistry.GetAll())
            {
                if (stage.Id == id) return stage;
            }
            return null;
        }

        private static BuildStageFingerprint LoadPreviousFingerprint(BuildContext ctx, string stageId)
        {
            string fingerprintPath = Path.Combine(ctx.Paths.CacheDir, $"{stageId}.fingerprint.json");
            if (!File.Exists(fingerprintPath)) return null;
            try
            {
                return JsonUtility.FromJson<BuildStageFingerprint>(File.ReadAllText(fingerprintPath));
            }
            catch { return null; }
        }

        private static BuildStageFingerprint ComputeStageFingerprint(BuildContext ctx, IBuildStage stage, bool includeOutputs)
        {
            var inputs = stage.GetInputs(ctx);
            if (string.IsNullOrEmpty(inputs.ProfileHash))
                inputs.ProfileHash = ctx.Profile.ComputeProfileHash();
            inputs.WithToolVersion("Unity", Application.unityVersion);

            var outputs = stage.GetExpectedOutputs(ctx);

            return new BuildStageFingerprint
            {
                StageId = stage.Id,
                PipelineVersion = PipelineVersion,
                StageVersion = stage.Version,
                ProfileHash = inputs.ProfileHash,
                InputsHash = HashInputs(inputs),
                OutputsHash = includeOutputs ? HashOutputs(outputs) : "",
                ToolsHash = HashTools(inputs),
                UnityVersion = Application.unityVersion,
            };
        }

        private static string HashInputs(BuildStageInputs inputs)
        {
            var sb = new StringBuilder();
            sb.Append(inputs.ProfileHash).Append('\n');
            foreach (string path in inputs.SourcePaths.OrderBy(p => p))
                AppendPathFingerprint(sb, path);
            foreach (string dep in inputs.DependsOnStages.OrderBy(d => d))
                sb.Append("dep:").Append(dep).Append('\n');
            return Sha256(sb.ToString());
        }

        private static string HashOutputs(BuildStageOutputs outputs)
        {
            var sb = new StringBuilder();
            foreach (string file in outputs.RequiredFiles.OrderBy(p => p))
                AppendPathFingerprint(sb, file);
            foreach (string dir in outputs.RequiredDirectories.OrderBy(p => p))
                AppendPathFingerprint(sb, dir);
            return Sha256(sb.ToString());
        }

        private static string HashTools(BuildStageInputs inputs)
        {
            var sb = new StringBuilder();
            foreach (var kv in inputs.ToolVersions.OrderBy(kv => kv.Key))
                sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
            return Sha256(sb.ToString());
        }

        private static void AppendPathFingerprint(StringBuilder sb, string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string normalized = path.Replace('\\', '/');
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                sb.Append("file:").Append(normalized).Append('|')
                    .Append(fi.Length).Append('|')
                    .Append(ComputeFileHash(path)).Append('\n');
                return;
            }

            if (!Directory.Exists(path))
            {
                sb.Append("missing:").Append(normalized).Append('\n');
                return;
            }

            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                         .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(f => f.Replace('\\', '/')))
            {
                var fi = new FileInfo(file);
                sb.Append("file:").Append(file.Replace('\\', '/')).Append('|')
                    .Append(fi.Length).Append('|')
                    .Append(ComputeFileHash(file)).Append('\n');
            }
        }

        private static string ComputeFileHash(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static string Sha256(string text)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
