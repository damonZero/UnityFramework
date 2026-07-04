# KJ Framework Codemap

**Generated:** 2026-07-01
**Unity:** 2022.3.62f2 LTS
**C# Language Version:** 9.0
**Target Platform:** Android (primary)

---

## Architecture Overview

KJ is a Unity client game framework implementing a strict 4-layer unidirectional dependency architecture. Each layer is enforced by assembly definition (`.asmdef`) files for compile-time isolation.

**Core pattern:** `ISystem` + `[CoreSystem]` attribute for Core-layer systems, `IModel` + `[Model]` attribute for business-layer models. Lifecycle is driven by VContainer DI.

**Bootstrap pattern:** Boot keeps minimal dependencies and creates ordinary C# `IBootstrapStage` instances from serialized type names. Stages are sorted by Priority and configure Core → General → Project without using prefab stage carriers.

---

## Layer Map

```
Packages (UPM third-party libraries)
    ^
    |
Framework/ (KJ-owned independent packages, no Scripts/ references)
    ^
    |
Boot ──▶ Core ──▶ General ──▶ Project
(Scripts/Boot) (Scripts/Core) (Scripts/General) (Scripts/Project)
```

**Dependency direction (enforced by .asmdef):**

| Layer | Can reference |
|-------|--------------|
| Boot | VContainer, Framework.Log |
| Core | Boot, Log, Pool, Cache, VContainer, MessagePipe, MessagePipe.VContainer, UniTask, YooAsset |
| General | Boot, Core, VContainer, VContainer.Unity, MessagePipe, MessagePipe.VContainer |
| Project | Boot, General, Log, VContainer, MessagePipe, MessagePipe.VContainer |
| Framework.Pool | UniTask, Cache |
| Framework.Cache | (none) |

---

## Dependency Matrix

```
                        Depends on
              Boot   Core   General   Project   Pool   Cache   VContainer   MessagePipe   UniTask   YooAsset
Boot           -      N       N         N        N      N         Y              N          N         N
Core           Y      -       N         N        Y      Y         Y              Y          Y         Y
General        Y      Y       -         N        N      N         Y              Y          N         N
Project        Y      N       Y         -        N      N         Y              Y          N         N
Pool           N      N       N         N        -      Y         N              N          Y         N
Cache          N      N       N         N        N      -         N              N          N         N
```

---

## File Index

### Layer: Boot (Assembly: `Boot`, Namespace: `Boot`)

Asmdef: `Assets/Scripts/Boot/KJ.Boot.asmdef`
References: VContainer, Log

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `Entry.cs` | `Assets/Scripts/Boot/Entry.cs` | `Entry : MonoBehaviour` | Game entry point. Sets DontDestroyOnLoad on itself. Minimal startup shell. | none |
| `AppLifetimeScope.cs` | `Assets/Scripts/Boot/AppLifetimeScope.cs` | `AppLifetimeScope : LifetimeScope` | Abstract base LifetimeScope for the app. Subclassed by BootLifetimeScope. | `VContainer.Unity` |
| `BootstrapContext.cs` | `Assets/Scripts/Boot/Bootstrap/BootstrapContext.cs` | `BootstrapContext` | Stage context holding `IContainerBuilder` and typed values in `Dictionary<Type, object>`. `ConfigureStages(IEnumerable<IBootstrapStage>)` filters nulls, sorts by Priority, rejects duplicate stage types, and calls `stage.Configure(this)`. | `VContainer`, `Log` |
| `IBootstrapStage.cs` | `Assets/Scripts/Boot/Bootstrap/IBootstrapStage.cs` | `IBootstrapStage` | Stage protocol: `int Priority`, `string StageName`, `void Configure(BootstrapContext context)` | none |
| `BootLifetimeScope.cs` | `Assets/Scripts/Boot/Bootstrap/BootLifetimeScope.cs` | `BootLifetimeScope : AppLifetimeScope` | The initial LifetimeScope. Reads `bootstrapStageTypeNames`, creates ordinary C# `IBootstrapStage` instances through reflection, and calls `context.ConfigureStages()`. | `VContainer`, `Log` |

### Layer: Core (Assembly: `Core`, Namespace: `Core`, `Core.Bootstrap`, `Core.Systems`, `Core.Asset`)

Asmdef: `Assets/Scripts/Core/KJ.Core.asmdef`
References: Boot, Pool, Cache, VContainer, MessagePipe, MessagePipe.VContainer, UniTask, YooAsset

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `ISystem.cs` | `Assets/Scripts/Core/Systems/ISystem.cs` | `ISystem` (interface), `ITickableSystem : ISystem` (interface) | `ISystem`: `int Priority`, `void Init()`, `void Shutdown()`. `ITickableSystem`: adds `Update(float)`, `LateUpdate(float)`, `FixedUpdate(float)`. | none |
| `SystemManager.cs` | `Assets/Scripts/Core/Systems/SystemManager.cs` | `SystemManager : IStartable, ITickable, ILateTickable, IFixedTickable, IDisposable, ICoreStartupStatus` | Manages all `ISystem` instances. Injected via constructor `IEnumerable<ISystem>`. `InitAll()` sorts by Priority, calls Init, records failures, and publishes `AppStartedEvent` only when all Core systems initialize successfully. VContainer drives `Start()` -> `InitAll()`, `Tick()` -> `Update()`, `LateTick()` -> `LateUpdate()`, `FixedTick()` -> `FixedUpdate()`. `ShutdownAll()` only shuts down systems whose `Init()` succeeded. | `MessagePipe`, `VContainer.Unity` |
| `GameLogBridge.cs` | `Assets/Scripts/Core/Logging/GameLogBridge.cs` | `GameLogBridge : ISystem` `[CoreSystem]` | Bridges `Framework.Log.GameLog` static delegates into the DI-managed `ILogger<T>`/ZLogger pipeline. Priority=`int.MinValue` so it initializes before other logging systems. | `Framework.Log`, `Microsoft.Extensions.Logging` |
| `StartupProbeSystem.cs` | `Assets/Scripts/Core/Systems/StartupProbeSystem.cs` | `StartupProbeSystem : ISystem` `[CoreSystem]` | Minimal verification system. Logs Init/Shutdown. Priority=0. | none |
| `ICoreStartupStatus.cs` | `Assets/Scripts/Core/Systems/ICoreStartupStatus.cs` | `ICoreStartupStatus` | Exposes Core startup result (`IsStarted`, `HasInitFailures`, `FailedSystemNames`) so upper layers can avoid business loading after Core init failures. | none |
| `PoolService.cs` | `Assets/Scripts/Core/PoolService.cs` | `PoolService : ISystem` `[CoreSystem]` | DI bridge for Framework/Pool. Injects `PoolDependencies.LoadAssetAsync` / `ReleaseAssetByPath` using `IAssetSystem`. Creates `GameObjectPool`. Exposes static shortcuts for `CollectionPool.RentList<>()` etc. Priority=110 (SystemPriority+10). | `IAssetSystem`, `Framework.Pool` |
| `CoreSystemAttribute.cs` | `Assets/Scripts/Core/Systems/Attributes/CoreSystemAttribute.cs` | `CoreSystemAttribute : Attribute` `[AttributeUsage(Class)]` | Marks a Core system class for automatic reflection-based DI registration. | none |
| `AppStartedEvent.cs` | `Assets/Scripts/Core/Systems/Events/AppStartedEvent.cs` | `AppStartedEvent : struct` `[GameEvent]` | Published by `SystemManager` after all Core systems have initialized successfully. | none |
| `AppShuttingDownEvent.cs` | `Assets/Scripts/Core/Systems/Events/AppShuttingDownEvent.cs` | `AppShuttingDownEvent : struct` `[GameEvent]` | Published by `SystemManager` before shutting down systems. | none |
| `CoreTypeRegistration.cs` | `Assets/Scripts/Core/Bootstrap/CoreTypeRegistration.cs` | `static CoreTypeRegistration` | Reflection scanner. `RegisterCoreTypes(IContainerBuilder, MessagePipeOptions, Assembly[])` -- scans assemblies for `[GameEvent]` structs and registers MessageBroker, scans for `[CoreSystem]` classes and registers `AsSelf().AsImplementedInterfaces()`. Validates that `[CoreSystem]` types implement `ISystem` and are in `Core.*` namespace. | `VContainer`, `MessagePipe` |
| `CoreContainerRegistration.cs` | `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs` | `static CoreContainerRegistration` | Entry point: `RegisterCoreServices(IContainerBuilder)`. Calls `RegisterMessagePipe()`, then `RegisterCoreTypes()` scanning the Core assembly, then `RegisterEntryPoint<SystemManager>()`. | `VContainer`, `MessagePipe` |
| `CoreBootstrapStage.cs` | `Assets/Scripts/Core/Bootstrap/CoreBootstrapStage.cs` | `CoreBootstrapStage : IBootstrapStage` `[Preserve]` | Priority=100, StageName="Core". Calls `builder.RegisterCoreServices()` and stores `MessagePipeOptions` in context. | `Boot`, `MessagePipe` |

#### Asset System (Core.Asset namespace)

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `AssetConfig.cs` | `Assets/Framework/Asset/AssetConfig.cs` | `AssetConfig : ScriptableObject`, `AssetConfig.PlayMode` (enum) | Configuration ScriptableObject. PlayMode: `EditorSimulate`, `Offline`, `Host`. Fields: `PackageName`, `CdnBaseUrl`, `DownloadTimeout`, `DownloadMaxConcurrency`, `FailedRetryCount`. | none |
| `AssetConstants.cs` | `Assets/Framework/Asset/AssetConstants.cs` | `static AssetConstants` | Constants: `InitPriority=-999`, `SystemPriority=100`. | none |
| `AssetSystem.cs` | `Assets/Scripts/Core/Asset/AssetSystem.cs` | `AssetSystem : ISystem` `[CoreSystem]` | Thin orchestration layer. Loads `AssetConfig` from Resources, initializes/deinitializes `IAssetRuntime`, publishes `AssetSystemReadyEvent`. | `Framework.Asset`, `MessagePipe` |
| `IAssetSystem.cs` | `Assets/Scripts/Core/Asset/IAssetSystem.cs` | `IAssetSystem` (interface) | Public API: `LoadAssetHandleAsync<T>(path)`, `LoadAssetAsync<T>(path)`, `InstantiateAsync(path, parent)`, `LoadSceneAsync(path, mode, onProgress)`, `CreateDownloader(tag/tags)`, `Release<T>(path)`, `Release(path)`, `UnloadUnused()`. | `UniTask` |
| `AssetHandle.cs` | `Assets/Scripts/Core/Asset/AssetHandle.cs` | `AssetHandle<T> : IDisposable` where T : Object | Typed handle wrapping `YooAsset.AssetHandle`. Properties: `Asset`, `Progress`, `IsDone`, `IsValid`, `Error`. Methods: `Instantiate(parent)`, `Dispose()`. Dispose releases the underlying handle and calls `onDispose` callback. | none |
| `AssetInstanceHandle.cs` | `Assets/Scripts/Core/Asset/AssetInstanceHandle.cs` | `AssetInstanceHandle : IDisposable` | Joint lifecycle for a GameObject instance + its source handle. `Instance` property. `Dispose()` destroys the GameObject and disposes the source handle. | none |
| `AssetSceneHandle.cs` | `Assets/Scripts/Core/Asset/AssetSceneHandle.cs` | `AssetSceneHandle : IDisposable` | Scene handle wrapping `YooAsset.SceneHandle`. `ActivateScene()`, `UnloadAsync()` (awaits unload), `Dispose()` (fire-and-forget unload). | `UniTask` |
| `AssetSystemReadyEvent.cs` | `Assets/Scripts/Core/Asset/AssetSystemReadyEvent.cs` | `AssetSystemReadyEvent : struct` `[GameEvent]` | Published by `AssetSystem.Init()` after the system is ready. | none |

### Layer: General (Assembly: `General`, Namespace: `General`)

Asmdef: `Assets/Scripts/General/KJ.General.asmdef`
References: Boot, Core, VContainer, MessagePipe, MessagePipe.VContainer

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `IModel.cs` | `Assets/Scripts/General/Models/IModel.cs` | `IModel` (interface) | Business lifecycle: `int Priority`, `void Load()`, `void Unload()`. | none |
| `ModelAttribute.cs` | `Assets/Scripts/General/Models/ModelAttribute.cs` | `ModelAttribute : Attribute` `[AttributeUsage(Class)]` | Marks a business layer class for automatic DI registration as `IModel`. | none |
| `ModelLifecycle.cs` | `Assets/Scripts/General/Models/ModelLifecycle.cs` | `ModelLifecycle : IPostStartable, IDisposable` | Manages `IModel` instances. Constructor sorts by Priority. `PostStart()` checks `ICoreStartupStatus` and calls `LoadAll()` only after Core start succeeds. `UnloadAll()` calls Unload in reverse order. `Dispose()` calls UnloadAll. | `Core`, `VContainer.Unity` |
| `GameEventAttribute.cs` | `Assets/Scripts/General/Events/GameEventAttribute.cs` | `GameEventAttribute : Attribute` `[AttributeUsage(Struct)]` | Marks a business event struct for MessagePipe registration. Separate from Core's GameEvent to scope reflection scanning. | none |
| `GeneralContainerRegistration.cs` | `Assets/Scripts/General/Bootstrap/GeneralContainerRegistration.cs` | `static GeneralContainerRegistration` | `RegisterBusinessLayer(IContainerBuilder, MessagePipeOptions, Assembly[])` -- scans assemblies for `[GameEvent]` structs and `[Model]` classes. Registers `ModelLifecycle` once as Singleton + implemented interfaces. | `VContainer`, `MessagePipe` |
| `GeneralBootstrapStage.cs` | `Assets/Scripts/General/Bootstrap/GeneralBootstrapStage.cs` | `GeneralBootstrapStage : IBootstrapStage` | Priority=200, StageName="General". Gets `MessagePipeOptions` from context and calls `builder.RegisterBusinessLayer()` scanning its own assembly. | `Boot`, `MessagePipe` |

### Layer: Project (Assembly: `Project`, Namespace: `Project`)

Asmdef: `Assets/Scripts/Project/KJ.Project.asmdef`
References: Boot, General, VContainer, MessagePipe, MessagePipe.VContainer

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `ProjectBootstrapper.cs` | `Assets/Scripts/Project/Bootstrap/ProjectBootstrapper.cs` | `static ProjectBootstrapper` | Project layer registration hook. `Configure(IContainerBuilder, MessagePipeOptions)` -- calls `builder.RegisterBusinessLayer()` scanning the Project assembly. | `General`, `VContainer`, `MessagePipe`, `Log` |
| `ProjectBootstrapStage.cs` | `Assets/Scripts/Project/Bootstrap/ProjectBootstrapStage.cs` | `ProjectBootstrapStage : IBootstrapStage` | Priority=300, StageName="Project". Gets `MessagePipeOptions` from context and calls `ProjectBootstrapper.Configure()`. | `Boot`, `MessagePipe` |

### Framework: Pool (Assembly: `Pool`, Namespace: `Framework.Pool`)

Asmdef: `Assets/Framework/Pool/Pool.asmdef`
References: UniTask, Cache
Note: Cannot reference any `Assets/Scripts/` code.

**GameObjectPool Lifecycle Rule**:
- **Global Scope (全局对象池)**: 生命期等同于游戏进程，挂载的 root 节点应该设置 `DontDestroyOnLoad`（例如由全局单例 `PoolService` 托管的池）。
- **Local/Scoped Scope (局部/功能对象池)**: 生命期跟随特定的 UI、场景或玩法预制体。挂载的 root 节点**不应该**设置 `DontDestroyOnLoad`，必须跟随父节点自然销毁以回收内存。其持有者（如 Model 或 UI 窗口）需在 `Unload`/`OnDestroy` 时显式调用 `GameObjectPool.Clear()` 释放未归还的对象和 Prefab 引用，防内存泄漏。

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `IPool.cs` | `Assets/Framework/Pool/Interfaces/IPool.cs` | `IPool<T>` (interface) | `T Rent()`, `void Return(T)`, `int IdleCount`. | none |
| `IPoolLease.cs` | `Assets/Framework/Pool/Interfaces/IPoolLease.cs` | `IPoolLease<out T> : IDisposable` (interface) | `T Value`, `bool IsDisposed`. Using-based auto-return. | none |
| `IPoolable.cs` | `Assets/Framework/Pool/Interfaces/IPoolable.cs` | `IPoolable` (interface) | `void ResetState()`. | none |
| `ObjectPool.cs` | `Assets/Framework/Pool/ObjectPool.cs` | `ObjectPool<T> : IPool<T>` where T : class | Generic object pool. `Stack<T>` backing, `lock`-based thread safety. Configurable: `factory`, `reset`, `maxIdle`, `preload`. Methods: `Rent()`, `Return(item)`, `RentLease()` -> `PoolLease<T>`, `GetStatistics()` -> `PoolStatistics`. | none |
| `PoolLease.cs` | `Assets/Framework/Pool/PoolLease.cs` | `PoolLease<T> : IPoolLease<T>` (struct) | Using-compatible struct lease. `Dispose()` calls `_pool.Return(_value)`. | none |
| `PoolStatistics.cs` | `Assets/Framework/Pool/PoolStatistics.cs` | `PoolStatistics` (readonly struct) | Diagnostic data: `IdleCount`, `CreatedCount`, `RentCount`, `ReturnCount`, `MaxIdle`. | none |
| `CollectionPool.cs` | `Assets/Framework/Pool/CollectionPool.cs` | `static CollectionPool` | Static entry points for pooled collections. `RentList<T>()`, `RentHashSet<T>()`, `RentQueue<T>()`, `RentStack<T>()`, `RentDictionary<TKey,TValue>()`. Uses 5 internal static `ObjectPool` instances (ListPool, HashSetPool, QueuePool, StackPool, DictionaryPool) with `preload=32`, `reset=list.Clear()`. | none |
| `PooledCollections.cs` | `Assets/Framework/Pool/PooledCollections.cs` | `PooledList<T>` (struct), `PooledHashSet<T>` (struct), `PooledQueue<T>` (struct), `PooledStack<T>` (struct), `PooledDictionary<TKey,TValue>` (struct), `LeaseState<T>` (sealed class) | RAII wrappers. Each struct holds a `LeaseState<T>` which calls `CollectionPool.Return(...)` on Dispose. Thread-safe disposal via `Interlocked.Exchange`. | none |
| `PoolDependencies.cs` | `Assets/Framework/Pool/PoolDependencies.cs` | `static PoolDependencies` | Static delegate injection points. `Func<string, Transform, UniTask<GameObject>> LoadAssetAsync`, `Action<string> ReleaseAssetByPath`, `ConcurrentDictionary<string, SemaphoreSlim> LoadGates`. | `UniTask` |
| `TypePool.cs` | `Assets/Framework/Pool/Types/TypePool.cs` | `static TypePool` | Global type-based pool registry (`ConcurrentDictionary<Type, object>`). `Register<T>(factory, reset, maxIdle)`, `TryGet<T>()`, `GetOrCreate<T>()`. | none |
| `GameObjectPool.cs` | `Assets/Framework/Pool/Unity/GameObjectPool.cs` | `GameObjectPool` | Unity GameObject pooling. Per-path idle stacks + instance tracking. LIFO recycle with `PoolInstanceTag` pollution detection. Prefab cache via `Cache<string, GameObject>` (from Framework/Cache). Async loading via `PoolDependencies.LoadAssetAsync`. Warmup support. Two container modes: `ChangeParent` / `MovePos`. | `Framework.Cache`, `UniTask` |
| `PoolContainerMode.cs` | `Assets/Framework/Pool/Unity/PoolContainerMode.cs` | `PoolContainerMode` (enum) | `ChangeParent=0` (move to root), `MovePos=1` (move to faraway position). | none |
| `PoolInstanceTag.cs` | `Assets/Framework/Pool/Unity/PoolInstanceTag.cs` | `PoolInstanceTag : MonoBehaviour` (internal) | Attached to pooled GameObjects. Tracks `PrefabPath` and `IsRecycled` for pollution detection. | none |

### Framework: Cache (Assembly: `Cache`, Namespace: `Framework.Cache`)

Asmdef: `Assets/Framework/Cache/Cache.asmdef`
References: (none -- zero external dependencies)

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `ICache.cs` | `Assets/Framework/Cache/Interfaces/ICache.cs` | `ICache<TKey, TValue>` (interface) | `int Count`, `int Capacity`, `TryGet()`, `GetOrAdd()`, `Put()`, `Remove()`, `Clear()`. | none |
| `ICacheEvictionPolicy.cs` | `Assets/Framework/Cache/Interfaces/ICacheEvictionPolicy.cs` | `ICacheEvictionPolicy<TKey>` (interface) | `Touch(key)`, `Remove(key)`, `TrySelectEvictionCandidate(out key)`, `Clear()`. | none |
| `ICacheResContainer.cs` | `Assets/Framework/Cache/ResContainer/ICacheResContainer.cs` | `ICacheResContainer<TKey, TValue>` (interface) | `TryGet()`, `GetOrCreate()`, `TryRemove()`, `Clear()`. Factory-pattern resource container. | none |
| `Cache.cs` | `Assets/Framework/Cache/Cache.cs` | `Cache<TKey, TValue> : ICache<TKey, TValue>` | Generic cache with pluggable eviction policy. `Dictionary` + `ICacheEvictionPolicy`. `lock`-based thread safety. `onEvicted` callback. O(1) put/get/remove, eviction when over capacity. | none |
| `LruCachePolicy.cs` | `Assets/Framework/Cache/Strategy/LruCachePolicy.cs` | `LruCachePolicy<TKey> : ICacheEvictionPolicy<TKey>` | O(1) LRU via `LinkedList<TKey>` + `Dictionary<TKey, LinkedListNode<TKey>>`. `Touch` moves to front. `TrySelectEvictionCandidate` returns the last node. | none |
| `ResourceCache.cs` | `Assets/Framework/Cache/ResContainer/ResourceCache.cs` | `ResourceCache<TKey, TValue> : ICacheResContainer<TKey, TValue>` | Factory-based resource container. `Function<TKey, TValue> factory` creates values on demand. `TryRemove` calls `reset` callback. | none |

---

## Key Interfaces and Contracts

### `ISystem` (Core.Systems)
- `int Priority` -- lower values initialize first
- `void Init()` -- called in priority order by SystemManager
- `void Shutdown()` -- called in reverse priority order by SystemManager
- Implementations: `StartupProbeSystem`, `AssetSystem`, `PoolService`

### `ITickableSystem : ISystem` (Core.Systems)
- `void Update(float deltaTime)`
- `void LateUpdate(float deltaTime)`
- `void FixedUpdate(float fixedDeltaTime)`
- No current implementations in the codebase (reserved for future systems like Timer).

### `IAssetSystem` (Core.Asset)
- `UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path)` -- caller-managed lifecycle
- `UniTask<T> LoadAssetAsync<T>(string path)` -- system-managed lifecycle (cached)
- `UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent)`
- `UniTask<AssetSceneHandle> LoadSceneAsync(string path, LoadSceneMode, Action<float>)`
- `ResourceDownloaderOperation CreateDownloader(string tag)` / `CreateDownloader(string[] tags)`
- `void Release<T>(string path)` / `void Release(string path)`
- `void UnloadUnused()`
- Implementation: `AssetSystem`

### `IBootstrapStage` (Boot)
- `int Priority`
- `string StageName`
- `void Configure(BootstrapContext context)`
- Implementations: `CoreBootstrapStage` (Priority=100), `GeneralBootstrapStage` (Priority=200), `ProjectBootstrapStage` (Priority=300)

### `IPool<T>` (Framework.Pool)
- `T Rent()`
- `void Return(T item)`
- `int IdleCount`
- Implementation: `ObjectPool<T>`

### `IPoolLease<T> : IDisposable` (Framework.Pool)
- `T Value`
- `bool IsDisposed`
- Implementation: `PoolLease<T>` (struct)

### `IPoolable` (Framework.Pool)
- `void ResetState()`

### `ICache<TKey, TValue>` (Framework.Cache)
- `int Count`, `int Capacity`
- `bool TryGet(TKey, out TValue)`
- `TValue GetOrAdd(TKey, Func<TKey, TValue>)`
- `void Put(TKey, TValue)`
- `bool Remove(TKey)`
- `void Clear()`
- Implementation: `Cache<TKey, TValue>`

### `ICacheEvictionPolicy<TKey>` (Framework.Cache)
- `void Touch(TKey key)`
- `void Remove(TKey key)`
- `bool TrySelectEvictionCandidate(out TKey key)`
- `void Clear()`
- Implementation: `LruCachePolicy<TKey>`

### `ICacheResContainer<TKey, TValue>` (Framework.Cache)
- `bool TryGet(TKey, out TValue)`
- `TValue GetOrCreate(TKey)`
- `bool TryRemove(TKey, out TValue)`
- `void Clear()`
- Implementation: `ResourceCache<TKey, TValue>`

### `IModel` (General)
- `int Priority`
- `void Load()`
- `void Unload()`
- No current implementations (reserved for business models).

---

## DI Registration Map

### Attributes (markers for reflection scanning)

| Attribute | Target | Validates | Scanned by |
|-----------|--------|-----------|------------|
| `[CoreSystem]` | Class | Must implement `ISystem`, namespace must start with "Core" | `CoreTypeRegistration.RegisterSystems()` |
| `[GameEvent]` (Core) | Struct | Must be value type, non-enum | `CoreTypeRegistration.RegisterGameEvents()` |
| `[GameEvent]` (General) | Struct | Must be value type, non-enum | `GeneralContainerRegistration.RegisterBusinessEvents()` |
| `[Model]` | Class | Must implement `IModel` | `GeneralContainerRegistration.RegisterModels()` |

### Registered Systems (via `[CoreSystem]` reflection)

| Class | Namespace | Priority | Interfaces | Dependencies injected |
|-------|-----------|----------|------------|----------------------|
| `StartupProbeSystem` | `Core.Systems` | 0 | `ISystem` | (none) |
| `AssetSystem` | `Core.Asset` | 100 | `ISystem` | `IAssetRuntime`, `IPublisher<AssetSystemReadyEvent>` |
| `PoolService` | `Core` | 110 | `ISystem` | `IAssetSystem` |

### Registration Flow

1. **CoreTypeRegistration.RegisterCoreTypes(builder, options, assemblies)**
   - `RegisterGameEvents()` -- for each `[GameEvent]` struct, calls `MessagePipe.ContainerBuilderExtensions.RegisterMessageBroker<T>()` via reflection
   - `RegisterSystems()` -- for each `[CoreSystem]` class, calls `builder.Register(type, Lifetime.Singleton).AsSelf().AsImplementedInterfaces()`

2. **CoreContainerRegistration.RegisterCoreServices(builder)**
   - `options = builder.RegisterMessagePipe()`
   - `builder.RegisterCoreTypes(options, CoreAssembly)` -- scans Core assembly
   - `builder.RegisterEntryPoint<SystemManager>()` -- VContainer calls IStartable.Start() -> InitAll()

3. **GeneralContainerRegistration.RegisterBusinessLayer(builder, options, assemblies)**
   - `RegisterBusinessEvents()` -- for each `[GameEvent]` struct in General/Project assemblies
   - `RegisterModels()` -- for each `[Model]` class, `builder.Register(type, Singleton).AsSelf().As<IModel>()`
   - `builder.Register<ModelLifecycle>(Singleton).AsSelf().AsImplementedInterfaces()` once

---

## Event Map

All events are `readonly struct` types marked with `[GameEvent]`. MessagePipe auto-registers them as `IMessageBroker<T>` via reflection.

| Event | Namespace | Publisher | Purpose |
|-------|-----------|-----------|---------|
| `AppStartedEvent` | `Core.Systems.Events` | `SystemManager.InitAll()` | All Core systems initialized successfully |
| `AppShuttingDownEvent` | `Core.Systems.Events` | `SystemManager.ShutdownAll()` | Systems about to shut down |
| `AssetSystemReadyEvent` | `Core.Asset` | `AssetSystem.Init()` | Asset system ready to serve requests |

No subscribers are currently registered -- these events are published for future consumers.

---

## Bootstrap Flow

```
Entry.Awake()
  |
  v
[Unity loads BootLifetimeScope]
  |
  v
BootLifetimeScope.Configure(IContainerBuilder builder)
  |-- Reads bootstrapStageTypeNames:
  |     Core.Bootstrap.CoreBootstrapStage, Core
  |     General.GeneralBootstrapStage, General
  |     Project.Bootstrap.ProjectBootstrapStage, Project
  |-- Creates each IBootstrapStage through Type.GetType + Activator.CreateInstance
  |-- Creates BootstrapContext(builder)
  |-- Calls context.ConfigureStages(stages)
       |-- sorts by Priority
       |-- rejects duplicate stage types
       |
       v
  CoreBootstrapStage.Configure(context)       Priority=100
    |-- builder.RegisterCoreServices()
    |     |-- Register ZLogger + ILogger<T>
    |     |-- builder.RegisterMessagePipe()
    |     |-- builder.RegisterCoreTypes(options, CoreAssembly)
    |     |     |-- Scans [GameEvent] structs -> RegisterMessageBroker<T>
    |     |     |-- Scans [CoreSystem] classes -> Register(type).AsSelf().AsImplementedInterfaces()
    |     |-- builder.RegisterEntryPoint<SystemManager>()
    |-- context.Set(options)
       |
       v
  GeneralBootstrapStage.Configure(context)    Priority=200
    |-- Gets MessagePipeOptions from context
    |-- builder.RegisterBusinessLayer(options, GeneralAssembly)
    |     |-- Scans [GameEvent] structs -> RegisterMessageBroker<T>
    |     |-- Scans [Model] classes -> Register(type).AsSelf().As<IModel>()
    |     |-- Registers ModelLifecycle once as Self + implemented interfaces
       |
       v
  ProjectBootstrapStage.Configure(context)    Priority=300
    |-- Gets MessagePipeOptions from context
    |-- ProjectBootstrapper.Configure(builder, options)
    |     |-- builder.RegisterBusinessLayer(options, ProjectAssembly)

[After all Configure() calls return, VContainer finalizes container]
  |
  v
VContainer calls IStartable.Start() on registered entry points
  |
  v
SystemManager.Start() -> InitAll()
  |-- Sorts systems by Priority
  |-- Calls Init() on each system in order:
  |     1. GameLogBridge (Priority=int.MinValue) -- routes Framework.GameLog into ZLogger
  |     2. StartupProbeSystem (Priority=0) -- logs probe
  |     3. AssetSystem (Priority=100) -- initializes AssetRuntime via Resources.Load, publishes AssetSystemReadyEvent
  |     4. PoolService (Priority=110) -- injects PoolDependencies, creates GameObjectPool
  |-- Publishes AppStartedEvent only if all Core systems initialized successfully
  |
  v
VContainer calls IPostStartable.PostStart()
  |-- ModelLifecycle.PostStart() -> LoadAll()

[Unity main loop]
  |
  v
VContainer calls ITickable, ILateTickable, IFixedTickable
  |
  v
SystemManager dispatches to ITickableSystem instances:
  Tick() -> Update(dt) for each tickable system
  LateTick() -> LateUpdate(dt) for each tickable system
  FixedTick() -> FixedUpdate(fixedDt) for each tickable system

[Application quit]
  |
  v
VContainer disposes LifetimeScope
  |
  v
SystemManager.Dispose() -> ShutdownAll()
  |-- Publishes AppShuttingDownEvent only if Core startup succeeded
  |-- Calls Shutdown() in reverse priority order for systems whose Init() succeeded
  |     4. PoolService (Priority=110) -- clears GameObjectPool, nulls delegates
  |     3. AssetSystem (Priority=100) -- releases all handles and scene handles
  |     2. StartupProbeSystem (Priority=0) -- logs shutdown
  |     1. GameLogBridge (Priority=int.MinValue) -- clears Framework.GameLog delegate
```

---

## External Dependencies

| Package | Version | Registry | Used by |
|---------|---------|----------|---------|
| VContainer | 1.1.0 | GitHub (hadashiA) | All layers: DI container foundation |
| VContainerSourceGenerator | 1.1.0 | Analyzer DLL | Boot, Core -- compile-time DI code gen |
| UniTask | 2.5.11 | GitHub (Cysharp) | Core (asset system), Pool (dependencies, GameObjectPool) |
| MessagePipe | latest | GitHub (Cysharp) | Core, General, Project -- type-safe event bus |
| MessagePipe.VContainer | latest | GitHub (Cysharp) | Core, General, Project -- VContainer integration |
| MessagePipe.Analyzer | 1.8.2 | Analyzer DLL | MessagePipe diagnostics |
| YooAsset | 3.0 | GitHub (tuyoogame) UPM git | Core -- asset management pipeline |

---

## Generic Type Instantiations (CollectionPool internal pools)

| Pool type | Internal class | Capacity | Reset action |
|-----------|---------------|----------|-------------|
| `ObjectPool<List<T>>` | `CollectionPool.ListPool<T>` | 32 | `list.Clear()` |
| `ObjectPool<HashSet<T>>` | `CollectionPool.HashSetPool<T>` | 32 | `set.Clear()` |
| `ObjectPool<Queue<T>>` | `CollectionPool.QueuePool<T>` | 32 | `queue.Clear()` |
| `ObjectPool<Stack<T>>` | `CollectionPool.StackPool<T>` | 32 | `stack.Clear()` |
| `ObjectPool<Dictionary<TKey, TValue>>` | `CollectionPool.DictionaryPool<TKey, TValue>` | 32 | `dict.Clear()` |

---

## Scene Objects / Startup Assets

Startup stages are ordinary C# classes and do not require Core/General/Project prefabs in `Resources/`.

| Asset | Purpose |
|-------|---------|
| `Assets/GameRes/Scene/Boot/Main.unity` | Minimal boot scene containing the entry LifetimeScope setup |
| `Assets/Resources/AssetConfig.asset` | Minimal Resources config loaded by `AssetSystem` |

`Assets/Resources/` is reserved for minimal boot configuration only.

---

## Directories Not Yet Created

Per `.planning/STATE.md`, the following resource directories are placeholder-only:
- `Assets/GameRes/`
- `Assets/Plugins/`
- `Assets/StreamingAssets/`
- `Assets/Editor/`

The following source modules are in the roadmap but not yet implemented:
- `Core/Timer/` -- tick-based timer system
- `Core/Pool/` (beyond PoolService) -- more pool DI bridge
- `Core/UI/` -- UIManager + UIWindow
- `Core/Network/` -- NetManager, Session, MessageRouter, Protobuf
- `General/Config/` -- Luban config manager
- `General/Audio/` -- AudioManager
- `General/RedDot/` -- RedDot system
- `General/Guide/` -- Guide system
- `General/L10N/` -- Localization

---

## Codemap Cross-Reference

| Document | Location |
|----------|----------|
| Project overview | `.planning/PROJECT.md` |
| Requirements | `.planning/REQUIREMENTS.md` |
| Roadmap & module status | `.planning/ROADMAP.md` |
| Current state & file listing | `.planning/STATE.md` |
| Architecture research | `.planning/research/ARCHITECTURE.md` |
| Directory structure spec | `.planning/目录结构规范.md` |
| DI/Event knowledge graph | `.planning/research/DI_EVENT_KNOWLEDGE_GRAPH.md` |
| Stack decisions | `.planning/research/STACK.md` |
| Pitfalls | `.planning/research/PITFALLS.md` |

---

*End of Codemap*
