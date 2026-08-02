# DS_HybridCLR热更程序集范围扩展——完整需求分析与实现设计

> **⚠️ 状态对齐（2026-07-08 补）**：本文为 DS 原稿（DeepSeek 产出），已被 `Hy3_HybridCLR热更程序集_需求分析与实现方案.md`（v2 合并版）**取代**。v2 合并版对照真实代码核实了 5 处修正（命名/去过度设计等），为当前唯一权威版本。HYB-03 已落地实现，EditMode 测试 45/45 全绿。如需了解当前实现状态，请以 Hy3 合并版及 `CODEMAP.md` 的 **Framework: Launcher + Boot** 章节为准。
>
> 文档性质：需求分析 + 架构设计 + 可执行实现方案
> 创建日期：2026-07-06
> 前置分析：`ProgressDoc/Archive/资源系统/HybridCLR热更程序集范围分析.md`
> 关联规范：`.planning/HOT_UPDATE_BOUNDARY.md`、`.planning/ROADMAP.md` HYB-03

---

## 一、需求分析

### 1.1 现状问题

当前 HybridCLR 热更程序集仅覆盖 3 个：

```yaml
# ProjectSettings/HybridCLRSettings.asset (当前)
hotUpdateAssemblies: [Core, General, Project]
```

Framework 层全部 6 个程序集（Pool / Cache / Event / Asset / Log / RuntimeLog）和 Boot 全部编入 AOT。

这不是架构决策，而是 Phase 0 起步阶段的保守默认配置。它带来三个实际问题：

1. **框架本身的 bug 修复需要换包。** 比如 ObjectPool<T> 的并发 bug、Cache 的 LRU 淘汰策略优化——改一行 Pool 代码就要走应用商店审核，热更体系形同虚设。

2. **Boot 启动逻辑锁死在 AOT。** 资源版本检查策略、下载重试逻辑、更新 UI 交互——这些都是上线后最可能需要高频迭代的内容，但目前全部不可热更。

3. **AOT 面过大违背热更体系初衷。** IL2CPP 编译进 Native 的代码永远无法替换。AOT 面越大，"只能换包不能热更"的代码就越多。

37 项目的教训：Boot 层积累到 **38 个文件全部 AOT**，更新流程高度耦合但无法热更，每次调整都极其痛苦。

### 1.2 设计原则

> **除了确实无法热更的原生代码，其余全部可热更。**

| 类别 | 热更？ | 理由 |
|------|--------|------|
| KJ 自有 C# 代码（Framework / Boot / Core / General / Project） | ✅ | 纯托管代码，HybridCLR 完全支持 |
| YooAsset / HybridCLR.Runtime | ❌ | C++ Native，技术上不可热更 |
| Unity Engine API | ❌ | C++ Native |
| VContainer / UniTask / MessagePipe / ZLogger / ZLinq / ZString | ❌ | 业务判断：极度稳定的第三方库，AOT 执行效率更高 |
| TestKit | ❌ | 非产品代码 |

### 1.3 分挡策略：为什么分两步走

把所有事情一次性做完风险太高。按"受 Boot 时序依赖"与否自然分成两步：

| | 挡 1 | 挡 2（HYB-03） |
|---|---|---|
| **内容** | Pool / Cache / Event 入热更 | Boot 拆分为 Launcher(AOT) + Boot(热更)，Asset/Log/RuntimeLog 随之入热更 |
| **改动面** | 2 个文件，~5 行 | ~12 新文件 + ~8 修改 + 4 迁移 |
| **为什么可以/必须分开** | 这三个不被 Boot 引用，放入热更毫无阻力 | 这三个被 Boot 直接引用，必须先拆分 Boot 才能移动 |
| **风险** | 极低——纯配置变更 | 中等——涉及程序集拆分和启动链改造 |
| **Gate** | Editor Play 通过即可 | Editor Play + HybridCLR Generate 验证 |

---

## 二、当前状态全貌

### 2.1 程序集依赖关系

```
AOT (IL2CPP 编译进 Native)                     HotUpdate (HybridCLR 解释执行)
═══════════════════════════                     ════════════════════════════
                                               Core.asmdef
Boot.asmdef                                      引用: Asset, Event, Pool, Cache,
  引用: Asset, Log, RuntimeLog,                        Log, RuntimeLog, VContainer, ...
     UniTask, HybridCLR.Runtime
                                               General.asmdef
Framework (全部 AOT):                            引用: Core, Event, Log
  Asset.asmdef   → UniTask, YooAsset, Log
  Log.asmdef     → (零引用)                     Project.asmdef
  RuntimeLog.asmdef → Log                         引用: Core, General
  Pool.asmdef    → UniTask, Cache
  Cache.asmdef   → (零引用)
  Event.asmdef   → ZLinq

Packages (第三方，全部 AOT):
  VContainer, UniTask, MessagePipe,
  YooAsset, HybridCLR.Runtime,
  ZLogger, ZLinq, ZString
```

### 2.2 关键约束：Boot 的 Framework 引用

此为挡 2 设计的核心约束。BootUpdateRunner.RunAsync() 的执行流及其 Framework 依赖：

```
RunAsync()
├─ GameLog.Info("[Boot] Startup begin")            ← Framework.Log (AOT)
├─ AssetRuntimeFactory.Create()                    ← Framework.Asset (AOT)
├─ _assetRuntime.BeginInitialize(config)           ← Framework.Asset (AOT)
├─ _assetRuntime.UpdateManifest()                  ← Framework.Asset (AOT)
├─ _assetRuntime.CreateDownloader()                ← Framework.Asset (AOT)
├─ _assetRuntime.LoadRawBytes(path)                ← Framework.Asset (AOT)
├─ RuntimeApi.LoadMetadataForAOTAssembly(...)      ← HybridCLR.Runtime (AOT)
├─ Assembly.Load(bytes)                            ← 🔴 热更 DLL 加载点
├─ RuntimeLogManager.Current?.UpdateSessionInfo()  ← Framework.RuntimeLog (AOT)
├─ Type.GetType(...) / method.Invoke(...)          ← 🔵 进入热更层
└─ RuntimeLogManager.Flush()                       ← Framework.RuntimeLog (AOT)
```

**热更 DLL 加载点就是分割线：Assembly.Load 之前的代码必须是 AOT，之后可以使用热更代码。**

### 2.3 当前构建工具拦截

`KJHybridClrBuildTools.ValidateRuntimePreloadAssemblyName()` 明确拦截：

```csharp
switch (assemblyName)
{
    case "Boot": case "Asset": case "Event":
    case "Log": case "Pool": case "Cache": case "TestKit":
        throw new InvalidOperationException("...");
}
```

### 2.4 三个"可直接入热更"的 Framework 程序集

| 程序集 | Boot 是否引用 | 被热更层谁使用 | 安全性 |
|--------|-------------|--------------|--------|
| Pool | ❌ | Core.PoolService（已在热更） | ✅ 完全安全 |
| Cache | ❌ | Core 内部系统（已在热更） | ✅ 完全安全 |
| Event | ❌ | Core/General 事件扫描（已在热更） | ✅ 完全安全 |

**这三个程序集的入热更之路毫无障碍——Boot 不引用它们，它们只被已在热更中的 Core 使用。这就是为什么挡 1 纯属配置变更。**

---

## 三、目标架构

### 3.1 最终状态（挡 2 完成后）

```
AOT (IL2CPP，10 个程序集)                  HotUpdate (HybridCLR，11 个程序集)
═══════════════════════════                 ═══════════════════════════

AssetShared.asmdef (🆕 AOT 共享)            Boot.asmdef (原 Boot 改名，现为热更)
  ├─ 引用: (零引用)                           ├─ 引用: Asset, Log, RuntimeLog,
  ├─ AssetConfig.cs, AssetConstants.cs        │         Core, General, UniTask, MessagePipe
  │                                           │
Launcher.asmdef (🆕 AOT Shell)              │
  ├─ 引用: UniTask, YooAsset,                 │
  ├─ Entry.cs                                 ├─ BootUpdateRunner.cs (从 AOT 迁移)
  ├─ BootLoader.cs (🆕 ~180行)               └─ BootRuntimeLogBootstrap.cs (从 AOT 迁移)
  ├─ BootBridge.cs (🆕 ~25行)
  ├─ BootStartupLog.cs (🆕 ~55行)           Asset.asmdef     ✅ 热更
  ├─ YooAssetStrategy/ (🆕)                  Log.asmdef       ✅ 热更
  │   ├─ BootRemoteService.cs                RuntimeLog.asmdef ✅ 热更
  │   ├─ BootBuildinQueryService.cs          Pool.asmdef      ✅ 热更
  │   └─ BootDecryptionService.cs            Cache.asmdef     ✅ 热更
  └─ Data/ (从原 Boot 迁移)                  Event.asmdef     ✅ 热更
      ├─ BootStartupSettings.cs              Core.asmdef      ✅ 热更 (已在)
      ├─ BootAssemblyEntry.cs                General.asmdef   ✅ 热更 (已在)
      ├─ BootMetadataEntry.cs                Project.asmdef   ✅ 热更 (已在)
      └─ IBootStartupView.cs
                                            YooAsset (C++)        ❌
YooAsset (C++)                          ❌    HybridCLR.Runtime (C++) ❌
HybridCLR.Runtime (C++)                 ❌    VContainer            ❌
VContainer                             ❌    UniTask               ❌
UniTask                                ❌    MessagePipe           ❌
MessagePipe                            ❌    ZLogger/ZLinq/ZString ❌
ZLogger / ZLinq / ZString              ❌    TestKit               ❌
TestKit                                ❌    ❌
```

### 3.2 最终 HybridCLR 配置

```yaml
hotUpdateAssemblies:
- Boot           # ← 变成热更了！
- Core
- General
- Project
- Pool           # ← 挡 1 新增
- Cache          # ← 挡 1 新增
- Event          # ← 挡 1 新增
- Asset          # ← 挡 2 新增
- Log            # ← 挡 2 新增
- RuntimeLog     # ← 挡 2 新增

patchAOTAssemblies:  # 不变
- mscorlib
- System
- System.Core
```

### 3.3 启动流对比

**当前：**
```
Entry.Awake()
  └─ BootUpdateRunner.RunAsync()
      ├─ Framework.Log (AOT)
      ├─ Framework.Asset (AOT) — YooAsset 初始化
      ├─ 资源版本检查 + 下载
      ├─ AOT metadata 加载
      ├─ Assembly.Load(Core/General/Project.dll) ← 分割线
      └─ 反射调用 ProjectStartup.Start()
```

**挡 2 后：**
```
Entry.Awake()                                       ← AOT
  └─ BootLoader.RunAsync()                          ← AOT
      ├─ BootStartupLog.Info() — 独立日志            ← AOT (不依赖 Framework.Log)
      ├─ YooAssets.CreatePackage() — YooAsset 原生   ← AOT
      ├─ Host模式: 版本检查 + 下载 + DLL 加载         ← AOT
      ├─ Assembly.Load(全部 10 个热更 DLL)            ← 🔴 分割线
      ├─ 构造 BootBridge(Package, Settings, ...)      ← AOT
      └─ 反射调用 BootUpdateRunner.Start(bridge)      ← 🔵 进入热更

BootUpdateRunner.Start(bridge)                      ← 🔵 热更
  ├─ BootRuntimeLogBootstrap.EnsureInstalled()       ← 现在可以用 Framework API 了
  ├─ ReplayEarlyLogs() 回放 AOT 日志                  ← 补齐日志链
  ├─ AssetRuntime.WrapFromExistingPackage()           ← 接管 Package
  ├─ GameLog.Info(...) — 正常使用 Framework.Log
  ├─ 后续流程不变...
  └─ 反射调用 ProjectStartup.Start()
```

---

## 四、挡 1：Pool / Cache / Event 入热更

### 4.1 改动文件（2 个）

| # | 文件 | 改动 | 行数 |
|---|------|------|------|
| 1 | `ProjectSettings/HybridCLRSettings.asset` | hotUpdateAssemblies 追加 Pool, Cache, Event | +3 |
| 2 | `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | ValidateRuntimePreloadAssemblyName() 删除三个 case | -3 个 case |

### 4.2 改动细节

**文件 1 — HybridCLRSettings.asset：**

```yaml
# 修改前
hotUpdateAssemblies:
- Core
- General
- Project

# 修改后
hotUpdateAssemblies:
- Core
- General
- Project
- Pool
- Cache
- Event
```

**文件 2 — KJHybridClrBuildTools.cs，ValidateRuntimePreloadAssemblyName()：**

```csharp
// 修改前
switch (assemblyName)
{
    case "Boot": case "Asset": case "Event": case "Log":
    case "Pool": case "Cache": case "TestKit":
        throw ...
}

// 修改后
switch (assemblyName)
{
    case "Boot":    // AOT Shell，挡 2 拆分后放行
    case "TestKit": // 非产品代码
        throw new InvalidOperationException(
            $"Assembly '{assemblyName}' is not supported by the current runtime preload " +
            "publication path. This assembly is either an AOT shell (Boot) or a non-product " +
            "assembly (TestKit).");
}
// Asset / Log / RuntimeLog 保留拦截——它们仍被 Boot AOT 引用，等挡 2 拆分后解除
```

### 4.3 安全性验证

| 检查项 | 结论 | 理由 |
|--------|------|------|
| Boot.asmdef 不引用 Pool? | ✅ | references: [Asset, Log, RuntimeLog, UniTask, HybridCLR.Runtime] |
| Boot.asmdef 不引用 Cache? | ✅ | 同上 |
| Boot.asmdef 不引用 Event? | ✅ | 同上 |
| Core 已在热更中? | ✅ | HybridCLRSettings 中已有 Core |
| 热更引用 AOT 正常? | ✅ | HybridCLR 原生支持。Pool 引用 UniTask(AOT) 是标准用法 |
| Pool 泛型桥缺失风险? | ✅ SuperSet 覆盖 | patchAOTAssemblies 含 mscorlib/System/System.Core，覆盖 List<T>/Dictionary<K,V>/HashSet<T> 等 |
| Cache 泛型桥缺失风险? | ✅ | 同上，Dictionary<K,V> + LinkedList<T> 均覆盖 |
| Event 类型扫描正常? | ✅ | GameEventTypeScanner 用反射扫描，HybridCLR 下行为一致 |

**泛型桥补充说明：** 如果运行中出现 `MissingMetadataException` 指向某个 Pool/Cache/Event 的泛型实例化，按需创建 `Framework.Aot` 补充程序集（见附录 A）。挡 1 阶段不预期需要，因为这三个程序集的泛型使用都在 SuperSet 覆盖范围内。

### 4.4 执行步骤

1. 修改 `HybridCLRSettings.asset` — 追加 Pool, Cache, Event
2. 修改 `KJHybridClrBuildTools.cs` — 从拦截 switch 中删除三个 case
3. Editor 执行 `KJ/HybridCLR/Prepare Runtime Assets And Boot` — 验证同步无报错
4. Editor Play — 验证启动链完整：`[AssetSystem] Ready` + `[SystemManager] 全部初始化完成`
5. 检查 Console 和 runtime log 无 `MissingMetadataException`

**挡 1 预估时间：~15 分钟**

---

## 五、挡 2：Boot 拆分 + Asset / Log / RuntimeLog 入热更（HYB-03）

### 5.1 设计核心思想

当前 `Boot.asmdef` 是一个程序集，包含两类完全不同的职责：
- **启动加载器**（必须在 AOT）：Assembly.Load 之前就需要运行的代码
- **更新编排器**（可以热更）：Assembly.Load 之后才运行的代码

挡 2 将其裂变为两个独立程序集，以 Assembly.Load 调用点为分界线：

```
裂变前:   Boot.asmdef (AOT) — 所有东西混在一起
裂变后:   Launcher.asmdef (AOT Shell)  +  Boot.asmdef (HotUpdate)
               ↑ 只做"找到并加载热更代码"          ↑ 做"启动更新流程编排"
```

### 5.2 前置问题：AssetConfig 跨 asmdef 边界共享

这是一个在详细设计阶段发现的额外关键问题。

**问题：** `BootLoader.cs`（AOT）需要读 `AssetConfig` 来决定 PlayMode 和 CDN URL：

```csharp
var config = Resources.Load<AssetConfig>("AssetConfig");
```

但 `AssetConfig` 类定义在 `Assets/Framework/Asset/AssetConfig.cs`，属于 `Asset.asmdef`。挡 2 后 `Asset` 变成热更程序集，**AOT 侧的 `Launcher.asmdef` 无法引用热更程序集**。

**解决方案：提取 `AssetConfig` 和 `AssetConstants` 到一个新的 AOT 共享程序集 `Framework.AssetShared`。**

这是一个极薄的程序集（只含 2 个纯数据类型文件），AOT 和热更双方都可以引用。这是 HybridCLR 处理跨边界共享类型的标准模式。

```
Assets/Framework/AssetShared/          ← 🆕 AOT 共享程序集（约 80 行）
  ├─ Framework.AssetShared.asmdef      ← 零引用
  ├─ AssetConfig.cs                    ← 从 Framework/Asset 移入
  └─ AssetConstants.cs                ← 从 Framework/Asset 移入
```

**asmdef 影响：**
- `Launcher.asmdef` 新增引用 `AssetShared`（AOT 侧，读取 AssetConfig）
- `Asset.asmdef` 新增引用 `AssetShared`（热更侧，保留对 AssetConfig 的使用）
- `AssetShared.asmdef` 零引用，`noEngineReferences=false`（Unity Object 需要）

**注意：** `AssetConfig` 的 `ScriptableObject` 序列化依赖 AssetBundle GUID，但 GUID 绑定在 `.meta` 文件上。通过 Unity 编辑器移动文件可自动保持 `.meta` 不丢失，保证序列化稳定性。

### 5.3 程序集重构方案

#### Launcher.asmdef (AOT Shell)

```
位置: Assets/Scripts/Boot/Launcher/
名称: "Launcher", rootNamespace: "Boot" (保持不变!)
引用: [UniTask, YooAsset, HybridCLR.Runtime, AssetShared]
       ❌ 不引用: Asset, Log, RuntimeLog, Pool, Cache, Event, Core, VContainer, MessagePipe
```

**新建文件：**

| 文件 | 行数 | 职责 |
|------|------|------|
| `BootLoader.cs` | ~180 | 核心启动壳：YooAsset 初始化、版本检查、下载、DLL 加载、反射 |
| `BootBridge.cs` | ~25 | AOT→热更桥梁数据对象 |
| `BootStartupLog.cs` | ~55 | AOT 独立日志（写 boot.log 文件 + 内存缓存条目） |
| `YooAssetStrategy/BootRemoteService.cs` | ~25 | IRemoteService 实现（从 AssetRuntime.CdnRemoteService 提取） |
| `YooAssetStrategy/BootBuildinQueryService.cs` | ~20 | IBuildinQueryServices 默认适配 |
| `YooAssetStrategy/BootDecryptionService.cs` | ~10 | IDecryptionServices 默认实现 |

**从原 Boot 移入文件（不需要改动内容）：**

| 文件 | 说明 |
|------|------|
| `Data/BootStartupSettings.cs` | 纯 POCO |
| `Data/BootAssemblyEntry.cs` | 纯 POCO |
| `Data/BootMetadataEntry.cs` | 纯 POCO |
| `Data/IBootStartupView.cs` | 极简接口 |

**从原 Boot 移入并修改：**

| 文件 | 改动点 |
|------|--------|
| `Entry.cs` | 删除 `BootRuntimeLogBootstrap.EnsureInstalled()`，改为委托给 `BootLoader` |

#### Boot.asmdef (HotUpdate，改名)

```
位置: Assets/Scripts/Boot/ (剩余文件)
名称: "Boot" (保持不变), rootNamespace: "Boot" (保持不变!)
引用: [Asset, Log, RuntimeLog, Pool, Cache, Event, Core, General, UniTask, MessagePipe]
      ← 新增: Pool, Cache, Event, Core, General, MessagePipe
      ← 保留: Asset, Log, RuntimeLog, UniTask
      ← 删除: HybridCLR.Runtime (不需要)
```

**文件：**

| 文件 | 改动 |
|------|------|
| `BootUpdateRunner.cs` | 接收 BootBridge 参数（修改构造函数约 40 行 + 新增 ReplayEarlyLogs） |
| `BootRuntimeLogBootstrap.cs` | 新增 EarlyLogs 回放逻辑 ~10 行 |

#### Framework.Asset 新增方法

| 文件 | 改动 |
|------|------|
| `AssetRuntime.cs` | 新增 `WrapFromExistingPackage(AssetConfig, ResourcePackage)` ~20 行 |
| `IAssetRuntime.cs` | 新增接口方法声明 ~5 行 |

### 5.4 新文件设计详案

#### 5.4.1 BootLoader.cs `Assets/Scripts/Boot/Launcher/BootLoader.cs`

核心启动壳，约 180 行。这是整个方案中最重要的新文件。

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// AOT 极薄启动壳。
    ///
    /// 关键约束：
    /// - 不引用任何 Framework 包（Asset/Log/RuntimeLog/...）
    /// - 不引用 VContainer
    /// - 只用 YooAsset 原生 API 操作资源包
    /// - 用自己的极简日志（BootStartupLog）代替 GameLog
    ///
    /// 职责：初始化 YooAsset → 版本检查 → 下载 → AOT metadata → Assembly.Load → 反射 BootUpdateRunner
    /// </summary>
    public sealed class BootLoader : IDisposable
    {
        private readonly BootStartupSettings _settings;
        private readonly IBootStartupView _view;
        private readonly BootStartupLog _log = new();
        private ResourcePackage _package;
        private bool _disposed;

        public BootStartupLog Log => _log;

        public BootLoader(BootStartupSettings settings, IBootStartupView view)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view;
        }

        public async UniTask RunAsync()
        {
            _log.Info("[BootLoader] Starting");

            var config = Resources.Load<AssetConfig>("AssetConfig");
            if (config == null)
                throw new InvalidOperationException("[BootLoader] AssetConfig not found in Resources.");

            _log.Info($"[BootLoader] PlayMode={config.Mode}, Package={config.PackageName}");

            // 1. 初始化 YooAsset Package（动态选择 PlayMode）
            YooAssets.Initialize();
            var packageName = GetPackageName(config);
            _package = YooAssets.CreatePackage(packageName);
            await InitializePackageByMode(_package, config, packageName);

            // 2. Host 模式：版本检查 + 下载
            if (config.Mode == AssetConfig.PlayMode.Host && _settings.EnableAssetUpdate)
            {
                _log.Info("[BootLoader] Checking resource version");
                _view?.SetStatus("Checking resources");

                var versionOp = _package.RequestPackageVersionAsync(true, config.DownloadTimeout);
                await versionOp.ToUniTask();
                if (versionOp.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException($"[BootLoader] Version check failed: {versionOp.Error}");

                var manifestOp = _package.UpdatePackageManifestAsync(versionOp.PackageVersion);
                await manifestOp.ToUniTask();
                if (manifestOp.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException($"[BootLoader] Manifest update failed: {manifestOp.Error}");

                var tag = string.IsNullOrWhiteSpace(_settings.AssetDownloadTag) ? "hotupdate" : _settings.AssetDownloadTag;
                var downloader = _package.CreateResourceDownloader(new ResourceDownloaderOptions(tag,
                    Math.Max(1, config.DownloadMaxConcurrency), Math.Max(0, config.FailedRetryCount)));

                if (downloader.TotalDownloadCount > 0)
                {
                    _log.Info($"[BootLoader] Downloading {downloader.TotalDownloadCount} files");
                    _view?.SetStatus("Updating resources");
                    downloader.StartDownload();
                    while (!downloader.IsDone)
                    {
                        _view?.SetProgress(Mathf.Lerp(0.1f, 0.5f, downloader.Progress));
                        await UniTask.Yield();
                    }
                    if (downloader.Status != EOperationStatus.Succeeded)
                        throw new InvalidOperationException($"[BootLoader] Download failed: {downloader.Error}");
                }
            }

            _view?.SetProgress(0.5f);

            // 3. 加载 AOT metadata + 热更 DLL
            await LoadHotUpdateCodeAsync();

            // 4. 构建 Bridge 并启动热更层
            _log.Info("[BootLoader] Entering hot-update layer");
            _view?.SetProgress(0.85f);

            var bridge = new BootBridge
            {
                Package = _package,
                Settings = _settings,
                View = _view,
                Config = config,
                EarlyLogs = _log.Entries
            };

            var startupType = Type.GetType("Boot.BootUpdateRunner, Boot", throwOnError: false);
            if (startupType == null)
                throw new InvalidOperationException("[BootLoader] BootUpdateRunner not found. Check hot-update assembly.");

            var method = startupType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("[BootLoader] BootUpdateRunner.Start method not found.");

            method.Invoke(null, new object[] { bridge });
            _view?.SetProgress(1f);
        }

        private async UniTask InitializePackageByMode(ResourcePackage package, AssetConfig config, string packageName)
        {
            InitializePackageOptions options = config.Mode switch
            {
                AssetConfig.PlayMode.EditorSimulate => new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters = FileSystemParameters
                        .CreateDefaultEditorFileSystemParameters(config.EditorSimulatePackageRoot)
                },
                AssetConfig.PlayMode.Offline => new OfflinePlayModeOptions
                {
                    BuiltinFileSystemParameters = FileSystemParameters
                        .CreateDefaultBuiltinFileSystemParameters(packageName)
                },
                AssetConfig.PlayMode.Host => new HostPlayModeOptions
                {
                    BuiltinFileSystemParameters = FileSystemParameters
                        .CreateDefaultBuiltinFileSystemParameters(packageName),
                    CacheFileSystemParameters = BuildSandboxParameters(config, packageName)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(config.Mode))
            };

            var op = package.InitializePackageAsync(options);
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException(
                    $"[BootLoader] Package initialization failed. Mode={config.Mode}, Error={op.Error}");
        }

        private FileSystemParameters BuildSandboxParameters(AssetConfig config, string packageName)
        {
            var cdnBaseUrl = string.IsNullOrWhiteSpace(config.CdnBaseUrl)
                ? "http://127.0.0.1:8080/CDN"
                : config.CdnBaseUrl;
            var parameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
                new BootRemoteService(cdnBaseUrl), packageName);
            parameters.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, config.DownloadMaxConcurrency);
            parameters.AddParameter(EFileSystemParameter.DownloadWatchdogTimeout, config.DownloadTimeout);
            return parameters;
        }

        private async UniTask LoadHotUpdateCodeAsync()
        {
            if (!_settings.EnableHotUpdate)
                return;

#if UNITY_EDITOR
            if (_settings.SkipHotUpdateInEditor)
            {
                _log.Info("[BootLoader] Hot update skipped in Editor");
                return;
            }
#endif

            _log.Info("[BootLoader] Loading AOT metadata");
            await LoadAotMetadataAsync();

            _log.Info("[BootLoader] Loading hot-update assemblies");
            await LoadHotUpdateAssembliesAsync();
        }

        private async UniTask LoadAotMetadataAsync()
        {
            var entries = _settings.AotMetadataAssemblies;
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName)) continue;

                var bytes = await LoadBytesAsync(entry);
                if (bytes == null || bytes.Length == 0) continue;

                var result = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                if (result != LoadImageErrorCode.OK)
                    throw new InvalidOperationException(
                        $"[BootLoader] Load AOT metadata failed: {entry.AssemblyName}, result={result}");
            }
        }

        private async UniTask LoadHotUpdateAssembliesAsync()
        {
            var entries = _settings.HotUpdateAssemblies;
            if (entries == null || entries.Length == 0)
            {
                if (_settings.EnableHotUpdate)
                    throw new InvalidOperationException("[BootLoader] Hot-update assemblies not configured.");
                return;
            }

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName)) continue;
                if (IsAssemblyLoaded(entry.AssemblyName)) continue;

                var bytes = await LoadBytesAsync(entry);
                if (bytes == null || bytes.Length == 0)
                    throw new FileNotFoundException(
                        $"[BootLoader] Hot-update DLL not found: {entry.AssemblyName}");

                _log.Info($"[BootLoader] Loading assembly: {entry.AssemblyName}");
                Assembly.Load(bytes);
            }
        }

        /// <summary>
        /// 加载 DLL bytes。优先级：YooAsset raw → StreamingAssets → Resources。
        /// 继承自当前 BootUpdateRunner.LoadBytesAsync 的逻辑。
        /// </summary>
        private async UniTask<byte[]> LoadBytesAsync(BootAssemblyEntry entry)
        {
            // Priority 1: YooAsset raw asset (最优先)
            if (!string.IsNullOrWhiteSpace(entry.AssetPath))
            {
                var handle = _package.LoadAssetSync<RawFileObject>(entry.AssetPath);
                try
                {
                    if (handle.Status == EOperationStatus.Succeeded)
                    {
                        var rawFile = handle.GetAssetObject<RawFileObject>();
                        var bytes = rawFile?.GetBytes();
                        if (bytes != null && bytes.Length > 0)
                            return bytes;
                    }
                }
                finally { handle.Release(); }
            }

            // Priority 2: StreamingAssets (Android-safe via UnityWebRequest)
            if (!string.IsNullOrWhiteSpace(entry.FileName))
            {
                var path = BuildStreamingPath(entry.FileName);
#if UNITY_ANDROID && !UNITY_EDITOR
                using var request = UnityWebRequest.Get(path);
                request.SendWebRequest();
                while (!request.isDone) await UniTask.Yield();
                if (request.result == UnityWebRequest.Result.Success &&
                    request.downloadHandler.data.Length > 0)
                    return request.downloadHandler.data;
#else
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
#endif
            }

            // Priority 3: Resources fallback
            if (!string.IsNullOrWhiteSpace(entry.ResourcesPath))
            {
                var asset = Resources.Load<TextAsset>(entry.ResourcesPath);
                if (asset != null) return asset.bytes;
            }

            return Array.Empty<byte>();
        }

        private string BuildStreamingPath(string fileName)
        {
            var root = string.IsNullOrWhiteSpace(_settings.StreamingAssetsRoot)
                ? Application.streamingAssetsPath
                : Path.Combine(Application.streamingAssetsPath, _settings.StreamingAssetsRoot);
            return Path.Combine(root, fileName);
        }

        private static bool IsAssemblyLoaded(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                if (string.Equals(a.GetName().Name, name, StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static string GetPackageName(AssetConfig config)
            => string.IsNullOrWhiteSpace(config?.PackageName) ? "DefaultPackage" : config.PackageName;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _log.Dispose();
        }
    }
}
```

#### 5.5.2 BootBridge.cs `Assets/Scripts/Boot/Launcher/BootBridge.cs`

```csharp
using System.Collections.Generic;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// AOT → 热更的桥梁数据对象。
    ///
    /// ResourcePackage 所有权：BootLoader 创建 → 通过 Bridge 传递
    /// → AssetRuntime.WrapFromExistingPackage() 接管 → Shutdown 时清理。
    /// </summary>
    public sealed class BootBridge
    {
        /// <summary>BootLoader 已初始化的 YooAsset ResourcePackage</summary>
        public ResourcePackage Package { get; init; }

        /// <summary>启动配置（来自 Entry 序列化字段）</summary>
        public BootStartupSettings Settings { get; init; }

        /// <summary>启动 UI 视图接口</summary>
        public IBootStartupView View { get; init; }

        /// <summary>资源运行时配置</summary>
        public AssetConfig Config { get; init; }

        /// <summary>AOT 阶段的早期日志（热更层初始化后回放到 RuntimeLog session）</summary>
        public IReadOnlyList<BootStartupLogEntry> EarlyLogs { get; init; }
    }
}
```

#### 5.5.3 BootStartupLog.cs `Assets/Scripts/Boot/Launcher/BootStartupLog.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Boot
{
    /// <summary>
    /// AOT 阶段极简日志。不依赖 Framework.Log 或 Framework.RuntimeLog。
    /// 直接写文本文件 + 存内存缓存。
    /// 热更层初始化后，BootUpdateRunner.ReplayEarlyLogs() 负责回放到 RuntimeLog session。
    /// </summary>
    internal sealed class BootStartupLog
    {
        private readonly List<BootStartupLogEntry> _entries = new();
        private readonly StreamWriter _writer;

        public IReadOnlyList<BootStartupLogEntry> Entries => _entries;

        internal BootStartupLog()
        {
            try
            {
                var dir = Application.isEditor
                    ? Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Logs", "Runtime")
                    : Path.Combine(Application.persistentDataPath, "Logs", "Runtime");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "boot.log");
                _writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
            }
            catch
            {
                // 日志不是关键路径，创建失败不阻塞启动
            }
        }

        public void Info(string message) => Write("INFO", message);
        public void Error(string message) => Write("ERROR", message);

        private void Write(string level, string message)
        {
            var entry = new BootStartupLogEntry
            {
                TimeUtc = DateTimeOffset.UtcNow,
                Level = level,
                Message = message
            };
            _entries.Add(entry);
            try { _writer?.WriteLine($"[{entry.TimeUtc:O}] [{level}] {message}"); }
            catch { /* 静默失败 */ }
        }

        public void Dispose()
        {
            try { _writer?.Dispose(); } catch { }
        }
    }

    public sealed class BootStartupLogEntry
    {
        public DateTimeOffset TimeUtc { get; init; }
        public string Level { get; init; }
        public string Message { get; init; }
    }
}
```

#### 5.5.4 BootRemoteService.cs `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootRemoteService.cs`

```csharp
using System.Collections.Generic;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// AOT 侧 IRemoteService 实现。
    /// 从 AssetRuntime.CdnRemoteService 提取，允许热更层通过
    /// <see cref="CustomUrlProvider"/> 注入动态 CDN 切换或 Token 附加逻辑。
    /// </summary>
    public sealed class BootRemoteService : IRemoteService
    {
        public static System.Func<string, IReadOnlyList<string>> CustomUrlProvider;

        private readonly string _baseUrl;

        public BootRemoteService(string baseUrl)
            => _baseUrl = (baseUrl ?? "").TrimEnd('/');

        public IReadOnlyList<string> GetRemoteUrls(string fileName)
            => CustomUrlProvider?.Invoke(fileName)
               ?? new[] { $"{_baseUrl}/{fileName}" };
    }
}
```

#### 5.5.5 BootBuildinQueryService.cs `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootBuildinQueryService.cs`

```csharp
using System.IO;
using UnityEngine;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// AOT 侧 IBuildinQueryServices 默认实现。
    /// </summary>
    public sealed class BootBuildinQueryService : IBuildinQueryServices
    {
        public bool QueryStreamingAssets(string packageName, string fileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, packageName, fileName);
#if UNITY_ANDROID && !UNITY_EDITOR
            return true; // YooAsset 的 builtin FS 在加载时处理
#else
            return File.Exists(path);
#endif
        }
    }
}
```

#### 5.5.6 BootDecryptionService.cs `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootDecryptionService.cs`

```csharp
using System.IO;
using UnityEngine;
using YooAsset;

namespace Boot
{
    /// <summary>
    /// AOT 侧 IDecryptionServices 默认实现（无加密）。
    /// </summary>
    public sealed class BootDecryptionService : IDecryptionServices
    {
        public AssetBundle LoadAssetBundle(string bundleName, Stream bundleStream, uint offset)
            => AssetBundle.LoadFromStream(bundleStream, offset);

        public AssetBundleCreateRequest LoadAssetBundleAsync(string bundleName, Stream bundleStream, uint offset)
            => AssetBundle.LoadFromStreamAsync(bundleStream, offset);
    }
}
```

### 5.5 修改文件详案

#### 5.5.1 Entry.cs — 简化委托

当前 `Entry.cs` 在 `Assets/Scripts/Boot/`，需移到 `Assets/Scripts/Boot/Launcher/` 下。

关键改动：
- 删除 `BootRuntimeLogBootstrap.EnsureInstalled()` — AOT 用 BootStartupLog 替代
- 删除 `Framework.Log` / `Framework.RuntimeLog` 引用（通过 asmdef 硬约束）
- 用 `BootLoader` 替代 `BootUpdateRunner`

```csharp
// 修改后的 Entry.cs（关键部位）
private void Awake()
{
    // 不再调用 BootRuntimeLogBootstrap.EnsureInstalled()
    // AOT 日志由 BootLoader 内部的 BootStartupLog 独立处理
    DontDestroyOnLoad(gameObject);
    RunStartupAsync().Forget();
}

private async UniTaskVoid RunStartupAsync()
{
    _isRunning = true;
    var view = startupView as IBootStartupView;
    _loader?.Dispose();
    _loader = new BootLoader(startupSettings, view);

    try
    {
        await _loader.RunAsync();
    }
    catch (Exception e)
    {
        _loader?.Log.Error($"Startup failed: {e}");
        view?.SetStatus("Startup failed");
        view?.SetRepairVisible(true);
    }
    finally
    {
        _isRunning = false;
    }
}
```

#### 5.5.2 BootUpdateRunner.cs — 接收 BootBridge

文件保留在 `Assets/Scripts/Boot/`（现在属于热更 asmdef）。

关键改动：
- 构造函数改为接收 `BootBridge`（不再自己创建 AssetRuntime）
- 新增 `Start(BootBridge)` 静态入口 → 由 BootLoader 反射调用
- 新增 `ReplayEarlyLogs()` → 回放 AOT 日志
- 恢复使用 `GameLog` / `RuntimeLogManager` / `IAssetRuntime`（现在在热更侧可以自由使用）

```csharp
namespace Boot
{
    public sealed class BootUpdateRunner : IDisposable
    {
        private readonly BootBridge _bridge;
        private readonly IAssetRuntime _assetRuntime;
        private bool _assetRuntimeTransferred;

        /// <summary>
        /// 静态入口。由 BootLoader(AOT) 通过反射调用。
        /// </summary>
        public static void Start(BootBridge bridge)
        {
            var runner = new BootUpdateRunner(bridge);
            runner.RunAsync().Forget();
        }

        public BootUpdateRunner(BootBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            var settings = bridge.Settings;
            var config = bridge.Config;

            _assetRuntime = AssetRuntimeFactory.Create();

            if (config != null && bridge.Package != null &&
                config.Mode == AssetConfig.PlayMode.Host)
            {
                // Host 模式：接管 BootLoader 已初始化的 Package
                _assetRuntime.WrapFromExistingPackage(config, bridge.Package);
            }
            else
            {
                // EditorSimulate / Offline：AssetRuntime 自行初始化
                var initHandle = _assetRuntime.BeginInitialize(config);
                // 这些模式下初始化是同步完成的
            }
        }

        public async UniTask RunAsync()
        {
            // 1. 安装 RuntimeLog session（现在可以用 Framework API 了）
            BootRuntimeLogBootstrap.EnsureInstalled(_bridge.Settings);

            // 2. 回放 AOT 阶段的早期日志
            ReplayEarlyLogs();

            GameLog.Info("[Boot] Startup begin", "Boot");
            // ... 后续流程与当前基本一致 ...
        }

        private void ReplayEarlyLogs()
        {
            if (_bridge.EarlyLogs == null || _bridge.EarlyLogs.Count == 0) return;

            var session = RuntimeLogManager.Current;
            if (session == null) return;

            foreach (var entry in _bridge.EarlyLogs)
            {
                session.Write(new RuntimeLogEntry
                {
                    Level = entry.Level == "ERROR" ? GameLogLevel.Error : GameLogLevel.Information,
                    Module = "Boot.AOT",
                    Category = "Boot.BootLoader",
                    Phase = "Boot.AOT",
                    Message = entry.Message,
                    TimeUtc = entry.TimeUtc
                });
            }
            GameLog.Info($"[Boot] Replayed {_bridge.EarlyLogs.Count} AOT-stage early logs", "Boot");
        }
    }
}
```

#### 5.5.3 BootRuntimeLogBootstrap.cs — 新增 EarlyLogs 回放

文件保留在 `Assets/Scripts/Boot/`（现在属于热更 asmdef）。原有逻辑基本不变（现在可以自由使用 Framework API），新增对 `BootBridge.EarlyLogs` 回放的支持。

#### 5.5.4 AssetRuntime.cs — 新增 WrapFromExistingPackage

```csharp
// 在 AssetRuntime 类中新增方法
/// <summary>
/// 从已初始化的 YooAsset ResourcePackage 构建 AssetRuntime。
/// 用于 Host 模式：BootLoader (AOT) 创建并初始化 Package，
/// 热更层 BootUpdateRunner 通过此方法将其移交给 AssetRuntime。
///
/// 所有权转移：调用者不再持有 Package 引用；AssetRuntime.Shutdown() 负责清理。
/// </summary>
public void WrapFromExistingPackage(AssetConfig config, ResourcePackage existingPackage)
{
    if (existingPackage == null)
        throw new ArgumentNullException(nameof(existingPackage));
    if (config == null)
        throw new ArgumentNullException(nameof(config));
    if (IsReady)
        throw new InvalidOperationException("AssetRuntime is already initialized.");

    _config = config;
    _defaultPackage = existingPackage;
    _downloadMaxConcurrency = Math.Max(1, config.DownloadMaxConcurrency);
    _failedRetryCount = Math.Max(0, config.FailedRetryCount);
    lock (_gate) { _lifecycleVersion++; }
    IsReady = true;
    LastError = string.Empty;
}
```

#### 5.5.5 IAssetRuntime.cs — 新增接口方法

```csharp
/// <summary>
/// 从已初始化的 YooAsset ResourcePackage 构建 AssetRuntime。
/// 仅 Host 模式使用。ResourcePackage 所有权转移给 IAssetRuntime。
/// </summary>
void WrapFromExistingPackage(AssetConfig config, ResourcePackage existingPackage);
```

#### 5.5.6 KJHybridClrBuildTools.cs — 更新白名单

```csharp
// 挡 2 后
switch (assemblyName)
{
    case "Launcher": // AOT Shell，永远不进热更
    case "TestKit":  // 非产品代码
        throw new InvalidOperationException(
            $"Assembly '{assemblyName}' is an AOT shell or test-only assembly. " +
            "It must not appear in the hot-update publication list.");
}
```

#### 5.5.7 HybridCLRSettings.asset

```yaml
hotUpdateAssemblies:
- Boot
- Core
- General
- Project
- Pool
- Cache
- Event
- Asset
- Log
- RuntimeLog
```

### 5.6 目录结构变化

```
当前
Assets/Scripts/Boot/                  → Boot.asmdef (AOT)
├── Entry.cs, BootUpdateRunner.cs, ...
└── Boot.Editor/                      → Boot.Editor.asmdef

挡 2 后
Assets/Scripts/Boot/
├── Launcher/                         → Launcher.asmdef (AOT)
│   ├── Entry.cs                      (修改)
│   ├── BootLoader.cs                 (新建)
│   ├── BootBridge.cs                 (新建)
│   ├── BootStartupLog.cs             (新建)
│   ├── YooAssetStrategy/             (新建)
│   │   ├── BootRemoteService.cs
│   │   ├── BootBuildinQueryService.cs
│   │   └── BootDecryptionService.cs
│   └── Data/                         (从原 Boot 移入)
│       ├── BootStartupSettings.cs
│       ├── BootAssemblyEntry.cs
│       ├── BootMetadataEntry.cs
│       └── IBootStartupView.cs
├── KJ.Boot.asmdef                    → Boot.asmdef (HotUpdate)
├── BootUpdateRunner.cs               (修改)
├── BootRuntimeLogBootstrap.cs        (修改)
└── Boot.Editor/                      → Boot.Editor.asmdef (不变)
    ├── KJHybridClrBuildTools.cs      (修改白名单)
    └── Build/
```

### 5.7 边界问题解决方案

#### 问题 1：YooAsset "鸡与蛋" 死锁 → ✅ 解决

**问题：** Asset.dll 在热更中，但 BootLoader(AOT) 需要 IRemoteService 来初始化 YooAsset 去下载 Asset.dll。

**解决：** 把 IRemoteService / IBuildinQueryServices / IDecryptionServices 的实现从 `Framework.Asset` 提取到 AOT 侧。BootLoader 用 AOT 侧的 `BootRemoteService` 等初始化 YooAsset。

#### 问题 2：Editor 模拟模式 → ✅ 解决

`BootLoader.InitializePackageByMode()` 根据 `AssetConfig.Mode` 动态路由：
- `EditorSimulate` → `EditorSimulateModeOptions`
- `Offline` → `OfflinePlayModeOptions`
- `Host` → `HostPlayModeOptions` + `BootRemoteService(AOT)`

Editor 中 `SkipHotUpdateInEditor = true` 时跳过 Assembly.Load，但仍反射调用 BootUpdateRunner（热更 asmdef 在 Editor 中直接编译）。

#### 问题 3：程序集名冲突 → ✅ 解决

`Launcher.asmdef` → `Launcher.dll`（AOT），`KJ.Boot.asmdef` → `Boot.dll`（HotUpdate）。名称不同，不冲突。

#### 问题 4：早期日志黑洞 → ✅ 解决

`BootStartupLog` → `BootBridge.EarlyLogs` → `BootUpdateRunner.ReplayEarlyLogs()` → `RuntimeLogSession.Write()` 完整回放链。AOT 崩溃时也可从 `boot.log` 文件排查。

### 5.8 任务分解与执行顺序

```
Phase A — 基础设施（可并行）
  [A1] 新建 Assets/Scripts/Boot/Launcher/ 目录
  [A2] 新建 KJ.Launcher.asmdef (AOT asmdef)
  [A3] Framework.Asset 新增方法
        - AssetRuntime.WrapFromExistingPackage()
        - IAssetRuntime.WrapFromExistingPackage()
  [A4] KJHybridClrBuildTools 白名单更新

Phase B — 纯数据迁移（可并行，依赖 A1）
  [B1] 移动 Data/ 文件到 Launcher/Data/
        - BootStartupSettings.cs, BootAssemblyEntry.cs,
          BootMetadataEntry.cs, IBootStartupView.cs
  [B2] 新建 BootBridge.cs
  [B3] 新建 BootStartupLog.cs
  [B4] 新建 YooAssetStrategy/ 三个策略类

Phase C — 核心逻辑（依赖 A+B）
  [C1] 新建 BootLoader.cs
  [C2] 修改 Entry.cs（委托给 BootLoader）

Phase D — 热更化改造（依赖 A）
  [D1] 更新 KJ.Boot.asmdef references（热更化）
  [D2] 修改 BootUpdateRunner.cs（接收 Bridge + ReplayEarlyLogs）
  [D3] 修改 BootRuntimeLogBootstrap.cs

Phase E — 配置 + 验证
  [E1] 更新 HybridCLRSettings.asset
  [E2] Unity 编译验证（Launcher.asmdef 不引用任何 Framework 包）
  [E3] Editor Play 启动链验证
  [E4] KJ/HybridCLR/Prepare Runtime Assets And Boot 验证
```

### 5.9 验证清单

| # | 验证项 | 验证方法 |
|---|--------|---------|
| 1 | Launcher.asmdef 不引用任何 Framework 包 | 检查 asmdef references 列表 |
| 2 | Launcher.asmdef 不引用任何热更程序集 | 检查 asmdef references 列表 |
| 3 | BootLoader 无 Framework 引用 | 代码审查（不能有 using Framework.* / GameLog / RuntimeLogManager 等） |
| 4 | BootLoader 只用 YooAsset 原生 API | 代码审查（LoadAssetSync<RawFileObject>、package.InitializePackageAsync 等） |
| 5 | Editor Play (EditorSimulate) 启动链完整 | Editor Play，日志含 `[AssetSystem] Ready` |
| 6 | Editor Play (Host + Skip) 正常 | Editor Play，日志含 SkipHotUpdateInEditor |
| 7 | HybridCLR Generate 后 DLL 列表正确 | 同步后的 Dlls/ 含全部 10 个程序集 |
| 8 | BootBridge.EarlyLogs 回放正常 | boot.log 内容出现在 latest.jsonl |
| 9 | AssetRuntime.WrapFromExistingPackage 正确 | Host 模式 IsReady=true，所有操作正常 |
| 10 | 无 MissingMetadataException | Console 和 runtime log |

---

## 六、附录

### 附录 A：Framework.Aot 补充程序集（按需，挡 2 后）

当热更代码创建了 IL2CPP AOT 未预见的泛型实例化时，`MissingMetadataException` 会在运行时抛出。此时需要创建补充 AOT 程序集声明对应的泛型组合。

```
目录: Assets/Framework/Aot/
asmdef: Framework.Aot.asmdef
  引用: Pool, Cache, Event, Asset, Log, RuntimeLog, HybridCLR.Runtime

// Framework.Aot 示例（按需追加）:
namespace Framework.Aot
{
    internal static class PoolAot
    {
        // 声明需 AOT 生成 metadata 的泛型组合
        private static ObjectPool<SomeType> _bridge;
    }
}
```

**触发条件：** 运行时 `MissingMetadataException` 指向具体泛型实例化时创建。挡 1 和挡 2 开发阶段不预期需要。

### 附录 B：软重启能力（后续规划）

37 项目的 `GameLife/GameRestart.cs` 支持不退出进程重新加载 DLL——"需要重启生效"的热更变更，游戏内重新走 Boot → Core 初始化链路即可，不必退出 App。

这是挡 2 之后的下一个能力点（ROADMAP 中未编号），不在本文档范围内。

### 附录 C：所有改动文件汇总

**挡 1：**

| 文件 | 类型 | 改动 |
|------|------|------|
| `ProjectSettings/HybridCLRSettings.asset` | 修改 | +3 行 |
| `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | 修改 | -3 case |

**挡 2：**

| 文件 | 类型 | 行数(估) |
|------|------|---------|
| `Assets/Framework/AssetShared/Framework.AssetShared.asmdef` | 🆕 新建 asmdef | ~10 |
| `Assets/Framework/AssetShared/AssetConfig.cs` | 🔀 从 Asset 移入 | 0 |
| `Assets/Framework/AssetShared/AssetConstants.cs` | 🔀 从 Asset 移入 | 0 |
| `Assets/Framework/Asset/Asset.asmdef` | 修改 (+AssetShared 引用) | +1 |
| `Assets/Scripts/Boot/Launcher/KJ.Launcher.asmdef` | 🆕 新建 asmdef | ~12 |
| `Assets/Scripts/Boot/Launcher/BootLoader.cs` | 🆕 新建 | ~180 |
| `Assets/Scripts/Boot/Launcher/BootBridge.cs` | 🆕 新建 | ~25 |
| `Assets/Scripts/Boot/Launcher/BootStartupLog.cs` | 🆕 新建 | ~55 |
| `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootRemoteService.cs` | 🆕 新建 | ~25 |
| `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootBuildinQueryService.cs` | 🆕 新建 | ~20 |
| `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootDecryptionService.cs` | 🆕 新建 | ~10 |
| `Assets/Scripts/Boot/Launcher/Entry.cs` | 🔀 移入+修改 | ~10 改动 |
| `Assets/Scripts/Boot/Launcher/Data/BootStartupSettings.cs` | 🔀 移入 | 0 |
| `Assets/Scripts/Boot/Launcher/Data/BootAssemblyEntry.cs` | 🔀 移入 | 0 |
| `Assets/Scripts/Boot/Launcher/Data/BootMetadataEntry.cs` | 🔀 移入 | 0 |
| `Assets/Scripts/Boot/Launcher/Data/IBootStartupView.cs` | 🔀 移入 | 0 |
| `Assets/Scripts/Boot/KJ.Boot.asmdef` | 修改 (+热更引用) | ~5 |
| `Assets/Scripts/Boot/BootUpdateRunner.cs` | 修改 (+Bridge/Replay) | ~40 |
| `Assets/Scripts/Boot/BootRuntimeLogBootstrap.cs` | 修改 (+EarlyLogs) | ~10 |
| `Assets/Framework/Asset/AssetRuntime.cs` | 修改 (+Wrap方法) | ~20 |
| `Assets/Framework/Asset/IAssetRuntime.cs` | 修改 (+接口方法) | ~5 |
| `Assets/Scripts/Boot.Editor/Boot.Editor.asmdef` | 修改 (+Launcher/AssetShared) | +2 |
| `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | 修改 (白名单) | ~3 |
| `ProjectSettings/HybridCLRSettings.asset` | 修改 (热更列表) | +7 |

### 附录 D：参考来源

| 来源 | 内容 |
|------|------|
| `ProgressDoc/Archive/资源系统/HybridCLR热更程序集范围分析.md` | 前置分析与讨论（2026-07-06） |
| `.planning/HOT_UPDATE_BOUNDARY.md` | HYB-00 热更边界固化 |
| `.planning/ROADMAP.md` | HYB-03 定义 + 依赖关系 |
| `.planning/STATE.md` | 当前文件清单与验证记录 |
| `.planning/PROJECT.md` | 架构设计总纲 |
| `.planning/目录结构规范.md` | 目录/命名空间/asmdef 命名规范 |
| `ProjectSettings/HybridCLRSettings.asset` | 当前热更程序集配置 |
| `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | 构建工具完整逻辑 |
| 37 项目 `CODE_MAP.md` | Boot 层成熟产品参考 |
