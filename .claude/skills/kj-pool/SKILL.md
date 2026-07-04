---
name: kj-pool
description: >
  KJ Framework 对象池系统完整指南。涵盖 ObjectPool<T>（泛型对象池，lock 并发安全）、CollectionPool（List/HashSet/Queue/Stack/Dictionary 集合池）、PooledCollections（RAII struct 自动归还）、TypePool（类型池注册表）、GameObjectPool（Unity GameObject 池，LIFO+LRU 双层缓存，污染检测）、PoolLease<T>（using 模式）、PoolDependencies（静态委托注入桥接资源加载）。
  触发场景：创建/使用对象池、性能优化减少 GC 分配、管理 GameObject 频繁创建销毁、使用 using 模式自动归还、配置池容量和预热。
  核心规则：Framework.Pool 不引用 Scripts；通过 PoolDependencies 静态委托桥接外部依赖；集合池通过 CollectionPool.Rent*() + using 使用；GameObject 池依赖 PoolInstanceTag 做污染检测。
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
CollectionPool ──→ PooledList<T> / PooledHashSet<T> / ...  (RAII struct wrappers)

TypePool  ←──  ConcurrentDictionary<Type, object>  (类型→池注册表)

GameObjectPool  ──→ PoolInstanceTag  (MonoBehaviour, 污染检测)
                  ──→ Cache<string, GameObject>  (Prefab 缓存, LruCachePolicy)
                  ──→ PoolDependencies.LoadAssetAsync / ReleaseAssetByPath (静态委托)
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
- `Return` 先调 `reset`（锁外），再入栈（锁内）— 减少锁持有时间

### CollectionPool — 集合池

```csharp
// 推荐用法：using 模式，自动归还
using var list = CollectionPool.RentList<int>();
list.Value.Add(42);

using var dict = CollectionPool.RentDictionary<string, int>();
dict.Value["key"] = 100;

// 可用类型：List<T> / HashSet<T> / Queue<T> / Stack<T> / Dictionary<TKey, TValue>
```

**性能要点：**
- 每种集合类型 5 个内部 static `ObjectPool`，capacity=32
- reset 动作为 `collection.Clear()`，不释放内部 capacity（避免下次重新分配）
- `CollectionPool` 和 `PooledCollections` 都是 struct wrapper，零 GC 分配

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
// 由 PoolService 注入依赖后创建（不能手动 new，需要先配置 PoolDependencies）
// PoolService.Init() 中：
var pool = new GameObjectPool(root: poolRoot, prefabCapacity: 64,
    mode: PoolContainerMode.ChangeParent);  // 或 MovePos

// 异步获取（自动加载 Prefab + SemaphoreSlim 并发保护）
var instance = await pool.GetAsync("Assets/Prefabs/Bullet.prefab", parent);

// 回收
pool.Recycle(instance);

// 预热
pool.Warmup("Assets/Prefabs/Bullet.prefab", count: 10);

// 诊断
int idle = pool.GetIdleCount(prefabPath);
int active = pool.GetActiveCount(prefabPath);

// 清理
pool.Clear();
```

**污染检测：** 回收的实例必须有 `PoolInstanceTag` 组件（自动添加），`IsRecycled` 标志防止重复回收，`PrefabPath` 校验防止跨路径混淆。

**容器模式：**
- `ChangeParent` — 回收时移回 root（层级整洁，但有 Transform 变更开销）
- `MovePos` — 回收时移到远处（无层级变更，但不整洁）

### PoolDependencies — 静态委托桥接

```csharp
// PoolService 在 Init() 时注入（Core 层），Framework 不直接引用 Scripts
PoolDependencies.LoadAssetAsync = (path, parent) => _assetSystem.LoadAssetAsync<GameObject>(path);
PoolDependencies.ReleaseAssetByPath = path => _assetSystem.Release<GameObject>(path);

// LoadGates: ConcurrentDictionary<string, SemaphoreSlim> — 防止同一 Prefab 并发加载
```

## 最佳实践

1. **集合池优先**: 任何临时集合都用 `using var list = CollectionPool.RentList<T>()`，而不是 `new List<T>()`
2. **使用 IPoolable 接口**: 让池化对象实现 `IPoolable`，在 `ResetState()` 中清理所有可变状态
3. **配置 maxIdle**: 根据内存预算设置合理的 maxIdle，避免无限堆积
4. **预热关键路径**: 对频繁创建的类型在初始化时 preload
5. **GameObjectPool 用 SemaphoreSlim**: Prefab 加载有并发保护，不用担心重复加载

## 依赖图

```
Framework.Pool (Pool.asmdef)
  引用: UniTask, Cache
  不引用: 任何 Scripts/ 代码
  
Scripts/Core/ (Core.asmdef)
  引用: Pool, Cache
  PoolService.cs 负责桥接
```
