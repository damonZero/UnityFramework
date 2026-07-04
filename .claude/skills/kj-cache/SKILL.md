---
name: kj-cache
description: >
  KJ Framework 缓存系统指南。涵盖 Cache<TKey,TValue>（通用缓存，可插拔淘汰策略，lock 并发安全）、LruCachePolicy<TKey>（O(1) LRU，LinkedList+Dictionary 双索引）、ResourceCache<TKey,TValue>（工厂模式资源容器，支持 reset 回调）。
  触发场景：实现数据缓存、图片/资源缓存、LRU 淘汰、缓存容量控制、工厂模式懒创建、需要 pluggable eviction policy。
  核心规则：Framework.Cache 零外部依赖；ICacheEvictionPolicy 可插拔扩展；Cache 的 onEvicted 回调在外部处理资源释放；所有公共方法 lock 保护。
metadata:
  doc: CODEMAP.md
  layer: Framework
---

# KJ 缓存系统 (Framework.Cache)

完整技术细节见 `CODEMAP.md` 的 Framework: Cache 章节，源码在 `Assets/Framework/Cache/`。

## 架构速查

```
ICache<TKey,TValue>  ←──  Cache<TKey,TValue>
                          ├── Dictionary<TKey,TValue> + lock(_gate)
                          └── ICacheEvictionPolicy<TKey> (可插拔)
                              └── LruCachePolicy<TKey>  ← LinkedList + Dictionary 双索引

ICacheResContainer<TKey,TValue>  ←──  ResourceCache<TKey,TValue>  (工厂模式)
```

## 各组件使用指南

### Cache<TKey, TValue> — 通用缓存

```csharp
// 创建：capacity=0 表示无限容量
var cache = new Cache<string, Texture2D>(
    capacity: 64,
    policy: new LruCachePolicy<string>(),
    onEvicted: (key, tex) => Object.Destroy(tex)  // 淘汰时释放资源
);

// TryGet — O(1)，命中时 Touch 策略（LRU 移到最前）
if (cache.TryGet("hero_icon", out var tex)) { }

// GetOrAdd — 缓存未命中时调用 factory 创建
var tex = cache.GetOrAdd("hero_icon", key => LoadFromDisk(key));

// Put — 显式存入，超容量时自动淘汰
cache.Put("hero_icon", loadedTex);

// Remove — 触发 onEvicted 回调
cache.Remove("hero_icon");

// Clear — 清空但不触发 onEvicted
cache.Clear();
```

**🚀 性能要点：**
- `TryGet` 命中时 O(1)：Dictionary 查找 + LinkedList 移动节点到头部
- `Put` 可能触发多次淘汰（while `count > capacity`），每次从尾部取 LRU 候选
- `GetOrAdd` 在锁内调 factory — factory 要快，否则阻塞其他访问
- `capacity=0` 时永远不淘汰（内部 while 条件 `_capacity > 0` 不满足）

### LruCachePolicy<TKey> — O(1) LRU 实现

```csharp
var policy = new LruCachePolicy<string>();

policy.Touch("a");  // 访问/新增：移到链表头部（MRU）
policy.Touch("b");
policy.Touch("a");  // a 再次移到头部

policy.TrySelectEvictionCandidate(out var key);  // 返回链表尾部（LRU=b），O(1)
policy.Remove("a");  // 从双索引中删除，O(1)
policy.Clear();      // 清空两个数据结构
```

**实现细节：**
- `LinkedList<TKey>` — 维护访问顺序，AddFirst=MRU，Last=LRU
- `Dictionary<TKey, LinkedListNode<TKey>>` — 提供 O(1) 节点查找
- `Touch` 已存在 key → Remove + AddFirst（O(1)）；新 key → AddFirst + 登记（O(1)）

### ResourceCache<TKey, TValue> — 工厂模式资源容器

```csharp
// 创建：factory 负责按 key 创建，reset 负责清理
var res = new ResourceCache<string, GameObject>(
    factory: path => Resources.Load<GameObject>(path),
    reset: go => Resources.UnloadAsset(go)
);

// GetOrCreate — 懒创建
var go = res.GetOrCreate("Prefabs/Enemy");

// TryRemove — 移除并调用 reset
if (res.TryRemove("Prefabs/Enemy", out var removed))
    Debug.Log("Unloaded");

// Clear — 对所有值调 reset 后清空
res.Clear();
```

**与 Cache 的差异：**
- ResourceCache **没有容量限制**，永不自动淘汰
- 不是 LRU，不管访问频率
- 适合"加载一次、长期持有、手动移除"的场景

## 扩展淘汰策略

实现 `ICacheEvictionPolicy<TKey>` 即可自定义淘汰策略：
- FIFO：`Touch` 时只在第一次入队
- LFU：`Touch` 增加计数器，淘汰时选最小频率
- TTL：`Touch` 记录时间戳，淘汰时检查过期

## 最佳实践

1. **capacity=0 谨慎使用** — 无界缓存在长期运行时可能 OOM
2. **onEvicted 回调中释放资源** — 图片/GameObject 等非托管资源必须在这里 Dispose/Destroy
3. **factory 要保持轻量** — 它在 `lock` 内执行，重 IO 操作会阻塞所有缓存访问
4. **用 GetOrAdd 代替手动 TryGet+Put** — 避免 TOCTOU 竞态，且代码更简洁
5. **ResourceCache 适用固定资源集** — 比如配置表数据、全局 Prefab，不适合大量动态数据的缓存

## 依赖图

```
Framework.Cache (Cache.asmdef)
  引用: (none) — 零外部依赖
  不引用: 任何 Scripts/ 代码，也不引用其他 Framework 包
```
