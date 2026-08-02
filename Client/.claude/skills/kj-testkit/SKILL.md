---
name: kj-testkit
description: >
  KJ Framework 测试工具包指南。涵盖 AssertEx（NUnit 断言扩展：IsDestroyed/IsNotDestroyed/AreApproximatelyEqual）、RecordingAssetSystem（IAssetSystem 内存 fake，记录加载/释放调用）、CallProbe（调用顺序记录+AssertSequence）、RecordingEventSink<TEvent>（类型化事件记录+AssertSingle/AssertEmpty）、ManualClock（手动时间驱动+递归防护）、ManualTickDriver（Tick/LateTick/FixedTick 手动分发）、TestGameObjectRoot（IDisposable 临时 GameObject 根节点）。
  触发场景：写单元测试、Mock IAssetSystem、验证调用顺序、捕获事件、手动驱动时间/Tick、创建测试 GameObject。
  核心规则：TestKit.asmdef autoReferenced=false + optionalUnityReferences:TestAssemblies 隔离测试依赖；所有 async 方法同步完成；RecordingAssetSystem 不支持 handle/scene/downloader 操作。
metadata:
  doc: CODEMAP.md
  layer: Framework
---

# KJ 测试工具包 (Framework.TestKit)

源码在 `Assets/Framework/TestKit/`。`autoReferenced: false` — 仅测试程序集显式引用。

## 架构速查

```
Assertions/AssertEx.cs           — NUnit 扩展断言
Fakes/RecordingAssetSystem.cs    — IAssetSystem 内存 fake
Fixtures/TestGameObjectRoot.cs   — 临时 GameObject 根节点
Probes/CallProbe.cs              — 调用顺序记录
Probes/RecordingEventSink.cs     — 事件记录器
Time/ManualClock.cs              — 手动时钟
Time/ManualTickDriver.cs         — 手动 Tick 驱动
```

## 各组件使用指南

### AssertEx — 断言扩展

```csharp
// Unity 对象销毁检测（利用 Unity 重载 == null 判断 fake null）
GameObject go = new GameObject();
Object.DestroyImmediate(go);
AssertEx.IsDestroyed(go);     // 验证已销毁
AssertEx.IsNotDestroyed(go);  // 验证未销毁（用 Assert.That + Is.Not.Null）

// 浮点近似比较
AssertEx.AreApproximatelyEqual(expected: 1.0f, actual: 0.9999f, tolerance: 0.0001f);
```

**注意:** `IsDestroyed` 使用 `Assert.That(value, Is.Null)` 而非 `Assert.IsTrue(value == null)`，确保断言失败时打印 actual 值信息。

### RecordingAssetSystem — 资源系统 Fake

```csharp
var assetSystem = new RecordingAssetSystem();

// 注册假资源
assetSystem.RegisterAsset<Texture2D>("Assets/Tex/hero.png", myFakeTexture);
assetSystem.RegisterAsset<GameObject>("Assets/Prefabs/Bullet.prefab", myFakePrefab);

// 加载（同步完成）
var tex = await assetSystem.LoadAssetAsync<Texture2D>("Assets/Tex/hero.png");
// tex == myFakeTexture

// 验证加载记录
Assert.Contains("Assets/Tex/hero.png", assetSystem.LoadedPaths);

// 释放
assetSystem.Release<Texture2D>("Assets/Tex/hero.png");
Assert.Contains("Assets/Tex/hero.png", assetSystem.ReleasedPaths);

// 清理记录（不清理注册的资源）
assetSystem.ClearRecords();

// 不支持的操作会抛 NotSupportedException:
// - LoadAssetHandleAsync, InstantiateAsync, LoadSceneAsync, CreateDownloader
```

**关键行为：**
- 重复注册同一 `(path, Type)` → `InvalidOperationException`
- 类型不匹配 → `InvalidCastException`（含完整类型信息）
- `LoadedPaths` 只在 `FindAsset` 成功后记录，异常不污染
- 所有 `UniTask` 方法同步完成（无异步等待）

### CallProbe — 调用顺序记录

```csharp
var probe = new CallProbe();

// 记录调用
probe.Record("Init");
probe.Record("Load");
probe.Record("Start");

// 断言顺序
probe.AssertSequence("Init", "Load", "Start");  // ✅ 通过

// 断言失败时消息包含 Expected 和 Actual 完整列表
// "Call sequence mismatch. Expected: [Init, Load]. Actual: [Init, Load, Start]."

probe.Clear();
probe.Count;  // 0
```

### RecordingEventSink<TEvent> — 事件记录器

```csharp
var sink = new RecordingEventSink<PlayerLevelUpEvent>();

// 记录事件
sink.Record(new PlayerLevelUpEvent { PlayerId = 1, NewLevel = 5 });

// 断言
sink.AssertEmpty();                    // 验证无事件
var evt = sink.AssertSingle();        // 验证只有一个事件并返回
// 失败消息: "Expected 1 event, got 3: [evt1, evt2, evt3]"

sink.Events   // IReadOnlyList<TEvent> — 所有记录的事件
sink.Count    // 事件数量
sink.Clear(); // 清空
```

### ManualClock — 手动时间驱动

```csharp
var clock = new ManualClock();

clock.Advance(0.016f);  // deltaTime 必须 >= 0
clock.Time;             // 0.016f
clock.DeltaTime;        // 0.016f

// 订阅时间推进
clock.Advanced += dt => { /* dt = 0.016f */ };

// 递归防护
clock.Advanced += dt => clock.Advance(0.1f);  // ❌ InvalidOperationException

clock.Reset();  // Time=0, DeltaTime=0 (不清理事件订阅)
```

### ManualTickDriver — 手动 Tick 驱动

```csharp
var driver = new ManualTickDriver();

driver.Tick += dt => { };       // Update 订阅
driver.LateTick += dt => { };  // LateUpdate 订阅
driver.FixedTick += dt => { }; // FixedUpdate 订阅

driver.Step(0.016f);        // 触发 Tick
driver.StepLate(0.016f);    // 触发 LateTick
driver.StepFixed(0.02f);    // 触发 FixedTick
```

**注意:** `FixedTick` 不模拟 Unity 的单帧多次 FixedUpdate 累积调用。

### TestGameObjectRoot — 测试 GameObject 根节点

```csharp
// using 模式，Dispose 时自动 Destroy
using var root = new TestGameObjectRoot("TestRoot");

root.GameObject;   // GameObject "TestRoot"
root.Transform;    // Transform

var child = root.CreateChild("TestChild");
// child.transform.parent == root.Transform

// Dispose 根据 Application.isPlaying 选择 Destroy 或 DestroyImmediate
// 子节点随层级自动级联销毁
```

## 最佳实践

1. **用 RecordingAssetSystem 测试资源加载逻辑** — 不依赖真实资源，快速可靠
2. **CallProbe + AssertSequence 验证方法调用顺序** — 比 mock 框架轻量
3. **RecordingEventSink 捕获事件** — 配合 AssertSingle 验证"只发布一次"
4. **ManualClock/ManualTickDriver 测试时间/Tick 依赖** — 不要用 `Task.Delay` 或真实 `Time.deltaTime`
5. **TestGameObjectRoot 用 using** — 自动清理，不留脏 GameObject
