---
name: kj-core-architecture
description: >
  KJ Framework Core 架构层指南。涵盖 ISystem（系统生命周期：Priority+Init+Shutdown）、ITickableSystem（可驱动系统：Update/LateUpdate/FixedUpdate）、SystemManager（VContainer 驱动的系统管理器：IStartable+ITickable+ILateTickable+IFixedTickable+IDisposable）、CoreSystemAttribute（标记 Core 系统用于自动 DI 注册）、CoreTypeRegistration（反射扫描注册：[CoreSystem]→VContainer + [GameEvent]→MessagePipe）、CoreContainerRegistration（RegisterCoreServices 扩展方法）、启动事件（AppStartedEvent/AppShuttingDownEvent）、StartupProbeSystem（启动链路验证）。
  触发场景：创建新 Core 系统、理解系统生命周期、配置 DI 注册、添加事件订阅、理解 SystemManager Tick 调度、注册 MessagePipe 事件 Broker、ZLinq 使用（禁止 System.Linq，必须用 AsValueEnumerable() 入口）。
  核心规则：Core 层用 [CoreSystem]+ISystem（业务层用 [Model]+IModel）；SystemManager 由 VContainer 驱动；反射只在注册时使用，运行时走构造函数 DI；[CoreSystem] 类必须在 Core.* 命名空间；日志模板跟随所属类，日志系统细节走 kj-log；禁止 System.Linq，必须用 ZLinq + AsValueEnumerable()。
metadata:
  doc: CODEMAP.md
  layer: Core
---

# KJ Core 架构层

源码在 `Assets/Scripts/Core/Systems/` 和 `Assets/Scripts/Core/Bootstrap/`，完整文档见 `CODEMAP.md` Layer: Core 章节。

## 架构速查

```
ISystem / ITickableSystem          — 系统生命周期接口
    ↑
[CoreSystem] 标记类                — 自动扫描注册到 VContainer
    ↑
SystemManager                     — 管理所有 ISystem 的生命周期 + Tick 驱动
    ↑ (VContainer)
CoreContainerRegistration         — 入口：RegisterCoreServices()
ICoreStartupStatus                — Core 启动结果，供上层决定是否进入业务加载
CoreTypeRegistration              — 反射：RegisterGameEvents() + RegisterSystems()
AppStartedEvent / AppShuttingDownEvent — 生命周期事件
```

## 核心接口

### ISystem — 系统生命周期

```csharp
public interface ISystem
{
    int Priority { get; }    // 越小越先 Init（Shutdown 逆序）
    void Init();             // 初始化
    void Shutdown();         // 关闭
}

// 实现示例
[CoreSystem]
public sealed class MyCoreSystem : ISystem
{
    public int Priority => 200;
    public void Init() { /* 初始化逻辑 */ }
    public void Shutdown() { /* 清理逻辑 */ }
}
```

### ITickableSystem — 可驱动系统

```csharp
public interface ITickableSystem : ISystem
{
    void Update(float deltaTime);        // 每帧
    void LateUpdate(float deltaTime);    // Update 之后
    void FixedUpdate(float fixedDeltaTime); // 物理帧
}

// SystemManager 通过 VContainer 的 ITickable/ILateTickable/IFixedTickable 驱动
```

## 系统注册与生命周期

### 1. 标记系统

```csharp
[CoreSystem]  // 必须实现 ISystem，必须在 Core.* 命名空间
public sealed class MySystem : ISystem
{
    public int Priority => 200;
    public void Init() { }
    public void Shutdown() { }
}
```

### 2. 自动 DI 注册流程

```
CoreContainerRegistration.RegisterCoreServices(builder)
  ├─ 1. builder.RegisterMessagePipe()                          → MessagePipeOptions
  ├─ 2. builder.Register<AssetRuntime>(Singleton).AsSelf().AsImplementedInterfaces()
  ├─ 3. builder.RegisterCoreTypes(options, CoreAssembly)     → CoreTypeRegistration
  │     ├─ RegisterGameEvents():
  │     │     GameEventTypeScanner.FindGameEventTypes(assemblies)
  │     │     → 对每个 [GameEvent] struct:
  │     │         RegisterMessageBroker<T>(builder, options) via 反射
  │     └─ RegisterSystems():
  │           扫描 [CoreSystem] 类 → 验证 ISystem + Core.* 命名空间
  │           → builder.Register(type, Singleton).AsSelf().AsImplementedInterfaces()
  └─ 4. builder.RegisterEntryPoint<SystemManager>()
```

### 3. 运行时生命周期

```
VContainer 构建容器
  ↓
IStartable.Start() → SystemManager.Start() → InitAll()
  ├─ _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority))
  ├─ 按 Priority 升序调用 Init()
  ├─ 每个系统 Init() 包在 try-catch 中（一个失败不影响其他）
  ├─ 记录失败系统到 ICoreStartupStatus
  └─ 如果没有失败，发布 _appStartedPublisher.Publish(new AppStartedEvent())

Unity 主循环:
  ITickable.Tick() → SystemManager.Tick()
    → foreach ITickableSystem: Update(Time.deltaTime)
  ILateTickable.LateTick() → SystemManager.LateTick()
    → foreach ITickableSystem: LateUpdate(Time.deltaTime)
  IFixedTickable.FixedTick() → SystemManager.FixedTick()
    → foreach ITickableSystem: FixedUpdate(Time.fixedDeltaTime)

应用退出:
  IDisposable.Dispose() → SystemManager.Dispose() → ShutdownAll()
    ├─ Core 启动成功时发布 AppShuttingDownEvent
    ├─ 只对 Init() 成功的系统按 Priority 降序调用 Shutdown()
    └─ 清空所有列表
```

## 系统注册优先级

| 系统 | Priority | 接口 | 依赖 |
|------|----------|------|------|
| `StartupProbeSystem` | 0 | ISystem | 无（纯验证） |
| `AssetSystem` | 100 | ISystem | IAssetRuntime, IPublisher&lt;AssetSystemReadyEvent&gt; |
| `PoolService` | 110 | ISystem | IAssetSystem |

**约定:**
- 基础设施系统 Priority &lt; 100
- Asset 相关 100~109
- Pool 相关 110~119
- 业务相关 ≥ 200（放在 General/Project 层，用 IModel）

## 事件

```csharp
// 当前 Core 层定义的事件（readonly struct + [GameEvent]）
AppStartedEvent       // SystemManager.InitAll() 全部成功后发布
AppShuttingDownEvent   // Core 启动成功后，SystemManager.ShutdownAll() 最前发布
AssetSystemReadyEvent  // AssetSystem.Init() 发布
```

## 最佳实践

1. **用 [CoreSystem] 标记，不要手动 Register** — 反射自动注册，零配置
2. **Priority 要合理** — 被依赖的系统 Priority 更小（先初始化）
3. **构造函数注入依赖** — VContainer 自动解析，不手动 new
4. **Init 做初始化，Shutdown 做逆序清理** — 不要跨生命周期泄漏状态
5. **TickableSystem 保持轻量** — Update 每帧调用，避免重计算
6. **系统命名用 XxxSystem** — 放在 Core 下对应功能目录
7. **[CoreSystem] 类必须在 Core.* 命名空间** — CoreTypeRegistration 运行时会校验
8. **不要手动持有 SystemManager 引用** — 用 VContainer 注入依赖系统
9. **日志模板跟随所属类** — `SystemManagerLog.cs` 放在 `Core/Systems/`，不要集中塞到 `Core/Logging/`；日志规则细节使用 `kj-log`
10. **容器级对象由容器释放** — `ILoggerFactory` 等不应在某个 `ISystem.Shutdown()` 中 dispose
11. **Ready 事件必须真实** — 如 `AssetSystemReadyEvent` 只能在底层 runtime 初始化成功且 `IsReady` 后发布
12. **Core 启动失败要可观测** — `SystemManager` 记录 Init 失败到 `ICoreStartupStatus`，失败时不发布 `AppStartedEvent`

## ZLinq 使用规范

**Core 层禁止使用 `System.Linq`，必须使用 ZLinq 替代，避免 GC 分配。**

### 核心规则

1. **`using ZLinq;` 代替 `using System.Linq;`** — asmdef 需引用 `"ZLinq"`
2. **数组/T[] 的 LINQ 方法必须通过 `.AsValueEnumerable()` 入口** — ZLinq 不直接在 `T[]` 上添加扩展方法
3. **每个 LINQ 链都以 `.AsValueEnumerable()` 开头，以 `.ToArray()`/`.ToList()`/`.First()`/`.FirstOrDefault()` 等终结**

### 正确 vs 错误

```csharp
// ❌ 错误：直接对 T[] 调用 .First()、.Where() 等 — 编译失败
MethodInfo[] methods = ...;
methods.Where(m => m.Name == "Foo");      // CS1061: 'MethodInfo[]' does not contain 'Where'
methods.First(m => m.Name == "Foo");      // CS1061: 'MethodInfo[]' does not contain 'First'

// ❌ 错误：使用 System.Linq — 产生 GC 分配
using System.Linq;
var result = methods.First(m => m.Name == "Foo");

// ✅ 正确：使用 ZLinq，通过 AsValueEnumerable() 进入
using ZLinq;
var result = methods.AsValueEnumerable().First(m => m.Name == "Foo");
var filtered = methods.AsValueEnumerable().Where(m => m.Name == "Foo").ToArray();
var first = methods.AsValueEnumerable().FirstOrDefault(m => m.Name == "Foo");
```

### 常见操作符映射

| System.Linq | ZLinq |
|---|---|
| `array.Where(...)` | `array.AsValueEnumerable().Where(...)` |
| `array.Select(...)` | `array.AsValueEnumerable().Select(...)` |
| `array.First(...)` | `array.AsValueEnumerable().First(...)` |
| `array.FirstOrDefault(...)` | `array.AsValueEnumerable().FirstOrDefault(...)` |
| `array.Any(...)` | `array.AsValueEnumerable().Any(...)` |
| `array.SelectMany(...)` | `array.AsValueEnumerable().SelectMany(...)` |
| `array.Distinct()` | `array.AsValueEnumerable().Distinct()` |
| `array.ToArray()` (结尾) | `....ToArray()` (ZLinq 原生支持) |
| `array.ToList()` (结尾) | `....ToList()` (ZLinq 原生支持) |

### 注意事项

- `AsValueEnumerable()` 后调用 `.Where()` 返回的是 `ValueEnumerable<T>`（ref struct），不能跨方法传递或异步持有
- ZLinq 的 `First()` 是终结操作（会遍历元素），不要多次调用
- 不需要启用 ZLinq DropIn Generator（全局替换 System.Linq），明确使用 `AsValueEnumerable()` 更可控
- 反射扫描链路如果遇到复杂泛型推断（例如 `SelectMany` 推断失败），优先改为清晰的 `foreach`，不要为了链式写法牺牲可编译性
