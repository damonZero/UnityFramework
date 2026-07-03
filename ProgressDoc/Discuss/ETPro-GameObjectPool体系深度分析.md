# ETPro GameObjectPool 体系深度分析

> 来源: [GitHub: 526077247/ETPro](https://github.com/526077247/ETPro)
> 分析文件: `GameObjectPoolComponent.cs` (Model) + `GameObjectPoolComponentSystem.cs` (Hotfix) + `LruCache.cs`
> 分析日期: 2026-07-01

---

## 一、文件清单

| 文件 | 路径 | 行数(估) | 角色 |
|------|------|----------|------|
| `GameObjectPoolComponent.cs` | `ModelView/Module/Resource/` | ~50 | 数据组件（Entity + 字段定义） |
| `GameObjectPoolComponentSystem.cs` | `HotfixView/Module/Resource/` | ~570 | 全部行为逻辑 |
| `LruCache.cs` | `Model/Module/Cache/` | ~180 | 通用 LRU 缓存实现 |

---

## 二、架构概览

```
GameObjectPoolComponent (挂载在 Game.Scene)
├── goPool: LruCache<string, GameObject>         ← 预制体资产缓存 (max 255)
├── instCache: Dictionary<string, List<GameObject>>  ← 回收的实例 (path → 空闲列表)
├── instPathCache: Dictionary<GameObject, string>     ← 反向索引 (实例 → path)
├── goInstCountCache: Dictionary<string, int>         ← 每个path的历史总实例数
├── persistentPathCache: Dictionary<string, bool>     ← 永不淘汰的path集合
├── goChildsCountPool: Dictionary<string, int>        ← 调试: 预制体子节点数
├── detailGoChildsCount: Dictionary<string, Dict>     ← 调试: 逐子节点的计数
└── cacheTransRoot: Transform                         ← 回收实例挂载点 (DontDestroyOnLoad)
```

**单一入口、一切挂在一个 Entity 上。没有额外策略层/容器层抽象。**

---

## 三、LRU Cache 实现

### 数据结构

```csharp
class LruCache<TKey, TValue>
{
    Dictionary<TKey, TValue> dictionary;        // O(1) 键值查找
    LinkedList<TKey> linkedList;                // 访问顺序 (head=最近, tail=最旧)
    ReaderWriterLockSlim locker;                // 线程安全
    int capacity;                               // 默认 255

    // 两个回调（Set 时传入）
    Action<TKey, TValue> popCb;                 // 淘汰回调
    Func<TKey, TValue, bool> checkCanPopFunc;   // 淘汰前置检查
}
```

### 关键行为

**TryGet** — 访问命中时更新 LRU 顺序:
```
1. dictionary.TryGetValue → 找到
2. linkedList.Remove(key) + linkedList.AddFirst(key)  ← 标记为最近使用
3. 返回 true
```

**TryOnlyGet** — 只读访问，不更新 LRU 顺序（用于检查存在性）

**Set** — 添加新条目，可能触发淘汰:
```
1. checkCanPopFunc != null → 不立即淘汰，等 MakeFreeSpace
2. linkedList.AddFirst(key)
3. dictionary[key] = value
4. 如果 checkCanPopFunc == null (简单模式) → 容量超限则淘汰尾部
```

**MakeFreeSpace** — LRU 淘汰引擎:
```
1. 从 linkedList.Last 开始 (最久未使用)
2. 最多检查 10 个候选 (MAX_CHECK_FREE_TIMES = 10)
3. 对每个候选:
   - checkCanPopFunc(key, value) → true?
     条件: goInstCountCache[path] - instCache[path].Count == 0 && !persistentPathCache.ContainsKey(path)
     (所有实例已回收 且 path 不是持久化的)
   - true: 删除 entry → popCb(key, value) → ReleaseAsset
   - false: 跳过这个候选, count++ 继续
4. 如果 10 个都不可淘汰 → 放弃淘汰，新条目仍然加入 (池可以超过 255)

[注] eviction 循环中用了硬编码的 DEFAULT_CAPACITY (255) 而不是 this.capacity —
    如果构造函数传了自定义 capacity，淘汰阈值仍然是 255。这是一个已知问题。
```

---

## 四、GetGameObjectAsync 完整流程

```csharp
public static async ETTask<GameObject> GetGameObjectAsync(
    this GameObjectPoolComponent self, string path)
```

```
┌── Step 1: TryGetFromCache(path) ────────────────────────────┐
│                                                               │
│  CheckHasCached(path)                                         │
│    • 空路径 → Log.Error, return false                         │
│    • 不以 ".prefab" 结尾 → Log.Error, return false            │
│    • instCache 有 entries 或 goPool 包含 key → return true    │
│                                                               │
│  [Priority 1] instCache 检查 (空闲实例缓存)                     │
│    • instCache[path].RemoveAt(count-1)  ← LIFO 弹出           │
│    • InitInst(inst) → SetActive(true)                         │
│    • return true                                              │
│                                                               │
│  [Priority 2] goPool 检查 (预制体资产存在，需要 Instantiate)    │
│    • goPool.TryGet(path)  ← 标记为最近使用                     │
│    • GameObject.Instantiate(pooledGo) → 新实例                │
│    • goInstCountCache[path]++                                 │
│    • instPathCache[inst] = path          ← 反向索引注册       │
│    • InitInst(inst) → SetActive(true)                         │
│    • return true                                              │
│                                                               │
│  [Miss] return false                                          │
└───────────────────────────────────────────────────────────────┘
    │
    ├── 命中 → callback?.Invoke(inst) → return inst
    │
    └── Miss →
          await PreLoadGameObjectAsync(path, instCount=1)
            ├── CoroutineLock(path.GetHashCode())  ← 防并发
            ├── CheckHasCached(path) 再次检查 (double-check)
            ├── Miss:
            │     await ResourcesComponent.LoadAsync<GameObject>(path)
            │     CacheAndInstGameObject(path, go, instCount=1)
            │       • goPool.Set(path, go)         ← LRU, 可能触发淘汰
            │       • InitGoChildCount(path, go)   ← 记录子节点数 (debug only)
            │       • Instantiate 1 个实例 (inactive, 挂 cacheTransRoot)
            │       • instCache[path] = [inst]
            │       • instPathCache[inst] = path
            │       • goInstCountCache[path] = 1
            ├── CoroutineLock 释放
            ├── TryGetFromCache(path) 再次尝试 → 命中
            └── return inst
```

**关键设计**:
- **LIFO 回收 + 弹出**: 最刚回收的实例最先被取出（缓存热对象）
- **CoroutineLock 防并发**: 用 path 的哈希值做锁 key，防止同路径并发加载
- **Double-check**: `CoroutineLock` 内再次检查缓存（经典的 double-check locking）
- **Prefab 和实例分离缓存**: `goPool` 存原始预制体，`instCache` 存已实例化的副本

---

## 五、RecycleGameObject 流程

```csharp
public static void RecycleGameObject(this GameObjectPoolComponent self, GameObject inst, bool isclear = false)
```

```
┌── Normal Recycle (isclear = false) ─────────────────────────┐
│                                                               │
│  1. Guard: self == null || self.IsDisposed → return           │
│                                                               │
│  2. 污染检测: instPathCache.ContainsKey(inst)                  │
│     • 不在 → Log.Error "inst not found from instPathCache"    │
│       (防止双回收、防止回收未知对象)                             │
│     • return                                                  │
│                                                               │
│  3. path = instPathCache[inst]                                │
│                                                               │
│  4. 调试污染检测: CheckRecycleInstIsDirty(path, inst)          │
│     (仅 Define.Debug 模式, 详见图5.1)                          │
│                                                               │
│  5. Reset: inst.transform.SetParent(cacheTransRoot, false)    │
│     • 不保留世界坐标 → localPosition 归零                     │
│                                                               │
│  6. inst.SetActive(false)                                     │
│                                                               │
│  7. instCache[path].Add(inst)  ← 回到空闲池                   │
│                                                               │
│  [注] 末尾有注释掉的 CheckCleanRes(path) — 立即清理被禁用了    │
│       清理推迟到 LRU 淘汰时执行                                │
└───────────────────────────────────────────────────────────────┘

┌── Destroy Recycle (isclear = true) ──────────────────────────┐
│                                                               │
│  1. instPathCache 查找 path                                   │
│  2. goInstCountCache[path]--                                  │
│  3. 调试污染检测                                               │
│  4. GameObject.Destroy(inst)                                  │
│  5. instPathCache 移除                                        │
└───────────────────────────────────────────────────────────────┘
```

### 调试时污染检测 (CheckRecycleInstIsDirty)

```
仅在 Define.Debug 模式下执行:

1. 记录预制体时 (InitGoChildCount)
   → 递归遍历所有子 GameObject
   → goChildsCountPool[path] = 总数
   → detailGoChildsCount[path] = 逐子路径计数
   → 排除 TMP 自动生成的子对象
     ("Input Caret", "TMP SubMeshUI", "TMP UI SubObject", TextArea/Caret)

2. 回收时 (CheckRecycleInstIsDirty)
   → 设实例为 inactive
   → 等待 2000ms (让 TMP/Input 等运行时系统来得及清理动态子对象)
   → 确认实例仍在池中 (CheckInstIsInPool)
   → 递归统计当前子对象数
   → 对比 pristine vs recycled 计数
   → 不匹配 → Log.Error 详细信息

3. Release 模式: 开销为零
```

---

## 六、LRU 淘汰 → ReleaseAsset 完整链路

```
触发: goPool.Set(path, go) 在 CacheAndInstGameObject 中
  → LruCache.Set()
    → MakeFreeSpace()
      → linkedList.Last 开始遍历 (最多10个候选)
      → checkCanPopFunc(key) → true?
        条件: 所有实例已回收 && 非持久化
        → popCb(key, value) → ReleaseAsset(path)
          
ReleaseAsset(path):
  1. 销毁 instCache[path] 中所有 GameObject 实例
     → Object.Destroy(inst) for each
     → instPathCache 中移除每个实例
  
  2. 移除 instCache[path] + goInstCountCache[path]
  
  3. goPool.TryOnlyGet(path) → pooledGo
     • TryOnlyGet 不更新 LRU 顺序（只读访问）
     • CheckNeedUnload(path) → 检查是否还有任何实例引用该 path
       → instPathCache.ContainsValue(path) ← O(n)
     • true → ResourcesComponent.ReleaseAsset(pooledGo)
     • goPool.Remove(path)

CheckNeedUnload(path):
  return !instPathCache.ContainsValue(path)
  // 如果整个 instPathCache 中没有任何值等于这个 path，才卸载预制体
  // O(n) 复杂度，遍历所有实例
```

**关键设计**:
- 淘汰只在 `Set` 时触发（新增预制体时才可能淘汰旧的），不是定时任务
- 淘汰前检查 `checkCanPopFunc`: 有活跃实例的预制体不会被淘汰
- 淘汰后检查 `CheckNeedUnload`: 只有完全没有实例引用的预制体才真正释放

---

## 七、备选回收路径: RecycleUIGameObject (UI 集成)

```csharp
public static void RecycleUIGameObject<T>(this GameObjectPoolComponent self, T obj)
    where T : Entity, IAwake, IOnCreate
```

专为 ET 的 UI Entity 设计:
```
1. obj.GetComponent<UITransform>() → 获取 Transform
2. RecycleGameObject(go) → 回收 GameObject
3. obj.BeforeOnDestroy() → Entity 生命周期钩子
4. UIWatcherComponent.Instance.OnDestroy(obj) → UI 销毁钩子
```

**对应的 GetUIGameObjectAsync\<T\>**:
```
1. GetGameObjectAsync → 获取 GameObject
2. 创建 Entity: Game.Scene.AddChild<T, GameObject>(gameObject)
3. AddComponent<UITransform>
4. UIWatcherComponent.OnCreate → UI 创建钩子
```

---

## 八、数据结构总结

| 结构 | Key | Value | 用途 |
|------|-----|-------|------|
| `goPool` | `string` (path) | `GameObject` | 预置体资产 (LRU 管理, max 255) |
| `instCache` | `string` (path) | `List<GameObject>` | 空闲实例池 (LIFO) |
| `instPathCache` | `GameObject` | `string` (path) | 反向索引 — 污染检测核心 |
| `goInstCountCache` | `string` (path) | `int` | 每个 path 的总实例数 |
| `persistentPathCache` | `string` (path) | `bool` | 永不淘汰标记 |
| `goChildsCountPool` | `string` (path) | `int` | Debug: 预置体子节点数 |
| `detailGoChildsCount` | `string` (path) | `Dictionary<string,int>` | Debug: 逐子节点计数 |

**污染检测链**: `instPathCache[GameObject] → path` + `goInstCountCache[path] = 总数` + `instCache[path].Count = 空闲数` → `总数 - 空闲数 = 活跃数`

---

## 九、与 37 项目 ObjectPool/Cache 体系的结构对比

| 维度 | 37 项目 | ETPro |
|------|---------|-------|
| 文件数 | 51 | 3 |
| 策略抽象 | `IStrategy<T>` (LruCacheStrategy/FIFOCacheStrategy 可插拔) | 内嵌在 LruCache 中 (无接口) |
| 容器抽象 | `ICacheResContainer` / `SingleAssetContainer<T>` / `MultiAssetContainer<T>` | 无, 直接字典操作 |
| 池骨架 | `ObjectPool<T>` (Entry[] + Interlocked 无锁) | `LruCache<string, GameObject>` |
| 重置契约 | `IPoolResettable.Reset()` | Transform 重设 + SetActive(false) |
| 污染检测 | `_objToUid` 反向索引 Prevent Double Recycle | `instPathCache` 反向索引 + Debug 子节点统计 |
| 工厂 | `IObjectFactory<T>` (Create/Destroy/Reset/Validate 四件套) | 无工厂, 直接 Instantiate |
| 调试 | `PoolStats` + `UnityObjPoolDebug` | Define.Debug 子节点对比 |
| using 池 | `PooledList/Queue/Stack/HashSet` (RAII) | 无 |
| 类型池 | `SafeTypePool` (ConcurrentDictionary<Type, TypePool>) | 无 |
| 集合池 | `CollectionPool<T>` (List/Set/Queue/Stack/SortedSet) | 无 |
| 回收模式 | ChangeParent + MovePos 两种 | 仅 ChangeParent |
| 淘汰粒度 | `UnityObjectPool` 组合策略, 按 idle 容量淘汰 | `LruCache` 按总条目淘汰, 但跳 on-use |
| 并发 | `Interlocked.CompareExchange` 无锁 + `ConcurrentDictionary` | `ReaderWriterLockSlim` |
| 依赖注入 | 静态委托 (`CacheDependencies` / `ObjectPoolExtensionDependencies`) | 直接引用 `ResourcesComponent.Instance` |
