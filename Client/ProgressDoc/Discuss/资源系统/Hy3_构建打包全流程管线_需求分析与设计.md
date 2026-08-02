# KJ 构建打包全流程管线 1.1

> 当前状态：已实现。本文只描述现行架构和后续验证，不保留已删除实现的兼容说明。

## 1. 目标

构建管线需要同时满足以下要求：

1. `BuildProfile` 是唯一配置源。
2. 人工构建只有 `KJ/Build/Dashboard` 一个入口，CI 只有 `Boot.Editor.Build.BuildCommandLine.Run` 一个入口。
3. 默认按 SHA-256 内容指纹验证输入与输出，未变化的 Stage 和昂贵子步骤直接使用缓存。
4. 用户可以显式选择强制全量，忽略 Stage、HybridCLR 子步骤和 YooAsset 构建缓存。
5. 每个 Stage 具有输入、输出、依赖、验证、失败诊断和耗时数据。
6. 构建修改过的项目配置始终通过 `BuildTransaction` 回滚。

## 2. 代码边界

### 2.1 纯数据契约

`Assets/Framework/BuildPipeline/`：

- `BuildPlan`、`BuildStageInputs`、`BuildStageOutputs`
- `BuildStageFingerprint`、`BuildSkipDecision`
- `BuildIssue`、`BuildErrorCodes`
- `BuildArtifactManifest`、`AiBuildHandoff`
- `BuildExitCode`

该程序集不拥有 Editor 执行逻辑，也不引用 Scripts 层。

### 2.2 Editor 执行层

`Assets/Scripts/Boot.Editor/Build/`：

- `BuildProfile` / `BuildProfileSet` / `BuildProfileValidator`
- `BuildContext` / `BuildPaths` / `BuildTransaction`
- `IBuildStage` / `BuildStageRegistry` / `BuildPipelineRunner`
- P0-P9 Stage
- `BuildDashboardWindow`
- `BuildCommandLine`
- 报告、诊断和 `BuildTelemetry`

## 3. 唯一入口

### 3.1 Editor

打开 `KJ/Build/Dashboard`：

- 默认不勾选“强制全量重建”，Runner 自动验证缓存。
- 勾选后，本轮所有 Stage 都执行；P2 昂贵子步骤与 P4 YooAsset 缓存也被强制失效。

`KJ/HybridCLR/Prepare Runtime Assets And Boot` 只服务 Editor Play 准备，不是 Player 构建入口，也不提供无条件完整代码生成。

### 3.2 CI

```bash
Unity -batchmode -quit \
  -executeMethod Boot.Editor.Build.BuildCommandLine.Run \
  -profile Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.asset \
  -outputRoot BuildBackup
```

强制全量增加 `-full`。可选参数包括 `-platform`、`-version` 和 `-outputRoot`。

## 4. P0-P9

| Stage | 职责 | 核心输出 |
|---|---|---|
| P0 Plan | 验证 Profile，初始化路径与计划 | `build_plan.json` |
| P1 Preflight | 检查平台、IL2CPP、HybridCLR、BootScene、YooAsset、Android/Formal 约束 | 结构化问题 |
| P2 Generate | 执行 HybridCLR 六子步骤并验证产物 | DLL、link.xml、AOT DLL、生成 C++ |
| P3 HybridCLR | 同步 P2 已验证的 DLL/AOT metadata | `Assets/GameRes/HotUpdate/**/*.dll.bytes` |
| P4 Assets | YooAsset RawFile 生产构建 | `StreamingAssets` package |
| P5 ApplyConfig | 事务化写入运行配置和 Defines | 构建期配置 |
| P6 Player | IL2CPP Player/Android Gradle 构建 | APK/Player |
| P7 Verify | 校验 Player、包、DLL、AOT metadata 与 Formal 泄露项 | artifact manifest/issues |
| P8 Smoke | Standalone/ADB 启动并验证关键日志里程碑 | smoke logs/issues |
| P9 Report | 汇总报告与日志归档 | JSON/Markdown/AI handoff |

依赖硬约束：`P2 -> P3 -> P4 -> P6`，配置必须在 P6 前由 P5 应用。

## 5. 缓存模型

缓存目录：

```text
Library/KJBuild/{ProfileName}/cache/{Platform}/
├── P0.Plan.fingerprint.json
├── ...
├── P9.Report.fingerprint.json
└── hybridclr_generation_cache.json
```

规则：

- 缓存跨 `VersionName` / `VersionCode` 复用，避免只升版本号导致 HybridCLR 全量生成。
- 不同 BuildTarget 物理隔离。
- 指纹包含 pipeline/stage 版本、Profile、输入、工具和输出内容。
- 文件内容使用 SHA-256；`.meta` 不参与内容判断。
- 输入不变但输出缺失或内容被改动时必须重跑。
- 本轮会产出新内容或修改配置的依赖 Stage 执行时，下游不能使用旧缓存。
- P0/P1/P9 等策略要求执行的 Stage 仍按其自身 Policy 运行。

## 6. P2 HybridCLR 六子步骤

P2 不调用聚合式完整生成命令，显式执行：

1. `CompileDllCommand.CompileDll`
2. `Il2CppDefGeneratorCommand.GenerateIl2CppDef`
3. `LinkGeneratorCommand.GenerateLinkXml`
4. `StripAOTDllCommand.GenerateStripedAOTDlls`
5. `MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper`
6. `AOTReferenceGeneratorCommand.GenerateAOTGenericReference`

### 6.1 Stage 级判断

热更运行时代码、AOT 壳、包依赖、Player/HybridCLR 设置、Profile 或输出内容均未变化时，P2 整体跳过。

P2 需要执行时，DLL、Il2CppDef 和 link.xml 先更新；最昂贵的后三类工作继续做子步骤级判断。

### 6.2 AOT Strip

以下任一条件成立时执行：

- 强制全量。
- link.xml 内容变化。
- AOT 壳、AssetShared、第三方包或包锁文件变化。
- EditorBuildSettings、HybridCLRSettings 或 ProjectSettings 变化。
- 所需裁剪 AOT DLL 缺失。

否则复用 `AssembliesPostIl2CppStrip/{Platform}` 的已验证内容。

### 6.3 MethodBridge

以下任一条件成立时执行：

- AOT Strip 本轮执行。
- AOT DLL 内容变化。
- Development/defines 等 HybridCLR Profile 输入变化。
- 含 `MonoPInvokeCallback`、`DllImport`、`UnmanagedCallersOnly`、`delegate*` 或 `calli` 的源码变化。
- `MethodBridge.cpp` 缺失。

普通业务代码变化且不影响上述桥接输入时复用缓存，避免重复承受最大泛型迭代成本。

### 6.4 AOT 泛型引用

热更 DLL 或 AOT DLL 内容变化、生成文件缺失、强制全量时重新生成；其他情况使用已验证缓存。

每个子步骤都写独立性能 Span，报告可以直接显示真实耗时。

## 7. P3 与 P4

P3 只做同步、清理过期文件和一致性验证，不再编译 DLL 或生成 AOT metadata。

P4 输入覆盖完整 `Assets/GameRes/`、`Assets/BundleCollectorSetting.asset`、Profile 资源构建字段和 P3 输出。默认 `ClearBuildCacheFiles=false`，YooAsset 只处理变化内容；强制全量时设为 `true`。写入 StreamingAssets 前仍清理目标 package，防止旧发布文件残留。

## 8. 配置与报告

`BuildProfile` 覆盖环境、平台、版本、渠道、签名、开发标志、YooAsset、日志、功能开关、Smoke 和输出目录。Formal/Audit 的禁止项由 `BuildProfileValidator` 强校验。

每轮构建输出：

- `build_report.json`
- `build_report.md`
- `ai_handoff.json`
- `build_plan.json`
- Editor/Runtime/Smoke 日志

报告必须包含 Stage 状态、跳过原因、输入输出指纹、产物清单、结构化问题和性能 Span。

## 9. 验收

1. 强制全量基线：P0-P9 执行，P2 六个子步骤与 P4 cache 清理均有 Span。
2. 无变更默认构建：内容指纹命中，昂贵 Stage/子步骤使用缓存，APK 与启动链仍有效。
3. 普通热更代码变化：P2 更新 DLL/link；不涉及桥接/AOT 输入时跳过 AOT Strip 和 MethodBridge；P3/P4/P6 正确级联。
4. 桥接敏感代码变化：MethodBridge 自动重建。
5. AOT 壳或包依赖变化：AOT Strip、MethodBridge、AOT 泛型引用及下游自动重建。
6. 缓存输出被删除或篡改：验证机制自动修复，不能错误跳过。
7. Android/Standalone 缓存互不复用。
