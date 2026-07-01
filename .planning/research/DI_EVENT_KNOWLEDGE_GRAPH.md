# DI Event Knowledge Graph

**Scope:** VContainer + MessagePipe integration for Unity Framework  
**Updated:** 2026-06-30  
**Purpose:** 快速查阅依赖注入、事件系统、启动链路和 AI 触发关键词

---

## 1. Core Concepts

### VContainer
- Role: dependency injection container for app composition
- In this project: each layer (Boot → Core → General → Project) registers its own services progressively
- Main API shapes:
  - `AppLifetimeScope` for prefab-chain bootstrap
  - `BootstrapContext` carries `IContainerBuilder` + `MessagePipeOptions` between stages
  - `IBootstrapStage.Configure(BootstrapContext)` — each layer's registration hook
  - `[CoreSystem]` attribute + reflection scanning → `Register(type, Singleton).AsSelf().AsImplementedInterfaces()`
  - `SystemManager` as `IStartable` entry point — VContainer drives its lifecycle

### MessagePipe
- Role: type-safe pub/sub event pipeline
- In this project: `[GameEvent]` structs scanned by `ArchitectureContainerRegistration`, registered as `MessageBroker<T>`
- Main API shapes:
  - `IPublisher<T>` / `ISubscriber<T>` — inject via constructor
  - `[GameEvent]` attribute — marks a struct for auto-registration
  - `MessagePipeOptions` — passed through `BootstrapContext` between stages
  - `IDisposable` subscription token — caller owns cleanup

---

## 2. Project Wiring Map

### Startup Chain
`BootLifetimeScope` → `BootstrapContext.ConfigurePrefab("Core")` → `CoreBootstrapStage.Configure()` → `builder.RegisterCoreServices()` → `CoreContainerRegistration.RegisterArchitecture()` scans `[CoreSystem]` + `[GameEvent]` → `builder.RegisterEntryPoint<SystemManager>()` → VContainer calls `SystemManager.Start()` → `InitAll()` → sorted by Priority

### Event Chain
`[GameEvent] struct` → auto-registered at container build time → `IPublisher<T>.Publish()` / `ISubscriber<T>.Subscribe()` at runtime

### Shutdown Chain
`SystemManager.Dispose()` → `ShutdownAll()` (reverse priority order) → each system's `Shutdown()` disposes its subscription tokens → `YooAssets.Destroy()`

---

## 3. File Map

### Boot Layer
- `[Entry.cs](../../Assets/Scripts/Boot/Entry.cs)` — root MonoBehaviour, `DontDestroyOnLoad`
- `[AppLifetimeScope.cs](../../Assets/Scripts/Boot/AppLifetimeScope.cs)` — abstract base
- `[BootLifetimeScope.cs](../../Assets/Scripts/Boot/Bootstrap/BootLifetimeScope.cs)` — creates BootstrapContext, starts prefab chain
- `[BootstrapContext.cs](../../Assets/Scripts/Boot/Bootstrap/BootstrapContext.cs)` — stage context + chain driver
- `[IBootstrapStage.cs](../../Assets/Scripts/Boot/Bootstrap/IBootstrapStage.cs)` — stage protocol

### Core Layer
- `[ISystem.cs](../../Assets/Scripts/Core/ISystem.cs)` — `ISystem` / `ITickableSystem`
- `[SystemManager.cs](../../Assets/Scripts/Core/SystemManager.cs)` — lifecycle driver
- `[CoreContainerRegistration.cs](../../Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs)` — `RegisterCoreServices()` entry
- `[CoreBootstrapStage.cs](../../Assets/Scripts/Core/Bootstrap/CoreBootstrapStage.cs)` — stage implementation
- `[ArchitectureContainerRegistration.cs](../../Assets/Scripts/Core/Architecture/Bootstrap/ArchitectureContainerRegistration.cs)` — `[CoreSystem]` + `[GameEvent]` scanner
- `[CoreSystemAttribute.cs](../../Assets/Scripts/Core/Architecture/Attributes/CoreSystemAttribute.cs)` — marker attribute
- `[GameEventAttribute.cs](../../Assets/Scripts/Core/Architecture/Events/GameEventAttribute.cs)` — Core event marker
- `[AssetSystem.cs](../../Assets/Scripts/Core/Asset/AssetSystem.cs)` — asset loading service (example [CoreSystem])

### Event Layer (Core)
- `[AppStartedEvent.cs](../../Assets/Scripts/Core/Architecture/Events/AppStartedEvent.cs)` — published after all systems init
- `[AppShuttingDownEvent.cs](../../Assets/Scripts/Core/Architecture/Events/AppShuttingDownEvent.cs)` — published before shutdown
- `[AssetSystemReadyEvent.cs](../../Assets/Scripts/Core/Asset/AssetSystemReadyEvent.cs)` — published when asset system is ready

### General Layer
- `[GeneralContainerRegistration.cs](../../Assets/Scripts/General/Bootstrap/GeneralContainerRegistration.cs)` — `RegisterBusinessLayer()` for `[Model]` + `[GameEvent]`
- `[IModel.cs](../../Assets/Scripts/General/Models/IModel.cs)` — business layer lifecycle
- `[ModelLifecycle.cs](../../Assets/Scripts/General/Models/ModelLifecycle.cs)` — sorts by priority, calls Load/Unload

### Packages
- `Packages/manifest.json`
- `Assets/Packages/MessagePipe.Analyzer.1.8.2/`
- `Assets/Packages/VContainerSourceGenerator.1.1.0/`

---

## 4. Trigger Keywords For AI

### VContainer Triggers
Use this when the user mentions:
- `VContainer`, `DI`, `依赖注入`
- `LifetimeScope`, `AppLifetimeScope`
- `IContainerBuilder`, `RegisterEntryPoint`
- `RegisterCoreServices`, `RegisterBusinessLayer`
- `[CoreSystem]`, `[Model]`
- `Boot 最小依赖`, `容器启动`, `prefab 链式启动`

### MessagePipe Triggers
Use this when the user mentions:
- `MessagePipe`, `事件系统`, `EventBus`
- `IPublisher`, `ISubscriber`
- `Publish`, `Subscribe`
- `[GameEvent]`
- `事件清理`, `订阅泄漏`, `subscription token`

### Combined Triggers
Use both when the user mentions:
- `启动并加载 system`, `系统注册`, `框架重构`
- `Boot 层`, `CoreBootstrapStage`
- `SystemManager`

---

## 5. Usage Rules

### VContainer Rules
1. Boot only creates `BootstrapContext` and starts the prefab chain.
2. Core/General/Project own their service registration via `IBootstrapStage`.
3. Keep Boot dependencies minimal (Boot.asmdef references only VContainer).
4. Prefer `[CoreSystem]` attribute + reflection scanning for Core systems; `[Model]` for business models.
5. `AsImplementedInterfaces()` automatically registers `IAssetSystem` etc.

### MessagePipe Rules
1. Events are `readonly struct` marked with `[GameEvent]`.
2. Subscribe with `ISubscriber<T>.Subscribe(handler)`; save the returned `IDisposable`.
3. Dispose the subscription token in `ISystem.Shutdown()` or `OnDestroy()`.
4. Core and General/Project have separate `[GameEvent]` attributes for scoped scanning.

---

## 6. Common Search Hints

- Startup bug: search `BootLifetimeScope`, `BootstrapContext`, `CoreBootstrapStage`, `SystemManager.Start`
- Event leak: search `IDisposable`, `subscription`, `Shutdown`
- DI wiring: search `RegisterCoreServices`, `RegisterBusinessLayer`, `RegisterArchitecture`
- Asset loading: search `AssetSystem`, `AssetInitSystem`, `IAssetSystem`

---

## 7. Known Constraints

- Boot must stay minimal and avoid business dependency graphs.
- MessagePipe packages must be restored before Unity compiles the assemblies.
- `SystemManager` should stay thin; systems register their own lifecycle.
- Shutdown order: systems by reverse priority (handled by SystemManager), container, scene.
- YooAsset types stay inside Core.Asset; upper layers use `IAssetSystem`.

---

## 8. Quick Reference

- Boot entry: `Assets/Scripts/Boot/Entry.cs`
- Container chain entry: `Assets/Scripts/Boot/Bootstrap/BootLifetimeScope.cs`
- Core registration: `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs`
- System scanner: `Assets/Scripts/Core/Architecture/Bootstrap/ArchitectureContainerRegistration.cs`
- Event marker: `Assets/Scripts/Core/Architecture/Events/GameEventAttribute.cs`
- Asset system: `Assets/Scripts/Core/Asset/AssetSystem.cs`
