using System;
using System.IO;
using Boot.Editor.HybridCLR;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Host update publisher. It never runs HybridCLR GenerateAll or BuildPlayer.
    /// </summary>
    public static class HostUpdatePublisher
    {
        private const string ServerRoot = "C:/ZZS/Project/NewProjectK/Server/CDN/Android/DefaultPackage";

        [MenuItem("KJ/Build/Publish Host Update 1.0.1")]
        public static void PublishMenu() => Publish("1.0.1");

        public static void Publish(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("version is required", nameof(version));

            var target = BuildTarget.Android;
            CompileDllCommand.CompileDll(target, true);
            KJHybridClrBuildTools.SyncExistingOutputs();

            var outputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot();
            var parameters = new RawFileBuildParameters
            {
                BuildOutputRoot = outputRoot,
                BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString(),
                BuildBundleType = (int)EBundleType.RawBundle,
                BuildTarget = target,
                PackageName = "DefaultPackage",
                PackageVersion = version,
                FileNameStyle = EFileNameStyle.HashName,
                VerifyBuildingResult = true,
                BundledCopyOption = EBundledCopyOption.ClearAndCopyAll,
                ClearBuildCacheFiles = true,
            };

            var result = new RawFileBuildPipeline().Run(parameters, true);
            if (!result.Success)
                throw new InvalidOperationException($"Host package build failed: {result.ErrorInfo}");

            string source = Path.Combine(outputRoot, "Android", "DefaultPackage", version);
            string archive = Path.Combine(ServerRoot, version);
            CopyDirectory(source, archive);
            CopyDirectory(source, ServerRoot);
            Debug.Log($"[HostUpdatePublisher] Published {version} to {ServerRoot}");
        }

        private static void CopyDirectory(string source, string destination)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException(source);
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }
    }
}
