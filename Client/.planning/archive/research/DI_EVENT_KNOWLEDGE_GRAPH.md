# DI Event Knowledge Graph

**Scope:** VContainer + MessagePipe integration for Unity Framework
**Updated:** 2026-08-02
**Purpose:** 快速查阅依赖注入、事件系统、启动链路和 AI 触发关键词

> 2026-08-02 更新：同步分层启动链（layered-startup-chain）。旧 `CoreBootstrapStage/GeneralBootstrapStage/ProjectBootstrapStage` 单容器注册已废弃，改为 Core→General→Project 三层嵌套 scope。

---

## 1. Core Concepts

### VContainer
- Role: dependency injection container for app composition
- In this project: Boot does not build the formal container. Boot updates resources/code, then reflects into `Core.Bootstrap.CoreStartup`. `CoreStartup` creates the Core root `LifetimeScope`; General/Project are child scopes (layered startup chain).
- Main API shapes:
  - `CoreStartup.Start(IAssetRuntime)` creates the Core root scope
  - `CoreLayerEntrypoint` (IPostStartable) reflects `GeneralStartup.Start(coreScope)` after Core systems init
  - `GeneralStartup.Start(LifetimeScope)` creates General child scope via `CreateChild`
  - `GeneralLayerEntrypoint` (IPostStartable) reflects `ProjectStartup.Start(generalScope)` after General models load
  - `ProjectStartup.Start(LifetimeScope)` creates Project child scope
  - `LayerStartupReflector` — shared reflection helper for layer-to-layer startup
  - `[CoreSystem]` attribute + reflection scanning → `Register(type, Singleton).AsSelf().AsImplementedInterfaces()`
  - `SystemManager` as `IStartable` entry point — VContainer drives its lifecycle

### MessagePipe
- Role: type-safe pub/sub event pipeline
- In this project: structs marked with `Framework.Event.GameEventAttribute` are scanned by shared Framework event scanning helpers, then registered as MessagePipe brokers per layer. **Single message domain**: `RegisterMessagePipe()` called only in Core scope; General/Project resolve the same `MessagePipeOptions` and register only their own brokers.
- Main API shapes:
  - `IPublisher<T>` / `ISubscriber<T>` — inject via constructor
  - `[GameEvent]` attribute from `Framework.Event` — marks a struct for auto-registration
  - `MessagePipeOptions` — resolved from Core container by General/Project startup
  - `IDisposable` subscription token — caller owns cleanup

---

## 2. Project Wiring Map

### Startup Chain
`Entry` → `BootUpdateRunner` initializes `Framework.Asset` and updates resources/code → loads AOT metadata + hot-update DLLs → reflects `Core.Bootstrap.CoreStartup.Start(IAssetRuntime)` → creates `CoreLifetimeScope` → `CoreContainerRegistration.RegisterCoreServices()` → `RegisterMessagePipe()` + `RegisterCoreTypes()` + `RegisterEntryPoint<SystemManager>()` + `RegisterEntryPoint<CoreLayerEntrypoint>()` → VContainer calls `SystemManager.Start()` → `InitAll()` → sorted by Priority → `CoreLayerEntrypoint.PostStart()` → reflects `GeneralStartup.Start(coreScope)` → `CreateChild<GeneralLifetimeScope>()` → registers General events/models/ModelLifecycle/GeneralLayerEntrypoint → `ModelLifecycle.PostStart()` loads General models → `GeneralLayerEntrypoint.PostStart()` → reflects `ProjectStartup.Start(generalScope)` → `CreateChild<ProjectLifetimeScope>()` → registers Project layer → `ProjectLayerEntrypoint.PostStart()` marks Project ready

### Event Chain
`[GameEvent] struct` → auto-registered at container build time → `IPublisher<T>.Publish()` / `ISubscriber<T>.Subscribe()` at runtime. Events flow upward (child scopes can subscribe to parent-scope events).

### Shutdown Chain
VContainer disposes nested scopes reverse-order: Project scope → General scope → Core scope. `SystemManager.Dispose()` → `ShutdownAll()` (reverse priority order) → each system's `Shutdown()` disposes its subscription tokens → `Core.AssetSystem.Shutdown()` → `Framework.Asset.AssetRuntime.Shutdown()` → `YooAssets.Destroy()`. `ModelLifecycle.Dispose()` → `UnloadAll()` (reverse load order).

---

## 3. File Map

### Boot Layer
- `[Entry.cs](../../Assets/Scripts/Boot/Launcher/Entry.cs)` — root MonoBehaviour, `DontDestroyOnLoad`; `Repair()` reflects `CoreStartup.Reset()` to rebuild
- `[BootStartupSettings.cs](../../Assets/Scripts/Boot/Launcher/Data/BootStartupSettings.cs)` — Entry serialized startup/update settings (default entry `Core.Bootstrap.CoreStartup, Core`)
- `[BootUpdateRunner.cs](../../Assets/Scripts/Boot/BootUpdateRunner.cs)` — resource/code update, metadata/DLL loading, reflection startup, catch → Repair UI
- `[BootAssemblyEntry.cs](../../Assets/Scripts/Boot/Launcher/Data/BootAssemblyEntry.cs)` — hot-update DLL entry
- `[BootMetadataEntry.cs](../../Assets/Scripts/Boot/Launcher/Data/BootMetadataEntry.cs)` — AOT metadata entry
- `[BootLoader.cs](../../Assets/Scripts/Boot/Launcher/BootLoader.cs)` — AOT startup shell (YooAsset + HybridCLR + reflection)

### Core Layer
- `[ISystem.cs](../../Assets/Scripts/Core/Systems/ISystem.cs)` — `ISystem` / `ITickableSystem`
- `[SystemManager.cs](../../Assets/Scripts/Core/Systems/SystemManager.cs)` — lifecycle driver
- `[CoreContainerRegistration.cs](../../Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs)` — `RegisterCoreServices()` entry
- `[CoreStartup.cs](../../Assets/Scripts/Core/Bootstrap/CoreStartup.cs)` — Boot-reflected entry, creates Core root scope; `Reset()` for Repair
- `[CoreLifetimeScope.cs](../../Assets/Scripts/Core/Bootstrap/CoreLifetimeScope.cs)` — Core root LifetimeScope
- `[CoreLayerEntrypoint.cs](../../Assets/Scripts/Core/Bootstrap/CoreLayerEntrypoint.cs)` — IPostStartable, reflects GeneralStartup
- `[LayerStartupReflector.cs](../../Assets/Scripts/Core/Bootstrap/LayerStartupReflector.cs)` — shared reflection helper
- `[CoreStartupContext.cs](../../Assets/Scripts/Core/Bootstrap/CoreStartupContext.cs)` — registration context (IContainerBuilder + MessagePipeOptions)
- `[CoreBootstrapStage.cs](../../Assets/Scripts/Core/Bootstrap/CoreBootstrapStage.cs)` — Core registration stage (called by CoreLifetimeScope)
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
- `[GeneralStartup.cs](../../Assets/Scripts/General/Bootstrap/GeneralStartup.cs)` — Core-reflected entry, creates General child scope
- `[GeneralLifetimeScope.cs](../../Assets/Scripts/General/Bootstrap/GeneralLifetimeScope.cs)` — General child scope
- `[GeneralLayerEntrypoint.cs](../../Assets/Scripts/General/Bootstrap/GeneralLayerEntrypoint.cs)` — IPostStartable, reflects ProjectStartup
- `[GeneralContainerRegistration.cs](../../Assets/Scripts/General/Bootstrap/GeneralContainerRegistration.cs)` — `RegisterBusinessEvents` / `RegisterModels` / `RegisterModelLifecycle`
- `[IModel.cs](../../Assets/Scripts/General/Models/IModel.cs)` — business layer lifecycle
- `[ModelLifecycle.cs](../../Assets/Scripts/General/Models/ModelLifecycle.cs)` — `IReadOnlyList<Type>` + `IObjectResolver` contract, sorts by priority
- `[ModelScanner.cs](../../Assets/Scripts/General/Models/ModelScanner.cs)` — assembly filter scanning `[Model]` → `Type[]`
- `[IModelStartupStatus.cs](../../Assets/Scripts/General/Models/IModelStartupStatus.cs)` — model load status query

### Project Layer
- `[ProjectStartup.cs](../../Assets/Scripts/Project/Bootstrap/ProjectStartup.cs)` — General-reflected entry, creates Project child scope
- `[ProjectLifetimeScope.cs](../../Assets/Scripts/Project/Bootstrap/ProjectLifetimeScope.cs)` — Project child scope
- `[ProjectLayerEntrypoint.cs](../../Assets/Scripts/Project/Bootstrap/ProjectLayerEntrypoint.cs)` — IPostStartable, marks Project ready

### Packages
- `Packages/manifest.json`
- `Assets/Packages/MessagePipe.Analyzer.1.8.2/`
- `Assets/Packages/VContainerSourceGenerator.1.1.0/`

---

## 4. Trigger Keywords For AI

### VContainer Triggers
Use this when the user mentions:
- `VContainer`, `DI`, `依赖注入`
- `LifetimeScope`, `CoreLifetimeScope`, `ProjectLifetimeScope`
- `IContainerBuilder`, `RegisterEntryPoint`
- `RegisterCoreServices`, `RegisterBusinessEvents`, `RegisterModels`, `RegisterModelLifecycle`
- `[CoreSystem]`, `[Model]`
- `Boot 最小依赖`, `容器启动`, `CoreStartup`, `ProjectStartup`

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
- `分层启动链`, `CoreLayerEntrypoint`, `GeneralLayerEntrypoint`
- `SystemManager`

---

## 5. Usage Rules

### VContainer Rules
1. Boot updates resources/code and reflects into `CoreStartup`; it does not create the formal VContainer root.
2. Each layer (Core/General/Project) owns its scope and registration; Core is root, General/Project are children.
3. Keep Boot dependencies minimal (Boot.asmdef references Asset/Log/RuntimeLog/UniTask/AssetShared/YooAsset/Launcher).
4. Prefer `[CoreSystem]` attribute + reflection scanning for Core systems; `[Model]` for business models.
5. `AsImplementedInterfaces()` automatically registers `IAssetSystem` etc.

### MessagePipe Rules
1. Events are `readonly struct` marked with `[GameEvent]`.
2. Subscribe with `ISubscriber<T>.Subscribe(handler)`; save the returned `IDisposable`.
3. Dispose the subscription token in `ISystem.Shutdown()` or `OnDestroy()`.
4. **Single message domain**: `RegisterMessagePipe()` only in Core; General/Project resolve the same options and register only their own brokers. Child-scope events are visible to parent-scope subscribers; not vice versa.

---

## 6. Common Search Hints

- Startup bug: search `BootUpdateRunner`, `CoreStartup`, `CoreLayerEntrypoint`, `GeneralStartup`, `GeneralLayerEntrypoint`, `SystemManager.Start`
- Event leak: search `IDisposable`, `subscription`, `Shutdown`
- DI wiring: search `RegisterCoreServices`, `RegisterBusinessEvents`, `RegisterModels`, `RegisterCoreTypes`
- Model lifecycle: search `ModelLifecycle`, `ModelScanner`, `IModelStartupStatus`
- Asset loading: search `Framework.Asset`, `AssetRuntime`, `IAssetSystem`, `AssetSystem`

---

## 7. Known Constraints

- Boot must stay minimal and avoid business dependency graphs.
- MessagePipe packages must be restored before Unity compiles the assemblies.
- `SystemManager` should stay thin; systems register their own lifecycle.
- Shutdown order: Project scope → General scope → Core scope (VContainer nested dispose), systems by reverse priority.
- YooAsset types stay inside `Framework.Asset`; upper layers use `Framework.Asset.IAssetSystem`.
- Framework modules do not reference `Assets/Scripts`; Core owns VContainer/MessagePipe registration.
- Dependency direction: Core → General → Project (one-way). Core doesn't reference General/Project; General doesn't reference Project (layer-to-layer via reflection).

---

## 8. Quick Reference

- Boot entry: `Assets/Scripts/Boot/Launcher/Entry.cs`
- Startup update runner: `Assets/Scripts/Boot/BootUpdateRunner.cs`
- Core scope entry: `Assets/Scripts/Core/Bootstrap/CoreStartup.cs`
- General scope entry: `Assets/Scripts/General/Bootstrap/GeneralStartup.cs`
- Project scope entry: `Assets/Scripts/Project/Bootstrap/ProjectStartup.cs`
- Core registration: `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs`
- System scanner: `Assets/Scripts/Core/Bootstrap/CoreTypeRegistration.cs`
- Model scanner: `Assets/Scripts/General/Models/ModelScanner.cs`
- Event marker: `Assets/Framework/Event/GameEventAttribute.cs`
- Asset API/runtime: `Assets/Framework/Asset/`
- Asset lifecycle bridge: `Assets/Scripts/Core/Asset/AssetSystem.cs`
