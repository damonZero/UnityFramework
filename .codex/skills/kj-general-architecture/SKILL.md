---
name: kj-general-architecture
description: >
  KJ Framework General 层架构指南。涵盖 IModel（业务模型生命周期：Priority+Load+Unload）、ModelAttribute（标记 class 用于自动 DI 注册）、ModelLifecycle（VContainer IPostStartable 驱动的模型管理器：LoadAll/UnloadAll/IDisposable）、GeneralContainerRegistration（反射扫描注册：[Model]→VContainer + [GameEvent]→MessagePipe）、GeneralBootstrapStage（由 ProjectLifetimeScope 在 Core 之后调用，编排业务层注册）。
  触发场景：创建新业务模型、理解 IModel 生命周期、配置 General 层 DI 注册、添加业务事件订阅、理解 Model vs System 的命名约定、GeneralBootstrapStage 排错。
  核心规则：业务层用 [Model]+IModel（而不是 [CoreSystem]+ISystem）；ModelLifecycle 由 VContainer IPostStartable 在 Core Start 成功后驱动；反射只在注册时使用，运行时走构造函数 DI；[Model] 类必须在 General.* 或 Project.* 命名空间；单个 model Load/Unload 失败不阻塞其他 model。
metadata:
  doc: CODEMAP.md
  layer: General
---

# KJ General 层 — 通用业务架构

源码在 `Assets/Scripts/General/`，完整文档见 `CODEMAP.md` Layer: General 章节。

## 架构速查

```
GeneralBootstrapStage (Priority=200)
    └─ GeneralContainerRegistration.RegisterBusinessLayer()
         ├─ RegisterBusinessEvents()  — 扫描 [GameEvent] → 注册 MessagePipe Broker
         ├─ RegisterModels()          — 扫描 [Model]+IModel → 注册 VContainer Singleton
         └─ builder.Register<ModelLifecycle>(Singleton) once

IModel                  — 业务模型协议 (Priority + Load + Unload)
ModelAttribute          — [Model] 标记特性 (AttributeTargets.Class, Inherited=false)
ModelLifecycle          — 模型生命周期管理器 (IPostStartable / LoadAll / UnloadAll / IDisposable)
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

标记在实现 `IModel` 的 class 上，`GeneralContainerRegistration.RegisterModels()` 通过反射发现并自动注册到 VContainer。

### ModelLifecycle — 生命周期管理

```csharp
// 由 GeneralContainerRegistration 注册为 Singleton
// VContainer 自动注入所有 IModel 实例
public sealed class ModelLifecycle : IPostStartable, IDisposable
{
    public ModelLifecycle(IEnumerable<IModel> models);  // 自动按 Priority 排序

    public void PostStart(); // VContainer 调用 → LoadAll()
    public void LoadAll();    // 按 Priority 升序调用 Load()
    public void UnloadAll();  // 按 Priority 降序调用 Unload()
    public void Dispose();    // → UnloadAll()
}
```

**错误隔离**: 单个 model 的 `Load()` 或 `Unload()` 抛出异常会被 catch 并 log，不会阻塞其他 model 的加载/卸载。

### GeneralContainerRegistration — DI 注册

```csharp
public static class GeneralContainerRegistration
{
    public static void RegisterBusinessLayer(
        this IContainerBuilder builder,
        MessagePipeOptions options,
        params Assembly[] assemblies);
}
```

三步注册流程：
1. **RegisterBusinessEvents** — 调用 `GameEventTypeScanner.FindGameEventTypes(assemblies)`，通过反射为每个 `[GameEvent]` 类型调用 `builder.RegisterMessageBroker<T>(options)`
2. **RegisterModels** — 扫描 assemblies 中所有带 `[Model]` 且实现 `IModel` 的非抽象 class，注册为 `AsSelf().As<IModel>()` + `Lifetime.Singleton`。如果 `[Model]` 标记的类未实现 `IModel` → `InvalidOperationException`
3. 幂等注册 `ModelLifecycle` 为 Singleton；General 和 Project 分批调用时只能有一个生命周期管理器

### GeneralBootstrapStage — 业务层注册阶段

```csharp
public static class GeneralBootstrapStage
{
    public static void Configure(CoreStartupContext context)
    {
        var options = context.MessagePipeOptions;
        if (options == null)
            throw new InvalidOperationException("MessagePipeOptions is missing. CoreBootstrapStage must run before GeneralBootstrapStage.");

        context.Builder.RegisterBusinessLayer(options, typeof(GeneralBootstrapStage).Assembly);
    }
}
```

在正式游戏容器构建中由 `ProjectLifetimeScope` 在 `CoreBootstrapStage.Configure(context)` 之后、`ProjectBootstrapStage.Configure(context)` 之前调用。从 `CoreStartupContext` 获取 Core 阶段存入的 `MessagePipeOptions`，然后调用 `RegisterBusinessLayer` 扫描 General 程序集。

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

完成。无需手动注册 — `RegisterBusinessLayer` 会通过反射自动发现并注册。

## 最佳实践

1. **用 [Model]+IModel 做业务建模** — 而不是 [CoreSystem]+ISystem。System 是引擎层概念。
2. **Priority 合理规划** — 被依赖的 model 给较小值（先 Load），依赖别人的给较大值（后 Load）
3. **Load/Unload 必须幂等** — ModelLifecycle 有 double-load 防护，但 model 自身也应处理
4. **Model 不Tick** — 需要帧更新的逻辑放在 Core System 中，通过事件驱动 model
5. **错误隔离是保障，不是借口** — Load/Unload 异常会被吃掉，务必在 model 内部做好错误处理
6. **[Model] 必须实现 IModel** — 否则注册阶段抛 InvalidOperationException
7. **RegisterBusinessLayer 可分批调用** — General/Project 可分别扫描自己的程序集，但 `ModelLifecycle` 和同一 model 类型注册必须幂等
8. **模型在 Core 系统 Start 成功后加载** — `ModelLifecycle` 通过 `IPostStartable.PostStart()` 检查 `ICoreStartupStatus`，Core Init 失败时跳过 `LoadAll()`
