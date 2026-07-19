# ZString / ZLinq / ZLogger 使用规范

> 2026-07-19 | 盘点结论：当前 ZString 零引用、ZLinq 仅 3 个文件使用，框架性能优化空间大
> 源数据：`.planning/PROJECT.md`、`Packages/manifest.json`、各 asmdef 引用

## 库定位

| 库 | asmdef 名 | 依赖 | 特点 |
|---|---|---|---|
| **ZString** | `ZString` | 零外部依赖 | `Utf8ValueStringInterpolation` 零分配字符串拼接 |
| **ZLinq** | `ZLinq.Unity` | 零外部依赖 | 零分配 LINQ（`AsValueEnumerable().Where/Select/Distinct`） |
| **ZLogger** | `ZLogger.Unity` | `Microsoft.Extensions.Logging` | 结构化日志源生成器 `[ZLoggerMessage]` |

这三个是 Cysharp 出品的高性能零分配库，已在项目中引入 (git URL + NuGet 双轨)，是项目技术栈的核心组成部分。

## ZString — 热路径字符串拼接使用

### 核心原则：只在能减少分配的地方用

ZString 的价值是避免字符串拼接产生 GC 分配。**如果当前代码不产生额外分配，用 ZString 没有收益，反而降低可读性。不要强行使用。**

```csharp
// ✅ 不需要 ZString — 单字面量，零分配
GameLog.Info("Asset system ready", module: "Asset");

// ✅ 不需要 ZString — 已经是常量，零分配
const string msg = "Config loaded";

// ✅ 不需要 ZString — 单变量直接传，零分配
GameLog.Error(path, module: "Asset");

// ❌ ZString 画蛇添足 — 原本没有拼接，硬用反而更啰嗦
using (var sb = ZString.CreateStringBuilder())  // 无意义
{
    sb.Append("Asset system ready");
    GameLog.Info(sb.ToString(), module: "Asset");
}
```

### 该用的场景

```csharp
// ✅ 动态拼接多个变量时用 ZString
using var sb = ZString.CreateStringBuilder();
sb.Append("Loaded ");
sb.Append(count);
sb.Append(" assets in ");
sb.Append(elapsedMs);
sb.Append("ms, failed: ");
sb.Append(failedCount);
GameLog.Warn(sb.ToString(), module: "Asset");
```

**判断标准：代码里有没有 `+` 字符串拼接、`$` 插值、`string.Format`、`StringBuilder` 用在热路径上。有→改用 ZString；没有→不用。**

### 特别注意：GameLog 已有编译期裁剪

`GameLog.cs` 的方法带 `[Conditional]`，在裁剪的分支里参数不会被求值。所以即使调用了 ZString，编译期也会跳过。但**在不会裁剪的分支里（如 `Error` 级别），分配依然存在**，这里用 ZString 才有意义。

```csharp
// ✅ 该用 — Error 通常不裁剪，会实际执行
using (var sb = ZString.CreateStringBuilder())
{
    sb.Append("Load failed: ");
    sb.Append(path);
    sb.Append(", error: ");
    sb.Append(ex.Message);
    GameLog.Error(sb.ToString(), module: "Asset");
}

// ⚡ 可用可不用 — Trace 在发布版会被裁剪掉，不会有分配
//    但如果调试时需要 Trace，用 ZString 减少调试时的 GC 压力也是好的
using (var sb = ZString.CreateStringBuilder())
{
    sb.Append("Loading asset [");
    sb.Append(index);
    sb.Append("/");
    sb.Append(total);
    sb.Append("]");
    GameLog.Trace(sb.ToString(), module: "Asset");
}
```

### asmdef 要求

ZString 是零依赖纯 C# 库，asmdef 引用它不会引入新的依赖链。按需引用即可：

| 层 | 引用 ZString | 说明 |
|---|---|---|
| **Framework（Log, RuntimeLog, Event, Asset, Pool, Cache）** | **推荐** | 提前引用好，写代码时想用就能直接用 |
| **Core, General, Project** | **推荐** | 同上 |
| **Boot** | **可选** | 启动流程字符串拼接少，按需加 |
| **Launcher（AOT 壳）** | **可选** | AOT 安全，`BootStartupLog` 可受益 |

⚠️ 引用不等于强制使用 — 加了 asmdef 引用只是允许写 `using ZString`，具体是否用取决于代码里有没有拼接需求。

## ZLinq — 有 LINQ 操作时优先使用

### 核心原则：有 LINQ 才换 ZLinq

如果代码里本来就没有 LINQ 操作，就不要为了用 ZLinq 而硬塞。**ZLinq 的意义是把 `System.Linq` 的堆分配替换为零分配，而不是让你到处写链式调用。**

```csharp
// ✅ 原本就用了 LINQ → 换成 ZLinq
using ZLinq;
var array = assemblies.AsValueEnumerable().Where(a => a != null).Distinct().ToArray();

// ✅ 原本用 foreach 遍历单个数组，简单直观 → 不需要改
foreach (var a in assemblies) { ... }

// ❌ 画蛇添足 — 原本就一个简单遍历，硬改成链式
assemblies.AsValueEnumerable().ForEach(a => { ... }); // 比 foreach 更难读，收益为零
```

### 判断标准：代码里有没有 `using System.Linq` 或者直接 `IEnumerable<T>` 的 `.Where/.Select/.First` 等方法调用。有→换 ZLinq；没有→不用。

### asmdef 要求

ZLinq 同样是零依赖纯 C# 库。按需引用：

| 层 | 引用 ZLinq | 说明 |
|---|---|---|
| **Framework（Event, Log, RuntimeLog, Asset, Pool, Cache）** | **推荐** | Event 已引用；其余模块加好引用，有 LINQ 就能直接用 |
| **Core, General, Project** | **推荐** | Core/General 已引用；Project 按需加 |
| **Boot** | **可选** | 按需 |
| **Launcher（AOT 壳）** | **可选** | AOT 安全，当前依赖最小 |

**豁免场景**（继续用 `System.Linq` 也无妨）：
- Editor 工具（构建管线、编辑器面板）
- 测试代码
- 一次性初始化且数据量 < 10 条

## ZLogger — 业务代码通过 GameLog + ILogger 使用

### 原则

```csharp
// ✅ 结构化日志（DI 注入 ILogger<T>）
public partial class AssetSystemLog
{
    [ZLoggerMessage(LogLevel.Information, Message = "Asset system ready: {count} configs")]
    public static partial void Ready(ILogger<AssetSystemLog> logger, int count);
}

// ✅ 日志门面（无 DI 场景）
GameLog.Info("Asset system ready", module: "Asset");
```

### 架构约束

- `Framework.Log` **不直接引用 ZLogger** — 它零依赖，是纯门面
- `Core` 层通过 `GameLogBridge` 将 `GameLog` 桥接到 ZLogger 管道
- `[ZLoggerMessage]` 的 Log partial 类放在 **Core/General/Project** 层（需要 `ILogger<T>` 的场景）
- Framework 内部代码不写 `[ZLoggerMessage]`，走 `GameLog` 静态门面

## 启动最小层白名单

当前项目有两种"最小"场景，允许引用的库不同：

### Launcher（AOT 壳 `KJ.Launcher.asmdef`）

目标是体积最小、完全 AOT 安全。当前允许：

| 库 | 是否稳定 | 用途 |
|---|---|---|
| UniTask | ✅ | 异步操作 |
| YooAsset | ✅ | 资源加载 |
| HybridCLR.Runtime | ✅ | 热更运行时 |
| AssetShared | ✅ | 共享数据契约 |

**ZString 可以加入 Launcher** — 零依赖纯 C#，AOT 安全，能让 `BootStartupLog` 零分配写日志。推荐加，但非强制。

**ZLinq 可以加入 Launcher** — 零依赖纯 C#。但如果 Launcher 当前不需要 LINQ 操作，可延缓。

### Boot（热更 `KJ.Boot.asmdef`）

热更启动流程，已引用 `Log, RuntimeLog, Asset, UniTask, AssetShared, YooAsset, Launcher`。

**ZString + ZLinq 应该加入 Boot** — 两者零依赖纯 C#，Boot 层的集合操作和日志拼接都能受益。

### AssetShared（AOT 共享层 `Framework.AssetShared.asmdef`）

当前零引用。**可以安全引用 ZString**（纯数据类如果有字符串生成需求）。

## 红线

- **代码里有 `using System.Linq` → 换 ZLinq**（Editor 和测试除外）
- **热路径动态拼接字符串 → 用 ZString**（单字面量或单变量不需要）
- **禁止**在 `Framework.Log` 中直接引用 ZLogger（保持门面零依赖）
- **禁止**在 `Launcher` 中引用任何热更程序集（当前约束不变）
- `[ZLoggerMessage]` partial 方法**禁止**有副作用 — 它只负责记录，不修改状态
- **不要为用而用**——原代码没有 LINQ 就别硬写链式，原代码没有字符串拼接就别硬上 ZString

## 与现有规则的关系

- `1external.md` — 无关，Z系库走 UPM，不走 1External
- `odin-inspector.md` — 无关
- `CLAUDE.md` 依赖方向 — ZString/ZLinq 是零依赖纯 C# 库，不违反任何方向约束

---

# Framework.Pool / Framework.Cache — 运行时减少内存分配

> 项目提供了完善的集合池 (`CollectionPool`) 和通用对象池 (`ObjectPool<T>`)，以及有界缓存 (`BoundedStore`)。
> 这两个模块已在 `Assets/Framework/` 下，asmdef 名 `Pool` / `Cache`，是 Framework 层的核心基础设施。

## CollectionPool — 临时集合必须走池

### 核心原则

**Runtime 代码中任何方法内临时 new 的 `List<T>` / `HashSet<T>` / `Dictionary<TKey,TValue>` / `Queue<T>` / `Stack<T>`，一律用 `CollectionPool.Rent` + `using`，禁止裸 `new`。**

```csharp
// ❌ 堆分配 — 每次调用都 new 一个新集合
var results = new List<string>();
foreach (var item in items) { results.Add(item.Name); }
return results.ToArray();

// ✅ 池化复用 — 归还后 Clear() 下次再用
using var results = PoolService.RentList<string>();
foreach (var item in items) { results.Value.Add(item.Name); }
return results.Value.ToArray();
```

```csharp
// ❌ 临时 Dictionary
var lookup = new Dictionary<int, AssetInfo>();
foreach (var a in assets) { lookup[a.Id] = a; }

// ✅ 池化
using var lookup = PoolService.RentDictionary<int, AssetInfo>();
foreach (var a in assets) { lookup.Value[a.Id] = a; }
```

### 必须用 `using` 或手动 `Dispose()`

`PooledList<T>` / `PooledHashSet<T>` 等是 `[NonCopyable]` struct，通过 `Dispose()` 自动归还池中。**禁止值拷贝，必须 `using` 或显式 `Dispose()`**。

```csharp
// ❌ 忘记 Dispose — 集合泄漏，永不归还
var pooled = PoolService.RentList<int>();
// ... 用 pooled.Value ...
// 方法返回后 pooled 被丢弃，List 泄漏

// ✅ using — 自动归还
using var pooled = PoolService.RentList<int>();
// ... 用 pooled.Value ...
// 作用域结束自动归还

// ✅ 手动 Dispose（需要提前释放的场景）
var pooled = PoolService.RentList<int>();
// ... 用 pooled.Value ...
pooled.Dispose(); // 显式归还
```

### 什么情况不需要走池

```csharp
// ✅ 不需要 — 长期持有、跨方法传递的集合
// 这种不是临时集合，是对象的成员字段，正常 new 即可
private readonly List<IDisposable> _disposables = new();

// ✅ 不需要 — 直接返回给调用方作为 API 输出
// 如果池化，调用方 Dispose 后集合就回到池里了（数据被清除），调用方无法持有
public List<Item> GetItems() { ... } // 返回 new List，调用方自己管理生命周期
```

### 判断标准：集合的生命周期是否局限于当前方法内。是→走池+using；不是（成员字段/返回给外部持有）→正常 new。

## ObjectPool<T> — 频繁创建销毁的普通对象

```csharp
// ✅ 池化 POCO 对象
private static readonly ObjectPool<MyData> _dataPool = new(
    factory: () => new MyData(),
    reset: data => data.Reset(),
    maxIdle: 32
);

// 租用
using var lease = _dataPool.RentLease();
var data = lease.Value;
// ... 使用 ...

// ✅ 线程不安全场景用 SingleThreadObjectPool<T>（更快，无锁）
private static readonly SingleThreadObjectPool<MyData> _dataPool = new(
    factory: () => new MyData(),
    reset: data => data.Reset(),
    maxIdle: 32
);
```

## BoundedStore — 有界缓存替代裸 Dictionary

当需要"容量有限的缓存，超出淘汰"时，用 `BoundedStore` 替代 `Dictionary` + 手动淘汰逻辑：

```csharp
// ❌ 手动管理淘汰
private readonly Dictionary<string, Texture> _cache = new();
// ... 每次 Put 前检查容量、手动 Remove ...

// ✅ BoundedStore 内置淘汰策略
private readonly BoundedStore<string, Texture> _cache = new(
    capacity: 64,
    policy: new LruPolicy<string>(),
    onEvicted: (key, tex) => UnityEngine.Object.Destroy(tex)
);
```

淘汰策略：`LruPolicy`（最近最少使用）、`TtlPolicy`（过期时间）、`CapacityPolicy`（优先淘汰命中次数少的）、`CompositePolicy`（组合多种策略）。

## 不使用就別引

**代码里没有临时集合/重复new对象/需要淘汰缓存的需求，就不要引用 Pool/Cache。** 和 ZString/ZLinq 一样的原则：有这个需求才用，不要为了用而用。

## asmdef 要求

`Pool` 和 `Cache` 是 Framework 层模块。所有引用了 `Pool` 或 `Cache` 的 asmdef 都可以通过 `CollectionPool` / `ObjectPool` / `BoundedStore` 使用上述模式。

`CollectionPool.RentXxx()` 的静态方法在 `Core.PoolService` 中已暴露为便捷入口，Core/General/Project 层都可以直接使用。

---

# 日志写入规范 — 让 AI 能读懂运行日志

> 完整设计见 `.planning/AI_RUNTIME_LOGGING.md`，本节聚焦代码层面必须遵守的写入规则。

## 总原则

KJ 日志系统两个输出端：
- **Console**（Unity Debug.Log）→ 人看
- **JSONL 文件**（`Logs/Runtime/latest.jsonl`）→ AI 读

**AI 分析以 `.jsonl` 为准，不得依赖用户截图 Console。**

## 三层 API 选择

| 层 | 使用什么 | 示例 |
|---|---|---|
| **Framework**（Log, RuntimeLog, Asset, Event, Pool, Cache） | `GameLog.Info/Warn/Error(...)` | 静态门面，零依赖 |
| **Boot** | `GameLog.Info/Warn/Error(...)` | Boot 不能依赖 Core/ZLogger |
| **Core / General / Project** | `ILogger<T>` + `[ZLoggerMessage]`（首选） 或 `GameLog`（无 DI 场景） | DI 注入的用 ZLogger；纯静态逻辑用 GameLog |

## 日志级别语义

只在正确语义下选择级别，不要无脑 Info：

| 级别 | 场景 |
|------|------|
| **Trace** | 每帧/高频详细诊断（加载进度、状态机步进） |
| **Debug** | 开发者调试信息（函数入口/出口、变量值） |
| **Information** | 重要的运行时事件（模块初始化完成、资源加载成功、配置热更应用） |
| **Warning** | 可恢复异常（资源降级、超时重试、配置缺失有默认值） |
| **Error** | 不可恢复但可跳过（某个模块初始化失败但引擎继续运行、资源加载失败有 fallback） |
| **Critical** | 致命错误需要立刻关注（启动链路中断、热更失败无法继续、数据损坏） |

```csharp
// ❌ 无脑 Info
_logger.ZLogInformation($"Loading asset: {path}"); // 每个资源加载都是 Info，噪音

// ✅ 正确语义
// 高频细节 → Trace/Debug
_logger.ZLogTrace($"Loading asset: {path}");  
// 模块就绪 → Information  
GameLog.Info("Asset system ready", module: "Framework.Asset");
// 可恢复失败 → Warning
_logger.ZLogWarning($"Asset not found, fallback to default: {path}");
// 初始化失败 → Error
GameLog.Error($"System init failed: {name}", module: "Core.SystemManager");
// 启动中断 → Critical
GameLog.Critical("Boot failed, cannot continue", module: "Boot");
```

## JSONL 文件格式要求

每条日志一行 JSON。代码层面不需要手动构造 JSON —— 这是 `RuntimeLogSession` / `ZLoggerMessage` / `GameLogBridge` 的事。但写代码时要确保落盘的关键字段正确：

- **module** — `GameLog` 的第二个参数、或 `ILogger<T>` 的 `T` 类名，会对接到 `.jsonl` 的 `module` 字段。**不要省略 module**：
  ```csharp
  // ❌ 缺 module — AI 无法按模块过滤
  GameLog.Info("Ready");
  
  // ✅ module 对 AI 归因很关键
  GameLog.Info("Ready", module: "Framework.Asset");
  ```

- **exception** — 必须传原始 Exception，不要只传 `ex.Message`：
  ```csharp
  // ❌ 丢失堆栈 — AI 无法定位
  GameLog.Error($"Load failed: {ex.Message}", module: "Asset");
  
  // ✅ 传原始异常 — JSONL 里会有完整堆栈
  GameLog.Exception(ex, "Load failed", module: "Asset");
  ```

- **[ZLoggerMessage] 模板** — 用命名参数，不要拼接：
  ```csharp
  // ❌ 拼接 — JSONL 里是字符串，AI 无法按参数提取
  _logger.ZLogError($"Asset {path} failed with {errcode}");
  
  // ✅ 命名参数 — JSONL 里 path 和 errcode 是独立 key
  [ZLoggerMessage(LogLevel.Error, "Asset load failed")]
  static partial void AssetFailed(ILogger logger, string path, int errcode);
  ```

## AI 分析工作流

当需要分析运行期问题时，按此顺序操作（由 AI 自己执行）：

1. 读取 `Logs/Runtime/latest.session.json` → 了解这次运行的环境和配置
2. 读取 `Logs/Runtime/latest.jsonl`，先 grep `Critical` / `Error` / `Exception`
3. 按 `phase` 字段回溯启动链路：`Boot → HybridCLR → Core.Asset → Core.Init → ModelLifecycle`
4. 资源问题补看 `assetPackageName` / `assetPackageVersion` 关联字段
5. 热更问题补看 `hotUpdateAssemblies` / DLL 加载顺序
6. 最终报告引用日志文件路径 + 关键错误摘要，不依赖 Console 截图

## 禁止事项

- **禁止**在 Runtime 代码用 `Debug.Log`（Editor 工具除外）
- **禁止**把异常只传 `ex.Message`（必须传原始 Exception 对象）
- **禁止**省略 `GameLog` 的 `module` 参数
- **禁止**在日志中记录 token、密码、实名、支付信息等敏感数据
- **禁止**在业务逻辑代码中散写 `[Conditional]` 日志符号（集中在 `GameLogSymbols`）
- **禁止**依赖用户截图 Console 作为调试方式，优先读 `.jsonl`
