# DI Event Knowledge Graph

**Scope:** VContainer + MessagePipe integration for KJ Unity Framework
**Updated:** 2026-06-28
**Purpose:** 快速查阅依赖注入、事件系统、启动链路、订阅清理和 AI 触发关键词

---

## 1. Core Concepts

### VContainer
- Role: dependency injection container for app composition
- In this project: Boot only builds the container, Core/General/Project register services
- Main API shapes:
  - `LifetimeScope` for scene bootstrap
  - `IContainerBuilder` for registration
  - `Register<TService, TImplementation>()`
  - `RegisterEntryPoint<T>()`
  - `AsImplementedInterfaces()`

### MessagePipe
- Role: event bus and request/response pipeline foundation
- In this project: framework event layer wraps MessagePipe, but keeps KJ-specific `EventId` and `FireUntil`
- Main API shapes:
  - `MessageBroker<T>` / `MessageBroker<TKey, TMessage>`
  - `IPublisher<T>` / `ISubscriber<T>`
  - `IMessageHandler<T>`
  - `MessagePipeOptions`
  - `MessagePipeDiagnosticsInfo`

---

## 2. Project Wiring Map

### Startup Chain
`Entry` -> `GameLifetimeScope.Configure()` -> `CoreContainerRegistration.RegisterCoreServices()` -> `SystemManager` entry point -> registered systems init

### Event Chain
`IEventSystem` -> `EventSystem` -> `MessagePipe.MessageBroker<EventEnvelope>`

### Shutdown Chain
`SystemManager.Dispose()` -> `ShutdownAll()` -> `IEventSystem.Clear()` -> unsubscribe and dispose tracked handles

---

## 3. File Map

### Boot Layer
- `[Entry.cs](../../Assets/Scripts/Boot/Entry.cs)`
- `[GameLifetimeScope.cs](../../Assets/Scripts/Boot/GameLifetimeScope.cs)`

### Core Layer
- `[SystemManager.cs](../../Assets/Scripts/Core/SystemManager.cs)`
- `[StartupProbeSystem.cs](../../Assets/Scripts/Core/StartupProbeSystem.cs)`
- `[CoreContainerRegistration.cs](../../Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs)`
- `[IAppBootstrapper.cs](../../Assets/Scripts/Core/Bootstrap/IAppBootstrapper.cs)`

### Event Layer
- `[EventId.cs](../../Assets/Scripts/Core/Events/EventId.cs)`
- `[IEventSystem.cs](../../Assets/Scripts/Core/Events/IEventSystem.cs)`
- `[EventSystem.cs](../../Assets/Scripts/Core/Events/EventSystem.cs)`

### Packages
- `Packages/manifest.json`
- `Assets/packages.config`
- `Assets/Packages/MessagePipe.1.1.0/`

---

## 4. Trigger Keywords For AI

### VContainer Triggers
Use this when the user mentions:
- `VContainer`
- `DI`
- `依赖注入`
- `LifetimeScope`
- `RegisterEntryPoint`
- `RegisterCoreServices`
- `Boot 最小依赖`
- `容器启动`
- `应用内重启`

### MessagePipe Triggers
Use this when the user mentions:
- `MessagePipe`
- `事件系统`
- `EventBus`
- `Publish`
- `Subscribe`
- `FireUntil`
- `事件清理`
- `owner 清理`
- `订阅泄漏`

### Combined Triggers
Use both when the user mentions:
- `启动并加载 system`
- `Boot 层`
- `系统注册`
- `框架重构`
- `事件基石`
- `容器里加载事件`

---

## 5. Usage Rules

### VContainer Rules
1. Boot only creates the lifetime scope and calls core registration.
2. Core/General/Project own service registration.
3. Keep Boot dependencies minimal for hot-update friendliness.
4. Prefer entry points for framework runners like `SystemManager`.

### MessagePipe Rules
1. Keep KJ-facing API stable via `IEventSystem`.
2. Use `EventId` enum, not strings.
3. Track owner subscriptions for cleanup.
4. Preserve `FireUntil` as framework semantic even if transport changes.

---

## 6. Common Search Hints

- Startup bug: search `GameLifetimeScope`, `RegisterCoreServices`, `SystemManager`
- Event leak: search `owner`, `Clear()`, `UnsubscribeOwner`
- MessagePipe integration: search `MessageBroker`, `MessagePipeOptions`, `IMessageHandler`
- DI wiring: search `LifetimeScope`, `IContainerBuilder`, `RegisterEntryPoint`

---

## 7. Known Constraints

- Boot must stay minimal and avoid business dependency graphs.
- MessagePipe dependency packages must be restored before Unity loads the DLL.
- `EventSystem` should stay thin; do not rebuild a parallel event bus.
- Shutdown order matters for application restart: systems first, container second, scene last.

---

## 8. Quick Reference

- Boot entry file: `Assets/Scripts/Boot/Entry.cs`
- Container scope: `Assets/Scripts/Boot/GameLifetimeScope.cs`
- Core registration: `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs`
- Event facade: `Assets/Scripts/Core/Events/EventSystem.cs`
- Event contract: `Assets/Scripts/Core/Events/IEventSystem.cs`
