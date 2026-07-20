---
name: kj-event
description: >
  KJ Framework 事件系统指南。涵盖 GameEventAttribute（标记 struct 为游戏事件）、GameEventTypeScanner（反射扫描所有 [GameEvent] 类型，安全处理 ReflectionTypeLoadException）。
  触发场景：定义新事件、发布/订阅事件、理解事件注册流程、扫描跨程序集事件类型。
  核心规则：事件必须是 readonly struct；[GameEvent] 只能标记 struct；Framework.Event 零外部依赖，不直接绑定 MessagePipe；实际 MessageBroker 注册在 Core 层 CoreTypeRegistration 中通过反射完成。
metadata:
  doc: CODEMAP.md
  layer: Framework
---

# KJ 事件系统 (Framework.Event)

源码在 `Assets/Framework/Event/`，完整文档见 `CODEMAP.md` Framework: Event 章节。

## 架构速查

```
Framework.Event (零外部依赖)
├── GameEventAttribute     — [AttributeUsage(Struct)] 标记事件类型
└── GameEventTypeScanner   — 反射扫描，返回 IReadOnlyList<Type>

Core.Bootstrap.CoreTypeRegistration (注册层)
└── RegisterGameEvents()   — 调用 GameEventTypeScanner + 反射调用 RegisterMessageBroker<T>
```

## 核心概念

**Framework.Event 只是标记+发现机制，不绑定任何消息后端。** 实际的消息发布/订阅由 MessagePipe 提供。

### 定义事件

```csharp
using Framework.Event;

[GameEvent]
public readonly struct AppStartedEvent { }

[GameEvent]
public readonly struct PlayerDiedEvent
{
    public readonly int PlayerId;
    public readonly Vector3 Position;

    public PlayerDiedEvent(int playerId, Vector3 position)
    {
        PlayerId = playerId;
        Position = position;
    }
}
```

**强制约束:**
- 必须是 `struct`（值类型），`GameEventTypeScanner` 运行时会校验
- 推荐 `readonly struct`（零分配，纯数据载体）
- 不能是枚举

### 扫描事件类型

```csharp
// 扫描指定程序集中的所有 [GameEvent] 类型
var types = GameEventTypeScanner.FindGameEventTypes(typeof(MyEvent).Assembly);

// 安全扫描：ReflectionTypeLoadException 会被内部捕获
var allTypes = GameEventTypeScanner.FindGameEventTypes(asm1, asm2, asm3);
```

### DI 注册流程（幕后自动完成）

```
CoreTypeRegistration.RegisterGameEvents()
  │
  ├─ 调用 GameEventTypeScanner.FindGameEventTypes(assemblies)
  │     └─ 过滤: type.IsValueType && !type.IsEnum && [GameEvent] 存在
  │
  └─ 对每个类型，反射调用:
       MessagePipe.ContainerBuilderExtensions.RegisterMessageBroker<T>(builder, options)
         └─ 注册 IMessageBroker<T> 到 VContainer
```

### 发布/订阅事件

```csharp
// 订阅 (任意层，只要 assemblies 被扫描到)
public class MySystem : ISystem
{
    private readonly ISubscriber<AppStartedEvent> _subscriber;

    public MySystem(ISubscriber<AppStartedEvent> subscriber)
    {
        _subscriber = subscriber;
    }

    public void Init()
    {
        _subscriber.Subscribe(e => Debug.Log("App started!"));
    }
}

// 发布
public class SomeManager
{
    private readonly IPublisher<PlayerDiedEvent> _publisher;

    public void OnPlayerDied(int playerId, Vector3 pos)
    {
        _publisher.Publish(new PlayerDiedEvent(playerId, pos));
    }
}
```

## 当前已定义事件

| 事件 | 命名空间 | 发布者 | 时机 |
|------|----------|--------|------|
| `AppStartedEvent` | `Core.Systems.Events` | `SystemManager.InitAll()` | 所有 Core 系统初始化成功 |
| `AppShuttingDownEvent` | `Core.Systems.Events` | `SystemManager.ShutdownAll()` | 系统即将关闭 |
| `AssetSystemReadyEvent` | `Core.Asset` | `AssetSystem.Init()` | 资源系统就绪 |

## 最佳实践

1. **事件定义为 readonly struct** — 零 GC 分配，不可变语义
2. **事件命名以 Event 结尾** — `XxxEvent` (如 `PlayerLevelUpEvent`)
3. **事件字段用 readonly** — 通过构造函数初始化，发布后不可修改
4. **事件尽量轻量** — 只传 ID/key 值，订阅者自行查询详情
5. **新事件放对层级** — Core 事件放 `Core.Systems.Events/`，业务事件放 `General/Events/` 或 `Project/<Feature>/Data/`
6. **不跨层依赖事件类型** — Project 可以引用 Core 的事件，但不要反过来

## 扩展

如需自定义事件后端（不用 MessagePipe），只需修改 `CoreTypeRegistration.RegisterGameEvents()` 和 `GeneralContainerRegistration.RegisterBusinessEvents()` 中的注册逻辑。`GameEventAttribute` 和 `GameEventTypeScanner` 不需要改。
