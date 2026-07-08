---
gsd_state_version: 1.0
milestone: v4.10.1
milestone_name: milestone
status: unknown
last_updated: "2026-07-08T14:00:00.000Z"
---

# Project State: KJ Unity Framework

**Last Updated:** 2026-07-08
**Current Status:** ✅ Phase 0 完成，🔄 Phase 1 稳定性验证中（HybridCLR 边界【含 HYB-03 裂变】、最小加载闭环、编辑器同步工具、AI_RUNTIME_LOGGING、构建打包全流程管线+增量构建 已落地；Editor Play 启动链已通过；HYB-03 EditMode 测试 45/45 全绿；Player 全量构建已在 Gradle 编译阶段；下一步真机冒烟验证 + 资源加载矩阵验证后进入 UI/Login 等新模块）

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
- [x] HYB-03: HybridCLR 热更边界裂变（AOT `Launcher` 壳 + 热更 `Boot`；10 热更程序集；`AssetConfig`/`AssetConstants` 迁入 AOT 共享 `Framework.AssetShared`；`IRemoteService` 死锁修复 `BootRemoteService`；AOT 极简日志 `BootStartupLog`；反射入口 `"Boot.BootUpdateRunner, Boot"`；对应 EditMode 测试 45/45 全绿、含 15 例 HYB-03 边界用例）
- [x] PKG-00: 构建打包全流程管线设计（S0–S9 全阶段编排，见 `ProgressDoc/Discuss/Hy3_构建打包全流程管线_需求分析与设计.md`）
- [x] PKG-01: KJBuildPipeline 编排器实现（`KJBuildPipeline.cs`：`Build()`/`BuildWithMask()`/`IncrementalBuild()`，KJ/Build/* 菜单）
- [x] PKG-02: YooAsset 生产构建 Stage（`StageBuildYooAsset.cs` → `ScriptableBuildPipeline` → StreamingAssets；与旧 `EditorSimulateBuildInvoker` 不同 API）
- [x] PKG-03: AssetConfig.Mode 直接 YAML 写入 + 回滚（`StageApplyConfig.cs`：设 Offline 后保存三连，构建完成后 `RollbackAssetConfig()` 恢复 Editor 状态）
- [x] PKG-04: BuildPlayer + Gradle 编译（`StageBuildPlayer.cs`：IL2CPP Android）
- [x] PKG-05: 增量构建 + 变更检测（`StageDependencyTracker.cs`：监视源码/资源/配置变更，级联 S1→S2→S3→S4→S6，S5→S6 独立）
- [x] PKG-06: 构建管理面板（`BuildStagePanel.cs`：EditorWindow 自动检测 + 手动勾选 Stage + 增量/全量按钮）
- [x] PKG-07: APK 冒烟验证（`StageSmokeRun.cs`：ADB 安装 + logcat 监听 + `latest.jsonl` 启动链验证）
- [x] PKG-08: 产物校验（`StageValidateArtifacts.cs`：APK 存在 + StreamingAssets 内容完整性）
- [x] DOC-01: 构建流程文档对齐（`ProgressDoc/Result/hybridclr_workflow.md` §4、`AGENTS.md` 构建管线章节）
- [x] BUGFIX-01: YooAsset EditorFileSystem 错误（APK 中 `AssetConfig.Mode` 未正确序列化为 Offline → 改用 YAML 直写）
- [x] BUGFIX-02: BuiltinFileSystem URI 错误（`BootLoader` 传入 `packageName` 作为 `packageRoot` → 改用无参重载取 `GetDefaultBuiltinPackageRoot()`）
- [ ] UI-01: UISystem（UI 管理）
- [ ] UI-02: UIWindow 基类

## 文件清单

```
Assets/Scripts/
├── Boot/
│   ├── KJ.Boot.asmdef           ← 热更程序集（HYB-03 裂变后）。引用 Asset/Log/RuntimeLog/UniTask/AssetShared/YooAsset/Launcher；不引用 VContainer/HybridCLR.Runtime/Core/General/Pool/Cache/Event/MessagePipe
│   ├── BootUpdateRunner.cs      ← 热更入口：被 Launcher 反射启动；资源版本检查/清单更新/下载/AOT metadata/Assembly.Load，反射 Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)，并回放早期日志
│   ├── BootRuntimeLogBootstrap.cs ← 热更层早期 RuntimeLog session 安装；Core/ZLogger 尚未接管前也能落盘
│   └── Launcher/                      → KJ.Launcher.asmdef (AOT Shell，HYB-03 裂变新增)
│       ├── KJ.Launcher.asmdef         ← 仅引用 UniTask/YooAsset/HybridCLR.Runtime/AssetShared；硬约束：不引用任何 Framework/热更程序集
│       ├── Entry.cs              ← AOT 入口 MonoBehaviour：Awake → DontDestroyOnLoad → new BootLoader().RunAsync()
│       ├── BootLoader.cs         ← AOT 壳：初始化 YooAsset、加载全部热更 DLL、构造 BootBridge、反射 BootUpdateRunner
│       ├── BootBridge.cs         ← 跨 AOT→热更边界的状态载体（Package/Settings/View/Config/EarlyLogs）
│       ├── BootStartupLog.cs     ← AOT 阶段日志（纯文本 + 内存快照，不依赖 Framework.Log/RuntimeLog）
│       ├── IsExternalInit.cs
│       ├── Data/
│       │   ├── BootStartupSettings.cs ← Entry 序列化配置：资源更新、热更 DLL/AOT metadata、正式入口
│       │   ├── BootAssemblyEntry.cs   ← 热更 DLL 条目
│       │   ├── BootMetadataEntry.cs   ← AOT metadata 条目
│       │   └── IBootStartupView.cs    ← 启动更新 UI 最小接口（状态/进度/修复可见）
│       └── YooAssetStrategy/
│           └── BootRemoteService.cs   ← AOT 侧 IRemoteService（死锁修复点）

├── Boot.Editor/
│   ├── Boot.Editor.asmdef       ← Editor-only，引用 Boot + Framework.Asset.Editor + HybridCLR.Editor + YooAsset.Editor
│   ├── Build/
│   │   ├── BuildConfig.cs           ← 可序列化构建配置（Platform/BuildType/输出路径）
│   │   ├── BuildStagePanel.cs       ← EditorWindow 构建管理面板（自动检测+手动勾选+增量/全量按钮）
│   │   ├── KJBuildPipeline.cs       ← S0–S9 全阶段编排器（Build/BuildWithMask/IncrementalBuild）
│   │   ├── StageApplyConfig.cs      ← S5: AssetConfig.Mode YAML 直写 Offline + 回滚
│   │   ├── StageBuildPlayer.cs      ← S6: BuildPipeline.BuildPlayer IL2CPP Android
│   │   ├── StageBuildYooAsset.cs    ← S4: YooAsset ScriptableBuildPipeline 生产构建 → StreamingAssets
│   │   ├── StageDependencyTracker.cs← 变更检测引擎（监视源码/资源/配置）
│   │   ├── StageSmokeRun.cs         ← S8: ADB 安装 + logcat + latest.jsonl 验证
│   │   ├── StageValidateArtifacts.cs← S7: APK + StreamingAssets 内容校验
│   │   └── PlayerBuildPrivatePathValidator.cs ← Player Build 前拦截 `_` 前缀路径
│   └── HybridCLR/
│       └── KJHybridClrBuildTools.cs ← `KJ/HybridCLR` 菜单：Prepare Runtime Assets And Boot / Generate Runtime Assets And Sync / Generate All And Sync / Compile Dlls And Sync / Apply To Open Entry / Validate Outputs
├── Core/
│   ├── KJ.Core.asmdef          ← 引用 Asset+Event+Pool+Cache+Log+RuntimeLog+VContainer+MessagePipe(.VContainer)+UniTask+ZLinq+ZLogger.Unity+AssetShared；不引用 General/Project
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
│   ├── KJ.General.asmdef       ← 引用 Core+Event+Log+MessagePipe(.VContainer)+VContainer(.Unity)+ZLinq；不引用 Project
│   ├── Bootstrap/
│   │   ├── GeneralContainerRegistration.cs
│   │   └── GeneralBootstrapStage.cs
│   └── Models/
│       ├── IModel.cs
│       ├── ModelAttribute.cs
│       ├── ModelLifecycleLog.cs
│       └── ModelLifecycle.cs
└── Project/
    ├── KJ.Project.asmdef       ← 引用 Asset+Core+General+Event+Log+MessagePipe(.VContainer)+VContainer(.Unity)；可引用所有下层
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
│   ├── InstanceRecyclePolicy.cs ← IInstanceRecyclePolicy / CapacityInstancePolicy / PersistentInstancePolicy
│   ├── Types/TypePool.cs       ← ConcurrentDictionary 类型池注册表
│   ├── Unity/GameObjectPool.cs  ← GameObject 池（PrefabPoolState 合并状态 + 反向索引防双回收/污染；prefab 缓存走 BoundedStore+LruPolicy；IInstanceRecyclePolicy 可插拔库存；[MainThread] 断言）
│   └── Unity/PoolContainerMode.cs ← ChangeParent / MovePos
├── Cache/
│   ├── Cache.asmdef             ← 无外部依赖
│   ├── ICache.cs               ← ICache<TKey,TValue> 接口（由 BoundedStore 实现）
│   ├── IStoreEvictionPolicy.cs ← 淘汰策略契约（原 ICacheEvictionPolicy，方法改 OnAdded/OnAccessed/OnRemoved）
│   ├── BoundedStore.cs         ← 当前缓存容器（实现 ICache；single-flight GetOrAdd；Put 覆盖走 Remove+Add；onEvicted 锁外）
│   ├── Cache.cs                ← [Obsolete] 转发壳，内部委托 BoundedStore（新代码直接用 BoundedStore）
│   ├── ResContainer/ICacheResContainer.cs ← 资源容器接口
│   ├── ResContainer/ResourceCache.cs     ← 工厂模式资源容器
│   └── Strategy/
│       ├── LruPolicy.cs        ← O(1) LinkedList + Dictionary LRU（原 LruCachePolicy）
│       ├── TtlPolicy.cs        ← 按 TTL 过期淘汰
│       ├── CapacityPolicy.cs   ← FIFO 容量淘汰
│       └── CompositePolicy.cs  ← 多策略扇出组合
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
├── AssetShared/              ← HYB-03 新增：AOT 与热更共享的资产配置
│   ├── AssetShared.asmdef        ← 零外部依赖，AOT Launcher 与热更 Boot 均可引用
│   ├── AssetConfig.cs            ← ScriptableObject: PlayMode + CDN URL 等（从 Asset/ 迁入）
│   └── AssetConstants.cs         ← Priority 常量（从 Asset/ 迁入）
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
- 已建 `Framework.Pool` / `Framework.Cache` 是后续模块的默认性能基础设施：临时集合用 `CollectionPool` / `PooledCollections`，短生命周期对象用 `ObjectPool<T>` / `TypePool`，GameObject 复用走 `GameObjectPool` + `PoolService`，有容量/淘汰需求的数据用 `BoundedStore<TKey,TValue>` + `IStoreEvictionPolicy` 策略（旧 `Cache<TKey,TValue>` 已 `[Obsolete]`，新代码直接用 `BoundedStore`）。
- 新模块不得重复实现私有对象池或缓存容器；确有特殊需求时，优先扩展 Framework.Pool / Framework.Cache 的稳定接口，再由 Core 层桥接。
- Unity 2022.3.62f2 满足 Source Generator 版本前提；使用需要预览语法的生成器能力前，确认项目已启用 `-langVersion:preview`。

## 下一步

Phase 1 剩余事项：

- **当前优先级：真机冒烟验证 gate**。构建管线已实现，下一步是端到端验证 APK 启动链路，确认 HYB-03 Launcher→Boot 全链路在真机通过。
- PKG-VERIFY: Player 冒烟 test。`KJ/Build/Full Player Build & Validate` 或 `Incremental Player Build` 构建后运行 APK，确认 Launcher→YooAsset init→manifest/download→HybridCLR metadata/DLL load→BootUpdateRunner→ProjectStartup→Core/SystemManager 全链路成功，`latest.jsonl` 包含 `[AssetSystem] Ready` 与 `[SystemManager] 全部初始化完成`，无启动期 Error/Exception。
- RES-VERIFY-01: 资源加载矩阵验证。覆盖 RawFile bytes、cached/owned 通道、InstantiateAsync/Dispose、LoadSceneAsync/Unload、CreateDownloader、Release/UnloadUnused。
- RES-VERIFY-02: PlayMode 覆盖（EditorSimulate + Player Offline）。
- HYB-VERIFY-01: 热更新行为 smoke（修改 Project 代码后验证无整包更新）。
- LOGIN-01: General/Login 业务模型（Boot 更新完成后进入；不放 Core）

Phase 2 规划：

- NET-01~05: 网络层
- UI-03~04: 窗口模式 + 窗口栈
- CFG-01~02: Luban 配置表集成

## 最新验证记录

- 2026-07-08: 构建打包全流程管线 PKG-00~08 落地：`KJBuildPipeline.cs` S0–S9 全阶段编排、`StageBuildYooAsset` 生产构建（`ScriptableBuildPipeline`，非旧 `EditorSimulateBuildInvoker`）、`StageApplyConfig` YAML 直写 `AssetConfig.Mode=Offline`+回滚、`StageBuildPlayer` IL2CPP Android、`StageValidateArtifacts` 产物校验、`StageSmokeRun` ADB 冒烟验证、`StageDependencyTracker` 变更检测级联、`BuildStagePanel` 可视化管理面板、`BuildConfig` 构建配置。
- 2026-07-08: BUGFIX-01 — 修复 YooAsset APK EditorFileSystem 错误：`StageApplyConfig` 从 ScriptableObject API（SetDirty+SaveAssets）改为 YAML 正则直写 `Mode: 0→Mode: 1` + `ImportAsset(ForceSynchronousImport)`，消除 `AssetDatabase.Refresh` 竞态。
- 2026-07-08: BUGFIX-02 — 修复 BuiltinFileSystem URI 格式错误：`BootLoader` 不再传 `packageName` 给 `CreateDefaultBuiltinFileSystemParameters`，改用无参重载取 `GetDefaultBuiltinPackageRoot()` 自动计算 `jar:file://` 路径。
- 2026-07-08: DOC-01 — 文档对齐：更新 `CODEMAP.md`（HYB-03 裂变后文件路径/启动流/依赖矩阵）、`AGENTS.md`（新增构建管线章节）、`STATE.md`（新增 PKG-* 和 BUGFIX-* 项）、`hybridclr_workflow.md` §4（构建管线入口）。
- 2026-07-08: 全量构建已在 Gradle 编译阶段（S1 MethodBridge 迭代 10/10）；`maxMethodBridgeGenericIteration` 保持 10 层（参考旧项目 `KJBuildSettings.cs`）。
- 2026-07-07: HYB-03 热更边界裂变已实现并验证。EditMode 全工程 **45/45 全绿**。
- 2026-07-05: 用户确认 Unity 编译与 Editor Play 无报错。

---
*Phase 0 Architecture 完成: 2026-06-29 | Phase 1 资源系统 Framework 化: 2026-07-02 | 启动链去 prefab 化落地: 2026-07-04 | 构建打包管线落地: 2026-07-08*
