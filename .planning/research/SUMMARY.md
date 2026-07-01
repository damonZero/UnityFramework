# Research Summary: KJ Unity Framework

**Date:** 2026-06-28
**Sources:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md, DI_EVENT_KNOWLEDGE_GRAPH.md

---

## 0. Quick Index

- [DI / Event Knowledge Graph](DI_EVENT_KNOWLEDGE_GRAPH.md)

---

## 1. Technology Stack (Verified Versions)

| Component | Technology | Version | Confidence |
|-----------|-----------|---------|------------|
| Engine | Unity | 2022.3.62f2 LTS | HIGH |
| Hot Update | HybridCLR | main branch (no tags) | MEDIUM — planned, not yet installed |
| Config Tables | Luban | v4.10.1 | MEDIUM — planned, not yet installed |
| Network Serialization | Google.Protobuf | 3.35.1 (netstandard2.0) | MEDIUM — planned, not yet installed |
| Async/Await | UniTask | v2.5.11 | HIGH |
| Tweening | DOTween | Latest (Asset Store) | MEDIUM |
| UI | UGUI (built-in) | Unity 2022.3 built-in | HIGH |
| Audio | Unity Audio (built-in) | Unity 2022.3 built-in | HIGH |
| Dependency Injection | VContainer | 1.1.0 | HIGH |
| Event Bus | MessagePipe | latest (UPM) | HIGH |
| Asset Management | YooAsset | 3.0 (UPM) | HIGH |

**Installation channels:**
- UPM (manifest.json): UniTask, VContainer, MessagePipe, MessagePipe.VContainer, YooAsset
- UPM (planned, not yet installed): HybridCLR
- NuGet (NuGetForUnity, analyzers only): MessagePipe.Analyzer, VContainerSourceGenerator
- NuGet/Plugins (planned): Google.Protobuf.dll
- Asset Store (planned): DOTween
- External tools (planned): Luban CLI, protoc

---

## 2. Features: Table Stakes vs Differentiators

### Table Stakes (must-have for any project)

| Feature | Complexity | Notes |
|---------|-----------|-------|
| Event System | Medium | DONE — MessagePipe + [GameEvent] attribute scanning |
| Resource Manager | High | DONE — YooAsset 3.0 封装为 AssetSystem, owned/cached 双通道 |
| Network Module | High | PLANNED — Session management, Protobuf serialization, heartbeat, message routing, TCP + WebSocket. |
| UI Framework | Medium-High | PLANNED — Window lifecycle, layer management (6 layers), async prefab loading, UI stack. |
| Config Table System | Medium | PLANNED — Luban integration, auto-generated classes, fast ID lookup. |
| Audio Manager | Low-Medium | PLANNED — BGM/SFX/Voice channels, volume control, source pooling. |
| Singleton/ISystem Infrastructure | Low | DONE — VContainer + ISystem + [CoreSystem] + AsImplementedInterfaces() |

### Common Modules (expected in mature framework)

| Feature | Complexity | Notes |
|---------|---------|-------|
| Object Pool | Low-Medium | Framework/Pool + Framework/Cache 代码已完成，缺 Core/Pool/PoolService.cs DI 桥接。 |
| Red Dot System | Medium | Tree-structured nodes, event-driven propagation, dirty-flag optimization. |
| Guide/Tutorial System | Medium-High | Step-based state machine, config-driven, event-triggered transitions. |
| Timer System | Low-Medium | One-shot + looping, pause/resume, tick-based (not coroutine). |
| Localization | Medium | Key-value lookup, runtime switching, Luban integration. Can defer. |

### Differentiators (set this framework apart)

1. **HybridCLR Hot Update** -- C# hot update across all IL2CPP platforms including iOS. Most competitors still use Lua.
2. **Boot/Core/General/Project Layered Architecture** -- Compile-time enforced boundaries via .asmdef. Most Unity frameworks are monolithic.
3. **Message/Protocol Auto-Generation** -- .proto files to handler registration automatically.

---

## 3. Architecture Approach

### Four-Layer Model

```
Boot  (entry point, HybridCLR bootstrap, startup flow)
  -> Core  (EventSystem, NetManager, AssetSystem, UIManager, ObjectPoolManager)
    -> General  (ConfigManager, AudioManager, RedDotManager, GuideManager)
      -> Project  (game-specific UI, logic, business flows)
```

**Dependency rule:** Strictly downward only. Each layer is a separate .asmdef. No circular references.

### Module Lifecycle

- `SystemManager` is the main system lifecycle driver.
- `ISystem` (Core) and `IModel` (General/Project) are the two lifecycle contracts.
- `[CoreSystem]` / `[Model]` attributes enable automatic container registration via reflection.
- `VContainer` owns object composition and startup wiring.
- `AsImplementedInterfaces()` ensures `IAssetSystem` etc. are resolvable via DI.

---

## 4. Critical Pitfalls to Avoid

### Critical (cause rewrites)

| # | Pitfall | Prevention |
|---|---------|------------|
| 1 | **Boot dependency creep** -- Boot starts owning business registrations | Keep Boot layer minimal. Move features into Core/General/Project |
| 2 | **Event bus rewrite drift** -- framework creates a parallel event system | Keep one base (`MessagePipe`); use `[GameEvent]` + `IPublisher<T>` / `ISubscriber<T>` |
| 3 | **Subscription leaks** -- destroyed owners still hold handlers | Dispose subscription tokens in Shutdown() / OnDestroy() |
| 4 | **Layer violation** -- Core depends on General, General depends on Project | Compile-time enforcement via .asmdef references |

---

## 5. Dependencies (Prerequisite Order)

Individual modules declare what they depend on; there is no fixed execution schedule.

Key dependency edges:
```
AssetSystem ← ISystem
Timer ← ISystem
ObjectPool ← AssetSystem
UIManager ← ISystem + AssetSystem
ConfigManager (Luban) ← AssetSystem
AudioManager ← AssetSystem + ObjectPool
NetManager ← ISystem + MessagePipe
MessageRouter ← MessagePipe + Protobuf
RedDot ← MessagePipe
Guide ← MessagePipe + UIManager + ConfigManager
Localization ← ConfigManager + AssetSystem
HybridCLR ← AssetSystem + Boot
```

See [ROADMAP.md](../ROADMAP.md) for per-module status.

---

## 6. Research Documents

- [STACK.md](STACK.md)
- [FEATURES.md](FEATURES.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [PITFALLS.md](PITFALLS.md)
- [DI_EVENT_KNOWLEDGE_GRAPH.md](DI_EVENT_KNOWLEDGE_GRAPH.md)

---

## Sources

- HybridCLR: https://github.com/focus-creative-games/hybridclr
- Luban: https://github.com/focus-creative-games/luban
- UniTask: https://github.com/Cysharp/UniTask
- Google.Protobuf: https://www.nuget.org/packages/Google.Protobuf
- MessagePipe: https://github.com/Cysharp/MessagePipe
- VContainer: https://github.com/hadashiA/VContainer
