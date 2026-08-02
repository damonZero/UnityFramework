# 对象池方案对比总结 — 37 vs ETPro

> **⚠️ 状态对齐（2026-07-08 补）**：本文为外部方案（37 / ETPro）的对比分析，**不是** KJ 当前实现的选型记录。KJ 最终采用独立设计的 Pool/Cache 体系，综合借鉴了两者优点但类型命名和层次设计均有差异。KJ 权威类型：`BoundedStore<TKey,TValue>` + `IStoreEvictionPolicy`（Cache 侧）、`GameObjectPool` + `IInstanceRecyclePolicy`（Pool 侧）。详见 `CODEMAP.md` 的 **Framework: Cache** 与 **Framework: Pool** 章节。
>
> 日期: 2026-07-01
> 详细拆解: 参见 `37项目-ObjectPool-Cache体系深度分析.md` 和 `ETPro-GameObjectPool体系深度分析.md`

---

## 一、优缺点总结

### 37 项目 ObjectPool/Cache 体系

**优点:**

1. **层次分明、可组合性强**
   - `IStrategy` (策略) + `ICacheResContainer` (容器) + `IObjectFactory` (工厂) 三个抽象层完全解耦
   - `UnityObjectPool<T>` = `MultiAssetContainer<T>` + `IStrategy<string>` — 策略可以单独替换（LRU vs FIFO vs 自定义）
   - 工厂可以单独替换（`PoolableObjectFactory` / `ResetObjectFactory` / `CollectionFactory` / 自定义）

2. **并发安全**
   - `ObjectPool<T>` 的 `Entry[]` + `Interlocked.CompareExchange` 无锁设计，主线程/后台线程均可安全访问
   - `TypePool` 双路径（单槽 fastItem + ConcurrentQueue）优化了高频场景
   - `SafeTypePool` 的 `ConcurrentDictionary<Type, TypePool>` 线程安全的类型注册表
   - 游戏逻辑主线程 + 资源异步加载回调线程都可以安全操作池

3. **工业级调试和可观测性**
   - `PoolStats` 返回 IdleCount / MaxCapacity / PoolUtilization
   - `UnityObjPoolDebug` (partial 条件编译) 提供 "调试名池" 的详细诊断
   - `StrategyDecorate<T>` 透明包装任何策略，增加 hit/miss 统计
   - 生产环境编译时这些代码完全不参与 IL

4. **RAII 风格的集合池化**
   - `PooledList<T>` 等 `readonly struct IDisposable`，用 `using` 语法自动归还
   - 解决 Update 循环中 `new List<T>()` 的 GC 分配问题

5. **两种回收模式的性能优化**
   - `ChangeParent`: 安全，适合大多数场景
   - `MovePos`: 避免频繁 `SetParent` 的 GC，适合极高频率的取还场景

6. **完整的接口体系**
   - `IObjectPool<T>` / `IMixedObjectPool<TKey,TValue>` / `ITypePool` 三种池接口
   - `IObjectFactory<T>` 的 `Create/Destroy/Reset/Validate` 四件套
   - `IPoolResettable.Reset()` 单一重置契约
   - `IPooledObject.OriginPool` 防跨池回收

**缺点:**

1. **学习成本高**
   - 51 个文件，8 层接口抽象，对于只想"开个池复用一个 Prefab"的场景太重
   - 新人需要先后理解 Interface → ObjectPool → Container → Strategy → Factory，5 层才能串起来
   - 创建池需要理解 `UnityObjPoolFactory` 的 8 个工厂方法的选择

2. **Cache 包和 ObjectPool 包有概念重叠**
   - `Cache<KeyT,T>` 和 `UnityObjectPool<T>` 都做 "策略+容器" 的组合
   - `GameObjectResContainer` (Cache 包) 和 `GameObjectSingleAssetContainer` (ObjectPool 包) 有类似功能
   - 两个包的 `ICacheResContainer` vs `SingleAssetContainer` 是两套平行体系

3. **委托注入是隐式的**
   - `ObjectPoolExtensionDependencies.LoadAssetAsync` 和 `CacheDependencies.InstantiateGameObject` 都是静态委托
   - 在 `CacheSystem.Init()` 中一次性注入，隐式耦合
   - 如果忘记调用 `Init()` 则会 NullReference，没有编译期检查

4. **`PricedCachePool` 和 `AccCostCachePool` 游离在体系外**
   - 这两个用 `SortedDictionary` 做价格/成本排序，但不实现 `IStrategy`
   - 不能和 `Cache` 或 `UnityObjectPool` 组合使用
   - 属于"写了但没用上"的代码

5. **Singleton 依赖**
   - `SafeTypePool` 通过 `IocModule.Instance.ResolveOrDefault` 获取——标准 VContainer 做法
   - `CollectionPool` 是静态属性——非 DI 风格的全局变量

---

### ETPro GameObjectPool 体系

**优点:**

1. **极简，学习成本为零**
   - 只有 3 个文件（Component/Sysmtem/LruCache），~800 行
   - 没有接口层，没有策略层，没有工厂层
   - 任何人 10 分钟读完所有源码就能理解全貌

2. **LRU 淘汰 + 持久化保护**
   - `LruCache` 的 `checkCanPopFunc` 回调让调用者可以否决淘汰
   - `persistentPathCache` 标记常驻资源（登录界面、全局 UI），永不淘汰
   - 淘汰尝试上限（10 次）防止死循环

3. **双层缓存架构清晰**
   - `goPool` (预制体资产层) — LRU 管理，只有预制体被加载到内存
   - `instCache` (实例层) — LIFO 栈，实例快速复用
   - 两层分离意味着：即使预制体被 LRU 淘汰，如果未来又需要，只需要重新 `LoadAssetAsync`

4. **污染检测是亮点（Debug 模式）**
   - 预制体首次加载时记录子节点数
   - 回收时 2 秒后验证子节点数是否一致
   - 能发现"使用了但忘记销毁"的子节点
   - 排除 TMP 动态子对象，避免误报

5. **反向索引防双回收**
   - `instPathCache: Dictionary<GameObject, string>` 是 O(1) 的实例→路径查询
   - 任何不在 `instPathCache` 中的 GameObject 无法回收
   - 简单高效

6. **与 UI Entity 系统直接集成**
   - `GetUIGameObjectAsync<T>` 自动创建 Entity + UITransform + UIWatcher 回调
   - `RecycleUIGameObject<T>` 自动清理 Entity 生命周期 + UI 销毁钩子

**缺点:**

1. **无策略抽象**
   - 淘汰策略硬编码在 `LruCache` 中，要换 FIFO 需要改源码
   - 没有 TTL（时间淘汰）概念，只有容量淘汰
   - 闲置 10 分钟的预制体不会被淘汰，除非有新预制体加载触发容量检查

2. **`checkCanPopFunc` 中硬编码的容量**
   - `MakeFreeSpace()` 用 `DEFAULT_CAPACITY` (255) 而非 `this.capacity`
   - 如果设置了自定义 capacity，淘汰阈值仍然是 255

3. **`CheckNeedUnload` O(n) 性能问题**
   - `instPathCache.ContainsValue(path)` 遍历所有实例的所有值
   - 在 `Cleanup` 中被循环调用 —— 最坏 O(n²)

4. **无并发安全**
   - 所有字典和 `LruCache` 都没有线程安全保护（虽然 Unity 主线程操作暂时没问题）

5. **无调试/统计接口**
   - 没有 `PoolStats`、没有使用率、没有 hit/miss 计数
   - 线上排查池行为只能加日志

6. **紧耦合 ResourcesComponent**
   - `PreLoadGameObjectAsync` 直接调用 `ResourcesComponent.Instance.LoadAsync<GameObject>(path)`
   - 无法替换资源加载后端（比如 Addressables 或自定义加载器）

7. **没有集合池化**
   - 不提供 `List<T>` / `HashSet<T>` 的池化
   - 框架内部大量临时列表分配走 GC

8. **注释掉的 `CheckCleanRes`**
   - 回收后立即检查是否需要清理资产的逻辑被禁用了
   - 预制体资产只在 LRU 淘汰时才释放，可能在内存中长时间闲置

---

## 二、ETPro 值得借鉴的设计

从 ETPro 中，以下设计点值得引入到 KJ 的对象池方案：

| 借鉴点 | 来源 | 说明 |
|--------|------|------|
| **双层缓存 (prefab + instance)** | ETPro `goPool` + `instCache` | 预制体资产和实例分离管理，清晰简洁 |
| **反向索引污染检测** | ETPro `instPathCache` | `Dictionary<GameObject, string>` O(1) 防双回收 |
| **持久化路径保护** | ETPro `persistentPathCache` | 常驻 UI / 全局 Prefab 永不淘汰 |
| **LRU 可否决淘汰** | ETPro `checkCanPopFunc` 回调 | 淘汰前检查"是否有活跃实例"，活跃则不淘汰 |
| **Debug 子节点污染检测** | ETPro `CheckRecycleInstIsDirty` | 回收时验证子节点数，发现"忘了销毁"的子对象 |

---

## 三、差距：ETPro 没有但 37 有的

| 37 项目的优势 | 对 KJ 的价值 |
|--------------|-------------|
| `IStrategy` 可插拔策略 | 未来可能需要 TTU 淘汰或自定义策略 |
| `ObjectPool<T>` 无锁并发 | 主线程 + 异步回调并发安全 |
| `PooledList<T>` using 集合池 | GC 优化的日常工具 |
| `PoolStats` 统计 | 线上可观测性 |
| `UnityObjectPool<T>` 组合模式 | 策略+容器自由组合，扩展性强 |
| `SafeTypePool` 类型池 | 任意 C# 对象的池化 |
| `PoolContainerMode.MovePos` | 高频场景的性能优化 |

---

## 四、结论

**37 项目 ObjectPool/Cache 体系** 是一个工业级、层次分明、可扩展的池化框架。51 个文件看起来多，但每条路径都走得通。对 KJ 框架来说，核心价值在于：策略插拔、无锁并发、集合 using 池、完整的统计调试体系。

**ETPro GameObjectPool** 是一个极简但完整的设计。3 个文件实现了双层缓存 + LRU 淘汰 + 污染检测 + UI 集成。对 KJ 来说，最值得借鉴的是 Prefab/Instance 双层分离的简洁性、反向索引的污染检测、和 `checkCanPopFunc` 的"活跃实例不淘汰"语义。

**两个方案不是互斥的**。37 项目提供了更完整的架构骨架和抽象层次，ETPro 提供了更简洁的具体实现思路。KJ 的对象池可以取两者的长处：用 37 的接口分层 + 策略组合 + 集合池，同时借鉴 ETPro 的双层实例分离 + 反向索引 + 持久化保护。
