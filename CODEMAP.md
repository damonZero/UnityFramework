# KJ Framework Codemap

**Generated:** 2026-07-08
**Unity:** 2022.3.62f2 LTS
**C# Language Version:** 9.0 (preview enabled for source generators)
**Target Platform:** Android (primary), Windows IL2CPP (dev/testing)

---

## Architecture Overview

KJ is a Unity client game framework implementing a strict 4-layer unidirectional dependency architecture. Each layer is enforced by assembly definition (`.asmdef`) files for compile-time isolation.

**Core pattern:** `ISystem` + `[CoreSystem]` attribute for Core-layer systems, `IModel` + `[Model]` attribute for business-layer models. Lifecycle is driven by VContainer DI.

**Bootstrap pattern (HYB-03 implemented):** AOT `Launcher` (`KJ.Launcher.asmdef`) is the ultra-minimal native entry — it initializes YooAsset, loads hot-update DLLs, loads HybridCLR AOT metadata, then reflects into the hot-update `Boot.BootUpdateRunner`. The hot-update `Boot` (`KJ.Boot.asmdef`) handles resource version check, manifest update, download, and hands off `IAssetRuntime` to `Project.Bootstrap.ProjectStartup`. Project creates the formal VContainer root and registers Core → General → Project. C# layer changes are classified separately from package updates: if a managed DLL was already loaded, replacement normally takes effect after restart/next launch; a package update is reserved for native/player/HybridCLR loading-mechanism changes or old packages that lack the needed loader path.

---

## Layer Map

```
Packages (UPM third-party libraries)
    ^
    |
Framework/ (KJ-owned independent packages, no Scripts/ references)
    ^
    |
Launcher (AOT) ──▶ Boot (HotUpdate) ──▶ Core ──▶ General ──▶ Project
```

**Dependency direction (enforced by .asmdef):**

| Layer | Can reference |
|-------|--------------|
| Launcher (AOT) | UniTask, YooAsset, HybridCLR.Runtime, AssetShared |
| Boot (HotUpdate) | Asset, Log, RuntimeLog, UniTask, AssetShared, YooAsset, Launcher |
| Core | Asset, AssetShared, Event, Log, RuntimeLog, Pool, Cache, VContainer, MessagePipe, MessagePipe.VContainer, UniTask, ZLinq, ZLogger, Asset |
| General | Core, Event, Log, VContainer, VContainer.Unity, MessagePipe, MessagePipe.VContainer, ZLinq |
| Project | Asset, Core, General, Event, Log, VContainer, VContainer.Unity, MessagePipe |
| Framework.Pool | UniTask, Cache |
| Framework.Cache | (none) |
| Framework.AssetShared | (none) |

---

## Dependency Matrix

```
                        Depends on
              Launcher Boot  Core  Gen  Proj  Pool Cache  AssetShared  VContainer  MP  UniTask  YooAsset
Launcher       -        N     N     N    N     N     N      Y           N          N    Y        Y
Boot           Y        -     N     N    N     N     N      Y           N          N    Y        Y
Core           N        N     -     N    N     Y     Y      Y           Y          Y    Y        Y
General        N        N     Y     -    N     N     N      N           Y          Y    N        N
Project        N        N     Y     Y    -     N     N      N           Y          Y    N        N
Pool           N        N     N     N    N     -     Y      N           N          N    Y        N
Cache          N        N     N     N    N     N     -      N           N          N    N        N
AssetShared    N        N     N     N    N     N     N      -           N          N    N        N
```

---

## File Index

### Layer: Launcher / AOT (Assembly: `KJ.Launcher`, Namespace: `Boot`)

Asmdef: `Assets/Scripts/Boot/Launcher/KJ.Launcher.asmdef`
References: `UniTask`, `YooAsset`, `HybridCLR.Runtime`, `Framework.AssetShared`
Hard rule: **Must not reference any Framework package or hot-update assembly** (asmdef-enforced).

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `Entry.cs` | `Assets/Scripts/Boot/Launcher/Entry.cs` | `Entry : MonoBehaviour` | AOT root MonoBehaviour. `Awake()` → `DontDestroyOnLoad` → `new BootLoader().RunAsync()`. Holds serialized startup settings. | `UnityEngine` |
| `BootLoader.cs` | `Assets/Scripts/Boot/Launcher/BootLoader.cs` | `BootLoader` | AOT shell: init YooAsset, load all hot-update DLLs, load AOT metadata, construct `BootBridge`, reflect `"Boot.BootUpdateRunner, Boot"`. Crosses AOT→hot-update boundary. | `YooAsset`, `HybridCLR.Runtime` |
| `BootBridge.cs` | `Assets/Scripts/Boot/Launcher/BootBridge.cs` | `BootBridge` | Cross-boundary state carrier: Package, Settings, View, Config, EarlyLogs. | none |
| `BootStartupLog.cs` | `Assets/Scripts/Boot/Launcher/BootStartupLog.cs` | `BootStartupLog` | AOT-stage plain-text + in-memory log. Independent of `Framework.Log`/`RuntimeLog`. Replayed by `BootUpdateRunner.ReplayEarlyLogs()`. | none |
| `IsExternalInit.cs` | `Assets/Scripts/Boot/Launcher/IsExternalInit.cs` | `System.Runtime.CompilerServices.IsExternalInit` | Polyfill for Unity .NET Standard 2.1 missing type. | none |
| `BootStartupSettings.cs` | `Assets/Scripts/Boot/Launcher/Data/BootStartupSettings.cs` | `BootStartupSettings` | Serializable Entry settings: update toggles, local StreamingAssets fallback root, asset tag, startup type/method, AOT metadata entries, hot-update DLL entries. | none |
| `BootAssemblyEntry.cs` | `Assets/Scripts/Boot/Launcher/Data/BootAssemblyEntry.cs` | `BootAssemblyEntry` | Serializable hot-update DLL entry: assembly name + YooAsset raw asset path / local file / Resources fallback. | none |
| `BootMetadataEntry.cs` | `Assets/Scripts/Boot/Launcher/Data/BootMetadataEntry.cs` | `BootMetadataEntry` | Serializable AOT metadata entry: assembly name + asset path fallback. | none |
| `IBootStartupView.cs` | `Assets/Scripts/Boot/Launcher/Data/IBootStartupView.cs` | `IBootStartupView` | Optional update UI contract: status, progress, repair visibility. | none |
| `BootRemoteService.cs` | `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootRemoteService.cs` | `BootRemoteService : IRemoteService` | AOT-side YooAsset IRemoteService (fixed deadlock). | `YooAsset` |

### Layer: Boot / HotUpdate (Assembly: `KJ.Boot`, Namespace: `Boot`)

Asmdef: `Assets/Scripts/Boot/KJ.Boot.asmdef`
References: `Asset`, `Log`, `RuntimeLog`, `UniTask`, `AssetShared`, `YooAsset`, `Launcher`
Note: **Hot-update assembly** loaded by Launcher. No longer references `HybridCLR.Runtime`.

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `BootUpdateRunner.cs` | `Assets/Scripts/Boot/BootUpdateRunner.cs` | `BootUpdateRunner : IDisposable` | Hot-update startup entry. Reflect-launched by Launcher. Initializes `IAssetRuntime`, requests version, updates manifest, downloads, loads AOT metadata + DLLs, reflects into `ProjectStartup`. Replays early logs. | `Framework.Asset`, `Framework.RuntimeLog` |
| `BootRuntimeLogBootstrap.cs` | `Assets/Scripts/Boot/BootRuntimeLogBootstrap.cs` | `static BootRuntimeLogBootstrap` | Installs `Framework.RuntimeLog.RuntimeLogSession` before Core/ZLogger exists. | `Framework.Log`, `Framework.RuntimeLog`, `UnityEngine` |
| `HybridClrReflection.cs` | `Assets/Scripts/Boot/HybridClrReflection.cs` | `HybridClrReflection` | Reflection wrapper around `HybridCLR.RuntimeApi` (Boot does not reference HybridCLR.Runtime directly). | none |

### Boot.Editor (Assembly: `Boot.Editor`, Editor-only)

Asmdef: `Assets/Scripts/Boot.Editor/Boot.Editor.asmdef`
References: `Boot`, `Asset`, `Framework.Asset.Editor`, `HybridCLR.Editor`, `YooAsset`, `YooAsset.Editor`

| File | Path | Key Types | Description | Dependencies |
|------|------|-----------|-------------|-------------|
| `KJHybridClrBuildTools.cs` | `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | `static KJHybridClrBuildTools` | `KJ/HybridCLR/*` menu commands: Prepare/Compile/Generate/Sync/Validate | HybridCLR, YooAsset Editor |
| `KJBuildPipeline.cs` | `Assets/Scripts/Boot.Editor/Build/KJBuildPipeline.cs` | `static KJBuildPipeline` | S0–S9 stage orchestrator. `Build()` / `BuildWithMask()` / `IncrementalBuild()`. `KJ/Build/*` menu. | Build stages |
| `StageDependencyTracker.cs` | `Assets/Scripts/Boot.Editor/Build/StageDependencyTracker.cs` | `StageDependencyTracker` | Change detection engine. Monitors `Assets/Scripts/**/*.cs` (S1/S2), `Assets/GameRes/HotUpdate/**` (S4), `Assets/Resources/AssetConfig.asset` (S5). Cascade: S1→S2→S3→S4→S6, S5→S6 independent. | none |
| `BuildStagePanel.cs` | `Assets/Scripts/Boot.Editor/Build/BuildStagePanel.cs` | `BuildStagePanel : EditorWindow` | Visual build stage manager. Auto-detection checkboxes + "增量构建"/"全量构建" buttons. | KJBuildPipeline |
| `BuildConfig.cs` | `Assets/Scripts/Boot.Editor/Build/BuildConfig.cs` | `BuildConfig : ScriptableObject` | Serializable build configuration: Platform, BuildType, output paths. | none |
| `StageBuildYooAsset.cs` | `Assets/Scripts/Boot.Editor/Build/StageBuildYooAsset.cs` | (static methods) | YooAsset production build via `ScriptableBuildPipeline` → `StreamingAssets/`. | YooAsset.Editor |
| `StageApplyConfig.cs` | `Assets/Scripts/Boot.Editor/Build/StageApplyConfig.cs` | (static methods) | Direct YAML write `AssetConfig.Mode = Offline` + SetDirty/SaveAssets/Refresh + rollback. | none |
| `StageBuildPlayer.cs` | `Assets/Scripts/Boot.Editor/Build/StageBuildPlayer.cs` | (static methods) | `BuildPipeline.BuildPlayer` with IL2CPP + Android. | UnityEditor |
| `StageSmokeRun.cs` | `Assets/Scripts/Boot.Editor/Build/StageSmokeRun.cs` | (static methods) | ADB install + logcat capture + `latest.jsonl` boot chain verification. | ADB |
| `StageValidateArtifacts.cs` | `Assets/Scripts/Boot.Editor/Build/StageValidateArtifacts.cs` | (static methods) | Verify APK exists, StreamingAssets contains bundles. | none |
| `PlayerBuildPrivatePathValidator.cs` | `Assets/Scripts/Boot.Editor/Build/PlayerBuildPrivatePathValidator.cs` | `PlayerBuildPrivatePathValidator` | Blocks Player builds when Build Settings/Resources/StreamingAssets contain `_` prefix segments. | none |

### Layer: Core (Assembly: `KJ.Core`, Namespace: `Core`)

Asmdef: `Assets/Scripts/Core/KJ.Core.asmdef`
References: Asset, AssetShared, Event, Pool, Cache, Log, RuntimeLog, VContainer, MessagePipe, MessagePipe.VContainer, UniTask, ZLinq, ZLogger

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `ISystem.cs` | `Assets/Scripts/Core/Systems/ISystem.cs` | `ISystem`, `ITickableSystem : ISystem` | Core lifecycle contracts. |
| `SystemManager.cs` | `Assets/Scripts/Core/Systems/SystemManager.cs` | `SystemManager : IStartable, ITickable, ILateTickable, IFixedTickable, IDisposable, ICoreStartupStatus` | Manages `ISystem` instances via VContainer. Priority-sorted Init/Shutdown + Tick dispatch. |
| `PoolService.cs` | `Assets/Scripts/Core/PoolService.cs` | `PoolService : ISystem` `[CoreSystem]` | DI bridge for Framework.Pool. Injects `PoolDependencies.LoadAssetAsync` / `ReleaseAssetByPath`. Creates `GameObjectPool`. Exposes `CollectionPool.RentList<>()` shortcuts. Priority=110. |
| `CoreSystemAttribute.cs` | `Assets/Scripts/Core/Systems/Attributes/CoreSystemAttribute.cs` | `CoreSystemAttribute : Attribute` | Marks a Core system class for reflection-based DI registration. |
| `CoreTypeRegistration.cs` | `Assets/Scripts/Core/Bootstrap/CoreTypeRegistration.cs` | `static CoreTypeRegistration` | Reflection scanner: `[GameEvent]` → MessageBroker, `[CoreSystem]` → `AsSelf().AsImplementedInterfaces()`. |
| `CoreContainerRegistration.cs` | `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs` | `static CoreContainerRegistration` | Entry: `RegisterCoreServices()` → MessagePipe + CoreTypes + SystemManager. |
| `CoreBootstrapStage.cs` | `Assets/Scripts/Core/Bootstrap/CoreBootstrapStage.cs` | `static CoreBootstrapStage` | Registers Core into `CoreStartupContext`. |
| `CoreStartupContext.cs` | `Assets/Scripts/Core/Bootstrap/CoreStartupContext.cs` | `CoreStartupContext` | Carries `IContainerBuilder` and `MessagePipeOptions`. |

#### Asset System (Core.Asset namespace)

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `AssetSystem.cs` | `Assets/Scripts/Core/Asset/AssetSystem.cs` | `AssetSystem : ISystem` `[CoreSystem]` | Thin orchestration. Verifies Boot-provided `IAssetRuntime` ready, publishes `AssetSystemReadyEvent`, owns shutdown. |
| `IAssetSystem.cs` | `Assets/Framework/Asset/IAssetSystem.cs` | `IAssetSystem` (interface) | Public loading API: `LoadAssetHandleAsync<T>`, `LoadAssetAsync<T>`, `InstantiateAsync`, `LoadSceneAsync`, `CreateDownloader`, `Release`, `UnloadUnused`. |
| `IAssetRuntime.cs` | `Assets/Framework/Asset/IAssetRuntime.cs` | `IAssetRuntime` (interface) | Startup-facing API: `BeginInitialize(config)`, `LastError`, `UpdateManifest()`, `CreateDownloader(tag/tags)`, `LoadRawBytes(path)`, `Shutdown()`. |
| `AssetRuntime.cs` | `Assets/Framework/Asset/AssetRuntime.cs` | `AssetRuntime` | YooAsset adapter implementation. Owned/cached dual-channel, SemaphoreSlim concurrency, `AssetCacheKey`. |
| `AssetRuntimeFactory.cs` | `Assets/Framework/Asset/AssetRuntimeFactory.cs` | `AssetRuntimeFactory` | Creates concrete runtime behind `IAssetRuntime` interface. |
| `AssetHandle.cs` | `Assets/Scripts/Core/Asset/AssetHandle.cs` | `AssetHandle<T> : IDisposable` where T : Object | Typed handle wrapping YooAsset handle. |

#### Logging (Core.Logging namespace)

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `GameLogBridge.cs` | `Assets/Scripts/Core/Logging/GameLogBridge.cs` | `GameLogBridge : IGameLogSink` | Adapter: writes `GameLog` → RuntimeLog session + ZLogger Console. NOT a `[CoreSystem]`. |
| `RuntimeLogBootstrap.cs` | `Assets/Scripts/Core/Logging/RuntimeLogBootstrap.cs` | `static RuntimeLogBootstrap` | Creates/reuses RuntimeLog session, fills Unity/session metadata. |
| `RuntimeLogLoggerProvider.cs` | `Assets/Scripts/Core/Logging/RuntimeLogLoggerProvider.cs` | `RuntimeLogLoggerProvider : ILoggerProvider` | Writes `ILogger<T>` / `[ZLoggerMessage]` into same JSONL session. |
| `StartupProbeSystem.cs` | `Assets/Scripts/Core/Systems/StartupProbeSystem.cs` | `StartupProbeSystem : ISystem` `[CoreSystem]` | Minimal verification system. Priority=0. |

### Layer: General (Assembly: `KJ.General`, Namespace: `General`)

Asmdef: `Assets/Scripts/General/KJ.General.asmdef`
References: Core, Event, Log, VContainer, VContainer.Unity, MessagePipe, MessagePipe.VContainer, ZLinq

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `IModel.cs` | `Assets/Scripts/General/Models/IModel.cs` | `IModel` (interface) | Business lifecycle: `int Priority`, `void Load()`, `void Unload()`. |
| `ModelAttribute.cs` | `Assets/Scripts/General/Models/ModelAttribute.cs` | `ModelAttribute : Attribute` | Marks business class for DI registration as `IModel`. |
| `ModelLifecycle.cs` | `Assets/Scripts/General/Models/ModelLifecycle.cs` | `ModelLifecycle : IPostStartable, IDisposable` | Priority-sorted Load/Unload lifecycle. Checks `ICoreStartupStatus`. |
| `GeneralContainerRegistration.cs` | `Assets/Scripts/General/Bootstrap/GeneralContainerRegistration.cs` | `static GeneralContainerRegistration` | `RegisterBusinessLayer()`: scans `[GameEvent]` + `[Model]`. |
| `GeneralBootstrapStage.cs` | `Assets/Scripts/General/Bootstrap/GeneralBootstrapStage.cs` | `static GeneralBootstrapStage` | Registers General business layer via `CoreStartupContext`. |

### Layer: Project (Assembly: `KJ.Project`, Namespace: `Project`)

Asmdef: `Assets/Scripts/Project/KJ.Project.asmdef`
References: Asset, Core, General, Event, Log, VContainer, VContainer.Unity, MessagePipe, MessagePipe.VContainer

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `ProjectStartup.cs` | `Assets/Scripts/Project/Bootstrap/ProjectStartup.cs` | `static ProjectStartup` | Formal hot-update entry called by Boot via reflection. Receives `IAssetRuntime`, creates VContainer root. |
| `ProjectLifetimeScope.cs` | `Assets/Scripts/Project/Bootstrap/ProjectLifetimeScope.cs` | `ProjectLifetimeScope : LifetimeScope` | Formal VContainer root. Consumes pending Boot asset runtime, registers Core→General→Project. |
| `ProjectBootstrapper.cs` | `Assets/Scripts/Project/Bootstrap/ProjectBootstrapper.cs` | `static ProjectBootstrapper` | Project layer registration hook. |
| `ProjectBootstrapStage.cs` | `Assets/Scripts/Project/Bootstrap/ProjectBootstrapStage.cs` | `static ProjectBootstrapStage` | Registers Project business layer. |

### Framework: Pool (Assembly: `KJ.Pool`, Namespace: `Framework.Pool`)

Asmdef: `Assets/Framework/Pool/Pool.asmdef`
References: UniTask, Cache

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `ObjectPool.cs` | `Assets/Framework/Pool/ObjectPool.cs` | `ObjectPool<T> : IPool<T>` | Generic object pool. Stack backing, lock-based thread safety. |
| `CollectionPool.cs` | `Assets/Framework/Pool/CollectionPool.cs` | `static CollectionPool` | Static entry: `RentList<T>()`, `RentHashSet<T>()`, `RentQueue<T>()`, `RentStack<T>()`, `RentDictionary<TKey,TValue>()`. |
| `PooledCollections.cs` | `Assets/Framework/Pool/PooledCollections.cs` | `PooledList<T>` (struct), etc. | RAII wrappers, `[NonCopyable]` marker (mutable struct — forbid value copy). |
| `TypePool.cs` | `Assets/Framework/Pool/Types/TypePool.cs` | `static TypePool` | `ConcurrentDictionary<Type, object>` type-based pool registry. |
| `InstanceRecyclePolicy.cs` | `Assets/Framework/Pool/InstanceRecyclePolicy.cs` | `IInstanceRecyclePolicy`, `CapacityInstancePolicy`, `PersistentInstancePolicy` | Pluggable retention: capacity cap, persistent-path exemption (ETPro-inspired). |
| `GameObjectPool.cs` | `Assets/Framework/Pool/Unity/GameObjectPool.cs` | `GameObjectPool`, `internal PrefabPoolState` | Unity GameObject pooling. `PrefabPoolState` merges old five dicts; reverse-index (`_instanceToPath`) guards double-recycle / cross-path; prefab cache via `BoundedStore<string, GameObject>` + `LruPolicy`; `IInstanceRecyclePolicy` pluggable retention; `AssertMainThread()` runtime check. |
| `PoolDependencies.cs` | `Assets/Framework/Pool/PoolDependencies.cs` | `static PoolDependencies` | Static delegate injection: `LoadAssetAsync`, `ReleaseAssetByPath`. |
| `PoolContainerMode.cs` | `Assets/Framework/Pool/Unity/PoolContainerMode.cs` | `PoolContainerMode` (enum) | `ChangeParent=0`, `MovePos=1`. |

### Framework: Cache (Assembly: `Cache`, Namespace: `Framework.Cache`)
> Refreshed 2026-07-08: `BoundedStore<TKey,TValue>` is the sole container. Legacy `Cache` / `LruCachePolicy` / `ResourceCache` / `ICacheResContainer` / `IPoolable` deleted (no forward compat needed). Eviction policy interface: `IStoreEvictionPolicy` (OnAdded/OnAccessed/OnRemoved).

Asmdef: `Assets/Framework/Cache/Cache.asmdef`
References: (none)

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `ICache.cs` | `Assets/Framework/Cache/Interfaces/ICache.cs` | `ICache<TKey, TValue>` (interface) | `Count`, `Capacity`, `TryGet`, `GetOrAdd`, `Put`, `Remove`, `Clear`. Implemented by `BoundedStore`. |
| `IStoreEvictionPolicy.cs` | `Assets/Framework/Cache/Interfaces/IStoreEvictionPolicy.cs` | `IStoreEvictionPolicy<TKey>` (interface) | `OnAdded` / `OnAccessed` / `OnRemoved` / `TrySelectEvictionCandidate` / `Clear`. |
| `BoundedStore.cs` | `Assets/Framework/Cache/BoundedStore.cs` | `BoundedStore<TKey, TValue> : ICache<TKey, TValue>` | Container: single-flight `GetOrAdd`, `Put` overwrite = Remove+Add (onEvicted+OnRemoved), `onEvicted` outside lock, H1/H2 guards. |
| `LruPolicy.cs` | `Assets/Framework/Cache/Strategy/LruPolicy.cs` | `LruPolicy<TKey> : IStoreEvictionPolicy<TKey>` | O(1) LRU. |
| `TtlPolicy.cs` | `Assets/Framework/Cache/Strategy/TtlPolicy.cs` | `TtlPolicy<TKey> : IStoreEvictionPolicy<TKey>` | TTL expiry with injectable clock. |
| `CapacityPolicy.cs` | `Assets/Framework/Cache/Strategy/CapacityPolicy.cs` | `CapacityPolicy<TKey> : IStoreEvictionPolicy<TKey>` | FIFO capacity. |
| `CompositePolicy.cs` | `Assets/Framework/Cache/Strategy/CompositePolicy.cs` | `CompositePolicy<TKey> : IStoreEvictionPolicy<TKey>` | Fan-out combinator. |

### Framework: Asset (Assembly: `KJ.Asset`, Namespace: `Framework.Asset`)

Asmdef: `Assets/Framework/Asset/Asset.asmdef`
References: UniTask, YooAsset

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `IAssetSystem.cs` | `Assets/Framework/Asset/IAssetSystem.cs` | `IAssetSystem` (interface) | Stable asset API for upper layers. |
| `IAssetRuntime.cs` | `Assets/Framework/Asset/IAssetRuntime.cs` | `IAssetRuntime` (interface) | Startup-facing runtime interface. |
| `AssetRuntime.cs` | `Assets/Framework/Asset/AssetRuntime.cs` | `AssetRuntime` | YooAsset adapter with owned/cached dual-channel. |
| `AssetRuntimeFactory.cs` | `Assets/Framework/Asset/AssetRuntimeFactory.cs` | `AssetRuntimeFactory` | Factory for concrete runtime behind interface. |
| `AssetDownloadHandle.cs` | `Assets/Framework/Asset/AssetDownloadHandle.cs` | `AssetDownloadHandle` | Downloader wrapper hiding YooAsset types. |
| `AssetInitializeHandle.cs` | `Assets/Framework/Asset/AssetInitializeHandle.cs` | `AssetInitializeHandle` | Pollable init handle hiding YooAsset types. |
| `AssetUpdateManifestHandle.cs` | `Assets/Framework/Asset/AssetUpdateManifestHandle.cs` | `AssetUpdateManifestHandle` | Pollable manifest update handle. |

### Framework: AssetShared (Assembly: `KJ.AssetShared`, Namespace: `Framework.Asset`)

Asmdef: `Assets/Framework/AssetShared/AssetShared.asmdef`
References: (none — zero external dependencies)
Purpose: Holds `AssetConfig` / `AssetConstants` so both AOT Launcher and hot-update layer can reference them across the boundary.

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `AssetConfig.cs` | `Assets/Framework/AssetShared/AssetConfig.cs` | `AssetConfig : ScriptableObject`, `PlayMode` (enum) | Config: `EditorSimulate`, `Offline`, `Host`. Fields: `PackageName`, `EditorSimulatePackageRoot`, `CdnBaseUrl`, `DownloadTimeout`, `DownloadMaxConcurrency`, `FailedRetryCount`. |
| `AssetConstants.cs` | `Assets/Framework/AssetShared/AssetConstants.cs` | `static AssetConstants` | `InitPriority=-999`, `SystemPriority=100`. |

### Framework: RuntimeLog (Assembly: `KJ.RuntimeLog`, Namespace: `Framework.RuntimeLog`)

Asmdef: `Assets/Framework/RuntimeLog/RuntimeLog.asmdef`
References: Log (`noEngineReferences=true`)

| File | Path | Key Types | Description |
|------|------|-----------|-------------|
| `RuntimeLogSession.cs` | `Assets/Framework/RuntimeLog/RuntimeLogSession.cs` | `RuntimeLogSession` | JSONL writer + session manifest + flush/dispose. |
| `RuntimeLogManager.cs` | `Assets/Framework/RuntimeLog/RuntimeLogManager.cs` | `RuntimeLogManager` | Current session management, avoids overwriting Core `GameLogBridge`. |
| `RuntimeLogEntry.cs` | `Assets/Framework/RuntimeLog/RuntimeLogEntry.cs` | `RuntimeLogEntry` | AI-readable runtime log entry. |
| `RuntimeLogSessionInfo.cs` | `Assets/Framework/RuntimeLog/RuntimeLogSessionInfo.cs` | `RuntimeLogSessionInfo` | Session manifest: Unity/platform/package/hot-update assemblies/AOT metadata. |
| `RuntimeLogJson.cs` | `Assets/Framework/RuntimeLog/RuntimeLogJson.cs` | (static) | No-dependency JSON serializer. |
| `RuntimeLogPhaseResolver.cs` | `Assets/Framework/RuntimeLog/RuntimeLogPhaseResolver.cs` | `RuntimeLogPhaseResolver` | Phase classification: Boot/HybridCLR/Core.Asset/Core.Init/ModelLifecycle. |

---

## Key Interfaces and Contracts

### `ISystem` (Core.Systems)
- `int Priority` — lower values initialize first
- `void Init()` — called in priority order by SystemManager
- `void Shutdown()` — called in reverse priority order
- Implementations: `StartupProbeSystem`, `AssetSystem`, `PoolService`

### `ITickableSystem : ISystem` (Core.Systems)
- `void Update(float)`, `void LateUpdate(float)`, `void FixedUpdate(float)`
- No current implementations (reserved for future systems).

### `IAssetSystem` (Framework.Asset)
- `UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path)` — caller-managed lifecycle
- `UniTask<T> LoadAssetAsync<T>(string path)` — system-managed lifecycle (cached)
- `UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent)`
- `UniTask<AssetSceneHandle> LoadSceneAsync(string path, LoadSceneMode, Action<float>)`
- `AssetDownloadHandle CreateDownloader(string tag)` / `CreateDownloader(string[] tags)`
- `byte[] LoadRawBytes(string path)` on `IAssetRuntime` — reads YooAsset RawFile bytes
- `AssetUpdateManifestHandle UpdateManifest()` on `IAssetRuntime`
- `void Release<T>(string path)`, `void Release(string path)`, `void UnloadUnused()`
- Implementation: `AssetSystem` (lifecycle), `AssetRuntime` (adapter)

### Boot/Launcher Startup
- `Entry` (AOT) is the only MonoBehaviour entry point in `Assets/Scripts/Boot/Launcher/`.
- `BootLoader` (AOT) initializes YooAsset, loads DLLs, reflects `Boot.BootUpdateRunner`.
- `BootUpdateRunner` (HotUpdate) performs resource/code update, reflects `ProjectStartup`.
- `IBootStartupView` is optional and only supports update progress, status, and repair visibility.

### HybridCLR Editor Tooling
- `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` provides `KJ/HybridCLR/*` menus.
- `Assets/Scripts/Boot.Editor/Build/PlayerBuildPrivatePathValidator.cs` blocks `_` prefix in build paths.
- `Prepare Runtime Assets And Boot` is the normal smoke-test path for Editor Play.
- `Prepare YooAsset Editor Simulate Package` rebuilds the editor-simulate virtual package.
- `Generate All And Sync` is the full formal-build preparation (heavy).

### Build Pipeline (KJ/Build/* menus)
- S0: Environment Check → S1: Compile HotUpdate DLLs → S2: Generate AOT Metadata → S3: Sync to HotUpdate → S4: Build YooAsset → S5: Apply Config → S6: Build Player → S7: Validate → S8: Smoke → S9: Post-Build
- Stage markers in `Build/Android/` prevent re-running completed stages.
- `StageDependencyTracker` does change detection for incremental builds.
- `BuildStagePanel` (`KJ → Build → Build Stage Manager...`) provides visual control.

### YooAsset Editor Rules
- `Assets/Framework/Asset.Editor/YooAsset/KJAssetIgnoreRule.cs` ignores `_` prefix path segments.
- YooAsset 3.0 `EditorSimulate` needs generated package root directory (not just package name).
- Player build validation covers Unity-native paths; YooAsset collection validation covers AB/raw-file collection.

### AI Runtime Logging
- `Framework.Log/GameLog.cs` — stable logging facade + bounded startup buffer.
- `Framework.RuntimeLog/RuntimeLogSession.cs` — JSON Lines + session manifest.
- `BootRuntimeLogBootstrap` installs session in `Entry.Awake()` before Core/ZLogger exists.
- `RuntimeLogLoggerProvider` writes `ILogger<T>` / `[ZLoggerMessage]` into same session.
- `GameLogBridge` writes `GameLog` → RuntimeLog + ZLogger Unity Console.
- `Core.Editor/Logging/RuntimeLogEditorTools.cs` → `KJ/Runtime Logs/*` menu.

---

## DI Registration Map

### Attributes

| Attribute | Target | Validates | Scanned by |
|-----------|--------|-----------|------------|
| `[CoreSystem]` | Class | Must implement `ISystem`, namespace starts with "Core" | `CoreTypeRegistration.RegisterSystems()` |
| `[GameEvent]` (Core/General) | Struct | Must be value type, non-enum | `CoreTypeRegistration` / `GeneralContainerRegistration` |
| `[Model]` | Class | Must implement `IModel` | `GeneralContainerRegistration.RegisterModels()` |

### Registered Systems

| Class | Namespace | Priority | Interfaces | Dependencies |
|-------|-----------|----------|------------|-------------|
| `StartupProbeSystem` | `Core.Systems` | 0 | `ISystem` | (none) |
| `AssetSystem` | `Core.Asset` | 100 | `ISystem` | `IAssetRuntime`, `IPublisher<AssetSystemReadyEvent>` |
| `PoolService` | `Core` | 110 | `ISystem` | `IAssetSystem` |

### Registration Flow

1. **CoreContainerRegistration.RegisterCoreServices(builder)**
   - `RegisterMessagePipe()` → `RegisterCoreTypes(CoreAssembly)` → `RegisterEntryPoint<SystemManager>()`
2. **CoreTypeRegistration.RegisterCoreTypes(builder, options, assemblies)**
   - `RegisterGameEvents()` — scans `[GameEvent]` structs → `RegisterMessageBroker<T>()`
   - `RegisterSystems()` — scans `[CoreSystem]` classes → `AsSelf().AsImplementedInterfaces()`
3. **GeneralContainerRegistration.RegisterBusinessLayer(builder, options, assemblies)**
   - Scans `[GameEvent]` + `[Model]` in General/Project assemblies
   - `Register<ModelLifecycle>(Singleton).AsSelf().AsImplementedInterfaces()`

---

## Event Map

| Event | Namespace | Publisher | Purpose |
|-------|-----------|-----------|---------|
| `AppStartedEvent` | `Core.Systems.Events` | `SystemManager.InitAll()` | All Core systems initialized successfully |
| `AppShuttingDownEvent` | `Core.Systems.Events` | `SystemManager.ShutdownAll()` | Systems about to shut down |
| `AssetSystemReadyEvent` | `Core.Asset` | `AssetSystem.Init()` | Asset system ready |

---

## Bootstrap Flow

```
Entry.Awake() (AOT Launcher)
  |-- BootStartupLog captures early events
  |-- DontDestroyOnLoad
  |
  v
BootLoader.RunAsync() (AOT)
  |-- Creates YooAsset DefaultPackage + BootRemoteService
  |-- Calls package.InitializeAsync() → polls completion
  |-- Reads BootStartupSettings from Entry serialized fields
  |-- Loads AOT metadata assemblies via HybridCLR.RuntimeApi
  |-- Loads hot-update DLL bytes from YooAsset RawFile
  |-- Assembles BootBridge { Package, Settings, View, Config, EarlyLogs }
  |-- Assembly.Load(byte[]) for each hot-update DLL
  |-- Reflects Boot.BootUpdateRunner.Start(BootBridge)
  |
  v
BootUpdateRunner.Start(BootBridge) (HotUpdate Boot)
  |-- BootRuntimeLogBootstrap.EnsureInstalled() → RuntimeLog session
  |-- Replays BootStartupLog → RuntimeLog
  |-- Creates IAssetRuntime via AssetRuntimeFactory
  |-- Initializes/reuses asset runtime from BootBridge.Package
  |-- Requests resource version and updates manifest
  |-- Downloads resources
  |-- Loads AOT metadata (supplemental)
  |-- Loads Core/General/Project hot-update DLLs via Assembly.Load
  |-- Reflects Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)
  |
  v
ProjectStartup.Start()
  |-- Creates ProjectLifetimeScope GameObject
  |-- VContainer Configure(builder)
       |-- CoreBootstrapStage → RegisterCoreServices(bootAssetRuntime)
       |     |-- Reuse RuntimeLog session, register ZLogger + ILogger<T>
       |     |-- Install GameLogBridge(runtimeLogSession, logger)
       |     |-- builder.RegisterMessagePipe()
       |     |-- builder.RegisterCoreTypes(options, CoreAssembly)
       |     |-- builder.RegisterEntryPoint<SystemManager>()
       |-- GeneralBootstrapStage → RegisterBusinessLayer(options, GeneralAssembly)
       |-- ProjectBootstrapStage → ProjectBootstrapper.Configure(builder, options)

[VContainer finalizes container]
  |
  v
SystemManager.Start() → InitAll()
  |-- 1. StartupProbeSystem (Priority=0) — logs probe
  |-- 2. AssetSystem (Priority=100) — verifies runtime, publishes AssetSystemReadyEvent
  |-- 3. PoolService (Priority=110) — injects PoolDependencies, creates GameObjectPool
  |-- Publishes AppStartedEvent if all succeeded

[VContainer IPostStartable → ModelLifecycle.LoadAll()]
[Unity main loop → SystemManager Tick/LateTick/FixedTick dispatch]
[Application quit → SystemManager.ShutdownAll() → reverse priority]
```

---

## Hot-Update Assembly List

HYB-03 established 10 hot-update assemblies (single source of truth: `ProjectSettings/HybridCLRSettings.asset`):

`Boot`, `Core`, `General`, `Project`, `Pool`, `Cache`, `Event`, `Asset`, `Log`, `RuntimeLog`

AOT-only: `Launcher` (never in hotUpdateAssemblies).

---

## External Dependencies

| Package | Version | Registry | Used by |
|---------|---------|----------|---------|
| VContainer | 1.1.0 | GitHub (hadashiA) | All layers: DI container foundation |
| VContainerSourceGenerator | 1.1.0 | Analyzer DLL | Core/General/Project — compile-time DI code gen |
| UniTask | 2.5.11 | GitHub (Cysharp) | Core (asset system), Pool (dependencies, GameObjectPool) |
| MessagePipe | latest | GitHub (Cysharp) | Core, General, Project — type-safe event bus |
| MessagePipe.VContainer | latest | GitHub (Cysharp) | Core, General, Project — VContainer integration |
| MessagePipe.Analyzer | 1.8.2 | Analyzer DLL | MessagePipe diagnostics |
| YooAsset | 3.0 | GitHub (tuyoogame) UPM git | Core — asset management pipeline |
| ZLogger | 2.5.10 | GitHub UPM + NuGetForUnity (Cysharp) | Core — structured logging backend |
| ZLinq | 1.5.6 | GitHub UPM + NuGetForUnity (Cysharp) | Core, General — zero-allocation LINQ |
| ZString | 2.6.0 | GitHub UPM (Cysharp) | Hot paths — low-allocation string building |

---

## Scene Objects / Startup Assets

| Asset | Purpose |
|-------|---------|
| `Assets/GameRes/Scene/Boot/Main.unity` | Minimal boot scene containing AOT `Entry` |
| `Assets/Resources/AssetConfig.asset` | Minimal Resources config loaded by Boot before formal Core startup |
| `Assets/GameRes/HotUpdate/Dlls/` | YooAsset RawFile input: `Core.dll.bytes`, `General.dll.bytes`, `Project.dll.bytes` |
| `Assets/GameRes/HotUpdate/AotMetadata/` | YooAsset RawFile input: `mscorlib.dll.bytes`, `System.dll.bytes`, `System.Core.dll.bytes` |

`Assets/Resources/` is reserved for minimal boot configuration only.

---

## Directories Not Yet Created / Modules Not Yet Implemented

- `Core/Timer/` — tick-based timer system
- `Core/UI/` — UIManager + UIWindow
- `Core/Network/` — NetManager, Session, MessageRouter, Protobuf
- `General/Config/` — Luban config manager
- `General/Audio/` — AudioManager
- `General/RedDot/` — RedDot system
- `General/Guide/` — Guide system
- `General/L10N/` — Localization
- `General/Login/` — Login business model

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
| Hot-update boundary | `.planning/HOT_UPDATE_BOUNDARY.md` |
| AI runtime logging | `.planning/AI_RUNTIME_LOGGING.md` |
| Build pipeline workflow | `ProgressDoc/Result/hybridclr_workflow.md` §4 |
| Build pipeline design & impl | `ProgressDoc/Discuss/Hy3_构建打包全流程管线_需求分析与设计.md` |

---

*End of Codemap — aligned to code as of 2026-07-08*
