# Build Pipeline v2 开发验证计划

> 创建日期：2026-07-09  
> 关联设计：`ProgressDoc/Discuss/资源系统/Hy3_构建打包全流程管线_需求分析与设计.md` 附录 E/F  
> 目标：把 Build Pipeline v2 按可验证的小步落地，避免一次性重写导致不可控风险。

---

## 1. 总目标

Build Pipeline v2 的开发目标不是替换一个菜单，而是建立一套可商业化维护的打包系统：

1. 配置驱动：Dev / QA / Pre / Profiling / Audit / Formal 通过 `BuildProfile` 管理。
2. 阶段插件化：每个 Stage 都有输入、输出、跳过原因、验证、失败诊断。
3. 可恢复：通过 `BuildPlan`、fingerprint、marker、事务回滚支持增量与失败续跑。
4. 可观察：每次构建输出 `build_report.json`、`issues.json`、`ai_handoff.json`、日志归档。
5. 可接管：AI 可以基于结构化报告定位问题，而不是依赖人工截图。
6. 可运营：Odin Dashboard 能清楚展示 Profile、Stage、产物、日志、问题和报告。

---

## 2. 开发原则

1. **v1 不删除、不破坏**：`KJBuildPipeline` 和现有菜单保留，直到 v2 在 Standalone + Android Dev + Formal dry-run 全部通过。
2. **先骨架，后增强**：先跑通 `BuildPlan + Runner + 空报告`，再接 Stage，最后做 Odin UI 和 AI 诊断。
3. **每阶段可单独验收**：每个里程碑都有明确产物、测试、回退策略。
4. **所有配置变更必须事务化**：任何会改 ProjectSettings、AssetConfig、PlayerSettings defines 的操作必须可 rollback。
5. **报告优先 JSON**：Markdown/HTML 是派生产物，JSON schema 是唯一机器事实源。
6. **Formal/Audit 强约束**：不允许 Smoke skipped passed，不允许 Debug/Gm/ScriptDebugging 泄露。
7. **目录双层边界**：纯数据契约放 `Assets/Framework/BuildPipeline/`；UnityEditor 执行、Odin、CI 入口放 `Assets/Scripts/Boot.Editor/Build/`；不要放到 `Core.Editor`。

---

## 3. 里程碑总览

| 里程碑 | 名称 | 目标 | 风险 |
|--------|------|------|------|
| M0 | 基线整理 | 修正当前 v1 编译风险，冻结 v1 行为 | 低 |
| M1 | v2 数据模型 | `BuildProfile`、`BuildContext`、`BuildIssue`、`BuildReportV2` | 低 |
| M2 | Runner 与 BuildPlan | `IBuildStage`、`BuildPipelineRunner`、fingerprint、marker | 中 |
| M3 | Stage 包装 | 用 v2 包装现有 S0-S9，先行为等价 | 中 |
| M4 | 事务与报告 | `BuildConfigTransaction`、ReportWriter、AI handoff | 中 |
| M5 | 验证器与诊断 | Static Verify、Smoke parser、BuildAnalyzer | 中 |
| M6 | Odin Dashboard | Profile / Stage / Report / Diagnostics 面板 | 中 |
| M7 | CI 与平台强化 | 命令行、退出码、Android smoke、Formal dry-run | 高 |
| M8 | 工业级验收 | Dev Standalone + Android Dev + Formal dry-run 全部通过 | 高 |

---

## 4. M0 — 基线整理

### 目标

在进入 v2 前，确保当前 v1 不处于明显编译风险状态，并把 v1 作为兼容基线保住。

### 工作项

- 修复 `StageSmokeRun.cs` 中 `System.Diagnostics.Debug` 与 `UnityEngine.Debug` 的潜在冲突。
- 替换 `new BuildConfig()` 为 `ScriptableObject.CreateInstance<BuildConfig>()`。
- 明确 `AndroidToolResolver` 要么接入 S0/S6，要么移除。
- 跑 `python asmdef_dependency_validator.py .`。
- 保留当前 v1 菜单和测试。

### 验收

- Unity Editor 编译无错误。
- `asmdef_dependency_validator.py .` 通过。
- v1 的 `Boot.Build.Editor.Tests` 仍通过。

### 回退

只改 v1 小问题，不引入 v2 文件；失败时可单独 revert 这些小修。

---

## 5. M1 — v2 数据模型

### 目标

新增 v2 基础数据结构，不执行真实构建。

### 新增文件

```text
Assets/Framework/BuildPipeline/
├── BuildPipeline.asmdef
├── Environment/
│   ├── BuildEnvironment.cs
│   ├── BuildChannel.cs
│   └── BuildFeatureFlags.cs
├── Reports/
│   ├── BuildReportV2.cs
│   ├── BuildArtifactManifest.cs
│   ├── BuildTimeline.cs
│   └── AiBuildHandoff.cs
├── Diagnostics/
│   ├── BuildIssue.cs
│   ├── BuildIssueSeverity.cs
│   └── BuildErrorCodes.cs
└── CI/
    └── BuildExitCode.cs

Assets/Scripts/Boot.Editor/Build/Config/
├── BuildProfile.cs
├── BuildProfileSet.cs
└── BuildProfileValidator.cs

Assets/Scripts/Boot.Editor/Build/Pipeline/
├── BuildContext.cs
├── BuildPaths.cs
├── BuildEnvironmentSnapshot.cs
└── BuildCancellationToken.cs
```

### 测试

- `BuildProfileValidationTests`
- `BuildPathsTests`
- `BuildIssueTests`

### 验收

- 能创建 Dev / Formal Android 两个 Profile asset。
- Formal Profile 校验能发现 Debug/Gm/ScriptDebugging/SmokeRequired 错误。
- `BuildPaths` 能生成稳定归档路径。
- `BuildPipeline.asmdef` 不引用 UnityEditor / Boot / Core / General / Project。
- 不影响 v1 菜单。

---

## 6. M2 — Runner 与 BuildPlan

### 目标

实现 v2 的执行骨架，支持空 Stage 的计划、执行、跳过、报告。

### 新增文件

```text
Assets/Framework/BuildPipeline/Plan/
├── BuildPlan.cs
├── BuildStageFingerprint.cs
├── BuildStageInputs.cs
├── BuildStageOutputs.cs
└── BuildSkipDecision.cs

Assets/Scripts/Boot.Editor/Build/Pipeline/
├── IBuildStage.cs
├── BuildStageBase.cs
├── BuildStageCategory.cs
├── BuildStagePolicy.cs
├── BuildStageRegistry.cs
├── BuildPipelineRunner.cs
└── BuildResumeState.cs
```

### 核心要求

- Stage 不直接写 marker。
- Runner 统一写 `stage_markers.json`。
- `BuildPlan` 必须包含跳过原因。
- Profile hash、输入 hash、工具 hash、输出 hash 参与 fingerprint。
- 上游 Stage 变化必须级联下游。

### 测试

- `BuildPlanTests`
- `BuildStageFingerprintTests`
- `BuildPipelineRunnerTests`

### 验收

- 使用 3 个 fake Stage 可以生成 `build_plan.json`。
- 输入 hash 不变时 Stage 可跳过。
- 上游变化时下游被触发。
- 输出缺失时强制重跑。

---

## 7. M3 — 包装现有 S0-S9

### 目标

不重写业务逻辑，先把现有 v1 S0-S9 包装为 v2 Stage，验证 Runner 可驱动真实阶段。

### 新增文件

```text
Assets/Scripts/Boot.Editor/Build/StagesV2/
├── P1_PreflightStage.cs
├── P2_GenerateAllStage.cs
├── P3_CompileHybridClrStage.cs
├── P3_SyncHotUpdateStage.cs
├── P4_BuildYooAssetStage.cs
├── P5_ApplyConfigStage.cs
├── P6_BuildPlayerStage.cs
├── P7_StaticVerifyStage.cs
├── P8_RuntimeSmokeStage.cs
└── P9_ReportStage.cs
```

### 核心要求

- 行为先与 v1 等价，避免同时重构逻辑和框架。
- 每个 Stage 填写 `GetInputs` / `GetExpectedOutputs`。
- `Verify` 至少覆盖 v1 已有不变量。
- `AnalyzeFailure` 先返回基础 `BuildIssue`。

### 测试

- `StageInputOutputTests`
- `StageWrapperSmokeTests`

### 验收

- Dev Standalone 可生成完整 BuildPlan。
- 可手动只跑 Preflight + Report。
- v1 仍可单独执行。

---

## 8. M4 — 事务与报告

### 目标

让构建具备可 rollback 和可 AI 读取的报告能力。

### 新增文件

```text
Assets/Scripts/Boot.Editor/Build/Pipeline/
└── BuildConfigTransaction.cs

Assets/Scripts/Boot.Editor/Build/Reports/
├── BuildReportWriter.cs
├── BuildJsonWriter.cs
├── BuildMarkdownWriter.cs
└── AiBuildHandoffWriter.cs
```

### 核心要求

- `AssetConfig.asset` 修改前必须 snapshot。
- PlayerSettings defines 修改前必须 snapshot。
- 失败、取消、报告异常都 rollback。
- 每次构建至少输出：
  - `build_plan.json`
  - `build_report.json`
  - `issues.json`
  - `ai_handoff.json`

### 测试

- `BuildTransactionTests`
- `BuildReportSchemaTests`
- `AiHandoffTests`

### 验收

- 人为制造 Stage 失败后，AssetConfig 和 defines 恢复。
- `ai_handoff.json` 包含 failed stage、blocking issue、日志路径、相关文件。

---

## 9. M5 — 验证器与诊断

### 目标

把失败从“异常字符串”升级为结构化问题。

### 新增文件

```text
Assets/Scripts/Boot.Editor/Build/Diagnostics/
├── BuildAnalyzer.cs
├── BuildKnowledgeBase.cs
├── BuildLogCollector.cs
├── StaticArtifactVerifier.cs
├── FormalLeakageVerifier.cs
├── SmokeLogParser.cs
└── AndroidAdbRunner.cs
```

### 核心规则

- DLL/AOT metadata 数量不足：Error。
- Formal 含 Debug/Gm/ScriptDebugging：Error。
- SmokeRequired 缺设备/缺 adb：Error。
- boot.log 含 Error/Failed：Error。
- latest.jsonl 缺关键里程碑：Error。

### 测试

- `StaticVerifyTests`
- `FormalLeakageVerifierTests`
- `SmokeLogParserTests`
- `BuildAnalyzerGoldenTests`

### 验收

- 每类失败至少有一个稳定错误码。
- `issues.json` 中 blocking issue 可直接指导修复。

---

## 10. M6 — Odin Dashboard

### 目标

提供可日常使用的工业化操作面板。

### 新增文件

```text
Assets/Scripts/Boot.Editor/Build/UI/
├── BuildDashboardWindow.cs
├── BuildProfileEditor.cs
├── BuildPlanView.cs
├── BuildStageMonitorView.cs
├── BuildReportViewer.cs
├── BuildArtifactView.cs
└── BuildDiagnosticsView.cs
```

### 交互验收

- 能选择 Profile。
- 能运行 Preflight / Incremental / Full / Smoke Only。
- 构建中禁用危险按钮。
- 能取消构建并 rollback。
- 能打开最近报告、日志、产物目录。
- 能复制 `ai_handoff.json` 路径。

### 测试

UI 主要靠手工验证；核心逻辑必须下沉到非 UI 类并由 EditMode 测试覆盖。

---

## 11. M7 — CI 与平台强化

### 目标

让 v2 可被 CI 和 AI 无头调用。

### 新增文件

```text
Assets/Scripts/Boot.Editor/Build/CI/
├── BuildCommandLine.cs
├── BuildArgs.cs
└── BuildExitCode.cs

ci/
└── build.ps1
```

### 验收

- batchmode 可指定 Profile、Mode、BuildNumber、OutputRoot。
- CI 失败时退出码符合附录 F。
- 即使构建失败，也尽力输出最小 `ai_handoff.json`。
- `ci/build.ps1` 能定位 Unity 路径或通过参数传入。

---

## 12. M8 — 工业级验收矩阵

### 必过矩阵

| 场景 | 期望 |
|------|------|
| Dev Standalone Full | 产出 Player、报告、日志；Smoke 通过。 |
| Dev Standalone Incremental | 无变更时跳过可跳过 Stage；报告说明 skip 原因。 |
| Android Dev Build | 产出 APK；可 install/start/pull 日志。 |
| Android Dev Smoke 缺设备 | 如果 SmokeRequired=false 可 skip，但报告必须标明 not verified。 |
| Formal Android Dry Run 缺签名 | Preflight 失败，退出码 20。 |
| Formal Android 缺设备 | SmokeRequired=true，失败，退出码 80。 |
| Formal Debug 泄露 | Static Verify 失败，退出码 70。 |
| 人为破坏 DLL | HybridCLR/Static Verify 失败，错误码稳定。 |
| 人为破坏 AssetConfig | Preflight 或 ApplyConfig 失败，并 rollback。 |

### 完成定义

v2 达到“可替代 v1”的条件：

1. Dev Standalone Full 通过。
2. Android Dev Build + Smoke 通过。
3. Formal Android Dry Run 能正确失败。
4. 所有失败有 `BuildIssue`。
5. 所有构建有 `build_report.json` 和 `ai_handoff.json`。
6. Odin Dashboard 能打开最近报告和问题。
7. v1 菜单保留但标注 Legacy。

---

## 13. 开发顺序建议

严格按以下顺序推进：

1. M0：修 v1 编译风险。
2. M1：`Framework.BuildPipeline` 纯契约 + `Boot.Editor` Profile 包装。
3. M2：Runner + BuildPlan。
4. M3：包装 S0-S9。
5. M4：事务 + 报告。
6. M5：诊断规则。
7. M6：Odin Dashboard。
8. M7：CI。
9. M8：验收矩阵。

不要提前做复杂 UI；不要提前重写 Stage 业务逻辑。先让 v2 骨架能稳定执行和报告，再逐步增强。

---

## 14. 风险与控制

| 风险 | 控制方式 |
|------|----------|
| v2 一次性改动过大 | 每个 M 独立提交，v1 不删除。 |
| 事务回滚遗漏 | 所有 ProjectSettings/AssetConfig 写入必须经 Transaction。 |
| Odin UI 绑死逻辑 | UI 只调用服务类，逻辑可单测。 |
| Smoke 不稳定 | Dev 可 skip 但记录 not verified；Formal/Audit 必须 fail。 |
| AI 诊断不准 | 先做规则库，不让 AI 自由猜；AI 读取 evidence。 |
| 增量误跳过 | 输出缺失强制重跑；Profile/tools/stageVersion 参与 hash。 |
| 文档漂移 | 改 v2 代码同时更新本计划和附录 F。 |
