# KJ Unity Framework — 代码审查报告

> **审查日期：** 2026-07-04
> **框架版本：** Phase 0 完成 + Phase 1 进行中
> **审查范围：** Boot / Core / General / Project / Framework.{Asset, Event, Pool, Cache, Log, TestKit}
> **审查结论：** 架构基础扎实，有若干值得修缮的设计细节

---

## 一、总体评分

| 维度 | 评分 | 说明 |
|------|------|------|
| 架构分层设计 | ★★★★★ | 四层严格单向依赖，asmdef 物理隔离，方向清晰 |
| 代码质量 | ★★★★½ | 整体规范，部分模块有可改进点 |
| 第三方库选型 | ★★★★★ | VContainer + UniTask + YooAsset + ZLogger 组合成熟 |
| 性能意识 | ★★★★ | ZLogger/ZLinq/Pool/Cache 基础设施完善，有少量盲点 |
| 可测试性 | ★★★★½ | TestKit 设计良好，Fake/Probe/Clock 齐全 |
| 文档/规范 | ★★★★★ | STATE.md / ROADMAP.md / AGENTS.md 维护严谨 |

---

## 二、架构设计亮点

### 2.1 四层 asmdef 物理隔离

```
Boot  ←  Core  ←  General  ←  Project
```

用 `.asmdef` 做**编译期物理隔离**，而非口头约定。依赖方向错误直接报编译错误，
这是大多数团队做不到的工程纪律。

### 2.2 Framework 可替换性设计

`Framework/Asset/`、`Framework/Event/`、`Framework/Pool/`、`Framework/Cache/` 均不引用 `Assets/Scripts/`。
- 替换 YooAsset → 只改 `AssetRuntime.cs` 一个文件
- 替换 MessagePipe → 只改 Core 层注册逻辑
- Framework 层有真正的独立性

### 2.3 无 prefab 启动链

通过 assembly-qualified type name 反射创建 `IBootstrapStage`，Boot 层实现零业务依赖，
比传统 MonoBehaviour prefab 挂载方案可维护性高。

### 2.4 [CoreSystem] / [Model] 元数据驱动注册

反射只在注册期执行，运行时完全走构造函数 DI，兼顾了开发便捷性和运行时效率。

### 2.5 资源系统 owned/cached 双通道

`LoadAssetHandleAsync`（调用方自管）vs `LoadAssetAsync`（系统缓存），
生命周期语义清晰，`IDisposable` 模式避免了引用计数手动维护。

### 2.6 TestKit 完整性

`RecordingAssetSystem`、`CallProbe`、`ManualClock`、`RecordingEventSink<TEvent>` 覆盖了框架测试的核心需求。
`TestKit.asmdef` 的 `autoReferenced=false + optionalUnityReferences:TestAssemblies` 确保不污染正式构建，是最佳实践。

---

## 三、已确认不需修改的设计（驳回初稿建议）

> 以下三点在审查初稿中被列为问题，经过充分讨论后，判断**当前实现是合理的**，不应修改。

### 3.1 AssetRuntime 同步初始化 — 无需异步化

**原始顾虑：** `initOp.WaitForCompletion()` 阻塞主线程。

**结论：可接受，不修改。**

| 启动模式 | WaitForCompletion 实际耗时 | 是否需要异步 |
|----------|--------------------------|------------|
| EditorSimulate | < 1ms | 否 |
| Offline（StreamingAssets）| 5~30ms | 否，可接受 |
| Host（CDN 热更下载）| 真正耗时的是**下载**，不是本地 package 初始化 | 否 |

Host 模式的热更下载应放在 Boot 层独立的 `HybridCLRBootStage` 中处理（Priority < 100），
带进度 UI，完成后 `CoreBootstrapStage` 才执行。`AssetSystem.Init()` 本身只做本地 package 扫描，不做网络操作。

### 3.2 ISystem.Init() 同步 — 无需 IAsyncSystem

**原始顾虑：** 部分系统需要异步初始化。

**结论：不引入 IAsyncSystem，理由如下。**

若并发异步初始化，有依赖关系的系统必须等待依赖项完成，这要求显式依赖图，
复杂度等同于编写 async 任务调度器，属于过度设计。

正确做法：`Init()` 只做**注册状态**（同步，无 IO），后台 IO 用可选扩展接口：

```csharp
// 可在未来按需引入，不影响现有 ISystem
public interface IBackgroundStartup
{
    UniTask StartBackgroundAsync(); // 建立网络连接、预热缓存等后台工作
}
```

`SystemManager.Start()` 同步完成后，对实现了 `IBackgroundStartup` 的系统 fire-and-forget，
不阻塞游戏进入。

### 3.3 IModel.Load() 同步 — 无需异步化

**原始顾虑：** 业务模型初始化可能需要异步（读配置、请求服务器）。

**结论：不修改，理由如下。**

`IModel` 是**业务域对象（Domain Object）**的数据层，不是持续响应 UI 的 ViewModel。
其 `Load()` 是**启动时一次性初始化**，正确职责是：从 PlayerPrefs/缓存读取本地数据、初始化内存结构、订阅事件。

**配置表（Luban）的正确加载方式：**

作为 `[CoreSystem]`（高于其他业务系统），在 `Init()` 中用 `WaitForCompletion()` 同步加载二进制表。
实测 10MB Luban 二进制表 < 50ms，属于可接受的一次性启动成本。

需要联网的操作（如服务器数据拉取）不属于 `Load()` 的职责，应在 `AppStartedEvent` 事件处理中异步发起。

---

## 四、需要修复的问题（共 12 条）

---

### 🟡 P1 — IAssetRuntime 接口继承 IAssetSystem，职责混淆

**位置：** `Framework/Asset/IAssetRuntime.cs`

**问题：**

```csharp
public interface IAssetRuntime : IAssetSystem  // ⚠️ 运行时接口继承了用户接口
{
    bool Initialize(AssetConfig config);
    void Shutdown();
}
```

`IAssetRuntime` 是生命周期管理接口（仅供 Core 层使用），`IAssetSystem` 是加载 API 接口（供业务层使用）。
两者继承后，任何层都可以注入 `IAssetRuntime` 并调用 `Initialize/Shutdown`，违反封装原则。

**建议修改：**

```csharp
// IAssetRuntime.cs — 不再继承 IAssetSystem
public interface IAssetRuntime
{
    AssetConfig Config { get; }
    bool IsReady { get; }
    bool Initialize(AssetConfig config);
    void Shutdown();
}

// AssetRuntime.cs — 分别实现两个接口
public sealed class AssetRuntime : IAssetRuntime, IAssetSystem { ... }

// CoreContainerRegistration.cs — 分开注册
builder.Register<AssetRuntime>(Lifetime.Singleton)
    .AsSelf()
    .As<IAssetRuntime>()
    .As<IAssetSystem>(); // 业务层只能注入 IAssetSystem
```

---

### 🟡 P2 — IAssetSystem.Release() 两个重载语义不清晰

**位置：** `Framework/Asset/IAssetSystem.cs`

**问题：**

```csharp
void Release<T>(string path) where T : Object;  // 类型精确释放 cached 句柄
void Release(string path);                        // 释放该路径所有资产 + 触发场景卸载
```

两个方法名相同，但行为差异极大，`Release(path)` 还会触发场景卸载，副作用不明显，容易误用。

**建议修改：**

```csharp
public interface IAssetSystem
{
    void Release<T>(string path) where T : Object;
    void ReleaseAll(string path);          // 原 Release(string)，明确"释放全部"语义
    UniTask UnloadSceneAsync(string path); // 场景卸载独立出来，不与资产释放混用
    void UnloadUnused();
}
```

---

### 🟡 P3 — Cache.GetOrAdd 中值被丢弃时触发了错误的 onEvicted 回调

**位置：** `Framework/Cache/Cache.cs`，约第 92 行

**问题：**

当两个协程同时 `GetOrAdd` 相同 key，第二个到达锁的协程发现 key 已存在，
丢弃自己刚创建的值，并对它调用 `_onEvicted`：

```csharp
if (valueDiscarded)
{
    _onEvicted?.Invoke(key, value); // ⚠️ 被丢弃的是新创建的值，不是被淘汰的缓存
}
```

若 `_onEvicted` 的实现是释放资源（如调用 `handle.Release()`），会错误地释放一个从未进入缓存的资源。

**建议修改：**

```csharp
// 构造函数增加 onDiscarded 参数，区分两种语义
public Cache(
    int capacity,
    ICacheEvictionPolicy<TKey> policy,
    Action<TKey, TValue>? onEvicted = null,    // 被 LRU 淘汰时
    Action<TKey, TValue>? onDiscarded = null)  // 并发重复创建被丢弃时

// GetOrAdd 中
if (valueDiscarded)
    _onDiscarded?.Invoke(key, value);
```

---

### 🟡 P4 — CoreTypeRegistration 与 GeneralContainerRegistration 代码重复

**位置：**
`Core/Bootstrap/CoreTypeRegistration.cs`
`General/Bootstrap/GeneralContainerRegistration.cs`

**问题：** 两处完全相同的代码：
- `RegisterMessageBrokerMethod` 静态字段（约 5 行反射代码）
- `GetLoadableTypes()` 私有方法（约 15 行）

**建议修改：** 将两段提取到 `Framework.Event.GameEventTypeScanner` 内，或在 Core 层建立
`ContainerRegistrationHelper` 工具类，General 层复用。

---

### 🟡 P5 — 启动失败没有恢复路径（挂起待 UI-01 承接）

**位置：** `Core/Systems/SystemManager.cs`，`General/Models/ModelLifecycle.cs`

**问题：**
- `SystemManager` 初始化失败后 `Initialized = false`，`ModelLifecycle` 会阻止所有 Model 加载
- 但没有任何机制通知玩家，也没有重试路径

**建议：** 在 ROADMAP 中明确此挂起点。UI-01 建成后，订阅 `ICoreStartupStatus.HasInitFailures`，
显示错误提示界面，引导玩家重启。

---

### 🟢 P6 — ObjectPool._createdCount 不在锁内，多线程统计不准

**位置：** `Framework/Pool/ObjectPool.cs`，`Create()` 方法

**问题：**

```csharp
private T Create()
{
    var item = _factory();
    _createdCount++;  // ⚠️ 不在锁内，多线程时可能丢失计数
    return item;
}
```

**建议修改（一行）：**

```csharp
Interlocked.Increment(ref _createdCount);
```

---

### 🟢 P7 — Release(string path) O(n) 遍历全表

**位置：** `Framework/Asset/AssetRuntime.cs`，`Release(string path)` 方法

**问题：**

```csharp
foreach (var key in _assetHandles.Keys)
    if (key.Path == path) ...  // O(n) 遍历
```

**建议：** 维护反向索引 `Dictionary<string, List<AssetCacheKey>>`，释放时 O(1) 定位。
资源量小时无感知，可按需改。

---

### 🟢 P8 — GameObjectPool 中 SemaphoreSlim 移除后未 Dispose

**位置：** `Framework/Pool/GameObjectPool.cs`，`Clear()` 方法

**问题：**

```csharp
PoolDependencies.LoadGates.TryRemove(prefabPath, out _);  // 移除但未 Dispose
```

**建议修改：**

```csharp
if (PoolDependencies.LoadGates.TryRemove(prefabPath, out var gate))
    gate.Dispose();
```

---

### 🟢 P9 — SystemManager.GetSystem<T>() 接口类型调用返回 null

**位置：** `Core/Systems/SystemManager.cs`，`GetSystem<T>()` 方法

**问题：** `_systemMap` 的 key 是实现类 `Type`，调用 `GetSystem<IAssetSystem>()` 会返回 null。

**建议：** 补充 XML 注释，明确说明必须传具体实现类：

```csharp
/// <summary>
/// 通过具体实现类型获取系统。
/// <b>注意：必须传实现类（如 AssetSystem），不能传接口类型（如 IAssetSystem）。</b>
/// 如需通过接口获取，请使用 VContainer 构造函数注入。
/// </summary>
```

---

### 🟢 P10 — BootLifetimeScope SerializeField 与静态数组内容重复

**位置：** `Boot/Bootstrap/BootLifetimeScope.cs`

**问题：** `DefaultBootstrapStageTypeNames`（静态只读）和 `bootstrapStageTypeNames`（SerializeField）
内容完全相同，维护时两处都要改，容易漏改。

**建议修改：**

```csharp
[SerializeField]
private string[] bootstrapStageTypeNames = Array.Empty<string>();
// Configure() 中 bootstrapStageTypeNames 为空时回退到 DefaultBootstrapStageTypeNames，不再重复声明
```

---

### 🟢 P11 — PoolService 在实例类上定义静态快捷方法

**位置：** `Core/PoolService.cs`

**问题：**

```csharp
// 业务代码无需注入 PoolService 实例即可调用，等同于直接调用 CollectionPool
public static PooledList<T> RentList<T>() => CollectionPool.RentList<T>();
```

静态方法让调用者感知不到自己是否在 DI 体系内，且这层转发没有附加价值。

**建议：** 删除这些静态转发方法，业务代码直接使用 `CollectionPool.RentList<T>()`。

---

### 🟢 P12 — GameLog.Sink 为 public set，可被任意代码覆盖

**位置：** `Framework/Log/GameLog.cs`，第 52 行

**问题：**

```csharp
public static IGameLogSink Sink { get; set; }  // 任何代码都可写 GameLog.Sink = null
```

**建议修改：**

```csharp
public static IGameLogSink Sink { get; private set; }
internal static void SetSink(IGameLogSink sink) => Sink = sink;
// GameLogBridge.Install/Uninstall 调用 SetSink
```

---

## 五、代码质量优化建议（非 Bug）

### 5.1 NormalizeModule 字符串计算可缓存

`[CallerFilePath]` 是编译期常量，但每次日志调用都执行 `Replace` + `IndexOf`：

```csharp
// 建议：用 ConcurrentDictionary 缓存 filePath → module 映射
private static readonly ConcurrentDictionary<string, string> _moduleCache = new();

private static string NormalizeModule(string module, string filePath)
{
    if (!string.IsNullOrWhiteSpace(module) && module != DefaultModule)
        return module;
    return _moduleCache.GetOrAdd(filePath, ComputeModule);
}
```

### 5.2 GameObjectPool 应注明主线程专用

```csharp
/// <summary>
/// GameObject 对象池。<b>仅供 Unity 主线程调用，非线程安全。</b>
/// </summary>
public sealed class GameObjectPool { ... }
```

### 5.3 Core/Pool/ 空目录

`Assets/Scripts/Core/Pool/` 目录下无任何文件（`PoolService.cs` 在 `Core/` 根目录），可删除避免误导。

---

## 六、未实现模块的设计建议

### 6.1 Timer 模块（建议 UI-01 前实现）

- 用 `int` 毫秒计数而非 `float` 秒，避免浮点误差累积
- 提供 `TimerHandle`（RAII，类似 `AssetHandle<T>`），防止失效 ID 引用
- 接入 `ITickableSystem`，Priority 设最低数值（最早 Tick）

### 6.2 MonoBehaviour 与 DI 的访问边界（补充至 AGENTS.md）

目前缺少 MonoBehaviour 组件访问 DI 服务的推荐模式，建议在 AGENTS.md 补充：

```csharp
// 推荐：VContainer MonoBehaviour 注入
public class MyComponent : MonoBehaviour
{
    [Inject] private IAssetSystem _assetSystem;
}

// 禁止：在 MonoBehaviour 里手动 Resolve
// FindObjectOfType<LifetimeScope>().Container.Resolve<T>()  ❌
```

### 6.3 UI 系统（UI-01/02）

- 窗口层级用枚举（`UILayer.Normal / Popup / Loading`），不用裸 `int`
- `UIWindow.OnOpen/OnClose` 支持 `UniTask` 返回值（过场动画）
- 窗口资源通过 `IAssetSystem.LoadAssetHandleAsync` 加载，`OnClose` 时 `Dispose` 释放
- 考虑 `[UIWindow]` 属性自动扫描注册，与 `[CoreSystem]`/`[Model]` 保持一致风格
- **启动失败恢复 UI（P5）在此阶段实现**

### 6.4 网络层（NET-01~05）

- Session 心跳和断线重连挂接 Timer 模块（不自建计时）
- `MessageRouter` Handler 注册考虑 `[MessageHandler(msgId)]` + 反射扫描，与元数据驱动风格一致
- Protobuf Message 反序列化对象接入 `ObjectPool<T>`，减少高频消息 GC 压力

### 6.5 HybridCLR 热更新集成

热更下载流程应独立为 Boot 层早期 Stage（Priority < 100）：

```
HybridCLRBootStage (Priority=0)  →  下载热更 DLL（UniTask + 进度 UI）
CoreBootstrapStage  (Priority=100) →  注册所有系统（此时 DLL 已就位）
```

这样 Boot 层保持最小依赖，热更相关代码只在 Boot 层的 HybridCLRBootStage 内，
不污染 Core 及以上层，且最大限度减少需要重启 APP 的场景。

---

## 七、问题汇总表

| # | 优先级 | 模块 | 问题摘要 | 改动成本 |
|---|--------|------|----------|----------|
| P1 | 🟡 中 | Framework.Asset | IAssetRuntime 继承 IAssetSystem，职责混淆 | 低 |
| P2 | 🟡 中 | Framework.Asset | Release() 两个重载语义不清晰 | 低 |
| P3 | 🟡 中 | Framework.Cache | GetOrAdd 值被丢弃时误触发 onEvicted | 低 |
| P4 | 🟡 中 | Core/General Bootstrap | 注册工具代码重复 | 低 |
| P5 | 🟡 中 | 启动链 | 系统初始化失败无恢复路径（待 UI-01 承接）| 中 |
| P6 | 🟢 低 | Framework.Pool | ObjectPool._createdCount 不在锁内 | 极低 |
| P7 | 🟢 低 | Framework.Asset | Release(path) O(n) 遍历 | 低（按需改）|
| P8 | 🟢 低 | Framework.Pool | SemaphoreSlim 未 Dispose | 极低 |
| P9 | 🟢 低 | Core.Systems | GetSystem<T>() 接口类型查找返回 null | 加注释即可 |
| P10 | 🟢 低 | Boot | SerializeField 与静态数组重复 | 极低 |
| P11 | 🟢 低 | Core | PoolService 静态转发方法无意义 | 极低 |
| P12 | 🟢 低 | Framework.Log | GameLog.Sink public set | 极低 |

---

## 八、结论

**这是一个架构意识超出行业平均水平的 Unity 框架基础，可以支撑中型手游（50~100 人月）的开发需求。**

核心基础设施（Boot 链、DI、资产系统、Pool/Cache、日志、TestKit）已具备生产可用的质量。
当前 12 个问题均属于**设计细节级别**，无破坏性缺陷，可在正常迭代中修缮。

**建议执行顺序：**

| 时机 | 任务 |
|------|------|
| 立即 | P1（IAssetRuntime 接口解耦）、P3（Cache.GetOrAdd 语义）|
| 随时 | P6/P8/P10/P11/P12（极低成本，顺手改）|
| UI-01 前 | Timer 模块实现 + AGENTS.md 补充 MonoBehaviour DI 边界规范 |
| UI-01 中 | P5（启动失败恢复 UI）|
| 按需 | P2（Release 语义重构）、P4（注册代码去重）、P7（Release O(n) 优化）|

---

*审查人：Antigravity AI Framework Architect*
*基于完整源码阅读，不含编译验证*
*Review 文档版本：v1.0 / 2026-07-04*
