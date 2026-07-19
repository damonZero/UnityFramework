---
name: kj-build-pipeline
description: >
  KJ 构建打包管线指南。覆盖 BuildProfile-only 配置、P0-P9 IBuildStage
  插件化执行、BuildPipelineRunner、真实 Stage fingerprint、BuildTransaction
  项目状态回滚、YooAsset/HybridCLR/Player 构建、静态校验、Runtime smoke、
  结构化报告、Odin Dashboard 和 CI 无头入口。
metadata:
  doc: ProgressDoc/Discuss/资源系统/Hy3_构建打包全流程管线_需求分析与设计.md
  layer: Boot.Editor + Framework.BuildPipeline
  asmdef: Boot.Build.Editor, Framework.BuildPipeline
---

# KJ 构建打包管线

当前实现只有一套架构：

- 配置：`BuildProfile`
- 执行：`BuildPipelineRunner`
- Stage：P0-P9 `IBuildStage`
- 增量依据：`state/{StageId}.fingerprint.json`
- 临时项目修改：`BuildTransaction`
- 报告：`BuildReportData` + `BuildReportWriter`

不要恢复或新增 `BuildConfig`、旧 `BuildReport`、`StageDependencyTracker`、
`.markers`、bool mask 或手动选择部分 Stage 的兼容逻辑。

## 目录

```text
Assets/Framework/BuildPipeline/
├── Environment/BuildEnvironment.cs
├── Plan/BuildPlan.cs
├── Plan/BuildStageInputs.cs
├── Plan/BuildStageOutputs.cs
├── Plan/BuildStageFingerprint.cs
├── Plan/BuildSkipDecision.cs
├── Diagnostics/BuildIssue*.cs
├── Reports/BuildArtifactManifest.cs
├── Reports/AiBuildHandoff.cs
└── CI/BuildExitCode.cs

Assets/Scripts/Boot.Editor/Build/
├── Config/
│   ├── BuildProfile.cs
│   ├── BuildProfile.asset
│   ├── BuildProfileValidator.cs
│   └── BuildProfileSet.cs
├── Pipeline/
│   ├── BuildContext.cs
│   ├── BuildPaths.cs
│   ├── BuildEnvironmentSnapshot.cs
│   ├── IBuildStage.cs
│   ├── BuildStageBase.cs
│   ├── BuildStagePolicy.cs
│   ├── BuildStageRegistry.cs
│   ├── BuildPipelineRunner.cs
│   └── BuildTransaction.cs
├── Stages/P0_PlanStage.cs ... P9_ReportStage.cs
├── Diagnostics/
├── UI/BuildDashboardWindow.cs
├── CI/BuildCommandLine.cs
└── KJBuildPipeline.cs
```

## P0-P9

```text
P0 Plan         — 校验 Profile、BuildPlan、输出目录
P1 Preflight    — HybridCLR、平台、BootScene、AssetConfig、Android/Formal 预检
P2 Generate     — HybridCLR GenerateAll、link.xml
P3 HybridCLR    — 编译热更 DLL、AOT metadata、同步 .dll.bytes
P4 Assets       — YooAsset ScriptableBuildPipeline → StreamingAssets
P5 ApplyConfig  — 事务化写 AssetConfig.Mode 和 Scripting Defines
P6 Player       — 事务化切 IL2CPP/build flags，BuildPipeline.BuildPlayer
P7 Verify       — Player、YooAsset、DLL、metadata、Formal 泄露校验
P8 Smoke        — Standalone/Android 多里程碑启动验证
P9 Report       — Editor/Runtime 日志归档；Runner 随后写正式报告
```

硬顺序：P3 → P4 → P5 → P6 → P7 → P8 → P9。

## BuildProfile

默认资产：
`Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset`

Profile 是唯一配置源，包含：

- Environment / Channel
- VersionName / VersionCode
- Platform
- Android PackageId / signing
- DevelopmentBuild / ScriptDebugging / Profiler / Defines
- YooAsset PackageName / CDN
- RuntimeLog / GM / Debug UI
- SmokeEnabled / SmokeRequired / device / timeout
- OutputRoot / retention

Formal/Audit 必须关闭 Development、Script Debugging、GM、Debug UI；Android
必须配置签名；Smoke 缺 adb 或设备时不得静默通过。

## Fingerprint 增量规则

Runner 对每个非 AlwaysRun/NoSkip Stage：

1. 读取上次成功 fingerprint。
2. 检查声明的 required outputs 是否存在。
3. 计算并比较 Profile hash、输入路径状态、工具版本、Stage/Pipeline 版本。
4. 匹配才允许跳过。
5. Execute + Verify 成功后由 Runner 写新 fingerprint。

Stage 不允许自行写增量状态。

## 事务规则

所有临时项目修改必须先通过 `BuildTransaction` snapshot：

- `AssetConfig.asset`
- Scripting Define Symbols
- ScriptingBackend
- `EditorUserBuildSettings.development`
- `EditorUserBuildSettings.allowDebugging`
- 后续新增的签名或 PlayerSettings

Runner 在失败、取消和成功结束时统一 rollback，保证 Editor 状态不被构建污染。

## 入口

Editor：

```text
KJ/Build/Dashboard
```

CI：

```bash
Unity -batchmode -quit -projectPath <project> \
  -executeMethod Boot.Editor.Build.BuildCommandLine.Run \
  -profile Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset \
  -outputRoot BuildBackup/CI
```

代码：

```csharp
BuildReportData report = KJBuildPipeline.Build(profile);
```

## 报告与诊断

输出位于 `{BuildProfile.GetOutputDir()}/reports/`：

- `build_report.json`
- `build_report.md`
- `ai_handoff.json`

`BuildReportData` schema 1.1.0 还包含 `PerformanceSpans`。`BuildPipelineRunner`
创建 `Framework.Aop` session，P2/P3/P4/P6 通过
`Boot.Editor.Build.Telemetry.BuildTelemetry` 记录内部步骤耗时。耗时使用
`Stopwatch` 单调时钟；Collector/Sink 故障不得改变构建结果。
当前 `Aop.asmdef` 是 Editor-only、`autoReferenced=false`，不进入 Player 或改变
HybridCLR 热更新程序集边界。

当前监控点：

- P2：HybridCLR GenerateAll
- P3：清理输出、编译热更新 DLL、同步资源、AssetDatabase Refresh
- P4：清理 StreamingAssets、YooAsset package build、AssetDatabase Refresh
- P6：AssetDatabase Refresh、BuildPipeline.BuildPlayer

Runtime smoke 默认读取 `boot.log` 与 `latest.jsonl`。必须命中：

1. `[BootLoader] YooAsset`
2. `[BootLoader] all DLLs loaded`
3. `[AssetSystem] Ready`
4. `[SystemManager]`

## 当前验证状态

- Unity Editor 编译：已通过（2026-07-19，含 Aop/Boot.Build.Editor/Tests）
- 旧配置/报告/marker/mask 文件：已删除
- AOP/Build Pipeline 定向 EditMode：14/14 全绿
- P0-P9 Standalone 端到端构建：待执行
- Android + ADB smoke：待执行
- Odin Dashboard 手工验证：待执行

## 修改要求

修改管线代码时同步：

- `AGENTS.md`
- `.planning/STATE.md`
- `.planning/ROADMAP.md`
- `ProgressDoc/Result/hybridclr_workflow.md` §4
- `ProgressDoc/Discuss/资源系统/Hy3_构建打包全流程管线_需求分析与设计.md`
- 本 Skill
