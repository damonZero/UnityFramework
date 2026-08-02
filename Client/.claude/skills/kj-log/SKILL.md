---
name: kj-log
description: >
  KJ Framework 日志系统指南。涵盖 Framework.Log 稳定接口（GameLog、GameLogProfile、
  GameLogConfig、GameLogSwitches、IGameLogSink）、Core 到 ZLogger Unity Console provider 的桥接、
  按打包环境编译期裁剪、按模块树运行时过滤、ZLoggerMessage 源生成日志模板规则。
  触发场景：新增日志、替换 Debug.Log、配置 dev/formal 等包环境日志级别、模块日志开关、
  编写 XxxLog.cs、接入 ZLogger、讨论 GameLogBridge、日志面板/打包脚本、AI 运行日志落盘。
---

# KJ 日志系统指南

## 分层职责

- `Assets/Framework/Log/` 是稳定日志接口层，不能依赖 Core、VContainer、ZLogger 或 Microsoft.Extensions.Logging。
- `Assets/Framework/RuntimeLog/` 是 AI 运行日志文件层，负责 JSONL/session writer，只引用 `Log`，`noEngineReferences=true`。
- `Assets/Scripts/Core/Logging/GameLogBridge.cs` 只负责把 `Framework.Log.IGameLogSink` 接到 Core 的 ZLogger 管线。
- `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs` 注册 `ILoggerFactory`、`ILogger<T>` 和 `AddZLoggerUnityDebug` provider。
- 业务层和 Framework 层不直接调用 `UnityEngine.Debug.Log*`。
- AI 运行日志规范见 `.planning/AI_RUNTIME_LOGGING.md`；运行日志落盘和 session 清单属于 `Framework.RuntimeLog` 稳定包，Unity/ZLogger 接入属于 Core/Logging，Framework.Log 只放稳定接口、数据结构和启动缓冲。

## 输出管线

当前输出使用 ZLogger Unity provider：

```csharp
logging.SetMinimumLevel(LogLevel.Trace);
logging.AddFilter((category, level) =>
    GameLog.Profile.IsEnabled(category ?? GameLog.DefaultModule, ToGameLogLevel(level)));
logging.AddZLoggerUnityDebug(options =>
{
    options.PrettyStacktrace = true;
});
```

ZLogger 内部会输出到 Unity Console，并保留更好的堆栈/跳转体验。不要因为底层 provider 使用 Unity Console，就在业务层回退到 `Debug.Log`。

## AI 运行日志

运行日志文件是 AI 调试的默认证据。Editor/dev/QA 环境目标输出为 JSON Lines + session 清单：

```text
Logs/Runtime/latest.jsonl
Logs/Runtime/latest.session.json
```

实现规则：

- Console provider 继续服务人类观察；文件日志服务 AI 分析和自动化验证。
- JSONL 是规范格式，人类可读 `.log` 只能作为辅助。
- Boot 阶段不能依赖 Core/ZLogger；当前通过 `BootRuntimeLogBootstrap` 安装 `Framework.RuntimeLog.RuntimeLogSession`，Boot 失败也尽量落盘。
- `Assets/Scripts/Core/Logging/` 负责 Unity 路径/session 信息、ILogger/ZLogger provider、flush 和 Core 接管；文件 writer 本身在 `Framework.RuntimeLog`。
- Player smoke、资源加载矩阵、热更 smoke 的报告应引用日志文件路径和关键错误摘要。
- 日志不得写 token、密码、实名账号、支付信息等敏感数据。

## 打包环境与编译期裁剪

编译期符号集中定义在 `Framework.Log.GameLogSymbols`；`GameLog.SymbolTrace` 等旧入口只作为兼容别名，不再在业务日志模板里重复写字符串：

| 环境 | 符号 | 保留级别 |
| --- | --- | --- |
| Trace 诊断包 | `KJ_LOG_TRACE` | Trace+ |
| Development/dev | `KJ_LOG_DEBUG` 或 `DEVELOPMENT_BUILD` / `UNITY_EDITOR` | Debug+ |
| QA | `KJ_LOG_INFORMATION` | Information+ |
| Formal monitoring | `KJ_LOG_WARNING` | Warning+ |
| Formal | `KJ_LOG_ERROR` | Error+ |
| Critical only | `KJ_LOG_CRITICAL` | Critical |
| Silent | 无日志符号 | none |

业务代码不要写这些 `[Conditional]`。普通日志使用 `GameLog.Debug/Info/Warn/Error/...`，调用点由 `GameLog` 门面集中裁剪。

同一个方法上的多个 `[Conditional]` 是 OR 关系：任意一个符号存在，调用点就会保留。运行时再由 `GameLogProfile.IsEnabled(module, level)` 做第二层过滤。

`XxxLog.cs` 中的 `[ZLoggerMessage]` 源生成方法如需在 formal 包删除调用点，应在该日志声明方法上加对应 `[Conditional(GameLogSymbols...)]`，集中在日志模板文件，不散落到业务逻辑，也不重复定义符号字符串。

## 模块过滤

模块过滤参考旧工程 `DebugSwitches` 思路：环境级别和模块规则分层。

- `GameLog.ApplyEnvironment(GameLogEnvironment.Formal)` 只改变全局默认最低级别，不清空模块规则。
- `GameLog.SetModuleMinimumLevel("Core.Asset", GameLogLevel.Warning)` 设置模块覆盖级别。
- `GameLog.SetModuleEnabled("Project.Fight", false)` 禁用模块。
- `GameLog.SetModuleEnabled("Project.Fight", true)` 清除模块覆盖，恢复继承全局规则。
- `GameLogSwitches.Configure(GameLogConfig config)` 是未来 Editor 面板和打包脚本的统一入口。
- `GameLogTreeAttribute` / `GameLogSwitchAttribute` 是 LOG-TOOLS 预留扫描声明，当前不直接参与运行时过滤。

模块名支持 `.`、`/`、`\` 层级回溯。`Core.Asset.AssetSystem` 可继承 `Core.Asset` 或 `Core` 规则；`Assets/Scripts/Core/Asset/AssetSystem.cs` 可继承路径规则。

## 使用规范

Framework 或 Boot 中需要日志时：

```csharp
GameLog.Info("Asset runtime initialized", "Framework.Asset");
GameLog.Error("Asset load failed", "Framework.Asset");
```

Core/General 中稳定、高频、结构化日志优先用 `ILogger<T>` + `[ZLoggerMessage]`：

```csharp
using System.Diagnostics;
using Framework.Log;
using Microsoft.Extensions.Logging;
using ZLogger;

internal static partial class AssetSystemLog
{
    [Conditional(GameLogSymbols.UnityEditor), Conditional(GameLogSymbols.DevelopmentBuild), Conditional(GameLogSymbols.Trace), Conditional(GameLogSymbols.Debug), Conditional(GameLogSymbols.Information)]
    [ZLoggerMessage(LogLevel.Information, "[AssetSystem] Ready")]
    internal static partial void Ready(ILogger logger);
}
```

`XxxLog.cs` 跟随所属类目录，不集中塞到 `Core/Logging/`。`Core/Logging/` 只放日志管线/桥接。

## 禁止事项

- 不在业务代码直接使用 `Debug.Log/LogWarning/LogError`。
- 不把 Unity Console 截图作为 AI 调试的默认输入；能读日志文件时优先读 `.jsonl`。
- 不在业务逻辑文件里散写日志裁剪符号。
- 不让 Framework.Log 引用 VContainer、Core、ZLogger、Microsoft.Extensions.Logging。
- `Framework.Log/Log.asmdef` 使用 `noEngineReferences=true`，不引用 UnityEngine API。
- 不把 `GameLogBridge` 做成 `[CoreSystem]`；它是 adapter，不是生命周期系统。
