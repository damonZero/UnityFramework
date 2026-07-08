---
name: kj-cache
description: >
  KJ Framework 缓存系统指南。涵盖 BoundedStore<TKey,TValue>（有界 KV 存储，lock 并发安全，single-flight GetOrAdd，Put/Remove/淘汰三路径统一的 onEvicted 回调）、IStoreEvictionPolicy<TKey>（可插拔淘汰策略：OnAdded/OnAccessed/OnRemoved 三方法）、LruPolicy<TKey>（O(1) LRU，LinkedList+Dictionary 双索引）、TtlPolicy<TKey>（TTL 过期淘汰，可刷新时钟）、CapacityPolicy<TKey>（FIFO 容量上限）、CompositePolicy<TKey>（策略组合扇出）。
  触发场景：实现数据缓存、图片/资源缓存、LRU 淘汰、TTL 过期、缓存容量控制、工厂模式懒创建、需要 pluggable eviction policy 组合（如 LRU + TTL）。
  核心规则：Framework.Cache 零外部依赖；淘汰策略通过 IStoreEvictionPolicy 可插拔扩展；onEvicted 回调在锁外执行；GetOrAdd 同 key 并发 miss 仅一个线程执行 factory（single-flight）；Put 覆盖已有 key 时旧值走 Remove+Add 两步（满足 onEvicted + 策略感知值变化）。
metadata:
  doc: CODEMAP.md
  layer: Framework
---

# KJ 缓存系统 (Framework.Cache)

完整技术细节见 `CODEMAP.md` 的 Framework: Cache 章节，源码在 `Assets/Framework/Cache/`。

## 架构速查

```
ICache<TKey,TValue>  ←──  BoundedStore<TKey,TValue>
                            ├── Dictionary<TKey,TValue> + lock(_gate)
                            ├── IStoreEvictionPolicy<TKey> (可插拔)
                            │     ├── LruPolicy<TKey>       ← LinkedList + Dictionary 双索引, O(1)
                            │     ├── TtlPolicy<TKey>       ← 时间戳 + 可选访问刷新
                            │     ├── CapacityPolicy<TKey>  ← FIFO 有序，O(1)
                            │     └── CompositePolicy<TKey> ← 策略组合扇出
                            └── single-flight _inflight 协议 (Lazy<TValue>)
```

## 各组件使用指南

### BoundedStore<TKey, TValue> — 有界 KV 存储

```csharp
// 创建：capacity=0 表示无限容量
var store = new BoundedStore<string, Texture2D>(
    capacity: 64,
    policy: new LruPolicy<string>(),
    onEvicted: (key, tex) => Object.Destroy(tex)  // 淘汰/覆盖/Remove 统一回调
);

// TryGet — O(1)，命中时通知策略 OnAccessed（LRU 移到最前）
store.TryGet("hero_icon", out var tex);

// GetOrAdd — 缓存未命中时调用 factory 创建（single-flight，同 key 仅一个线程执行）
var tex = store.GetOrAdd("hero_icon", key => LoadFromDisk(key));

// Put — 显式存入；覆盖已有 key 时旧值走 Remove+Add 两步（onEvicted + OnRemoved）
store.Put("hero_icon", loadedTex);

// Remove — 触发 onEvicted 回调
store.Remove("hero_icon");
store.Clear();
```

**🚀 性能要点：**
- `TryGet` 命中时 O(1)：Dictionary 查找 + 策略 OnAccessed（LRU 下为 LinkedList 移动节点到头部）
- `Put` 覆盖已有 key：先 Remove 旧值（onEvicted + OnRemoved），再 Add 新值（OnAdded）→ 行为与容量淘汰一致
- `GetOrAdd` single-flight：同 key 并发 miss 仅 owner 线程执行 factory，其余等待复用结果；factory 在锁外运行
- `PutUnsafe` H1 守卫：淘汰循环不会选中「刚 Put 的 key」，保证 "Put 后 key 必存在"
- `GetOrAdd` H2 修复：factory 抛异常时 Lazy 进入 faulted 态，finally 中清除 _inflight，防止 key 永久故障
- `capacity=0` 时永远不淘汰

### 淘汰策略

#### LruPolicy<TKey> — O(1) LRU

```csharp
var policy = new LruPolicy<string>();
policy.OnAdded("a");   // a → MRU 头部
policy.OnAccessed("b"); // b → 移到头部（刷新 recency）
policy.TrySelectEvictionCandidate(out var key); // 返回链表尾部（LRU），O(1)
policy.OnRemoved("a"); // 从双索引中删除，O(1)
policy.Clear();
```

#### TtlPolicy<TKey> — TTL 过期淘汰

```csharp
var policy = new TtlPolicy<string>(
    ttl: TimeSpan.FromMinutes(5),
    refreshOnAccess: false,  // 仅写入计时，访问不刷新
    clock: null              // 默认 DateTime.UtcNow.Ticks，可注入自定义时钟用于测试
);
// OnAdded 记录时间戳；OnAccessed 可选刷新
// TrySelectEvictionCandidate: O(n) 扫描，返回任一超时 key；配合 CapacityPolicy 控制规模
```

#### CapacityPolicy<TKey> — FIFO 容量上限

```csharp
var policy = new CapacityPolicy<string>();
// OnAdded → 入队尾；OnAccessed → no-op（FIFO 不重排）
// TrySelectEvictionCandidate → 返回队首（最旧），永不返回刚 Put 的新 key
```

#### CompositePolicy<TKey> — 策略组合

```csharp
// 组合 LRU + TTL：容量+过期双约束
var composite = new CompositePolicy<string>(
    new LruPolicy<string>(),
    new TtlPolicy<string>(TimeSpan.FromMinutes(10))
);
// 所有操作扇出到子策略；TrySelectEvictionCandidate 任一命中即淘汰
```

### 扩展淘汰策略

实现 `IStoreEvictionPolicy<TKey>` 即可：
- `OnAdded(TKey)` — 新条目加入
- `OnAccessed(TKey)` — 已有条目被访问（TryGet 命中）
- `OnRemoved(TKey)` — 条目被移除（Remove / 覆盖 / 容量淘汰）
- `TrySelectEvictionCandidate(out TKey)` — 返回淘汰候选
- `Clear()` — 清空内部状态

## 最佳实践

1. **capacity=0 谨慎使用** — 无界缓存在长期运行时可能 OOM
2. **onEvicted 回调中释放资源** — 图片/GameObject 等非托管资源必须在这里 Dispose/Destroy
3. **factory 保持轻量** — 虽然 factory 在锁外执行，但重 IO 仍会阻塞调用方等待
4. **用 GetOrAdd 代替手动 TryGet+Put** — 自动 single-flight，避免 TOCTOU 竞态
5. **组合策略而非写死** — 用 CompositePolicy 组合 LRU + TTL，而非在 Cache 里加参数

## 依赖图

```
Framework.Cache (Cache.asmdef)
  引用: (none) — 零外部依赖
  不引用: 任何 Scripts/ 代码，也不引用其他 Framework 包
```
