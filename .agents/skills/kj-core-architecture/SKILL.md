---
name: kj-core-architecture
description: >
  KJ Framework Core 架构层指南。涵盖 ISystem（系统生命周期：Priority+Init+Shutdown）、ITickableSystem（可驱动系统：Update/LateUpdate/FixedUpdate）、SystemManager（VContainer 驱动的系统管理器：IStartable+ITickable+ILateTickable+IFixedTickable+IDisposable）、CoreSystemAttribute（标记 Core 系统用于自动 DI 注册）、ArchitectureContainerRegistration（反射扫描注册：[CoreSystem]→VContainer + [GameEvent]→MessagePipe）、CoreContainerRegistration（RegisterCoreServices 扩展方法）、启动事件（AppStartedEvent/AppShuttingDownEvent）、StartupProbeSystem（启动链路验证）。
  触发场景：创建新 Core 系统、理解系统生命周期、配置 DI 注册、添加事件订阅、理解 SystemManager Tick 调度、注册 MessagePipe 事件 Broker。
  核心规则：Core 层用 [CoreSystem]+ISystem（业务层用 [Model]+IModel）；SystemManager 由 VContainer 驱动；反射只在注册时使用，运行时走构造函数 DI；[CoreSystem] 类必须在 Core.* 命名空间。
metadata:
  doc: CODEMAP.md
  layer: Core
---

# KJ Core 架构层

源码在 `Assets/Scripts/Core/` 和 `Assets/Scripts/Core/Architecture/`，完整文档见 `CODEMAP.md` Layer: Core 章节。

## 架构速查

```
ISystem / ITickableSystem          — 系统生命周期接口
    ↑
[CoreSystem] 标记类                — 自动扫描注册到 VContainer
    ↑
SystemManager                     — 管理所有 ISystem 的生命周期 + Tick 驱动
    ↑ (VContainer)
CoreContainerRegistration         — 入口：RegisterCoreServices()
ArchitectureContainerRegistration — 反射：RegisterGameEvents() + RegisterSystems()
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
  ├─ 3. builder.RegisterArchitecture(options, CoreAssembly)     → ArchitectureContainerRegistration
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
  └─ _appStartedPublisher.Publish(new AppStartedEvent())

Unity 主循环:
  ITickable.Tick() → SystemManager.Tick()
    → foreach ITickableSystem: Update(Time.deltaTime)
  ILateTickable.LateTick() → SystemManager.LateTick()
    → foreach ITickableSystem: LateUpdate(Time.deltaTime)
  IFixedTickable.FixedTick() → SystemManager.FixedTick()
    → foreach ITickableSystem: FixedUpdate(Time.fixedDeltaTime)

应用退出:
  IDisposable.Dispose() → SystemManager.Dispose() → ShutdownAll()
    ├─ 发布 AppShuttingDownEvent
    ├─ 按 Priority 降序调用 Shutdown()
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
AppStartedEvent       // SystemManager.InitAll() 最后发布
AppShuttingDownEvent   // SystemManager.ShutdownAll() 最前发布
AssetSystemReadyEvent  // AssetSystem.Init() 发布
```

## 最佳实践

1. **用 [CoreSystem] 标记，不要手动 Register** — 反射自动注册，零配置
2. **Priority 要合理** — 被依赖的系统 Priority 更小（先初始化）
3. **构造函数注入依赖** — VContainer 自动解析，不手动 new
4. **Init 做初始化，Shutdown 做逆序清理** — 不要跨生命周期泄漏状态
5. **TickableSystem 保持轻量** — Update 每帧调用，避免重计算
6. **系统命名用 XxxSystem** — 放在 Core 下对应功能目录
7. **[CoreSystem] 类必须在 Core.* 命名空间** — ArchitectureContainerRegistration 运行时会校验
8. **不要手动持有 SystemManager 引用** — 用 VContainer 注入依赖系统
