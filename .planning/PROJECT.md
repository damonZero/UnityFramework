# KJ Unity Framework

## 技术栈

- 引擎: Unity 2022.3.62f2 LTS
- 依赖注入: VContainer
- 热更新: HybridCLR
- 资源管理: YooAsset 3.0
- 配置表: Luban v4.10.1
- 网络通信: Google.Protobuf 3.35.1
- 异步: UniTask v2.5.11
- 事件: MessagePipe
- 日志: Framework.Log 稳定门面 + ZLogger 2.5.10 Unity Console provider；AI 运行日志落盘规范见 `.planning/AI_RUNTIME_LOGGING.md`
- 性能工具: ZString 2.6.0, ZLinq 1.5.6
- UI: UGUI (内置)

## 架构设计

四层分层架构（严格单向依赖）：

```text
Boot <- Core <- General <- Project
```

Boot 层必须保持最小依赖，只承担稳定启动壳、最小更新界面、资源版本检查、清单/资源下载、热更 DLL/AOT metadata 加载，以及反射调用正式游戏入口。Boot 不引用 Core/General/Project，不创建正式游戏容器。
HybridCLR 边界见 `.planning/HOT_UPDATE_BOUNDARY.md`：当前工具默认把 `Core` / `General` / `Project` 作为正式运行时热更程序集；`Boot` 与 `Framework/*` 作为启动加载器和稳定契约保持极薄、稳定。C# 层改动不等同于必须换包；已加载程序集的新 DLL 需要重启 APP/下次启动生效，真正换包仅限 native/player/HybridCLR 底层加载机制或旧包缺少加载能力。目标形态是把 Boot 拆成极薄 BootLoader 和可热更但更新后需重启/下次启动生效的 `Boot.Update`。Boot 必须先完成资源/代码更新，再反射调用热更层的 `Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)`。

启动流程由 Entry 的序列化启动配置驱动，Boot 完成更新后再进入热更层正式组合根：

```text
Entry
  -> BootStartupSettings（Entry prefab/场景序列化，正式环境可由版本清单覆盖）
  -> Framework.Asset 最小资源运行时
  -> 资源版本检查/清单更新/下载/修复
  -> HybridCLR AOT metadata + Core/General/Project DLL
  -> Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)
  -> Project 创建 VContainer root 并按 Core -> General -> Project 注册
```

依赖注入主体通过 VContainer 在 Core / General / Project 各阶段逐步注册。Boot 不参与正式容器构建；`ProjectStartup` 创建 VContainer root，并通过 `CoreStartupContext` 传递注册上下文。

每层对应一个 .asmdef 文件，实现编译隔离。

对象池/缓存体系采用”三段式”边界：
- `Framework/Pool/` 负责纯 C# 对象池（`ObjectPool<T>`）、集合租约（`PooledList<T>` 等）、类型池注册表、GameObject 池（`GameObjectPool`）
- `Framework/Cache/` 负责缓存策略（LRU/FIFO）、资源容器
- `Core/Pool/` 负责桥接：注入 Framework 依赖委托、注册到 VContainer、暴露统一门面 `PoolService`

集合租借必须保留 `using` 用法，不能只提供手动归还 API。

后续新模块默认复用已有 Pool/Cache 能力：
- 临时 `List<T>` / `Dictionary<TKey,TValue>` / `HashSet<T>` / `Queue<T>` / `Stack<T>` 使用 `CollectionPool` 或 `PooledCollections`，禁止在热路径反复 `new` 临时集合。
- 高频创建销毁的纯 C# 对象使用 `ObjectPool<T>` / `TypePool`；实现可重置对象时优先实现 `IPoolable`。
- GameObject / prefab 实例复用走 `GameObjectPool`，由 Core 的 `PoolService` 桥接资源加载和释放，不在业务层手写 instantiate/destroy 循环。
- 有生命周期和容量限制的数据缓存使用 `Framework.Cache.Cache<TKey,TValue>` + LRU/FIFO 策略；资源类缓存优先使用 `ResourceCache` 或 AssetRuntime 已有缓存通道。
- Framework 内部保持独立实现；Core/General/Project 通过已暴露的接口、门面或 VContainer 注册入口使用，不重复实现私有对象池/缓存容器。

## 底层模块下沉原则

稳定、低变动、可替换第三方实现的基础模块下沉到 `Assets/Framework/`，直接成为独立 asmdef：

- `Framework/Asset/`：资源系统统一 API、句柄、配置和 YooAsset 适配。上层只依赖 `IAssetSystem`、`AssetHandle<T>`、`AssetDownloadHandle` 等 Framework 类型，不直接引用 YooAsset。
- `Framework/Event/`：统一事件标记和事件类型扫描。上层事件只使用同一个 `[GameEvent]` 标记；MessagePipe 只是当前注册后端。
- `Framework/Pool/` / `Framework/Cache/`：对象池和缓存基础设施。
- `Framework/TestKit/`：基于 Unity Test Framework / NUnit 的通用测试基础设施、Fake、Probe、Fixture 和断言扩展；不放具体测试用例。

Framework 模块不能引用 `Assets/Scripts/` 下任何汇编；也不放 `Assets/Framework/Package/` 子目录。Core 负责把 Framework 的稳定能力接入 VContainer、MessagePipe、SystemManager 等项目编排层。

切换第三方库时，优先修改 Framework 内的适配实现；Core/General/Project 的业务调用面保持不变。

## Cysharp 工具使用约定

项目已引入 ZLogger / ZString / ZLinq。后续搭建 Framework、Core、General、Project 模块时，应主动利用这些库降低 GC 和运行时反射/格式化开销。

- **Framework.Log**：日志稳定接口位于 `Assets/Framework/Log/`，包含 `GameLog`、`GameLogProfile`、`GameLogConfig`、`GameLogSwitches`、`IGameLogSink` 等。Framework.Log 不引用 Core、VContainer、ZLogger、Microsoft.Extensions.Logging 或 UnityEngine（`Log.asmdef noEngineReferences=true`）。
- **ZLogger**：日志后端采用 `Microsoft.Extensions.Logging` + ZLogger Unity provider。Core 注册 `AddZLoggerUnityDebug(options => options.PrettyStacktrace = true)`，日志输出到 Unity Console 并保留良好的堆栈/跳转体验。业务代码不直接使用 `Debug.Log/LogWarning/LogError`。
- **AI Runtime Logging**：运行日志必须可以落盘为 AI 可读取的 session 产物。规范格式是 JSON Lines + session 清单；Editor/dev/QA 环境默认保留文件日志，Player smoke、资源矩阵和热更 smoke 的验证报告应引用日志文件，而不是依赖用户截图 Console。实现边界见 `.planning/AI_RUNTIME_LOGGING.md`。
- **环境与模块开关**：打包脚本通过 `KJ_LOG_TRACE` / `KJ_LOG_DEBUG` / `KJ_LOG_INFORMATION` / `KJ_LOG_WARNING` / `KJ_LOG_ERROR` / `KJ_LOG_CRITICAL` 控制编译期裁剪，符号常量统一从 `Framework.Log.GameLogSymbols` 引用；多个 `[Conditional]` 是 OR 关系，运行时再由 `GameLogProfile` 做模块/级别过滤。Editor 面板或打包脚本通过 `GameLogSwitches.Configure(GameLogConfig)`、`GameLog.ApplyEnvironment(...)`、`GameLog.SetModuleMinimumLevel(...)`、`GameLog.SetModuleEnabled(...)` 控制模块过滤。
- **VContainer 集成**：日志工厂、`ILogger<T>` 注册、日志等级和输出 provider 由 Core 层接入 VContainer。`Core.Logging.GameLogBridge` 只实现 `IGameLogSink`，把 Framework.Log 日志条目桥接到 ZLogger；它不是 `[CoreSystem]`。
- **Source Generator 前提**：当前 Unity 2022.3.62f2 高于 2022.3.12f1，可使用 Incremental Source Generator；需要使用 ZLoggerMessage / ZLinq DropIn Generator 等预览语法时，确保项目编译参数启用 `-langVersion:preview`。
- **ZString**：热路径字符串拼接、格式化、日志 message 构建、资源路径组合优先使用 ZString 或 Utf8StringInterpolation，避免 `string.Format`、频繁插值和临时 `StringBuilder` 分配。
- **ZLinq**：集合查询优先使用 `AsValueEnumerable()` 和 ZLinq 操作符；在系统初始化、资源扫描、事件类型扫描、UI 列表构建等路径避免普通 LINQ 产生枚举器/闭包分配。暂不默认启用全局 DropIn Generator，除非单独评估 asmdef 范围和兼容性。
- **Pool / Cache**：临时集合、短生命周期对象、GameObject 实例、LRU/FIFO 数据缓存优先使用已建 `Framework.Pool` / `Framework.Cache`。ZString/ZLinq 负责减少格式化和查询分配，Pool/Cache 负责减少对象生命周期 churn，两者应组合使用。

推荐日志扩展示例（稳定高频日志）：

```csharp
using Microsoft.Extensions.Logging;
using ZLogger;
using Framework.Log;

public static partial class AssetLogExtensions
{
    [System.Diagnostics.Conditional(GameLogSymbols.UnityEditor)]
    [System.Diagnostics.Conditional(GameLogSymbols.DevelopmentBuild)]
    [System.Diagnostics.Conditional(GameLogSymbols.Debug)]
    [ZLoggerMessage(LogLevel.Debug, "Asset loaded. Path: {path}")]
    public static partial void AssetLoaded(this ILogger<AssetSystem> logger, string path);
}
```

普通 Framework/Boot 日志示例：

```csharp
GameLog.Info("Asset runtime initialized", "Framework.Asset");
GameLog.Error("Asset load failed", "Framework.Asset");
```

测试用例放在 `Assets/Tests/EditMode/` 和 `Assets/Tests/PlayMode/`；第三方测试框架通过 Packages 引入。
`Framework/TestKit/` 只提供可复用测试支撑，不自建测试运行器或断言框架。当前 `RecordingAssetSystem` 用于记录和返回已注册资源，覆盖 `LoadAssetAsync<T>` / `Release` 这类上层常用调用；句柄、场景和下载器仍由真实资源系统或后续专门 adapter 覆盖，避免把 YooAsset 后端细节泄漏进测试 fake。

## 命名规范

- **Core 层**：`System`（如 ResourceSystem、UISystem）
  - 接口：`ISystem` / `ITickableSystem` / `IAsyncSystem`
  - 标记：`[CoreSystem]`
  - 管理器：`SystemManager`
- **业务层（General/Project）**：`Model`（MVVM 规范，如 TaskModel、ShopModel）
  - 标记：`[Model]`
  - 业务功能按需实现接口来区分职责（如 ILoginHandler 处理登录数据）
  - 不使用 Module / System 作为业务功能命名

## 事件规范

- 事件使用统一的 `Framework.Event.GameEventAttribute` 标记。
- 当前后端是 MessagePipe 强类型事件，由 Core/General 注册阶段扫描并注册 broker。
- 业务代码直接依赖 `IPublisher<TEvent>` / `ISubscriber<TEvent>`。
- 不再使用 `EventId + object payload` 的统一事件总线。
