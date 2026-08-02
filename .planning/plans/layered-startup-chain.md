# KJ 分层启动链重构计划

> 状态：**全部完成（Phase 0-5 + Review 修复）**
> 日期：2026-07-31
> 适用项目：KJ Unity 2022.3.62f2
> 核心决策：Boot -> Core -> General -> Project 逐层启动；每层拥有独立 VContainer Scope、入口和生命周期
> 修订：2026-08-02 Phase 0-5 全部验证通过；Review 修复：Repair 重建（Reset 反射）、反射去重（LayerStartupReflector）、死字段清理、排序修复
>
> **实施进度：**
> - ✅ Phase 0：ModelScanner / IModelStartupStatus / 拆分 RegisterBusinessLayer（完成）
> - ✅ Phase 1：Core root scope（CoreStartup / CoreLifetimeScope / CoreLayerEntrypoint）—— Editor Play 验证通过
> - ✅ Phase 2：General child scope（GeneralStartup / GeneralLifetimeScope / GeneralLayerEntrypoint）—— Editor Play 验证通过
> - ✅ Phase 3：Project child scope（ProjectStartup / ProjectLifetimeScope / ProjectLayerEntrypoint）—— Editor Play 验证通过
> - ✅ Phase 4：异常与释放（Boot 异常可观察 + TargetInvocationException 解包 + child scope 清理确认；UI 通知为日志阻断取舍）—— 代码完成，待编译验证
> - ⏳ Phase 5：文档与构建工具

---

## 0. 前置设计决议（先读此节，影响所有阶段）

> 本节的三个决策是所有后续阶段的**前置条件**，不在实现中再做选择。

### 0.1 事件域：全链唯一消息域，事件向上可见

**决议：`RegisterMessagePipe()` 全链只允许在 Core scope 调用一次。** General/Project 不创建独立消息域。

```
Core scope:
  var options = builder.RegisterMessagePipe();        ← 唯一一次，基础设施 + 共享 provider
  确保 MessagePipeOptions 可被子层解析（MessagePipe 通常自动注册；
  若未注册则 builder.RegisterInstance(options).As<MessagePipeOptions>()）
  RegisterMessageBroker<CoreEvent>                     ← Core 自己的事件

General scope:
  不调用 RegisterMessagePipe()                         ← 红线：调用即创建第二消息域，事件全断
  从 Core 容器解析 MessagePipeOptions
  RegisterMessageBroker<GeneralEvent>                  ← 只注册 General 自己的事件

Project scope:
  同上，只注册 Project 事件
```

**事件可见性（与依赖方向完全一致）：**

| 发布方 | 可订阅方 | 机制 |
|--------|----------|------|
| Core | General / Project | 子 scope 解析父容器的 broker（共享 provider） |
| General | Project | 同上 |
| Project | General / Core | ❌ 不可见（父不可见子，方向正确） |

**options 跨层传递：** `Configure()` 只能拿到 `IContainerBuilder`，不能从父容器解析。因此在入口方法中解析并存入本层 context：

```csharp
public static void Start(LifetimeScope parentScope)
{
    var options = parentScope.Container.Resolve<MessagePipeOptions>();  // Core 容器已构建
    GeneralStartupContext.PendingOptions = options;                     // 供 Configure 消费，用后即清
    // ... 创建 GeneralLifetimeScope（parentScope child）
}
```

### 0.2 模型归属：程序集过滤 + scoped 类型契约（不是 `IEnumerable<IModel>`）

**决议：每层只管理本层程序集扫出的模型。** 但仅靠程序集过滤**不够**——它解决"注册哪些类型"，而 `IEnumerable<IModel>` 注入会跨 scope 聚合父层注册，导致子层 ModelLifecycle 拿到父层模型并重复 Load。

**必须把扫描结果（`Type[]`）注册为 scoped 契约，ModelLifecycle 注入该契约 + `IObjectResolver` 惰性解析：**

```csharp
// 每层 Configure
var modelTypes = ModelScanner.ScanModelTypes(typeof(本层Marker).Assembly);  // 只扫本层程序集
foreach (var t in modelTypes) builder.Register(t, Lifetime.Singleton).AsSelf();  // 保留 DI 构造注入
builder.RegisterInstance(modelTypes).As<IReadOnlyList<Type>>();  // scoped 契约，子层覆盖父层

// ModelLifecycle
public ModelLifecycle(IReadOnlyList<Type> modelTypes, IObjectResolver resolver,
    ICoreStartupStatus coreStartupStatus, ILogger<ModelLifecycle> logger)
{
    _modelTypes = modelTypes ?? Array.Empty<Type>();
    _resolver = resolver;
}
public void LoadAll()
{
    foreach (var type in _modelTypes)
    {
        var model = (IModel)_resolver.Resolve(type);   // 惰性解析，构造注入可用
        try { model.Load(); }
        catch (Exception e) { _failedModelNames.Add(type.Name); }
    }
    _loaded = _failedModelNames.Count == 0;            // 失败即不算成功
}
```

**隔离原理：** `IReadOnlyList<Type>` 用 `RegisterInstance` 注册，VContainer 子 scope 解析按"最近作用域优先"，子层注册覆盖父层。因此 General 的 ModelLifecycle 拿到 General 的 Type[]，Project 的拿到 Project 的。

**禁止**：ModelLifecycle 继续注入 `IEnumerable<IModel>`（聚合语义，跨层混合）。

### 0.3 启动成功 = 本层状态可查询，不静默吞错

**决议：每层启动状态由本层入口实现可查询接口，失败即阻断下一层，不允许"记录错误后继续"。**

- `ICoreStartupStatus`（已有，`SystemManager` 实现）→ Core 层状态。
- 新增 `IModelStartupStatus`（`ModelLifecycle` 实现）：`IsLoaded / HasFailures / FailedModelNames`。
- General 层没有系统，启动状态直接以 `ModelLifecycle` 暴露的 `IModelStartupStatus` 为准，**不新增 `IGeneralStartupStatus` 空壳接口**。
- 单 Model Load 失败：**不抛中断**（延续现状，单个失败不阻塞其他），但**汇总失败并阻断下一层**。

---

## 1. 背景与问题

当前运行链为：

```text
BootUpdateRunner
  -> ProjectStartup.Start(IAssetRuntime)
      -> ProjectLifetimeScope.Configure()
          -> CoreBootstrapStage.Configure()
          -> GeneralBootstrapStage.Configure()
          -> ProjectBootstrapStage.Configure()
```

虽然注册顺序是 Core -> General -> Project，但三层在同一个 Project root scope 中一次完成注册。`CoreBootstrapStage`、`GeneralBootstrapStage` 和 `ProjectBootstrapStage` 只是注册函数，不是各层独立的启动与生命周期节点。

这与目标设计不符：

- Boot 应只完成更新并把运行时资源所有权交给 Core；
- Core 完整启动成功后才能启动 General；
- General 完整启动成功后才能启动 Project；
- 每一层应拥有自己的 VContainer Scope；
- 每层入口可实现 `IStartable`、`IPostStartable`、`IDisposable` 等接口并维护本层生命周期；
- 下层启动失败时，上层不得继续启动；
- 关闭时按 Project -> General -> Core 逆序释放。

## 2. 目标启动链

```text
Entry / BootLoader                         AOT Launcher
  -> BootUpdateRunner                     Hot-update Boot
      -> CoreStartup.Start(IAssetRuntime)  reflection
          -> CoreLifetimeScope
              -> CoreBootstrapStage.Configure
              -> SystemManager.Start / Core systems Init
              -> CoreLayerEntrypoint.PostStart
                  -> GeneralStartup.Start(CoreLifetimeScope)  reflection
                      -> GeneralLifetimeScope (Core child)
                          -> GeneralBootstrapStage.Configure
                          -> ModelLifecycle.PostStart / models Load
                          -> GeneralLayerEntrypoint.PostStart
                              -> ProjectStartup.Start(GeneralLifetimeScope) reflection
                                  -> ProjectLifetimeScope (General child)
                                      -> ProjectBootstrapStage.Configure
                                      -> ProjectLayerEntrypoint.PostStart
```

Scope 层级：

```text
CoreLifetimeScope
  `- GeneralLifetimeScope
       `- ProjectLifetimeScope
```

父 Scope 服务自动向子 Scope 可见，`IAssetRuntime` 只在 Boot -> Core 边界传递一次。General 和 Project 从父容器解析 Core 提供的稳定服务，不再直接接收 Boot 状态。

## 3. 强制架构约束

1. `Boot` 不引用 VContainer、Core、General 或 Project，继续通过配置的程序集限定名反射 Core 入口。
2. `Core` 不引用 General/Project；Core -> General 使用反射入口契约。
3. `General` 只引用 Core，不引用 Project；General -> Project 使用反射入口契约。
4. `Project` 可以引用所有下层，但不得反向负责 Core/General 注册。
5. Launcher 边界保持不变，不新增任何 Framework 或热更程序集引用。
6. Boot 创建的 `IAssetRuntime` 所有权只转移给 Core scope；Core scope 销毁时由 `AssetSystem` 关闭。
7. 一个层启动失败时停止后续层，不允许记录错误后仍继续创建下一层。
8. VContainer scope 是层生命周期所有者；不使用无所有者的静态全局容器。
9. **`RegisterMessagePipe()` 全链只允许在 Core scope 调用一次**（见 §0.1）；子层不得创建独立消息域。
10. **模型注册走 §0.2 的 scoped 类型契约**；禁止 `IEnumerable<IModel>` 聚合注入。

## 4. 入口契约

### 4.1 Boot -> Core

场景配置中的正式入口由：

```text
Project.Bootstrap.ProjectStartup, Project
```

改为：

```text
Core.Bootstrap.CoreStartup, Core
```

入口签名：

```csharp
public static void Start(IAssetRuntime assetRuntime)
```

`BootUpdateRunner.StartGame()` 保留通用反射签名校验，但其目标改为 Core。

### 4.2 Core -> General

程序集限定入口：

```text
General.Bootstrap.GeneralStartup, General
```

入口签名：

```csharp
public static void Start(LifetimeScope parentScope)
```

Core 不编译期引用 General。反射调用集中封装，检查类型、方法和参数契约，并保留原始内部异常。入口内部先用 `parentScope.Container` 解析 `MessagePipeOptions` 存入本层 context（见 §0.1）。

**⚠️ 创建本层 scope 必须用官方 `parentScope.CreateChild<TScope>()`（VContainer 内部 `new GameObject` + `AddComponent` + 自动设 `parentReference.Object = this`），不要手动 `new GameObject`。** 这样父 scope 关联由 VContainer 官方 API 完成，避免手写 `Parent` 赋值出错。`TScope` 是本层自己的 `GeneralLifetimeScope`，在 General 程序集内可解析，不违反 Core 不引用 General 的边界。

### 4.3 General -> Project

程序集限定入口：

```text
Project.Bootstrap.ProjectStartup, Project
```

入口签名：

```csharp
public static void Start(LifetimeScope parentScope)
```

General 不编译期引用 Project。

## 5. 生命周期顺序

> **VContainer 生命周期机制（先读）：** VContainer 不区分"入口类"和"普通类"——任何类只要注册为 `Register<T>().AsImplementedInterfaces()`（或 `RegisterEntryPoint<T>()`）且实现了 `IStartable` / `IPostStartable` / `ITickable` / `IDisposable` 等接口，容器构建后即按接口自动驱动。`RegisterEntryPoint<T>()` 与普通注册的唯一实际区别是**额外注册 `EntryPointDispatcher` + 默认异常处理器（`Debug.LogException`）**；普通 `AsImplementedInterfaces()` 由 `LifetimeScope` 兜底隐式触发同一个 dispatcher。
>
> **注册决策：**
> - **Layer 入口**（`CoreLayerEntrypoint` / `GeneralLayerEntrypoint` / `ProjectLayerEntrypoint`）→ **`RegisterEntryPoint<T>()`**：语义自文档化（"这是入口不是服务"），且生命周期方法（`PostStart` 反射调下一层）异常默认进 `Debug.LogException`，不静默。与现有 `SystemManager`（`RegisterEntryPoint<SystemManager>()`）风格一致。
> - **普通服务**（`ModelLifecycle`、`AssetSystem` 等）→ `Register<T>().AsImplementedInterfaces()`：是服务不是入口，无需显式 dispatcher。

### 5.1 Core

```text
CoreLifetimeScope.Configure（注册顺序）
  -> 1. LoggerFactory / SimpleLogger / GameLogBridge
  -> 2. RegisterMessagePipe()                       ← 全链唯一一次（§0.1）
  -> 3. MessagePipeOptions 注册为可解析实例
  -> 4. Core 事件 broker（RegisterCoreTypes）
  -> 5. IAssetRuntime / IAssetSystem 实例
  -> 6. [CoreSystem] 扫描注册
  -> 7. SystemManager（实现 IStartable/ITickable/IDisposable，RegisterEntryPoint）
  -> 8. CoreLayerEntrypoint（实现 IPostStartable/IDisposable，RegisterEntryPoint）

VContainer 构建后按接口驱动
  -> SystemManager.Start()（IStartable）
      -> ISystem.Init() by Priority
  -> CoreLayerEntrypoint.PostStart()（IPostStartable）
      -> inspect ICoreStartupStatus
      -> only on full success start General
```

`IPostStartable` 用作层间交接，避免依赖多个 `IStartable` 的隐式注册顺序。

### 5.2 General

```text
GeneralLifetimeScope.Configure（注册顺序）
  -> 1. 解析 MessagePipeOptions（从 Core 容器，见 §0.1）
  -> 2. General 事件 broker（只扫 General 程序集）
  -> 3. ModelScanner 扫 General -> Type[] scoped 注册（§0.2）
  -> 4. ModelLifecycle（实现 IPostStartable/IDisposable/IModelStartupStatus，AsImplementedInterfaces）
  -> 5. GeneralLayerEntrypoint（实现 IPostStartable/IDisposable，RegisterEntryPoint）

VContainer 构建后按接口驱动（IPostStartable 按注册顺序）
  -> ModelLifecycle.PostStart() -> LoadAll()          ← General 模型先 Load
  -> GeneralLayerEntrypoint.PostStart()
      -> confirm IModelStartupStatus（以 ModelLifecycle 状态为准，见 §0.3）
      -> start Project
```

General 层没有系统，启动内容只有模型加载，因此其启动状态以 `ModelLifecycle` 暴露的 `IModelStartupStatus` 为准，不再单独引入 `IGeneralStartupStatus` 接口。Model 加载异常应汇总并阻断 Project，而不是逐个吞掉后仍把 General 标记为成功（§0.3）。

### 5.3 Project

Project 创建自己的 child scope，只执行 Project 注册。可新增：

```csharp
ProjectLayerEntrypoint : IStartable, IDisposable
// 注册：builder.RegisterEntryPoint<ProjectLayerEntrypoint>()
```

它负责项目专属开始/结束逻辑。Project 模型按 §0.2 走独立的 ModelLifecycle（只扫 Project 程序集 + scoped Type[] 契约）。不得让 General 的单个 `ModelLifecycle` 同时拥有 Project 模型。

### 5.4 关闭顺序

销毁 Core root scope 时由 VContainer 按嵌套关系释放：

```text
Project scope Dispose
  -> Project models/services dispose
General scope Dispose
  -> General models unload
Core scope Dispose
  -> SystemManager.ShutdownAll() reverse Priority
  -> AssetSystem.Shutdown()
  -> logger/runtime session dispose
```

## 6. 文件变更清单

### 新增

- `Assets/Scripts/Core/Bootstrap/CoreStartup.cs`
- `Assets/Scripts/Core/Bootstrap/CoreLifetimeScope.cs`
- `Assets/Scripts/Core/Bootstrap/CoreLayerEntrypoint.cs`
- `Assets/Scripts/General/Bootstrap/GeneralStartup.cs`
- `Assets/Scripts/General/Bootstrap/GeneralLifetimeScope.cs`
- `Assets/Scripts/General/Bootstrap/GeneralLayerEntrypoint.cs`
- `Assets/Scripts/General/Models/IModelStartupStatus.cs`
- `Assets/Scripts/General/Models/ModelScanner.cs`（程序集过滤扫描 `[Model]` → `Type[]`）
- `Assets/Scripts/Project/Bootstrap/ProjectLayerEntrypoint.cs`
- 必要的分层启动契约测试。

### 修改

- `Assets/Scripts/Boot/BootUpdateRunner.cs`：正式反射入口从 Project 改为 Core；改善异步异常传播。
- `Assets/Scripts/Boot/Launcher/Data/BootStartupSettings.cs`：默认启动类型改为 Core。
- `Assets/GameRes/Scene/Boot/Main.unity`：序列化 `startupTypeName` 改为 Core。
- `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs`：写入 Entry 时使用 Core 启动入口。
- `Assets/Scripts/Boot.Editor/Build/Config/BuildProfile.cs` 与 `.asset`：默认启动入口改为 Core。
- `Assets/Scripts/General/Models/ModelLifecycle.cs`：改为注入 `IReadOnlyList<Type>` + `IObjectResolver`；暴露 `IModelStartupStatus`；失败不算成功（§0.2/§0.3）。
- `Assets/Scripts/General/Bootstrap/GeneralContainerRegistration.cs`：拆分 `RegisterBusinessLayer` 为 `RegisterBusinessEvents` / `RegisterModels` / `RegisterModelLifecycle`，供各层按需调用。
- `Assets/Scripts/Project/Bootstrap/ProjectStartup.cs`：改为创建 General 的 child scope；解析 `MessagePipeOptions` 存入 context。
- `Assets/Scripts/Project/Bootstrap/ProjectLifetimeScope.cs`：删除 Core/General 注册，只配置 Project。
- `.planning/STATE.md`、`.planning/ROADMAP.md`、`.planning/HOT_UPDATE_BOUNDARY.md`、`CODEMAP.md`：同步新启动链。

### 删除或收敛

- 删除 `ProjectLifetimeScope.PendingBootAssetRuntime`。
- 删除 Project scope 内对 `CoreBootstrapStage`、`GeneralBootstrapStage` 的调用。
- `CoreStartupContext` 不应跨层充当通用可变注册袋；改为各层明确的 context（如 `GeneralStartupContext` 携带 `MessagePipeOptions` / `ParentScope`），或直接传稳定依赖。

## 7. 实施步骤

> 重排说明：原计划 Phase 1 直接拆 Core root。修订后先做 Phase 0 基础设施重构——ModelLifecycle 与 MessagePipe 是跨层设计的地基，不改完，拆层必踩双加载/双消息域。

### Phase 0：前置基础设施重构（单 scope 下回归验证）

1. 新增 `ModelScanner.ScanModelTypes(Assembly)`（程序集过滤扫描 `[Model]` → `Type[]`）。
2. 重构 `ModelLifecycle`：注入 `IReadOnlyList<Type>` + `IObjectResolver`；实现 `IModelStartupStatus`；失败不设 `_loaded = true`。
3. 拆分 `GeneralContainerRegistration.RegisterBusinessLayer` 为 `RegisterBusinessEvents` / `RegisterModels` / `RegisterModelLifecycle`。
4. 确认 `RegisterMessagePipe()` 只在 Core 注册路径调用一次，`MessagePipeOptions` 可解析。
5. **单 scope 下跑 EditMode + Editor Play，验证重构后行为与现状完全一致（回归门）**。

### Phase 1：建立 Core Root

1. 新增 `CoreStartup` 和 `CoreLifetimeScope`。
2. Boot 默认入口改为 `CoreStartup.Start(IAssetRuntime)`。
3. Core scope 按 §5.1 顺序注册（先 MessagePipe 基础设施 + options 可解析，再事件/资源/系统/入口）。
4. Core root scope 创建方式：`new GameObject("CoreLifetimeScope")` + `AddComponent`（同现有 `ProjectStartup`），或官方 `LifetimeScope.Create()`。
5. 新增 Core post-start 入口，Core 失败时明确阻断。
6. 保持 General/Project 暂未启动，先验证 Boot -> Core。

### Phase 2：General Child Scope

1. 新增 `GeneralStartup` 和 `GeneralLifetimeScope`（含 `GeneralStartupContext` 解析 `MessagePipeOptions`）。
2. Core post-start 反射启动 General；`GeneralStartup` 内部用 `parentScope.CreateChild<GeneralLifetimeScope>()` 创建本层 scope（§4.2，不手动 new）。
3. General scope 用 `ModelScanner` 只扫 General 程序集（§0.2）。
4. General 事件 broker 只注册 General 事件，不调用 `RegisterMessagePipe()`（§0.1）。
5. 验证 Core 事件 General 能收到；验证 Core 完成事件早于所有 General Model Load。

### Phase 3：Project Child Scope

1. `ProjectStartup` 改为接收 General parent scope。
2. `ProjectLifetimeScope` 只配置 Project；用 `parentScope.CreateChild<ProjectLifetimeScope>()` 创建（§4.3）。
3. Project 使用独立 ModelLifecycle（只扫 Project 程序集）。
4. 验证 General 全部完成后才启动 Project；验证 General 事件 Project 能收到。

### Phase 4：异常与释放

1. Boot 不再通过 `Forget()` 丢失热更层异常；建立可观察的启动任务或统一失败回调。
2. 反射调用解包 `TargetInvocationException`，日志记录真实内部异常。
3. 任一层失败时显示 Boot repair/failure UI 或统一 fatal startup UI。
4. 验证 child scope 创建失败后的清理。
5. 验证退出时 Project -> General -> Core 逆序释放。

### Phase 5：文档与构建工具

1. 更新所有写入 `startupTypeName` 的 Editor/Build 工具。
2. 更新 HYB-03 反射契约测试。
3. 更新启动链文档和 runtime smoke 里程碑。
4. 运行 asmdef 边界校验和 EditMode 测试。

## 8. 测试计划

### EditMode

- Boot 默认入口精确为 `Core.Bootstrap.CoreStartup, Core`。
- `CoreStartup.Start(IAssetRuntime)` 反射契约存在。
- `GeneralStartup.Start(LifetimeScope)` 反射契约存在。
- `ProjectStartup.Start(LifetimeScope)` 反射契约存在。
- Launcher asmdef 引用不变。
- Boot asmdef 不引用 Core/General/Project/VContainer。
- Core asmdef 不引用 General/Project。
- General asmdef 不引用 Project。
- **Core system 失败时 General 不创建。**
- **General model 失败时 Project 不创建。**
- **每层只扫描和注册本层类型。**
- **双加载回归：Project 的 ModelLifecycle 不包含 General 模型（§0.2 契约隔离）。**
- **跨层事件：General 能订阅 Core 事件；Project 能订阅 General 事件（§0.1 单消息域）。**
- 重复调用 Start 不创建重复 scope。
- Dispose 顺序为 Project -> General -> Core。

### Editor Play

日志里程碑必须严格有序：

```text
Boot update completed
Core scope created
Core systems initialized
General scope created
General models loaded
Project scope created
Project startup completed
```

### Player / Android

- Offline 完整启动链。
- Host 无更新完整启动链。
- Host 下载新 Core/General/Project DLL 后冷启动完整链。
- CDN 不可达时停在 Boot，不创建 Core。
- Core 初始化故障时不创建 General。
- General 初始化故障时不创建 Project。
- ZLogger AOT 修复后的全链验证。

## 9. 验收标准

1. Boot 的正式入口为 Core，不再直接调用 Project。
2. Core、General、Project 各自拥有独立 `LifetimeScope`。
3. 每层都拥有实现 VContainer 生命周期接口的入口对象。
4. 下一层只在上一层明确成功后创建。
5. 每层只注册、启动和释放本层对象。
6. `IAssetRuntime` 只从 Boot 交接给 Core 一次。
7. 依赖方向和现有 asmdef 红线全部通过。
8. 启动异常可观察，不因 `Forget()` 静默丢失。
9. 关闭顺序为 Project -> General -> Core。
10. **全链只有一个 MessagePipe 消息域，Core 事件 General/Project 可订阅（§0.1）。**
11. **每层 ModelLifecycle 只管理本层模型，无重复 Load（§0.2）。**
12. EditMode、Editor Play、Android smoke 均有可验证的有序日志。

## 10. 风险与决策记录

### 嵌套 Scope 而非四个平级 Root

采用嵌套 child scope。平级 Root 无法自然继承下层服务，容易重新引入静态 Service Locator 或重复注册。

### 层间反射保留

反射是为了维持强制单向 asmdef，不是一般业务调用方式。反射仅发生一次且有固定契约测试，成本可忽略。

### 注册完成不等于启动完成

`Configure()` 只表示容器注册完成。层成功必须由本层启动状态明确表达，不能以 scope 已创建或入口方法已返回替代。

### 单消息域红线

子层任何一次 `RegisterMessagePipe()` 都会创建独立消息域，导致跨层事件全部断开。该红线由契约测试覆盖（§8）。

### 模型归属隔离

程序集过滤只解决注册源；`IEnumerable<IModel>` 聚合注入会跨 scope 混合。必须用 scoped `IReadOnlyList<Type>` 契约 + `IObjectResolver` 惰性解析（§0.2）。双加载回归测试是 Phase 2/3 的必过门槛。

### ModelLifecycle 失败语义必须收紧

当前 Model 加载逐个捕获异常后仍设置 `_loaded = true`。分层启动后必须记录失败并阻断 Project，否则"上一层成功后启动下一层"的承诺不成立。

### Boot 异常所有权

当前 `BootUpdateRunner.Start()` 内部 `RunAsync().Forget()` 会切断 `Entry` 的异常捕获链。重构应同步建立统一启动失败通道，避免 Core/General/Project 反射或 scope 构建失败只进入未观察异常处理器。
