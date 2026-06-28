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
| Hot Update | HybridCLR | main branch (no tags) | HIGH |
| Config Tables | Luban | v4.10.1 | HIGH |
| Network Serialization | Google.Protobuf | 3.35.1 (netstandard2.0) | HIGH |
| Async/Await | UniTask | v2.5.11 | HIGH |
| Tweening | DOTween | Latest (Asset Store) | MEDIUM |
| UI | UGUI (built-in) | Unity 2022.3 built-in | HIGH |
| Audio | Unity Audio (built-in) | Unity 2022.3 built-in | HIGH |
| Dependency Injection | VContainer | current project integration | HIGH |
| Event Base | MessagePipe | current project integration | HIGH |

**Installation channels:**
- UPM (manifest.json): UniTask, HybridCLR, VContainer, MessagePipe.VContainer
- NuGet/Plugins: Google.Protobuf.dll, MessagePipe transitive DLLs via NuGetForUnity
- Asset Store: DOTween
- External tools: Luban CLI, protoc

---

## 2. Features: Table Stakes vs Differentiators

### Table Stakes (must-have for any project)

| Feature | Complexity | Notes |
|---------|-----------|-------|
| Event System | Medium | Foundation -- everything depends on it. Enum-based IDs, priority, owner management. |
| Resource Manager | High | Async loading, reference counting, caching, unified API over AssetBundle/Addressables. |
| Network Module | High | Session management, Protobuf serialization, heartbeat, message routing, TCP + WebSocket. |
| UI Framework | Medium-High | Window lifecycle, layer management (6 layers), async prefab loading, UI stack. |
| Config Table System | Medium | Luban integration, auto-generated classes, fast ID lookup. |
| Audio Manager | Low-Medium | BGM/SFX/Voice channels, volume control, source pooling. |
| Singleton/IModule Infrastructure | Low | Generic singleton, IModule interface, layering contract. |

### Common Modules (expected in mature framework)

| Feature | Complexity | Notes |
|---------|---------|-------|
| Object Pool | Low-Medium | Generic + GameObject pools, auto-expand, timed shrink, reset callback. |
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
  -> Core  (EventSystem, NetManager, ResourceManager, UIManager, ObjectPoolManager)
    -> General  (ConfigManager, AudioManager, RedDotManager, GuideManager)
      -> Project  (game-specific UI, logic, business flows)
```

**Dependency rule:** Strictly downward only. Each layer is a separate .asmdef. No circular references.

### Module Lifecycle

- `IEventSystem` is the current event facade contract.
- `EventSystem` is the MessagePipe-backed implementation.
- `SystemManager` remains the main system lifecycle driver.
- `VContainer` owns object composition and startup wiring.

---

## 4. Critical Pitfalls to Avoid

### Critical (cause rewrites)

| # | Pitfall | Prevention |
|---|---------|------------|
| 1 | **Boot dependency creep** -- Boot starts owning business registrations | Keep `GameLifetimeScope` thin. Move features into Core/General/Project registration only |
| 2 | **Event bus rewrite drift** -- framework creates a parallel event system on top of MessagePipe | Keep one facade (`IEventSystem`) and one base (`MessagePipe`) |
| 3 | **Subscription leaks** -- destroyed owners still hold handlers | Track owners and dispose on `Shutdown()` / `UnsubscribeOwner()` |
| 4 | **Textual API mismatch** -- AI generates incorrect integration code | Use the knowledge graph doc and trigger keywords in this file |

---

## 5. Build Order Recommendation

1. Boot Layer
2. VContainer startup wiring
3. MessagePipe-backed event facade
4. ResourceManager
5. ObjectPoolManager
6. UIManager
7. ConfigManager
8. NetManager
9. AudioManager
10. Timer System
11. RedDotManager
12. GuideManager
13. HybridCLR Integration

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
