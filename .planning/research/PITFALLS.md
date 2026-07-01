# Domain Pitfalls

**Domain:** Unity Game Framework (Boot/Core/General/Project layered architecture)
**Researched:** 2026-06-26

---

## Critical Pitfalls

Mistakes that cause rewrites, major refactoring, or fundamental architectural problems.

### Pitfall 1: Over-Engineered System Interface (ISystem God Interface)

**What goes wrong:** The `ISystem` interface grows too large with `Initialize()`, `Tick()`, `LateTick()`, `Dispose()`, `OnPause()`, `OnResume()`, `OnAppFocus()` etc. Every system must implement all methods even if irrelevant.

**Why it happens:** Designing the interface before understanding what each system actually needs. Trying to anticipate every lifecycle scenario.

**Consequences:**
- 30+ empty method implementations across systems
- Adding a new lifecycle method requires touching every system
- New developers afraid to add systems due to boilerplate

**Prevention:**
- Kept `ISystem` minimal: `Priority`, `Init()`, `Shutdown()`
- Optional orthogonal: `ITickableSystem`
- DI (VContainer) handles construction; `[CoreSystem]` attribute handles discovery
- `AsImplementedInterfaces()` for automatic binding

**Detection:** If `ISystem` has more than 3 members before shipping any system, stop adding.

**Phase:** Already prevented by current design.

---

### Pitfall 2: Circular Module Dependencies

**What goes wrong:** UIModule depends on AudioModule (play sound on button click). AudioModule depends on UIModule (show volume settings panel). Neither can initialize without the other.

**Why it happens:** Modules are designed as singletons that directly reference each other. No dependency injection or mediator pattern.

**Consequences:**
- Initialization order bugs (null references at startup)
- Cannot reuse modules in other projects
- Cannot test modules in isolation
- "It works on my machine" due to initialization race conditions

**Prevention:**
- Enforce unidirectional dependency flow: Core assemblies cannot reference General assemblies
- Use MessagePipe for cross-module communication instead of direct references
- DI (VContainer) resolves dependencies at startup; circular refs are compile-time or registration-time errors
- Document dependency graph: `Boot -> Core -> General -> Project` (downward only)
- If A needs B and B needs A, extract shared logic into a lower layer or use events

**Detection:** Draw your module dependency graph. If you see cycles, refactor immediately.

**Phase:** Already enforced via .asmdef references.

---

### Pitfall 3: Event System Becomes Spaghetti

**What goes wrong:** Old pattern: string-based event names (`"PlayerDeath"`, `"playerDeath"`, `"PLAYER_DEATH"` — all different events). No compile-time checking. Events fire events that fire events. Debugging is impossible.

**Why it happens:** Events are easy to add, hard to remove. Developers use events for everything including direct module communication.

**Consequences:**
- Silent failures from typos in event names
- Impossible to trace event flow ("who subscribes to this?")
- Memory leaks from forgotten unsubscribe calls
- Performance issues from event chains (A fires B fires C fires D)

**Prevention (already in place):**
- MessagePipe with `[GameEvent]` structs — compile-time safety, no string IDs
- `IPublisher<T>` / `ISubscriber<T>` with `IDisposable` subscription tokens
- Subscription tokens disposed in system `Shutdown()` or `OnDestroy()`
- Keep MessagePipe for cross-module decoupling; use direct calls within a module

**Detection:** If you cannot answer "what subscribes to event X?" in under 30 seconds, your event system is spaghetti.

**Phase:** Already solved by MessagePipe architecture.

---

### Pitfall 4: Hot Update Boundary Violations (HybridCLR)

**What goes wrong:** Hot-update code directly references AOT types that get stripped. Generic instantiations missing from AOT metadata cause runtime crashes. Serialization fails for hot-update types.

**Why it happens:** Developers don't understand the AOT/hot-update boundary. They put code in the wrong assembly or forget to preserve types.

**Consequences:**
- Crashes on iOS after hot update (AOT stripping removed needed types)
- Generic method calls fail at runtime with `TypeLoadException`
- JSON/Protobuf serialization silently fails for hot-update DTOs
- Cannot hot-update the code you thought you could

**Prevention:**
- Define clear assembly boundaries from Day 1: AOT assemblies vs hot-update assemblies
- Keep all interface definitions in AOT assemblies
- Add `[Preserve]` attributes on types/methods used by hot-update code
- Test with IL2CPP stripping level "High" during development, not just Mono
- Generate AOT generic method supplement metadata for all Protobuf message types

**Detection:** Build with IL2CPP + stripping level High. If it crashes but Mono works, you have boundary violations.

**Phase:** Phase 6 (HybridCLR integration). But assembly boundary design must be in Phase 1.

---

### Pitfall 5: Memory Leaks from Event/Delegate Subscriptions

**What goes wrong:** Objects subscribe to events in `OnEnable()` but forget to unsubscribe in `OnDisable()`. Destroyed GameObjects remain referenced by the event system, preventing garbage collection.

**Why it happens:** C# events hold strong references. Unity's `Destroy()` does not automatically unsubscribe from C# events.

**Consequences:**
- Memory grows linearly with gameplay time
- Ghost subscribers execute on destroyed objects (MissingReferenceException)
- GC spikes become longer and more frequent
- Eventually out-of-memory on mobile devices

**Prevention:**
- MessagePipe uses `IDisposable` subscription tokens — dispose in Shutdown() / OnDestroy()
- No need for `System.WeakReference` — the token pattern is simpler and deterministic

**Detection:** Open/close a UI window 100 times. If memory grows, you have a leak. Check Profiler for growing object counts.

**Phase:** Phase 2 (event system) and Phase 5 (UI framework). Build leak prevention into the system, not as afterthought.

---

## Moderate Pitfalls

### Pitfall 6: UI Framework — Single Canvas for Everything

**What goes wrong:** All UI elements live on one Canvas. Any change to any element triggers `Canvas.Rebuild()` for the entire UI. Frame rate drops to 15fps with 50+ UI elements.

**Why it happens:** UGUI defaults to single Canvas. Developers don't know about Canvas splitting.

**Consequences:**
- UI performance degrades linearly with element count
- Scrolling lists cause frame drops
- Animations on one panel stutter other panels

**Prevention:**
- Split UI into multiple Canvases: static background, dynamic HUD, popup layer, loading screen
- Never put frequently-changing elements (timers, animations) on the same Canvas as static elements
- Use CanvasGroups for show/hide instead of `SetActive()`
- Disable `RaycastTarget` on all non-interactive Image/Text components

**Detection:** Use Frame Debugger. If one Canvas has 50+ draw calls, split it.

**Phase:** Phase 5 (UI framework). Design Canvas hierarchy upfront.

---

### Pitfall 7: Config Table Loading Spikes (Luban)

**What goes wrong:** Loading all Luban config tables at startup causes 2-3 second freeze. Or loading tables on-demand causes hitches during gameplay.

**Why it happens:** Binary config data is large. Loading and deserializing everything at once blocks the main thread.

**Consequences:**
- Startup time exceeds 5 seconds on mobile
- Gameplay hitches when entering new areas that need new config tables
- Memory waste from loading tables that are never used in current scene

**Prevention:**
- Load only essential tables at startup (items, skills, characters)
- Lazy-load tables on first access with async loading
- Use binary format (not JSON) — 10x faster deserialization
- Split large tables (10,000+ rows) into sub-tables by category
- Cache parsed results, not raw bytes

**Detection:** Profile startup. If Luban loading exceeds 1 second, you need lazy loading.

**Phase:** Phase 4 (config table system). Design loading strategy before writing code.

---

### Pitfall 8: Network Session Zombie Connections

**What goes wrong:** Client disconnects (network switch, app background) but session stays alive on server. Client reconnects, creates new session, old session wastes resources. Or client uses stale session ID.

**Why it happens:** No heartbeat mechanism. No proper disconnect detection. Session cleanup is lazy.

**Consequences:**
- Server resource waste from zombie sessions
- Duplicate messages sent to same player
- Reconnection fails or causes state inconsistency

**Prevention:**
- Implement heartbeat with configurable interval (e.g., 10 seconds) and timeout (30 seconds)
- Clean up session on both disconnect detection AND explicit logout
- Use session version/token that invalidates on reconnect
- Handle Unity `OnApplicationPause`/`OnApplicationFocus` for mobile background detection
- Queue messages during brief disconnections, discard on long disconnections

**Detection:** Simulate network interruption (airplane mode). If reconnection doesn't work cleanly, fix session management.

**Phase:** Phase 3 (network module). Session lifecycle is core to networking.

---

### Pitfall 9: Object Pool Not Resetting State

**What goes wrong:** Objects returned to pool retain position, rotation, active children, component state, animation state, particle effects. Objects taken from pool show stale data.

**Why it happens:** `ReturnToPool()` only deactivates the object. No reset logic.

**Consequences:**
- Visual glitches (objects appearing at old positions)
- Logic bugs (pooled bullets with leftover collision flags)
- Hard-to-reproduce "ghost" bugs

**Prevention:**
- Implement `IPoolable` interface with `OnGetFromPool()` and `OnReturnToPool()` methods
- Reset transform (position, rotation, scale) on return
- Stop all coroutines, particle systems, animations on return
- Clear all event subscriptions on return
- Document what gets reset for each pooled type

**Detection:** Return an object to pool, take it out. If anything looks wrong, your reset is incomplete.

**Phase:** Phase 7 (object pool). Design reset contract upfront.

---

### Pitfall 10: Protobuf Version Mismatch Between Client and Server

**What goes wrong:** Client uses `.proto` file version 1, server uses version 2. Deserialization succeeds but produces garbage data. No error, just wrong values.

**Why it happens:** No version negotiation. Protobuf is forward/backward compatible by design, which masks mismatches.

**Consequences:**
- Silent data corruption
- Bugs that only appear in specific message combinations
- "It works in testing" but fails in production with different server version

**Prevention:**
- Include protocol version in handshake message
- Validate version compatibility on connection
- Use `required` fields for critical data (catches missing fields)
- Generate C# code from single source of truth (shared `.proto` repo)
- Add integration tests that serialize/deserialize all message types

**Detection:** Compare client and server `.proto` file checksums. If different, investigate.

**Phase:** Phase 3 (network module). Version negotiation must be part of connection handshake.

---

## Minor Pitfalls

### Pitfall 11: `GetComponent<T>()` in Update Loop

**What goes wrong:** Calling `GetComponent<T>()` every frame causes unnecessary overhead. It's a string-based lookup internally.

**Prevention:** Cache component references in `Awake()` or `Start()`. Use `[SerializeField]` for inspector-assigned references.

**Phase:** All phases. Code review convention.

---

### Pitfall 12: String Concatenation in Hot Paths

**What goes wrong:** Building strings with `+` operator in loops or Update() creates garbage. `"Score: " + score.ToString()` every frame = GC pressure.

**Prevention:** Use `StringBuilder` for dynamic strings. Use `string.Format()` or interpolated strings only outside hot paths. Cache formatted strings when possible.

**Phase:** All phases. Code review convention.

---

### Pitfall 13: LINQ in Performance-Critical Code

**What goes wrong:** LINQ queries (`.Where()`, `.Select()`, `.ToList()`) allocate iterators and lists on every call. In Update() or network handlers, this causes GC spikes.

**Prevention:** Use `for` loops in hot paths. Reserve LINQ for initialization/loading code. Profile allocations regularly.

**Phase:** All phases. Code review convention.

---

### Pitfall 14: `renderer.material` Creates Copy

**What goes wrong:** Accessing `renderer.material` (not `sharedMaterial`) creates a new material instance. If done in a loop, you get material leak.

**Prevention:** Use `renderer.sharedMaterial` for shared changes. Use `MaterialPropertyBlock` for per-object property changes. Cache material references.

**Phase:** Phase 5 (UI framework) and any rendering code.

---

### Pitfall 15: Full-Screen Transparent Overlay Blocks Batching

**What goes wrong:** A full-screen semi-transparent Image for modal dimming breaks batching for everything behind it. All UI behind it gets individual draw calls.

**Prevention:** Use a small black texture with low alpha stretched to fill screen. Or use a custom shader that doesn't break batching.

**Phase:** Phase 5 (UI framework).

---

## Phase-Specific Warnings

| Phase | Topic | Likely Pitfall | Mitigation |
|-------|-------|----------------|------------|
| Boot | Project Foundation | Over-engineered ISystem interface, circular dependencies | Start minimal, enforce dependency direction, use optional interfaces |
| Core | Event System | Event spaghetti, subscription leaks | MessagePipe struct events, IDisposable tokens, dispose in Shutdown() |
| Core | Network Module | Zombie sessions, Protobuf version mismatch, main thread blocking | Heartbeat, version handshake, marshal to main thread |
| General | Config Tables | Loading spikes, memory waste, type mapping errors | Lazy loading, binary format, type validation |
| Core | UI Framework | Single Canvas bottleneck, object pool state leaks, raycast abuse | Canvas splitting, IPoolable reset, disable RaycastTarget |
| Boot | Hot Update | AOT boundary violations, generic sharing failures, stripping | Assembly boundary design, preserve attributes, test with High stripping |
| Core | Object Pool | State not reset, pool never shrinks, stale references | IPoolable interface, pool size limits, clear on scene change |

---

## Architecture-Specific Pitfalls (Boot/Core/General/Project Layers)

### Layer Violation: General Depending on Project

**What goes wrong:** General layer (reusable systems) imports Project layer (game-specific code). Now General cannot be reused in other projects.

**Prevention:** Compile-time enforcement. General assemblies must not reference Project assemblies. Use interfaces or events for upward communication.

### Layer Violation: Core Depending on General

**What goes wrong:** Core layer depends on General layer. Core becomes project-specific and not reusable.

**Prevention:** Core assemblies must not reference General or Project assemblies. Use interfaces defined in Core or MessagePipe events for upward communication.

### Boot Layer Bloat

**What goes wrong:** Boot layer (initialization, splash screen, hot update check) grows into a mini-framework with its own managers and systems.

**Prevention:** Boot layer does exactly 3 things: initialize Core, check for hot updates, load first scene. Nothing else.

---

## Sources

- Unity UI optimization best practices (Canvas rebuild, batching)
- HybridCLR official documentation and common issues
- Luban configuration table integration guides
- UniTask async/await pitfalls in Unity
- Unity memory management and profiling best practices
- Game Programming Patterns (Robert Nystrom) — event systems, object pools
- Common Unity game framework architecture discussions (Chinese game dev community)

---

*Researched: 2026-06-26 | Confidence: MEDIUM — Based on established Unity development patterns and community knowledge. Verify specific HybridCLR and Luban version compatibility against current releases.*
