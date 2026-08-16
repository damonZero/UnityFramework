# 软重启静态变量重置规范（约定 + 强制检查）

> 2026-08-16 | 落地：`Framework.Restart.StaticReset` + `Boot.GameLife.GameRestart` + `Tests/EditMode/StaticResetContractTest`

## 背景

软重启（`GameRestart.SoftRestart`）会反射扫描所有 KJ 热更游戏层（命名空间 `Framework.` / `Core` / `General` / `Project`），把「可变 static 字段」重置为 `default`。这解决了「静态单例 / 缓存跨重启泄漏」的问题，但也带来一个陷阱：**字段声明时的非默认初始值会被丢成 default**。

```csharp
static bool N = true;   // 软重启后 N 变 false（default），而不是回到 true —— 静默 BUG！
```

## 核心约定（写静态字段时必须遵守）

| 写法 | 语义 | 软重启行为 | 是否需标注 |
|---|---|---|---|
| `const` | 编译期常量 | 自动跳过（`IsLiteral`） | 否 |
| `static readonly` | 不变值 / 基础设施（池、锁、固定配置） | 自动跳过（`IsInitOnly`） | 否 |
| 可变 `static`（默认初始值） | 可变状态 | 重置为 `default` | 否 |
| `static X N = <非默认>` | **坏味道** | 重置为 default → 错 | 必须改 |

## 规则

1. **不变值用 `const` 或 `static readonly`**，绝不用可变 `static` 存常量：
   - `const bool N = true` ✅　`static readonly bool N = true` ✅　`static bool N = true` ❌
2. **可变 static 字段的声明初始值必须是 default**（null / 0 / false / 空集合）。
   - 需要非默认起始值时，用 `[SoftRestartField(initialValue: x)]`（**标目标值而非动作**，仅限编译期常量，如 `initialValue: true`）。
3. **必须跨重启保留的可变 static**（罕见），用 `[SoftRestartField(SoftRestartAction.DoNotReset)]`。
4. **Tier0 程序集**（如 `Framework.Log`）**不能标 `[SoftRestartField]`**（Tier0→Tier0 违反 R4 红线），改用**惰性初始化**：
   ```csharp
   private static GameLogProfile _profile;
   public static GameLogProfile Profile => _profile ??= CreateDefaultProfile();
   ```
5. 引用类型「`= new Xxx()` 的对象池 / 固定注册表」直接改 `static readonly`（`static readonly List<T> _pool = new()`）——引用固定、内容可变，跨重启保留无害。

## 强制检查

`Tests/EditMode/StaticResetContractTest.MutableStaticFields_ShouldNotHaveNonDefaultInitializers` 反射扫描热更程序集，把「可变 static 带非默认初始值、且未标 `[SoftRestartField]`」打印到测试输出。新增/改动静态字段后跑一下该测试即可发现遗漏（当前因 Tier0 存量字段软化为「打印不阻塞」，收敛完可改回 `Assert.IsEmpty` 恢复强制）。

## 相关

- 重置器：`Framework.Restart.StaticReset`（`Reset(Type)` 可单测 / `ResetAll()` 全量）。
- 特性：`Framework.Restart.SoftRestartFieldAttribute` / `SoftRestartAction` / `SoftRestartClassAttribute`。
- 软重启入口：`Boot.GameLife.GameRestart`（`SoftRestart` / `HardRestart`）。
- 软重启销毁顺序铁律：**先删 prefab（OnDisable/OnDestroy 时系统仍存活）→ 同步释放 Core scope → 最后重置静态**；`CoreStartup.Reset()` 必须 `scope.Dispose()` 同步释放（不能只 `Object.Destroy`，否则旧系统仍在 Tick 读空静态会 NRE）。
- **DI scope 必须留在 `DontDestroyOnLoad` 持久层**（`CoreLifetimeScope` 由 `CoreStartup.Start` 显式挂；`GeneralLifetimeScope`/`ProjectLifetimeScope` 经 `CreateChild` 的 `SetParent` 继承）。不要因为「反正软重启会拆」就改放到场景上，两个原因：
  1. **跨场景存活**：框架有场景加载（`BaseScene`/`NewSceneManager`/`LoadSceneAsync`），正常切游戏场景不能把 DI 容器一起卸载（VContainer 的 `LifetimeScope` 不会自动 DontDestroyOnLoad）。
  2. **软重启两级拆除**：`GameRestart.DestroyNonPersistentRoots()` 靠 `obj.scene == bootComponent.scene` **跳过 DontDestroyOnLoad 场景**，让 scope 走 `ResetCoreScope` → `CoreStartup.Reset` 的**同步 `Dispose()`**（先于静态重置）。若放场景上，会被 sweep 当普通根对象 `Object.Destroy`（帧末才 OnDestroy→Dispose，落在静态重置之后），破坏上面的销毁顺序铁律。
