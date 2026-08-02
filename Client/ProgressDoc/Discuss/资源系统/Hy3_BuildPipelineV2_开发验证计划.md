# Build Pipeline 1.1 开发验证计划

> 当前实现已经收敛为 `BuildProfile + BuildPipelineRunner + P0-P9`。本文只记录现行验证 Gate。

## 1. 验证原则

1. 默认验证内容指纹和缓存命中，不手工选择局部 Stage。
2. 强制全量只用于建立基线、工具链升级或排查缓存问题。
3. 以 `build_plan.json`、`build_report.json` 和性能 Span 为判定依据。
4. 输入变化必须使正确的最小子步骤与依赖下游重建。
5. 输出缺失或被篡改必须自动失效。
6. Formal/Audit 不允许跳过必需 Smoke，也不允许 Debug/GM/Development 配置泄露。
7. Unity Editor 编译和 TestRunner 由项目负责人执行；文档与代码静态检查由代理完成。

## 2. 已完成实现

| 项目 | 状态 | 验证点 |
|---|---|---|
| BuildProfile-only | Done | Dashboard/CI/代码入口都接收 BuildProfile |
| P0-P9 Runner | Done | 依赖排序、验证、失败早退、报告、回滚 |
| SHA-256 Stage 指纹 | Done | 输入、输出、Profile、工具、Stage/Pipeline 版本 |
| 跨版本平台隔离缓存 | Done | `Library/KJBuild/{Profile}/cache/{Platform}` |
| P2 六子步骤 | Done | DLL、Il2CppDef、link、AOT Strip、MethodBridge、AOT 泛型引用 |
| P3 单一同步职责 | Done | 不重复编译和生成 |
| P4 YooAsset 增量 | Done | 完整 GameRes/collector 输入，默认保留构建缓存 |
| 强制全量 | Done | Dashboard 选项与 CI `-full` |
| BuildTransaction | Done | 项目配置和 PlayerSettings 最终回滚 |
| 结构化报告与性能 Span | Done | JSON/Markdown/AI handoff |

用户已确认 Editor 编译与 TestRunner 无错误。

## 3. 当前 E2E 矩阵

### 3.1 强制全量基线

操作：Dashboard 勾选“强制全量重建”，执行 Android P0-P9。

期望：

- 所有必需 Stage 执行。
- P2 六个子步骤都有性能 Span。
- AOT Strip 与 MethodBridge 实际执行。
- P4 清理 YooAsset build cache 后构建。
- P7、P8 通过，报告和 APK 可用。

记录：RunId、总耗时、P2 各 Span、P4、P6、APK SHA-256/大小。

### 3.2 无变更默认构建

操作：不修改工程，不勾选强制全量，再次执行同 Profile/Platform。

期望：

- `build_plan.json` 清楚记录缓存命中原因。
- P2/P3/P4/P6 在输入输出完全一致时跳过。
- 被 Policy 要求执行的校验/报告 Stage正常运行。
- 上轮产物仍通过完整性校验。

### 3.3 普通热更代码变化

操作：修改不含桥接敏感 API 的 Project 或 General 普通业务代码。

期望：

- P2 编译 DLL、生成 Il2CppDef/link.xml。
- AOT 输入未变化时 AOT Strip 使用缓存。
- 不含桥接敏感变化且 AOT DLL 未变化时 MethodBridge 使用缓存。
- 热更 DLL 内容变化使 AOT 泛型引用按规则更新。
- P3、P4、P6 正确级联，包内 DLL hash 更新。

### 3.4 桥接敏感变化

操作：修改包含 P/Invoke、反向 P/Invoke 或 `calli` 的代码。

期望：MethodBridge 自动重建，并使所需下游执行。

### 3.5 AOT 或依赖变化

操作：修改 Launcher/AssetShared，或调整 package lock/HybridCLR settings。

期望：AOT Strip、MethodBridge、AOT 泛型引用与依赖下游全部重建。

### 3.6 输出完整性自愈

操作：分别备份后删除一个缓存输出，或修改其内容，再运行默认构建。

期望：对应 Stage/子步骤检测到输出不一致并重建；构建结束后输出 hash 恢复有效。

### 3.7 平台隔离

操作：分别运行 Android 与 Standalone Profile。

期望：两者读取不同缓存目录，不跨平台命中 HybridCLR 产物。

## 4. 报告检查

每个 E2E Run 至少检查：

- Stage `Passed/Skipped/Failed` 是否符合计划。
- `SkipReason` 是否能解释缓存命中。
- P2 子步骤 Span 是否与实际执行一致。
- `PerformanceDroppedCount` 与 collector failure 是否为 0。
- P3 同步清单、P4 package、P7 artifact manifest 是否一致。
- `boot.log` 与 `latest.jsonl` 是否包含 Launcher/YooAsset/HybridCLR/Boot/Core 里程碑。
- Formal/Audit 是否无禁止 define、调试构建和日志泄露。

## 5. 完成标准

以下全部满足后关闭 Build Pipeline 1.1 E2E Gate：

1. Android 完成“强制全量 -> 无变更 -> 普通热更变化”三轮并保存耗时对比。
2. 至少一次桥接敏感变化和一次 AOT/依赖变化按预期失效。
3. 至少一次输出删除/篡改验证自动修复。
4. Standalone 完成一轮 P0-P9 与 smoke。
5. Android 和 Standalone 缓存目录及产物隔离。
6. APK/Player 可启动并到达完整启动链，无构建期或运行期异常。
