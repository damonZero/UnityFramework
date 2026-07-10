---
name: kj-build-pipeline
description: >
  KJ 构建打包全流程管线（Boot.Build.Editor + Framework.BuildPipeline）。覆盖 P0-P9 IBuildStage 插件化管线（Plan→Preflight→Generate→HybridCLR→BuildAsset→ApplyConfig→BuildPlayer→Verify→Smoke→Report）、BuildProfile 多环境配置（Dev/QA/Profiling/Audit/Formal）、BuildPipelineRunner Plan 驱动编排器 + 事务系统、BuildIssue 结构化诊断 + BuildErrorCodes 稳定错误码、SmokeLogParser 多里程碑冒烟判定、FormalLeakageVerifier 发布包泄露检查、BuildDashboardWindow Odin 六视图面板、BuildCommandLine CI 无头入口。触发场景：Player 打包、CI 构建、增量构建排查、Profile 配置、冒烟调试、产物校验、AI 诊断。
metadata:
  doc: ProgressDoc/Discuss/Hy3_构建打包全流程管线_需求分析与设计.md
  layer: Boot.Editor + Framework.BuildPipeline
  asmdef: Boot.Build.Editor, Framework.BuildPipeline
---

# KJ 构建打包全流程管线

源码分两层：
- **纯数据契约**：`Assets/Framework/BuildPipeline/`（`Framework.BuildPipeline.asmdef`，不引用 UnityEditor/Boot/Core）
- **Editor 执行层**：`Assets/Scripts/Boot.Editor/Build/`（`Boot.Build.Editor.asmdef`）

设计文档见 `ProgressDoc/Discuss/资源系统/Hy3_构建打包全流程管线_需求分析与设计.md`（附录 E/F：工业级重构设计）。

## 架构速查

```
Assets/Framework/BuildPipeline/              — 纯契约程序集
├── Framework.BuildPipeline.asmdef
├── Environment/BuildEnvironment.cs            — Dev/QA/Profiling/Audit/Formal/Pre 枚举
├── Plan/
│   ├── BuildPlan.cs                           — 构建计划 + 跳过/运行计数
│   ├── BuildStageInputs.cs                    — Stage 输入规格
│   ├── BuildStageOutputs.cs                   — Stage 预期输出
│   ├── BuildStageFingerprint.cs               — Stage 指纹（Pipeline/stage 版本 + hash）
│   └── BuildSkipDecision.cs                   — 跳过决策（原因代码/证据）
├── Diagnostics/
│   ├── BuildIssue.cs                          — 结构化问题（Code/Severity/StageId/Evidence/SuggestedFix）
│   ├── BuildIssueSeverity.cs                  — Error/Warning/Info
│   └── BuildErrorCodes.cs                     — 50+ 稳定错误码
├── Reports/
│   ├── BuildArtifactManifest.cs               — 产物清单（路径/大小/SHA256）
│   └── AiBuildHandoff.cs                      — AI 可读交接数据
└── CI/BuildExitCode.cs                        — CI 退出码（0/10/20/…/99）

Assets/Scripts/Boot.Editor/Build/              — Editor 执行层
├── Config/
│   ├── BuildProfile.cs                        — ScriptableObject 多环境配置（替代旧 BuildConfig）
│   ├── BuildProfileValidator.cs               — Formal/Audit 强约束校验
│   └── BuildProfileSet.cs                     — Profile 集合
├── Pipeline/
│   ├── BuildContext.cs                        — 单次构建上下文（RunId/Plan/Artifacts/Issues/Transaction）
│   ├── BuildPaths.cs                          — 输出路径集
│   ├── BuildEnvironmentSnapshot.cs            — Unity/Git/OS/SDK 版本快照
│   ├── IBuildStage.cs                         — Stage 接口
│   ├── BuildStageBase.cs                      — Stage 抽象基类
│   ├── BuildStagePolicy.cs                    — Stage 策略标志
│   ├── BuildStageRegistry.cs                  — Stage 注册/排序/依赖验证
│   ├── BuildPipelineRunner.cs                 — Plan 驱动编排器 + 报告写入
│   └── BuildConfigTransaction.cs              — 事务系统（文件/PlayerSettings snapshot + rollback）
├── Stages/
│   ├── P0_PlanStage.cs                        — P0 计划生成
│   ├── P1_PreflightStage.cs                   — P1 环境预检
│   ├── P2_GenerateStage.cs                    — P2 HybridCLR 生成
│   ├── P3_HybridCLRStage.cs                   — P3 编译 DLL + AOT metadata + 同步
│   ├── P4_BuildAssetStage.cs                  — P4 YooAsset 生产构建
│   ├── P5_ApplyConfigStage.cs                 — P5 写入运行时配置（事务化）
│   ├── P6_BuildPlayerStage.cs                 — P6 Unity Player 构建
│   ├── P7_VerifyStage.cs                      — P7 产物静态校验 + Formal 泄露检查
│   ├── P8_SmokeStage.cs                       — P8 多里程碑冒烟
│   └── P9_ReportStage.cs                      — P9 报告归档
├── Diagnostics/
│   ├── SmokeLogParser.cs                      — 多里程碑判定（Launcher→YooAsset→HybridCLR→Boot→Core）
│   ├── FormalLeakageVerifier.cs               — Formal/Audit 泄露检查
│   ├── BuildAnalyzer.cs                       — 问题分类/合并/推荐
│   └── BuildKnowledgeBase.cs                  — 常见错误 → 修复建议映射
├── UI/
│   └── BuildDashboardWindow.cs                — OdinMenuEditorWindow 六视图面板
├── CI/
│   └── BuildCommandLine.cs                    — batchmode 入口
├── BuildConfig.cs                             — 旧构建配置（保留兼容）
├── BuildReport.cs                             — 旧报告结构（保留兼容）
├── BuildStagePanel.cs                         — 旧 EditorWindow（保留，跳转 Dashboard）
├── StageDependencyTracker.cs                  — 差量检测引擎（已更新为 P0-P9 ID）
└── KJBuildPipeline.cs                         — 编排器入口（委托 BuildPipelineRunner）
```

## 10-Stage 管线（P0-P9）

```
P0  Plan              — 验证 Profile、生成 BuildPlan、初始化输出目录
P1  Preflight         — 全维度预检（HC 运行时/平台/BootScene/AssetConfig/IL2CPP/Android/Formal）
P2  Generate          — HybridCLR GenerateAll + link.xml 校验
P3  HybridCLR         — 编译热更 DLL + AOT metadata + 同步 .dll.bytes + 清理过期文件
P4  BuildAsset        — YooAsset ScriptableBuildPipeline 生产构建 → StreamingAssets
P5  ApplyConfig       — 事务化 AssetConfig YAML 写入 + ScriptingDefines（按环境）
P6  BuildPlayer       — BuildPipeline.BuildPlayer(IL2CPP) + Android 工具链
P7  Verify            — Player/StreamingAssets/DLL 数量校验 + Formal 泄露检查
P8  Smoke             — 多里程碑冒烟（Launcher→YooAsset→HybridCLR→Boot→Core，含 Android ADB）
P9  Report            — 复制 Editor.log + Runtime 日志到归档目录
```

**排序硬约束**：P3→P4→P6（DLL 先编译同步 → YooAsset 打包 → BuildPlayer 嵌入）。P5→P6（配置落盘后才构建）。

## 双配置模型

### BuildProfile（新，推荐）
`Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset`

环境驱动：Dev/QA/Profiling/Audit/Formal。覆盖平台、签名、日志、Smoke、输出路径。

| 分组 | 关键字段 |
|------|---------|
| Identity | `ProfileName`, `Environment`, `Channel` |
| Version | `VersionName`, `VersionCode` |
| Platform | `Platform` |
| Android | `PackageId`, `KeystorePath`, `KeystoreAlias` |
| Build | `DevelopmentBuild`, `ScriptDebugging`, `EnableProfiler`, `ExtraScriptingDefines` |
| YooAsset | `PackageName`, `AssetDownloadTag`, `StartupTypeName`, `CdnBaseUrl` |
| Logging | `EnableRuntimeLog` |
| Feature Flags | `EnableGm`, `EnableDebugUi` |
| Smoke | `SmokeEnabled`, `SmokeRequired`, `SmokeDeviceSerial`, `SmokeTimeoutSec` |
| Output | `OutputRoot`, `KeepLastBuildCount` |

**Formal/Audit 强约束**（`BuildProfileValidator`）：
- `DevelopmentBuild=false`, `ScriptDebugging=false`, `EnableGm=false`, `EnableDebugUi=false`
- Android 签名必填

### BuildConfig（旧，保留兼容）
路径 `Assets/Scripts/Boot.Editor/Build/BuildConfig.asset`。菜单入口继续使用此配置，内部委托给 `BuildPipelineRunner`。

## 入口与调用方式

### Editor 菜单

```
KJ/
├── Build/
│   ├── Dashboard                                — Odin 六视图面板（Profile/Plan/Stage/Reports/Artifacts/Diagnostics）
│   ├── Full Player Build & Validate             — 全量构建（清除标记后跑全部 P0-P9）
│   ├── Incremental Build (Auto-detect changes)  — 差量构建（自动检测变更）
│   ├── Build Stage Manager...                   — 旧 EditorWindow 面板（跳转 Dashboard）
│   ├── Clear All Stage Markers                  — 清除全部标记
│   ├── Create BuildConfig                       — 创建 BuildConfig.asset
│   └── Create Build Profile                     — 创建 BuildProfile.asset
├── HybridCLR/                                   — 保留：14 个开发内循环菜单项（不变）
```

### CI 无头入口

```bash
# 通过 BuildConfig（兼容旧方式）
Unity -batchmode -quit -executeMethod Boot.Editor.Build.KJBuildPipeline.BuildFromCommandLine \
  -platform:Android -development:true -version:1.0.0

# 通过 BuildProfile（新方式）
Unity -batchmode -quit -executeMethod Boot.Editor.Build.BuildCommandLine.Run \
  -profile Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset \
  -mode Full -outputRoot BuildBackup
```

### 代码调用

```csharp
// 通过 BuildConfig + Runner
var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
var report = KJBuildPipeline.Build(config);                     // 全量
var report = KJBuildPipeline.IncrementalBuild(config);          // 差量检测

// 通过 BuildProfile + Runner（新架构）
var context = new BuildContext { Profile = profile };
var runner = new BuildPipelineRunner(context);
var reportData = runner.Run();
```

## IBuildStage 接口

```csharp
public interface IBuildStage
{
    string Id { get; }                           // "P1.Preflight"
    string DisplayName { get; }                   // "Environment Preflight"
    int Order { get; }                            // 升序执行
    BuildStagePolicy Policy { get; }              // Required/Optional/AlwaysRun/NoSkip/Transactional/…
    IReadOnlyList<string> DependsOn { get; }      // 依赖的 Stage ID

    BuildStageInputs GetInputs(BuildContext ctx);             // 输入声明
    BuildStageOutputs GetExpectedOutputs(BuildContext ctx);   // 预期输出
    BuildSkipDecision CanSkip(BuildContext ctx, BuildStageFingerprint prev); // 跳过判定
    void Execute(BuildContext ctx);                           // 执行
    void Verify(BuildContext ctx);                            // 验证
    IReadOnlyList<BuildIssue> AnalyzeFailure(BuildContext ctx, Exception ex); // 失败诊断
    void Rollback(BuildContext ctx);                          // 回滚（Transactional Stage）
}
```

## BuildConfigTransaction 事务系统

覆盖所有项目状态修改：AssetConfig YAML、PlayerSettings defines、Android 签名。

- `SnapshotFile(path)` / `SnapshotTextSetting()` / `SnapshotBoolSetting()` — 保存原始值
- `Commit()` — 成功后放弃回滚能力
- `Rollback()` — 失败/取消时恢复所有快照

红线：Stage 不允许绕过事务直接修改项目资产/设置。

## BuildIssue 结构化诊断

```json
{
  "Code": "KJ-BUILD-HYB-001",
  "Severity": "Error",
  "StageId": "P3.HybridCLR",
  "Message": "Hot-update assembly compilation failed",
  "Evidence": ["BuildOutput.txt line 42: error CS0001"],
  "LikelyCause": "Missing reference or syntax error in hot-update code",
  "SuggestedFix": "Check Unity Console for compilation errors",
  "RelatedFiles": ["Assets/Scripts/Core/MySystem.cs"],
  "IsBlocking": true
}
```

错误码前缀体系：`KJ-BUILD-PLAN-*` / `KJ-BUILD-PRE-*` / `KJ-BUILD-GEN-*` / `KJ-BUILD-HYB-*` / `KJ-BUILD-YOO-*` / `KJ-BUILD-CONFIG-*` / `KJ-BUILD-PLAYER-*` / `KJ-BUILD-VERIFY-*` / `KJ-BUILD-SMOKE-*` / `KJ-BUILD-FORMAL-*` / `KJ-BUILD-REPORT-*`。

## SmokeLogParser 多里程碑判定

启动链里程碑（必须全部命中）：
1. `[BootLoader] YooAsset` — AOT 壳完成 YooAsset 初始化
2. `[BootLoader] all DLLs loaded` — 热更 DLL 全部加载
3. `[AssetSystem] Ready` — Core 资源系统就绪
4. `[SystemManager]` — SystemManager 初始化完成

判定规则：
- `boot.log` 不得含 Error/Failed/Exception
- 以上 4 个里程碑必须全部出现在 `boot.log` 或 `latest.jsonl` 中

## FormalLeakageVerifier 泄露检查

Formal/Audit 环境强制执行：
- Development Build = false
- Script Debugging = false
- IL2CPP 后端
- 禁止 `KJ_GM_ENABLED` / `KJ_DEBUG_UI` / `KJ_DEV` define

## 报告体系

每次构建输出（路径：`BuildBackup/{Environment}/{Version}/{BuildNo}/reports/`）：
- `build_report.json` — 结构化 JSON（Stage 状态/产物/问题/环境快照）
- `build_report.md` — 人读 Markdown 摘要
- `ai_handoff.json` — AI 诊断交接（失败阶段/阻断问题/日志路径/建议操作）

AI 原则：AI 不读 Unity Console 截图，只读取固定路径结构化文件。

## 增量构建与续跑

### 标记文件
`Build/{Platform}/.markers/.{StageId}.done`

### 差量检测
`StageDependencyTracker` 对比标记文件 mtime 与输入文件 mtime，StageNames 使用 P0-P9 ID。

排除规则：`*.Editor/` 目录变更不触发热更相关 Stage 重跑。`.meta` 忽略。

## 已知坑点

1. **AssetConfig.Mode 序列化**：YAML 直写（Regex 替换），`ImportAsset(ForceSynchronousImport)`
2. **BootLoader packageName 误传**：`CreateDefaultBuiltinFileSystemParameters()` 必须无参重载
3. **MethodBridge 泛型迭代**：`maxMethodBridgeGenericIteration: 10`，已缓存 ~355MB 文件，不涉及新泛型时可跳过 P2
4. **Android Gradle 兼容**：Export Project 后需修复 Gradle/compileSdk
5. **P4 前置清理**：清空 `StreamingAssets/{PackageName}/` 旧产物

## 最佳实践

1. **日常开发用 KJ/HybridCLR 菜单**（秒级），出包验证用 **KJ/Build 菜单**（分钟到小时级）
2. **先 Standalone** 打通全链路，再上 Android
3. **增量优先**：日常用 `Incremental Build`，仅首次/清除标记后全量
4. **Smoke 失败读双日志**：`boot.log`（AOT 阶段）+ `latest.jsonl`（热更阶段）
5. **新增 Stage**：(1) 实现 `IBuildStage`；(2) 在 `BuildStageRegistry` 注册；(3) 更新 `StageDependencyTracker.StageNames`；(4) 更新本 skill
6. **Formal/Audit 出包前**先过 `BuildProfileValidator.Validate()`
