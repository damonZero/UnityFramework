# Project State: KJ Unity Framework

**Last Updated:** 2026-06-30
**Current Status:** ✅ Phase 0 完成，🔄 Phase 1 进行中（资源系统已完成）

## 进度

- [x] 需求讨论完成
- [x] 命名规范确定（Core=System，业务=Model）
- [x] FOUND-01: Boot Layer (Entry + AppLifetimeScope)
- [x] FOUND-02: ISystem 接口 (ISystem + ITickableSystem)
- [x] FOUND-03: SystemManager
- [x] DI-01: VContainer 接入方案（Core 注册入口 + MessagePipe）
- [x] DI-02: Boot 层最小依赖约束落地（Boot 不引用 Core/General/Project）
- [x] DI-03: Core System 迁移到容器驱动注册（`[CoreSystem]` + 反射扫描 `AsSelf().AsImplementedInterfaces()`）
- [x] EVT-01: 删除旧 `EventId + object payload` 事件总线
- [x] BOOT-CHAIN-01: prefab 字符串链式启动协议
- [x] RES-01: AssetSystem — 基于 YooAsset 3.0 的资源管理（owned/cached 双通道、SemaphoreSlim 并发保护、AssetCacheKey 类型感知缓存）
- [x] RES-02: AssetHandle<T> / AssetInstanceHandle / AssetSceneHandle / IAssetSystem — 句柄式 API + 实例生命周期管理 + 场景串行化加载卸载
- [ ] BOOT-CHAIN-02: Unity prefab 资源配置与场景切换验证
- [ ] MODEL-01: General/Project Model 生命周期验证
- [ ] UI-01: UISystem（UI 管理）
- [ ] UI-02: UIWindow 基类

## 文件清单

```
Assets/Scripts/
├── Boot/
│   ├── KJ.Boot.asmdef           ← 仅引用 VContainer
│   ├── Entry.cs                 ← 稳定启动入口 MonoBehaviour
│   ├── AppLifetimeScope.cs      ← Boot 层 LifetimeScope 基类
│   └── Bootstrap/
│       ├── BootstrapContext.cs  ← 阶段上下文
│       ├── IBootstrapStage.cs   ← 阶段协议
│       └── BootLifetimeScope.cs ← 通过 nextBootstrapPrefabPath 启动下一层 prefab
├── Core/
│   ├── KJ.Core.asmdef          ← 引用 Boot + Pool + Cache + VContainer + MessagePipe + MessagePipe.VContainer + UniTask + YooAsset
│   ├── ISystem.cs              ← ISystem + ITickableSystem 接口
│   ├── SystemManager.cs        ← 系统生命周期管理器（VContainer 驱动）
│   ├── StartupProbeSystem.cs   ← 启动链路验证系统
│   ├── Architecture/
│   │   ├── Attributes/CoreSystemAttribute.cs
│   │   ├── Events/GameEventAttribute.cs
│   │   ├── Events/AppStartedEvent.cs
│   │   ├── Events/AppShuttingDownEvent.cs
│   │   └── Bootstrap/ArchitectureContainerRegistration.cs
│   ├── Bootstrap/
│   │   ├── CoreContainerRegistration.cs
│   │   ├── CoreBootstrapStage.cs
│   │   └── CoreLifetimeScope.cs
│   └── Asset/
│       ├── AssetConfig.cs              ← ScriptableObject: PlayMode + CDN URL
│       ├── AssetConstants.cs           ← Priority 常量 (Init=-999, System=100)
│       ├── AssetInitSystem.cs          ← [CoreSystem] YooAsset 初始化 + IAssetPackageProvider
│       ├── AssetSystem.cs              ← [CoreSystem] 加载 API + owned/cached 双通道 + Downloader
│       ├── AssetSystemReadyEvent.cs    ← [GameEvent] 就绪通知
│       ├── AssetHandle.cs              ← 对外句柄封装 (IDisposable)
│       ├── AssetInstanceHandle.cs      ← 实例化句柄 (GameObject + Handle 联合生命周期)
│       ├── AssetSceneHandle.cs         ← 场景句柄 (+UnloadAsync 串行化)
│       ├── IAssetPackageProvider.cs    ← 内部桥接接口
│       └── IAssetSystem.cs             ← 对上层暴露的接口
├── General/
│   ├── KJ.General.asmdef       ← 引用 Boot + Core
│   ├── Bootstrap/
│   │   ├── GeneralContainerRegistration.cs
│   │   └── GeneralBootstrapStage.cs
│   ├── Events/GameEventAttribute.cs
│   └── Models/
│       ├── IModel.cs
│       ├── ModelAttribute.cs
│       └── ModelLifecycle.cs
└── Project/
    ├── KJ.Project.asmdef       ← 引用 Boot + General
    ├── ProjectBootstrapper.cs
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
└── Cache/
    ├── Cache.asmdef             ← 无外部依赖
    ├── ICache.cs               ← ICache<TKey,TValue> / ICacheEvictionPolicy / ICacheResContainer
    ├── Cache.cs                ← Cache<TKey,TValue> (lock 并发安全 + 可插拔策略)
    ├── ResContainer/ResourceCache.cs ← 工厂模式资源容器
    └── Strategy/LruCachePolicy.cs    ← O(1) LinkedList + Dictionary LRU
```

## 外部依赖

| 包名 | 来源 | 版本 |
|------|------|------|
| VContainer | GitHub (hadashiA) | 1.1.0 |
| UniTask | GitHub (Cysharp) | 2.5.11 |
| MessagePipe | GitHub (Cysharp) | — |
| MessagePipe.VContainer | GitHub (Cysharp) | — |
| YooAsset | GitHub (tuyoogame) | 3.0 (UPM git, path=Assets/YooAsset) |

## 下一步

Phase 1 剩余事项：
- BOOT-CHAIN-02: 创建/配置 Boot/Core/General/Project 启动 prefab
- MODEL-01: 创建示例 Model 并验证 ModelLifecycle Load/Unload
- UI-01: UISystem（UI 管理）
- UI-02: UIWindow 基类

Phase 2 规划：
- NET-01~05: 网络层
- RES-03: PoolService.cs（Framework/Pool + Framework/Cache 的 DI 桥接，Framework 代码已完成）
- UI-03~04: 窗口模式 + 窗口栈
- CFG-01~02: Luban 配置表集成

---
*Phase 0 Architecture 完成: 2026-06-29 | Phase 1 资源系统完成: 2026-06-30*
