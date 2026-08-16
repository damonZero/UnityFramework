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
    /// 热更补丁发布工具。不执行 P2 HybridCLR 生成或 BuildPlayer。
    /// </summary>
    public static class HostUpdatePublisher
    {
        /// <summary>
        /// 默认 CDN 根目录（相对仓库根 KJ）——与 CDN 服务器 server.py 的 Web 根（Server/Res）一致。
        /// </summary>
        public const string DefaultCdnRelativeRoot = "Server/Res/CDN/Android/DefaultPackage";

        /// <summary>CDN 根目录（相对仓库根 KJ 解析，即 Server/Res/CDN/Android/DefaultPackage）。</summary>
        public static readonly string ServerRoot = Path.Combine(
            GetRepoRoot(),
            DefaultCdnRelativeRoot);

        /// <summary>仓库根目录（KJ）——Unity 工程（Client）的父目录。</summary>
        public static string GetRepoRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            return Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
        }

        [MenuItem("KJ/Build/Publish Host Update 1.0.1")]
        public static void PublishMenu() => Publish("1.0.1");

        /// <summary>
        /// 完整热更补丁发布：编译热更 DLL + 同步 + 构建 RawFile + 复制到 CDN。
        /// 适用于手动发布补丁（不跑完整 P0-P9 构建）。
        /// </summary>
        public static void Publish(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("version is required", nameof(version));

            var target = BuildTarget.Android;
            // SyncExistingOutputs() 内部读取 EditorUserBuildSettings.activeBuildTarget，
            // 而这里硬编码发布 Android —— 必须先切换到目标平台再同步，结束后恢复。
            BuildTarget previousTarget = EditorUserBuildSettings.activeBuildTarget;
            bool switched = false;
            if (previousTarget != target)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    UnityEditor.BuildPipeline.GetBuildTargetGroup(target), target))
                {
                    throw new InvalidOperationException($"Failed to switch active build target to {target} for publishing.");
                }
                switched = true;
            }

            try
            {
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
                    ClearBuildCacheFiles = false,
                };

                var result = new RawFileBuildPipeline().Run(parameters, true);
                if (!result.Success)
                    throw new InvalidOperationException($"Host package build failed: {result.ErrorInfo}");

                string source = Path.Combine(outputRoot, "Android", "DefaultPackage", version);
                CopyBuildOutputToCdn(source, ServerRoot, version);
            }
            finally
            {
                if (switched)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(
                        UnityEditor.BuildPipeline.GetBuildTargetGroup(previousTarget), previousTarget);
                }
            }
        }

        /// <summary>
        /// 把 YooAsset 构建输出复制到 CDN（轻量发布，复用构建管线 P4 的产物，不重新构建）。
        /// 供构建管线 P10_PublishCdnStage 调用。
        /// </summary>
        public static void PublishFromBuildOutput(string sourceDir, string version, string cdnRoot = null)
        {
            if (string.IsNullOrWhiteSpace(sourceDir) || !Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException($"YooAsset build output not found: {sourceDir}");
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("version is required", nameof(version));

            string targetRoot = string.IsNullOrWhiteSpace(cdnRoot) ? ServerRoot : cdnRoot;
            CopyBuildOutputToCdn(sourceDir, targetRoot, version);
            BuildLogger.Info($"[HostUpdatePublisher] Published build output to CDN: {targetRoot}");
        }

        private static void CopyBuildOutputToCdn(string source, string cdnRoot, string version)
        {
            string archive = Path.Combine(cdnRoot, version);
            // 先铺平复制到 cdnRoot，再复制版本归档。CopyDirectory 会先 Delete 目标目录，
            // 若先写 cdnRoot/version 再写 cdnRoot，后者会把刚写好的版本子目录整个删掉。
            CopyDirectory(source, cdnRoot);
            CopyDirectory(source, archive);
            BuildLogger.Info($"[HostUpdatePublisher] Published {version} to {cdnRoot}");
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
