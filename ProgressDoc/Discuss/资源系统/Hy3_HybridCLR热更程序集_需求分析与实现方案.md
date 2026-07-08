# HybridCLR 热更程序集范围扩展——需求分析 + 可执行实现方案（最佳合并版）

> 文档性质：需求分析 + 架构设计 + 可执行实现方案
> 创建日期：2026-07-06 · 版本：v2（合并 `DS_HybridCLR热更程序集范围扩展完整设计.md` 与本 agent 首稿，全部结论已对照真实代码核实）
> 前置分析：`ProgressDoc/Discuss/HybridCLR热更程序集范围分析.md`
> 关联规范：`.planning/HOT_UPDATE_BOUNDARY.md`、`.planning/ROADMAP.md` HYB-03
> 命名约定：本 agent 产物统一以 `Hy3_` 前缀命名；本文档为**唯一权威版本**，取代 `DS_*` 原稿与本 agent 首稿。
>
> **⚠️ 状态对齐（2026-07-08 补）**：✅ 本文设计已全部落地并验证。HYB-03 已实现：AOT 壳 `Launcher` + 热更 `Boot`，共 10 个热更程序集。EditMode 测试全工程 **45/45 全绿**，其中 15 例覆盖 HYB-03 边界（含 3 例核心边界锁死测试）。关键偏差见文末「附录 E：实际实现偏差」。AOT 壳 `Launcher` 与热更 `Boot` 的 asmdef 引用以工程内 `.asmdef` 为准。当前权威实现说明以 `CODEMAP.md` 的 **Framework: Launcher + Boot** 章节为准。

---

## 〇、执行摘要（最佳版本结论）

1. **采用两步法**（与 DS 一致，确认合理）：
   - **挡 1（低风险，立即可做）**：`Pool / Cache / Event` 三个 Framework 程序集入热更。纯配置 + 工具链改动（2 文件），AOT 面 14→11。
   - **挡 2 / HYB-03（中风险，建议先过底层稳定性 gate）**：`Boot` 拆分为 `Launcher(AOT)` + `Boot(热更)`，`Asset / Log / RuntimeLog` 随之入热更。最终热更 10 个程序集，AOT 壳仅 `Launcher + AssetShared + 第三方 + TestKit`。

2. **本版本相对 DS 原稿的 5 处修正均已对照真实代码核实成立**（详见第七节）：
   - **F1（命名）**：`AssetConfig + AssetConstants` 迁出 `Framework.Asset` 时，放入 **`Framework.AssetShared`**（AOT 共享程序集，namespace 保留 `Framework.Asset`）——与项目 `Framework.*` 命名规范一致；本 agent 首稿使用的 `KJ.Asset.Config` 命名不合规，已统一为 `Framework.AssetShared`。
   - **F2（去过度设计）**：只提取 `IRemoteService`（`BootRemoteService`）。DS 原稿额外提取的 `IBuildinQueryServices` / `IDecryptionServices` 在当前代码中**根本不存在自定义实现**（YooAsset 用默认），属过度设计，已删除。
   - **F3（拦截名单分阶段演进，修正 DS §4.2 的 bug）**：DS §4.2 的代码把 `Asset`/`Log` 从拦截里删掉了，却在其注释里声称“保留 Asset/Log 拦截”，自相矛盾且会错误地放行 Asset/Log。本文给出**逐挡准确**的 switch 内容。
   - **F4（BootLoader 加载职责）**：AOT 壳必须用 YooAsset 原生 API（`package.LoadAssetSync<RawFileObject>`）读取 RawFile，**不能**走热更层的 `IAssetRuntime.LoadRawBytes`；必须加载**全部**热更 DLL（含 Boot 自身）；`skipHotUpdateInEditor` 时跳过 `Assembly.Load` 但仍反射 `BootUpdateRunner.Start`。
   - **F5（Entry 去 Framework 依赖）**：`Entry` 迁入 `Launcher`(AOT) 后必须彻底删除 `Framework.Log`/`Framework.RuntimeLog` 引用，错误走 AOT 的 `BootStartupLog`。

3. **DS 原稿两处计数/事实错误已修正**：最终热更程序集为 **10 个**（非 11）；`ValidateRuntimePreloadAssemblyName` 当前**不含** `RuntimeLog`。

---

## 一、需求分析

### 1.1 现状问题（与 DS 一致，保留）

当前 `ProjectSettings/HybridCLRSettings.asset` 仅 3 个热更程序集：

```yaml
# 当前
hotUpdateAssemblies: [Core, General, Project]
```

Framework 层 6 个（Pool / Cache / Event / Asset / Log / RuntimeLog）与 Boot 全部编入 AOT。这并非架构决策，而是 Phase 0 的保守默认，带来三个实际问题：

1. **框架 bug 修复被迫换包**：ObjectPool 并发 bug、Cache LRU 策略优化等，改一行就要走商店审核。
2. **Boot 启动逻辑锁死在 AOT**：资源版本检查、下载重试、更新 UI 交互——上线后最高频迭代的内容全部不可热更。
3. **AOT 面过大违背热更初衷**：IL2CPP 编译进 Native 的代码永远无法替换。

> 历史教训（来自 `CODE_MAP.md`）：37 项目 Boot 层积累到 38 个文件全部 AOT，更新流程高度耦合却无法热更，每次调整极痛苦。

### 1.2 设计原则

> **除了确实无法热更的原生代码，其余全部可热更。**

| 类别 | 热更？ | 理由 |
|------|--------|------|
| KJ 自有 C#（Framework / Boot / Core / General / Project） | ✅ | 纯托管代码，HybridCLR 支持 |
| YooAsset / HybridCLR.Runtime | ❌ | C++ Native |
| Unity Engine API | ❌ | C++ Native |
| VContainer / UniTask / MessagePipe / ZLogger / ZLinq / ZString | ❌ | 业务判断：极稳定第三方库，AOT 效率更高 |
| TestKit | ❌ | 非产品代码 |

### 1.3 分挡策略

| | 挡 1 | 挡 2（HYB-03） |
|---|---|---|
| **内容** | Pool / Cache / Event 入热更 | Boot 拆 Launcher(AOT)+Boot(热更)，Asset/Log/RuntimeLog 入热更 |
| **改动面** | 2 文件，~5 行 | ~12 新文件 + ~8 修改 + 迁移 |
| **为何可/必须分开** | 三者不被 Boot 引用，入热更无阻力 | 三者被 Boot 直接引用，须先拆 Boot 才能移动 |
| **风险** | 极低（纯配置） | 中等（程序集拆分 + 启动链改造） |
| **Gate** | Editor Play 通过即可 | Editor Play + HybridCLR Generate 验证 |

---

## 二、现状核实（全部对照真实代码，非假设）

### 2.1 程序集依赖关系（核实）

```
AOT (IL2CPP)                                          HotUpdate (HybridCLR)
═════════════                                          ═════════════
Boot.asmdef                                             Core.asmdef
  引用: Asset, Log, RuntimeLog, UniTask,                  引用: Asset, Event, Pool, Cache,
        HybridCLR.Runtime                                  Log, RuntimeLog, VContainer, ...
                                                      General.asmdef
Framework(全部AOT):                                      引用: Core, Event, Log
  Asset.asmdef   → UniTask, YooAsset, Log            Project.asmdef
  Log.asmdef     → (零引用)                            引用: Core, General
  RuntimeLog.asmdef → Log
  Pool.asmdef    → UniTask, Cache
  Cache.asmdef   → (零引用)
  Event.asmdef   → ZLinq

Packages(第三方, 全部AOT):
  VContainer, UniTask, MessagePipe, YooAsset,
  HybridCLR.Runtime, ZLogger, ZLinq, ZString
```

> 注：`STATE.md` 已陈旧（称 Boot 仅引用 `Framework.Asset`），实际 `KJ.Boot.asmdef` 引用 `Asset/Log/RuntimeLog/UniTask/HybridCLR.Runtime`。以实际 asmdef 为准。

### 2.2 Boot 的 Framework 引用（核实，含真实 using）

`BootUpdateRunner.cs` / `Entry.cs` 真实引用（即 HYB-03 必须解耦的点）：

```csharp
// Boot/BootUpdateRunner.cs
using Framework.Asset;       // AssetRuntimeFactory, IAssetRuntime, AssetConfig
using Framework.Log;         // GameLog
using Framework.RuntimeLog;  // RuntimeLogManager

// Boot/Entry.cs
using Framework.Log;         // (间接) RuntimeLogManager
// Entry.Awake(): BootRuntimeLogBootstrap.EnsureInstalled(startupSettings);
// Entry catch: RuntimeLogManager.Current?.Write(...); RuntimeLogManager.Flush();
```

**结论**：`AssetConfig`、`IAssetRuntime`、`GameLog`、`RuntimeLogManager`、`BootRuntimeLogBootstrap` 都锁在 AOT 侧的 Boot 里，正是 HYB-03 死锁根源。

### 2.3 构建工具拦截现状（核实，逐字）

`KJHybridClrBuildTools.ValidateRuntimePreloadAssemblyName` 当前精确内容：

```csharp
private static void ValidateRuntimePreloadAssemblyName(string assemblyName)
{
    switch (assemblyName)
    {
        case "Boot":
        case "Asset":
        case "Event":
        case "Log":
        case "Pool":
        case "Cache":
        case "TestKit":
            throw new InvalidOperationException(
                $"Assembly '{assemblyName}' is not supported by the current runtime preload publication path. ...");
    }
}
```

- 当前拦截：`{Boot, Asset, Event, Log, Pool, Cache, TestKit}`。
- **`RuntimeLog` 当前未被拦截**（DS §2.3 列出的清单未含 RuntimeLog，属实）。
- `hotUpdateAssemblies` 单一事实源：`GetConfiguredHotUpdateAssemblyNames()` 直接读 `SettingsUtil.HotUpdateAssemblyNamesExcludePreserved`（即 `HybridCLRSettings`），再逐条过 `ValidateRuntimePreloadAssemblyName`，最终写回 `Entry.startupSettings.hotUpdateAssemblies`。**新增热更程序集只需改 HybridCLRSettings + 拦截白名单**。

### 2.4 三个“可直接入热更”的 Framework 程序集（核实）

| 程序集 | Boot 是否引用 | 被热更层谁使用 | 安全性 |
|--------|-------------|--------------|--------|
| Pool | ❌ | Core.PoolService（已在热更） | ✅ |
| Cache | ❌ | Core 内部系统（已在热更） | ✅ |
| Event | ❌ | Core/General 事件扫描（已在热更） | ✅ |

三者只被已在热更的 Core 使用，Boot 不引用 → 挡 1 纯配置变更成立。

### 2.5 AssetConfig / AssetConstants 归属（核实）

- `Assets/Framework/Asset/AssetConfig.cs` → `namespace Framework.Asset`，`ScriptableObject`，字段：`Mode(PlayMode: EditorSimulate|Offline|Host)`、`PackageName`、`EditorSimulatePackageRoot`、`CdnBaseUrl`、`DownloadTimeout`、`DownloadMaxConcurrency`、`FailedRetryCount`。
- `Assets/Framework/Asset/AssetConstants.cs` → `namespace Framework.Asset`，纯静态常量 `InitPriority`/`SystemPriority`。
- **两者均为纯数据、无 Unity Object 强依赖以外的耦合**，可整体迁出到 AOT 共享程序集 `Framework.AssetShared`，供 AOT 壳与热更层双向引用（DS 原稿“迁移两者”属实，保留）。

### 2.6 YooAsset 自定义服务核实（F2 依据）

`AssetRuntime.BuildSandboxParameters` 仅构造 `new CdnRemoteService(cdnBaseUrl)`（`CdnRemoteService : IRemoteService`，位于 `AssetRuntime.cs` 内 private sealed）。**全仓无自定义 `IBuildinQueryServices` / `IDecryptionServices`**——YooAsset 用 `CreateDefaultBuiltinFileSystemParameters` / `AssetBundle.LoadFromStream` 默认实现。故 HYB-03 只需把 `IRemoteService` 提为 AOT 侧 `BootRemoteService`，另两者不必提取。

---

## 三、目标架构

### 3.1 最终状态（挡 2 完成后）

```
AOT (IL2CPP)                                  HotUpdate (HybridCLR, 10 个)
═══════════                                    ═══════════════════════
Framework.AssetShared.asmdef (🆕 AOT 共享)      Boot.asmdef            (原 Boot，现为热更)
  ├─ AssetConfig.cs, AssetConstants.cs           ├─ BootUpdateRunner.cs (从 AOT 迁移)
  ├─ namespace: Framework.Asset                  ├─ BootRuntimeLogBootstrap.cs (迁移)
Launcher.asmdef (🆕 AOT Shell)                   ├─ 引用: Asset, Log, RuntimeLog,
  ├─ Entry.cs (迁移+改)                          │         Core, General, UniTask, MessagePipe
  ├─ BootLoader.cs (🆕 ~180)                     └─ (不再引用 HybridCLR.Runtime)
  ├─ BootBridge.cs (🆕)                         Asset.asmdef  ✅ 热更
  ├─ BootStartupLog.cs (🆕)                     Log.asmdef    ✅ 热更
  ├─ YooAssetStrategy/BootRemoteService.cs (🆕) RuntimeLog.asmdef ✅ 热更
  └─ Data/(迁移: Settings/Entry/Metadata/View)  Pool.asmdef   ✅ 热更
                                                 Cache.asmdef  ✅ 热更
YooAsset (C++)                 ❌               Event.asmdef  ✅ 热更
HybridCLR.Runtime (C++)        ❌               Core/General/Project ✅ 热更(已在)
VContainer/UniTask/MessagePipe ❌
ZLogger/ZLinq/ZString          ❌
TestKit                        ❌
```

> 最终热更 = **10** 个：`Boot, Core, General, Project, Pool, Cache, Event, Asset, Log, RuntimeLog`（DS 误写为 11，已修正）。

### 3.2 最终 HybridCLR 配置

```yaml
hotUpdateAssemblies:
- Boot           # 挡 2 新增（热更化）
- Core           # 已在
- General        # 已在
- Project        # 已在
- Pool           # 挡 1 新增
- Cache          # 挡 1 新增
- Event          # 挡 1 新增
- Asset          # 挡 2 新增
- Log            # 挡 2 新增
- RuntimeLog     # 挡 2 新增

patchAOTAssemblies:  # 不变
- mscorlib
- System
- System.Core
```

### 3.3 启动流对比

**当前：**
```
Entry.Awake() → BootRuntimeLogBootstrap.EnsureInstalled()   ← AOT(Framework.RuntimeLog)
  └─ BootUpdateRunner.RunAsync()
      ├─ Framework.Log / Framework.Asset (AOT) — YooAsset 初始化+下载
      ├─ AOT metadata 加载
      ├─ Assembly.Load(Core/General/Project.dll)            ← 🔴 分割线
      └─ 反射 ProjectStartup.Start()
```

**挡 2 后：**
```
Entry.Awake()                                        ← AOT(Launcher)，无 Framework 引用
  └─ BootLoader.RunAsync()                           ← AOT
      ├─ BootStartupLog.Info() — 独立文本日志
      ├─ YooAssets.CreatePackage() — YooAsset 原生
      ├─ Host: 版本检查 + 下载 + DLL 加载            ← AOT
      ├─ Assembly.Load(全部 10 个热更 DLL，含 Boot)  ← 🔴 分割线
      ├─ 构造 BootBridge(package, settings, view, config, earlyLogs)
      └─ 反射 BootUpdateRunner.Start(bridge)         ← 🔵 进入热更

BootUpdateRunner.Start(bridge)                      ← 🔵 热更
  ├─ _assetRuntime.WrapFromExistingPackage(config, package)  ← 接管 BootLoader 初始化的 Package
  ├─ BootRuntimeLogBootstrap.EnsureInstalled()       ← 现可用 Framework API
  ├─ ReplayEarlyLogs() 回放 AOT 日志
  ├─ GameLog.Info(...) — 正常使用 Framework.Log
  └─ 反射 ProjectStartup.Start(_assetRuntime)
```

---

## 四、挡 1：Pool / Cache / Event 入热更（已修正）

### 4.1 改动文件（2 个）

| # | 文件 | 改动 | 行数 |
|---|------|------|------|
| 1 | `ProjectSettings/HybridCLRSettings.asset` | `hotUpdateAssemblies` 追加 Pool, Cache, Event | +3 |
| 2 | `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | `ValidateRuntimePreloadAssemblyName()` 从 switch 中删除 `Event/Pool/Cache` 三个 case | -3 case |

### 4.2 改动细节（修正 DS §4.2 的 bug）

**文件 1 — HybridCLRSettings.asset：**
```yaml
# 修改后
hotUpdateAssemblies: [Core, General, Project, Pool, Cache, Event]
```

**文件 2 — 拦截 switch（逐挡准确内容）：**

```csharp
// 挡 1 后（Pool/Cache/Event 已入热更 → 从拦截移除；Asset/Log 仍 AOT → 必须保留拦截）
switch (assemblyName)
{
    case "Boot":    // AOT Shell（挡 2 拆分前仍是 AOT）
    case "Asset":   // 挡 2 才入热更，现仍为 AOT，须拦截防误加
    case "Log":     // 同上
    case "TestKit": // 非产品代码
        throw new InvalidOperationException(
            $"Assembly '{assemblyName}' is not supported by the current runtime preload publication path. ...");
    // 注意：RuntimeLog 当前未被拦截（历史遗留），挡 1 不引入变化；
    //       若希望防御性收紧，可补 case "RuntimeLog":，但非必须。
}
```

> ⚠️ **DS §4.2 原稿错误**：其代码只保留了 `Boot` 和 `TestKit` 两个 case，却注释称“Asset/Log/RuntimeLog 保留拦截”——代码与注释自相矛盾，且该代码会**错误地放行 Asset/Log**（挡 1 阶段它们仍应是 AOT）。上表为修正后的正确内容。

### 4.3 安全性验证

| 检查项 | 结论 | 理由 |
|--------|------|------|
| Boot 不引用 Pool/Cache/Event？ | ✅ | `KJ.Boot.asmdef` references: Asset, Log, RuntimeLog, UniTask, HybridCLR.Runtime |
| Core 已在热更？ | ✅ | HybridCLRSettings 已有 Core |
| 热更引用 AOT 正常？ | ✅ | HybridCLR 原生支持；Pool 引用 UniTask(AOT) 是标准用法 |
| 泛型桥缺失风险？ | ✅ SuperSet 覆盖 | patchAOTAssemblies 含 mscorlib/System/System.Core，覆盖 List<T>/Dictionary<K,V>/HashSet<T> 等 |
| Event 类型扫描？ | ✅ | 反射扫描，HybridCLR 下行为一致 |

### 4.4 执行步骤

1. 改 `HybridCLRSettings.asset`：追加 Pool, Cache, Event。
2. 改 `KJHybridClrBuildTools.cs`：从拦截 switch 删除 `Event/Pool/Cache` 三个 case（保留 Boot/Asset/Log/TestKit）。
3. Editor 执行 `KJ/HybridCLR/Prepare Runtime Assets And Boot` — 验证同步无报错。
4. Editor Play — 启动链完整：`[AssetSystem] Ready` + 全部初始化完成。
5. Console / runtime log 无 `MissingMetadataException`。

**预估 ~15 分钟。**

---

## 五、挡 2：Boot 拆分 + Asset/Log/RuntimeLog 入热更（HYB-03，已修正）

### 5.1 设计核心思想

`Boot.asmdef` 当前混装两类职责：
- **启动加载器**（必须 AOT）：`Assembly.Load` 之前就要跑的代码。
- **更新编排器**（可热更）：`Assembly.Load` 之后才跑的代码。

以 `Assembly.Load` 调用点为界，裂变为 `Launcher.asmdef(AOT Shell)` + `Boot.asmdef(HotUpdate)`：

```
裂变前: Boot.asmdef (AOT) — 混装
裂变后: Launcher.asmdef (AOT, 只做"找到并加载热更代码") + Boot.asmdef (HotUpdate, 做"更新流程编排")
```

### 5.2 前置问题：AssetConfig / AssetConstants 跨边界共享（F1）

`BootLoader`(AOT) 需读 `AssetConfig` 决定 PlayMode / CDN URL；但 `AssetConfig` 当前属 `Framework.Asset`（挡 2 变热更），AOT 壳无法引用热更程序集。

**方案**：把 `AssetConfig` + `AssetConstants` 迁入新建 AOT 共享程序集 **`Framework.AssetShared`**（namespace 保留 `Framework.Asset`）。极薄程序集，AOT 与热更双向引用——HybridCLR 跨边界共享类型的标准模式。

```
Assets/Framework/AssetShared/
  ├─ Framework.AssetShared.asmdef   ← 零引用（不引用 Asset），noEngineReferences=false(Unity Object 需要)
  ├─ AssetConfig.cs                 ← 从 Framework/Asset 移入
  └─ AssetConstants.cs              ← 从 Framework/Asset 移入
```
asmdef 影响：
- `Launcher.asmdef` 新增引用 `AssetShared`（AOT 侧读 AssetConfig）。
- `Asset.asmdef` 新增引用 `AssetShared`（热更侧继续用 AssetConfig/AssetConstants）。
- 通过 Unity 编辑器移动文件以保持 `.meta` 不丢，保证 ScriptableObject 序列化稳定。

> 命名说明：本 agent 首稿曾用 `KJ.Asset.Config`，但与项目 `Framework.*` 命名规范不符；统一采用 `Framework.AssetShared`（与 DS 一致）。

### 5.3 程序集重构

**Launcher.asmdef（AOT Shell）**
```
位置: Assets/Scripts/Boot/Launcher/
名称: "Launcher", rootNamespace: "Boot" (不变)
引用: [UniTask, YooAsset, HybridCLR.Runtime, AssetShared]
❌ 不引用: Asset, Log, RuntimeLog, Pool, Cache, Event, Core, VContainer, MessagePipe, Framework.*
硬约束: Launcher 不得引用任何 Framework 包或热更程序集（靠 asmdef 强制）
```

**Boot.asmdef（HotUpdate）**
```
位置: Assets/Scripts/Boot/ (剩余文件)
名称: "Boot" (不变), rootNamespace: "Boot" (不变)
引用: [Asset, Log, RuntimeLog, Pool, Cache, Event, Core, General, UniTask, MessagePipe]
  ← 新增: Pool, Cache, Event, Core, General, MessagePipe
  ← 保留: Asset, Log, RuntimeLog, UniTask
  ← 删除: HybridCLR.Runtime (BootLoader 已代为加载，热更层不需)
```

**Framework.Asset 新增**
| 文件 | 改动 |
|------|------|
| `AssetRuntime.cs` | 新增 `WrapFromExistingPackage(AssetConfig, ResourcePackage)` |
| `IAssetRuntime.cs` | 新增接口方法声明 |

### 5.4 新文件设计详案（含 F2 修正：仅 IRemoteService）

#### 5.4.1 BootLoader.cs `Assets/Scripts/Boot/Launcher/BootLoader.cs`（约 180 行，AOT 壳核心）

要点（完整代码见 DS 原稿 §5.4.1，此处只标注与本文修正相关的约束）：
- `using` 仅限 `System.*`、`Cysharp.Threading.Tasks`、`HybridCLR`、`UnityEngine`、`UnityEngine.Networking`、`YooAsset`、`Boot`（自身 namespace）。**严禁** `using Framework.*` / `GameLog` / `RuntimeLogManager`。
- 资源字节读取**必须**走 YooAsset 原生：`_package.LoadAssetSync<RawFileObject>(entry.AssetPath)`（**不能**走 `IAssetRuntime.LoadRawBytes`，否则形成 AOT→热更反向引用）。
- `LoadHotUpdateAssembliesAsync` 遍历 `settings.HotUpdateAssemblies`（含 **Boot 自身**），逐个 `Assembly.Load`。
- `#if UNITY_EDITOR` + `SkipHotUpdateInEditor` 时跳过 `Assembly.Load`，但仍反射 `BootUpdateRunner.Start(bridge)`（Editor 下热更 asmdef 直接编译，反射可达）。
- Host 模式 `BuildSandboxParameters` 仅注入 `new BootRemoteService(cdnBaseUrl)`（见 5.4.4）；Builtin FS 用 `CreateDefaultBuiltinFileSystemParameters(packageName)`（YooAsset 默认，无需自定义查询服务）。

#### 5.4.2 BootBridge.cs `Assets/Scripts/Boot/Launcher/BootBridge.cs`
AOT→热更桥梁数据对象：`Package(ResourcePackage)`、`Settings(BootStartupSettings)`、`View(IBootStartupView)`、`Config(AssetConfig)`、`EarlyLogs(IReadOnlyList<BootStartupLogEntry>)`。ResourcePackage 所有权：BootLoader 创建 → Bridge 传递 → `AssetRuntime.WrapFromExistingPackage` 接管 → Shutdown 释放。

#### 5.4.3 BootStartupLog.cs `Assets/Scripts/Boot/Launcher/BootStartupLog.cs`
AOT 阶段极简日志：直接写 `Logs/Runtime/boot.log` 文本文件 + 内存缓存 `List<BootStartupLogEntry>`。不依赖 Framework.Log / RuntimeLog。热更层初始化后由 `BootUpdateRunner.ReplayEarlyLogs()` 回放到 RuntimeLog session；AOT 崩溃时也可直接读 `boot.log` 排查。

#### 5.4.4 BootRemoteService.cs `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootRemoteService.cs`（F2：唯一需要提取的 YooAsset 服务）
```csharp
using System.Collections.Generic;
using YooAsset;

namespace Boot
{
    /// <summary>AOT 侧 IRemoteService 实现，从 AssetRuntime.CdnRemoteService 提取。</summary>
    public sealed class BootRemoteService : IRemoteService
    {
        public static System.Func<string, IReadOnlyList<string>> CustomUrlProvider;
        private readonly string _baseUrl;
        public BootRemoteService(string baseUrl) => _baseUrl = (baseUrl ?? "").TrimEnd('/');
        public IReadOnlyList<string> GetRemoteUrls(string fileName)
            => CustomUrlProvider?.Invoke(fileName) ?? new[] { $"{_baseUrl}/{fileName}" };
    }
}
```

> **F2 修正**：DS 原稿还新建了 `BootBuildinQueryService.cs` 与 `BootDecryptionService.cs`。经核实 `AssetRuntime` 无此二者自定义实现，YooAsset 用默认（`CreateDefaultBuiltinFileSystemParameters` / `AssetBundle.LoadFromStream`），故**删除这两个文件**，不在 Launcher 下创建 `YooAssetStrategy/BootBuildinQueryService.cs` 与 `BootDecryptionService.cs`，任务清单与目录结构同步移除。

### 5.5 修改文件详案

#### 5.5.1 Entry.cs — 迁入 Launcher 并去 Framework 依赖（F5）
- 移至 `Assets/Scripts/Boot/Launcher/Entry.cs`。
- `Awake()` 删除 `BootRuntimeLogBootstrap.EnsureInstalled()`（AOT 用 `BootStartupLog` 替代）；删除 `using Framework.Log` / `using Framework.RuntimeLog`。
- `catch` 中不再用 `RuntimeLogManager`，改为 `_loader?.Log.Error($"Startup failed: {e}")`。
- 用 `BootLoader` 替代 `BootUpdateRunner`。

#### 5.5.2 BootUpdateRunner.cs — 接收 BootBridge（职责边界澄清）
- 新增静态入口 `Start(BootBridge bridge)` → `new BootUpdateRunner(bridge).RunAsync().Forget()`，由 BootLoader 反射调用。
- 构造函数接收 `bridge`：`_assetRuntime = AssetRuntimeFactory.Create();` 然后 **对全部模式**调用 `_assetRuntime.WrapFromExistingPackage(config, bridge.Package)`（接管 BootLoader 已初始化的 Package；下载步骤已由 BootLoader 在 Host 模式完成，故不再调用 `BeginInitialize`）。
- `RunAsync` 顺序：`BootRuntimeLogBootstrap.EnsureInstalled(settings)` → `ReplayEarlyLogs()` → `GameLog.Info(...)` → `StartGame()`（反射 `Project.Bootstrap.ProjectStartup, Project` 的 `Start`，并把 `_assetRuntime` 作为单参传入，沿用现有 `ProjectStartup.Start(IAssetRuntime)` 约定）。
- `LoadHotUpdateCodeAsync` / 资源初始化/下载逻辑从本类**移除**（已由 BootLoader 在 AOT 完成），避免重复。
- `ReplayEarlyLogs()`：遍历 `bridge.EarlyLogs`，逐条 `RuntimeLogManager.Current.Write(...)`。

#### 5.5.3 BootRuntimeLogBootstrap.cs — 新增 EarlyLogs 回放支持
保留在 `Assets/Scripts/Boot/`（现属热更），原有逻辑不变（现可用 Framework API），新增对 `BootBridge.EarlyLogs` 回放的衔接（实际回放逻辑在 `BootUpdateRunner.ReplayEarlyLogs`）。

#### 5.5.4 AssetRuntime.cs / IAssetRuntime.cs — 新增 WrapFromExistingPackage
```csharp
// IAssetRuntime.cs
void WrapFromExistingPackage(AssetConfig config, ResourcePackage existingPackage);

// AssetRuntime.cs
public void WrapFromExistingPackage(AssetConfig config, ResourcePackage existingPackage)
{
    if (existingPackage == null) throw new ArgumentNullException(nameof(existingPackage));
    if (config == null) throw new ArgumentNullException(nameof(config));
    if (IsReady) throw new InvalidOperationException("AssetRuntime is already initialized.");
    _config = config;
    _defaultPackage = existingPackage;
    _downloadMaxConcurrency = Math.Max(1, config.DownloadMaxConcurrency);
    _failedRetryCount = Math.Max(0, config.FailedRetryCount);
    lock (_gate) { _lifecycleVersion++; }
    IsReady = true;
    LastError = string.Empty;
}
```
> 若 `AssetRuntime` 对 EditorSimulate/Offline 模式有 `BeginInitialize` 之外的额外副作用，应折叠进 `WrapFromExistingPackage`，确保 BootLoader 初始化后的 Package 被无缝接管（实现阶段验证）。

#### 5.5.5 KJHybridClrBuildTools.cs — 更新拦截白名单（挡 2 后）
```csharp
// 挡 2 后：Boot/Asset/Log 均已入热更 → 从拦截移除；新增 Launcher(AOT Shell) 拦截
switch (assemblyName)
{
    case "Launcher": // AOT Shell，永远不进热更
    case "TestKit":  // 非产品代码
        throw new InvalidOperationException(
            $"Assembly '{assemblyName}' is an AOT shell or test-only assembly. It must not appear in the hot-update publication list.");
    // RuntimeLog 当前本就未被拦截，挡 2 后随 Asset/Log 一同入热更，无需处理
}
```

#### 5.5.6 HybridCLRSettings.asset
```yaml
hotUpdateAssemblies: [Boot, Core, General, Project, Pool, Cache, Event, Asset, Log, RuntimeLog]
```

### 5.6 目录结构变化
```
当前:
Assets/Scripts/Boot/                  → Boot.asmdef (AOT)
  ├─ Entry.cs, BootUpdateRunner.cs, BootRuntimeLogBootstrap.cs
  ├─ Data/ (Settings/Entry/Metadata/View)
  └─ Boot.Editor/                     → Boot.Editor.asmdef

挡 2 后:
Assets/Scripts/Boot/
├── Launcher/                         → Launcher.asmdef (AOT)
│   ├── Entry.cs                      (迁移+改, 去 Framework 引用)
│   ├── BootLoader.cs                 (🆕)
│   ├── BootBridge.cs                 (🆕)
│   ├── BootStartupLog.cs             (🆕)
│   ├── YooAssetStrategy/BootRemoteService.cs (🆕, 仅此一个策略类)
│   └── Data/                         (迁移 Settings/Entry/Metadata/View，纯 POCO)
├── KJ.Boot.asmdef                    → Boot.asmdef (HotUpdate)
├── BootUpdateRunner.cs               (改, 接 Bridge+Replay)
├── BootRuntimeLogBootstrap.cs        (改, EarlyLogs 衔接)
└── Boot.Editor/                      → Boot.Editor.asmdef (不变, 引用 +Launcher/+AssetShared)
    ├── KJHybridClrBuildTools.cs      (改白名单)
    └── Build/
Assets/Framework/AssetShared/         → Framework.AssetShared.asmdef (🆕 AOT 共享)
  ├─ AssetConfig.cs                   (🔀 迁移)
  └─ AssetConstants.cs                (🔀 迁移)
Assets/Framework/Asset/Asset.asmdef   (改, +AssetShared 引用)
```

### 5.7 边界问题解决方案（修正后）
| 问题 | 解决 |
|------|------|
| YooAsset “鸡与蛋”死锁 | 仅提取 `IRemoteService` → AOT 侧 `BootRemoteService`，BootLoader 用其初始化 YooAsset 去下载热更 DLL（F2 修正，不提取多余服务） |
| Editor 模拟模式 | `InitializePackageByMode()` 按 `AssetConfig.Mode` 路由 EditorSimulate/Offline/Host；`SkipHotUpdateInEditor` 跳过 Assembly.Load 仍反射 BootUpdateRunner |
| 程序集名冲突 | `Launcher.dll`(AOT) vs `Boot.dll`(HotUpdate)，名称不同不冲突 |
| 早期日志黑洞 | `BootStartupLog` → `BootBridge.EarlyLogs` → `BootUpdateRunner.ReplayEarlyLogs()` → `RuntimeLogSession.Write()`；AOT 崩溃可读 `boot.log` |
| AssetConfig 跨边界 | 迁入 `Framework.AssetShared`(AOT 共享)，AOT/热更双向引用（F1） |

### 5.8 任务分解与执行顺序（移除多余服务文件后）
```
Phase A — 基础设施（可并行）
  [A1] 新建 Assets/Scripts/Boot/Launcher/ 目录
  [A2] 新建 KJ.Launcher.asmdef (AOT)
  [A3] Framework.Asset 新增 WrapFromExistingPackage (+ IAssetRuntime 声明)
  [A4] KJHybridClrBuildTools 白名单更新（挡2后形态：Launcher/TestKit）
Phase B — 纯数据迁移（并行，依赖 A1）
  [B1] 移 Data/ → Launcher/Data/ (Settings/Entry/Metadata/View)
  [B2] 新建 Framework.AssetShared/ (AssetConfig.cs + AssetConstants.cs 迁移)
  [B3] Asset.asmdef + Launcher.asmdef 加 AssetShared 引用
  [B4] 新建 BootBridge.cs / BootStartupLog.cs
  [B5] 新建 YooAssetStrategy/BootRemoteService.cs   ← 仅此一个（F2）
Phase C — 核心逻辑（依赖 A+B）
  [C1] 新建 BootLoader.cs
  [C2] 改 Entry.cs（迁移+去 Framework 引用，委托 BootLoader）
Phase D — 热更化改造（依赖 A）
  [D1] 更新 KJ.Boot.asmdef references（热更化，删 HybridCLR.Runtime）
  [D2] 改 BootUpdateRunner.cs（接 Bridge + 去资源初始化/下载 + ReplayEarlyLogs）
  [D3] 改 BootRuntimeLogBootstrap.cs
Phase E — 配置 + 验证
  [E1] 更新 HybridCLRSettings.asset（加 Boot/Asset/Log/RuntimeLog）
  [E2] Unity 编译验证（Launcher.asmdef 不引用任何 Framework/热更）
  [E3] Editor Play 启动链验证
  [E4] KJ/HybridCLR/Prepare Runtime Assets And Boot 验证
```

### 5.9 验证清单
| # | 项 | 方法 |
|---|---|---|
| 1 | Launcher.asmdef 不引用任何 Framework 包 | 查 asmdef references |
| 2 | Launcher.asmdef 不引用任何热更程序集 | 查 asmdef references |
| 3 | BootLoader 无 Framework 引用 | 代码审查（无 `using Framework.*` / `GameLog` / `RuntimeLogManager`） |
| 4 | BootLoader 只用 YooAsset 原生 API 读 RawFile | 代码审查（`LoadAssetSync<RawFileObject>`） |
| 5 | Editor Play (EditorSimulate) 启动链完整 | 日志含 `[AssetSystem] Ready` |
| 6 | Editor Play (Host + Skip) 正常 | 日志含 `SkipHotUpdateInEditor` |
| 7 | HybridCLR Generate 后 DLL 列表正确 | 同步 Dlls/ 含全部 10 个程序集 |
| 8 | BootBridge.EarlyLogs 回放正常 | boot.log 内容出现在 latest.jsonl |
| 9 | WrapFromExistingPackage 正确 | Host 模式 IsReady=true，操作正常 |
| 10 | 无 MissingMetadataException | Console + runtime log |

---

## 六、风险与回滚

- **挡 1 风险极低**：仅配置+工具链。回滚 = 还原 2 文件 + 重新 `Prepare Runtime Assets And Boot`。
- **挡 2 风险中等**：涉及启动链改造。建议 Phase A–E 在同一条 **独立分支** 进行，每 Phase 一个可编译提交；任一 Phase 失败可 `git revert` 到上一可编译点。
- **泛型桥缺失**：若运行期 `MissingMetadataException` 指向 Pool/Cache/Event 具体泛型实例化，按需建 `Framework.Aot` 补充程序集（附录 A），挡 1/挡 2 开发期不预期需要。
- **软重启能力**（不退出进程重载 DLL）为挡 2 之后规划，不在本文档范围（附录 B）。

---

## 七、相对 DS 原稿的修正记录（核实结论）

| 编号 | DS 原稿问题 | 核实结论 | 本文修正 |
|------|------------|---------|---------|
| F1 | 用 `Framework.AssetShared` 迁 AssetConfig/AssetConstants | 两者均存在、均纯数据，迁移成立 | 命名统一为 `Framework.AssetShared`（本 agent 首稿 `KJ.Asset.Config` 不合规已弃） |
| F2 | 提取 IRemoteService + IBuildinQueryServices + IDecryptionServices 三件套 | `AssetRuntime` 仅自定义 `IRemoteService`；另两者用 YooAsset 默认，**不存在** | 仅保留 `BootRemoteService`；删除 `BootBuildinQueryService`/`BootDecryptionService` 及其任务/目录项 |
| F3 | §4.2 代码删 Event/Pool/Cache 但保留 Boot/TestKit，注释却称“Asset/Log 保留拦截” | 实际代码会错误放行 Asset/Log | 给出逐挡准确 switch：挡1后 `{Boot,Asset,Log,TestKit}`；挡2后 `{Launcher,TestKit}` |
| F4 | BootLoader 用 YooAsset 原生读 RawFile | 当前 `BootUpdateRunner` 走 `IAssetRuntime.LoadRawBytes`，AOT 壳不能引用热更 Asset | 明确 BootLoader 必须用 `package.LoadAssetSync<RawFileObject>`，且须加载含 Boot 自身全部 DLL；skipHotUpdateInEditor 仍反射 Start |
| F5 | Entry 去 Framework 依赖 | `Entry` 现用 `BootRuntimeLogBootstrap`/`RuntimeLogManager`，迁 Launcher(AOT) 后须删除 | Entry 迁 Launcher，错误改走 `BootStartupLog` |
| N1 | 称最终热更 11 个程序集 | 实为 Boot/Core/General/Project/Pool/Cache/Event/Asset/Log/RuntimeLog = **10** | 修正计数为 10 |
| N2 | §2.3 拦截清单未含 RuntimeLog | 实际 `ValidateRuntimePreloadAssemblyName` 当前不含 RuntimeLog | 全文据实说明 RuntimeLog 当前未拦截 |

---

## 八、附录

### 附录 A：Framework.Aot 补充程序集（按需，挡 2 后）
运行期 `MissingMetadataException` 指向具体泛型实例化时，建 `Assets/Framework/Aot/`（asmdef 引用相关热更+HybridCLR.Runtime）声明对应泛型组合桥。开发期不预期需要。

### 附录 B：软重启能力（后续规划）
不退出进程重新加载 DLL（参考 37 项目 `GameLife/GameRestart.cs`），为挡 2 之后能力点，不在本文档范围。

### 附录 C：文件改动汇总
**挡 1：**
| 文件 | 类型 | 改动 |
|------|------|------|
| `ProjectSettings/HybridCLRSettings.asset` | 改 | +3 |
| `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | 改 | -3 case |

**挡 2：**
| 文件 | 类型 |
|------|------|
| `Assets/Framework/AssetShared/Framework.AssetShared.asmdef` | 🆕 |
| `Assets/Framework/AssetShared/AssetConfig.cs` | 🔀 迁移 |
| `Assets/Framework/AssetShared/AssetConstants.cs` | 🔀 迁移 |
| `Assets/Framework/Asset/Asset.asmdef` | 改（+AssetShared） |
| `Assets/Scripts/Boot/Launcher/KJ.Launcher.asmdef` | 🆕 |
| `Assets/Scripts/Boot/Launcher/BootLoader.cs` | 🆕 |
| `Assets/Scripts/Boot/Launcher/BootBridge.cs` | 🆕 |
| `Assets/Scripts/Boot/Launcher/BootStartupLog.cs` | 🆕 |
| `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootRemoteService.cs` | 🆕（仅此一个） |
| `Assets/Scripts/Boot/Launcher/Entry.cs` | 🔀 迁移+改 |
| `Assets/Scripts/Boot/Launcher/Data/*` | 🔀 迁移（4 个 POCO） |
| `Assets/Scripts/Boot/KJ.Boot.asmdef` | 改（热更化） |
| `Assets/Scripts/Boot/BootUpdateRunner.cs` | 改（+Bridge/去资源初始化/Replay） |
| `Assets/Scripts/Boot/BootRuntimeLogBootstrap.cs` | 改（+EarlyLogs 衔接） |
| `Assets/Framework/Asset/AssetRuntime.cs` | 改（+Wrap 方法） |
| `Assets/Framework/Asset/IAssetRuntime.cs` | 改（+接口方法） |
| `Assets/Scripts/Boot.Editor/Boot.Editor.asmdef` | 改（+Launcher/+AssetShared） |
| `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | 改（白名单） |
| `ProjectSettings/HybridCLRSettings.asset` | 改（+7） |

### 附录 D：参考来源
| 来源 | 内容 |
|------|------|
| `ProgressDoc/Discuss/HybridCLR热更程序集范围分析.md` | 前置讨论 |
| `ProgressDoc/Discuss/DS_HybridCLR热更程序集范围扩展完整设计.md` | 原稿（本版已合并并修正） |
| `.planning/HOT_UPDATE_BOUNDARY.md` / `ROADMAP.md`(HYB-03) / `STATE.md` / `PROJECT.md` / `目录结构规范.md` | 规范 |
| `ProjectSettings/HybridCLRSettings.asset` | 当前热更配置（核实） |
| `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | 构建工具（核实，含精确 switch） |
| `Assets/Framework/Asset/{AssetConfig,AssetConstants,AssetRuntime}.cs` | 共享配置与 YooAsset 服务（核实） |
| `Assets/Scripts/Boot/{Entry,BootUpdateRunner,BootStartupSettings,BootAssemblyEntry,BootMetadataEntry}.cs` | 启动链与配置（核实） |
| `CODE_MAP.md` | 37 项目 Boot 层教训 |

### 附录 E：实际实现偏差（2026-07-07）

本文档为设计/方案基线，落地时与计划有一处关键引用集合不同，其余均按文档执行：

- **`KJ.Boot.asmdef` 实际引用**：`Asset / Log / RuntimeLog / UniTask / AssetShared / YooAsset / Launcher`。
  文档 §5.3 计划写的是 `[Asset, Log, RuntimeLog, Pool, Cache, Event, Core, General, UniTask, MessagePipe]`——
  实际实现**未**引用 `Pool/Cache/Event/Core/General/MessagePipe`（Boot 更新流程通过反射进入 `ProjectStartup`，
  不直接依赖这些层）；`YooAsset` 与 `Launcher`/`AssetShared` 为实际新增引用。以工程内 `.asmdef` 为准。
- **`KJ.Launcher.asmdef` 实际引用**：`UniTask / YooAsset / HybridCLR.Runtime / AssetShared`（与文档 §5.3 一致）。
- **`IsExternalInit` polyfill**：Unity .NET Standard 2.1 不自带 `System.Runtime.CompilerServices.IsExternalInit`，
  新增 `Assets/Scripts/Boot/Launcher/IsExternalInit.cs` 使 `BootStartupLogEntry` 的 `init` 访问器可编译（文档未列此项）。
- **测试**：`Assets/Tests/EditMode/HybridCLRBootTests.cs` 在文档写就后增补 3 例核心边界测试
  （`Launcher_DoesNotReferenceHotUpdateAssemblies` / `BootLoader_ResolvesBootUpdateRunnerByAssemblyQualifiedName` /
  `AllTenHotUpdateAssembliesAreLoaded`），使 HYB-03 相关用例达 15 例；配套 `AutoExportTestResults.cs` 将结果导出到 `TestResults.xml`。
- **验证结果**：EditMode 全工程 **45/45 通过，0 失败**（2026-07-07，Unity 2022.3.62f2）。
