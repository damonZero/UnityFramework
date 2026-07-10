---
name: kj-pool
description: >
  KJ Framework 对象池系统完整指南。涵盖 ObjectPool<T>（泛型对象池，lock 并发安全 + 重复归还防护）、SingleThreadObjectPool<T>（内部单线程轻量池，CollectionPool 热路径使用）、CollectionPool（List/HashSet/Queue/Stack/Dictionary 集合池）、PooledCollections（RAII struct 自动归还，[NonCopyable] 标记）、TypePool（类型池注册表）、GameObjectPool（Unity GameObject 池，PrefabPoolState 内聚状态，BoundedStore+LruPolicy Prefab 缓存，IInstanceRecyclePolicy 实例库策略，反向索引污染检测，主线程断言）、PoolLease<T>（using 模式）、PoolDependencies（静态委托注入桥接资源加载）、InstanceRecyclePolicy（CapacityInstancePolicy/PersistentInstancePolicy）。
  触发场景：创建/使用对象池、性能优化减少 GC 分配、管理 GameObject 频繁创建销毁、使用 using 模式自动归还、配置池容量和预热、常驻路径保护、实例回收策略定制。
  核心规则：Framework.Pool 不引用 Scripts；通过 PoolDependencies 静态委托桥接外部依赖；ObjectPool<T> 是通用线程安全池；CollectionPool 使用 SingleThreadObjectPool<T>，面向主线程/帧内热路径；GameObjectPool 仅主线程调用，异步加载只做同路径 single-flight，不表示多线程 Unity 对象池；PooledX struct 禁止值拷贝。
metadata:
  doc: CODEMAP.md
  layer: Framework
---

# KJ 对象池系统 (Framework.Pool)

完整技术细节见 `CODEMAP.md` 的 Framework: Pool 章节，关键源码在 `Assets/Framework/Pool/`。

## 架构速查

```
IPool<T>  ←──  ObjectPool<T>  ←──  PoolLease<T> (struct, IDisposable)
                                                ↑
SingleThreadObjectPool<T> (internal, no lock, assertion guards)
        ↑
CollectionPool ──→ PooledList<T> / PooledHashSet<T> / ...  (RAII struct wrappers, [NonCopyable])

TypePool  ←──  ConcurrentDictionary<Type, object>  (类型→池注册表)

GameObjectPool  ──→ PrefabPoolState (Idle/Instances/ActiveCount/IdleCount/IsPersistent/IsPrefabCached)
                 ├── BoundedStore<string, GameObject> + LruPolicy  (Prefab 引用缓存)
                 ├── IInstanceRecyclePolicy (CapacityInstancePolicy / PersistentInstancePolicy)
                 ├── _instanceToPath (反向索引: O(1) 污染检测 + 防双回收)
                 └── AssertMainThread() (运行时主线程断言)
```

## 各组件使用指南

### ObjectPool<T> — 泛型对象池

```csharp
// 创建：factory 必传，reset/maxIdle/preload 可选
var pool = new ObjectPool<MyClass>(
    factory: () => new MyClass(),
    reset: obj => obj.Clear(),
    maxIdle: 64,
    preload: 8        // 预热：创建时直接填充 idle 栈
);

// Rent — 无空闲时自动 Create
var item = pool.Rent();

// Return — 调用 reset，超 maxIdle 时丢弃
pool.Return(item);

// RentLease — using 模式自动归还（推荐）
using (var lease = pool.RentLease())
{
    var value = lease.Value;
    // ... 用完自动 Return
}

// 诊断
var stats = pool.GetStatistics();  // IdleCount / CreatedCount / RentCount / ReturnCount / MaxIdle
```

**性能要点：**
- `Stack<T>` 做 LIFO 空闲缓存（CPU cache 友好）
- `lock(_gate)` 保护所有 mutation 操作
- `_idleSet` 防止同一对象重复归还，避免 PoolLease / 调用方误用导致同实例多次入栈
- 适合跨线程或后台逻辑使用；不要把它作为 CollectionPool 热路径的默认实现

### CollectionPool — 集合池

```csharp
// 推荐用法：using 模式，自动归还（返回的是 PooledX struct，禁止值拷贝）
using var list = CollectionPool.RentList<int>();
list.Value.Add(42);

using var dict = CollectionPool.RentDictionary<string, int>();
dict.Value["key"] = 100;

// 可用类型：List<T> / HashSet<T> / Queue<T> / Stack<T> / Dictionary<TKey, TValue>
// ⚠️ PooledX 是 mutable struct，禁止 `var b = a;` 值拷贝——会导致同一集合双归还损坏共享池
```

**性能要点：**
- 每种集合类型使用内部 static `SingleThreadObjectPool<T>`，capacity=32
- `SingleThreadObjectPool<T>` 不加 lock，面向 Unity 主线程/帧内临时集合热路径
- `UNITY_ASSERTIONS` 下会校验创建线程与重复归还；正式包不保留 `_idleSet`，避免额外开销
- reset 动作为 `collection.Clear()`，不释放内部 capacity（避免下次重新分配）
- `PooledCollections` 是 struct wrapper，零 GC 分配
- 不要从后台线程使用 `CollectionPool`；后台多线程临时对象应使用 `ObjectPool<T>` 或局部 new

### TypePool — 类型池注册表

```csharp
// 注册
TypePool.Register<MyClass>(() => new MyClass(), reset: obj => obj.Clear(), maxIdle: 32);

// 查询
if (TypePool.TryGet<MyClass>(out var pool))
    var item = pool.Rent();

// 懒创建（T 必须有 new() 约束）
var pool = TypePool.GetOrCreate<MyClass>(maxIdle: 16);
```

### GameObjectPool — Unity 对象池

```csharp
// 由 PoolService 注入依赖后创建
var pool = new GameObjectPool(
    root: poolRoot,
    prefabCapacity: 64,           // Prefab 引用 LRU 缓存容量
    mode: PoolContainerMode.ChangeParent,
    maxIdlePerPrefab: 64,         // 每个 prefab 的最大 idle 实例数
    recyclePolicy: null           // 默认 CapacityInstancePolicy(maxIdlePerPrefab)
);

// 异步获取（自动加载 Prefab + SemaphoreSlim 并发保护 + 二次 TryGet）
var instance = await pool.GetAsync("Assets/Prefabs/Bullet.prefab", parent);

// 回收（超 maxIdle 时 Destroy 而非入栈）
pool.Recycle(instance);

// 预热（fire-and-forget）
pool.Warmup("Assets/Prefabs/Bullet.prefab", count: 10);

// 常驻保护 — 标记某 prefab 为常驻，容量淘汰永不回收其实例
pool.MarkPersistent("Assets/Prefabs/Player.prefab");

// 诊断
int idle = pool.GetIdleCount(prefabPath);
int active = pool.GetActiveCount(prefabPath);

// 清理
pool.Clear();
```

**核心设计要点：**
- **五字典合并**：`PrefabPoolState` 内聚 per-prefab 的 Idle/Instances/ActiveCount/IdleCount/IsPersistent/IsPrefabCached
- **反向索引**：`_instanceToPath`（Dictionary<GameObject, string>）提供 O(1) 污染检测 + 防双回收，借鉴 ETPro instPathCache 精神
- **实例库策略化**：`IInstanceRecyclePolicy` 决定 Recycle 时保留还是 Destroy；默认 `CapacityInstancePolicy(maxIdlePerPrefab)`，可注入 `PersistentInstancePolicy` 保护常驻路径
- **Prefab 缓存**：`BoundedStore<string, GameObject>` + `LruPolicy`，替换旧 `Cache` 硬编码 LRU
- **主线程断言**：`GameObjectPool` 构造时记录 `_mainThreadId`，所有公开方法入口 `AssertMainThread()` 抛 `InvalidOperationException`
- **污染检测**：回收的实例必须有 `PoolInstanceTag` 组件（自动添加），`IsRecycled` 防重复回收，反向索引校验 `PrefabPath` 防跨路径混淆
- **线程模型**：Unity 对象操作主线程 only；`LoadGates` 只防止同一路径异步重复加载，不代表 `GameObjectPool` 支持多线程调用

**容器模式：**
- `ChangeParent` — 回收时移回 root（层级整洁，但有 Transform 变更开销）
- `MovePos` — 回收时移到远处（无层级变更，但不整洁）

**生命周期范围：**
- **全局对象池**：生命期等同于游戏进程（如由 `PoolService` 单例托管的池），其挂载的 `root` 节点通常设置为 `DontDestroyOnLoad`
- **局部/功能对象池**：跟随特定的 UI 窗口、场景或预制体存在，其 `root` 不应设置 `DontDestroyOnLoad`
- **防止内存泄漏**：局部对象池在其持有者卸载时，**必须显式调用 `pool.Clear()`**

### IInstanceRecyclePolicy — 实例回收策略

```csharp
// 默认：容量策略，maxIdlePerPrefab 控制
var policy = new CapacityInstancePolicy(maxIdle: 20);

// 常驻装饰器：部分路径永远保留
var persistentSet = new HashSet<string> { "Prefabs/Player", "Prefabs/UI_Button" };
var policy = new PersistentInstancePolicy(persistentSet, new CapacityInstancePolicy(10));
// 命中 persistentSet → 永远保留；否则 → 按 CapacityInstancePolicy 决策
```

### PoolDependencies — 静态委托桥接

```csharp
// PoolService 在 Init() 时注入（Core 层），Framework 不直接引用 Scripts
PoolDependencies.LoadAssetAsync = (path, parent) => _assetSystem.LoadAssetAsync<GameObject>(path);
PoolDependencies.ReleaseAssetByPath = path => _assetSystem.Release<GameObject>(path);

// LoadGates: ConcurrentDictionary<string, SemaphoreSlim> — 防止同一 Prefab 并发加载
```

## 最佳实践

1. **集合池优先**: 任何临时集合都用 `using var list = CollectionPool.RentList<T>()`，而不是 `new List<T>()`
2. **PooledX 禁止值拷贝**: `PooledList<T>` 等是 mutable struct，写 `var b = a;` 后两个都 Dispose 会把同一集合两次归还进共享池 → 池损坏
3. **CollectionPool 仅主线程热路径使用**: 它为性能使用单线程池，后台线程不要调用
4. **配置 maxIdle**: 根据内存预算设置合理的 maxIdle，避免无限堆积
5. **预热关键路径**: 对频繁创建的类型在初始化时 preload
6. **GameObjectPool 仅主线程调用**: 有运行时断言保护，子线程调用会抛 InvalidOperationException
7. **局部池必须 Clear**: 跟随 UI/场景的局部池销毁前调 `pool.Clear()` 释放实例和 Prefab 引用
8. **常驻路径用 MarkPersistent**: 对不会卸载的常用 Prefab（如主角、UI 通用控件）标记常驻，避免被容量淘汰

## 依赖图

```
Framework.Pool (Pool.asmdef)
  引用: UniTask, Cache
  不引用: 任何 Scripts/ 代码

Scripts/Core/ (Core.asmdef)
  引用: Pool, Cache
  PoolService.cs 负责桥接
```
