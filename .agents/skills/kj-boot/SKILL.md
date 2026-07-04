---
name: kj-boot
description: >
  KJ Framework Boot 层指南。涵盖 Entry（游戏入口+MonoBehaviour+DontDestroyOnLoad）、AppLifetimeScope（VContainer LifetimeScope 抽象基类）、IBootstrapStage（启动阶段协议：Priority+StageName+Configure）、BootstrapContext（启动上下文：IContainerBuilder+Transform+类型化值存储+ConfigurePrefab 链式加载）、BootLifetimeScope（链式启动入口，通过 serializedField nextBootstrapPrefabPath 加载下一阶段 prefab）。
  触发场景：理解启动流程、添加新启动阶段、调试启动链、配置 BootstrapStage 优先级、链式加载预制体、理解 Entry 点的最小依赖原则。
  核心规则：Boot 层只能引用 VContainer（最小依赖）；不引用 Core/General/Project；通过 IBootstrapStage 协议发现阶段；ConfigurePrefab 从 Resources 加载 prefab 并链式调用；同一个 prefab 不能配置两次。
metadata:
  doc: CODEMAP.md
  layer: Boot
---

# KJ Boot 层 — 应用启动

源码在 `Assets/Scripts/Boot/`，完整文档见 `CODEMAP.md` Layer: Boot 章节。

## 架构速查

```
Entry.cs                          — Awake() → DontDestroyOnLoad(gameObject)
AppLifetimeScope.cs               — abstract LifetimeScope 基类
Bootstrap/
├── IBootstrapStage.cs            — 阶段协议
├── BootstrapContext.cs           — 上下文 + 链式 prefab 加载
└── BootLifetimeScope.cs          — 启动入口 LifetimeScope
```

## 启动流程

```
Entry.Awake() → DontDestroyOnLoad
    ↓
BootLifetimeScope.Configure(IContainerBuilder)
    └─ BootstrapContext.ConfigurePrefab(nextBootstrapPrefabPath)
         ├─ Resources.Load<GameObject>(prefabPath)
         ├─ Instantiate under StageRoot
         ├─ GetComponentsInChildren<IBootstrapStage> (按 Priority 排序)
         └─ foreach stage.Configure(context)
  ↓
CoreBootstrapStage (Priority=100)
    ├─ builder.RegisterCoreServices()  // MessagePipe + Core 系统
    ├─ context.Set(options)
    └─ context.ConfigurePrefab(nextBootstrapPrefabPath)
         ↓
GeneralBootstrapStage (Priority=200)
    ├─ context.GetRequired<MessagePipeOptions>()
    ├─ builder.RegisterBusinessLayer(options, GeneralAssembly)
    └─ context.ConfigurePrefab(nextBootstrapPrefabPath)
         ↓
ProjectBootstrapStage (Priority=300)
    ├─ context.GetRequired<MessagePipeOptions>()
    ├─ ProjectBootstrapper.Configure(builder, options)
    └─ context.ConfigurePrefab(nextBootstrapPrefabPath)  // 可选终端
```

## 核心组件

### Entry — 入口

```csharp
// 挂载在场景中的最小启动壳
// Awake 只做 DontDestroyOnLoad，确保跨场景存活
public class Entry : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("[Entry] 游戏启动");
    }
}
```

### AppLifetimeScope — VContainer 根 LifetimeScope

```csharp
// 抽象基类，空实现。由 BootLifetimeScope 继承。
// 提供扩展点：如果想要不同的启动策略，继承此类。
public abstract class AppLifetimeScope : LifetimeScope { }
```

### IBootstrapStage — 阶段协议

```csharp
public interface IBootstrapStage
{
    int Priority { get; }        // 越小越先执行
    string StageName { get; }     // 日志用
    void Configure(BootstrapContext context);  // 注册 DI + 链式加载下一阶段
}

// 实现示例：
// CoreBootstrapStage — Priority=100, "Core"
// GeneralBootstrapStage — Priority=200, "General"
// ProjectBootstrapStage — Priority=300, "Project"
```

### BootstrapContext — 启动上下文

```csharp
var context = new BootstrapContext(builder, transform);

// 类型化值存储（阶段间通信）
context.Set<MessagePipeOptions>(options);           // Core 存入
var options = context.GetRequired<MessagePipeOptions>(); // General/Project 取出

// 链式 prefab 加载
context.ConfigurePrefab("Core");  // 从 Resources 加载 Core.prefab
// 自动: Instantiate → 找所有 IBootstrapStage → 按 Priority 排序 → Configure

// 防护
// - 空路径跳过
// - 同一个 prefab 配置两次 → InvalidOperationException
// - prefab 没有 IBootstrapStage → InvalidOperationException
// - prefab 不存在 → InvalidOperationException
```

### BootLifetimeScope — 链式启动入口

```csharp
public sealed class BootLifetimeScope : AppLifetimeScope
{
    [SerializeField] private string nextBootstrapPrefabPath = "Core";
    // 在 Unity Editor 中配置，指向 CoreBootstrapStage 所在的 prefab
}
```

## 启动优先级

| 阶段 | Priority | 职责 |
|------|----------|------|
| Core | 100 | RegisterCoreServices (MessagePipe + CoreSystem) |
| General | 200 | RegisterBusinessLayer (IModel + General 事件) |
| Project | 300 | ProjectBootstrapper (项目专属 Model + 事件) |
| 自定义 | 任意 | 插入新阶段，选择合适 Priority |

## 预置 Prefab 清单

这些 prefab 尚未创建（BOOT-CHAIN-02 待实现）：

| Prefab | 包含组件 | 链向 |
|--------|----------|------|
| `Resources/Core.prefab` | `CoreBootstrapStage` | `nextBootstrapPrefabPath = "General"` |
| `Resources/General.prefab` | `GeneralBootstrapStage` | `nextBootstrapPrefabPath = "Project"` |
| `Resources/Project.prefab` | `ProjectBootstrapStage` + `ProjectBootstrapper` | 可选 |

## 最佳实践

1. **不要修改 Entry** — 它是最小启动壳，永远只做 DontDestroyOnLoad
2. **通过 Priority 控制执行顺序** — 不要依赖 prefab 的 Inspector 排序
3. **用 BootstrapContext.Set/Get 传值** — 而不是 Singleton 或静态变量
4. **ConfigurePrefab 自动验证** — 利用内置的错误检测（重复配置、无 stage、prefab 不存在）
5. **新启动阶段实现 IBootstrapStage + MonoBehaviour** — 放在对应层级的 Bootstrap/ 目录
6. **Boot 只引用 VContainer** — 不要给 Boot.asmdef 加任何 Core/General/Project 引用
