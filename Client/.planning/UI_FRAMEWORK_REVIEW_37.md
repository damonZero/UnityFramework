# UI 框架对照审查：KJ ↔ int_37_pack 参考项目

> 审查日期：2026-08-15
> 参考项目：`F:\int_37_pack\client`（P37，C# Unity，作者 WangXing-汪兴）
> 本项目：`G:\Mine\NewProjectK\KJ\Client`
> 结论一句话：**KJ 的 `Framework/View` 是 37 项目 `Framework/Package/View` 运行时层的忠实移植（含 Navigation/MVVM/ViewCache/Timer/DI），但缺失全部编辑器工具链（VarBind / CSharpAutoBind 代码生成 / Navigation 图编辑器）与一批应用层集成模块（事件转发、安全区适配、Coverage、Touch、UI 特效扩展）。**

---

## 0. 实施进度（2026-08-15 更新）

| 项 | 状态 | 落地位置 |
|---|---|---|
| **D0** CacheDependencies 接线（修复场景加载 NRE） | ✅ | `Scripts/Core/ViewSystem/ViewSystem.cs`（`WireViewCacheDependencies`） |
| **D1** Form 生命周期事件桥接 | ✅ | `Scripts/Core/ViewSystem/FormLifecycleEvent.cs` + `FormSubSystem.BindLifecycleEvents` |
| **D2** ScreenHelper 安全区 + 分辨率适配 | ✅ | `Scripts/Core/UI/ScreenHelper.cs` + `ViewSystem.CreateUIRoot/CreateSafeUIRoot` |
| **D4** View.Base.Editor（CSharpAutoBind + VarBind + ViewObjectEditor） | ✅ | `Framework/View/Editor/` + `Scripts/Core.Editor/ViewSystem/Binding/AutoBindingRegister.cs` |
| Demo + 测试 | ✅ 验证通过 | `Scripts/Project/Demo/DemoForm.cs`（MvvmForm）+ `ViewDemo.OpenDemoForm` + `ProjectLayerEntrypoint` 自动打开 + `Tests/EditMode/ViewFrameworkTests.cs` |
| **D7** Coverage 系统 | ✅ | `Framework/Coverage/`（运行时 + Editor）+ `Scripts/Core/ViewSystem/Coverage/FormCoverage.cs`、`SceneCoverage.cs` + `FormFullScreenJudge` 接线 |
| **D8** Touch 系统 | ✅ | `Framework/Touch/` + `SetEventSystemEnable` 接线 + 前缀表补 Touch 类型 + `ViewSystem.CreateEventSystem`（补 EventSystem + StandaloneAdvInputModule） |
| **D6** Navigation 编辑器 | ✅ | `Framework/View/Navigation.Editor/`（GraphView/Record/TreeView 全量，SafeTypePool→TypePool 适配） |
| **D9** UIEffectExtension | ⛔ 阻塞 | **依赖 URP（`Unity.RenderPipelines.Universal.Runtime`），KJ 未装 URP**，需先决策是否引入 URP |
| **D10** TransitionLoadingScreenshot | ⏸️ 游戏特定 | 依赖 37 的 `LoadingForm`（KJ 尚无），待加载界面落地后再做 |

> 附：`CoverageChecker`（依赖 UIEffectExtension）与 `SkeletonCoverageChild`（依赖 Spine）同因上层依赖缺失暂缓，已在上表 D9 一并说明。

### 本次会话最终验证结果（2026-08-16）

- ✅ **DemoForm 界面成功加载并显示**，点击交互正常（`ViewSystem` → `FormSubSystem.Open` → `LoadAssetAsync("DemoForm.prefab")` → 生命周期 → 显示 → 点击关闭）。
- ✅ Unity 编译通过、`ViewFrameworkTests` 通过。

为跑通「资源加载 + 显示 + 点击」完整链路，本次会话额外做了几处收尾/临时改动：

| 改动 | 说明 | 去向 |
|---|---|---|
| `ViewSystem.CreateEventSystem` | 补建 `EventSystem + StandaloneAdvInputModule`（KJ 无启动场景预置，UGUI 交互依赖它） | 保留 |
| `FormManager.LoadForm` 判空 + `FormSubSystem.InstantiateForm` 日志 | 资源加载失败不再被 NRE 掩盖，报「资源加载失败」 | 保留 |
| 临时 YooAsset collector | `BundleCollectorSetting.asset` 加 `GameRes` group + `AddressByFileNameAndExt`；`EnableAddressable: 0→1`；`Prepare` 构建类型 `VirtualRawBundle→VirtualAssetBundle` | 见 `.planning/YOOASSET_RESOURCE_COLLECTION.md`，正式拆多 Package |
| `Framework/Asset.Editor/AddressByFileNameAndExt.cs` | 自定义地址规则（返回文件名+扩展名），匹配 `LoadAssetAsync("DemoForm.prefab")` | 临时，正式方案用 `KJAddressByRelativePath` |

---

## 0. 复查修正（self-review，2026-08-15 追加）

对本清单二次核对代码后，发现 4 处需修正（原报告已按此更新）：

- **【严重 · 原报告遗漏】`CacheDependencies` 静态委托保留了、但从未接线。**
  `Framework/ViewCache/CacheDelegates.cs` 里 `CacheDependencies.InstantiateGameObject` / `.GetMemory` 两个静态委托**全库没有任何赋值点**。而 `GameObjectResContainer.InstanceAsync` / `ComponentResContainer.InstanceAsync` / `MemoryStatistics.AfterTake` 三处直接调用它们。
  最致命的一条链路：**`SceneSubSystem` 加载场景 → `NewSceneManager.AfterLoadScene`（第 324 行）→ `StatisticsFactory.Get<MemoryStatistics>().AfterTake` → `CacheDependencies.GetMemory(key)`（`MemoryStatistics.cs` 第 87 行，无空值守卫）→ NullReferenceException**。即**当前 KJ 只要加载场景就会空引用崩溃**。
  原报告写“KJ 改用构造注入 IAssetSystem 替代 CacheDependencies”是错的——只有 `FormSubSystem.InstantiateForm` 走了构造注入，**ViewCache 层的 `CacheDependencies` 静态委托仍是悬空未接线状态**。已列为最高优先级 D0。

- **【降级】原 D3“ObjectPool.Extension（GoPool/SafeTypePool）未对齐”被高估。**
  KJ 的 `Framework/View/Navigation` 已改用 KJ 自己的 `Framework/Pool`（`ObjectPool<T>` + `CollectionPool`，见 `NavigationFactory.cs` / `TransitionStaticRender.cs`），**Navigation 已能编译运行，GoPool/SafeTypePool 并非 View 框架的阻塞项**。D3 从“P0 运行时缺口”降级为“非阻塞、按需补齐”。

- **【修正】TMP/UGUI 其实已在 KJ manifest 中。**
  `Packages/manifest.json` 已含 `com.unity.textmeshpro: 3.0.7` 与 `com.unity.ugui: 1.0.0`，编辑器工具链（D4）**无需引入新包**（37 只是把 TMP 包进 `Assembly.Framework.Package.TextMeshPro` 自定义 asmdef）。

- **【细节】`CacheFactory.CACHER_LOG_DEBUG` 常量已沦为历史遗留。**
  KJ 里它的值改为 `"Framework.ViewCache"`，`FormManager`/`GameObjectResContainer` 已改为直接 `GameLog.Debug(..., module: CacheFactory.CACHER_LOG_DEBUG)`，该 const 仅作为 module 字符串复用，语义已与 37 的 `DebugSwitches` 日志开关解耦。

---

## 1. 背景与结论速览

37 项目的 UI 框架是一个**“轻核心 + 独立 Package + 强依赖注入”**的模块化框架（见 `Assets/Framework/README.md`）。它把 Form / Node / Scene 三种“显示单元”抽象在 `Framework/Package/View` 包内，运行时本身**零业务依赖**，所有外部能力（资源加载、日志、内存统计、事件、触摸、全屏判定）都通过**静态委托或接口注入**（`CacheDependencies` / `ObjectPoolExtensionDependencies` / `Dependencies.Scope`），由 `ScriptsC#/Core` 应用层在启动时注入。

KJ 已经移植了 37 的**整个 View 运行时核心**（`Framework/View` + `Framework/View.Navigation` + `Framework/MVVM` + `Framework/ViewCache` + `Framework/DependencyInjection` + `Framework/Timer`），并且做了符合 KJ 架构的改造：

| 37 做法 | KJ 改造 |
|---|---|
| 命名空间 `Package.*` | 命名空间 `Framework.*` |
| 日志走 `Package.DebugSwitches.DebugLog` | 日志走 `Framework.Log.GameLog`（`Utilities/Log.cs` 门面保留同名签名） |
| 资源注入靠静态委托 `CacheDependencies.InstantiateGameObject` | **仅 Form 路径**用构造注入 `IAssetSystem` + override `InstantiateForm`；**ViewCache 的 `CacheDependencies` 静态委托保留但未接线（见 D0）** |
| 生命周期靠 `AppSystems.ISystem/IAsyncSystem` + `SoftRestartField` | `[CoreSystem]` + `ISystem`/`ITickableSystem` + `SystemManager` |
| 事件桥接靠 `EventManager.Instance.Publish(EventKey<T>)` | **尚未桥接**（见差异清单 D1） |

**核心差距不在运行时，而在“编辑器工具链 + 应用层集成模块”。**

---

## 2. 37 项目 UI 框架完整模块清单（Form/Node/Scene 的依赖全景）

### 2.1 包级结构（37）

`Framework/Package/` 下与 UI 相关的独立 asmdef 包（每包一个 `Assembly.Framework.Package.Xxx.asmdef`）：

| 包 | asmdef | 说明 | 被谁依赖 |
|---|---|---|---|
| **View.Base** | `View.BaseView/` | Form/Node/Scene 基类 + 生命周期状态机 + 组件系统 + VarBind 运行时数据 | 核心，被 MVVM / Navigation 依赖 |
| **View.Navigation** | `View/Navigation/` | 导航系统（容器/加载器/过渡/渲染签名/锁） | 依赖 View.Base |
| **View.Base.Editor** | `View/BaseView.Editor/` | **VarBind + CSharpAutoBind 代码生成 + ViewObjectEditor** | 依赖 View.Base |
| **View.Navigation.Editor** | `View/Navigation.Editor/` | **GraphView 导航图编辑器 + Record 录制回放 + TreeView** | 依赖 View.Navigation |
| **MVVM** | `Package/MVVM/` | MvvmForm/Node/Scene + BaseModel/BaseViewModel | 依赖 View.Base + DI |
| **Cache** | `Package/Cache/` | View 缓存（Cache<T>/CacheFactory/FIFO/LRU/统计） | 被 View.Base 依赖 |
| **DependencyInjection** | `Package/DependencyInjection/` | `Dependencies.Scope`（VContainer LifetimeScope 静态挂载点） | 被 MVVM 依赖 |
| **ObjectPool (+Extension)** | `Package/ObjectPool/` | 通用对象池 + GoPool（GameObject 池）+ SafeTypePool | 被 View.Base / Navigation 依赖 |
| **DebugSwitches** | `Package/DebugSwitches/` | `[DebugLog]` 属性 + 编译期/运行时日志开关 | 被 View.Base / MVVM / Cache / Navigation 依赖 |
| **Timer** | `Package/Timer/` | 定时器（View 未直接引用，同级模块） | — |

### 2.2 Form/Node/Scene 运行时直接依赖（asmdef 级）

`Assembly.Framework.Package.View.Base` 的 references 只有 6 项：

```
UniTask, ZString,
Assembly.Framework.Package.Cache,      // View 缓存
Assembly.Framework.Package.ObjectPool, // 对象池
Assembly.Framework.Package.DebugSwitches, // 调试日志开关
UnityEngine.UI                         // UGUI
```

`View.Navigation` 额外再依赖：`ObjectPool.Extension`（GoPool/SafeTypePool）。

`MVVM` 额外再依赖：`VContainer`、`R3.Unity`、`DependencyInjection`。

### 2.3 运行时 DI 委托注入点（37 关键设计）

View 包“脱离项目环境仍能工作”的关键，是三个静态委托/挂载点，由应用层注入：

| 注入点 | 位置 | 注入内容 | 注入者 |
|---|---|---|---|
| `Dependencies.Scope` | `Package.DependencyInjection.Dependencies` | VContainer `LifetimeScope`（全局容器） | `ScriptsC#/Core/IocSystem/IocModule.cs` |
| `CacheDependencies.InstantiateGameObject` / `.GetMemory` | `Package.Cache.CacheDelegates` | 资源异步实例化 + 资源内存统计 | `ScriptsC#/Core/CacheSystem/CacheSystem.cs` |
| `ObjectPoolExtensionDependencies.LoadAssetAsync` / `.ReleaseAsset` | `Package.ObjectPool.Extension` | GameObject 池的资源加载/释放 | `CacheSystem.cs` |

### 2.4 应用层集成模块（ScriptsC#/Core，Form/Node/Scene 真正跑起来的“胶水”）

| 模块 | 位置 | 作用 | 被 View 哪里用到 |
|---|---|---|---|
| **ViewSystem** | `Core/ViewSystem/ViewSystem.cs` | 编排 Form/Scene/Navigation 三子系统 | 总入口 |
| **FormSubSystem** | `Core/ViewSystem/Form/` | 继承 `FormManager`，注入 `InstantiateForm` + 事件转发 | `ScreenHelper.SafeUIRoot`、`AssetUtil.InstantiateAsync`、`EventManager` |
| **SceneSubSystem** | `Core/ViewSystem/Scene/` | 继承 `SceneManager` | 场景加载注入 |
| **NavigationSubSystem** | `Core/ViewSystem/Navigation/` | 继承 `NavigationManager` | `Coverage`(全屏判定)、`Touch`(EventSystem)、`TransitionLoadingScreenshot` |
| **CacheSystem** | `Core/CacheSystem/` | 注入 CacheDependencies / ObjectPoolExtensionDependencies | 资源/对象池桥接 |
| **EventManager** | 全局事件总线 | `EventKey<T>` + `Publish` | FormSubSystem 把 Form 生命周期转发为全局事件 |
| **ScreenHelper** | 屏幕适配 | `SafeUIRoot`（安全区）、`AdaptResolution`（分辨率适配） | Form 打开时适配 |
| **Coverage** | `Package/Coverage/` | 界面覆盖区域检测（SegmentTree 等） | Navigation 的 `FormFullScreenJudge`（判断界面是否全屏以决定导航渲染优化） |
| **Touch** | `Package/Touch/` | 自定义输入模块（StandaloneAdvInputModule 等） | Navigation 的 `SetEventSystemEnable` |
| **TransitionLoadingScreenshot** | 游戏侧过渡 | 导航切换时的截图 Loading 过渡 | `TransitionFactory.Create<TransitionLoadingScreenshot>` |
| **SoftRestartField / Boot.GameLife** | 软重启 | `[SoftRestartField]` 标记字段软重启时重置 | ViewSystem 静态字段 |
| **模块表 codegen** | `#Generated/ViewSystemTable.api.cs` | `AppInjectInfoTool` 生成 DI 注册表 | `GetInjectable<T>` 解析（MVVM 的 VM/Model 注入） |

---

## 3. 模块对照表（37 → KJ，逐模块）

图例：✅ 已移植　🟡 部分/有差异　❌ 缺失　🔁 用不同方案替代

### 3.1 View 运行时核心

| 37 模块 / 文件 | KJ 对应 | 状态 | 备注 |
|---|---|---|---|
| `View/BaseView/Base/ViewBase.cs` | `Framework/View/Base/ViewBase.cs` | ✅ | 生命周期状态机（None→Opened→Shown/Hidden→Closed）、FIFO 槽、VisibleController 全量移植；日志改 `GameLog`，异常改传原始 Exception（符合 KJ 规范） |
| `Base/ViewObject.cs` | `Framework/View/Base/ViewObject.cs` | ✅ | 组件系统 + `bindData` + `GetBindField<T>` + AsyncDelay |
| `Base/ViewLifeCycle.cs` / `Events` / `Phase` / `IViewManager` / `IViewOptions` | 同名 | ✅ | — |
| `Base/Visible/*`（IVisibleStrategy/VisibleController） | 同名 | ✅ | — |
| `Component/*`（IView*Component + AsyncDelayComponent） | 同名 | ✅ | 8 个组件接口全量 |
| `Form/BaseForm.cs` / `FormManager.cs` / `FormOptions.cs` / `FormResContainer.cs` / `Visible/*` | 同名 | ✅ | 层级管理、`Cache<BaseForm>` 全量 |
| `Node/INode.cs` | `Framework/View/Node/INode.cs` | ✅ | 标记接口（供 VarBind 判断） |
| `Scene/BaseScene.cs` / `ISceneManager.cs` / `NewSceneManager.cs` / `SceneCacheStrategy.cs` / `Visible/*` | 同名 | ✅ | 类名仍为 `SceneManager` |
| `Scene/Tests/CollectCamerasBenchmark.cs` | — | ❌ | 性能基准测试，可忽略 |
| `Utilities/Algorithm.cs` / `Log.cs` | 同名 | 🔁 | `Log.cs` 改为转发 `GameLog` |
| `VarBind/SerializationDictionary.cs` / `VarBindData.cs` | 同名 | ✅ | **运行时数据已移植，但编辑器工具缺失（见 D4）** |

### 3.2 View.Navigation 运行时

| 37 | KJ | 状态 |
|---|---|---|
| `Navigation/NavigationManager.cs` | `Framework/View/Navigation/NavigationManager.cs` | ✅ |
| `Behaviour/*`（NavigationBehaviour/Factory/Container/Loader/Cached/TraversalOrder + Loader 3 类） | 同名全量 | ✅ |
| `Interfaces/*`（5 接口） | 同名 | ✅ |
| `Options/*`（NavigationMode/NavigateFormOptions/NavigateSceneOptions） | 同名 | ✅ |
| `State/*`（ClearType/LockType/StateType） | 同名 | ✅ |
| `RenderingSignature/*`（4 文件） | 同名 | ✅ |
| `Transition/*`（ITransition/Base/Composite/Factory/NoOp/StaticRender/ViewComponent） | 同名 | ✅ |
| `Exception/*`（NavigationException/ExceptionMgr） | 同名 | ✅ |
| `Utilities/*`（Log/NavigateUtils/NavigationEvent/NavigationMemory） | 同名 | ✅ |

### 3.3 MVVM

| 37 | KJ | 状态 |
|---|---|---|
| `MVVM/Dependencies.cs`（VContainer Scope/Resolver 门面） | `Framework/MVVM/Dependencies.cs` | ✅ 完全一致 |
| `Interface/ICompositeDisposable.cs` | 同名 | ✅ |
| `Model/BaseModel.cs` / `MvvmBaseModel.cs` / `IAutoInjectModel.cs` | 同名 | ✅ |
| `ViewModel/BaseViewModel.cs` / `IAutoInjectVm.cs` | 同名 | ✅ |
| `View/IMvvmView.cs` / `MvvmForm.cs`(+Injectable) / `MvvmNode.cs`(+Injectable) / `MvvmScene.cs`(+Injectable) | 同名全量 | ✅ 完全一致（R3 CompositeDisposable 三档清理 + `CreateContainer` + `GetInjectable<T>`） |

### 3.4 支撑包

| 37 包 | KJ 对应 | 状态 | 备注 |
|---|---|---|---|
| `Cache`（ViewCache：Cache/CacheFactory/CacheDelegates/FIFO/LRU/ResContainer/Statistics） | `Framework/ViewCache/` | 🟡 | 运行时全量移植，**但 `CacheDependencies.InstantiateGameObject`/`GetMemory` 静态委托从未赋值（见 D0）** |
| `DependencyInjection`（Dependencies.Scope） | `Framework/DependencyInjection/` | ✅ | 完全一致 |
| `Timer` | `Framework/Timer/` + `Core/Timer/TimerSystem` | ✅ | 37 引用 UniTask；KJ 零依赖 + noEngineReferences |
| `ObjectPool`（CollectionPool/ObjectPool/PooledCollection） | `Framework/Pool/` | ✅ | `CollectionPool`/`ObjectPool`/`SingleThreadObjectPool`/`PooledCollections` 全量 |
| `ObjectPool.Extension`（GoPool：GameObjectPool/MultiAsset/SingleAsset/ComponentContainer + SafeTypePool） | `Framework/Pool/Unity/GameObjectPool.cs` + `Pool/Types/TypePool.cs` | 🟡 | **Navigation 已改用 KJ 的 `ObjectPool<T>`/`CollectionPool`，非阻塞**；GoPool 完整容器与 SafeTypePool 若业务需要再补齐 |
| `DebugSwitches`（[DebugLog] + 树形运行时开关） | — | 🔁 | KJ 用 `Framework.Log.GameLog`（编译期 `[Conditional]` + 模块树过滤），**功能对等但无独立的 DebugSwitches 包** |

### 3.5 应用层集成（ScriptsC#/Core）

| 37 模块 | KJ 对应 | 状态 |
|---|---|---|
| `Core/ViewSystem/ViewSystem.cs` | `Scripts/Core/ViewSystem/ViewSystem.cs` | 🔁 改造为 `[CoreSystem]` + 构造注入 IAssetSystem，创建 UIRoot |
| `FormSubSystem` | `FormSubSystem.cs` | 🟡 注入 `InstantiateForm` 已有；**缺事件转发 + ScreenHelper.AdaptResolution** |
| `SceneSubSystem` | `SceneSubSystem.cs` | ✅ |
| `NavigationSubSystem` | `NavigationSubSystem.cs` | 🟡 有常驻容器；**FormFullScreenJudge 恒 false（缺 Coverage）、SetEventSystemEnable 直接切 EventSystem（缺 Touch）** |
| `CacheSystem`（注入 CacheDependencies） | 无独立类 | ❌ **未接线：KJ 无对应注入点，`CacheDependencies` 静态委托悬空（见 D0）** |
| `EventManager`（EventKey<T> 全局事件） | `Framework/Event/`（GameEventAttribute + MessagePipe） | 🟡 **事件系统有，但 FormSubSystem 未把 Form 生命周期转发到事件总线** |
| `ScreenHelper`（SafeUIRoot/AdaptResolution） | — | ❌ **完全缺失**（KJ 的 UIRoot 是裸 Canvas，无安全区/分辨率适配） |
| `Coverage` | — | ❌ 缺失 |
| `Touch` | — | ❌ 缺失（KJ 直接操作 EventSystem） |
| `TransitionLoadingScreenshot` | `TransitionFactory.None` | 🟡 占位（游戏侧过渡未实现） |
| `SoftRestartField` / `Boot.GameLife` | KJ Boot 自有生命周期 | 🔁 方案不同 |
| 模块表 codegen（`#Generated/ViewSystemTable.api.cs` / AppInjectInfoTool） | `Core/CoreTypeRegistration`（反射扫描） | 🔁 方案不同（反射 vs 生成） |

---

## 4. 差异清单（按优先级）

### 🔴 P0 — 运行时缺口（不补则 View 不完整，甚至崩溃）

- **D0. `CacheDependencies` 静态委托未接线 —— 加载场景即空引用崩溃（最优先）**
  `Framework/ViewCache/CacheDelegates.cs` 的 `CacheDependencies.InstantiateGameObject` / `.GetMemory` 全库无赋值点，但被 3 处调用：
  1. `NewSceneManager.AfterLoadScene`（第 324 行）→ `MemoryStatistics.AfterTake`（第 87 行）→ `CacheDependencies.GetMemory(key)` **每次场景加载都会触发 NRE**；
  2. `GameObjectResContainer.InstanceAsync`（第 30 行）→ `CacheDependencies.InstantiateGameObject`，一般 GameObject 缓存 miss 时 NRE；
  3. `ComponentResContainer.InstanceAsync`（第 37 行）同上。
  - 方案（二选一，推荐 A）：
    - **A. 构造注入重构**：把 `CacheDependencies` 静态委托改为 ViewCache 的 DI 注入（`ICacheAssetBridge` 接口，由 `CoreContainerRegistration` 绑定到 `IAssetSystem`），与 KJ 其余框架一致。
    - **B. 最小接线**：在 `CoreLayerEntrypoint`（已设 `Dependencies.Scope` 处）给 `CacheDependencies.InstantiateGameObject` / `GetMemory` 赋值（`InstantiateGameObject = (n,p) => assetSystem.LoadAssetAsync<GameObject>(n).ContinueWith(...)`；`GetMemory` 先用 YooAsset 内存估算或返回 0）。
  - 无论哪种，**场景加载冒烟测试必须通过**（当前必崩）。

- **D1. Form 生命周期事件未桥接到全局事件总线**
  37：`FormSubSystem` 监听 `FormManager` 的 14 个 C# 事件 → `EventManager.Instance.Publish(EventKey<T>)`，业务层用 `EventKey<BaseForm>` 订阅（如 `formPostOpen`）。KJ 的 `FormSubSystem` 只有 `FindForms` 等查询，**没有事件转发**，业务无法订阅 Form 打开/关闭等全局事件。
  - 方案：在 `FormSubSystem.Init` 里把 `FormPreOpen/PostOpen/.../FormRenderingChanged` 转发为 `Framework.Event` 的 `GameEvent`（struct + `[GameEvent]`），或提供一个 `FormEventBridge` 静态订阅。

- **D2. ScreenHelper 缺失（安全区 + 分辨率适配）**
  37：`ScreenHelper.SafeUIRoot`（刘海屏安全区）与 `ScreenHelper.AdaptResolution(rectTransform)`（各分辨率适配），在 `InstantiateForm` 时对每个 Form 调用。KJ 的 `ViewSystem.CreateUIRoot` 只创建裸 `Canvas+CanvasScaler+GraphicRaycaster`，无安全区处理。
  - 方案：Core 层新增 `ScreenHelper`（安全区计算 + 分辨率适配策略），`FormSubSystem.InstantiateForm` 后调用。

- **D3. ObjectPool.Extension（GoPool/SafeTypePool）未对齐**
  Navigation 运行时依赖 `ObjectPool.Extension`（GameObject 池 + SafeTypePool）。KJ 有 `Pool/Unity/GameObjectPool.cs` 和 `Pool/Types/TypePool.cs` 雏形，但缺 MultiAsset/SingleAsset/Component 容器与 SafeTypePool 完整实现，需核对 Navigation 里 `UnityObjectPool` 等类型是否已能用。

### 🟠 P1 — 编辑器工具链（37 最大的、KJ 完全缺失的部分）

- **D4. View.Base.Editor 全套缺失（VarBind + CSharpAutoBind + ViewObjectEditor）**
  这是 37 UI 框架的**核心生产力工具**：
  - **CSharpAutoBind**：遍历子节点，按命名前缀（`_go`→GameObject、`_img`→Image、`_tf`→Text、`_rt`→RectTransform、`_txt`→TMP_Text 等）自动生成 `Xxx.Binding.cs` 的 `partial class` + `[SerializeField]` 字段，并把引用序列化注入（`[DidReloadScripts]` 二次注入）。依赖 `VarBaseBind.PrefixTypeDict`、`ZString`、`PrefabStageUtility`、`PrefabUtility`。
  - **VarBind**：`VarBindType`（Scene/PrefabInScene/Prefab）分发到 `VarPrefabBind`/`VarSceneBind`/`VarPrefabInSceneBind` 三个处理器，把绑定写入 `ViewObject.bindData`（`SerializationDictionary<string, VarBindData>`），运行时用 `GetBindField<T>(name)` 取。
  - **ViewObjectEditor**：Inspector 可视化。
  - 依赖：`UnityEditor.UI`、`TextMeshPro`（GUID `650a869…` = `Assembly.Framework.Package.TextMeshPro`）。
  - **注意**：KJ 已移植 `VarBindData`/`SerializationDictionary` 运行时与 `INode` 标记接口，说明**运行时已为绑定就绪，只缺编辑器工具与 codegen**。

- **D5. DebugSwitches 依赖（可替代）**
  37 的 View 运行时日志走 `DebugSwitches.DebugLog`（`[DebugLog]` 属性 + 树形运行时开关 + Editor 面板）。KJ 已用 `GameLog` 替代（`Utilities/Log.cs` 门面），**运行时已无依赖**。但若移植 Editor 工具（D4），`VarBind`/`CSharpAutoBind` 里对 `DebugSwitches` 的调用需替换为 `GameLog` 或补一个最小 `DebugSwitches` 兼容层。建议直接用 `GameLog`，不引入 DebugSwitches 包。

### 🟡 P2 — Navigation 编辑器 + 运行时集成优化

- **D6. View.Navigation.Editor 全套缺失**
  - GraphView：`NavigationWindow` + `NavigationTotalView/NodeView/LoaderNodeView/ContainerNodeView` + `NavigationViewKit`，用 UIElements GraphView 可视化导航容器树。
  - Record：`NavigationRecordMgr` + `EditorNavigationFormLoader/SceneLoader` + `RecordStackWindow`，录制导航操作序列用于回放调试。
  - TreeView：`NavigationTreeWindow` + `NavigationTreeShowContent/GroupShow/LoaderShow/Width`，树形浏览导航结构。
  - 依赖：`Unity.EditorCoroutines.Editor`、`UnityEditor.UI`、`UnityEngine.UI`、`ObjectPool.Extension`。

- **D7. Coverage 系统缺失**
  37 `Package/Coverage`（`UICoverageArea`/`CanvasCoverage`/`CameraCoverage` + `SegmentTree`/`IntRect`）用于检测界面是否铺满屏幕，供 Navigation 的 `FormFullScreenJudge` 判断是否做渲染优化。KJ 的 `NavigationSubSystem.FormFullScreenJudge` 恒返回 `false`。

- **D8. Touch 系统缺失**
  37 `Package/Touch`（`StandaloneAdvInputModule` + `BaseTrigger/BaseButton/BaseDrag/PassTrigger` 等）统一管理输入与 EventSystem 开关。KJ 的 `SetEventSystemEnable` 直接 `EventSystem.current.enabled = enable`。

### 🟢 P3 — UI 特效扩展 + 游戏级能力

- **D9. UIEffectExtension 缺失**
  `EffectImage`（顶点特效）、`FrameAnimation`（帧动画）、`ImageBlur`（模糊）、`MaskImg`/`MaskProgressBar`（遮罩/进度遮罩）、`UIModelImage`（UI 内嵌 3D 模型渲染：`UIModelImg`/`UIModelCam`/`UIModelLocMgr`/`UIModelScreenFitting`）。含 `UIEffectDependencyBridge`（依赖桥接）。

- **D10. 过渡截图（TransitionLoadingScreenshot）占位**
  37 用 `TransitionFactory.Create<TransitionLoadingScreenshot>` 实现导航切换的截图 Loading 过渡；KJ 用 `TransitionFactory.None`。

---

## 5. 开发计划

> 原则：先补齐运行时缺口让 View 真正可用，再补编辑器工具链（投入最大），最后补特效与游戏级能力。每阶段产出可独立验证。

### Phase 1（P0）——运行时完整性补齐（含崩溃修复）
0. **接线 `CacheDependencies`（D0，最高优先）**：按 D0 方案 A（构造注入 `ICacheAssetBridge`）或 B（`CoreLayerEntrypoint` 最小赋值），修复场景加载 NRE。冒烟：`SceneSubSystem` 加载任意场景不崩溃。
1. **Form 事件桥接**：`FormSubSystem.Init` 中把 14 个 Form 生命周期事件转发为 `Framework.Event` 的 `[GameEvent]` struct（或 `GameEvent` 泛型），提供全局订阅。复用 `Framework/Event/GameEventAttribute.cs` 与 `GameEventTypeScanner.cs`。
2. **ScreenHelper**：Core 新增安全区 + 分辨率适配，`ViewSystem.CreateUIRoot` 应用安全区，`FormSubSystem.InstantiateForm` 后 `AdaptResolution`。
3. **（降级，按需）ObjectPool.Extension 补齐**：Navigation 已可用；仅当业务需要 GoPool 完整容器/SafeTypePool 时再补，复用 `Framework/Pool/Unity/GameObjectPool.cs` 扩展。

验证：Editor 编译通过；EditMode 测试打开一个 Form 能收到 `formPostOpen` 事件；不同分辨率/刘海屏下 UI 不被遮挡。

### Phase 2（P1）——编辑器工具链（工作量最大）
4. **CSharpAutoBind 代码生成**：移植 `CSharpAutoBinding.cs` + `CSharpAutoBindingConfig.cs` + `IAutoBindingRegister.cs`，前缀类型表改为 KJ 命名空间（去掉 `Package.URPSceneEffect.PlanarShadow` 等 37 特有项）。生成 `.Binding.cs` 的 partial + `[SerializeField]`。
5. **VarBind 编辑器**：移植 `VarBaseBind`/`VarBind`/`VarPrefabBind`/`VarSceneBind`/`VarPrefabInSceneBind`/`VarBindTool`/`VaryTextAliasInitializer`/`ViewObjectEditor`。依赖的 `DebugSwitches.DebugLog` 全部替换为 `GameLog`。
6. **asmdef**：新建 `Framework.View.Editor`（含 `includePlatforms: ["Editor"]`），references 加 `UnityEditor.UI`、`UnityEngine.UI`、`Unity.TextMeshPro`（TMP 已在 `manifest.json`：`com.unity.textmeshpro 3.0.7`，直接引用即可，无需新包）。
7. **配置资产**：创建 `CSharpAutoBindingConfig.asset`，登记 KJ 常用前缀映射（`_go/_img/_tf/_txt/_rt/_btn/_scroll` 等）。

验证：在 Editor 中对一个带命名前缀子节点的 Form 预制体执行“自动绑定”，生成 `.Binding.cs` 且字段序列化注入成功；`GetBindField<T>` 运行时取到正确引用。

### Phase 3（P2）——Navigation 编辑器 + 运行时集成
8. **Coverage**：移植最小可用的 `Coverage` 核心（`BaseCoverage`/`CanvasCoverage`/`UICoverageArea`/`SegmentTree`/`IntRect`），实现 `FormFullScreenJudge` 真实判定。
9. **Touch**：若项目需要自定义输入（拖拽/穿透/长按），移植 `Package/Touch`；否则至少把 `SetEventSystemEnable` 收敛到统一开关。
10. **Navigation 编辑器**：按需移植 GraphView / Record / TreeView（工作量大，建议先用 GraphView 做容器可视化，Record 回放后置）。

验证：导航切换时全屏界面能正确触发渲染优化；导航图编辑器能展示容器树。

### Phase 4（P3）——UI 特效扩展 + 游戏级能力
11. **UIEffectExtension**：按需移植（优先 `FrameAnimation`/`ImageBlur`/`MaskProgressBar` 这类常用项；`UIModelImage` 视项目是否需要 UI 内嵌 3D 模型）。
12. **过渡截图**：实现 `TransitionLoadingScreenshot`（导航切换截图 Loading），替换 `TransitionFactory.None`。

验证：各特效组件在 EditMode/PlayMode 下表现正常，无内存泄漏。

---

## 6. 关键文件对照（快速索引）

| 作用 | 37 | KJ |
|---|---|---|
| View 基类状态机 | `Framework/Package/View/BaseView/Base/ViewBase.cs` | `Assets/Framework/View/Base/ViewBase.cs` |
| Form 管理器 | `.../BaseView/Form/FormManager.cs` | `Assets/Framework/View/Form/FormManager.cs` |
| Scene 管理器 | `.../BaseView/Scene/NewSceneManager.cs`（类名 `SceneManager`） | `Assets/Framework/View/Scene/NewSceneManager.cs` |
| 导航管理器 | `.../Navigation/NavigationManager.cs` | `Assets/Framework/View/Navigation/NavigationManager.cs` |
| View 缓存 | `Package/Cache/Cache.cs` + `CacheDelegates.cs` | `Assets/Framework/ViewCache/Cache.cs` + `CacheDelegates.cs` |
| MVVM Form | `Package/MVVM/View/MvvmForm.cs` | `Assets/Framework/MVVM/View/MvvmForm.cs` |
| DI 挂载点 | `Package/DependencyInjection/Dependencies.cs` | `Assets/Framework/DependencyInjection/Dependencies.cs` |
| 编辑器 codegen（缺） | `Package/View/BaseView.Editor/CSharpAutoBind/CSharpAutoBinding.cs` | ❌ |
| VarBind 编辑器（缺） | `Package/View/BaseView.Editor/VarBind/*.cs` | ❌ |
| Navigation 编辑器（缺） | `Package/View/Navigation.Editor/*` | ❌ |
| 应用层绑定 | `ScriptsC#/Core/ViewSystem/*` + `CacheSystem` | `Assets/Scripts/Core/ViewSystem/*` |

---

## 7. 说明与边界

- 本审查聚焦 **UI 框架（Form/Node/Scene 及其依赖）**，不覆盖 37 的完整游戏业务（网络、战斗、角色等）。
- 37 的 `Timer`、`Network`、`SQLite`、`UniversalStateMachine` 等是同级 Package，**View 并未直接依赖**（`View.Base` 的 asmdef 只有 6 项 references），故未展开；其中 `Timer` KJ 已另行实现。
- 移植时注意 37 命名空间 `Package.*` → KJ `Framework.*`，以及 `Package.URPSceneEffect.PlanarShadow` 等 37 特有类型在 CSharpAutoBind 前缀表里的清理。
