---
name: kj-boot
description: >
  KJ Framework Boot 层指南。涵盖 Entry（游戏入口+MonoBehaviour+DontDestroyOnLoad）、AppLifetimeScope（VContainer LifetimeScope 抽象基类）、IBootstrapStage（普通 C# 启动阶段协议：Priority+StageName+Configure）、BootstrapContext（启动上下文：IContainerBuilder+类型化值存储+ConfigureStages）、BootLifetimeScope（通过序列化类型名反射创建启动阶段）。触发场景：理解启动流程、添加新启动阶段、调试 Boot/Core/General/Project 注册顺序、配置 BootstrapStage 优先级、保持 Boot 最小依赖。核心规则：Boot 层只引用 VContainer 和稳定 Framework 基础包；不引用 Core/General/Project；Stage 实现为普通 C# 类，不继承 MonoBehaviour；Boot 通过 assembly-qualified type name 反射创建 Stage；启动资源 prefab 不再作为阶段载体。
metadata:
  doc: CODEMAP.md
  layer: Boot
---

# KJ Boot 层 — 应用启动

源码在 `Assets/Scripts/Boot/`，完整文档见 `CODEMAP.md` Layer: Boot 章节。

## 架构速查

```
Entry.cs                 — Awake() → DontDestroyOnLoad(gameObject)
AppLifetimeScope.cs      — abstract LifetimeScope 基类
Bootstrap/
├── IBootstrapStage.cs   — 普通 C# 阶段协议
├── BootstrapContext.cs  — IContainerBuilder + 类型化值存储 + ConfigureStages
└── BootLifetimeScope.cs — 从序列化类型名反射创建 Stage 并执行
```

## 启动流程

```
Entry.Awake()
  ↓
BootLifetimeScope.Configure(IContainerBuilder)
  ├─ 根据 bootstrapStageTypeNames 反射创建 IBootstrapStage 实例
  ├─ BootstrapContext.ConfigureStages(stages)
  │   ├─ 去重 stage 类型
  │   ├─ 按 Priority 升序排序
  │   └─ 逐个 stage.Configure(context)
  ↓
CoreBootstrapStage (Priority=100)
  ├─ builder.RegisterCoreServices()
  └─ context.Set<MessagePipeOptions>(options)
  ↓
GeneralBootstrapStage (Priority=200)
  ├─ context.GetRequired<MessagePipeOptions>()
  └─ builder.RegisterBusinessLayer(options, GeneralAssembly)
  ↓
ProjectBootstrapStage (Priority=300)
  ├─ context.GetRequired<MessagePipeOptions>()
  └─ ProjectBootstrapper.Configure(builder, options)
```

## 核心约束

- Boot asmdef 只引用 VContainer 和稳定 Framework 基础包（如 `Framework.Log`），不直接引用 Core/General/Project。
- `IBootstrapStage` 保留在 Boot；实现类放在所属层的 `Bootstrap/` 目录。
- Stage 是普通 C# 类：不要继承 `MonoBehaviour`，不要依赖 prefab/Inspector 字段传递下一阶段。
- Boot 通过 assembly-qualified type name 创建 stage，例如 `Core.Bootstrap.CoreBootstrapStage, Core`；序列化列表为空时使用默认 Core/General/Project。
- 被类型名反射创建的 Stage 必须加 `UnityEngine.Scripting.Preserve`，避免 IL2CPP managed stripping 裁掉。
- 阶段顺序由 `Priority` 控制，不由配置数组顺序或 Unity 对象层级控制。
- 阶段间共享值使用 `BootstrapContext.Set<T>()` / `GetRequired<T>()`，不要用静态变量接力。

## 新增 Stage

```csharp
using Boot;
using UnityEngine.Scripting;

namespace Core.Foo
{
    [Preserve]
    public sealed class FooBootstrapStage : IBootstrapStage
    {
        public int Priority => 150;
        public string StageName => "Foo";

        public void Configure(BootstrapContext context)
        {
            // 注册本层服务
        }
    }
}
```

然后把类型名加入 `BootLifetimeScope.bootstrapStageTypeNames`。

## 最佳实践

1. Entry 永远保持最小：只做 `DontDestroyOnLoad` 和启动日志。
2. 启动阶段只负责 DI/MessagePipe/上下文注册，不做运行时业务初始化。
3. Core 注册 `MessagePipeOptions` 后用 `context.Set(options)` 传给 General/Project。
4. General/Project 业务用 `[Model]+IModel`，不要在 Boot 阶段里手动 new 业务对象。
5. 如果一个阶段依赖另一个阶段的上下文值，给被依赖阶段更小的 Priority。
6. `Resources/` 只放最小启动配置（如 `AssetConfig.asset`），不要放 stage prefab 或场景。
