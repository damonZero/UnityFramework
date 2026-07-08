---
name: kj-build-pipeline
description: >
  KJ 构建打包全流程管线（Boot.Build.Editor）。覆盖 10-Stage 统一管线（预检 → HybridCLR 生成 → 编译 → 同步 → YooAsset 正式包 → 写配置 → BuildPlayer IL2CPP → 产物校验 → 无头冒烟 → 报告）、BuildConfig 配置模型、增量/全量构建、标记文件续跑、差量检测引擎与级联规则、BuildReport JSON/Markdown 双输出、StageManager 可视化面板、CI 无头入口、以及已知坑点与修复（YAML 直写、BootLoader 参数、Gradle 兼容等）。触发场景：Player 打包、CI 构建、增量构建排查、标记清理、新增/修改 Stage、冒烟调试、产物校验失败排障。
metadata:
  doc: ProgressDoc/Discuss/Hy3_构建打包全流程管线_需求分析与设计.md
  layer: Boot.Editor
  asmdef: Boot.Build.Editor
---

# KJ 构建打包全流程管线

源码在 `Assets/Scripts/Boot.Editor/Build/`，设计文档见 `ProgressDoc/Discuss/Hy3_构建打包全流程管线_需求分析与设计.md`。

## 架构速查

```
Assets/Scripts/Boot.Editor/Build/
├── Boot.Build.Editor.asmdef          — 引用 Boot/Boot.Editor/Asset/AssetShared/HybridCLR.Editor/YooAsset.Editor/Launcher/UniTask
├── KJBuildPipeline.cs                — 单一编排器（Build / BuildWithMask / IncrementalBuild / BuildFromCommandLine）+ 菜单入口
├── BuildConfig.cs                    — ScriptableObject 构建配置（平台/IL2CPP/dev/version/smoke）
├── BuildReport.cs                    — JSON + Markdown 双报告（StageResult / ArtifactEntry / SmokeConclusion / BuildSummary）
├── StageDependencyTracker.cs         — 文件时间戳差量检测 + 级联传播引擎
├── BuildStagePanel.cs                — EditorWindow 可视化管理面板（KJ → Build → Build Stage Manager...）
├── PlayerBuildPrivatePathValidator.cs — IPreprocessBuildWithReport，拦截 _ 前缀路径进入 Player build
├── Stages/
│   ├── StagePreFlightCheck.cs        — S0 预检
│   ├── StageGenerateAll.cs           — S1 HybridCLR 生成
│   ├── StageCompile.cs               — S2 编译热更 DLL + AOT metadata
│   ├── StageSync.cs                  — S3 同步 DLL 到 YooAsset 源目录
│   ├── StageBuildYooAsset.cs         — S4 YooAsset 正式包构建（ScriptableBuildPipeline）
│   ├── StageApplyConfig.cs           — S5 写 Entry 配置 + AssetConfig.Mode=Offline（YAML 直写+回滚+启动安全网）
│   ├── StageBuildPlayer.cs           — S6 Unity Player 构建（IL2CPP + BuildOptions）
│   ├── StageValidateArtifacts.cs     — S7 产物静态校验
│   ├── StageSmokeRun.cs              — S8 无头运行冒烟（Win Standalone Process.Start）
│   └── StageReport.cs                — S9 归档 + 报告生成
└── Tests/
    ├── Boot.Build.Editor.Tests.asmdef — autoReferenced=false, UNITY_INCLUDE_TESTS
    └── BuildPipelineTests.cs         — 29 个 EditMode 测试（BuildConfig / BuildReport / Markers / SmokeResult / ArtifactEntry）
```

## 10-Stage 管线

```
Stage 0  PreFlightCheck     — 前置校验（HC 运行时 / 平台切换 / Boot 场景 / AssetConfig / IL2CPP）
Stage 1  GenerateAll         — HybridCLR 生成（PrebuildCommand.GenerateAll → link.xml / AOTGenericReferences）
Stage 2  Compile             — 编译热更 DLL + AOT metadata DLL（先清空旧产物再编译）
Stage 3  Sync                — 拷贝 DLL → Assets/GameRes/HotUpdate/（复用 SyncExistingOutputs）
Stage 4  BuildYooAsset       — YooAsset ScriptableBuildPipeline 生产构建 → StreamingAssets
Stage 5  ApplyConfig         — YAML 直写 AssetConfig.Mode=Offline + ApplyToOpenEntry + PrepareBootScene
Stage 6  BuildPlayer         — BuildPipeline.BuildPlayer(IL2CPP) → Build/{Platform}/KJ.{ext}
Stage 7  ValidateArtifacts   — Player / YooAsset 包 / HybridCLR 物料清单静态校验
Stage 8  SmokeRun            — 无头运行冒烟（Win Standalone: -batchmode -nographics）
Stage 9  Report              — 输出 build_report.json + build_report.md
```

**排序硬约束**：S2（编译）→ S3（同步）→ S4（打正式包）→ S6（BuildPlayer）。错序必炸——DLL 必须先编译、先同步进源目录、先被打进 YooAsset 真包，BuildPlayer 才会把它们封进 StreamingAssets。

S4 落到 StreamingAssets → S6 嵌入 Player。S5 设 Mode=Offline 并保存 → S6 构建前配置落盘。

## 入口与调用方式

### Editor 菜单（`KJBuildPipelineMenu`）

```
KJ/
├── Build/
│   ├── Full Player Build & Validate       — 全量构建（清除标记后跑全部 10 个 Stage）
│   ├── Incremental Build (Auto-detect)    — 差量构建（自动检测变更）
│   ├── Build Stage Manager...             — 可视化管理面板
│   ├── Clear All Stage Markers            — 手动清除全部标记
│   └── Create BuildConfig                 — 创建 BuildConfig.asset 模板
├── HybridCLR/                             — 保留：14 个开发内循环菜单项（不变）
```

### CI 无头入口

```bash
Unity -batchmode -quit -executeMethod Boot.Editor.Build.KJBuildPipeline.BuildFromCommandLine \
  -platform:Android -development:true -version:1.0.0 -config:Assets/Scripts/Boot.Editor/Build/BuildConfig.asset
```

### 代码调用

```csharp
var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(configPath);
var report = KJBuildPipeline.Build(config);                    // 全量（使用标记续跑）
var report = KJBuildPipeline.BuildWithMask(config, boolMask);  // 掩码构建（true=强制重跑，false=跳过）
var report = KJBuildPipeline.IncrementalBuild(config);          // 差量检测自动构建
```

## BuildConfig 配置

ScriptableObject，路径 `Assets/Scripts/Boot.Editor/Build/BuildConfig.asset`。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Platform` | BuildTarget | StandaloneWindows64 | 目标平台 |
| `Development` | bool | true | Development 构建（冒烟=true） |
| `Version` | string | "1.0.0" | 产物版本号 |
| `PackageName` | string | "DefaultPackage" | YooAsset 包名 |
| `AssetDownloadTag` | string | "hotupdate" | 资源下载 Tag |
| `StartupTypeName` | string | "Project.Bootstrap.ProjectStartup, Project" | 启动类型全名 |
| `StartupMethodName` | string | "Start" | 启动方法名 |
| `OutputDir` | string | "" | 输出根目录（空则自动推导 `Build/{Platform}`） |
| `SmokeEnabled` | bool | true | 是否执行 S8 冒烟 |
| `SmokeTimeoutSec` | int | 120 | 冒烟超时（秒） |

v2 预留字段（已声明但不实现逻辑）：`BuildEnvironment`（Dev/Profiling/Pre/Release）、`CdnBaseUrl`。

## 增量构建与续跑机制

### 标记文件

每个 Stage 完成后写入 `Build/.markers/.{StageName}.done`（内容为 ISO 8601 时间戳）。下次构建时检查标记存在则跳过。

```csharp
KJBuildPipeline.MarkStageDone("S4_BuildYooAsset");    // 写标记
KJBuildPipeline.IsStageDone("S4_BuildYooAsset");      // 检查标记
KJBuildPipeline.ClearAllMarkers();                     // 清除全部标记
KJBuildPipeline.ClearStageMarker("S1_GenerateAll");    // 清除指定标记
```

### 差量检测引擎（StageDependencyTracker）

对比各 Stage 标记文件 mtime 与输入文件 mtime，自动判断需重跑的 Stage。

| Stage | 监控输入 | 级联 |
|-------|---------|------|
| S0 | — | 始终运行 |
| S1 | `Assets/Scripts/Boot/`, `Core/`, `General/`, `Project/`, `Assets/Framework/` | S1→S2→S3→S4→S6 |
| S2 | 同上 | S2→S3→S4→S6 |
| S3 | `HybridCLRData/HotUpdateDlls/` | S3→S4→S6 |
| S4 | `Assets/GameRes/HotUpdate/` | S4→S6 |
| S5 | `Assets/Resources/AssetConfig.asset` | S5→S6（独立，不触发 S2-S4） |
| S7-S9 | — | 始终运行 |

**排除规则**：`*.Editor/` 目录下文件变更不触发热更相关 Stage 重跑（避免改 Editor 工具触发 20 分钟 MethodBridge 重生成）。`.meta` 文件忽略。

`BuildStagePanel` 可视化面板自动调用 `DetectChanges()`，橙色高亮需重跑的 Stage，支持手动勾选/取消。

## BuildReport 报告

### 机器可读 JSON (`build_report.json`)

```json
{
  "pipelineVersion": "1.0.0",
  "platform": "Android",
  "stages": [
    { "name": "S0_PreFlightCheck", "passed": true, "durationSec": 2.3, "skipped": false }
  ],
  "artifacts": [
    { "path": "Build/Android/KJ.apk", "exists": true, "sizeBytes": 52428800, "sha256": "abc123..." }
  ],
  "smoke": {
    "enabled": true,
    "result": "Passed",
    "milestonesFound": ["[Boot]", "ProjectStartup"]
  },
  "summary": { "allPassed": true, "stagesPassed": 8, "stagesSkipped": 2 }
}
```

### 人读 Markdown (`build_report.md`)

Stage 结果表格、冒烟结论、产物清单、总体摘要。

## SmokeRun 冒烟测试（S8）

### 行为

- **Win Standalone**：`Process.Start(playerPath, "-batchmode -nographics")` → 等待退出/超时 → 读 `boot.log` + `latest.jsonl` → 判定里程碑
- **Android**：自动 Skip（需 adb），标记 `SmokeResult.Skipped`
- **Export Project**（目录输出）：自动 Skip，标记 `SmokeResult.Skipped`

### 成功判定

`boot.log` 无 AOT 阶段 Error/Failed + `latest.jsonl` 含成功里程碑（`[Boot] Starting game` / `ProjectStartup` / `BOOT_OK` / `PROJECTSTARTUP_OK`）。

### SmokeResult 枚举

```
NotScheduled — SmokeEnabled=false，S8 未被调度
Skipped      — 因平台/环境原因跳过（Android / Export Project）
Passed       — 冒烟执行且判定成功
Failed       — 冒烟执行但判定失败
```

## AssetConfig.Mode 管理（S5 关键修复）

### 问题

`Resources.Load<AssetConfig>` + `SetDirty` + `SaveAssets` 序列化时机不可靠——S6 的 `AssetDatabase.Refresh()` 可能在 `SaveAssets` 落盘前重新导入。

### 方案

S5 用 YAML 直写：

```csharp
string yaml = File.ReadAllText("Assets/Resources/AssetConfig.asset");
yaml = ModeFieldRegex.Replace(yaml, "$1" + offlineInt);
File.WriteAllText(fullPath, yaml);
AssetDatabase.ImportAsset(AssetConfigPath, ForceSynchronousImport);
```

### 回滚

`KJBuildPipeline.Build()` 构建结束后调用 `StageApplyConfig.RollbackAssetConfig()`，YAML 直写回原值。

### Editor 启动安全网

若 Unity 在构建中崩溃导致回滚未执行，`StageApplyConfig` 的 `static` 构造函数通过 `EditorApplication.delayCall` 在 Editor 启动后自动检测并修复——若发现 Mode 卡在 Offline，写入 warning 日志并自动重置为 EditorSimulate。

## 产物校验（S7）

1. Player 本体存在且非空（文件或 Export Project 目录）
2. YooAsset 包 `{PackageName}.version` 存在 + `.rawfile`/`.bundle` 数量正确
3. HybridCLR 源目录物料复核（热更 DLL + AOT metadata DLL 数量）
4. `HybridCLRSettings.hotUpdateAssemblies` 与同步落点一致
5. 同步目标 `dll.bytes` 数量 >= 配置的热更程序集数量

## 测试

`Boot.Build.Editor.Tests`（29 EditMode 测试），覆盖：

- BuildConfig 默认值/路径推导/版本/多平台
- BuildReport AddStage/AddArtifact/JSON/Markdown/SmokeConclusion 序列化
- SmokeResult 枚举值独立性 + 默认值为 NotScheduled
- 标记文件：IsStageDone/MarkStageDone/ClearAllMarkers
- BuildFailedException 携带 StageName + InnerException
- ArtifactEntry SHA256 同内容同哈希/异内容异哈希
- StageResult invariants/skipped/duration 语义

运行：Unity Test Runner → EditMode → `Boot.Build.Editor.Tests`

## 与现有工具的关系

- **保留**：`KJ/HybridCLR/*` 全部 14 个菜单（开发内循环：改代码 → Generate Runtime Assets And Sync → Prepare YooAsset Editor Simulate → Editor Play）
- **新增**：`KJ/Build/*` 5 个菜单（出包验证循环）
- **复用**：S1-S3+S5 内部调用现有 `PrebuildCommand` / `CompileDllCommand` / `StripAOTDllCommand` / `SyncExistingOutputs` / `ApplyToOpenEntry` / `PrepareBootScene`
- **新增**：S4（YooAsset 生产构建）、S6（BuildPlayer）、S7-S9（校验+冒烟+报告）

## 已知坑点

1. **AssetConfig.Mode 序列化**：YAML 直写而非 ScriptableObject API（见上方 S5 关键修复）
2. **BootLoader packageName 误传**：`CreateDefaultBuiltinFileSystemParameters()` 必须用无参重载，不能传包名（包名被当作路径导致 IL2CPP 下 Uri 解析失败）
3. **MethodBridge 泛型迭代**：`maxMethodBridgeGenericIteration: 10` 处理 3M-6M 泛型方法时 CPU 满载数十分钟；缓存的 `MethodBridge.cpp`（~355MB）复用时不重生成。只改代码不涉及新泛型时 S1 可跳过
4. **Android Gradle 兼容**：Unity Export Project 后需修复 Gradle 7.5.1→8.5、compileSdk 36→34、`gradle.properties` 加临时目录（每次 Export Project 覆盖）
5. **S4 前置清理**：`ClearBuildCacheFiles = true` + 先删 `StreamingAssets/{PackageName}/` 旧产物，否则 YooAsset 缓存命中导致用旧 bundle

## 最佳实践

1. **开发内循环 vs 出包验证循环分离**：日常开发用 `KJ/HybridCLR` 菜单（秒级），出包验证用 `KJ/Build` 菜单（分钟到小时级）。不要混用。
2. **先 Standalone 打通用全链路，再上 Android**：Standalone 不需要 Android SDK/NDK/JDK，能 `-batchmode` 直接跑冒烟，最快验证"编译→同步→打真包→BuildPlayer→冒烟"全链路。
3. **增量优先**：日常出包用 `Incremental Build` 或 `BuildStagePanel` 勾选模式。仅首次构建、清除了标记、或修改了启动配置时用全量。
4. **标记机制慎用**：S1 的 MethodBridge 耗时巨大，清除 S1 标记前确认确实需要——否则会浪费 ~20 分钟。
5. **Smoke 失败先读双日志**：不要只看 `latest.jsonl`——AOT 阶段 `boot.log` 包含 YooAsset 初始化、热更 DLL 加载等最可能失败的关键路径。
6. **AssetConfig.Mode 不应手动改**：管线 S5→回滚全自动管理，手动改 Mode 可能导致 Editor Play 或 Player 启动异常。若 Editor Play 初始化失败提示 `EditorFileSystem`，检查 AssetConfig.Mode 是否被错误地留在 Offline（Editor 启动安全网会自动修复此情况）。
7. **PlayerBuildPrivatePathValidator** 通过 `IPreprocessBuildWithReport` 在 callbackOrder=-1000 拦截——不要移除或降低其 callbackOrder。
8. **新增 Stage 的步骤**：(1) 在 `StageDependencyTracker.StageNames` 和 `Inputs` 中注册；(2) 在 `KJBuildPipeline.BuildWithMask` 中按序插入 `RunStage` 调用；(3) 在 `DetectChanges` 中添加级联规则；(4) 更新 `BuildStagePanel.StageLabels`；(5) 更新设计文档与本 skill。
