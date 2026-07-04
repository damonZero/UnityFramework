# Logging System Review 报告

**Review 日期:** 2026-07-04
**Review 范围:** `Assets/Framework/Log/` + `Assets/Scripts/Core/Logging/` + 各层 `*Log.cs`
**Review 轮次:** 第 1 轮

---

## 整体评价

项目采用**双层日志架构**，设计清晰：`Framework.Log` 作为稳定门面（零第三方依赖），`Core.Logging.GameLogBridge` 桥接到 ZLogger + Unity Console 后端。业务层通过 ZLogger Source Generator 输出结构化日志。依赖方向完全符合 CLAUDE.md 规范，编译期 `[Conditional]` 裁剪 + 运行时 `GameLogProfile` 过滤形成双重保障。

---

## 架构总览

```
┌─────────────────────────────────────────────────────┐
│  Framework.Log（稳定门面，零依赖）                     │
│  GameLog static class  →  IGameLogSink              │
│  GameLogProfile / GameLogConfig / GameLogSwitches    │
│  Conditional 编译期裁剪                                │
├─────────────────────────────────────────────────────┤
│  Core.Logging（桥接层）                                │
│  GameLogBridge : IGameLogSink                        │
│       ↓                                              │
│  Microsoft.Extensions.Logging + ZLogger              │
│       ↓                                              │
│  Unity Console (AddZLoggerUnityDebug)                │
├─────────────────────────────────────────────────────┤
│  业务层（Core/General/Project）                        │
│  ZLoggerMessage Source Generator → 结构化日志         │
│  ILogger<T> via VContainer DI                        │
└─────────────────────────────────────────────────────┘
```

---

## 优点

### 1. 依赖方向正确

**文件:** `Assets/Framework/Log/Log.asmdef`

```json
"references": []
```

`Framework.Log` 的引用列表为空，不依赖 VContainer、ZLogger 或任何第三方库。Framework 层的独立性和可替换性得到保证。

### 2. Boot 层最小依赖保持良好

**文件:** `Assets/Scripts/Boot/KJ.Boot.asmdef`

```json
"references": ["VContainer", "Log"]
```

Boot 层只引用 `VContainer` + `Log`，通过 `GameLog.Info()` 输出日志，不触碰 ZLogger 或 Microsoft.Extensions.Logging。符合 CLAUDE.md 中"Boot 不引用 Core"的约束。

### 3. 分层日志策略合理

| 层级 | 日志方式 | 适用场景 |
|------|---------|---------|
| Framework / Boot | `GameLog` 静态门面 | 无 DI 可用时 |
| Core / General / Project | ZLogger Source Generator `[ZLoggerMessage]` | 有 DI、需要结构化日志时 |

两种方式共存且不冲突：`GameLog` 通过 `IGameLogSink` → `GameLogBridge` 最终也汇入 ZLogger 管线。

### 4. 编译期裁剪设计精巧

**文件:** `Assets/Framework/Log/GameLog.cs:78-157`

`[Conditional]` 属性链式叠加实现级别裁剪——`Error` 方法叠加了 `Trace`→`Debug`→`Info`→`Warning`→`Error` 全部条件符号。例如只定义 `KJ_LOG_WARNING` 时，Warning/Error/Critical 三个级别会被保留，Debug/Info 在编译期就被 IL 裁剪掉。这是对 `Conditional` 特性的巧妙利用。

### 5. 模块级日志过滤支持层级匹配

**文件:** `Assets/Framework/Log/GameLogProfile.cs:98-115`

```csharp
private GameLogLevel ResolveMinimumLevel(string module)
{
    var cursor = module;
    while (true)
    {
        if (_moduleMinimumLevels.TryGetValue(cursor, out var level))
            return level;
        var index = cursor.LastIndexOfAny(ModuleSeparators);
        if (index < 0) return MinimumLevel;
        cursor = cursor[..index];
    }
}
```

通过 `.` `/` `\` 分隔符向上查找到最近的模块规则。例如模块 `Core.Asset.YooAsset` 会先查找自身，再查 `Core.Asset`，再查 `Core`，最后 fallback 到全局 `MinimumLevel`。比简单的 Dictionary 查找更灵活。

### 6. 环境预设覆盖完整生命周期

**文件:** `Assets/Framework/Log/GameLogEnvironment.cs`

6 种环境预设覆盖了从正式发布到开发调试的完整场景：

| 环境 | MinimumLevel | 适用 |
|------|-------------|------|
| `Silent` | `None` | 完全静默 |
| `Formal` | `Error` | 正式发布 |
| `FormalMonitoring` | `Warning` | 正式+监控 |
| `Qa` | `Information` | QA 测试 |
| `Development` | `Debug` | 日常开发 |
| `Trace` | `Trace` | 详细追踪 |

### 7. 全项目无 `Debug.Log` 残留

搜索结果显示项目源码中已经没有 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` 的调用残留，只有第三方 Package 的 README 中有文本提及。日志统一性做得彻底。

---

## 问题与建议

### 🔴 问题 1：`GameLogTreeAttribute` 和 `GameLogSwitchAttribute` 属于死代码

**文件:** `Assets/Framework/Log/GameLogTreeAttribute.cs`, `Assets/Framework/Log/GameLogSwitchAttribute.cs`

这两个 Attribute 在当前代码中没有任何消费方。`GameLogTreeAttribute` 仅在 `GameLog.cs:40` 被声明了一次：

```csharp
[GameLogTree(CSharpRootPath, CSharpFilePattern)]
public const string CSharpModuleTree = "KJ C# Logs";
```

但没有代码去读取这个 Attribute 的值。ROADMAP 中列出了 `LOG-TOOLS 日志工具面板/打包接入` 作为待实现项，这两个 Attribute 应该是为该面板预留的。

**建议:** 
- 如果近期会实现 LOG-TOOLS，可以保留但建议在代码注释中标注 `// Reserved for LOG-TOOLS`
- 如果短期内不实现，建议暂时移除，避免后来者困惑

### 🟡 问题 2：`ToMicrosoftLogLevel` 映射逻辑重复定义

**文件 (A):** `Assets/Scripts/Core/Logging/GameLogBridge.cs:43-54`
**文件 (B):** `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs:56-66`

两处有完全相同的 `GameLogLevel` → `LogLevel` 映射代码：

```csharp
// GameLogBridge.cs
private static LogLevel ToMicrosoftLogLevel(GameLogLevel level)
{
    return level switch
    {
        GameLogLevel.Trace => LogLevel.Trace,
        GameLogLevel.Debug => LogLevel.Debug,
        GameLogLevel.Information => LogLevel.Information,
        GameLogLevel.Warning => LogLevel.Warning,
        GameLogLevel.Error => LogLevel.Error,
        GameLogLevel.Critical => LogLevel.Critical,
        _ => LogLevel.None
    };
}
```

**建议:** 提取为 `GameLogBridge` 上的 `internal static` 方法，供 `CoreContainerRegistration` 复用。

### 🟡 问题 3：`CoreContainerRegistration` 中存在双向映射但 `ToGameLogLevel` 仅一处使用

**文件:** `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs:55-81`

```csharp
private static LogLevel ToMicrosoftLogLevel(GameLogLevel level) { ... }  // 重复于 GameLogBridge
private static GameLogLevel ToGameLogLevel(LogLevel level) { ... }        // 仅用于 filter
```

`ToGameLogLevel` 仅在 `AddFilter` 回调中使用，功能正确但代码分散。建议将两个映射方法都集中到 `GameLogBridge` 或一个共享的 `LogLevelConverter` 工具类中。

### 🟡 问题 4：ZLogger `partial` Log 类中条件符号常量重复定义

**文件:**
- `Assets/Scripts/Core/Asset/AssetSystemLog.cs:9-16`
- `Assets/Scripts/Core/Systems/SystemManagerLog.cs:10-16`
- `Assets/Scripts/Core/Systems/StartupProbeSystemLog.cs:9-12`
- `Assets/Scripts/General/Models/ModelLifecycleLog.cs:10-16`

每个 Log 文件都重复定义了相同的条件符号常量：

```csharp
private const string UnityEditor = "UNITY_EDITOR";
private const string DevelopmentBuild = "DEVELOPMENT_BUILD";
private const string Trace = "KJ_LOG_TRACE";
private const string Debug = "KJ_LOG_DEBUG";
private const string Info = "KJ_LOG_INFORMATION";
private const string Warning = "KJ_LOG_WARNING";
private const string Error = "KJ_LOG_ERROR";
```

**建议:** 提取到一个共享的 `internal static class LogSymbols` 中，减少重复和后续修改的维护成本。

### 🟡 问题 5：`[Conditional]` 叠加语义需要更清晰的文档说明

**文件:** `Assets/Framework/Log/GameLog.cs:85-93`

```csharp
[Conditional(UnityEditor)]
[Conditional(DevelopmentBuild)]
[Conditional(SymbolTrace)]
[Conditional(SymbolDebug)]
public static void Debug(string message, ...)
```

多个 `[Conditional]` 是 **OR** 关系——任意一个符号定义即激活。这层编译期裁剪与 `GameLogProfile.IsEnabled()` 的运行时过滤形成双重保障。但这种"编译 OR + 运行 AND"的叠加容易引起混淆。

**建议:** 在 `GameLog` 类的 XML 注释中补充说明：
```csharp
/// <para>Compile-time: any one of UNITY_EDITOR / DEVELOPMENT_BUILD / KJ_LOG_* defined → method kept.</para>
/// <para>Runtime: <see cref="GameLogProfile.IsEnabled"/> provides a second filter based on module + level.</para>
```

### 🟡 问题 6：`GameLogSwitches` 未做线程保护

**文件:** `Assets/Framework/Log/GameLogSwitches.cs`

```csharp
public static GameLogConfig CurrentConfig { get; private set; }

public static void Configure(GameLogConfig config)
{
    CurrentConfig = config;
    GameLog.ApplyProfile(config != null ? config.CreateProfile() : GameLogProfile.Silent());
}
```

`CurrentConfig` 和 `Configure` 没有线程保护。虽然 Profile 切换通常在启动阶段完成（Unity 主线程），但 `GameLog` 可能从异步回调中调用（如 `AssetRuntime` 中的 `GameLog.Error`），如果在运行时热切换配置存在理论上的 race condition。

**建议:** 当前风险较低，可在 `Configure` 方法注释中添加"应在主线程启动阶段调用"的说明。如果未来需要运行时动态切换，再考虑 `Interlocked` 或轻量锁。

### 🟢 建议 1：`Log.asmdef` 评估 `noEngineReferences`

**文件:** `Assets/Framework/Log/Log.asmdef`

当前 `noEngineReferences: false`。`GameLog` 使用 `[CallerFilePath]` 是纯 C# 特性，不依赖 UnityEngine API。如果确认 Log 模块未来也不会使用 Unity API，可以考虑设为 `true` 以进一步隔离 Framework 层，甚至可以在非 Unity 项目中复用。

### 🟢 建议 2：`Exception` 方法缺少级别参数

**文件:** `Assets/Framework/Log/GameLog.cs:152-157`

```csharp
public static void Exception(Exception exception, string message, ...) =>
    Log(GameLogLevel.Error, module, message, exception, filePath);
```

`Exception` 方法固定使用 `Error` 级别。有时业务可能需要以 `Warning` 级别记录可恢复的异常。

**建议:** 考虑增加重载：
```csharp
public static void Exception(Exception exception, string message,
    GameLogLevel level = GameLogLevel.Error, ...)
```

### 🟢 建议 3：`GameLogEntry` 缺少时间戳字段

**文件:** `Assets/Framework/Log/GameLogEntry.cs`

`GameLogEntry` 没有时间戳字段，当前由 ZLogger 底层补充。如果未来添加文件 Sink、网络 Sink 等非 ZLogger 后端，时间戳可能需要在 Framework 层就记录。

**建议:** 当前不是问题；当需要实现非 ZLogger 的 Sink 时再考虑添加。

### 🟢 建议 4：ROADMAP 中 `LOG-TOOLS` 待实现项

ROADMAP 列出的 `LOG-TOOLS 日志工具面板/打包接入` 是目前日志系统最明显的功能缺口。Editor 面板应允许：
- 可视化浏览 `GameLogTreeAttribute` 定义的模块树
- 运行时切换各模块日志级别
- 保存/加载 `GameLogConfig` 配置
- 打包时自动注入 `KJ_LOG_*` 编译符号

建议在 UI 模块实现完成后优先推进此任务。

---

## 涉及文件清单

| 文件 | 角色 |
|------|------|
| `Assets/Framework/Log/Log.asmdef` | 汇编定义（零引用） |
| `Assets/Framework/Log/GameLog.cs` | 静态日志门面 |
| `Assets/Framework/Log/GameLogEntry.cs` | 日志条目数据 |
| `Assets/Framework/Log/IGameLogSink.cs` | Sink 接口 |
| `Assets/Framework/Log/GameLogProfile.cs` | 运行时过滤配置 |
| `Assets/Framework/Log/GameLogConfig.cs` | 可序列化配置 |
| `Assets/Framework/Log/GameLogSwitches.cs` | 配置切换入口 |
| `Assets/Framework/Log/GameLogEnvironment.cs` | 环境枚举 |
| `Assets/Framework/Log/GameLogModuleRule.cs` | 模块规则（不可变） |
| `Assets/Framework/Log/GameLogModuleRuleData.cs` | 模块规则（可序列化） |
| `Assets/Framework/Log/GameLogTreeAttribute.cs` | 模块树标记（预留） |
| `Assets/Framework/Log/GameLogSwitchAttribute.cs` | 模块开关标记（预留） |
| `Assets/Scripts/Core/Logging/GameLogBridge.cs` | ZLogger 桥接 |
| `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs` | DI 注册 + ZLogger 管线 |
| `Assets/Scripts/Core/Asset/AssetSystemLog.cs` | AssetSystem ZLogger 日志 |
| `Assets/Scripts/Core/Systems/SystemManagerLog.cs` | SystemManager ZLogger 日志 |
| `Assets/Scripts/Core/Systems/StartupProbeSystemLog.cs` | StartupProbe ZLogger 日志 |
| `Assets/Scripts/General/Models/ModelLifecycleLog.cs` | ModelLifecycle ZLogger 日志 |

---

## 总结评分

| 维度 | 评分 | 说明 |
|------|------|------|
| 架构设计 | ★★★★★ | 双层门面 + 桥接模式，依赖方向完全正确 |
| 编译期裁剪 | ★★★★★ | `[Conditional]` 链式设计精巧，零运行时开销裁剪 |
| 运行时过滤 | ★★★★☆ | 模块层级匹配有创意，环境预设覆盖完整 |
| 代码整洁 | ★★★☆☆ | `ToMicrosoftLogLevel` 重复、条件符号常量重复、两个 Attribute 为死代码 |
| 扩展性 | ★★★★☆ | `IGameLogSink` 接口支持多后端，`GameLogConfig` 支持序列化配置 |
| 文档完整性 | ★★★★☆ | `GameLog` 类注释清晰，PROJECT.md 中 ZLogger 使用示例充分 |

**整体结论:** 设计良好，实现扎实。2 个代码重复问题（`ToMicrosoftLogLevel` 映射、条件符号常量）和 2 个预留 Attribute 需要处理，其余为改善建议。建议在推进 `LOG-TOOLS` 任务时一并解决上述问题。
