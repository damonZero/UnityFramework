# Project State: KJ Unity Framework

**Last Updated:** 2026-07-04
**Current Status:** ✅ Phase 0 完成，🔄 Phase 1 进行中（资源系统和无 prefab 启动链已完成；下一步进入 UI/业务示例验证）

## 进度

- [x] 需求讨论完成
- [x] 命名规范确定（Core=System，业务=Model）
- [x] FOUND-01: Boot Layer (Entry + AppLifetimeScope)
- [x] FOUND-02: ISystem 接口 (ISystem + ITickableSystem)
- [x] FOUND-03: SystemManager
- [x] DI-01: VContainer 接入方案（Core 注册入口 + MessagePipe）
- [x] DI-02: Boot 层最小依赖约束落地（Boot 不引用 Core/General/Project）
- [x] DI-03: Core System 迁移到容器驱动注册（`[CoreSystem]` + 反射扫描 `AsSelf().AsImplementedInterfaces()`）
- [x] EVT-01: 删除旧 `EventId + object payload` 事件总线，统一 `Framework.Event.GameEventAttribute`
- [x] BOOT-CHAIN-01: prefab 字符串链式启动协议（历史方案；当前判断 prefab 仅作为 MonoBehaviour/SerializeField 载体，不再作为后续目标）
- [x] RES-01: `Framework.Asset` — 基于 YooAsset 3.0 的底层资源适配（owned/cached 双通道、SemaphoreSlim 并发保护、AssetCacheKey 类型感知缓存）
- [x] RES-02: AssetHandle<T> / AssetInstanceHandle / AssetSceneHandle / AssetDownloadHandle / IAssetSystem — Framework 统一 API + 实例生命周期管理 + 场景串行化加载卸载
- [x] RES-03: `Core.AssetSystem` 薄编排 — 负责资源运行时初始化、Shutdown 和 AssetSystemReadyEvent 发布
- [x] POOL-01: PoolService.cs — Framework/Pool + Framework/Cache + Framework.Asset 的 DI 桥接
- [x] TEST-01: `Framework.TestKit` — 基于 Unity Test Framework / NUnit 的通用断言、Fake、Probe、Fixture、手动时间驱动
- [x] BOOT-CHAIN-02: 无 prefab 启动链迁移与验证（Boot 仍保持最小依赖，通过类型名/反射创建普通 C# `IBootstrapStage`，由 VContainer 接管系统生命周期）
- [x] MODEL-01: General/Project Model 生命周期接入（`ModelLifecycle` 由 VContainer `IPostStartable` 在 Core 系统 Start 后驱动 `LoadAll()`）
- [x] LOG-01: `Framework.Log` 稳定日志门面 + 环境/模块开关接口 + Core ZLogger Unity Console provider 桥接（含现有 ZLoggerMessage 调用点裁剪）
- [ ] UI-01: UISystem（UI 管理）
- [ ] UI-02: UIWindow 基类

## 文件清单

```
Assets/Scripts/
├── Boot/
│   ├── KJ.Boot.asmdef           ← 引用 VContainer + Framework.Log，不引用 Core/General/Project
│   ├── Entry.cs                 ← 稳定启动入口 MonoBehaviour
│   ├── AppLifetimeScope.cs      ← Boot 层 LifetimeScope 基类
│   └── Bootstrap/
│       ├── BootstrapContext.cs  ← 阶段上下文
│       ├── IBootstrapStage.cs   ← 阶段协议
│       └── BootLifetimeScope.cs ← 通过类型名/反射创建普通 C# Stage，按 Priority 执行
├── Core/
│   ├── KJ.Core.asmdef          ← 引用 Boot + Asset + Event + Pool + Cache + Log + VContainer + MessagePipe + UniTask
│   ├── PoolService.cs          ← [CoreSystem] Framework.Pool DI 桥接 + 集合池快捷入口
│   ├── Logging/
│   │   └── GameLogBridge.cs    ← IGameLogSink 到 Core ZLogger 管线的桥接（adapter，不是 CoreSystem）
│   ├── Bootstrap/
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
├── General/
│   ├── KJ.General.asmdef       ← 引用 Boot + Core + Event + Log
│   ├── Bootstrap/
│   │   ├── GeneralContainerRegistration.cs
│   │   └── GeneralBootstrapStage.cs
│   └── Models/
│       ├── IModel.cs
│       ├── ModelAttribute.cs
│       ├── ModelLifecycleLog.cs
│       └── ModelLifecycle.cs
└── Project/
    ├── KJ.Project.asmdef       ← 引用 Boot + General
    └── Bootstrap/
        ├── ProjectBootstrapper.cs   ← 静态 Project 注册入口
        └── ProjectBootstrapStage.cs

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
│   ├── AssetRuntime.cs               ← YooAsset 适配实现，运行时加载/释放/下载
│   ├── IAssetRuntime.cs              ← Core 生命周期编排接口
│   ├── IAssetSystem.cs               ← 对上层暴露的稳定资源接口
│   ├── AssetHandle.cs                ← 对外句柄封装 (IDisposable)
│   ├── AssetInstanceHandle.cs        ← 实例化句柄
│   ├── AssetSceneHandle.cs           ← 场景句柄
│   └── AssetDownloadHandle.cs        ← 下载器句柄，不暴露 YooAsset 类型
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
└── TestKit/
    ├── TestKit.asmdef                ← 引用 Unity Test Framework / NUnit，autoReferenced=false
    ├── Assertions/AssertEx.cs        ← NUnit 断言扩展
    ├── Fakes/RecordingAssetSystem.cs ← 可记录资源系统 fake
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
- 日志接口在 `Framework.Log`；Core 只注册 `ILoggerFactory` / `ILogger<T>` / ZLogger Unity provider，并通过 `GameLogBridge : IGameLogSink` 桥接。
- 普通日志使用 `GameLog` 门面，业务层不直接 `Debug.Log`，也不散写 `[Conditional]`。
- 高频日志优先使用 ZLogger Source Generator：`static partial` 扩展方法 + `[ZLoggerMessage]`，并在 `XxxLog.cs` 日志声明方法上集中加对应 `[Conditional(GameLogSymbols...)]` 以支持 formal 包裁剪。
- dev/formal/qa 等打包环境通过 `KJ_LOG_*` 编译符号控制调用点保留，符号常量唯一入口是 `Framework.Log.GameLogSymbols`；多个 `[Conditional]` 是 OR 关系，模块开关通过 `GameLogConfig` / `GameLogSwitches` / `GameLogProfile` 控制。
- ZString 用于热路径字符串构建；ZLinq 用于可读但低分配的集合查询，优先 `AsValueEnumerable()`，暂不默认开启全局 DropIn Generator。
- 已建 `Framework.Pool` / `Framework.Cache` 是后续模块的默认性能基础设施：临时集合用 `CollectionPool` / `PooledCollections`，短生命周期对象用 `ObjectPool<T>` / `TypePool`，GameObject 复用走 `GameObjectPool` + `PoolService`，有容量/淘汰需求的数据用 `Cache<TKey,TValue>` + 策略。
- 新模块不得重复实现私有对象池或缓存容器；确有特殊需求时，优先扩展 Framework.Pool / Framework.Cache 的稳定接口，再由 Core 层桥接。
- Unity 2022.3.62f2 满足 Source Generator 版本前提；使用需要预览语法的生成器能力前，确认项目已启用 `-langVersion:preview`。

## 下一步

Phase 1 剩余事项：
- MODEL-01 后续: 创建示例 Model 验证真实业务 Load/Unload 行为（生命周期驱动已接入）
- PERF-01: 已实现模块性能治理（ZLogger + VContainer 日志接入、启动期反射扫描去 LINQ、Bootstrap stage 收集优化、Unity 编译验证）
- UI-01: UISystem（UI 管理）
- UI-02: UIWindow 基类

Phase 2 规划：
- NET-01~05: 网络层
- UI-03~04: 窗口模式 + 窗口栈
- CFG-01~02: Luban 配置表集成

---
*Phase 0 Architecture 完成: 2026-06-29 | Phase 1 资源系统 Framework 化: 2026-07-02 | 启动链去 prefab 化落地: 2026-07-04*
