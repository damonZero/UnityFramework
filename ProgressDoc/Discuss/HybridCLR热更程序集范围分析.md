# HybridCLR 热更程序集范围分析

> 讨论时间：2026-07-06
> 结论：Framework 层应尽可能进入热更程序集，仅受限于 Boot 启动时序依赖。目标是最小化 AOT 面、最大化热更覆盖。

---

## 一、背景

当前 HybridCLR 热更程序集配置为 `Core` / `General` / `Project` 三个。Framework 层（Pool、Cache、Event、Log、RuntimeLog、Asset）以及 Boot 全部编入 AOT。这种划分是 Phase 0 起步阶段的保守配置，并非最终形态。

核心目标：**除了确实无法热更的原生代码（YooAsset / HybridCLR Runtime / Unity Engine / 第三方 native 插件），其余全部可热更。** 拆分最小 Boot 的意义正在于此——让热更不发版成为常态。

---

## 二、所有程序集分层

### 2.1 当前状态

```
AOT (编译进 Native, 不可热更)                 HotUpdate (HybridCLR 解释, 可热更)
─────────────────────────────────────        ──────────────────────────────
Boot           ← 启动壳，最先运行              Core            ← 系统管理器
                                              General         ← 业务
Framework:                                    Project         ← 项目专属
  Pool          ← 对象池
  Cache         ← 缓存
  Event         ← 事件基础
  Log           ← 日志门面
  RuntimeLog    ← AI 运行日志
  Asset         ← YooAsset 适配
  TestKit       ← 测试基础设施（非产品代码）

Packages (第三方):
  VContainer    ← DI 容器
  UniTask       ← 异步
  MessagePipe   ← 消息总线
  YooAsset      ← 资源管理 (含 native C++)
  HybridCLR.Runtime ← 解释引擎 (含 native C++)
  ZLogger       ← 日志库
  ZLinq         ← LINQ 零分配
  ZString       ← 字符串构建
```

### 2.2 当前 HybridCLR 配置

`ProjectSettings/HybridCLRSettings.asset`:

```yaml
hotUpdateAssemblies:
- Core
- General
- Project
```

`KJHybridClrBuildTools.ValidateRuntimePreloadAssemblyName()` 拦截了以下程序集：

```csharp
case "Boot":
case "Asset":
case "Event":
case "Log":
case "Pool":
case "Cache":
case "TestKit":
    throw new InvalidOperationException("...");
```

---

## 三、分类决策

### 3.1 永远不进热更（原生依赖或非产品代码）

| 程序集 | 原因 |
|--------|------|
| YooAsset | 含原生 C++ 文件系统/资源加载引擎 |
| HybridCLR.Runtime | 解释引擎本身，C++ 实现 |
| Unity Engine | 整个引擎都是 C++ |
| TestKit | 测试基础设施，非运行时产品代码 |

### 3.2 保持 AOT 但非技术限制（业务判断）

| 程序集 | 原因 |
|--------|------|
| VContainer | 纯 C# 技术上可热更，但 DI 容器极度稳定、升级频率极低、AOT 执行效率更高。热更收益 ≈ 0 |
| UniTask | 同上，纯 C# 但极度稳定，AOT 效率更高 |
| MessagePipe | 同上 |
| ZLogger / ZLinq / ZString | 同上 |

> 三方纯 C# 库统一策略：**保持 AOT**。原因：极度稳定几乎不改，AOT 执行效率高于 HybridCLR 解释执行，不存在"需要热更不发版"的场景。

### 3.3 当前可进热更（Pool / Cache / Event）

这三个 Framework 程序集 **Boot 不引用，仅在 Core 层被使用**，不存在启动时序问题：

- `Pool` — 被 `Core.PoolService` 使用
- `Cache` — 被 Core 系统使用
- `Event` — 被 Core/General 事件系统使用

### 3.4 需要 HYB-03 后才能进热更（Asset / Log / RuntimeLog）

这三个 Framework 程序集 **被 Boot 直接引用且先于热更加载被调用**，需要 Boot 拆分后释放。

详见下述 HYB-03 设计。

---

## 四、HYB-03：Boot 拆分为极薄 BootLoader + 热更 BootUpdate

### 4.1 问题分析

当前 `BootUpdateRunner` 里，热更 DLL 加载前后都有对 `Asset` / `Log` / `RuntimeLog` 的调用：

```
Entry.Awake()
├─ BootRuntimeLogBootstrap.EnsureInstalled()
│  └─ RuntimeLogManager.InstallIfNone()   ← RuntimeLog (AOT)  ← 无法热更
│     └─ Resources.Load<AssetConfig>()    ← 访问 Asset
        
└─ RunAsync()
   ├─ InitializeAssetsAsync()
   │  └─ AssetRuntimeFactory.Create()     ← Asset (AOT)        ← 无法热更
   │  └─ _assetRuntime.BeginInitialize()  ← Asset (AOT)
   │  └─ GameLog.Info(...)                ← Log (AOT)          ← 无法热更
   │
   ├─ UpdateAssetsAsync()
   │  └─ _assetRuntime.UpdateManifest()   ← Asset (AOT)
   │  └─ _assetRuntime.CreateDownloader() ← Asset (AOT)
   │
   ├─ LoadHotUpdateCodeAsync()
   │  └─ _assetRuntime.LoadRawBytes(...)  ← Asset (AOT)
   │  └─ Assembly.Load(bytes)            ← 🔴 热更 DLL 加载点
   │
   └─ StartGame()
      └─ 反射调用 ProjectStartup.Start()  ← 🔵 热更层入口
```

**热更 DLL 加载点就是分割线：其前的代码必须是 AOT，其后的代码可以热更。**

### 4.2 拆分后架构

```
┌───────────────────────────────────────────────────────────────────┐
│                        AOT (编译进 Native)                         │
│                                                                   │
│  Entry.cs                     启动 MonoBehaviour                   │
│  BootLoader.cs                极薄启动壳                           │
│  BootStartupSettings.cs       序列化配置（不依赖任何 Framework）   │
│  IBootStartupView.cs          启动 UI 接口                         │
│                                                                   │
│  只引用:                                                          │
│    YooAsset        (资源初始化 + 下载)                            │
│    HybridCLR.Runtime (LoadMetadata / Assembly.Load)               │
│    UniTask         (异步)                                         │
│                                                                   │
│  不引用: 任何 Framework 包 (Asset/Log/RuntimeLog/...)              │
│                                                                   │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  BootLoader.RunAsync():  ← 只用 YooAsset 原生 API，不用 Framework  │
│                                                                   │
│  ┌─ 1. BootStartupLog.cs  ← 极简 AOT 日志（直接写文件）           │
│  ├─ 2. YooAssets.Initialize() + CreatePackage                     │
│  │     直接调 YooAsset API，不走 Framework.Asset.Runtime          │
│  ├─ 3. package.RequestPackageVersionAsync()                       │
│  ├─ 4. downloader = package.CreateResourceDownloader()            │
│  ├─ 5. downloader.StartDownload() → 下载热更资源                  │
│  ├─ 6. package.LoadAssetSync<RawFileObject>(dllPath)              │
│  │     直接读取 RawFile bytes                                     │
│  ├─ 7. RuntimeApi.LoadMetadataForAOTAssembly()                    │
│  └─ 8. Assembly.Load(bytes)                                       │
│                                                                   │
│  🔴—— 热更分割线 ——🔴                                              │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────┐
│                    HotUpdate (HybridCLR 解释)                       │
│                                                                   │
│  BootUpdateRunner.cs           启动更新编排（热更版）              │
│  BootRuntimeLogBootstrap.cs    运行日志安装（热更版）              │
│  Asset / Log / RuntimeLog      ← 现在可以热更了！                 │
│                                                                   │
│  引用: Framework 全家 + Core + General + Project                  │
│                                                                   │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  BootUpdateRunner.RunAsync():  ← 现在用 Framework API 了          │
│                                                                   │
│  ├─ BootRuntimeLogBootstrap.EnsureInstalled()                     │
│  │   └─ RuntimeLogManager / GameLog.Sink / AssetConfig 读取      │
│  ├─ 接收 BootLoader 传入的 ResourcePackage                        │
│  │   └─ AssetRuntime.WrapFromExistingPackage()                    │
│  ├─ 业务级启动逻辑...                                             │
│  └─ 反射调用 ProjectStartup                                       │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

### 4.3 关键接口变化

#### BootLoader (AOT) — 极简化（修正：使用 AssetConfig 动态选择 PlayMode）

```csharp
namespace Boot
{
    public class BootLoader
    {
        private readonly BootStartupLog _log = new();
        
        public async UniTask<BootBridge> RunAsync()
        {
            _log.Info("[BootLoader] Starting");
            var config = Resources.Load<AssetConfig>("AssetConfig");

            // 1. 初始化 YooAsset Package（动态选择 PlayMode）
            YooAssets.Initialize();
            var package = YooAssets.CreatePackage(config.PackageName);
            await InitializePackageByMode(package, config);

            // 2. 版本检查 + 下载 Host 模式的 Manifest/Bundle
            //    注意：EditorSimulate / Offline 模式不需要此步
            if (config.Mode == AssetConfig.PlayMode.Host)
            {
                var versionOp = package.RequestPackageVersionAsync();
                await versionOp.ToUniTask();
                var manifestOp = package.UpdatePackageManifestAsync(versionOp.PackageVersion);
                await manifestOp.ToUniTask();
                var downloader = package.CreateResourceDownloader(tags: "bootstrap");
                downloader.StartDownload();
                while (!downloader.IsDone) { await UniTask.Yield(); }
            }

            // 3. 加载 AOT metadata + 热更 DLL
            await LoadMetadataAndHotUpdateDlls(package);

            // 4. Bridge: 把 ResourcePackage + 早期日志传给热更层
            var bridge = new BootBridge
            {
                Package = package,
                Settings = _settings,
                View = _view,
                EarlyLogs = _log.Entries  // 🆕 回放早期日志
            };

            // 5. 反射启动热更层
            var startupType = Type.GetType("Boot.BootUpdateRunner, Boot");
            var method = startupType.GetMethod("Start",
                BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { bridge });
            
            return bridge;
        }
        
        private async UniTask InitializePackageByMode(
            ResourcePackage package, AssetConfig config)
        {
            switch (config.Mode)
            {
#if UNITY_EDITOR
                case AssetConfig.PlayMode.EditorSimulate:
                    await package.InitializePackageAsync(
                        new EditorSimulateModeOptions
                        {
                            EditorFileSystemParameters = FileSystemParameters
                                .CreateDefaultEditorFileSystemParameters(
                                    config.EditorSimulatePackageRoot)
                        }).ToUniTask();
                    break;
#endif
                case AssetConfig.PlayMode.Offline:
                    await package.InitializePackageAsync(
                        new OfflinePlayModeOptions
                        {
                            BuiltinFileSystemParameters = FileSystemParameters
                                .CreateDefaultBuiltinFileSystemParameters(
                                    config.PackageName)
                        }).ToUniTask();
                    break;

                case AssetConfig.PlayMode.Host:
                    var builtinParams = FileSystemParameters
                        .CreateDefaultBuiltinFileSystemParameters(
                            config.PackageName);
                    var sandboxParams = FileSystemParameters
                        .CreateDefaultSandboxFileSystemParameters(
                            new BootRemoteService(config),  // ← AOT 侧策略
                            config.PackageName);
                    sandboxParams.AddParameter(
                        EFileSystemParameter.DownloadMaxConcurrency,
                        config.DownloadMaxConcurrency);
                    sandboxParams.AddParameter(
                        EFileSystemParameter.DownloadWatchdogTimeout,
                        config.DownloadTimeout);
                    await package.InitializePackageAsync(
                        new HostPlayModeOptions
                        {
                            BuiltinFileSystemParameters = builtinParams,
                            CacheFileSystemParameters = sandboxParams
                        }).ToUniTask();
                    break;
            }
        }
    }

    // AOT 侧独立 URL 拼接策略（从 Framework.Asset 提取出来）
    public sealed class BootRemoteService : IRemoteService
    {
        /// <summary>
        /// 热更层可在启动后注入自定义 URL 提供商（动态 CDN、Token 附加等）。
        /// 为 null 时回退到 AssetConfig.CdnBaseUrl 拼接。
        /// </summary>
        public static Func<string, IReadOnlyList<string>>? CustomUrlProvider;

        private readonly string _baseUrl;

        public BootRemoteService(AssetConfig config)
            => _baseUrl = (config?.CdnBaseUrl ?? "http://127.0.0.1:8080/CDN")
                .TrimEnd('/');

        public IReadOnlyList<string> GetRemoteUrls(string fileName)
            => CustomUrlProvider?.Invoke(fileName)
                ?? new[] { $"{_baseUrl}/{fileName}" };
    }

    // AOT 侧可回放的启动日志
    internal class BootStartupLog
    {
        private readonly List<BootStartupLogEntry> _entries = new();
        private readonly StreamWriter _writer;

        public IReadOnlyList<BootStartupLogEntry> Entries => _entries;

        // ... 写文件 + 存 _entries
    }
}
```

#### Framework.Asset 新增接口（从已有 Package 构建 AssetRuntime）

```csharp
// AssetRuntime.cs 新增
public sealed class AssetRuntime : IAssetRuntime, IAssetSystem
{
    // 现有：从零初始化（Editor/Offline 模式仍用此路径）
    public AssetInitializeHandle BeginInitialize(AssetConfig config) { ... }
    
    // 🆕 新增：复用 BootLoader 已初始化的 YooAsset Package（Host 模式）
    // Package 从 BootLoader(BootBridge) 传入，AssetRuntime 接管所有权
    public void WrapFromExistingPackage(AssetConfig config, ResourcePackage existingPackage)
    {
        if (existingPackage == null) throw new ArgumentNullException(nameof(existingPackage));
        _config = config;
        _defaultPackage = existingPackage;
        _downloadMaxConcurrency = Math.Max(1, config.DownloadMaxConcurrency);
        _failedRetryCount = Math.Max(0, config.FailedRetryCount);
        lock (_gate) { _lifecycleVersion++; }
        IsReady = true;
    }
}
```

**⚠️ 注意：`BeginInitialize` 仍然保留**——EditorSimulate 和 Offline 模式下，资源从本地加载，不存在"鸡与蛋"问题，`Framework.Asset` 可以自行初始化。只有 Host 模式需要通过 `WrapFromExistingPackage` 桥接。具体选择由 `BootUpdateRunner` 根据 `AssetConfig.Mode` 决定。

#### BootUpdateRunner (热更版) — 从桥接启动

```csharp
namespace Boot  // ← 现在这是热更 DLL！
{
    public class BootUpdateRunner
    {
        // 从 BootLoader 接收已就绪的 YooAsset Package
        public static async UniTaskVoid Start(BootBridge bridge)
        {
            var runner = new BootUpdateRunner(bridge);
            await runner.RunAsync();
        }
        
        public BootUpdateRunner(BootBridge bridge)
        {
            _settings = bridge.Settings;
            _view = bridge.View;
            
            // 🆕 用现有 Package 构建 AssetRuntime（不走 Init 流程）
            var assetRuntime = AssetRuntimeFactory.Create();
            assetRuntime.WrapFromExistingPackage(config, bridge.Package);
            _assetRuntime = assetRuntime;
        }
        
        public async UniTask RunAsync()
        {
            // 现在可以安全使用 Framework API 了！
            BootRuntimeLogBootstrap.EnsureInstalled(_settings);
            GameLog.Info("[BootUpdate] Startup begin", "Boot");
            
            // Asset 已就绪，跳过 InitializeAssets
            // 跳过 UpdateAssets（BootLoader 已完成）
            // 跳过 LoadHotUpdateCode（BootLoader 已完成）
            
            StartGame();
        }
    }
}
```

### 4.4 程序集拆分方案

HYB-03 的本质不仅是代码拆分，更是**程序集边界重新划分**。当前的单一 `Boot` 程序集需要裂变为两个：

#### 拆分前（当前状态）

```
Boot.asmdef                          ← AOT 程序集，引用 Asset/Log/RuntimeLog/UniTask/HybridCLR.Runtime
├── Entry.cs
├── BootStartupSettings.cs
├── BootAssemblyEntry.cs
├── BootMetadataEntry.cs
├── IBootStartupView.cs
├── BootUpdateRunner.cs              ← 🔴 热更后这些逻辑不应在 AOT 里
├── BootRuntimeLogBootstrap.cs       ← 🔴 同上
└── HybridClrReflection.cs           ← 已废弃（有 .meta 但目前不调用）
```

#### 拆分后

```
Launcher.asmdef                      ← 🆕 AOT Shell 程序集（零框架依赖）
│   引用: UniTask / YooAsset / HybridCLR.Runtime
│   不引用: 任何 Framework 包 / VContainer
│
├── Entry.cs                         ← 保持不变，委托给 BootLoader
├── BootStartupSettings.cs           ← 保留：纯序列化数据 + 基础类型
├── BootAssemblyEntry.cs             ← 保留：纯 POCO
├── BootMetadataEntry.cs             ← 保留：纯 POCO
├── IBootStartupView.cs              ← 保留：极简接口
├── BootLoader.cs                    ← 🆕 极薄启动壳
├── BootBridge.cs                    ← 🆕 AOT→热更桥梁
└── BootStartupLog.cs                ← 🆕 AOT 独立日志

Boot.Update.asmdef                   ← 🆕 热更程序集
│   引用: Framework 全家 / Core / General / Project
│
├── BootUpdateRunner.cs              ← 从 AOT 迁移过来（逻辑不变）
└── BootRuntimeLogBootstrap.cs       ← 从 AOT 迁移过来（逻辑不变）
```

**关键约束：**
- `Launcher.asmdef` 必须确保**绝对不引用任何热更程序集**。编译边界由 `.asmdef` 的 `references` 列表硬约束。
- `BootStartupSettings` / `BootAssemblyEntry` / `BootMetadataEntry` / `IBootStartupView` 保留在 Launcher 中：它们是纯数据结构或极简接口，不依赖 Framework，也不需要热更能力。热更版的 `BootUpdateRunner` 通过 `BootBridge` 读取这些数据。
- 命名避开了 `Boot.xxx` vs `BootXXX.xxx` 的歧义：`Launcher`（AOT Shell）vs `Boot.Update`（热更启动编排）。
- 这里只有 `Framework.Pool/Cache/Event` 需要加入热更程序集列表，因为它们被 Core 引用且 Boot 不引用。

#### 挡 1 调整：`KJHybridClrBuildTools` 的验证逻辑

挡 1 执行后（Pool/Cache/Event 入热更），验证逻辑改为：

```csharp
// 白名单：只拦截已知不能进热更的程序集
private static void ValidateRuntimePreloadAssemblyName(string assemblyName)
{
    switch (assemblyName)
    {
        case "Boot":        // AOT Shell，永远不进热更
        case "TestKit":     // 非产品代码
            throw new InvalidOperationException(
                $"Assembly '{assemblyName}' is not supported by the current runtime preload publication path.");
    }
    // 其他全部允许（包括 Launcher、Boot.Update、Framework 全家）
}
```

这样当后续新建 `Boot.Update` 热更程序集时，不需要再改验证逻辑。

### 4.5 AOT 面变化对比

```
当前 AOT 面 (14 个程序集):
  Boot + Asset + Pool + Cache + Event + Log + RuntimeLog + TestKit
  + VContainer + UniTask + MessagePipe + ZLogger + ZLinq + ZString
  + YooAsset + HybridCLR.Runtime

挡 1 AOT 面 (11 个):
  Boot + Asset + Log + RuntimeLog + TestKit
  + VContainer + UniTask + MessagePipe + ZLogger + ZLinq + ZString
  + YooAsset + HybridCLR.Runtime

HYB-03 后 AOT 面 (8 个):
  Launcher(极薄Shell) + TestKit
  + VContainer + UniTask + MessagePipe + ZLogger + ZLinq + ZString
  + YooAsset + HybridCLR.Runtime
```

### 4.6 拆分验证清单

拆分完成后确认：
- [ ] `Launcher.asmdef` 不引用任何 Framework 包（Asset/Log/RuntimeLog/...），只引用 YooAsset / HybridCLR.Runtime / UniTask
- [ ] `Launcher.asmdef` 不引用任何热更程序集（编译边界硬拦截）
- [ ] `BootLoader` 无 System.IO 之外的任何日志依赖（不引用 Framework.Log / Framework.RuntimeLog）
- [ ] `BootLoader` 直接用 YooAsset 原生 API 读 RawFile（不通过 `IAssetRuntime.LoadRawBytes`）
- [ ] 热更 DLL 加载成功后，`BootUpdateRunner`（热更版 `Boot.Update`）可以正常使用 `GameLog`、`RuntimeLogManager`、`IAssetRuntime`
- [ ] 原有 `BootUpdateRunner` 逻辑完整保留，仅移动文件位置 + 改为从 BootBridge 接收参数
- [ ] `AssetRuntime` 新增 `WrapFromExistingPackage()` 方法，允许从外部已初始化的 `ResourcePackage` 构建
- [ ] `ResourcePackage` 的生命周期所有权明确：BootLoader 创建，AssetRuntime 接管，Core.AssetSystem.Shutdown() 时统一清理

---

## 五、整体 Review

### 5.1 挡 1（Pool/Cache/Event 入热更）— ✅ 安全

**改动面极小，风险极低：**

- `HybridCLRSettings.asset` — 追加三个 hotUpdate 程序集名
- `KJHybridClrBuildTools.cs` — 删除三个 case 的拦截

**安全性验证：**

| 检查项 | 结论 |
|--------|------|
| Boot.asmdef 是否引用 Pool？ | ❌ 不引用。只在 `Core.PoolService` 中使用 |
| Boot.asmdef 是否引用 Cache？ | ❌ 不引用 |
| Boot.asmdef 是否引用 Event？ | ❌ 不引用 |
| Core 是否在热更中？ | ✅ Core 已在 `hotUpdateAssemblies` 中 |
| HybridCLR 热更程序集引用 AOT 程序集（UniTask）是否正常？ | ✅ 正常，Pool 引用 UniTask(AOT)，HybridCLR 原生支持 |

**唯一注意：`Pool/Cache/Event` 在 AOT metadata 配置中是否已有补充？**

当前 `patchAOTAssemblies` 只有 `mscorlib` / `System` / `System.Core`。这三个 Framework 程序集使用 `SuperSet` 元数据模式，AOT 编译器会自动为热更代码中涉及的 AOT 类型生成桥。`Pool` 中用到 `UniTask` / `GameObject` / `Transform` 等 AOT 类型，如果热更代码创建了 AOT 未预见的泛型实例化（如 `ObjectPool<SomeHotUpdateType>`），需要确保 `patchAOTAssemblies` 包含了对应的补充元数据源。当前 Pool 主要使用 `ObjectPool<GameObject>`（AOT 已有），`CollectionPool` 操作 `List<T>` / `Dictionary<TKey,TValue>` 等标准集合（AOT 元数据已有），**风险极低。**

### 5.2 HYB-03 (Boot 拆分) — ⚠️ 规模评估

这个改动**不只是移动代码，而是重构启动链路**。涉及的文件和改动量：

| 改动 | 范围 | 复杂度 |
|------|------|--------|
| 新建 `BootLoader.cs` (AOT) | ~120 行新代码 | Medium |
| 新建 `BootBridge.cs` (AOT) | ~15 行 | Low |
| 新建 `BootStartupLog.cs` (AOT) | ~30 行 | Low |
| 修改 `Entry.cs` | 委托给 BootLoader | Low |
| 修改 `Boot.KJ.Boot.asmdef` | 移除 Asset/Log/RuntimeLog 引用 | Low |
| 新建 `BootUpdate.KJ.BootUpdate.asmdef` (热更) | 新 asmdef | Low |
| 迁移 `BootUpdateRunner.cs` → `BootUpdate/` | 纯移动，不改逻辑 | Low |
| 迁移 `BootRuntimeLogBootstrap.cs` → `BootUpdate/` | 纯移动 | Low |
| 修改 `AssetRuntime.cs` | ~20 行新增 `WrapFromExistingPackage()` | Low |
| 修改 `AssetRuntimeFactory.cs` | ~5 行新增 `WrapFromExistingPackage()` | Low |
| 修改 `KJHybridClrBuildTools.cs` | Boot.Update 加入支持的程序集列表 | Low |
| 修改 `HybridCLRSettings.asset` | 追加 Boot.Update 到热更列表 | Low |

**总计：~200 行新代码 + 现有 ~300 行迁移。单次迁移工作，不是一个复杂的功能开发。**

### 5.3 关键设计问题逐项检查

#### 🔍 问题 1：BootLoader 的日志怎么办？

当前 `GameLog` 是 static 门面 + `GameLog.Sink` 机制 + 启动缓冲区。如果 BootLoader 不引用 `Framework.Log`，那启动期的日志碎片就无法进入 AI Runtime Log 体系。

**答案：** BootLoader 自己写一个极简日志（`BootStartupLog`），直接写文本文件。这是**故意的取舍**——BootLoader 阶段日志量极少（只有"开始下载"、"DLL 加载成功/失败"几条），完全不需要 RuntimeLog 的 JSONL 结构化能力。Core 接管后，`GameLogBridge` 会把后续日志全量输出到 RuntimeLog session。

**日志连续性处理（Review 反馈）：** 为了保持排查问题的连贯性，热更层 `RuntimeLogManager` 初始化后，自动执行：
1. 检查 `boot.log` 是否存在且未过期
2. 读取内容，作为第一批日志条目注入 `latest.jsonl`（标记 phase=`Boot.AOT`）
3. 在注入的末尾追加一条 `[Boot.Loader] HotUpdate DLLs loaded, entering Boot.Update` 标记分割线
4. 打印一条 `[Boot] BootLoader AOT logs merged from {boot.log.path}` 方便排查
5. 如果 `boot.log` 不存在或无法读取，则仅打一条 Info 标记 AOT 日志缺失，不中断启动

这样避免了查 Crash 时的日志断层——AI 分析只需读 `latest.jsonl` 就能看到完整时间线。实现上 ~30 行代码，`BootStartupLog` 保持纯文本写入不变，不增加复杂度。

#### 🔍 问题 2：`AssetRuntime.WrapFromExistingPackage()` 是否完整？

当前 `AssetRuntime` 的状态机是：`未初始化 → BeginInitialize → 等待 initOp → 设置 IsReady=true`。

`WrapFromExistingPackage()` 需要直接跳过初始化，设置 `IsReady=true`。但它还必须：
- 设置 `_config`（后续 `UpdateManifest` / `CreateDownloader` 可能读取 config 参数）
- 设置 `_downloadMaxConcurrency` / `_failedRetryCount`
- 处理 `_lifecycleVersion`（确保并发安全）

这些在接口里已经覆盖了。额外要注意：如果 `AssetRuntime` 本身在热更 DLL 中，那 `AssetRuntime` 的类型解析走的是热更版本，但 `ResourcePackage` 引用是 AOT 创建的实例。HybridCLR 下，**AOT 对象传给热更代码是完全合法且推荐的**——热更代码通过接口/基类操作 AOT 对象。

**ResourcePackage 所有权（Review 反馈）：**
- `ResourcePackage` 由 `BootLoader` 在 AOT 侧创建（`YooAssets.CreatePackage`）
- 通过 `BootBridge.Package` 传给热更层
- `AssetRuntime.WrapFromExistingPackage()` 接管引用，赋值给 `_defaultPackage`
- 退出时的 `AssetRuntime.Shutdown()` → `_defaultPackage = null; YooAssets.Destroy()` 负责清理
- 因为 `DefaultPackage` 伴随整个 App 生命周期，不存在"BootLoader 提前释放"的竞态
- 在 `BootBridge` 和 `WrapFromExistingPackage` 上添加 XML doc 明确所有权转移

#### 🔍 问题 3：`BootStartupSettings` 的生命周期

`BootStartupSettings` 是 Entry 上的 `[SerializeField]` 数据，在 MonoBehaviour 生命周期内一直活到场景卸载。BootLoader 需要它来知道要加载哪些 DLL 和 metadata。在拆分后：

- BootLoader 从 Entry 的 `BootStartupSettings` 读取配置
- 构建 `BootBridge` 时把这些信息传给热更版本
- 热更版的 `BootUpdateRunner` 不再需要 Entry 上的引用

**这是干净的：AOT 侧只读配置数据，不做任何 Framework 相关的初始化。**

#### 🔍 问题 4：热更版 `BootUpdateRunner` 在哪个程序集？

当前 `BootUpdateRunner` 在 `Boot` 程序集（AOT）。拆分后需要一个新的热更程序集，如 `Boot.Update`（asmdef 名也叫 `Boot.Update`）。`KJHybridClrBuildTools` 需要允许它进热更。

**当前工具的验证逻辑需要改为白名单制**——只拦截 `Boot` 和 `TestKit`，其他全部放行。这样 `Boot.Update`（新程序集）自动被允许。

#### 🔍 问题 5：Editor 下的兼容性

当前 Editor Play 模式下 `skipHotUpdateInEditor = true`，Boot 直接走 `StartGame()` 而跳过下载和 DLL 加载。拆分后 Editor 下需要保证：

- `BootLoader` 检测 Editor 模式 + skip 标志 → 手动加载热更 DLL 对应的 Editor 程序集 → 再反射调用 BootUpdateRunner.Start()
- 或者 Editor 下不经过 BootLoader，直接从 Entry 走简化路径

更简单的方式：**Editor 下保持 `skipHotUpdateInEditor = true`，BootLoader 绕过下载但保留 Bridge 传递。** 它不下载 DLL，但任然构造 Bridge → 反射 BootUpdateRunner.Start()。编辑器下 BootUpdateRunner 不像 Player 那样是 Assembly.Load 加载的，而是直接编译在项目中的程序集——所以需要确保 it 在 Editor 下也被 link 到 Build 中（它是热更程序集，Editor 下 Unity 会直接编译它）。

#### 🔍 问题 6：`StartupProbeSystem` 的启动验证

`Core.StartupProbeSystem` 在 `SystemManager` 初始化时验证启动链路。如果 BootLoader 的日志无法进入 RuntimeLog，`StartupProbeSystem` 的验证日志里就会缺失 Boot 阶段的信息。但这不影响核心功能——它验证的是 Core/AssetSystem/PoolService 的初始化链路，Boot 阶段不在它的验证范围内。

#### 🔍 问题 7：Pool/Cache/Event 入热更后，HybridCLR 泛型桥需要补充吗？

挡 1 把 Pool/Cache/Event 加入热更程序集后，它们在热更 DLL 中运行时，会通过 HybridCLR 解释执行。这些程序集大量使用泛型：

| 程序集 | 泛型实例化 |
|--------|-----------|
| `Pool` | `ObjectPool<T>`（对象池）、`TypePool` 反射、`CollectionPool.RentList<T>()` 等 |
| `Cache` | `Cache<TKey,TValue>`（泛型缓存）、`LruCachePolicy<TKey>` |
| `Event` | `MessagePipe` 的泛型消息处理器（`ISubscriber<T>`、`IPublisher<T>`） |

HybridCLR 使用 **SuperSet HomologousImageMode** 加载 AOT metadata，会自动为热更代码中涉及的 AOT 类型生成桥代码。当前 `patchAOTAssemblies` 覆盖了 `mscorlib` / `System` / `System.Core`，这些基础库已经包含了 `List<T>` / `Dictionary<TKey,TValue>` / `HashSet<T>` / `Stack<T>` / `Queue<T>` 等标准集合的泛型实例化信息。

**风险点：**
- 如果热更代码中创建了 AOT 未预见的泛型组合（如 `ObjectPool<CoreSomeNewType>` 或 `Cache<int, ProjectNewType>`），需要运行时额外补充 metadata
- `MessagePipe` 的消息处理器泛型（`ISubscriber<SomeHotUpdateEvent>`)：如果 `SomeHotUpdateEvent` 是热更 struct 且用到了 AOT 未预见的字段访问模式，需要补充
- `TypePool` 使用 `ConcurrentDictionary<Type, object>` + 反射创建池——反射在 HybridCLR 内与 AOT 行为一致，不额外需要补充

**挡 1 是否可以直接执行？** ✅ 可以。原因：
1. 当前 Core 程序集中对 Pool/Cache/Event 的使用主要集中在 `PoolService` 和 `EventManager`，它们用的是 `List<T>` / `Dictionary<TKey,TValue>` / `ObjectPool<GameObject>` 等标准泛型组合——这些在 AOT metadata 中已有覆盖
2. HybridCLR 的 SuperSet 模式会在 IL2CPP 编译时扫描所有热更程序集的泛型使用，生成桥代码
3. 如果后续业务层创建了新的泛型实例化组合，运行时会有 `MissingMetadataException`——届时可以按需追加 AOT 补充程序集（见下文）

### 5.4 AOT 补充程序集设计（按需，后续）

借鉴 37 项目的 `Core.Aot/` 实践：当热更代码创建了 HybridCLR 无法解决的泛型实例化时，需要建立一个 **AOT 补充程序集**，在其中显式实例化这些泛型，编译进 AOT，提供 metadata。

**命名：`Framework.Aot.asmdef`**

```
Assets/Scripts/Boot/Launcher.Aot/          ← 位于 AOT 侧
├── Framework.Aot.asmdef                   ← 引用 Framework（仅补充代码）+ HybridCLR.Runtime
├── PoolAot.cs                             ← Pool 相关补充
│   // 示例：显式实例化热更中会用到的泛型组合
│   // static readonly ObjectPool<SomeHotUpdateType> _bridge1 = null;
├── CacheAot.cs                            ← Cache 补充
└── EventAot.cs                            ← Event 补充
```

**什么时候需要：**
- 运行时出现 `MissingMetadataException` 指向某个泛型实例化
- 在 `Framework.Aot` 中显式引用该泛型组合（字段声明/方法调用），让 IL2CPP 生成 metadata
- 重新执行 HybridCLR Generate → 补充 AOT metadata → 无需修改热更 DLL

**挡 1 阶段不需要创建 `Framework.Aot`——它只在运行时出现 metadata 缺失时才按需添加。** 但文件结构和命名约定在此定义好，后续不会乱。

### 5.5 HYB-03 实施前置：YooAsset 初始化死锁与 Editor 兼容

这里的 4 个问题是深度 Review 发现的**实施前必须解决的边界问题**。如果不处理，HYB-03 一旦拆分就会崩溃。

#### 🚨 致命问题 1：YooAsset 初始化的 "鸡与蛋" 死锁

当前 `AssetRuntime.BuildSandboxParameters()`（第 543-553 行）在 Host 模式下创建 `FileSystemParameters` 需要传入 `IRemoteService` 实现：

```csharp
// AssetRuntime.cs:549 — Framework.Asset (热更 DLL 中)
var parameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
    new CdnRemoteService(cdnBaseUrl),  // ← IRemoteService 实现在此！
    packageName);
```

`CdnRemoteService` 是 `AssetRuntime` 的 `private sealed class`，锁在 `Framework.Asset` 中。如果按 HYB-03 把 `Asset` 移入热更：

```
BootLoader (AOT) 启动
  → 需要 YooAsset 下载热更 DLL
    → YooAsset Host 模式需要 IRemoteService
      → IRemoteService 实现在 Asset.dll (热更) 中
        → 热更 DLL 还没下载！
          → 💀 死锁
```

**根本原因：** YooAsset 的初始化策略类（`IRemoteService`、`IBuildinQueryServices`、`IDecryptionServices`）是**启动基础设施**，不是业务逻辑。它们应当与 BootLoader 一起留在 AOT。

**解决方案：** 把这些策略类从 `Framework.Asset` 提取到 AOT 侧的 `Launcher` 程序集中。

```
Assets/Scripts/Boot/Launcher/YooAssetStrategy/   ← 新建，AOT 侧
├── BootRemoteService.cs         ← IRemoteService（URL 拼接）
├── BootBuildinQueryService.cs   ← IBuildinQueryServices（StreamingAssets 查询）
└── BootDecryptionService.cs     ← IDecryptionServices（默认无加密）

Framework/Asset/AssetRuntime.cs  ← 修改：
  BuildSandboxParameters() 改为接收 IRemoteService 等参数（DI 注入）
  不再内部 new CdnRemoteService()
```

`BootRemoteService` 实现最基础的 URL 拼接（从 `AssetConfig.CdnBaseUrl` 读取），与当前 `CdnRemoteService` 完全等价。热更层启动后如需更复杂策略（动态 CDN 切换、Token 附加），通过静态代理注入：

```csharp
// Launcher/AOT
public sealed class BootRemoteService : IRemoteService
{
    public static Func<string, IReadOnlyList<string>>? CustomUrlProvider;
    private readonly string _baseUrl;

    public BootRemoteService(AssetConfig config)
        => _baseUrl = (config?.CdnBaseUrl ?? "http://127.0.0.1:8080/CDN").TrimEnd('/');

    public IReadOnlyList<string> GetRemoteUrls(string fileName)
        => CustomUrlProvider?.Invoke(fileName)
            ?? new[] { $"{_baseUrl}/{fileName}" };
}
```

#### ⚠️ 致命问题 2：Editor 模拟模式 (EditorSimulateMode) 丢失

文档 4.3 节中的 BootLoader 伪代码直接硬编码了 `HostPlayModeOptions`。如果上线：

```
EditorPlay → 每次都要打 AB 包 → 开发效率暴跌 → 没人能接受
```

**解决方案：** BootLoader 必须根据 `AssetConfig.PlayMode` 动态选择初始化参数，而不是硬编码 Host：

```csharp
// BootLoader.cs — 三种模式全支持
var config = Resources.Load<AssetConfig>("AssetConfig");

#if UNITY_EDITOR
if (config.Mode == AssetConfig.PlayMode.EditorSimulate)
{
    var initOp = package.InitializePackageAsync(new EditorSimulateModeOptions
    {
        EditorFileSystemParameters = FileSystemParameters
            .CreateDefaultEditorFileSystemParameters(config.EditorSimulatePackageRoot)
    });
    await initOp.ToUniTask();
    // Editor 模拟模式下不下载 DLL（本地已有编译产物）
    // 直接构造 Bridge → 反射启动
}
else
#endif
if (config.Mode == AssetConfig.PlayMode.Offline)
{
    var initOp = package.InitializePackageAsync(new OfflinePlayModeOptions
    {
        BuiltinFileSystemParameters = FileSystemParameters
            .CreateDefaultBuiltinFileSystemParameters(config.PackageName)
    });
    await initOp.ToUniTask();
    // 从 StreamingAssets 读取 RawFile → Assembly.Load
}
else // Host
{
    var sandboxParams = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(
        new BootRemoteService(config), config.PackageName);
    sandboxParams.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, config.DownloadMaxConcurrency);
    sandboxParams.AddParameter(EFileSystemParameter.DownloadWatchdogTimeout, config.DownloadTimeout);

    var builtinParams = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(config.PackageName);
    var initOp = package.InitializePackageAsync(new HostPlayModeOptions
    {
        BuiltinFileSystemParameters = builtinParams,
        CacheFileSystemParameters = sandboxParams
    });
    await initOp.ToUniTask();
    // 版本检查 → 下载 → 读取 RawFile → Assembly.Load
}
```

**Editor 下的初始化参数也需要从 `AssetConfig` 读取，BootLoader 不再硬编码任何 PlayMode。**

#### 🔧 细节隐患 3：程序集重名冲突 (.asmdef 边界)

Unity 不允许两个 `.asmdef` 生成同名 DLL。当前 `KJ.Boot.asmdef` 编译为 `Boot.dll`。如果拆分后热更层也要叫 `Boot`，就必须改名。

**解决方案——明确 .asmdef 命名：**

| asmdef | DLL 名 | 位置 | 层级 |
|--------|--------|------|------|
| `KJ.Launcher.asmdef` | `Launcher.dll` | `Assets/Scripts/Boot/Launcher/` | AOT |
| `KJ.Boot.asmdef` | `Boot.dll` | `Assets/Scripts/Boot/` 其余代码 → 清空 AOT 代码后复活 | HotUpdate |

**具体操作：**
1. **挡 1 不涉及 asmdef 拆分**——Pool/Cache/Event 直接加入已有热更列表，不创建新 asmdef
2. HYB-03 时：
   - 新建 `KJ.Launcher.asmdef`，移入 Entry、BootLoader、BootBridge、BootStartupLog、纯数据类、策略类
   - 保留原有 `KJ.Boot.asmdef`，改引用为 Framework 全家 + Core 等热更程序集，成为热更层
   - `BootUpdateRunner` + `BootRuntimeLogBootstrap` 留在 `KJ.Boot.asmdef`（现在它是热更了）
   - 反射代码不变：`Type.GetType("Boot.BootUpdateRunner, Boot")` 保持正确

#### 📉 细节隐患 4：早期崩溃的 "日志黑洞"

BootLoader 的 `BootStartupLog` 直接写 `boot.log`，不走 Framework.Log / RuntimeLog。如果在下载热更 DLL 过程中崩溃（断网、CDN 挂、解析错误），这些错误日志只存在于本地文本文件，不会被崩溃收集 SDK（Bugly/Firebase 等）上报。

**解决方案：** BootBridge 传递早期日志缓存，热更层初始化后回放。

```csharp
// BootBridge.cs — 增加早期日志属性
public class BootBridge
{
    public ResourcePackage Package { get; init; }
    public BootStartupSettings Settings { get; init; }
    public IBootStartupView View { get; init; }
    public IReadOnlyList<BootStartupLogEntry> EarlyLogs { get; init; }  // 🆕
}

// BootStartupLog.cs — 改为可回放的记录器
internal class BootStartupLog
{
    private readonly List<BootStartupLogEntry> _entries = new();  // 内存缓存
    // ... 写文件 + 存 _entries
    public IReadOnlyList<BootStartupLogEntry> Entries => _entries;
}

// BootUpdateRunner (热更) — 启动后立即回放
public async UniTask RunAsync()
{
    BootRuntimeLogBootstrap.EnsureInstalled(_settings);

    // 🆕 回放 AOT 阶段的早期日志到 RuntimeLog session
    if (bridge.EarlyLogs?.Count > 0)
    {
        foreach (var entry in bridge.EarlyLogs)
        {
            RuntimeLogManager.Current?.Write(new RuntimeLogEntry
            {
                Level = entry.Level,
                Phase = "Boot.AOT",
                Message = entry.Message,
                // ...
            });
        }
        RuntimeLogManager.Current?.Write(new RuntimeLogEntry
        {
            Level = GameLogLevel.Information,
            Phase = "Boot",
            Message = "[Boot.Loader] AOT-stage early logs replayed. HotUpdate DLLs loaded."
        });
    }
    // ...
}
```

这确保了：
- AOT 阶段的日志不丢失，完整注入 `latest.jsonl`
- 崩溃收集 SDK 接入后（通常在 `Core.Init` 阶段），早期日志也已回放完毕
- AI 分析只需读 `latest.jsonl` 就能看到完整启动时间线（含 AOT 阶段）

---

## 六、37 项目 Boot 层参考

> 来源：`F:\int_37_pack\client\CODE_MAP.md`
> 37 项目是已上线的成熟产品（~6,600 个 C# 文件，~100 个模块），架构设计经过了生产验证。

### 6.1 Boot 层概览

```
Boot/ (~38 文件, 全部 AOT, ❌ 非热更)
├── Entry/        BootEntry.cs, BootType.cs, BootTypeMgr.cs
├── GameLife/     GameRestart.cs, StartScreen.cs
└── Update/       HotFix.cs, Channel.cs, ServerConfig.cs
```

| 子目录 | 职责 |
|--------|------|
| `Entry/` | 游戏启动入口，HybridCLR 热更 DLL 加载，启动模式管理 |
| `GameLife/` | 软重启机制（不退出进程重新加载 DLLs） |
| `Update/` | 热更新管理，渠道包配置，服务器地址配置 |

### 6.2 与 KJ 的对比

| 维度 | 37 项目 | KJ 当前 | KJ 目标 (HYB-03) |
|------|---------|---------|------------------|
| Boot 是否热更 | ❌ 全部 AOT（~38 文件） | ❌ 全部 AOT（~8 文件） | 仅 Launcher AOT（~6 文件），其余热更 |
| Boot 复杂度 | 中等（含渠道包、软重启、服务器配置） | 低（仅启动 + DLL 加载） | 极薄 Shell |
| 热更覆盖核心诉求 | Core 起热更即可 | — | **最大化热更覆盖** |
| `Core.Aot` 桥 | ✅ 有（~6 文件 AOT 补充代码） | ❌ 无 | 预留 `Framework.Aot` 桥约定（按需） |
| Framework 结构 | `UniCore/` + `Package/`（44 包）+ `External/`（42 库） | 7 个扁平包 | 同当前 |

### 6.3 关键差异分析

**37 选择 Boot 全部 AOT 的原因**——已上线产品，Boot 复杂度已积累（渠道包、多服务器环境、软重启），且热更需求已被 Core 覆盖。重构成本大于收益。

**KJ 选择更激进拆分的原因**——Phase 1 架构奠基期，Boot 只有 ~8 个文件，**此时拆分成本最低**。等积累更多逻辑后再拆，成本指数增长。

### 6.4 可借鉴的点

**① 目录组织方式**

37 用子目录区分 Entry / GameLife / Update，而非扁平堆放。HYB-03 后借鉴这个思路：

```
Launcher/ (AOT Shell)              ← 对应 37 的 Boot/Entry/
├── Launcher.asmdef
├── Entry.cs                       ← 启动 MonoBehaviour
├── BootLoader.cs                  ← 极薄启动壳
├── BootBridge.cs                  ← AOT→热更桥梁
├── BootStartupLog.cs              ← AOT 独立日志
└── Data/                          ← 纯数据 + 极简接口
    ├── BootStartupSettings.cs
    ├── BootAssemblyEntry.cs
    ├── BootMetadataEntry.cs
    └── IBootStartupView.cs

Boot.Update/ (HotUpdate)            ← 对应 37 的 Boot/Update/
├── Boot.Update.asmdef
├── BootUpdateRunner.cs            ← 从 AOT 迁移
└── BootRuntimeLogBootstrap.cs     ← 从 AOT 迁移

Framework.Aot/ (AOT Bridge)        ← 预留，按需创建
├── Framework.Aot.asmdef           ← 对应 37 的 Core.Aot/
├── PoolAot.cs
├── CacheAot.cs
└── EventAot.cs
```

**② 软重启 (`GameRestart`)**

37 的 `GameLife/GameRestart.cs` 支持不退出进程重新加载 DLL——热更"需要重启生效"的场景中，游戏内重新走 Boot → Core 初始化链路即可，不必退出 App。**这是 HYB-03 之后的下一个能力。**

**③ Framework 的 External 物理隔离**

37 把 42 个第三方库全部放在 `Framework/External/` 下。KJ 的第三方库走 UPM，当前只有 7 个 Framework 包，暂时不需要——但包数增长后可考虑。

### 6.5 结论：37 验证了方向，不改变设计

- 37 的 Boot 积累到 ~38 文件全部 AOT——这正是我们不想要的未来
- **趁 Boot 还简单（~8 文件），尽早把边界定清楚**
- HYB-03 设计不需要改变，反而更坚定了"现在拆"

## 六、结论与行动

### 挡 1：立即执行（本次会话）

| 文件 | 改动 |
|------|------|
| `HybridCLRSettings.asset` | `hotUpdateAssemblies` 追加 `Pool`、`Cache`、`Event` |
| `KJHybridClrBuildTools.cs` | `ValidateRuntimePreloadAssemblyName()` 仅拦截 `Boot`、`TestKit`，放开对 Framework 程序集的拦截 |

### 挡 2：HYB-03 Boot 拆分（后续阶段）

**前置检查：** 必须先完成 5.5 节中的四个问题——
1. ✅ `IRemoteService` / `IBuildinQueryServices` / `IDecryptionServices` 从 Framework.Asset 提取到 `Launcher/YooAssetStrategy/`
2. ✅ BootLoader 根据 `AssetConfig.Mode` 动态选择 PlayMode（含 `#if UNITY_EDITOR` 分支）
3. ✅ 新建 `KJ.Launcher.asmdef` *→* 保留 `KJ.Boot.asmdef` 为热更 asmdef
4. ✅ `BootBridge.EarlyLogs` 回放机制就绪

目标：将 AOT 面从 11 个程序集缩至 8 个，释放 `Asset` / `Log` / `RuntimeLog` / `Boot.Update` 进热更。

**估价：~300 行新代码 + ~300 行迁移。**

**程序集裂变：**
1. 新建 `KJ.Launcher.asmdef`（AOT Shell）— `Assets/Scripts/Boot/Launcher/`
2. `KJ.Boot.asmdef` 保留，改为热更 asmdef — 引用 Framework 全家 + Core 等热更程序集
3. `KJ.BootUpdate.asmdef` → 并入 `KJ.Boot.asmdef`（不再需要单独 asmdef，因为热更层已是 Boot）

**目录结构：**
```
Boot/
├── Launcher/                       ← 🆕 AOT Shell
│   ├── KJ.Launcher.asmdef          ← 引用: UniTask / YooAsset / HybridCLR.Runtime
│   ├── Entry.cs                    ← 保留
│   ├── BootLoader.cs               ← 🆕
│   ├── BootBridge.cs               ← 🆕
│   ├── BootStartupLog.cs           ← 🆕
│   ├── YooAssetStrategy/           ← 🆕 从 Framework.Asset 提取的策略类
│   │   ├── BootRemoteService.cs    ← 实现 IRemoteService
│   │   ├── BootBuildinQueryService.cs ← 实现 IBuildinQueryServices
│   │   └── BootDecryptionService.cs ← 实现 IDecryptionServices
│   └── Data/                       ← 纯数据 + 极简接口
│       ├── BootStartupSettings.cs
│       ├── BootAssemblyEntry.cs
│       ├── BootMetadataEntry.cs
│       └── IBootStartupView.cs
│
├── KJ.Boot.asmdef                  ← 现为热更 asmdef
│   │   引用: Framework 全家 / Core / General / UniTask / MessagePipe
│   ├── BootUpdateRunner.cs         ← 从 AOT 迁移（逻辑不变）
│   └── BootRuntimeLogBootstrap.cs  ← 从 AOT 迁移（逻辑不变，回放 EarlyLogs）
│
└── Boot.Editor/                    ← Editor-only, 不参与运行时层级
    └── ...
```

### 最终目标状态

```
AOT (编译进 Native)                       HotUpdate (HybridCLR 解释)
─────────────────────────────────         ────────────────────────────
Launcher  (极薄Shell，仅启动/下载/DLL加载) Asset   ✅ HYB-03 后入热更
TestKit   (非产品代码)                     Log     ✅ HYB-03 后入热更
YooAsset  (native C++)                     RuntimeLog ✅ HYB-03 后入热更
HybridCLR.Runtime (native C++)             Pool    ✅ 挡1入热更
VContainer / UniTask / MessagePipe         Cache   ✅ 挡1入热更
ZLogger / ZLinq / ZString                  Event   ✅ 挡1入热更
                                           Core    ✅ 已在热更
                                           General ✅ 已在热更
                                           Project ✅ 已在热更
                                           Boot.Update ✅ HYB-03 入热更
```

---

## 八、相关文件索引

| 文件 | 角色 |
|------|------|
| `ProjectSettings/HybridCLRSettings.asset` | HybridCLR 热更程序集配置入口 |
| `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` | DLL 编译同步 + 热更程序集验证 |
| `Assets/Scripts/Boot/KJ.Boot.asmdef` | Boot 的引用约束 |
| `Assets/Scripts/Boot/BootUpdateRunner.cs` | 启动流程，当前含热更前后逻辑 |
| `Assets/Scripts/Boot/BootRuntimeLogBootstrap.cs` | 启动期 RuntimeLog 安装 |
| `Assets/Scripts/Boot/Entry.cs` | 启动 MonoBehaviour 入口 |
| `Assets/Scripts/Boot/BootStartupSettings.cs` | 序列化启动配置 |
| `Assets/Scripts/Boot/IBootStartupView.cs` | 启动 UI 最小接口 |
| `Assets/Framework/Asset/AssetRuntime.cs` | YooAsset 适配实现 |
| `Assets/Framework/Asset/IAssetRuntime.cs` | 启动期资源运行时接口 |
| `Assets/Framework/Asset/AssetRuntimeFactory.cs` | 工厂创建 AssetRuntime |
| `Assets/Framework/Log/GameLog.cs` | 稳定日志门面 |
| `Assets/Framework/RuntimeLog/RuntimeLogManager.cs` | 当前 Session 管理 |
| `Assets/Framework/Pool/ObjectPool.cs` | 泛型对象池（lock 并发安全） |
| `Assets/Framework/Pool/CollectionPool.cs` | 集合池静态入口 |
| `Assets/Framework/Cache/Cache.cs` | 泛型缓存（可插拔策略） |
| `Assets/Framework/Event/GameEventAttribute.cs` | 统一事件标记 |
| `.planning/ROADMAP.md` | HYB-03 定义 |
| `.planning/STATE.md` | 当前状态跟踪 |
