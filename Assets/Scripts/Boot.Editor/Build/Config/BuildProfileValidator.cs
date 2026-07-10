using System.Collections.Generic;
using System.Text;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Environment;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// BuildProfile 校验器 —— 在构建前执行规则集，输出结构化 BuildIssue 列表。
    /// </summary>
    public static class BuildProfileValidator
    {
        public static List<BuildIssue> Validate(BuildProfile profile)
        {
            var issues = new List<BuildIssue>();

            if (profile == null)
            {
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.PlanInvalidProfile, "P0.Plan",
                    "BuildProfile is null"));
                return issues;
            }

            // 版本号
            if (string.IsNullOrWhiteSpace(profile.VersionName))
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.PlanInvalidProfile, "P0.Plan",
                    "VersionName is empty"));

            if (profile.VersionCode <= 0)
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.PlanInvalidProfile, "P0.Plan",
                    "VersionCode must be positive"));

            // 包名
            if (string.IsNullOrWhiteSpace(profile.PackageName))
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.PlanInvalidProfile, "P0.Plan",
                    "PackageName is empty"));

            // Formal / Audit 强约束
            if (profile.Environment == BuildEnvironment.Formal
                || profile.Environment == BuildEnvironment.Audit)
            {
                ValidateFormalProfile(profile, issues);
            }

            // Host 模式需要 CDN
            if (profile.CdnBaseUrl != "")
            {
                // 这需要校验 YooAsset Mode，但在纯契约层不能访问 AssetConfig，
                // Profile 中可通过其他方式表达；此处仅验证 URL 格式
            }

            // Android Formal 签名
            if (profile.Platform == BuildTarget.Android
                && profile.RequireSigning)
            {
                if (string.IsNullOrWhiteSpace(profile.KeystorePath))
                    issues.Add(BuildIssue.Error(
                        BuildErrorCodes.FormalSigningMissing, "P0.Plan",
                        "KeystorePath is required for Formal/Audit Android builds",
                        "Signing was not configured", "Set KeystorePath in BuildProfile"));
                if (string.IsNullOrWhiteSpace(profile.KeystoreAlias))
                    issues.Add(BuildIssue.Error(
                        BuildErrorCodes.FormalSigningMissing, "P0.Plan",
                        "KeystoreAlias is required for Formal/Audit Android builds"));
                if (string.IsNullOrWhiteSpace(profile.PackageId))
                    issues.Add(BuildIssue.Error(
                        BuildErrorCodes.FormalSigningMissing, "P0.Plan",
                        "PackageId (applicationIdentifier) is required"));
            }

            // Smoke 必须跑且不可跳过
            if (profile.IsSmokeMandatory)
            {
                if (!profile.SmokeEnabled)
                    issues.Add(BuildIssue.Warning(
                        BuildErrorCodes.PlanInvalidProfile, "P0.Plan",
                        "Formal/Audit environment should have SmokeEnabled=true"));
            }

            return issues;
        }

        private static void ValidateFormalProfile(BuildProfile profile, List<BuildIssue> issues)
        {
            if (profile.DevelopmentBuild)
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.FormalDebugBuildLeak, "P0.Plan",
                    $"DevelopmentBuild must be false for {profile.Environment}",
                    "Debug symbols and extra logging would leak into release build",
                    "Set DevelopmentBuild=false in the BuildProfile"));

            if (profile.ScriptDebugging)
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.FormalScriptDebuggingLeak, "P0.Plan",
                    $"ScriptDebugging must be false for {profile.Environment}"));

            if (profile.EnableGm)
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.FormalGmEnabled, "P0.Plan",
                    $"EnableGm must be false for {profile.Environment}"));

            if (profile.EnableDebugUi)
                issues.Add(BuildIssue.Error(
                    BuildErrorCodes.FormalDebugUiEnabled, "P0.Plan",
                    $"EnableDebugUi must be false for {profile.Environment}"));
        }
    }
}
