using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Boot.Editor.Build.Telemetry;
using Framework.BuildPipeline.Plan;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P2 HybridCLR generation with independently verified and cached AOT strip and MethodBridge steps.
    /// </summary>
    public class P2_GenerateStage : BuildStageBase
    {
        private const string LinkXmlPath = "Assets/HybridCLRGenerate/link.xml";
        private const string AotGenericReferencesPath = "Assets/HybridCLRGenerate/AOTGenericReferences.cs";
        private static readonly string[] RuntimeSourceRoots =
        {
            "Assets/Scripts/Boot/",
            "Assets/Scripts/Core/",
            "Assets/Scripts/General/",
            "Assets/Scripts/Project/",
            "Assets/Framework/Asset/",
            "Assets/Framework/AssetShared/",
            "Assets/Framework/Cache/",
            "Assets/Framework/Event/",
            "Assets/Framework/Log/",
            "Assets/Framework/Pool/",
            "Assets/Framework/RuntimeLog/",
        };

        private static readonly string[] AotStripInputPaths =
        {
            "Assets/Scripts/Boot/Launcher/",
            "Assets/Framework/AssetShared/",
            "Assets/Framework/1External/",
            "Assets/Packages/",
            "Packages/manifest.json",
            "Packages/packages-lock.json",
            "ProjectSettings/EditorBuildSettings.asset",
            "ProjectSettings/HybridCLRSettings.asset",
            "ProjectSettings/ProjectSettings.asset",
        };

        private static readonly string[] BridgeSensitiveTokens =
        {
            "MonoPInvokeCallback",
            "DllImport",
            "UnmanagedCallersOnly",
            "delegate*",
            "calli",
        };

        public override string Id => "P2.Generate";
        public override string DisplayName => "Generate HybridCLR Artifacts (Incremental)";
        public override int Version => 4;
        public override int Order => 2;
        public override string Category => "HybridCLR";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P1.Preflight" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.ProducesArtifacts;

        public override BuildStageInputs GetInputs(BuildContext context)
        {
            var inputs = new BuildStageInputs()
                .WithSourcePaths(RuntimeSourceRoots)
                .WithSourcePaths(AotStripInputPaths);
            inputs.ProfileHash = context.Profile.ComputeHybridClrProfileHash();
            return inputs;
        }

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
        {
            var outputs = new BuildStageOutputs()
                .WithRequiredFile(LinkXmlPath)
                .WithRequiredFile(AotGenericReferencesPath)
                .WithRequiredFile(Path.Combine(SettingsUtil.GeneratedCppDir, "UnityVersion.h"))
                .WithRequiredFile(Path.Combine(SettingsUtil.GeneratedCppDir, "AssemblyManifest.cpp"))
                .WithRequiredFile(Path.Combine(SettingsUtil.GeneratedCppDir, "MethodBridge.cpp"))
                .WithRequiredDirectory(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(context.Profile.Platform))
                .WithRequiredDirectory(SettingsUtil.GetAssembliesPostIl2CppStripDir(context.Profile.Platform));

            string aotDirectory = SettingsUtil.GetAssembliesPostIl2CppStripDir(context.Profile.Platform);
            foreach (string assemblyName in SettingsUtil.HybridCLRSettings.patchAOTAssemblies ?? Array.Empty<string>())
                outputs.WithRequiredFile(Path.Combine(aotDirectory, assemblyName + ".dll"));
            return outputs;
        }

        public override void Execute(BuildContext context)
        {
            var profile = context.Profile ?? throw new InvalidOperationException("BuildProfile is required");
            BuildTarget target = profile.Platform;
            var cache = LoadCache(context);
            using var developmentBuildScope = new DevelopmentBuildScope(profile.DevelopmentBuild);
            string pathHashManifestPath = GetPathHashManifestPath(context);
            string bridgeSensitiveManifestPath = GetBridgeSensitiveManifestPath(context);

            BuildLogger.Info("[P2] HybridCLR: compiling hot-update DLLs...");
            BuildTelemetry.Measure("P2.CompileHotUpdateDlls", "HybridCLR",
                () => CompileDllCommand.CompileDll(target, profile.DevelopmentBuild));
            string compiledDllHash = HashPathsCached(new[] { SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target) }, pathHashManifestPath);

            BuildTelemetry.Measure("P2.GenerateIl2CppDef", "HybridCLR",
                Il2CppDefGeneratorCommand.GenerateIl2CppDef);

            BuildTelemetry.Measure("P2.GenerateLinkXml", "HybridCLR",
                () => LinkGeneratorCommand.GenerateLinkXml(target));
            string linkXmlHash = HashFile(LinkXmlPath);
            string aotStripInputHash = HashPathsCached(AotStripInputPaths, pathHashManifestPath);
            string currentAotDllHash = HashPathsCached(new[] { SettingsUtil.GetAssembliesPostIl2CppStripDir(target) }, pathHashManifestPath);

            bool stripRequired = context.ForceFullRebuild
                || !File.Exists(LinkXmlPath)
                || !HasRequiredAotMetadata(target)
                || !StringEquals(cache.LinkXmlHash, linkXmlHash)
                || !StringEquals(cache.AotStripInputHash, aotStripInputHash)
                || !StringEquals(cache.AotDllHash, currentAotDllHash);
            if (stripRequired)
            {
                BuildLogger.Info("[P2] HybridCLR: link.xml changed, rebuilding stripped AOT DLLs.");
                BuildTelemetry.Measure("P2.StripAotDlls", "HybridCLR",
                    () => StripAOTDllCommand.GenerateStripedAOTDlls(target));
            }
            else
            {
                BuildLogger.Info("[P2] HybridCLR: stripped AOT DLL cache verified.");
            }
            string aotDllHash = stripRequired
                ? HashPathsCached(new[] { SettingsUtil.GetAssembliesPostIl2CppStripDir(target) }, pathHashManifestPath)
                : currentAotDllHash;

            string bridgeInputHash = HashBridgeSensitiveSources(bridgeSensitiveManifestPath);
            string methodBridgePath = Path.Combine(SettingsUtil.GeneratedCppDir, "MethodBridge.cpp");
            string methodBridgeCacheKey = ComputeMethodBridgeCacheKey(
                aotDllHash, bridgeInputHash,
                profile.ComputeHybridClrProfileHash(), profile.DevelopmentBuild);
            string cachedMethodBridgePath = Path.Combine(
                context.Paths.CacheDir, "methodbridge", methodBridgeCacheKey, "MethodBridge.cpp");

            if (context.ForceFullRebuild || !File.Exists(cachedMethodBridgePath))
            {
                BuildLogger.Info("[P2] HybridCLR: MethodBridge cache miss, generating.");
                BuildTelemetry.Measure("P2.GenerateMethodBridge", "HybridCLR",
                    () => MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target));
                SaveMethodBridgeToCache(methodBridgePath, cachedMethodBridgePath);
            }
            else
            {
                BuildLogger.Info("[P2] HybridCLR: MethodBridge cache hit, restoring.");
                RestoreMethodBridgeFromCache(methodBridgePath, cachedMethodBridgePath);
            }

            bool genericReferencesRequired = context.ForceFullRebuild
                || !File.Exists(AotGenericReferencesPath)
                || !StringEquals(cache.CompiledDllHash, compiledDllHash)
                || !StringEquals(cache.AotDllHash, aotDllHash)
                || !StringEquals(cache.AotGenericReferencesHash, HashFile(AotGenericReferencesPath));
            if (genericReferencesRequired)
            {
                BuildTelemetry.Measure("P2.GenerateAotGenericReferences", "HybridCLR",
                    () => AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target));
            }
            else
            {
                BuildLogger.Info("[P2] HybridCLR: AOT generic reference cache verified.");
            }

            SaveCache(context, new HybridClrGenerationCache
            {
                CompiledDllHash = compiledDllHash,
                LinkXmlHash = linkXmlHash,
                AotStripInputHash = aotStripInputHash,
                AotDllHash = aotDllHash,
                BridgeSensitiveHash = bridgeInputHash,
                HybridClrProfileHash = profile.ComputeHybridClrProfileHash(),
                MethodBridgeHash = HashFile(methodBridgePath),
                AotGenericReferencesHash = HashFile(AotGenericReferencesPath),
                UpdatedAtUtc = DateTime.UtcNow.ToString("o"),
            });
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            if (!HasRequiredAotMetadata(context.Profile.Platform))
                throw new BuildFailedException(Id, "Required stripped AOT metadata DLLs are missing");
            BuildLogger.Info("[P2] HybridCLR generated artifacts verified.");
        }

        private static bool HasRequiredAotMetadata(BuildTarget target)
        {
            string directory = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            return (SettingsUtil.HybridCLRSettings.patchAOTAssemblies ?? Array.Empty<string>())
                .All(name => File.Exists(Path.Combine(directory, name + ".dll")));
        }

        private static HybridClrGenerationCache LoadCache(BuildContext context)
        {
            string path = GetCachePath(context);
            if (!File.Exists(path))
                return new HybridClrGenerationCache();
            try { return JsonUtility.FromJson<HybridClrGenerationCache>(File.ReadAllText(path)) ?? new HybridClrGenerationCache(); }
            catch { return new HybridClrGenerationCache(); }
        }

        private static void SaveCache(BuildContext context, HybridClrGenerationCache cache)
        {
            AtomicWriteAllText(GetCachePath(context), JsonUtility.ToJson(cache, true));
        }

        private static string GetCachePath(BuildContext context)
            => Path.Combine(context.Paths.CacheDir, "hybridclr_generation_cache.json");

        /// <summary>
        /// bridge 敏感源码哈希（带 mtime 短路）：只有含 MonoPInvokeCallback / DllImport / delegate* / calli
        /// 等标记的 .cs 文件才参与哈希。文件 (size, mtime) 未变时复用清单里缓存的「是否敏感 + 内容哈希」，
        /// 不重读文件；变了才重新读 + 重新判定 token。结果仍是全部敏感文件的确定性摘要。
        /// </summary>
        private static string HashBridgeSensitiveSources(string manifestPath)
        {
            var manifest = LoadBridgeSensitiveManifest(manifestPath);
            var index = new Dictionary<string, BridgeSensitiveManifestEntry>(StringComparer.Ordinal);
            foreach (var entry in manifest.Entries)
            {
                if (!string.IsNullOrEmpty(entry.Path))
                    index[entry.Path] = entry;
            }

            var files = RuntimeSourceRoots
                .Where(Directory.Exists)
                .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            foreach (string file in files)
            {
                string canonical = Path.GetFullPath(file).Replace('\\', '/');
                var fi = new FileInfo(canonical);
                long length = fi.Length;
                long mtime = fi.LastWriteTimeUtc.Ticks;

                index.TryGetValue(canonical, out var entry);
                bool isSensitive;
                string hash;
                if (entry != null && entry.Length == length && entry.MtimeTicks == mtime)
                {
                    // mtime 短路：复用缓存的敏感判定与哈希
                    isSensitive = entry.IsSensitive;
                    hash = entry.Hash;
                }
                else
                {
                    string text = File.ReadAllText(canonical);
                    isSensitive = BridgeSensitiveTokens.Any(token => text.IndexOf(token, StringComparison.Ordinal) >= 0);
                    if (isSensitive)
                    {
                        using var sha256 = SHA256.Create();
                        hash = ToHex(sha256.ComputeHash(Encoding.UTF8.GetBytes(text)));
                    }
                    else
                    {
                        hash = "";
                    }

                    if (entry == null)
                    {
                        entry = new BridgeSensitiveManifestEntry { Path = canonical };
                        index[canonical] = entry;
                    }
                    entry.Length = length;
                    entry.MtimeTicks = mtime;
                    entry.IsSensitive = isSensitive;
                    entry.Hash = hash;
                }

                if (isSensitive)
                    sb.Append(canonical).Append('|').Append(hash).Append('\n');
            }

            SaveBridgeSensitiveManifest(new BridgeSensitiveManifest { Entries = index.Values.ToList() }, manifestPath);
            using var sha = SHA256.Create();
            return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        private static string GetBridgeSensitiveManifestPath(BuildContext context)
            => Path.Combine(context.Paths.CacheDir, "bridge_sensitive_manifest.json");

        private static BridgeSensitiveManifest LoadBridgeSensitiveManifest(string path)
        {
            if (!File.Exists(path)) return new BridgeSensitiveManifest();
            try { return JsonUtility.FromJson<BridgeSensitiveManifest>(File.ReadAllText(path)) ?? new BridgeSensitiveManifest(); }
            catch { return new BridgeSensitiveManifest(); }
        }

        private static void SaveBridgeSensitiveManifest(BridgeSensitiveManifest manifest, string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonUtility.ToJson(manifest, true));
            }
            catch (Exception ex)
            {
                BuildLogger.Warn($"[P2] HybridCLR: failed to save bridge-sensitive manifest: {ex.Message}");
            }
        }

        private static string GetPathHashManifestPath(BuildContext context)
            => Path.Combine(context.Paths.CacheDir, "path_hash_manifest.json");

        /// <summary>
        /// 内容哈希（带 mtime 短路）：文件 (size, mtime) 未变时复用清单里的旧 SHA-256，不重读文件；
        /// 变了才重新计算。结果仍是对全部文件路径 + 内容的确定性摘要。第三方库 / 裁剪后 AOT DLL
        /// 基本不变，靠这一层把「几百 MB 逐字节哈希」降为「stat + 复用旧哈希」。
        /// </summary>
        private static string HashPathsCached(IEnumerable<string> paths, string manifestPath)
        {
            var manifest = LoadPathHashManifest(manifestPath);
            var index = new Dictionary<string, FileHashManifestEntry>(StringComparer.Ordinal);
            foreach (var entry in manifest.Entries)
            {
                if (!string.IsNullOrEmpty(entry.Path))
                    index[entry.Path] = entry;
            }

            var sb = new StringBuilder();
            foreach (string path in paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(path))
                {
                    AppendCachedPath(sb, path, index);
                }
                else if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                                 .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                        AppendCachedPath(sb, file, index);
                }
                else
                {
                    sb.Append("missing:").Append(path.Replace('\\', '/')).Append('\n');
                }
            }

            SavePathHashManifest(new FileHashManifest { Entries = index.Values.ToList() }, manifestPath);
            using var sha = SHA256.Create();
            return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        private static void AppendCachedPath(StringBuilder sb, string path, Dictionary<string, FileHashManifestEntry> index)
        {
            string canonical = Path.GetFullPath(path).Replace('\\', '/');
            sb.Append(canonical).Append('|').Append(GetFileContentHash(canonical, index)).Append('\n');
        }

        private static string GetFileContentHash(string canonicalPath, Dictionary<string, FileHashManifestEntry> index)
        {
            var fi = new FileInfo(canonicalPath);
            long length = fi.Length;
            long mtime = fi.LastWriteTimeUtc.Ticks;

            if (index.TryGetValue(canonicalPath, out var entry)
                && entry.Length == length
                && entry.MtimeTicks == mtime
                && !string.IsNullOrEmpty(entry.Hash))
                return entry.Hash;

            using var stream = File.OpenRead(canonicalPath);
            using var sha = SHA256.Create();
            string hash = ToHex(sha.ComputeHash(stream));

            if (entry == null)
            {
                entry = new FileHashManifestEntry { Path = canonicalPath };
                index[canonicalPath] = entry;
            }
            entry.Length = length;
            entry.MtimeTicks = mtime;
            entry.Hash = hash;
            return hash;
        }

        private static FileHashManifest LoadPathHashManifest(string path)
        {
            if (!File.Exists(path)) return new FileHashManifest();
            try { return JsonUtility.FromJson<FileHashManifest>(File.ReadAllText(path)) ?? new FileHashManifest(); }
            catch { return new FileHashManifest(); }
        }

        private static void SavePathHashManifest(FileHashManifest manifest, string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonUtility.ToJson(manifest, true));
            }
            catch (Exception ex)
            {
                BuildLogger.Warn($"[P2] HybridCLR: failed to save path hash manifest: {ex.Message}");
            }
        }

        private static string HashFile(string path)
        {
            if (!File.Exists(path)) return "missing";
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return ToHex(sha.ComputeHash(stream));
        }

        /// <summary>
        /// MethodBridge.cpp 缓存的输入键。故意不含 compiledDllHash（热更 DLL 全量内容）：
        /// 19 分钟的 methodBridgeAnalyzer.Run() 只用 CreateAOTAssemblyResolver 扫描裁剪后的 AOT 程序集，
        /// 完全不读热更程序集；热更侧只影响逆向 P/Invoke / calli / DllImport 三个快速分析器，
        /// 这部分由 bridgeInputHash（bridge 敏感源码哈希）覆盖。因此 ZLinq 等基本库不变、且未新增
        /// bridge 敏感标记时，即便修改普通业务代码也直接命中缓存，跳过全量泛型展开。
        /// </summary>
        private static string ComputeMethodBridgeCacheKey(
            string aotDllHash, string bridgeInputHash,
            string hybridClrProfileHash, bool developmentBuild)
        {
            string hybridClrVersion = "";
            try
            {
                hybridClrVersion = UnityEditor.PackageManager.PackageInfo
                    .FindForPackageName("com.code-philosophy.hybridclr")?.version ?? "";
            }
            catch { }

            var sb = new StringBuilder();
            sb.Append(aotDllHash).Append('\n');
            sb.Append(bridgeInputHash).Append('\n');
            sb.Append(hybridClrProfileHash).Append('\n');
            sb.Append(Application.unityVersion).Append('\n');
            sb.Append(hybridClrVersion).Append('\n');
            sb.Append(SettingsUtil.HybridCLRSettings.maxMethodBridgeGenericIteration).Append('\n');
            sb.Append(developmentBuild).Append('\n');
            using var sha = SHA256.Create();
            return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        private static void SaveMethodBridgeToCache(string methodBridgePath, string cachedPath)
        {
            try
            {
                string dir = Path.GetDirectoryName(cachedPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                AtomicFileCopy(methodBridgePath, cachedPath);
                BuildLogger.Info($"[P2] HybridCLR: MethodBridge cached: {cachedPath}");
            }
            catch (Exception ex)
            {
                BuildLogger.Warn($"[P2] HybridCLR: failed to cache MethodBridge: {ex.Message}");
            }
        }

        /// <summary>写临时文件后原子替换，避免中途崩溃留下半截损坏的缓存。</summary>
        private static void AtomicWriteAllText(string path, string content)
        {
            string tmpPath = path + ".tmp";
            File.WriteAllText(tmpPath, content);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(tmpPath, path);
        }

        /// <summary>先复制到临时文件再原子替换，避免中途崩溃留下半截损坏的缓存文件。</summary>
        private static void AtomicFileCopy(string sourcePath, string destinationPath)
        {
            string tmpPath = destinationPath + ".tmp";
            File.Copy(sourcePath, tmpPath, true);
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);
            File.Move(tmpPath, destinationPath);
        }

        private static void RestoreMethodBridgeFromCache(string methodBridgePath, string cachedPath)
        {
            string dir = Path.GetDirectoryName(methodBridgePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(cachedPath, methodBridgePath, true);
            BuildLogger.Info($"[P2] HybridCLR: MethodBridge restored from cache: {cachedPath}");
        }

        private static string ToHex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        private static bool StringEquals(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);

        private sealed class DevelopmentBuildScope : IDisposable
        {
            private readonly bool _previous;

            public DevelopmentBuildScope(bool value)
            {
                _previous = EditorUserBuildSettings.development;
                EditorUserBuildSettings.development = value;
            }

            public void Dispose()
            {
                EditorUserBuildSettings.development = _previous;
            }
        }

        [Serializable]
        private sealed class FileHashManifest
        {
            public List<FileHashManifestEntry> Entries = new List<FileHashManifestEntry>();
        }

        [Serializable]
        private sealed class FileHashManifestEntry
        {
            public string Path;
            public long Length;
            public long MtimeTicks;
            public string Hash;
        }

        [Serializable]
        private sealed class BridgeSensitiveManifest
        {
            public List<BridgeSensitiveManifestEntry> Entries = new List<BridgeSensitiveManifestEntry>();
        }

        [Serializable]
        private sealed class BridgeSensitiveManifestEntry
        {
            public string Path;
            public long Length;
            public long MtimeTicks;
            public bool IsSensitive;
            public string Hash;
        }

        [Serializable]
        private sealed class HybridClrGenerationCache
        {
            public string CompiledDllHash;
            public string LinkXmlHash;
            public string AotStripInputHash;
            public string AotDllHash;
            public string BridgeSensitiveHash;
            public string HybridClrProfileHash;
            public string MethodBridgeHash;
            public string AotGenericReferencesHash;
            public string UpdatedAtUtc;
        }
    }
}
