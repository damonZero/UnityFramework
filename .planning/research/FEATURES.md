# Feature Landscape

**Domain:** Unity Game Client Framework
**Researched:** 2026-06-26
**Overall Confidence:** MEDIUM-HIGH

---

## Table Stakes

These are the features every Unity game framework must have. Without them, the framework is unusable for any real project.

### 1. Event System (事件系统)

**Why Expected:** The fundamental decoupling mechanism. Every module communicates through events. Without it, modules become tightly coupled spaghetti.

**Complexity:** Medium  
**Status:** ✅ Done — MessagePipe with `[GameEvent]` attribute-driven registration

**Requirements:**
- [x] Type-safe events via `IPublisher<T>` / `ISubscriber<T>` (compile-time safety, no `object` boxing)
- [x] Attribute-based auto-registration (`[GameEvent]` structs scanned and registered as MessagePipe brokers)
- [x] Auto-cleanup via `IDisposable` subscription (caller owns the subscription token)
- [x] Core and General/Project have separate `[GameEvent]` attributes for scoped scanning

**Backend:** MessagePipe + MessagePipe.VContainer bridge

---

### 2. Network Module (网络模块)

**Why Expected:** Any multiplayer or server-connected game needs reliable network communication. The PROJECT.md explicitly requires Protobuf integration.

**Complexity:** High

**Requirements:**
- Session management (connect, disconnect, reconnect)
- Message send/receive with Protobuf serialization
- Request-response pattern (send message, await response)
- Heartbeat/ping mechanism
- Message dispatcher (route incoming messages to handlers)
- Support for both TCP and WebSocket transports

**Common Patterns:**
- `NetMgr.Instance.Send(msgId, protoObj)`
- `NetMgr.Instance.RegisterHandler(msgId, handler)`
- `Session` class managing connection lifecycle

**Notes:** The PROJECT.md specifies Protobuf for cross-language compatibility. The session abstraction is critical — it cleanly separates connection management from message handling.

---

### 3. UI Framework (UI 框架)

**Why Expected:** Every game has UI. A framework without UI management forces each project to reinvent window lifecycle, layering, and navigation.

**Complexity:** Medium-High

**Requirements:**
- Window lifecycle management (Open, Close, Show, Hide)
- Layer management (Normal, Popup, Top, Toast, etc.)
- UI stack for navigation (push/pop/back)
- Async loading of UI prefabs via resource manager
- UI base class with standard lifecycle hooks (OnInit, OnShow, OnHide, OnDestroy)
- Data binding or at minimum a refresh mechanism

**Common Patterns:**
```csharp
UIManager.Instance.Open<LoginWindow>(args);
UIManager.Instance.Close<LoginWindow>();
UIManager.Instance.CloseAll();
```

**Architecture Decision:** PROJECT.md specifies UGUI (not FairyGUI, not UI Toolkit). This is a solid choice — UGUI is battle-tested, has the largest community knowledge base, and no external dependency.

---

### 4. Resource Management (资源管理)

**Why Expected:** Loading assets is the most fundamental Unity operation. Without a proper resource manager, projects end up with hard references, memory leaks, and no hot-update support.

**Complexity:** High  
**Status:** ✅ Done — YooAsset 3.0 封装为 `Core/Asset/AssetSystem`

**Requirements:**
- [x] Async resource loading (UniTask-based)
- [x] Dual-channel handle management: owned (caller manages lifecycle) / cached (system manages lifecycle)
- [x] Type-aware caching via `AssetCacheKey` (path + Type)
- [x] Scene loading with serialized unload/load per path
- [x] Downloader exposure for update pipeline
- [x] Unity API regardless of backend — `IAssetSystem` interface hides YooAsset

**Backend:** YooAsset 3.0 (UPM git URL). PlayMode: EditorSimulate / Offline / Host. CDN URL configurable via `AssetConfig` ScriptableObject.

---

### 5. Config Table System (配置表系统)

**Why Expected:** Data-driven design is standard. Game designers write configs in Excel, the framework loads them at runtime. PROJECT.md specifies Luban.

**Complexity:** Medium

**Requirements:**
- Luban integration (Excel → code generation)
- Auto-generated data classes from config tables
- Fast lookup by ID (dictionary-based)
- Support for nested/complex data types
- Hot-reload during development (optional but valuable)

**Common Patterns:**
```csharp
var itemCfg = Tables.Instance.TbItem.Get(itemId);
var skillCfg = Tables.Instance.TbSkill.Get(skillId);
```

**Notes:** Luban is the right choice — it is the dominant solution in Chinese game dev ecosystem, supports multi-language export, and is designer-friendly.

---

### 6. Audio Manager (音频管理)

**Why Expected:** Every game has sound effects and music. Without a manager, audio sources leak, volume control is inconsistent, and music/SFX layering is chaotic.

**Complexity:** Low-Medium

**Requirements:**
- BGM playback (loop, fade in/out)
- SFX playback (one-shot, pooled)
- Volume control (separate BGM/SFX/UI channels)
- Mute/unmute per channel
- Audio source pooling (avoid creating new AudioSources constantly)

**Common Patterns:**
```csharp
AudioManager.Instance.PlayBGM("main_theme");
AudioManager.Instance.PlaySFX("button_click");
AudioManager.Instance.SetVolume(AudioChannel.BGM, 0.5f);
```

---

### 7. DI + System Infrastructure (基础架构)

**Why Expected:** The framework needs foundational patterns that every module uses.

**Complexity:** Low  
**Status:** ✅ Done — VContainer + ISystem + [CoreSystem] + AsImplementedInterfaces()

**Requirements:**
- [x] DI-driven system registration via `[CoreSystem]` attribute + reflection scanning
- [x] `ISystem` / `ITickableSystem` lifecycle contracts
- [x] `IModel` lifecycle contract for General/Project business code
- [x] Boot layer minimal — no dependency on Core/General/Project

**Backend:** VContainer 1.1.0

---

## Common Modules

These modules are expected in a mature framework. Not every project uses all of them, but a framework that lacks them forces projects to build from scratch.

### 8. Object Pool (对象池)

**Why Expected:** Frequent Instantiate/Destroy causes GC pressure. Object pooling is a universal optimization.

**Complexity:** Low-Medium

**Requirements:**
- Generic object pool (`ObjectPool<T>`)
- GameObject-specific pool (handles prefab instantiation)
- Auto-expand when pool is empty
- Pre-warm option (create N objects at startup)
- Timed auto-shrink (release unused objects after timeout)
- Reset callback on return (clean object state)

**Dependencies:** Resource Manager (for loading prefabs)

**Notes:** This is one of the simplest modules but has high reuse across the entire framework — UI elements, network messages, particles, etc.

---

### 9. Red Dot System (红点系统)

**Why Expected:** Mobile games universally use red dots for notification. Building it generically once saves every project from reinventing it.

**Complexity:** Medium

**Requirements:**
- Tree-structured red dot nodes (parent aggregates children)
- Boolean and numeric red dot types
- Event-driven UI refresh (when child state changes, propagate up)
- Efficient dirty-flag propagation (only update affected branches)
- Serializable state (persist across sessions if needed)

**Architecture:**
```
Root
├── Mail (has unread → red dot ON)
│   ├── SystemMail (3 unread)
│   └── FriendMail (0 unread)
├── Shop (children clean → red dot OFF)
└── Quest (has claimable → red dot ON)
```

**Dependencies:** Event System (for notifying UI of state changes)

**Notes:** The tree structure is key. Each node tracks its own state and aggregates children. When a leaf changes, the update propagates up to root. UI components subscribe to specific nodes.

---

### 10. Guide/Tutorial System (引导系统)

**Why Expected:** New player onboarding is critical for retention. A reusable guide system with config-driven steps is standard.

**Complexity:** Medium-High

**Requirements:**
- Step-based state machine (current step, next step, conditions)
- Config-driven steps (Excel/Luban table)
- Multiple guide types: highlight click, mask overlay, finger pointer
- Progress persistence (resume guide after app restart)
- Event-triggered step transitions (open panel, click button, receive network message)
- Support for parallel guides (multiple tutorial tracks)

**Dependencies:** Event System (trigger-based transitions), UI Framework (overlay rendering), Config Table (guide step data)

**Notes:** This is one of the more complex modules because it touches many other systems. Build it after Event, UI, and Config are stable.

---

### 11. Timer System (定时器)

**Why Expected:** Delayed execution, cooldowns, scheduled tasks — every game needs timing logic. Coroutines are not a substitute (they are tied to MonoBehaviour lifecycle).

**Complexity:** Low-Medium

**Requirements:**
- One-shot timer (execute once after delay)
- Looping timer (execute every N seconds)
- Timer cancellation by ID
- Pause/resume support (game pause compatibility)
- Tick-based management (not coroutine-based)
- Minimal GC allocation (reuse timer objects)

**Dependencies:** Object Pool (for timer object reuse)

---

### 12. Localization System (本地化)

**Why Expected:** Any game targeting multiple markets needs localization. A framework module that handles text, images, and audio switching is standard.

**Complexity:** Medium

**Requirements:**
- Key-value text lookup (language table)
- Runtime language switching
- Rich text placeholder support (`{0}`, `{1}`)
- Image/audio localization (different assets per language)
- UI Text auto-binding component (`LocalizeText`)
- Integration with Luban config tables for language data

**Dependencies:** Config Table System (for language data), Resource Manager (for per-language assets)

**Notes:** Can be deferred — most games start with one language and add localization later. But the framework should have the hook points ready.

---

## Differentiators

Features that set this framework apart. Not every Unity framework has these, and they provide significant value.

### 13. HybridCLR Hot Update Integration (热更新)

**Why Valuable:** Hot update is a critical requirement for Chinese market games (App Store review, rapid iteration). HybridCLR is the dominant C# hot-update solution.

**Complexity:** High

**Requirements:**
- HybridCLR setup and integration
- Hot-update code loading
- Hot-update asset loading (paired with Resource Manager)
- Version checking and patch download
- AOT metadata supplement generation

**Notes:** This is already specified in PROJECT.md. It is a differentiator because many frameworks still use Lua (xLua/ToLua) for hot update, which requires a different language. HybridCLR keeps everything in C#.

---

### 14. Boot/Core/General/Project Layered Architecture

**Why Valuable:** Clear layering with explicit dependency rules prevents the "everything depends on everything" problem that plagues most Unity projects.

**Complexity:** Medium

**Requirements:**
- **Boot**: Entry point, initialization sequence, module registration
- **Core**: Framework-level singletons and managers (Event, Resource, Network)
- **General**: Reusable game systems (UI, Audio, Pool, RedDot, Guide)
- **Project**: Game-specific business logic

**Dependency Rules:**
- Boot → Core → General → Project (strict downward)
- No upward or same-layer circular dependencies
- Each layer is a separate assembly definition (.asmdef)

**Notes:** This is a differentiator because most Unity frameworks are monolithic. The layered approach with assembly definitions enforces architectural boundaries at compile time.

---

### 15. Message/Protocol Auto-Generation

**Why Valuable:** Manually writing Protobuf message handlers is tedious and error-prone. Auto-generating handler registration from .proto files saves significant development time.

**Complexity:** Medium

**Requirements:**
- Auto-generate C# message classes from .proto files
- Auto-generate handler registration code
- Auto-generate message ID constants
- Code generation tool (editor script or build step)

**Notes:** Luban already handles config code generation. Adding Protobuf code generation creates a consistent "define once, use everywhere" workflow.

---

## Anti-Features

Features to deliberately NOT build. These are tempting but harmful for a reusable framework.

### Anti-Feature 1: ECS/DOTS Architecture

**Why Avoid:** ECS (Entity-Component-System) via Unity DOTS is powerful for specific use cases (massive simulations, thousands of entities) but adds enormous complexity for typical mobile/PC games. The learning curve is steep, the tooling is still maturing, and it inverts how most Unity developers think.

**What to Do Instead:** Stick with traditional MonoBehaviour + manager pattern. If a specific subsystem needs ECS-level performance (e.g., crowd simulation), implement it locally within that subsystem, not as the framework's architecture.

---

### Anti-Feature 2: Built-in Server Implementation

**Why Avoid:** PROJECT.md explicitly defers server-side to later. A framework that tries to be both client and server ends up mediocre at both. Server concerns (database, load balancing, matchmaking) are fundamentally different from client concerns.

**What to Do Instead:** Define clean network interfaces (send, receive, session). The server can be implemented later in C# using the same Protobuf definitions.

---

### Anti-Feature 3: Visual Scripting / Node-Based Editor

**Why Avoid:** Building a visual scripting system is a massive undertaking (think Bolt, Blueprints). It requires a custom node editor, serialization format, runtime interpreter, and debugger. This is a product in itself, not a framework feature.

**What to Do Instead:** If visual scripting is needed, integrate Unity's official Visual Scripting (formerly Bolt) or recommend third-party solutions.

---

### Anti-Feature 4: Built-in Analytics / Crash Reporting

**Why Avoid:** Analytics and crash reporting are platform-specific and service-specific (Firebase, Unity Analytics, custom). Baking them into the framework couples it to specific services.

**What to Do Instead:** Provide extension points (hooks for initialization, event reporting) but let projects choose their analytics provider.

---

### Anti-Feature 5: Custom Serialization Format

**Why Avoid:** Building a custom binary or text serialization format is error-prone, hard to debug, and reinvents the wheel. Protobuf (for network) and Luban (for configs) already cover the two main serialization needs.

**What to Do Instead:** Use Protobuf for network messages, Luban-generated code for config tables, and JSON for any ad-hoc persistence needs.

---

### Anti-Feature 6: Full-Featured UI Binding Framework (MVVM)

**Why Avoid:** A complete MVVM data-binding framework (like WPF-style `INotifyPropertyChanged`, `BindingExpression`) is complex to build and maintain. For game UI, the overhead often exceeds the benefit — game UI updates are usually explicit and event-driven, not continuously data-bound.

**What to Do Instead:** Provide a simple `Refresh()` pattern on UI panels. Let the UI explicitly pull data when events fire. If a project needs MVVM, they can add it on top.

---

## Feature Dependencies

```
Event System ─────────────────────────────────────────┐
    │                                                  │
    ├──> Network Module                                │
    │       └──> Protobuf Integration                  │
    │                                                  │
    ├──> UI Framework                                  │
    │       └──> Resource Manager                      │
    │               └──> Object Pool                   │
    │                                                  │
    ├──> Config Table System (Luban)                   │
    │       └──> Localization System                   │
    │                                                  │
    ├──> Red Dot System                                │
    │                                                  │
    ├──> Guide System                                  │
    │       └──> UI Framework + Config Table + Events  │
    │                                                  │
    └──> Timer System                                  │
            └──> Object Pool                           │

Audio Manager ──> Resource Manager + Object Pool

Boot Infrastructure ──> ISystem + IModel + [CoreSystem] + [Model]
```

**Critical Path:**
1. Event System (foundation — everything depends on it)
2. Resource Manager (UI, Audio, ObjectPool all need asset loading)
3. Object Pool (used by UI, Audio, Timer, Network)
4. Config Table (Luban integration)
5. UI Framework (most visible user-facing module)
6. Network Module (Protobuf + session management)
7. Audio Manager
8. Common modules (RedDot, Guide, Timer, Localization)
9. HybridCLR integration (after core is stable)

---

## MVP Recommendation

### Build First (Phase 1 - Foundation)
1. **Event System** — Everything depends on it
2. **Resource Manager** — Core loading infrastructure
3. **Object Pool** — Used everywhere, simple to build
4. **DI + System Infrastructure** — VContainer + ISystem + [CoreSystem]

### Build Second (Phase 2 - Core)
5. **UI Framework** — Most visible, validates the architecture
6. **Config Table (Luban)** — Data-driven foundation
7. **Audio Manager** — Simple, validates resource manager usage

### Build Third (Phase 3 - Network)
8. **Network Module** — Session management, Protobuf, message dispatch
9. **Timer System** — Small module, used by network (heartbeat)

### Build Fourth (Phase 4 - Game Systems)
10. **Red Dot System** — Validates event system depth
11. **Guide System** — Complex, validates cross-module integration
12. **Localization** — Can be deferred further if single-language initially

### Build Last (Phase 5 - Advanced)
13. **HybridCLR Hot Update** — After core is stable and tested
14. **Message Auto-Generation** — Productivity polish

### Defer Indefinitely
- ECS/DOTS — Only if a specific project needs it
- Visual scripting — Use third-party if needed
- Analytics — Extension points only
- MVVM binding — Add per-project if needed

---

## Complexity Summary

| Feature | Complexity | Build Phase | Dependencies |
|---------|-----------|-------------|--------------|
| Event System | Medium | 1 | None |
| Resource Manager | High | 1 | None |
| Object Pool | Low-Medium | 1 | Resource Manager |
| Singleton/ISystem Infrastructure | Low | DONE — VContainer + ISystem + [CoreSystem] | Event Bus, object lifecycle |
| UI Framework | Medium-High | 2 | Resource Manager, Event System |
| Config Table (Luban) | Medium | 2 | None |
| Audio Manager | Low-Medium | 2 | Resource Manager, Object Pool |
| Network Module | High | 3 | Event System, Protobuf |
| Timer System | Low-Medium | 3 | Object Pool |
| Red Dot System | Medium | 4 | Event System |
| Guide System | Medium-High | 4 | Event System, UI, Config Table |
| Localization | Medium | 4 | Config Table, Resource Manager |
| HybridCLR | High | 5 | Resource Manager |
| Message Auto-Gen | Medium | 5 | Network Module |

---

## Sources

- Unity official documentation (UGUI, Audio)
- GameFramework (Ellan Jiang) — enterprise Unity framework, modular design
- QFramework — lightweight Unity architecture framework
- ET Framework — ECS + networking Unity framework
- Luban configuration table tool documentation
- HybridCLR hot update solution documentation
- YooAsset asset management documentation
- Common Unity game architecture patterns (DI, pub/sub, pool)
- Chinese game dev community best practices (events, red dot, guide systems)

**Confidence Notes:**
- Core systems (Event, UI, Resource, Network): HIGH confidence — well-documented, widely implemented
- Common modules (RedDot, Guide, Timer): MEDIUM-HIGH confidence — standard patterns exist, implementation details vary
- Anti-features: MEDIUM confidence — based on community consensus and practical experience
- HybridCLR integration: MEDIUM confidence — rapidly evolving, check latest docs before implementation
