# 构建面板 Odin V2 工业化实施方案

> 状态：待审批  
> 版本：v2.0  
> 日期：2026-07-17  
> 适用版本：Unity 2022.3.62f2、当前 `packages-lock.json` 锁定的 Odin/YooAsset 版本

## 1. 背景与目标

在不改变 BuildProfile-only、P0-P9、fingerprint、BuildTransaction 和 CI 契约的前提下，新增一个中文 Odin 构建工作台，为开发者提供以下闭环：

1. 发现、创建、选择和编辑多个 `BuildProfile`。
2. 在正式构建前保存配置并执行结构化校验。
3. 打开 YooAsset 原生资源收集器。
4. 通过唯一正式入口 `KJBuildPipeline.Build(profile)` 执行完整构建。
5. 查看最近构建报告、失败阶段和结构化问题。
6. 构建期间防止重复执行，并能在窗口重开或 Domain Reload 后恢复可观察状态。

本次交付是 Editor 工具改造，不改变 Player 运行时行为，不新增手动 Stage mask，也不允许绕过 Runner 执行正式构建。

## 2. 成功标准

交付完成必须同时满足：

- 任意已登记 Profile 均可被选择、编辑、校验和完整构建。
- 点击完整构建前，Profile 已落盘，构建输入与磁盘资产一致。
- Profile 存在阻塞问题时，完整构建按钮不可直接启动 P0-P9。
- 同一时刻最多存在一个 Dashboard 发起的构建或预检操作。
- “运行预检”不会写 Stage fingerprint，也不会生成伪造的完整构建报告。
- 报告列表能容忍目录不存在、空目录、旧 schema 和损坏 JSON。
- Dashboard 不显示、记录或复制签名密码。
- CI 入口和 P0-P9 行为无回归；人工构建统一从 Dashboard 发起。
- Unity 编译通过，新增 EditMode 测试通过；可用环境中的手工验收项全部完成，外部环境阻塞项有证据和跟踪记录。

## 3. 架构约束

### 3.1 必须保持

- 配置唯一来源仍为 `BuildProfile`。
- 正式构建唯一代码入口仍为 `KJBuildPipeline.Build(BuildProfile)`。
- Stage 注册、排序、依赖、跳过和 fingerprint 仍由现有 Runner 管理。
- 不新增 `BuildConfig`、marker、bool mask 或手动选择部分 Stage 的逻辑。
- 构建事务仍由 Runner 统一 rollback。
- CI 继续通过显式 `-profile` 参数或默认 Profile 工作，不依赖 EditorPrefs。

### 3.2 明确不承诺

- Dashboard 不负责修复 P0-P9 当前尚未完成的 Standalone/Android 端到端验证。
- Dashboard 不把 Unity 构建 API 放入后台线程。Unity 构建仍在主线程同步执行。
- Dashboard 不重写 YooAsset `BundleCollectorWindow`。
- Dashboard 不把 UI 中当前选择隐式升级为 CI 或菜单入口的默认 Profile。

## 4. 变更范围

| 类型 | 文件 | 责任 |
|---|---|---|
| 修改 | `Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.cs` | Odin 展示属性、条件显示、敏感字段隐藏、创建入口安全化 |
| 修改 | `Assets/Scripts/Boot.Editor/Build/Config/BuildProfileValidator.cs` | 新字段、凭据引用和数值/路径规则的结构化校验 |
| 修改 | `Assets/Scripts/Boot.Editor/Build/Config/BuildProfileSet.cs` | Profile 目录及默认项约束 |
| 新建 | `Assets/Scripts/Boot.Editor/Build/Config/BuildProfileSet.asset` | Dashboard 可选择 Profile 的显式目录 |
| 新建 | `Assets/Scripts/Boot.Editor/Build/UI/BuildDashboardWindowV2.cs` | 单页 Odin 工作台和操作状态机 |
| 新建 | `Assets/Scripts/Boot.Editor/Build/UI/BuildDashboardProfileStore.cs` | Profile 枚举、选择持久化、创建和修复 |
| 新建 | `Assets/Scripts/Boot.Editor/Build/UI/BuildDashboardReportReader.cs` | 报告发现、限量读取、schema/损坏处理 |
| 新建 | `Assets/Scripts/Boot.Editor/Build/UI/BuildPreflightService.cs` | Profile 校验和 P1 环境预检的受控入口 |
| 新建/修改 | `Assets/Scripts/Boot.Editor/Build/Tests/*` | Profile、预检编排和报告读取 EditMode 测试 |
| 保留 | `Assets/Scripts/Boot.Editor/Build/UI/BuildDashboardWindow.cs` | 旧面板对照入口；试运行结束后单独审批删除 |
| 修改 | `KJBuildPipeline.cs` | 移除重复的直接构建菜单；保留 Build API 与 CI 契约 |
| 不修改 | `BuildPipelineRunner.cs`、P0-P9 Stage | 保持正式构建执行契约 |

按仓库约束，实施后同步更新：

- `.planning/STATE.md`
- `.planning/ROADMAP.md`
- `ProgressDoc/Result/hybridclr_workflow.md` 第 4 节
- `ProgressDoc/Discuss/资源系统/Hy3_构建打包全流程管线_需求分析与设计.md`
- `.agents/skills/kj-build-pipeline/SKILL.md`

## 5. BuildProfile 展示设计

### 5.1 原则

- 不重命名现有非敏感序列化字段，不改变其类型。
- Odin 属性只改变 Editor 展示，不改变字段语义。
- 使用显式 `PropertyOrder` 保证 Inspector 顺序稳定。
- `ShowIf` 使用可编译的 Odin 表达式或无参 bool 属性，不使用伪代码条件。
- UI 隐藏不能代替 Validator；所有强约束仍由 `BuildProfileValidator` 判定。
- `ComputeProfileHash()` 必须覆盖所有影响 Stage 输入、产物或验证结果的非敏感字段；新增字段必须同步加入 hash 覆盖测试。

### 5.2 分组

| 顺序 | 分组 | 默认状态 | 字段 |
|---|---|---|---|
| 0 | 基本信息 | 展开 | ProfileName、Environment、Channel、VersionName、VersionCode |
| 10 | 平台 | 展开 | Platform、PackageId |
| 20 | Android 签名 | 折叠 | KeystorePath、KeystoreAlias、凭据配置状态 |
| 30 | 构建选项 | 展开 | DevelopmentBuild、ScriptDebugging、EnableProfiler、ExtraScriptingDefines |
| 40 | 资源打包 | 展开 | PackageName、AssetDownloadTag、CdnBaseUrl、StartupTypeName、StartupMethodName |
| 50 | 功能开关 | 展开 | EnableRuntimeLog、EnableGm、EnableDebugUi |
| 60 | 冒烟测试 | 折叠 | SmokeEnabled、SmokeRequired、SmokeDeviceSerial、SmokeTimeoutSec |
| 70 | 输出设置 | 折叠 | OutputRoot、KeepLastBuildCount |

Android 字段条件：

- `PackageId` 仅在 `Platform == BuildTarget.Android` 时显示。
- Android 签名分组仅在 Android 平台显示。
- `SmokeDeviceSerial` 仅在 Android 且 `SmokeEnabled` 时显示。
- `SmokeRequired` 和 `SmokeTimeoutSec` 仅在 `SmokeEnabled` 时显示。

环境约束以 `InfoBox` 展示：

- Formal/Audit 必须关闭 Development、Script Debugging、GM 和 Debug UI。
- Formal/Audit Android 必须完成签名配置。
- `SmokeTimeoutSec <= 0`、无效输出路径、空 PackageName 等问题必须由 Validator 返回结构化 issue，不能只依赖提示文字。

### 5.3 凭据安全

现有 `KeystorePassword` 是可序列化明文字段，本次不得继续在 Dashboard 中编辑或回显：

1. 对现有字段增加隐藏/废弃标记，保留字段仅用于 YAML 兼容迁移。
2. 新增的凭据配置只保存环境变量名，例如 `KJ_ANDROID_KEYSTORE_PASSWORD`，不保存值。
3. Dashboard 只显示“已配置”或“未配置”。
4. Validator 只报告变量缺失，不把变量值写入 issue、Console 或报告。
5. 旧资产若检测到明文密码，显示阻塞警告并提供清空操作；不得自动复制或迁移明文。

`ComputeProfileHash()` 不得拼接凭据值。它可以包含环境变量名和“是否已配置”状态，但不能把密码原文或可用于离线猜测的密码摘要写入 fingerprint。

签名值如何注入 PlayerSettings 属于构建管线能力。如果现有 P6 尚未消费这些字段，应单独登记阻塞项，不能由 Dashboard 宣称“已完成正式签名构建”。

## 6. Profile 生命周期

### 6.1 Profile 来源

`BuildProfileSet.asset` 是 Dashboard 的显式 Profile 目录：

- `Profiles` 保存允许在 Dashboard 中出现的 Profile 引用。
- 第一项默认包含现有 `BuildProfile.asset`。
- 空引用和重复引用在加载时过滤并显示可修复警告。
- 不使用全项目 `FindAssets` 作为日常数据源，避免无意纳入样例或废弃配置。
- 提供“扫描并添加未登记 Profile”作为显式修复操作。

### 6.2 当前选择

- 用资产 GUID 而不是路径或 `ProfileName` 保存选择。
- 选择仅保存到 `EditorPrefs`，key 包含项目标识，避免不同项目冲突。
- 恢复顺序：已保存 GUID -> ProfileSet 默认项 -> 现有默认 `BuildProfile.asset` -> 空状态。
- Profile 被移动时 GUID 仍有效；被删除时进入空状态并禁止构建。
- 当前选择只影响 V2 Dashboard。菜单和 CI 的默认行为保持不变，UI 必须明确提示这一点。

### 6.3 新建 Profile

点击“新建配置”时：

1. 使用 `SaveFilePanelInProject` 选择 `Assets/` 下的唯一资产路径。
2. 从当前 Profile 克隆；无当前 Profile 时使用 `CreateInstance<BuildProfile>()` 默认值。
3. 清除任何遗留明文凭据。
4. 创建资产、加入 ProfileSet、设为当前选择。
5. 调用 `SetDirty`、`SaveAssets`，并在失败时保持原选择。

取消文件对话框不产生任何资产或目录。

### 6.4 编辑和保存

- Profile 修改后显示未保存状态。
- 提供显式“保存配置”和“还原未保存修改”。
- “验证配置”“运行预检”“完整构建”执行前都必须 `SetDirty + SaveAssets`。
- 保存失败时禁止后续操作，并显示异常摘要。

## 7. Dashboard 布局

窗口基类使用 `OdinEditorWindow`，最小尺寸 `760 x 620`，主体使用单页滚动布局。

```text
构建面板
当前配置 [Profile 下拉] [新建] [保存] [定位资产]
状态摘要：环境 / 平台 / 版本 / 当前输出路径 / 配置合法性

BuildProfile 内嵌编辑器

[打开 YooAsset 资源收集器]
[验证配置] [运行预检] [打开输出目录] [打开最新报告]
[完整构建]

当前操作状态 / 当前 Stage / 结果摘要

P0-P9 阶段表（默认折叠）
最近构建报告（默认展开）
```

### 7.1 中文显示

- 按钮、字段、分组、状态、校验摘要全部使用中文。
- Stage `Id` 保留原值以便诊断，DisplayName、Category、Policy 通过只读映射显示中文。
- BuildEnvironment 和受支持平台使用中文显示项，但底层枚举值不变。
- 未建立中文映射的新枚举值回退为原始名称，不能导致窗口异常。

### 7.2 YooAsset 入口

当前锁定的 YooAsset 包中已验证存在：

```csharp
YooAsset.Editor.BundleCollectorWindow.OpenWindow();
```

`Boot.Editor.asmdef` 已引用 `YooAsset.Editor`。实现仍需：

- 使用准确命名空间。
- 捕获窗口打开异常并输出可操作提示。
- 不通过反射调用第三方内部 API。
- YooAsset 升级后将该入口纳入编译验证。

## 8. 操作语义

### 8.1 验证配置

纯读取操作，不切换平台，不写输出目录，不执行 Stage：

1. 保存当前 Profile。
2. 调用 `BuildProfileValidator.Validate(profile)`。
3. 调用 `BuildStageRegistry.ValidateDependencies()`。
4. 将结果按 Error、Warning、Info 展示。
5. 存在 blocking issue 时将完整构建按钮标记为不可执行。

验证结果不是缓存的安全凭据；点击完整构建时必须重新验证。

### 8.2 运行预检

“运行预检”表示实际环境预检，不是 dry-run：

1. 保存并执行“验证配置”。
2. 有 blocking issue 时立即结束，不执行 P1。
3. 若目标平台与当前平台不同，弹窗明确告知将触发平台切换和可能的脚本重编译；用户取消则无副作用。
4. 创建独立 `BuildContext`、`BuildPaths` 和 `BuildTransaction`。
5. 调用 `P1_PreflightStage.Execute(context)`，成功后调用 `Verify(context)`。
6. 捕获异常并输出 `PreflightResult`：Success、Issues、Duration、TargetChanged、ErrorSummary。
7. `finally` 中 rollback 已登记事务，并释放操作锁。

约束：

- 不写 Stage fingerprint。
- 不写 `build_report.json`，避免与正式构建报告混淆。
- P1 当前的平台切换不是 BuildTransaction 管理对象；预检后保留目标平台，这是明确、已确认的副作用。
- 不直接调用 P0，因为 P0 的 Verify 依赖 Runner 预先生成的 build plan 文件。

### 8.3 完整构建

1. 保存 Profile。
2. 重新执行配置和 Stage 依赖校验。
3. blocking issue 存在时禁止启动并聚焦问题列表。
4. Formal/Audit 构建显示二次确认，内容包含 Profile、环境、平台、版本和输出路径。
5. 获取全局操作锁，禁用 Profile 编辑和全部执行按钮。
6. 写入 `SessionState` 运行标记，仅用于窗口恢复展示，不参与 Runner 决策。
7. 同步调用 `KJBuildPipeline.Build(selectedProfile)`。
8. 根据 `BuildReportData` 显示成功、失败阶段、报告路径和问题摘要。
9. `finally` 中释放 UI 锁、清理进度展示并刷新报告列表。

Dashboard 不自行执行 Stage、不自行 rollback、不写 fingerprint。

## 9. 操作状态机

```text
Idle -> Saving -> Validating -> Idle
Idle -> Saving -> Validating -> Preflighting -> Succeeded/Failed -> Idle
Idle -> Saving -> Validating -> Building -> Succeeded/Failed -> Idle
```

规则：

- 非 Idle 状态禁止再次启动验证、预检或构建。
- Profile 编辑、切换和新建在 Preflighting/Building 状态禁用。
- 所有状态迁移使用 `try/finally`，异常不能遗留永久锁。
- V2 窗口的进程内全局锁使用静态状态；`SessionState` 只负责 Domain Reload 后提示“上次操作可能被中断”。
- Runner 报告是构建结果的唯一事实来源。窗口恢复时重新扫描报告，不根据 SessionState 推断成功。
- 本次不实现伪异步或后台线程。进度可使用 Runner 日志、Stage 结果和 Unity 进度 UI 增强，但不得跨线程调用 Unity API。

## 10. 报告列表

### 10.1 发现规则

- 搜索根为 `BuildBackup/` 以及 ProfileSet 中显式 `OutputRoot` 的去重绝对路径。
- 先按文件最后修改时间排序，只读取最近 50 个 `build_report.json`，避免无界扫描和解析。
- 刷新时机：窗口打开、Profile 切换、构建结束、用户点击刷新。
- 文件 IO 在 Editor 主线程执行时必须限量；不在 `OnGUI` 每帧扫描磁盘。

### 10.2 解析和展示

每行展示：版本、环境、平台、完成时间、耗时、结果、失败 Stage、报告路径。

- 使用 `JsonUtility.FromJson<BuildReportData>` 解析当前 schema。
- schema 不支持时展示“版本不兼容”，仍允许定位原文件。
- 损坏或读取中的 JSON 展示“报告不可读取”，不能让整个窗口失效。
- 支持打开 JSON、Markdown、输出目录和复制 `ai_handoff.json` 路径。
- UI 不根据文件夹名猜测成功状态。

Dashboard 不在浏览报告时删除产物。当前代码尚未消费 `KeepLastBuildCount`，因此历史清理是已知能力缺口，需另立构建管线任务，V2 不得宣称已实现自动保留策略。

## 11. 失败处理

| 场景 | 预期行为 |
|---|---|
| ProfileSet 缺失 | 回退默认 Profile，显示修复按钮，不自动覆盖资产 |
| Profile 为空或被删除 | 显示空状态，禁用执行按钮 |
| Profile 保存失败 | 保留编辑内容，禁止预检/构建，显示异常摘要 |
| Validator 返回 blocking issue | 不进入 P1 或 Runner |
| 平台切换被取消 | 预检结果为 Cancelled，不视为构建失败 |
| P1 抛出异常 | 结构化展示 issue 和异常摘要，释放操作锁 |
| Runner 返回失败报告 | 展示失败 Stage，保留报告入口，释放操作锁 |
| Runner 调用前抛异常 | Console 记录完整异常，UI 展示安全摘要 |
| 报告目录不存在 | 展示空状态，不自动创建目录 |
| 报告 JSON 损坏 | 单行标记不可读取，其他报告继续展示 |
| YooAsset 窗口 API 异常 | 显示包版本/API 检查建议，不影响 Dashboard |

## 12. 测试方案

### 12.1 EditMode 自动化测试

- ProfileStore 按 GUID 恢复选择。
- 选择失效时按既定优先级回退。
- ProfileSet 过滤 null 和重复引用。
- 新建 Profile 使用唯一路径并清除遗留明文凭据。
- BuildProfile Odin 改造后原字段序列化值不变。
- 任一影响构建的非敏感 Profile 字段变化都会改变 `ComputeProfileHash()`；凭据值不会进入 hash。
- 非 Android 不展示 Android 专属字段的条件方法返回 false。
- Validator blocking issue 阻止 PreflightService 调用 P1。
- PreflightService 成功、异常和取消路径都释放操作锁。
- PreflightService 不创建 fingerprint 和正式报告。
- ReportReader 正确解析成功、失败、旧 schema、损坏 JSON 和空目录。
- ReportReader 最多读取约定数量，排序稳定。
- Stage 中文映射未知值能回退。

对 Unity 平台切换、HybridCLR 安装检查等重型行为使用可注入适配器或手工测试，不在普通 EditMode 测试中实际切换平台。

### 12.2 手工验收矩阵

| 场景 | Standalone Dev | Android Dev | Android Formal |
|---|---:|---:|---:|
| Profile 条件显示 | 必测 | 必测 | 必测 |
| 保存/重开窗口/选择恢复 | 必测 | 必测 | 必测 |
| 配置校验成功与失败 | 必测 | 必测 | 必测 |
| 平台切换确认与取消 | 不适用 | 必测 | 必测 |
| YooAsset 窗口打开 | 必测 | 必测 | 必测 |
| 构建防重复点击 | 必测 | 必测 | 必测 |
| 失败报告展示 | 必测 | 必测 | 必测 |
| 完整 P0-P9 | 至少一次 | 至少一次 | 签名链就绪后必测 |

此外验证：窄窗口、长 ProfileName、长路径、中文路径、无报告目录、损坏报告、Domain Reload 后窗口恢复。

## 13. 实施顺序

1. 完成 ProfileSet/ProfileStore 和对应测试。
2. 完成 BuildProfile Odin 展示及凭据隐藏，验证旧资产序列化无漂移。
3. 完成 ReportReader 和解析测试。
4. 完成 PreflightService 和结构化结果测试。
5. 实现 V2 布局、状态机和所有按钮。
6. Unity 编译、EditMode 测试和手工 UI 验收。
7. 执行至少一次 Standalone P0-P9；Android 环境具备时执行 Android 验收。
8. 更新构建管线权威文档、STATE、ROADMAP 和 skill。
9. V2 试运行通过后再单独决定是否删除旧 Dashboard。

## 14. Definition of Done

- [ ] 所有范围内代码完成且 Unity 无编译错误。
- [ ] 新增 EditMode 测试全部通过，原构建管线测试无回归。
- [ ] Profile 创建、保存、选择恢复和缺失修复通过手工验收。
- [ ] Dashboard 不显示或持久化新的明文密码。
- [ ] 配置校验、环境预检、完整构建三种操作语义与本方案一致。
- [ ] 构建期间防重入，所有异常路径均释放 UI 状态。
- [ ] 最近 50 份报告可稳定读取，损坏文件不影响其他报告。
- [ ] YooAsset Collector 在当前锁定包版本中可打开。
- [ ] Standalone P0-P9 至少成功执行一次并能从 V2 打开报告。
- [ ] Android/Formal 未完成的外部环境或签名问题被明确登记，不虚报完成。
- [ ] `KeepLastBuildCount` 未落地的现状在 UI 和权威文档中如实标注。
- [ ] 两份构建权威文档、STATE、ROADMAP 和 skill 已同步。

## 15. 非目标与后续项

- 不重写 YooAsset Collector。
- 不支持任意选择 P0-P9 子集执行。
- 不把构建放入后台线程。
- 不删除旧 Dashboard。
- 不在报告浏览时自动删除历史构建。
- 构建取消、跨 Domain Reload 的阶段级续跑、远程构建队列属于后续版本。
- 旧 Dashboard 的最终下线条件：V2 完成上述 DoD，并经过至少一个迭代周期试运行。
