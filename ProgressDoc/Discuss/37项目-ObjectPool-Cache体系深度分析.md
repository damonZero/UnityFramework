# 37项目 ObjectPool / Cache 体系深度分析

> 来源: `F:\int_37_pack\client\Assets\Framework\Package\ObjectPool\` + `Cache\`
> 关联桥接: `Core/CacheSystem/CacheSystem.cs`
> 分析日期: 2026-07-01

---

## 一、文件清单与整体架构

### 文件总数: 51 个

```
Framework/
├── Package/ObjectPool/                     (35 files)
│   ├── Interfaces/                         (8)   — 接口契约层
│   ├── ObjectPool/                         (4)   — 核心池化实现
│   ├── ObjectFactory/                      (5)   — 对象工厂
│   ├── ObjectPool/ObejctPool.cs            (1)   — 泛型对象池
│   ├── ObjectPoolFactory.cs                (1)   — 静态工厂入口
│   ├── PoolStats.cs                        (1)   — 统计数据结构
│   └── Extension/
│       ├── ObjectPoolExtensionDependencies (1)   — 外部依赖委托注入
│       ├── SafeTypePool/                   (3)   — 类型级安全池
│       └── GoPool/
│           ├── Container/                  (5)   — 资源容器（单/多资产）
│           ├── Pool/                       (3)   — Unity对象池+Debug
│           └── UnityObjPoolFactory.cs       (1)   — Unity对象池工厂
│
├── Package/Cache/                          (16 files)
│   ├── Cache.cs                            (1)   — 缓存组合器（策略+容器）
│   ├── CacheDelegates.cs                   (1)   — 依赖委托注入
│   ├── CacheFactory.cs                     (1)   — 静态缓存工厂
│   ├── Strategy/                           (6)   — 策略层（LRU/FIFO/Decorator/Stats）
│   ├── ResContainer/                       (5)   — 资源容器层
│   └── Pool/                               (2)   — 价格/累积成本池
│
└── ScriptsC#/Core/CacheSystem/
    └── CacheSystem.cs                       (1)   — 桥接接入层 (ISystem)
```

### 架构分层图

```
┌─────────────────────────────────────────────────────┐
│  接入层: CacheSystem (Core/ISystem)                   │
│    - 桥接 AssetUtil + UniCore 依赖                    │
│    - 暴露 CollectionPool / SafeTypePool 全局访问入口   │
│    - 提供 using 模式 PooledList/HashSet/Queue/Stack   │
├─────────────────────────────────────────────────────┤
│  策略层: IStrategy / LRU / FIFO / StrategyDecorate    │
│    - 插拔式淘汰策略，O(1) LRU (LinkedList+Dictionary)  │
│    - 只在空闲对象上淘汰（in-use 对象不参与淘汰）        │
├─────────────────────────────────────────────────────┤
│  容器层: ICacheResContainer / ICacheResContainer     │
│    - SingleAssetContainer<T>   — 单资产对象池         │
│    - MultiAssetContainer<T>    — 多资产对象池注册表    │
│    - 支持 ChangeParent / MovePos 两种回收模式          │
├─────────────────────────────────────────────────────┤
│  池化层: ObjectPool<T> / UnityObjectPool<T>          │
│    - Interlocked 无锁泛型池 (Entry[] 槽位)            │
│    - SafeTypePool — 类型级池注册表                    │
│    - CollectionPool — 全局集合池                      │
└─────────────────────────────────────────────────────┘
```

---

## 二、核心实现逐层拆解

### 2.1 接口契约层 (8 files)

| 接口 | 路径 | 核心方法 | 设计角色 |
|------|------|----------|----------|
| `IPool` | `Interfaces/IPool.cs` | `void Recycle(object)` | 所有池的根接口 |
| `IObjectPool<T>` | `Interfaces/IObjectPool.cs` | `T Get()`, `void Recycle(T)` | 泛型池 |
| `IMixedObjectPool<TKey,TValue>` | `Interfaces/IMixedObjectPool.cs` | `TValue Get(TKey)`, `void Recycle(TKey, TValue)` | 按 Key 分组的池 |
| `ITypePool` | `Interfaces/ITypePool.cs` | `T Get<T>()`, `void Recycle(object)`, `IObjectPool<T> CreatePool<T>()` | 按类型注册的池注册表 |
| `IObjectFactory<T>` | `Interfaces/IObjectFactory.cs` | `T Create(pool)`, `void Destroy(T)`, `void Reset(T)`, `bool Validate(T)` | 对象创建/销毁/重置工厂 |
| `IMixedObjectFactory<TKey,TValue>` | `Interfaces/IMixedObjectFactory.cs` | 同上，带 Key 参数 | 键控工厂 |
| `IPoolResetable` | `Interfaces/IPoolResettable.cs` | `void Reset()` | 对象重置契约 |
| `IPooledObject` | `Interfaces/IPooledObject.cs` | `OriginPool`, `RecycleToPool()` | 自回收对象（ext IPoolResettable） |

**核心设计思想**: `IPoolResettable.Reset()` 是唯一的重置契约。所有工厂在 `Recycle` 时调用 `factory.Reset(obj)`。`IPooledObject.OriginPool` 用于防止重复回收。

---

### 2.2 ObjectPool\<T\> — 无锁泛型池

**路径**: `ObjectPool/ObjectPool/ObjectPool.cs`
**核心代码**: ~200行

**数据结构**: 固定大小的 `Entry[]` 数组（`struct Entry { T value; }`）

**并发方案**: `Interlocked.CompareExchange` 无锁 CAS

```csharp
// Get: 线性扫描 Entry[], Interlocked.CompareExchange 取出空槽
for (var i = 0; i < _entries.Length; i++)
{
    var value = _entries[i].value;
    if (value == null) continue;
    if (Interlocked.CompareExchange(ref _entries[i].value, null, value) == value)
        return value;
}
return _factory.Create(this); // 池空则创建

// Recycle: Interlocked.CompareExchange 放回空槽
for (var i = 0; i < _entries.Length; i++)
{
    if (Interlocked.CompareExchange(ref _entries[i].value, obj, null) == null)
        return; // 放回成功
}
_factory.Destroy(obj); // 池满则销毁
```

**两个静态工厂方法**:
- `CreatePool<TPoolableObject>(maxSize, initialSize)` — 要求类型实现 `IPoolResettable, new()`
- `CreateWithReset<TObject>(resetFunc, maxSize, initialSize)` — 委托方式提供重置逻辑

**支持 `IPooledObject`**: `Get()` 时自动设置 `OriginPool = this`; `Recycle()` 时验证 `OriginPool == this` 防止跨池回收。

---

### 2.3 SafeTypePool — 类型级池注册表

**路径**: `Extension/SafeTypePool/SafeTypePool.cs`, `TypePool.cs`, `TypedPool.cs`

**架构**: 三级嵌套池
```
SafeTypePool (ConcurrentDictionary<Type, TypePool>)
  └── TypePool (ConcurrentQueue<object> + 单槽 fastItem)
       └── TypedPool<T> (提供 IObjectPool<T> 接口的适配层)
```

**TypePool 的双路径设计**:
- **快速路径**: `_fastItem` 单槽，CAS 操作。命中率最高时 O(1)。
- **慢速路径**: `ConcurrentQueue<object>`，`TryDequeue`。

**SafeTypePool 对外 API**:
```csharp
T Get<T>()                    // 从类型注册表获取
void Recycle(object)          // 回收
void RegisterFactory<T>(Func<T>)           // 注册类型工厂
IObjectPool<T> CreatePool<T>(Func<T>, maxSize)  // 创建类型专属池
void ClearPool<T>()           // 清理特定类型
void ClearAllIdleObjects()    // 清理所有空闲对象
PoolStats GetStats<T>()       // 获取统计
```

---

### 2.4 CollectionPool — 全局集合池

**路径**: `ObjectPool/ObjectPool/CollectionPool.cs` + `PooledCollection.cs`

为 **List、HashSet、Queue、Stack、SortedSet** 五种集合类型各提供一个 `ObjectPool<TCollection>`（maxSize=32）。

**using 模式** (`PooledList<T>` 等):

```csharp
// 内部实现
public readonly struct PooledList<T> : IDisposable
{
    public readonly List<T> value;
    public PooledList(IObjecTPool<List<T>> pool) { value = pool.Get(); }
    public void Dispose() { value.Clear(); pool.Recycle(value); }
    public static implicit operator List<T>(PooledList<T> p) => p.value;
}

// 使用方式
using var pooled = CacheSystem.RentList<int>();
pooled.Value.Add(1); // 或直接 pooled.Add(1) 通过隐式转换
// Dispose 时自动 Clear + Return
```

---

### 2.5 SingleAssetContainer\<T\> — 单资产容器

**路径**: `Extension/GoPool/Container/SingleAssetContainer.cs`

为**一个资源路径（assetName）**管理所有对象实例。

**数据结构**:

| 字段 | 类型 | 用途 |
|------|------|------|
| `FreeStack` | `Stack<PoolObjWrapper<T>>` | 空闲对象栈（LIFO） |
| `_usedUidWrapperMap` | `Dictionary<string, PoolObjWrapper<T>>` | 使用中对象（UID→Wrapper） |
| `_objToUid` | `Dictionary<T, string>` | **反向索引**（对象→UID，污染检测关键） |

**核心方法流程**:

```
Get (异步):
  PreTake() → 从 FreeStack Pop 或 new PoolObjWrapper
  CreateAsync(wrapper, parent) → LoadAssetAsync (异步) → InstantiateAsset (同步)
  Take(wrapper, parent) → InitObj（设置parent）+ MarkAsUsed（加入usedMap）

Put (回收):
  _objToUid 查找 obj → UID → _usedUidWrapperMap 查找 wrapper
  ResetObj → SetParent(PoolRoot) 或 MovePos(FarAway)
  MarkAsUnused → FreeStack.Push(wrapper)

PreLoadAsync:
  状态机: NotLoad → Loading → LoadSuccess/LoadFailed
  await LoadAssetAsync() → 缓存 Asset
```

**两种回收模式** (`PoolContainerMode`):
- `ChangeParent`: 回收时 `SetParent(PoolRoot)`，取出时 `SetParent(target, false)` — 安全但频繁 SetParent 有 GC
- `MovePos`: 回收时移动到 `(9999,9999,9999)` 远处，取出时 `localPosition=0` — 避免 SetParent 开销

---

### 2.6 MultiAssetContainer\<T\> — 多资产容器

**路径**: `Extension/GoPool/Container/MultiAssetContainer.cs`

管理**多个** `SingleAssetContainer<T>`，按 `assetName` 索引。

```csharp
Dictionary<string, SingleAssetContainer<T>> _pools;
```

对外提供 `PreLoadAsync(assetName)` / `Get(assetName, parent)` / `Put(obj)` / `Clear()`，内部委托给对应的 `SingleAssetContainer`。

**Put(T obj)** 的实现比较特殊——遍历所有子容器查找对象:
```csharp
foreach (var pool in _pools.Values)
{
    var wrapper = pool.Put(obj);
    if (wrapper != null) return wrapper;
}
```

---

### 2.7 UnityObjectPool\<T\> — 策略驱动的 Unity 对象池

**路径**: `Extension/GoPool/Pool/UnityObjectPool.cs`
**File**: ~260 行

**核心组合**: `MultiAssetContainer<T>` + `IStrategy<string>`

```csharp
public partial class UnityObjectPool<T> where T : UnityEngine.Object
{
    MultiAssetContainer<T> _container;
    IStrategy<string> _strategy;
    int _capacity;
}
```

**GetAsync 完整流程**:
```
1. BeforeTake(assetName)
   → container.PreTake(assetName) 获取 wrapper
   → strategy.BeforeTake(wrapper.Uid)

2. container.CreateAsync(wrapper, parent)
   → 如果资源未加载则异步加载
   → 如果 wrapper 无效则 Instantiate

3. AfterTake(wrapper, parent)
   → container.Take(wrapper, parent) 移除空闲标记
   → strategy.AfterTake(wrapper.Uid) 或 strategy.Destroy（创建失败时）

4. 返回 wrapper.Obj
```

**Recycle 流程**:
```
1. container.Put(obj) → wrapper
2. strategy.AfterPut(wrapper.Uid) → 触发容量检查
3. 如果超容量: strategy → OnEviction(uid) → container.EvictionByStrategy(uid)
```

**策略回调注册**:
```csharp
strategy.Eviction = OnEviction; // 容量超限时的淘汰回调
container.OnObjExpectInvalidated = OnObUnExpectDestroy; // 对象意外销毁通知策略
```

---

### 2.8 UnityObjPoolFactory — 工厂入口

**路径**: `Extension/GoPool/UnityObjPoolFactory.cs`

提供 **8 个静态工厂方法**，覆盖 3 个维度:
- **资产数**: 多资产 (UnityObjectPool) / 单资产 (UnitySingleAssetPool)
- **类型**: GameObject / Component\<T\>
- **性能模式**: 标准 (ChangeParent) / 高性能 (MovePos)

```csharp
// 多资产 LRU 策略池
CreateGameObjectPool(poolRoot, capacity, containerMode) → UnityObjectPool<GameObject>
CreateComponentPool<T>(poolRoot, capacity, containerMode) → UnityObjectPool<T>

// 单资产池（无策略）
CreateSingleAssetGameObjectPool(assetName, poolRoot, capacity) → UnitySingleAssetPool<GameObject>

// 高性能模式（MovePos）
CreateHighPerfGameObjectPool(poolRoot, capacity, farAway) → UnityObjectPool<GameObject>
```

默认策略是 `LruCacheStrategy`（来自 Cache 包）。

---

### 2.9 Cache\<KeyT, T\> — 策略+容器的组合器

**路径**: `Package/Cache/Cache.cs`

```csharp
public class Cache<KeyT, T>
{
    ICacheResContainer<KeyT, T> Container; // 资源容器
    IStrategy<KeyT> _strategy;             // 淘汰策略
    int _capacity;                         // 容量
}
```

**Take(assetName) 流程**:
```
1. strategy.BeforeTake(assetName) — 策略前置（FIFO: 检查容量，提前淘汰）
2. container.Take(assetName) — 从容器取（如果没有则创建）
3. strategy.AfterTake(assetName) — 策略后置（LRU: 移入 in-use 集合）
```

**Put(obj, assetName) 流程**:
```
1. container.Put(obj, assetName) — 放入容器
2. strategy.AfterPut(assetName) — 策略后置（LRU: 移入 idle 队列，检查容量淘汰）
```

---

### 2.10 LruCacheStrategy — O(1) LRU 淘汰

**路径**: `Package/Cache/Strategy/LruCacheStrategy.cs`

**数据结构**:
```csharp
LinkedList<KeyT> _idleAccessOrder;        // 空闲对象 LRU 顺序 (head=最近, tail=最旧)
Dictionary<KeyT, LinkedListNode<KeyT>> _idleNodeMap;  // O(1) 查找节点
HashSet<KeyT> _inUseObjects;              // 使用中对象（不参与淘汰）
```

**关键设计: 只在空闲对象上淘汰**
```
Put(key): 从 in-use 移除 → 移到 idle 头部 → CheckIdleCapacityAndEvict()
Take(key): 从 idle 移除 → 加入 in-use
Destroy(key): 从所有数据结构中移除

CheckIdleCapacityAndEvict():
  while (_idleAccessOrder.Count > Capacity)
     EvictLastIdle() → Eviction?.Invoke(key)
```

---

### 2.11 CacheSystem — 桥接接入层

**路径**: `Core/CacheSystem/CacheSystem.cs`

**作用**: 将 package 层的 `CacheDependencies` 和 `ObjectPoolExtensionDependencies` 的静态委托注入为实际的 `AssetUtil` 方法。

```csharp
public class CacheSystem : ISystem
{
    public void Init()
    {
        // 注入 Cache 依赖
        CacheDependencies.InstantiateGameObject = (name, parent) => AssetUtil.InstantiateAsync(name, parent);
        CacheDependencies.GetMemory = (key) => AssetBundleIndex.GetMemory(key);

        // 注入 ObjectPool 依赖
        ObjectPoolExtensionDependencies.LoadAssetAsync = (name, owner) => AssetUtil.LoadAssetAsync(name, owner);
        ObjectPoolExtensionDependencies.ReleaseAsset = AssetUtil.ReleaseAsset;
    }

    // 暴露的全局访问点
    public static ObjectPool<List<T>> ListPool<T>() => CollectionPool<T>.ListPool;
    public static PooledList<T> RentList<T>() => new(CollectionPool<T>.ListPool);
    public static SafeTypePool SafeTypePool => IocModule.Instance.ResolveOrDefault<SafeTypePool>();
}
```

---

## 三、关键代码路径

```
┌── 使用场景 1: GameObject 池化（UI 窗口复用）
│
│   游戏业务代码:
│     var pool = UnityObjPoolFactory.CreateGameObjectPool(root, 32);
│     var go = await pool.GetAsync("UI/Panel.prefab", parent);
│     pool.Recycle(go);
│
│   代码路径:
│     UnityObjPoolFactory.CreateGameObjectPool()
│       → new GameObjectPoolContainer(root)           [GoPool/Container/]
│       → new LruCacheStrategy()                      [Cache/Strategy/]
│       → new UnityObjectPool<GameObject>(container, strategy, capacity)
│     pool.GetAsync(assetName, parent)
│       → BeforeTake() → container.PreTake(assetName) → strategy.BeforeTake(uid)
│       → container.CreateAsync(wrapper, parent)
│           → SingleAssetContainer.CreateAsync()
│               → PreLoadAsync()
│                   → LoadAssetAsync()                [委托 → AssetUtil.LoadAssetAsync]
│               → InstantiateAsset(asset, parent)      [Object.Instantiate]
│       → AfterTake() → container.Take(wrapper, parent) → strategy.AfterTake(uid)
│     pool.Recycle(go)
│       → container.Put(go)
│           → _objToUid 查找 UID (污染检测)
│           → ResetObj → SetParent(PoolRoot)
│           → FreeStack.Push(wrapper)
│       → strategy.AfterPut(uid)
│           → LRU: 移入 idle 队列
│           → CheckIdleCapacityAndEvict()
│               → EvictLastIdle() → Eviction?.Invoke(key)
│                   → container.EvictionByStrategy(uid)
│                       → DestroyInFree(uid)          [Object.Destroy]
│
├── 使用场景 2: 集合池化（Update 循环中避免 GC Alloc）
│
│   游戏业务代码:
│     using var list = CacheSystem.RentList<int>();
│     list.Value.Add(1);
│     // using 结束自动 Dispose → Clear + Return
│
│   代码路径:
│     CacheSystem.RentList<T>()
│       → new PooledList<T>(CollectionPool<T>.ListPool)
│     PooledList.Dispose()
│       → value.Clear()
│       → pool.Recycle(value)
│           → ObjectPool<T>.Recycle()
│               → factory.Reset(obj)         [CollectionFactory: Clear()]
│               → Interlocked.CompareExchange 放回 Entry[]
│
├── 使用场景 3: 类型池化（异步委托分配）
│
│   游戏业务代码:
│     var pool = CacheSystem.SafeTypePool;
│     pool.RegisterFactory(() => new MyClass());
│     var obj = pool.Get<MyClass>();
│     pool.Recycle(obj);
│
│   代码路径:
│     SafeTypePool.Get<T>()
│       → ConcurrentDictionary.TryGetValue(type, out TypePool)
│       → TypePool.Get()
│           → 快速路径: CAS _fastItem
│           → 慢速路径: ConcurrentQueue.TryDequeue
│           → 兜底: _factory() 或 反射 ConstructorInfo.Invoke
│     SafeTypePool.Recycle(obj)
│       → TypePool.Recycle(obj)
│           → if (obj is IPoolResettable) obj.Reset()
│           → 快速路径: CAS _fastItem
│           → 慢速路径: ConcurrentQueue.Enqueue (容量检查)
│
└──
```

---

## 四、设计模式汇总

| 模式 | 出现位置 | 用途 |
|------|----------|------|
| **Object Pool** | `ObjectPool<T>`, `TypePool`, `SingleAssetContainer` | 核心池化 |
| **Strategy** | `IStrategy<KeyT>` → `LruCacheStrategy`, `FIFOCacheStrategy` | 可插拔淘汰策略 |
| **Template Method** | `SingleAssetContainer<T>` (abstract LoadAssetAsync/InstantiateAsset/ResetObj) | 容器行为定义 |
| **Abstract Factory** | `IObjectFactory<T>`, `IMixedObjectFactory<TKey,TValue>` | 对象创建/销毁/重置 |
| **Static Factory** | `CreatePool()`, `UnityObjPoolFactory`, `CacheFactory` | 简化创建 |
| **Decorator** | `StrategyDecorate<KeyT>` 包装策略增加 hit/miss 统计 | 统计增强 |
| **Adapter** | `TypedPool<T>` 适配 `SafeTypePool` 到 `IObjectPool<T>` | 接口统一 |
| **RAII** | `PooledList<T>` 等 (`readonly struct IDisposable`) | using 模式 |
| **Delegate Injection** | `CacheDependencies`, `ObjectPoolExtensionDependencies` | 外部依赖注入 |
| **Lock-Free** | `ObjectPool<T>` 的 `Interlocked.CompareExchange`, `TypePool` 的 CAS fastItem | 无锁并发 |
