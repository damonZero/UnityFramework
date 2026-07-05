# AI Runtime Logging and Diagnostics

> 目标：把运行日志变成 AI 可以直接读取、检索、归因和复盘的一等调试产物，而不是只给人看的 Console 输出。

## 背景

KJ 当前已经用 `Framework.Log` 作为稳定日志门面，并由 Core 通过 ZLogger 接管 Unity Console 输出。由于项目主要由 AI 参与构建，运行期问题不应该依赖用户截图、手工复制 Console 或口头描述。Editor Play、Player smoke、资源验证、热更验证都应该留下结构化日志文件，让 AI 能直接分析失败链路。

因此日志系统分成两类输出：

- **Console 输出**：给人实时观察，继续使用 ZLogger Unity provider，保留堆栈跳转体验。
- **运行日志文件**：给 AI 和自动化验证使用，作为调试、回归、复盘和上下文交接的主要依据。

## 总原则

1. 所有业务和框架日志仍然从 `GameLog` / `ILogger<T>` / `[ZLoggerMessage]` 进入，不允许回退到 `Debug.Log`。
2. Console 不是唯一输出端。dev/editor/diagnostic 包必须能落盘完整运行日志。
3. AI 分析以结构化文件为准，Console 只作为人类即时反馈。
4. 每次运行生成独立 session，日志文件和会话清单必须能对应到一次启动。
5. Boot、HybridCLR、YooAsset、Core/SystemManager、Asset、Pool、General/Project Model 生命周期都是默认采集范围。
6. 日志不得记录 token、密码、实名账号、手机号、身份证、支付信息或其他敏感数据；必要时只记录脱敏值、hash 或短 ID。

## 文件位置

运行日志是生成物，不进入版本库。`.gitignore` 已忽略 `Logs/`。

推荐默认路径：

```text
Editor:
  <ProjectRoot>/Logs/Runtime/{yyyyMMdd_HHmmss}_{platform}_{sessionId}.jsonl
  <ProjectRoot>/Logs/Runtime/{yyyyMMdd_HHmmss}_{platform}_{sessionId}.session.json
  <ProjectRoot>/Logs/Runtime/latest.jsonl
  <ProjectRoot>/Logs/Runtime/latest.session.json

Player:
  {Application.persistentDataPath}/Logs/Runtime/{yyyyMMdd_HHmmss}_{platform}_{sessionId}.jsonl
  {Application.persistentDataPath}/Logs/Runtime/{yyyyMMdd_HHmmss}_{platform}_{sessionId}.session.json
```

`latest.*` 只在 Editor 或本地诊断环境维护，用于 AI 快速定位最近一次运行。Player 真机日志由导出工具或平台日志采集工具复制到工作区 `Logs/Runtime/Imported/` 后再分析。

## 文件格式

规范格式是 JSON Lines：一行一条日志。人类可读 `.log` 可以作为辅助输出，但 AI 分析以 `.jsonl` 为准。

每条日志至少包含：

```json
{
  "schema": "kj.runtime.log.v1",
  "timeUtc": "2026-07-05T12:34:56.789Z",
  "sessionId": "20260705-123456-a1b2c3",
  "seq": 42,
  "frame": 128,
  "threadId": 1,
  "level": "Information",
  "module": "Core.Asset",
  "category": "Core.Asset.AssetSystem",
  "phase": "Core.Init",
  "message": "[AssetSystem] Ready",
  "exceptionType": null,
  "exceptionMessage": null,
  "stackTrace": null
}
```

允许扩展字段：

- `assetPackageName` / `assetPackageVersion`
- `hotUpdateAssemblies`
- `bootStep`
- `scene`
- `correlationId`
- `elapsedMs`
- `context`

扩展字段只能添加，不能改变已有字段语义。破坏性变更必须提升 `schema` 版本。

## Session 清单

每次运行必须生成 `.session.json`，用于把日志和运行环境绑定。至少包含：

- `schema`
- `sessionId`
- `startTimeUtc`
- `projectName`
- `unityVersion`
- `platform`
- `applicationVersion`
- `buildGuid` 或构建时间
- `gitCommit`，如果本地可得
- `logProfile`，包含环境和最低级别
- `assetPlayMode`
- `assetPackageName`
- `hotUpdateAssemblies`
- `aotMetadataAssemblies`

Session 清单不能依赖 Core/Project 才能写出。Boot 阶段失败也必须尽量留下清单和错误日志。

## 当前落地结构

第一版已落地为独立 `Framework.RuntimeLog` 包，而不是把文件 writer 塞进 `Framework.Log` 或 Core：

- `Assets/Framework/Log/`：保留稳定日志门面、模块开关、`IGameLogSink` 和启动期有界缓冲。
- `Assets/Framework/RuntimeLog/`：纯 C# session writer，负责 JSONL、session 清单、`latest.*`、sessionId、文件名、phase 归类和 `RuntimeLogManager`。该包只引用 `Log`，`noEngineReferences=true`。
- `Assets/Scripts/Boot/`：`BootRuntimeLogBootstrap` 在 `Entry.Awake()` 最早安装 runtime session，Boot 失败时也尽量写入文件并 flush。
- `Assets/Scripts/Core/Logging/`：`RuntimeLogBootstrap` 补全 Unity/session 信息，`RuntimeLogLoggerProvider` 接入 `ILogger<T>` / `[ZLoggerMessage]`，`GameLogBridge` 同时写 runtime session 和 ZLogger Unity Console。
- `Assets/Scripts/Core.Editor/Logging/`：提供 `KJ/Runtime Logs/*` 菜单，打开 latest、生成摘要、导出诊断包、清理本地日志。
- `Assets/Framework/TestKit/Fakes/RecordingRuntimeLogSink.cs`：测试用记录 sink，供启动缓冲和接入顺序测试复用。

这套结构的原则是：文件格式和 session 生命周期作为稳定 Framework 能力可被 Boot/TestKit 复用；Unity 路径、帧号、ZLogger provider 和 Editor 菜单留在 Scripts/Core 侧。

## 分层职责

### Framework.Log

`Assets/Framework/Log/` 只定义稳定接口、数据结构和启动期缓冲：

- `GameLog`
- `GameLogEntry`
- `IGameLogSink`
- `GameLogConfig`
- 早期启动 ring buffer
- 未来可增加上下文接口、脱敏规则数据

Framework.Log 不引用 UnityEngine、VContainer、ZLogger、Core 或任何 `Assets/Scripts` 代码。

### Framework.RuntimeLog

`Assets/Framework/RuntimeLog/` 负责 AI runtime logging 的稳定文件产物：

- `RuntimeLogSession`
- `RuntimeLogSessionInfo`
- `RuntimeLogEntry`
- `RuntimeLogManager`
- `RuntimeLogJson`
- `RuntimeLogPhaseResolver`

Framework.RuntimeLog 只引用 `Framework.Log`，不引用 UnityEngine、VContainer、ZLogger、Core 或任何 `Assets/Scripts` 代码。

### Boot

Boot 可以通过 `GameLog` 写启动更新日志，但不能引用 Core/ZLogger。

Boot 阶段日志必须被保留。当前实现是在 `Entry.Awake()` 调用 `BootRuntimeLogBootstrap.EnsureInstalled()`，安装 `Framework.RuntimeLog.RuntimeLogSession` 作为早期 `GameLog.Sink`。当 Core 后续接管时，`GameLogBridge` 会替换 sink，但沿用同一个 session，避免 Boot 日志丢失或 session 断裂。

### Core.Logging

`Assets/Scripts/Core/Logging/` 负责把日志接入真实运行管线：

- 注册 ZLogger Unity Console provider。
- 注册运行日志 provider，复用或创建 `RuntimeLogSession`。
- 补全 session 清单中的 Unity、资源运行时和启动配置字段。
- 将 `GameLogBridge`、`ILogger<T>`、ZLoggerMessage 输出统一汇入文件。
- 在退出或崩溃前尽量 flush。

`GameLogBridge` 仍是 adapter，不是 `[CoreSystem]`。

### Editor 工具

日志面板、模块开关和本地分析工具放所属模块的 Editor 程序集：

```text
Assets/Framework/Log.Editor/      # Framework.Log 配置面板、模块树配置
Assets/Scripts/Core.Editor/Logging/ # Core 日志收集、打开 latest、导出诊断包
```

跨层打包入口确实无法归属时，才放 `Assets/Editor/`。

## 默认策略

| 环境 | 文件日志 | 默认级别 | 说明 |
| --- | --- | --- | --- |
| Unity Editor | 开启 | Debug+ | AI 本地开发默认可分析 |
| Development Player | 开启 | Debug+ 或 Information+ | Player smoke 必须保留 |
| QA | 开启 | Information+ | 用于测试服问题回放 |
| Formal monitoring | 可开启 | Warning+ | 仅保留异常和关键状态 |
| Formal | 默认关闭或 Error+ | Error+ | 由隐私和性能策略决定 |

运行时模块过滤仍由 `GameLogProfile` / `GameLogConfig` 控制。文件日志和 Console 日志可以有不同 minimum level，但不能绕过模块禁用规则。

## AI 开发工作流

以后处理运行期问题时，优先按这个顺序：

1. 找最新 session 清单和 `.jsonl` 日志。
2. 先检索 `Critical` / `Error` / `Exception`。
3. 再按 `sessionId`、`phase`、`module` 看启动链路：
   `Boot -> YooAsset -> HybridCLR -> ProjectStartup -> Core -> General -> Project`
4. 对资源问题，补看 asset package version、raw file path、download result、cache/owned handle 生命周期。
5. 对热更问题，补看 DLL/AOT metadata 列表、加载顺序、重启生效判断。
6. 对 Player smoke，最终报告必须引用日志文件路径和关键错误摘要。

不要把“请用户截图 Console”作为默认调试方式。除非文件日志缺失，否则 AI 应直接读运行日志。

## 验收标准

第一版 `LOG-AI-01` 完成时至少满足：

- Editor Play 自动生成 `Logs/Runtime/latest.jsonl` 和 `latest.session.json`。
- Player Development Build 自动生成 session 日志。
- Boot 到 Core 的关键启动日志不会因为 Core logger 尚未安装而丢失。
- `GameLog` 和 `ILogger<T>` / `[ZLoggerMessage]` 的输出都能进入同一 session 文件。
- 异常日志包含 exception type、message、stack trace。
- Player smoke 报告可以只靠日志文件判断启动链路是否成功。
- 退出时 flush，异常退出时最多丢失最后少量 buffered 日志。

后续 `LOG-AI-02` 再补：

- Editor 菜单：打开 latest、导出诊断包、清理旧日志。
- 自动摘要：按严重级别、模块、启动阶段生成 Markdown 报告。
- 日志 retention、压缩和大小限制。
- 敏感字段脱敏测试。

## 禁止事项

- 不在业务层写 `Debug.Log`。
- 不把 Console 文本作为唯一诊断依据。
- 不把运行日志提交到版本库。
- 不在日志中写账号密码、鉴权 token、支付信息或隐私数据。
- 不让 Framework.Log 依赖 Core、ZLogger、UnityEngine 或 VContainer。
- 不为了 AI 分析把 formal 包默认打开 Trace/Debug 级别。
