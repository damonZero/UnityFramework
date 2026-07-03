# TestKit Review 报告

**Review 日期:** 2026-07-03  
**Review 范围:** `Assets/Framework/TestKit/`  
**Review 轮次:** 第 3 轮

---

## 整体评价

框架设计清晰，分类合理（Assertions / Fakes / Fixtures / Probes / Time），asmdef 配置正确（`autoReferenced: false` + `optionalUnityReferences: ["TestAssemblies"]`）。经过前两轮 review 修复后，当前代码无 crash 级别缺陷。以下按严重程度列出剩余问题。

---

## 🔴 需要修复

### 1. `AssertEx.IsDestroyed` / `IsNotDestroyed` — 断言失败时信息丢失严重

**文件:** `Assets/Framework/TestKit/Assertions/AssertEx.cs:10,15`

```csharp
Assert.IsTrue(value == null, message ?? "...");
Assert.IsFalse(value == null, message ?? "...");
```

**问题:** `Assert.IsTrue(bool)` 是 NUnit 中信息量最差的断言重载。当断言失败时，NUnit 只输出 `Expected: True, But was: False`，不显示 actual value 的类型、名称或自定义 message。测试失败后开发者看不到"哪个对象没被销毁"等关键上下文，排查效率低。

**建议:** 改用 `Assert.That(value, Is.Null, message)` / `Assert.That(value, Is.Not.Null, message)`，NUnit 会在失败时打印 actual value 信息。

```csharp
public static void IsDestroyed(Object value, string message = null)
{
    Assert.That(value, Is.Null, message ?? "Expected Unity object to be destroyed.");
}

public static void IsNotDestroyed(Object value, string message = null)
{
    Assert.That(value, Is.Not.Null, message ?? "Expected Unity object to exist.");
}
```

**影响范围:** 所有使用 `AssertEx.IsDestroyed` / `IsNotDestroyed` 的测试用例。

---

### 2. `RecordingAssetSystem.LoadAssetAsync` — loadedPaths 在 FindAsset 之前记录，异常时数据被污染

**文件:** `Assets/Framework/TestKit/Fakes/RecordingAssetSystem.cs:32-36`

```csharp
public UniTask<T> LoadAssetAsync<T>(string path) where T : Object
{
    _loadedPaths.Add(path);          // ← 先记录
    return UniTask.FromResult(FindAsset<T>(path));  // ← 后查找（可能抛异常）
}
```

**问题:** 当 `RegisterAsset<Texture2D>("hero", tex)` 后调用 `LoadAssetAsync<Sprite>("hero")`：
1. `"hero"` 被加入 `_loadedPaths`
2. `FindAsset<Sprite>("hero")` 因类型不匹配抛 `InvalidCastException`
3. 异常传播到调用方的 `await` 处

此时 `LoadedPaths` 中包含了 `"hero"`，但这次加载实际上失败了。调用方检查 `LoadedPaths` 会误认为加载成功。

**建议:** 将 `_loadedPaths.Add` 移到 `FindAsset` 成功返回之后：

```csharp
public UniTask<T> LoadAssetAsync<T>(string path) where T : Object
{
    var asset = FindAsset<T>(path);
    _loadedPaths.Add(path);
    return UniTask.FromResult(asset);
}
```

**影响范围:** 任何依赖 `LoadedPaths` 断言测试结果的用例，在类型不匹配场景下会得到 false positive。

---

## 🟡 建议改进

### 3. `RecordingAssetSystem` — 所有异步方法同步完成，与真实 IAssetSystem 不一致

**文件:** `Assets/Framework/TestKit/Fakes/RecordingAssetSystem.cs`

**说明:** `UniTask.FromResult` 使所有 `async` 方法同步返回，而真实 `IAssetSystem` 至少会 yield 到主线程。依赖时序的测试（例如"await 加载后某状态已变化"）在 fake 和真实实现下行为不同。

**建议:** 在类注释中明确说明同步语义：

```csharp
/// <summary>
/// IAssetSystem 的内存 fake 实现。
/// 所有异步方法均为同步完成（UniTask.FromResult），不模拟真实异步等待。
/// 仅适用于数据层单元测试，不适合测试异步时序逻辑。
/// </summary>
public sealed class RecordingAssetSystem : IAssetSystem
```

---

### 4. `CallProbe.AssertSequence` — count 检查与 CollectionAssert.AreEqual 逻辑冗余

**文件:** `Assets/Framework/TestKit/Probes/CallProbe.cs:23-34`

**说明:** 前半段手动 count 检查 + `Assert.Fail`，后半段 `CollectionAssert.AreEqual` 自身也会在失败时比较 count 和内容。两段逻辑部分重叠。当前写法不影响正确性，只是代码冗余。

**建议:** 可简化为单次 `CollectionAssert.AreEqual` 调用，其自带的信息已经足够：

```csharp
public void AssertSequence(params string[] expected)
{
    expected ??= new string[0];
    CollectionAssert.AreEqual(expected, _calls,
        $"Call sequence mismatch. Expected: [{string.Join(", ", expected)}]. Actual: [{string.Join(", ", _calls)}].");
}
```

如果想保留更明确的 count mismatch 消息，也可保持现状——这不影响正确性。

---

### 5. `RecordingEventSink` — AssertSingle / AssertEmpty 失败时不显示事件内容

**文件:** `Assets/Framework/TestKit/Probes/RecordingEventSink.cs:23-32`

**说明:** 当 `AssertSingle` 收到多个事件时，错误消息只显示 count，不列出具体事件内容，定位需要额外加断点或日志。

**建议:** 在消息中追加事件列表（依赖 `TEvent.ToString()`）：

```csharp
public TEvent AssertSingle()
{
    Assert.AreEqual(1, _events.Count,
        $"Expected 1 event, got {_events.Count}: [{string.Join(", ", _events)}].");
    return _events[0];
}
```

---

### 6. `ManualClock` / `ManualTickDriver` — 多播委托异常会导致后续订阅者丢失

**文件:**  
- `Assets/Framework/TestKit/Time/ManualClock.cs:27`
- `Assets/Framework/TestKit/Time/ManualTickDriver.cs:14-25`

**说明:** C# event 多播委托逐 subscriber 调用，中间某个 subscriber 抛异常会导致后续 subscriber 收不到事件。在测试场景中，如果 `Advanced` / `Tick` 有多个订阅者（如多个 System 同时监听时钟），某一个在回调中 NUnit 断言失败，其余订阅者就会被静默跳过。

**建议:** 可加 safe invoke（非必须——测试中通常只有一个订阅者，断言失败本就是需要立刻暴露的硬错误）：

```csharp
var handlers = Advanced;
if (handlers != null)
{
    foreach (Action<float> handler in handlers.GetInvocationList())
    {
        try { handler(deltaTime); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
```

**是否修复:** 自行判断。对于单订阅者场景不是问题。

---

### 7. `ManualTickDriver.FixedTick` — 不模拟 Unity 的 FixedUpdate 多次累积调用

**文件:** `Assets/Framework/TestKit/Time/ManualTickDriver.cs:21-24`

**说明:** Unity `FixedUpdate` 在单帧内可能被调用多次（`while (accumulated >= fixedDelta) FixedUpdate(fixedDelta)`），但 `StepFixed` 只触发一次。如果 `SystemManager` 的 FixedTick 驱动依赖这个累积逻辑，当前 API 覆盖不到。

**确认项:** 检查你 `SystemManager` 中 FixedTick 的实际驱动方式，确认单次 `StepFixed` 是否满足测试需求。

---

### 8. `TestGameObjectRoot` — `CreateChild` 未防已在 Dispose 后调用

**文件:** `Assets/Framework/TestKit/Fixtures/TestGameObjectRoot.cs:17-22`

**说明:** `using var root = new TestGameObjectRoot(); root.Dispose(); root.CreateChild("oops");` 会在已销毁的 GameObject 上挂子节点。不过实际使用中 `using` 作用域结束即不可访问，触发概率极低。

**建议:** 可选加 `_disposed` 字段守卫，优先级低。

---

## 🟢 亮点

- **asmdef 配置正确:** `autoReferenced: false` + `optionalUnityReferences: ["TestAssemblies"]` 有效隔离测试依赖，不会泄露到运行时编译
- **`TestGameObjectRoot.Dispose`:** 区分 `Application.isPlaying` 选择 `Destroy` vs `DestroyImmediate`，细节到位
- **`RecordingAssetSystem.RegisterAsset`:** 有重复注册检测，抛 `InvalidOperationException`，错误消息清晰
- **`RecordingAssetSystem.FindAsset`:** 类型不匹配时抛 `InvalidCastException` 并附带完整类型信息，定位方便
- **`ManualClock.Advance`:** `_isAdvancing` 递归守卫 + `try/finally` 保证异常安全
- **`CallProbe.AssertSequence`:** 错误消息将 expected 和 actual 都打印出来
- **`RecordingEventSink`:** 已补上 `AssertSingle` / `AssertEmpty` 便捷方法
- **命名空间:** `Framework.TestKit.*` 与项目规范一致
- **目录结构:** Assertions / Fakes / Fixtures / Probes / Time 五类清晰

---

## 优先级排序

| 优先级 | # | 问题 | 修复成本 |
|--------|---|------|----------|
| 🔴 P0 | 1 | `AssertEx` 用 `IsTrue` 替换 `That` | 1 分钟 |
| 🔴 P0 | 2 | `RecordingAssetSystem` loadedPaths 记录时机 | 2 分钟 |
| 🟡 P1 | 5 | `RecordingEventSink` 错误消息增强 | 2 分钟 |
| 🟡 P2 | 3 | `RecordingAssetSystem` 同步语义文档注释 | 1 分钟 |
| 🟡 P3 | 4 | `CallProbe` 逻辑简化 | 1 分钟 |
| 🟡 P3 | 6 | 多播委托异常保护 | 自行判断 |
| 🟡 P3 | 7 | FixedTick 累积调用 | 需确认 |
| 🟡 P3 | 8 | TestGameObjectRoot dispose 守卫 | 低优先级 |
