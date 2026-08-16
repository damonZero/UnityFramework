---
name: kj-build-pipeline
description: >
  KJ 构建打包全流程管线（Boot.Build.Editor + Framework.BuildPipeline）。覆盖 P0-P9 IBuildStage 插件化管线（Plan→Preflight→Generate→HybridCLR→BuildAsset→ApplyConfig→BuildPlayer→Verify→Smoke→Report）、BuildProfile 多环境配置（Dev/QA/Profiling/Audit/Formal）、BuildPipelineRunner Plan 驱动编排器 + 事务系统、BuildIssue 结构化诊断 + BuildErrorCodes 稳定错误码、SmokeLogParser 多里程碑冒烟判定、FormalLeakageVerifier 发布包泄露检查、BuildDashboardWindow Odin 六视图面板、BuildCommandLine CI 无头入口。触发场景：Player 打包、CI 构建、增量构建排查、Profile 配置、冒烟调试、产物校验、AI 诊断。
metadata:
  doc: ProgressDoc/Discuss/资源系统/Hy3_构建打包全流程管线_需求分析与设计.md
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
│   ├── BuildProfile.cs                        — ScriptableObject 多环境唯一配置源
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
│   └── BuildTransaction.cs                    — 事务系统（文件/PlayerSettings snapshot + rollback）
├── Stages/
│   ├── P0_PlanStage.cs                        — P0 计划生成
│   ├── P1_PreflightStage.cs                   — P1 环境预检
│   ├── P2_GenerateStage.cs                    — P2 HybridCLR 六子步骤内容感知增量生成
│   ├── P3_HybridCLRStage.cs                   — P3 同步已验证 DLL + AOT metadata
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
└── KJBuildPipeline.cs                         — 编排器入口（委托 BuildPipelineRunner）
```

## 10-Stage 管线（P0-P9）

```
P0  Plan              — 验证 Profile、生成 BuildPlan、初始化输出目录
P1  Preflight         — 全维度预检（HC 运行时/平台/BootScene/AssetConfig/IL2CPP/Android/Formal）
P2  Generate          — CompileDll/Il2CppDef/link/AOT Strip/MethodBridge/AOTGenericReference 增量生成
P3  HybridCLR         — 同步已验证热更 DLL + AOT metadata 为 .dll.bytes
P4  BuildAsset        — YooAsset RawFileBuildPipeline 增量构建 → StreamingAssets
P5  ApplyConfig       — 事务化 AssetConfig YAML 写入 + ScriptingDefines（按环境）
P6  BuildPlayer       — BuildPipeline.BuildPlayer(IL2CPP) + Android 工具链
P7  Verify            — Player/StreamingAssets/DLL 数量校验 + Formal 泄露检查
P8  Smoke             — 多里程碑冒烟（Launcher→YooAsset→HybridCLR→Boot→Core，含 Android ADB）
P9  Report            — 复制 Editor.log + Runtime 日志到归档目录
```

**排序硬约束**：P2→P3→P4→P6（DLL 先编译生成 → 同步 → YooAsset 打包 → BuildPlayer 嵌入）。P5→P6（配置落盘后才构建）。

## 配置模型

### BuildProfile（唯一配置源）
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

## 入口与调用方式

### Editor 菜单

```
KJ/
├── Build/
│   └── Dashboard                                — 默认自动增量；可勾选“强制全量重建”
├── HybridCLR/                                   — Editor Play 准备与维护菜单；不提供无条件聚合全量生成入口
```

### CI 无头入口

```bash
# 默认自动增量
Unity -batchmode -quit -executeMethod Boot.Editor.Build.BuildCommandLine.Run \
  -profile Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset \
  -outputRoot BuildBackup

# 显式忽略所有 Stage/HybridCLR/YooAsset 缓存
Unity -batchmode -quit -executeMethod Boot.Editor.Build.BuildCommandLine.Run \
  -profile Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset -full
```

### 代码调用

```csharp
var report = KJBuildPipeline.Build(profile);                    // 默认自动增量
var fullReport = KJBuildPipeline.Build(profile, true);          // 强制全量
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

## BuildTransaction 事务系统

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

管线版本 1.1.0 使用内容指纹，不使用仅记录完成状态或依赖 mtime 的缓存：

- Stage 指纹：`Library/KJBuild/{ProfileName}/cache/{Platform}/{StageId}.fingerprint.json`
- P2 子步骤缓存：同目录 `hybridclr_generation_cache.json`
- 输入/输出均按 SHA-256 内容校验；`.meta` 忽略，文件只改时间不会失效，输出被篡改会自动重建。
- 缓存与版本化归档分离，因此只升 `VersionName/VersionCode` 不会丢 HybridCLR 缓存；Android/Standalone 缓存物理隔离。
- 本轮将执行的 `ProducesArtifacts` / `Transactional` Stage 会级联使下游执行。

P2 子步骤策略：

| 子步骤 | 默认执行条件 |
|---|---|
| CompileDll + link.xml | P2 热更源码/Profile/工具输入变化 |
| AOT Strip | link.xml、AOT 壳、包依赖、Player/HybridCLR 设置变化，或 AOT metadata 缺失 |
| MethodBridge | AOT DLL、Development/defines、P/Invoke/反向 P/Invoke/calli 敏感源码变化 |
| AOTGenericReference | 热更 DLL 或 AOT DLL 内容变化 |
| P3 同步 | P2 执行或同步产物缺失/变化 |
| P4 YooAsset | `Assets/GameRes`、collector、包版本或 P3 产物变化；默认保留 YooAsset build cache |

第一次运行 1.1.0 会建立新缓存并执行完整生成。Dashboard 勾选“强制全量重建”或 CI 使用 `-full` 会忽略缓存。

## 已知坑点

1. **AssetConfig.Mode 序列化**：YAML 直写（Regex 替换），`ImportAsset(ForceSynchronousImport)`
2. **BootLoader packageName 误传**：`CreateDefaultBuiltinFileSystemParameters()` 必须无参重载
3. **MethodBridge 泛型迭代**：`maxMethodBridgeGenericIteration: 10`，不得直接降低；1.1.0 通过 AOT/桥接敏感输入缓存跳过无关重算
4. **Android Gradle 兼容**：Export Project 后需修复 Gradle/compileSdk
5. **P4 前置清理**：清空 `StreamingAssets/{PackageName}/` 旧产物

## 最佳实践

1. **日常开发用 KJ/HybridCLR 菜单**（秒级），出包验证用 **KJ/Build 菜单**（分钟到小时级）
2. **先 Standalone** 打通全链路，再上 Android
3. **增量优先**：Dashboard 默认自动增量；仅首次建立缓存、工具链升级、正式基线或缓存排障时勾选“强制全量重建”
4. **Smoke 失败读双日志**：`boot.log`（AOT 阶段）+ `latest.jsonl`（热更阶段）
5. **新增 Stage**：(1) 实现 `IBuildStage`；(2) 在 `BuildStageRegistry` 注册；(3)声明精确输入/输出与 Profile hash；(4) 更新本 skill
6. **Formal/Audit 出包前**先过 `BuildProfileValidator.Validate()`
