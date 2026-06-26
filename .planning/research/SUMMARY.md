# Research Summary: KJ Unity Framework

**Date:** 2026-06-26
**Sources:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md

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

**Optional additions (defer):** VContainer (DI), FMOD (advanced audio)

**Installation channels:**
- UPM (manifest.json): UniTask, HybridCLR
- NuGet/Plugins: Google.Protobuf.dll
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
|---------|-----------|-------|
| Object Pool | Low-Medium | Generic + GameObject pools, auto-expand, timed shrink, reset callback. |
| Red Dot System | Medium | Tree-structured nodes, event-driven propagation, dirty-flag optimization. |
| Guide/Tutorial System | Medium-High | Step-based state machine, config-driven, event-triggered transitions. |
| Timer System | Low-Medium | One-shot + looping, pause/resume, tick-based (not coroutine). |
| Localization | Medium | Key-value lookup, runtime switching, Luban integration. Can defer. |

### Differentiators (set this framework apart)

1. **HybridCLR Hot Update** -- C# hot update across all IL2CPP platforms including iOS. Most competitors still use Lua.
2. **Boot/Core/General/Project Layered Architecture** -- Compile-time enforced boundaries via .asmdef. Most Unity frameworks are monolithic.
3. **Message/Protocol Auto-Generation** -- .proto files to handler registration automatically.

### Anti-Features (deliberately exclude)

ECS/DOTS, built-in server, visual scripting, analytics/crash reporting, custom serialization, MVVM binding framework.

---

## 3. Architecture Approach

### Four-Layer Model

```
Boot  (entry point, HybridCLR bootstrap, startup flow)
  -> Core  (EventManager, NetManager, ResourceManager, UIManager, ObjectPoolManager)
    -> General  (ConfigManager, AudioManager, RedDotManager, GuideManager)
      -> Project  (game-specific UI, logic, business flows)
```

**Dependency rule:** Strictly downward only. Each layer is a separate .asmdef. No circular references.

### Module Lifecycle

- `IModule` interface with `Priority`, `Init()`, `Update()`, `LateUpdate()`, `FixedUpdate()`, `Shutdown()`
- `ModuleManager` (MonoBehaviour) sorts by priority, drives lifecycle
- Priority ordering: ResourceManager(100) -> EventManager(200) -> NetManager(300) -> ConfigManager(400) -> UIManager(500) -> AudioManager(600) -> ObjectPool(700) -> RedDot(800) -> Guide(900)

### Key Design Decisions

- **Event System:** Enum-based event IDs (not strings), priority ordering, owner-based subscription with auto-cleanup, synchronous + deferred dispatch
- **Network:** 4-layer architecture (Transport -> Session -> MessageRouter -> Handler), Protobuf `IMessage<T>` wrapper, session state machine (Disconnected/Connecting/Connected/Authenticating/Reconnecting)
- **UI:** 6 sorting layers (Background/Normal/Popup/Top/Loading/System), UIWindow base class with OnInit/OnOpen/OnClose/OnPause/OnResume lifecycle, window modes (Normal/Single/HideOthers/Overlay)
- **Resources:** Handle-based async loading (`AssetHandle<T>`), strong + weak reference cache, reference counting
- **Object Pool:** Generic `ObjectPool<T>` with Queue + HashSet tracking, auto-expand, `IPoolable` reset contract

### Data Flow Patterns

- **Network inbound:** Server -> NetManager -> MessageRouter -> Handler -> EventManager.Fire -> UI updates
- **User action:** Button click -> UIWindow -> EventManager.Fire -> BusinessLogic -> NetManager.Send -> Server
- **Resource loading:** UI request -> ResourceManager.LoadAsync -> AssetBundle/cache -> callback -> UI uses asset

---

## 4. Critical Pitfalls to Avoid

### Critical (cause rewrites)

| # | Pitfall | Prevention |
|---|---------|------------|
| 1 | **Over-engineered IModule interface** -- too many methods, empty implementations everywhere | Start with Init/Dispose only. Use optional interfaces (ITickable, etc.) |
| 2 | **Circular module dependencies** -- UIModule <-> AudioModule deadlocks | Enforce downward-only dependency. Use EventManager for cross-module communication |
| 3 | **Event system spaghetti** -- string-based events, event chains, memory leaks | Enum-based IDs, owner management, limit chain depth to 2 hops, debug logging |
| 4 | **HybridCLR AOT boundary violations** -- runtime crashes on iOS after hot update | Define assembly boundaries Day 1. [Preserve] attributes. Test with IL2CPP stripping "High" |
| 5 | **Memory leaks from event subscriptions** -- destroyed objects still referenced by events | Weak references or owner-based auto-cleanup. Subscribe in OnEnable, unsubscribe in OnDisable |

### Moderate (cause significant rework)

| # | Pitfall | Prevention |
|---|---------|------------|
| 6 | **Single Canvas for all UI** -- Canvas.Rebuild kills framerate | Split into multiple Canvases by update frequency. Use CanvasGroups for show/hide |
| 7 | **Config table loading spikes** -- 2-3 second startup freeze | Lazy-load non-essential tables. Use binary format. Split large tables |
| 8 | **Network zombie sessions** -- stale connections waste server resources | Heartbeat with timeout. Session token invalidation. Handle OnApplicationPause |
| 9 | **Object pool state leaks** -- pooled objects retain stale position/animation/event state | IPoolable interface with OnGetFromPool/OnReturnToPool. Reset everything on return |

### Minor (code review conventions)

- Cache GetComponent calls (never in Update)
- No string concatenation in hot paths (use StringBuilder)
- No LINQ in performance-critical code (use for loops)
- Use sharedMaterial or MaterialPropertyBlock (renderer.material creates copies)
- Avoid full-screen transparent overlays that break batching

### Phase-Specific Warnings

| Phase | Key Risk |
|-------|----------|
| Phase 1 (Foundation) | Over-engineered IModule, circular dependencies |
| Phase 2 (Event System) | String-based events, memory leaks, event spaghetti |
| Phase 3 (Network) | Zombie sessions, Protobuf version mismatch |
| Phase 4 (Config Tables) | Loading spikes, memory waste |
| Phase 5 (UI) | Single Canvas bottleneck, object pool state leaks |
| Phase 6 (HybridCLR) | AOT boundary violations, generic sharing failures |

---

## 5. Build Order Recommendation

### Phase 1: Foundation (Boot + Core base)

1. **Boot Layer** -- Entry point, startup flow state machine, .asmdef setup for all 4 layers
2. **ModuleManager + IModule** -- Minimal interface (Init/Dispose), priority-based lifecycle
3. **EventManager** -- Enum-based IDs, owner management, priority, sync + deferred dispatch
4. **ResourceManager** -- Async loading, reference counting, caching, handle-based API

### Phase 2: Core Framework

5. **ObjectPoolManager** -- Generic pool + GameObject pool, IPoolable reset contract
6. **UIManager** -- 6-layer system, UIWindow lifecycle, async prefab loading, window stack
7. **ConfigManager** -- Luban integration, binary format loading, lazy-load strategy

### Phase 3: Network + Audio

8. **NetManager + Session** -- Transport, session state machine, heartbeat, Protobuf integration, MessageRouter
9. **AudioManager** -- BGM/SFX/Voice, source pooling, volume channels
10. **Timer System** -- Tick-based, pause/resume, pool-backed

### Phase 4: Game Systems

11. **RedDotManager** -- Tree-structured nodes, event-driven propagation
12. **GuideManager** -- Step-based state machine, config-driven, event-triggered

### Phase 5: Advanced

13. **HybridCLR Integration** -- Hot update loading, AOT metadata supplement, version checking
14. **Message Auto-Generation** -- .proto to handler registration codegen

### Defer Indefinitely

Localization (add hook points only), ECS/DOTS, visual scripting, analytics, MVVM binding

### Critical Path

```
EventManager (Phase 1)
  -> ResourceManager (Phase 1)
    -> ObjectPool (Phase 2)
      -> UIManager (Phase 2)
        -> NetManager (Phase 3)
          -> Game Systems (Phase 4)
            -> HybridCLR (Phase 5)
```

The Event System and Resource Manager are the two pillars everything else rests on. Get them right first -- they are the hardest to change later.

---

## Sources

- HybridCLR: https://github.com/focus-creative-games/hybridclr
- Luban: https://github.com/focus-creative-games/luban
- UniTask: https://github.com/Cysharp/UniTask
- Google.Protobuf: https://www.nuget.org/packages/Google.Protobuf
- DOTween: http://dotween.demigiant.com
- GameFramework: https://github.com/EllanJiang/GameFramework
- Knight Framework: https://github.com/winddyhe/knight
