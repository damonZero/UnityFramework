namespace Framework.BuildPipeline.Diagnostics
{
    /// <summary>
    /// 构建管线错误码常量 —— 稳定、可被 CI/AI 解析。
    /// 格式: KJ-BUILD-{PHASE}-{ID}
    /// </summary>
    public static class BuildErrorCodes
    {
        // Plan
        public const string PlanInvalidProfile = "KJ-BUILD-PLAN-001";
        public const string PlanDependencyCycle = "KJ-BUILD-PLAN-002";

        // Preflight
        public const string PreHybridCLRNotInstalled = "KJ-BUILD-PRE-001";
        public const string PrePlatformMismatch = "KJ-BUILD-PRE-002";
        public const string PreBootSceneMissing = "KJ-BUILD-PRE-003";
        public const string PreAssetConfigMissing = "KJ-BUILD-PRE-004";
        public const string PreIL2CPPRequired = "KJ-BUILD-PRE-005";
        public const string PreAndroidModuleMissing = "KJ-BUILD-PRE-006";
        public const string PreIOSModuleMissing = "KJ-BUILD-PRE-007";
        public const string PreDiskSpaceLow = "KJ-BUILD-PRE-008";
        public const string PreUnityVersionMismatch = "KJ-BUILD-PRE-009";
        public const string PreAsmdefDependencyViolation = "KJ-BUILD-PRE-010";

        // Generate
        public const string GenHybridCLRFailed = "KJ-BUILD-GEN-001";
        public const string GenLinkXmlMissing = "KJ-BUILD-GEN-002";

        // HybridCLR
        public const string HybCompileFailed = "KJ-BUILD-HYB-001";
        public const string HybAssemblyCountMismatch = "KJ-BUILD-HYB-002";
        public const string HybAOTMetadataMissing = "KJ-BUILD-HYB-003";
        public const string HybDllSyncFailed = "KJ-BUILD-HYB-004";

        // YooAsset
        public const string YooBuildFailed = "KJ-BUILD-YOO-001";
        public const string YooManifestMissing = "KJ-BUILD-YOO-002";
        public const string YooHotupdateBundleEmpty = "KJ-BUILD-YOO-003";

        // Config
        public const string ConfigWriteFailed = "KJ-BUILD-CONFIG-001";
        public const string ConfigRollbackFailed = "KJ-BUILD-CONFIG-002";

        // Player
        public const string PlayerBuildFailed = "KJ-BUILD-PLAYER-001";
        public const string PlayerNotFound = "KJ-BUILD-PLAYER-002";
        public const string PlayerEmpty = "KJ-BUILD-PLAYER-003";
        public const string PlayerGradleFailed = "KJ-BUILD-PLAYER-004";
        public const string PlayerSigningFailed = "KJ-BUILD-PLAYER-005";

        // Verify
        public const string VerifyDllCountMismatch = "KJ-BUILD-VERIFY-001";
        public const string VerifyBundleContentMissing = "KJ-BUILD-VERIFY-002";
        public const string VerifyConfigInconsistent = "KJ-BUILD-VERIFY-003";

        // Smoke
        public const string SmokeDeviceNotFound = "KJ-BUILD-SMOKE-001";
        public const string SmokeAdbNotFound = "KJ-BUILD-SMOKE-002";
        public const string SmokeBootLogMissing = "KJ-BUILD-SMOKE-003";
        public const string SmokeMilestoneMissing = "KJ-BUILD-SMOKE-004";
        public const string SmokeBootErrors = "KJ-BUILD-SMOKE-005";
        public const string SmokeTimeout = "KJ-BUILD-SMOKE-006";
        public const string SmokeRequiredButNoDevice = "KJ-BUILD-SMOKE-007";

        // Formal
        public const string FormalDebugBuildLeak = "KJ-BUILD-FORMAL-001";
        public const string FormalScriptDebuggingLeak = "KJ-BUILD-FORMAL-002";
        public const string FormalGmEnabled = "KJ-BUILD-FORMAL-003";
        public const string FormalDebugUiEnabled = "KJ-BUILD-FORMAL-004";
        public const string FormalSigningMissing = "KJ-BUILD-FORMAL-005";
        public const string FormalDefineLeak = "KJ-BUILD-FORMAL-006";

        // Report
        public const string ReportWriteFailed = "KJ-BUILD-REPORT-001";
        public const string ReportArchiveFailed = "KJ-BUILD-REPORT-002";
    }
}
