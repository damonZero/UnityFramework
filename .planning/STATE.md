# Project State: KJ Unity Framework

**Last Updated:** 2026-07-05
**Current Status:** ✅ Phase 0 完成，🔄 Phase 1 稳定性验证中（HybridCLR 边界、最小加载闭环、编辑器同步工具与 AI_RUNTIME_LOGGING 已落地；Editor Play 启动链已通过；下一步先做 Player 打包 smoke 与资源加载矩阵验证，确认底层框架稳定后再进入 UI/Login 等新模块）

## 进度

- [x] 需求讨论完成
- [x] 命名规范确定（Core=System，业务=Model）
- [x] FOUND-01: Boot Layer (Entry + serialized startup settings + minimal update UI hook)
- [x] FOUND-02: ISystem 接口 (ISystem + ITickableSystem)
- [x] FOUND-03: SystemManager
- [x] DI-01: VContainer 接入方案（Core 注册入口 + MessagePipe）
- [x] DI-02: Boot 层最小依赖约束落地（Boot 不引用 Core/General/Project）
- [x] DI-03: Core System 迁移到容器驱动注册（`[CoreSystem]` + 反射扫描 `AsSelf().AsImplementedInterfaces()`）
- [x] EVT-01: 删除旧 `EventId + object payload` 事件总线，统一 `Framework.Event.GameEventAttribute`
- [x] BOOT-CHAIN-01: prefab 字符串链式启动协议（历史方案；当前判断 prefab 仅作为 MonoBehaviour/SerializeField 载体，不再作为后续目标）
- [x] RES-01: `Framework.Asset` — 基于 YooAsset 3.0 的底层资源适配（owned/cached 双通道、SemaphoreSlim 并发保护、AssetCacheKey 类型感知缓存）
- [x] RES-02: AssetHandle<T> / AssetInstanceHandle / AssetSceneHandle / AssetDownloadHandle / IAssetSystem — Framework 统一 API + 实例生命周期管理 + 场景串行化加载卸载
- [x] RES-03: `Core.AssetSystem` 薄编排 — 复用 Boot 已初始化的资源运行时，负责 Shutdown 和 AssetSystemReadyEvent 发布
- [x] POOL-01: PoolService.cs — Framework/Pool + Framework/Cache + Framework.Asset 的 DI 桥接
- [x] TEST-01: `Framework.TestKit` — 基于 Unity Test Framework / NUnit 的通用断言、Fake、Probe、Fixture、手动时间驱动
- [x] BOOT-CHAIN-02: Boot 收敛为更新壳（Entry 序列化启动配置；Boot 仅更新资源/代码并反射调用热更层正式入口；VContainer root 由 ProjectStartup 创建）
- [x] MODEL-01: General/Project Model 生命周期接入（`ModelLifecycle` 由 VContainer `IPostStartable` 在 Core 系统 Start 后驱动 `LoadAll()`）
- [x] LOG-01: `Framework.Log` 稳定日志门面 + 环境/模块开关接口 + Core ZLogger Unity Console provider 桥接（含现有 ZLoggerMessage 调用点裁剪）
- [x] LOG-AI-00: AI 运行日志规范落地（`.planning/AI_RUNTIME_LOGGING.md`：JSONL + session 清单、Boot/Core 职责、AI 分析流程、验收标准）
- [x] LOG-AI-01: AI 运行日志落盘与会话清单（`Framework.RuntimeLog` 纯 C# session writer + Boot 早期安装 + Core `GameLog`/`ILogger<T>`/ZLogger 接入）
- [x] LOG-AI-02: 首版日志收集与 AI 分析入口（`KJ/Runtime Logs/*` Editor 菜单：打开 latest、生成摘要、导出诊断包、清理本地日志）
- [x] HYB-00: HybridCLR 热更边界固化（托管 DLL 下发 / 需重启生效 / 真正换包规则）
- [x] HYB-01: HybridCLR 最小加载闭环代码落地（Boot 加载 AOT metadata + Core/General/Project DLL 后反射调用 ProjectStartup；Unity Editor/Player 验证待 HYB-02 工具链）
- [x] HYB-02A: 热更构建同步工具（`KJ/HybridCLR/*` Editor 菜单：生成/编译 HybridCLR 产物，同步 `.dll.bytes` RawFile，维护 YooAsset collector，回写打开的 Entry 序列化配置；日常 smoke 与完整构建前生成已拆分；工具归属 `Assets/Scripts/Boot.Editor/HybridCLR/`）
- [ ] UI-01: UISystem（UI 管理）
- [ ] UI-02: UIWindow 基类

## 文件清单

```
Assets/Scripts/
├── Boot/
│   ├── KJ.Boot.asmdef           ← 仅引用 Framework.Asset，不引用 VContainer/Log/Core/General/Project
│   ├── Entry.cs                 ← 稳定启动入口 MonoBehaviour；持有序列化启动配置和可选启动 UI
│   ├── BootStartupSettings.cs   ← Entry 序列化配置：资源更新、热更 DLL/AOT metadata 资源路径、正式启动入口
│   ├── BootRuntimeLogBootstrap.cs ← Boot 早期 RuntimeLog session 安装；Core/ZLogger 尚未接管前也能落盘
│   ├── BootUpdateRunner.cs      ← Boot 更新流程：资源版本检查、清单更新、下载、读取 RawFile/本地兜底 bytes、AOT metadata、Assembly.Load、反射启动，并移交 AssetRuntime
│   ├── BootAssemblyEntry.cs / BootMetadataEntry.cs
│   ├── HybridClrReflection.cs   ← 反射调用 HybridCLR.RuntimeApi，Boot asmdef 不直接引用 HybridCLR.Runtime
│   └── IBootStartupView.cs      ← 启动更新 UI 最小接口（状态/进度/修复可见）
├── Boot.Editor/
│   ├── Boot.Editor.asmdef       ← Editor-only，引用 Boot + Framework.Asset.Editor + HybridCLR.Editor + YooAsset.Editor
│   ├── Build/
│   │   └── PlayerBuildPrivatePathValidator.cs ← Player Build 前拦截 Build Settings/Resources/StreamingAssets 中的 `_` 前缀路径
│   └── HybridCLR/
│       └── KJHybridClrBuildTools.cs ← `KJ/HybridCLR` 菜单：Prepare Runtime Assets And Boot / Generate Runtime Assets And Sync / Generate All And Sync / Compile Dlls And Sync / Apply To Open Entry / Validate Outputs
├── Core/
│   ├── KJ.Core.asmdef          ← 引用 Asset + Event + Pool + Cache + Log + VContainer + MessagePipe + UniTask
│   ├── PoolService.cs          ← [CoreSystem] Framework.Pool DI 桥接 + 集合池快捷入口
│   ├── Logging/
│   │   ├── GameLogBridge.cs    ← IGameLogSink 到 RuntimeLog + Core ZLogger 管线的桥接（adapter，不是 CoreSystem）
│   │   ├── RuntimeLogBootstrap.cs       ← Core 侧 RuntimeLog session 创建/补全：Unity 信息、资源信息、路径、frame provider
│   │   └── RuntimeLogLoggerProvider.cs  ← Microsoft.Extensions.Logging provider，把 ILogger<T>/ZLoggerMessage 写入同一 JSONL session
│   ├── Bootstrap/
│   │   ├── CoreStartupContext.cs
│   │   ├── CoreTypeRegistration.cs
│   │   ├── CoreContainerRegistration.cs
│   │   └── CoreBootstrapStage.cs
│   ├── Systems/
│   │   ├── ISystem.cs              ← ISystem + ITickableSystem 接口
│   │   ├── SystemManager.cs        ← 系统生命周期管理器（VContainer 驱动）
│   │   ├── SystemManagerLog.cs     ← SystemManager ZLogger 源生成日志
│   │   ├── StartupProbeSystem.cs   ← 启动链路验证系统
│   │   ├── StartupProbeSystemLog.cs← StartupProbeSystem ZLogger 源生成日志
│   │   ├── Attributes/CoreSystemAttribute.cs
│   │   ├── Events/AppStartedEvent.cs
│   │   ├── Events/AppShuttingDownEvent.cs
│   │   └── ICoreStartupStatus.cs ← Core 启动结果，供业务层决定是否加载
│   └── Asset/
│       ├── AssetSystem.cs              ← [CoreSystem] Framework.Asset 生命周期编排 + Ready 事件
│       ├── AssetSystemLog.cs           ← AssetSystem ZLogger 源生成日志
│       └── AssetSystemReadyEvent.cs    ← [GameEvent] 就绪通知
├── Core.Editor/
│   ├── Core.Editor.asmdef       ← Editor-only，引用 Core + Log + RuntimeLog
│   └── Logging/
│       └── RuntimeLogEditorTools.cs ← `KJ/Runtime Logs` 菜单：打开 latest、生成摘要、导出诊断包、清理日志
├── General/
│   ├── KJ.General.asmdef       ← 引用 Core + Event + Log
│   ├── Bootstrap/
│   │   ├── GeneralContainerRegistration.cs
│   │   └── GeneralBootstrapStage.cs
│   └── Models/
│       ├── IModel.cs
│       ├── ModelAttribute.cs
│       ├── ModelLifecycleLog.cs
│       └── ModelLifecycle.cs
└── Project/
    ├── KJ.Project.asmdef       ← 引用 Core + General
    └── Bootstrap/
        ├── ProjectStartup.cs        ← Boot 反射调用的正式热更入口，接收 IAssetRuntime
        ├── ProjectLifetimeScope.cs  ← VContainer root，串联 Core→General→Project 注册，并复用 Boot AssetRuntime
        ├── ProjectBootstrapper.cs   ← 静态 Project 注册入口
        └── ProjectBootstrapStage.cs

Assets/Editor/
└── .gitkeep                     ← 仅保留跨层 Editor 工具占位；模块工具放各自 `*.Editor/`

Assets/GameRes/
└── HotUpdate/
    ├── Dlls/                    ← Core/General/Project `.dll.bytes` RawFile 输出目录
    └── AotMetadata/             ← mscorlib/System/System.Core `.dll.bytes` RawFile 输出目录

Assets/Framework/
├── Pool/
│   ├── Pool.asmdef             ← 引用 UniTask + Cache
│   ├── IPool.cs                ← IPool<T> / IPoolLease<T> / IPoolable
│   ├── ObjectPool.cs            ← 泛型对象池 (lock 并发安全)
│   ├── CollectionPool.cs       ← 集合池静态入口 (List/HashSet/Queue/Stack/Dictionary)
│   ├── PooledCollections.cs    ← RAII using 包装 (PooledList<T> 等)
│   ├── PoolDependencies.cs     ← 静态委托注入 (LoadAssetAsync / ReleaseAsset)
│   ├── PoolLease.cs            ← PoolLease<T> : IPoolLease<T>
│   ├── PoolStatistics.cs       ← 池统计 (IdleCount / RentCount 等)
│   ├── Types/TypePool.cs       ← ConcurrentDictionary 类型池注册表
│   ├── Unity/GameObjectPool.cs  ← GameObject 池 (双层缓存 LIFO + LRU / 污染检测)
│   └── Unity/PoolContainerMode.cs ← ChangeParent / MovePos
├── Cache/
│   ├── Cache.asmdef             ← 无外部依赖
│   ├── ICache.cs               ← ICache<TKey,TValue> / ICacheEvictionPolicy / ICacheResContainer
│   ├── Cache.cs                ← Cache<TKey,TValue> (lock 并发安全 + 可插拔策略)
│   ├── ResContainer/ResourceCache.cs ← 工厂模式资源容器
│   └── Strategy/LruCachePolicy.cs    ← O(1) LinkedList + Dictionary LRU
├── Asset/
│   ├── Asset.asmdef                  ← 引用 UniTask + YooAsset
│   ├── AssetConfig.cs                ← ScriptableObject: PlayMode + CDN URL
│   ├── AssetConstants.cs             ← Priority 常量
│   ├── AssetRuntime.cs               ← YooAsset 适配实现，运行时加载/释放/下载/RawFile bytes 读取
│   ├── AssetRuntimeFactory.cs        ← Boot 通过工厂获取 IAssetRuntime，避免直接触碰完整实现类型
│   ├── IAssetRuntime.cs              ← Boot/Core 共享的启动期资源运行时接口
│   ├── IAssetSystem.cs               ← 对上层暴露的稳定资源接口
│   ├── AssetHandle.cs                ← 对外句柄封装 (IDisposable)
│   ├── AssetInstanceHandle.cs        ← 实例化句柄
│   ├── AssetSceneHandle.cs           ← 场景句柄
│   └── AssetDownloadHandle.cs        ← 下载器句柄，不暴露 YooAsset 类型
├── Asset.Editor/
│   ├── Framework.Asset.Editor.asmdef ← Editor-only，引用 YooAsset.Editor
│   └── YooAsset/
│       └── KJAssetIgnoreRule.cs      ← YooAsset 收集忽略规则：`_` 前缀路径段不参与资源包
├── Event/
│   ├── Event.asmdef                  ← 无第三方依赖
│   ├── GameEventAttribute.cs         ← 统一事件标记
│   └── GameEventTypeScanner.cs       ← 事件类型扫描与 struct 校验
├── Log/
│   ├── Log.asmdef                    ← 无外部依赖，noEngineReferences=true
│   ├── GameLog.cs                    ← 稳定日志门面、模块树声明
│   ├── GameLogSymbols.cs             ← 编译期裁剪符号唯一常量入口
│   ├── GameLogProfile.cs             ← 环境默认级别 + 模块覆盖规则
│   ├── GameLogConfig.cs              ← 面板/打包脚本配置入口数据
│   ├── GameLogSwitches.cs            ← 应用配置到 GameLog 的统一入口
│   ├── GameLogEntry.cs               ← 日志条目
│   ├── IGameLogSink.cs               ← 输出端口，由 Core 桥接到 ZLogger
│   ├── GameLogEnvironment.cs         ← dev/formal/qa 等环境枚举
│   ├── GameLogModuleRule*.cs         ← 模块规则数据
│   └── GameLog*Attribute.cs          ← 未来 Editor 面板扫描用声明属性
├── RuntimeLog/
│   ├── RuntimeLog.asmdef             ← 只引用 Log，noEngineReferences=true
│   ├── RuntimeLogSession.cs          ← JSONL writer + session manifest + latest 指针 + flush/dispose
│   ├── RuntimeLogManager.cs          ← 当前 session 管理；避免覆盖 Core GameLogBridge
│   ├── RuntimeLogEntry.cs            ← AI 可读运行日志条目
│   ├── RuntimeLogSessionInfo.cs      ← session 清单数据：Unity/平台/资源包/热更程序集/AOT metadata
│   ├── RuntimeLogJson.cs             ← 无第三方依赖 JSON serializer
│   ├── RuntimeLogPhaseResolver.cs    ← Boot/HybridCLR/Core.Asset/Core.Init/ModelLifecycle 等 phase 归类
│   ├── RuntimeLogFileName.cs
│   └── RuntimeLogSessionId.cs
└── TestKit/
    ├── TestKit.asmdef                ← 引用 Unity Test Framework / NUnit，autoReferenced=false
    ├── Assertions/AssertEx.cs        ← NUnit 断言扩展
    ├── Fakes/RecordingAssetSystem.cs ← 可记录资源系统 fake
    ├── Fakes/RecordingRuntimeLogSink.cs ← 可记录 GameLog sink，测试启动缓冲和 RuntimeLog 接入顺序
    ├── Fixtures/TestGameObjectRoot.cs← 临时 GameObject 根节点
    ├── Probes/CallProbe.cs           ← 调用顺序记录
    ├── Probes/RecordingEventSink.cs  ← 事件记录
    └── Time/                         ← ManualClock / ManualTickDriver
```

## 外部依赖

| 包名 | 来源 | 版本 |
|------|------|------|
| VContainer | GitHub (hadashiA) | 1.1.0 |
| UniTask | GitHub (Cysharp) | 2.5.11 |
| MessagePipe | GitHub (Cysharp) | — |
| MessagePipe.VContainer | GitHub (Cysharp) | — |
| YooAsset | GitHub (tuyoogame) | 3.0 (UPM git, path=Assets/YooAsset) |
| ZLogger | GitHub UPM + NuGetForUnity (Cysharp) | 2.5.10 |
| ZLinq | GitHub UPM + NuGetForUnity (Cysharp) | 1.5.6 |
| ZString | GitHub UPM (Cysharp) | 2.6.0 |

## 编码约束补充

- ZLogger / ZString / ZLinq 已纳入技术栈；后续新模块默认考虑这些库，尤其是 Framework/Core 的热路径。
- 日志接口在 `Framework.Log`；AI runtime logging 文件能力在 `Framework.RuntimeLog`；Core 注册 `ILoggerFactory` / `ILogger<T>` / ZLogger Unity provider，并通过 `GameLogBridge : IGameLogSink` 同时桥接到 RuntimeLog 和 Console。
- 普通日志使用 `GameLog` 门面，业务层不直接 `Debug.Log`，也不散写 `[Conditional]`。
- 高频日志优先使用 ZLogger Source Generator：`static partial` 扩展方法 + `[ZLoggerMessage]`，并在 `XxxLog.cs` 日志声明方法上集中加对应 `[Conditional(GameLogSymbols...)]` 以支持 formal 包裁剪。
- dev/formal/qa 等打包环境通过 `KJ_LOG_*` 编译符号控制调用点保留，符号常量唯一入口是 `Framework.Log.GameLogSymbols`；多个 `[Conditional]` 是 OR 关系，模块开关通过 `GameLogConfig` / `GameLogSwitches` / `GameLogProfile` 控制。
- AI 运行日志规范见 `.planning/AI_RUNTIME_LOGGING.md`：后续 Editor/dev/QA 运行应生成 JSONL + session 清单；AI 调试、Player smoke、资源矩阵和热更验证优先读取日志文件分析，不默认要求用户截图 Console。
- `Logs/Runtime/` 是本地生成物，已被 `.gitignore` 的 `Logs/` 规则覆盖；日志不得记录 token、密码、实名账号、支付信息等敏感数据。
- ZString 用于热路径字符串构建；ZLinq 用于可读但低分配的集合查询，优先 `AsValueEnumerable()`，暂不默认开启全局 DropIn Generator。
- 已建 `Framework.Pool` / `Framework.Cache` 是后续模块的默认性能基础设施：临时集合用 `CollectionPool` / `PooledCollections`，短生命周期对象用 `ObjectPool<T>` / `TypePool`，GameObject 复用走 `GameObjectPool` + `PoolService`，有容量/淘汰需求的数据用 `Cache<TKey,TValue>` + 策略。
- 新模块不得重复实现私有对象池或缓存容器；确有特殊需求时，优先扩展 Framework.Pool / Framework.Cache 的稳定接口，再由 Core 层桥接。
- Unity 2022.3.62f2 满足 Source Generator 版本前提；使用需要预览语法的生成器能力前，确认项目已启用 `-langVersion:preview`。

## 下一步

Phase 1 剩余事项：
- **当前优先级：底层稳定性验证 gate**。暂不继续实现 UI/Login/Config 等新模块；先确认 Boot/HybridCLR/Asset/Core/Pool 在 Editor 与 Player 中稳定可用。
- HYB-02B: 在 Unity 中运行 `KJ/HybridCLR/Prepare Runtime Assets And Boot`，确认只同步 `Core/General/Project.dll.bytes` 与配置的 AOT metadata；正式 Player 打包前跑完整 `Generate All And Sync`。
- PKG-01: Player 打包 smoke test。构建并运行 Player，确认 Boot -> YooAsset init -> manifest/download -> HybridCLR metadata/DLL load -> ProjectStartup -> Core/SystemManager 全链路成功，关键日志包含 `[AssetSystem] Ready` 与 `[SystemManager] 全部初始化完成`，且无启动期 Error/Exception。
- RES-VERIFY-01: 资源加载矩阵验证。覆盖 RawFile bytes（DLL/AOT metadata）、`LoadAssetAsync<T>` cached 通道、`LoadAssetHandleAsync<T>` owned 通道 + Dispose、`InstantiateAsync` + Dispose、`LoadSceneAsync`/Unload、`CreateDownloader` 无下载/有下载路径、`Release`/`UnloadUnused` 行为。
- RES-VERIFY-02: PlayMode 覆盖。至少验证 EditorSimulate 与 Player Offline；Host/CDN 模式可用本地 HTTP 或后续测试服验证下载、重试、超时和缓存。
- HYB-VERIFY-01: 热更新行为 smoke。修改 Project 层代码/资源后重新同步，验证无需整包即可下发；已加载 DLL 替换需重启/下次启动生效；记录哪些场景需要 APP 重启、哪些只需游戏内流程刷新。
- HYB-03: 未来拆分 `Boot.Update`：当前工具默认只发布 `Core` / `General` / `Project` 运行时预加载 DLL；目标是极薄 BootLoader + 启动更新流程可热更，但更新后需重启 APP/下次启动生效
- LOGIN-01: General/Login 业务模型与登录流程骨架（Boot 更新完成后进入；不放 Core）
- MODEL-01 后续: 创建示例 Model 验证真实业务 Load/Unload 行为（生命周期驱动已接入）
- PERF-01: 已实现模块性能治理（ZLogger + VContainer 日志接入、启动期反射扫描去 LINQ、Bootstrap stage 收集优化、Unity 编译验证）
- UI-01: UISystem（UI 管理）
- UI-02: UIWindow 基类

Phase 2 规划：
- NET-01~05: 网络层
- UI-03~04: 窗口模式 + 窗口栈
- CFG-01~02: Luban 配置表集成

## 最新验证记录

- 2026-07-05: LOG-AI-01/02 已落地：新增 `Framework.RuntimeLog`、Boot 早期 session、Core `RuntimeLogLoggerProvider`/`GameLogBridge` 双输出、Core.Editor `KJ/Runtime Logs/*` 菜单，以及 `RuntimeLogTests` 覆盖 JSONL/latest/session、异常字段、最低级别、启动缓冲回放、Core bridge sink 不被覆盖、`ILogger<T>` provider。
- 2026-07-05: 新增 AI 运行日志规范 `.planning/AI_RUNTIME_LOGGING.md`，明确运行日志应落盘为 JSONL + session 清单，并作为后续 AI 调试、Player smoke、资源矩阵与热更验证的默认分析入口。
- 2026-07-05: Unity 编译由用户确认无报错。
- 2026-07-05: `KJHybridClrBuildTools` 已在 HybridCLR full generate 前写入 Boot 场景到 `ProjectSettings/EditorBuildSettings.asset`，修复 `GenerateStripedAOTDlls` 的 `Cannot build untitled scene` 失败。
- 2026-07-05: `Generate All Sync And Prepare Boot` 已进入 HybridCLR method bridge/AOT generic reference 生成阶段；该完整流程较重，日常 HYB-02B smoke 改用 `Prepare Runtime Assets And Boot`。
- 2026-07-05: 热更同步工具会校验最终 `Assets/GameRes/HotUpdate/Dlls` 只包含配置的运行时预加载程序集，默认是 `Core` / `General` / `Project`；`Boot`/`Framework` 若要走托管更新，需要独立启动更新清单、加载顺序和重启策略。
- 2026-07-05: 参考旧项目 `Boot.Entry -> Boot.Update` 模型后，热更边界已修正为“C# 层改动不等同于必须换包；已加载托管 DLL 的替换需重启/下次启动生效；真正换包仅限 native/player/HybridCLR 底层加载机制或旧包缺少加载能力”。
- 2026-07-05: Editor 工具目录已模块化：HybridCLR 工具从 `Assets/Editor/HybridCLR/` 迁到 `Assets/Scripts/Boot.Editor/HybridCLR/`；新增 `Assets/Framework/Asset.Editor/YooAsset/KJAssetIgnoreRule.cs`，YooAsset 收集阶段忽略 `_` 前缀路径；新增 `PlayerBuildPrivatePathValidator`，Player Build 前拦截 Build Settings/Resources/StreamingAssets 中的 `_` 前缀路径。
- 2026-07-05: YooAsset 3.0 `EditorSimulate` 初始化需要模拟构建输出 root，不是 `PackageName`；新增 `AssetConfig.EditorSimulatePackageRoot`、`IAssetRuntime.LastError`，并让 `KJ/HybridCLR/Prepare Runtime Assets And Boot` / `Prepare YooAsset Editor Simulate Package` 生成虚拟 RawFile 包后写回配置。Boot/Core 初始化失败会透出 YooAsset 原始错误。
- 2026-07-05: YooAsset 3.0 `InitializePackageOperation` 不支持 `WaitForCompletion()`；`AssetRuntime` 新增 `BeginInitialize()` / `AssetInitializeHandle`，Boot 在协程里轮询初始化，Core 只验证 Boot 传入的 runtime 已 ready。
- 2026-07-05: 用户确认 Unity 编译与 Editor Play 无报错；`Editor.log` 显示 `[AssetSystem] Ready`、`[SystemManager] Init [1/3] StartupProbeSystem`、`[2/3] AssetSystem`、`[3/3] PoolService`、`[SystemManager] 全部初始化完成`。下一步不新增业务模块，先执行 Player 打包 smoke 与各类资源加载验证。

---
*Phase 0 Architecture 完成: 2026-06-29 | Phase 1 资源系统 Framework 化: 2026-07-02 | 启动链去 prefab 化落地: 2026-07-04*
