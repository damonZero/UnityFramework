using System;
using System.Collections.Generic;
using System.IO;
using Framework.BuildPipeline.Plan;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P10 Publish CDN — 构建成功后，把 P4 的 YooAsset 输出发布到 CDN（可勾选）。
    /// 仅当 BuildProfile.PublishToCdn 为 true 时执行；复制 P4 产物到
    /// Server/Res/CDN/Android/DefaultPackage（相对仓库根 KJ 解析），
    /// 供 Host 模式设备从 CDN 下载最新热更资源。
    /// </summary>
    public class P10_PublishCdnStage : BuildStageBase
    {
        public override string Id => "P10.PublishCdn";
        public override string DisplayName => "Publish to CDN";
        public override int Order => 10;
        public override string Category => "CDN";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P9.Report" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Optional | BuildStagePolicy.NoSkip;

        public override BuildStageInputs GetInputs(BuildContext context)
        {
            // NoSkip 策略保证总是执行（不依赖指纹）；条件由 Execute 内 PublishToCdn 判断。
            // 依赖在 Stage 级 DependsOn 声明（P9.Report），这里不重复。
            return new BuildStageInputs { AlwaysRun = true };
        }

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
            => new BuildStageOutputs();

        public override void Execute(BuildContext context)
        {
            var profile = context.Profile ?? throw new InvalidOperationException("BuildProfile is required");

            if (!profile.PublishToCdn)
            {
                BuildLogger.Info("[P10] PublishToCdn disabled, skipping");
                return;
            }

            string cdnRoot = profile.GetCdnRoot();
            if (string.IsNullOrWhiteSpace(cdnRoot))
            {
                BuildLogger.Warn("[P10] CdnServerRoot is empty, skipping CDN publish");
                return;
            }

            string version = string.IsNullOrWhiteSpace(profile.CdnPublishVersion)
                ? profile.VersionName
                : profile.CdnPublishVersion;

            // P4 的 YooAsset 输出：Bundles/{Platform}/{PackageName}/{version}
            string outputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot();
            string sourceDir = Path.Combine(outputRoot, profile.Platform.ToString(), profile.PackageName, version);

            BuildLogger.Info($"[P10] Publishing to CDN: {sourceDir} -> {cdnRoot} (version={version})");

            if (!Directory.Exists(sourceDir))
                throw new BuildFailedException(Id, $"YooAsset build output not found: {sourceDir}");

            HostUpdatePublisher.PublishFromBuildOutput(sourceDir, version, cdnRoot);

            // 记录产物
            context.AddArtifact(cdnRoot, "CDN hot-update package", 0);
            BuildLogger.Info("[P10] CDN publish DONE");
        }

        public override void Verify(BuildContext context)
        {
            var profile = context.Profile ?? throw new InvalidOperationException("BuildProfile is required");
            if (!profile.PublishToCdn)
            {
                BuildLogger.Info("[P10] ✓ CDN publish skipped (disabled)");
                return;
            }

            string cdnRoot = profile.GetCdnRoot();
            if (string.IsNullOrWhiteSpace(cdnRoot)) return;

            string version = string.IsNullOrWhiteSpace(profile.CdnPublishVersion)
                ? profile.VersionName
                : profile.CdnPublishVersion;
            string versionFile = Path.Combine(cdnRoot, "DefaultPackage.version");
            string manifest = Path.Combine(cdnRoot, $"DefaultPackage_{version}.bytes");

            if (!File.Exists(versionFile))
                throw new InvalidOperationException($"CDN version file missing: {versionFile}");
            if (!File.Exists(manifest))
                throw new InvalidOperationException($"CDN manifest missing: {manifest}");

            BuildLogger.Info($"[P10] ✓ CDN publish verified (version={version})");
        }
    }
}
