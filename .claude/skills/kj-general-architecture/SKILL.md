---
name: kj-general-architecture
description: >
  KJ Framework General 层架构指南。涵盖 IModel（业务模型生命周期：Priority+Load+Unload）、ModelAttribute（标记 class 用于自动 DI 注册）、ModelLifecycle（VContainer IPostStartable 驱动的模型管理器：LoadAll/UnloadAll/IDisposable，用 IReadOnlyList<Type> + IObjectResolver 契约防跨 scope 双加载）、ModelScanner（程序集扫描 [Model]）、GeneralContainerRegistration（反射扫描注册：[Model]→VContainer + [GameEvent]→MessagePipe）、GeneralLayerEntrypoint（由 Core 反射启动 General 子 scope，检查 IModelStartupStatus）。
  触发场景：创建新业务模型、理解 IModel 生命周期、配置 General 层 DI 注册、添加业务事件订阅、理解 Model vs System 的命名约定、General 层启动排错。
  核心规则：业务层用 [Model]+IModel（而不是 [CoreSystem]+ISystem）；ModelLifecycle 由 VContainer IPostStartable 在 Core Start 成功后驱动；反射只在注册时使用，运行时走构造函数 DI；[Model] 类必须在 General.* 或 Project.* 命名空间；单个 model Load/Unload 失败不阻塞其他 model；每层独立 ModelLifecycle 只管理本层模型。
metadata:
  doc: CODEMAP.md
  layer: General
---

# KJ General 层 — 通用业务架构

源码在 `Assets/Scripts/General/`，完整文档见 `CODEMAP.md` Layer: General 章节，分层启动链见 `.planning/STATE.md`。

## 架构速查

```
CoreLayerEntrypoint.PostStart (IPostStartable, Core scope)
    └─ 反射 General.Bootstrap.GeneralStartup.Start(coreScope)
         └─ coreScope.CreateChild<GeneralLifetimeScope>()   ← General 子 scope
              └─ GeneralLifetimeScope.Configure
                   ├─ RegisterBusinessEvents()  — 扫描本层 [GameEvent] → 注册 MessagePipe Broker
                   ├─ RegisterModels()          — ModelScanner 扫本层 [Model] → Type[] scoped 契约 + 实例注册
                   ├─ RegisterModelLifecycle()  — 注册本层 ModelLifecycle (Singleton)
                   └─ RegisterEntryPoint<GeneralLayerEntrypoint>()
              └─ ModelLifecycle.PostStart (IPostStartable) → LoadAll()
              └─ GeneralLayerEntrypoint.PostStart → 反射 ProjectStartup

IModel                  — 业务模型协议 (Priority + Load + Unload)
ModelAttribute          — [Model] 标记特性 (AttributeTargets.Class, Inherited=false)
ModelScanner            — 程序集过滤扫描 [Model] → Type[]
ModelLifecycle          — 模型生命周期管理器 (IPostStartable / LoadAll / UnloadAll / IDisposable / IModelStartupStatus)
IModelStartupStatus     — 模型加载状态查询 (IsLoaded / HasFailures / FailedModelNames)
```

## 核心概念：Model vs System

| | ISystem (Core 层) | IModel (General/Project 层) |
|---|---|---|
| **标记** | `[CoreSystem]` | `[Model]` |
| **管理器** | `SystemManager` | `ModelLifecycle` |
| **驱动** | VContainer (IStartable + ITickable) | VContainer (IPostStartable + IDisposable) |
| **Tick** | 支持 Update/LateUpdate/FixedUpdate | 不支持（纯业务模型） |
| **用途** | 引擎基础设施 | 业务领域建模 |

❌ 业务层永远不用 `System` 命名，不用 `[CoreSystem]`。

## 核心 API

### IModel — 业务模型协议

```csharp
public interface IModel
{
    int Priority { get; }   // 越小越先 Load，越后 Unload
    void Load();            // 初始化模型（注册事件、加载数据等）
    void Unload();          // 清理模型（注销事件、释放资源等）
}
```

### ModelAttribute — 标记特性

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModelAttribute : Attribute { }
```

标记在实现 `IModel` 的 class 上，`ModelScanner` 通过反射发现，`RegisterModels()` 自动注册到 VContainer。

### ModelScanner — 程序集扫描

```csharp
public static class ModelScanner
{
    public static Type[] ScanModelTypes(Assembly assembly);  // 只扫本层程序集 [Model]+IModel → Type[]
}
```

### ModelLifecycle — 生命周期管理

```csharp
// 每层独立注册为 Singleton（General/Project 各有自己的 ModelLifecycle）
// 用 IReadOnlyList<Type> scoped 契约 + IObjectResolver 惰性解析：
//   - Type[] 由 RegisterModels 注册为 IReadOnlyList<Type>（子 scope 覆盖父 scope）
//   - 避免 IEnumerable<IModel> 跨 scope 聚合（Project 拿到 General 模型双加载）
public sealed class ModelLifecycle : IPostStartable, IDisposable, IModelStartupStatus
{
    public ModelLifecycle(
        IReadOnlyList<Type> modelTypes,
        IObjectResolver resolver,
        ICoreStartupStatus coreStartupStatus,
        ILogger<ModelLifecycle> logger);

    public void PostStart(); // VContainer 调用 → LoadAll()（检查 ICoreStartupStatus）
    public void LoadAll();    // 解析全部 Type → 实例，按 Priority 升序调用 Load()
    public void UnloadAll();  // 按加载逆序调用 Unload()
    public void Dispose();    // → UnloadAll()
    // IModelStartupStatus: IsLoaded / HasFailures / FailedModelNames
}
```

**错误隔离**: 单个 model 的 `Load()` 或 `Unload()` 抛出异常会被 catch 并 log，不会阻塞其他 model 的加载/卸载，但 `HasFailures` 记录失败并阻断上层启动。

### GeneralContainerRegistration — DI 注册（拆分方法）

```csharp
public static class GeneralContainerRegistration
{
    public static void RegisterBusinessEvents(
        this IContainerBuilder builder,
        MessagePipeOptions options,
        params Assembly[] assemblies);
    public static void RegisterModels(
        this IContainerBuilder builder,
        params Assembly[] assemblies);
    public static void RegisterModelLifecycle(
        this IContainerBuilder builder);
}
```

三步注册流程（在 `GeneralLifetimeScope.Configure` 中依次调用）：
1. **RegisterBusinessEvents** — 调用 `GameEventTypeScanner.FindGameEventTypes(assemblies)`，通过反射为每个 `[GameEvent]` 类型调用 `builder.RegisterMessageBroker<T>(options)`。**不调用 `RegisterMessagePipe`**（消息域由 Core scope 统一建立，分层启动计划 §0.1）
2. **RegisterModels** — 用 `ModelScanner` 扫本层 assemblies 的 `[Model]+IModel` 非抽象 class，注册为 `AsSelf().AsImplementedInterfaces()` + `Lifetime.Singleton`，并把 `Type[]` 注册为 `IReadOnlyList<Type>` scoped 契约
3. **RegisterModelLifecycle** — 幂等注册本层 `ModelLifecycle` 为 Singleton

### GeneralStartup / GeneralLifetimeScope — 分层入口

```csharp
public static class GeneralStartup
{
    public static void Start(LifetimeScope parentScope);  // 被 CoreLayerEntrypoint 反射调用
}
```

`GeneralStartup.Start` 从 `parentScope.Container.Resolve(typeof(MessagePipeOptions))` 拿消息域配置，存入 `GeneralLifetimeScope.PendingMessagePipeOptions`，然后 `parentScope.CreateChild<GeneralLifetimeScope>()` 创建子 scope。`GeneralLifetimeScope.Configure` 消费 options 注册本层事件/模型/ModelLifecycle/GeneralLayerEntrypoint。

### GeneralLayerEntrypoint — 层启动入口

```csharp
public sealed class GeneralLayerEntrypoint : IPostStartable, IDisposable
{
    // 注入 IModelStartupStatus（本层 ModelLifecycle）+ IObjectResolver（拿 ApplicationOrigin scope）
    public void PostStart();  // 检查 IModelStartupStatus，成功后反射 ProjectStartup
}
```

在 `ModelLifecycle.PostStart`（IPostStartable 先注册先触发）之后执行。仅当本层模型全部加载成功才反射启动 Project；否则记录失败并阻断。

## 创建新 Model 的步骤

```csharp
// 1. 放在 Scripts/General/ 或 Scripts/Project/ 下
// 2. 实现 IModel，标记 [Model]
using General;

[Model]
public class PlayerModel : IModel
{
    public int Priority => 100;

    public void Load()
    {
        // 订阅事件、加载数据
    }

    public void Unload()
    {
        // 注销事件、释放资源
    }
}
```

完成。无需手动注册 — `RegisterModels` 会通过 `ModelScanner` 反射自动发现并注册。

## 最佳实践

1. **用 [Model]+IModel 做业务建模** — 而不是 [CoreSystem]+ISystem。System 是引擎层概念。
2. **Priority 合理规划** — 被依赖的 model 给较小值（先 Load），依赖别人的给较大值（后 Load）
3. **Load/Unload 必须幂等** — ModelLifecycle 有 double-load 防护，但 model 自身也应处理
4. **Model 不Tick** — 需要帧更新的逻辑放在 Core System 中，通过事件驱动 model
5. **错误隔离是保障，不是借口** — Load/Unload 异常会被吃掉，务必在 model 内部做好错误处理
6. **[Model] 必须实现 IModel** — 否则注册阶段抛 InvalidOperationException
7. **同类型模型只能一个实例** — `IReadOnlyList<Type>` 契约按 Type 解析，同类型多实例无法区分
8. **模型在 Core 系统 Start 成功后加载** — `ModelLifecycle` 通过 `IPostStartable.PostStart()` 检查 `ICoreStartupStatus`，Core Init 失败时跳过 `LoadAll()`
9. **每层独立 ModelLifecycle** — General/Project 各有自己的 ModelLifecycle 和 `IReadOnlyList<Type>`，互不混管
