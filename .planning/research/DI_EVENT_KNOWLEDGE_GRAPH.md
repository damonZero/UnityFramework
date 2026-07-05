# DI Event Knowledge Graph

**Scope:** VContainer + MessagePipe integration for Unity Framework  
**Updated:** 2026-07-05  
**Purpose:** 快速查阅依赖注入、事件系统、启动链路和 AI 触发关键词

---

## 1. Core Concepts

### VContainer
- Role: dependency injection container for app composition
- In this project: Boot does not build the formal container. Boot updates resources/code, then reflects into ProjectStartup. ProjectLifetimeScope builds the formal Core → General → Project container.
- Main API shapes:
  - `ProjectStartup.Start(IAssetRuntime)` creates the formal `ProjectLifetimeScope`
  - `CoreStartupContext` carries `IContainerBuilder`, `IAssetRuntime`, and `MessagePipeOptions` between Core/General/Project registration stages
  - `CoreBootstrapStage.Configure(CoreStartupContext)` / `GeneralBootstrapStage.Configure(CoreStartupContext)` / `ProjectBootstrapStage.Configure(CoreStartupContext)`
  - `[CoreSystem]` attribute + reflection scanning → `Register(type, Singleton).AsSelf().AsImplementedInterfaces()`
  - `SystemManager` as `IStartable` entry point — VContainer drives its lifecycle

### MessagePipe
- Role: type-safe pub/sub event pipeline
- In this project: structs marked with `Framework.Event.GameEventAttribute` are scanned by shared Framework event scanning helpers, then registered as MessagePipe brokers by Core/General container stages.
- Main API shapes:
  - `IPublisher<T>` / `ISubscriber<T>` — inject via constructor
  - `[GameEvent]` attribute from `Framework.Event` — marks a struct for auto-registration
  - `MessagePipeOptions` — passed through `CoreStartupContext` between stages
  - `IDisposable` subscription token — caller owns cleanup

---

## 2. Project Wiring Map

### Startup Chain
`Entry` → `BootUpdateRunner` initializes `Framework.Asset` and updates resources/code → loads AOT metadata + hot-update DLLs → reflects `Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)` → creates `ProjectLifetimeScope` → `CoreBootstrapStage.Configure()` → `builder.RegisterCoreServices(assetRuntime)` → `GeneralBootstrapStage.Configure()` → `ProjectBootstrapStage.Configure()` → `CoreTypeRegistration.RegisterCoreTypes()` scans `[CoreSystem]` + `[GameEvent]` → `builder.RegisterEntryPoint<SystemManager>()` → VContainer calls `SystemManager.Start()` → `InitAll()` → sorted by Priority

### Event Chain
`[GameEvent] struct` → auto-registered at container build time → `IPublisher<T>.Publish()` / `ISubscriber<T>.Subscribe()` at runtime

### Shutdown Chain
`SystemManager.Dispose()` → `ShutdownAll()` (reverse priority order) → each system's `Shutdown()` disposes its subscription tokens → `Core.AssetSystem.Shutdown()` → `Framework.Asset.AssetRuntime.Shutdown()` → `YooAssets.Destroy()`

---

## 3. File Map

### Boot Layer
- `[Entry.cs](../../Assets/Scripts/Boot/Entry.cs)` — root MonoBehaviour, `DontDestroyOnLoad`
- `[BootStartupSettings.cs](../../Assets/Scripts/Boot/BootStartupSettings.cs)` — Entry serialized startup/update settings
- `[BootUpdateRunner.cs](../../Assets/Scripts/Boot/BootUpdateRunner.cs)` — resource/code update, metadata/DLL loading, reflection startup
- `[BootAssemblyEntry.cs](../../Assets/Scripts/Boot/BootAssemblyEntry.cs)` — hot-update DLL entry
- `[BootMetadataEntry.cs](../../Assets/Scripts/Boot/BootMetadataEntry.cs)` — AOT metadata entry
- `[HybridClrReflection.cs](../../Assets/Scripts/Boot/HybridClrReflection.cs)` — reflection wrapper for HybridCLR.RuntimeApi

### Core Layer
- `[ISystem.cs](../../Assets/Scripts/Core/Systems/ISystem.cs)` — `ISystem` / `ITickableSystem`
- `[SystemManager.cs](../../Assets/Scripts/Core/Systems/SystemManager.cs)` — lifecycle driver
- `[CoreContainerRegistration.cs](../../Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs)` — `RegisterCoreServices()` entry
- `[CoreStartupContext.cs](../../Assets/Scripts/Core/Bootstrap/CoreStartupContext.cs)` — registration context for Core/General/Project
- `[CoreBootstrapStage.cs](../../Assets/Scripts/Core/Bootstrap/CoreBootstrapStage.cs)` — Core registration stage
- `[CoreTypeRegistration.cs](../../Assets/Scripts/Core/Bootstrap/CoreTypeRegistration.cs)` — `[CoreSystem]` scanner + MessagePipe broker registration
- `[CoreSystemAttribute.cs](../../Assets/Scripts/Core/Systems/Attributes/CoreSystemAttribute.cs)` — marker attribute
- `[AssetSystem.cs](../../Assets/Scripts/Core/Asset/AssetSystem.cs)` — Framework.Asset lifecycle orchestration (example [CoreSystem])

### Framework Layer
- `[GameEventAttribute.cs](../../Assets/Framework/Event/GameEventAttribute.cs)` — unified event marker
- `[GameEventTypeScanner.cs](../../Assets/Framework/Event/GameEventTypeScanner.cs)` — shared event type scanner and validator
- `[IAssetSystem.cs](../../Assets/Framework/Asset/IAssetSystem.cs)` — stable asset API for upper layers
- `[AssetRuntime.cs](../../Assets/Framework/Asset/AssetRuntime.cs)` — YooAsset adapter implementation
- `[AssetDownloadHandle.cs](../../Assets/Framework/Asset/AssetDownloadHandle.cs)` — downloader wrapper that hides YooAsset types

### Event Layer (Core)
- `[AppStartedEvent.cs](../../Assets/Scripts/Core/Systems/Events/AppStartedEvent.cs)` — published after all Core systems init successfully
- `[AppShuttingDownEvent.cs](../../Assets/Scripts/Core/Systems/Events/AppShuttingDownEvent.cs)` — published before shutdown
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
- `LifetimeScope`, `ProjectLifetimeScope`
- `IContainerBuilder`, `RegisterEntryPoint`
- `RegisterCoreServices`, `RegisterBusinessLayer`
- `[CoreSystem]`, `[Model]`
- `Boot 最小依赖`, `容器启动`, `ProjectStartup`

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
1. Boot updates resources/code and reflects into ProjectStartup; it does not create the formal VContainer root.
2. Core/General/Project own their service registration via `CoreStartupContext` stages.
3. Keep Boot dependencies minimal (Boot.asmdef references only Framework.Asset).
4. Prefer `[CoreSystem]` attribute + reflection scanning for Core systems; `[Model]` for business models.
5. `AsImplementedInterfaces()` automatically registers `IAssetSystem` etc.

### MessagePipe Rules
1. Events are `readonly struct` marked with `[GameEvent]`.
2. Subscribe with `ISubscriber<T>.Subscribe(handler)`; save the returned `IDisposable`.
3. Dispose the subscription token in `ISystem.Shutdown()` or `OnDestroy()`.
4. Core and General/Project share the same `Framework.Event.GameEventAttribute`; registration scope comes from the assemblies passed to each stage.

---

## 6. Common Search Hints

- Startup bug: search `BootUpdateRunner`, `ProjectStartup`, `ProjectLifetimeScope`, `CoreBootstrapStage`, `SystemManager.Start`
- Event leak: search `IDisposable`, `subscription`, `Shutdown`
- DI wiring: search `RegisterCoreServices`, `RegisterBusinessLayer`, `RegisterCoreTypes`
- Asset loading: search `Framework.Asset`, `AssetRuntime`, `IAssetSystem`, `AssetSystem`

---

## 7. Known Constraints

- Boot must stay minimal and avoid business dependency graphs.
- MessagePipe packages must be restored before Unity compiles the assemblies.
- `SystemManager` should stay thin; systems register their own lifecycle.
- Shutdown order: systems by reverse priority (handled by SystemManager), container, scene.
- YooAsset types stay inside `Framework.Asset`; upper layers use `Framework.Asset.IAssetSystem`.
- Framework modules do not reference `Assets/Scripts`; Core owns VContainer/MessagePipe registration.

---

## 8. Quick Reference

- Boot entry: `Assets/Scripts/Boot/Entry.cs`
- Startup update runner: `Assets/Scripts/Boot/BootUpdateRunner.cs`
- Container chain entry: `Assets/Scripts/Project/Bootstrap/ProjectLifetimeScope.cs`
- Core registration: `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs`
- System scanner: `Assets/Scripts/Core/Bootstrap/CoreTypeRegistration.cs`
- Event marker: `Assets/Framework/Event/GameEventAttribute.cs`
- Asset API/runtime: `Assets/Framework/Asset/`
- Asset lifecycle bridge: `Assets/Scripts/Core/Asset/AssetSystem.cs`
