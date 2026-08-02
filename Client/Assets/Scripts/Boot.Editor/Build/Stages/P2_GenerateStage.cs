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

            BuildLogger.Info("[P2] HybridCLR: compiling hot-update DLLs...");
            BuildTelemetry.Measure("P2.CompileHotUpdateDlls", "HybridCLR",
                () => CompileDllCommand.CompileDll(target, profile.DevelopmentBuild));
            string compiledDllHash = HashDirectory(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target));

            BuildTelemetry.Measure("P2.GenerateIl2CppDef", "HybridCLR",
                Il2CppDefGeneratorCommand.GenerateIl2CppDef);

            BuildTelemetry.Measure("P2.GenerateLinkXml", "HybridCLR",
                () => LinkGeneratorCommand.GenerateLinkXml(target));
            string linkXmlHash = HashFile(LinkXmlPath);
            string aotStripInputHash = HashPaths(AotStripInputPaths);
            string currentAotDllHash = HashDirectory(SettingsUtil.GetAssembliesPostIl2CppStripDir(target));

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
                ? HashDirectory(SettingsUtil.GetAssembliesPostIl2CppStripDir(target))
                : currentAotDllHash;

            string bridgeInputHash = HashBridgeSensitiveSources();
            string methodBridgePath = Path.Combine(SettingsUtil.GeneratedCppDir, "MethodBridge.cpp");
            bool bridgeRequired = context.ForceFullRebuild
                || stripRequired
                || !File.Exists(methodBridgePath)
                || !StringEquals(cache.BridgeSensitiveHash, bridgeInputHash)
                || !StringEquals(cache.AotDllHash, aotDllHash)
                || !StringEquals(cache.HybridClrProfileHash, profile.ComputeHybridClrProfileHash())
                || !StringEquals(cache.MethodBridgeHash, HashFile(methodBridgePath));
            if (bridgeRequired)
            {
                BuildLogger.Info("[P2] HybridCLR: bridge inputs changed, generating MethodBridge.");
                BuildTelemetry.Measure("P2.GenerateMethodBridge", "HybridCLR",
                    () => MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target));
            }
            else
            {
                BuildLogger.Info("[P2] HybridCLR: MethodBridge cache verified.");
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
            File.WriteAllText(GetCachePath(context), JsonUtility.ToJson(cache, true));
        }

        private static string GetCachePath(BuildContext context)
            => Path.Combine(context.Paths.CacheDir, "hybridclr_generation_cache.json");

        private static string HashBridgeSensitiveSources()
        {
            var files = RuntimeSourceRoots
                .Where(Directory.Exists)
                .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
            using var sha = SHA256.Create();
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                if (!BridgeSensitiveTokens.Any(token => text.IndexOf(token, StringComparison.Ordinal) >= 0))
                    continue;
                byte[] bytes = Encoding.UTF8.GetBytes(file.Replace('\\', '/') + "\n" + text + "\n");
                sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return ToHex(sha.Hash);
        }

        private static string HashDirectory(string directory)
        {
            if (!Directory.Exists(directory)) return "missing";
            using var sha = SHA256.Create();
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                byte[] pathBytes = Encoding.UTF8.GetBytes(file.Replace('\\', '/') + "\n");
                sha.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);
                byte[] content = File.ReadAllBytes(file);
                sha.TransformBlock(content, 0, content.Length, content, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return ToHex(sha.Hash);
        }

        private static string HashPaths(IEnumerable<string> paths)
        {
            using var sha = SHA256.Create();
            foreach (string path in paths.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(path))
                {
                    AppendFileToHash(sha, path);
                    continue;
                }

                if (!Directory.Exists(path))
                {
                    byte[] missing = Encoding.UTF8.GetBytes("missing:" + path.Replace('\\', '/') + "\n");
                    sha.TransformBlock(missing, 0, missing.Length, missing, 0);
                    continue;
                }

                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                             .Where(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                             .OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
                    AppendFileToHash(sha, file);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return ToHex(sha.Hash);
        }

        private static void AppendFileToHash(HashAlgorithm hash, string path)
        {
            byte[] pathBytes = Encoding.UTF8.GetBytes(path.Replace('\\', '/') + "\n");
            hash.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);
            var buffer = new byte[64 * 1024];
            using var stream = File.OpenRead(path);
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                hash.TransformBlock(buffer, 0, read, buffer, 0);
        }

        private static string HashFile(string path)
        {
            if (!File.Exists(path)) return "missing";
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return ToHex(sha.ComputeHash(stream));
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
