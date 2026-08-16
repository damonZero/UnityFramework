# KJ 项目启动链路完整文档

> 生成日期: 2026-07-31 | 基于代码实际分析，逐方法追踪

---

## 概述

KJ 项目采用 **AOT → 热更新 → DI 容器** 三层启动架构：

```
Unity Awake (AOT Entry)
  → 加载热更 DLL (HybridCLR)
    → BootUpdateRunner (热更新入口)
      → ProjectStartup (VContainer DI 根容器)
        → Core → General → Project 三层注册
          → SystemManager.InitAll (Core 系统初始化)
            → ModelLifecycle.LoadAll (业务模型加载)
```

**当前状态：** 框架底层启动链完整且可用（AOT→热更→DI 全部贯通），但 Login/UIManager/NetManager/ConfigManager 等上层业务模块**尚未实现**。

---

## 一、AOT 入口 (KJ.Launcher.asmdef)

> AOT 壳只引用 `UniTask / YooAsset / HybridCLR.Runtime / AssetShared`，不得引用任何热更新程序集。

### 1. `Entry.Awake()` — `Assets/Scripts/Boot/Launcher/Entry.cs:25`

```csharp
private void Awake()
{
    DontDestroyOnLoad(gameObject);
    RunStartupAsync().Forget();
}
```

**角色：** Unity 场景中挂载的第一个 MonoBehaviour，整个应用的真正起点。

**做了什么：**
- `DontDestroyOnLoad(gameObject)` — 将自身 GameObject 标记为跨场景持久化
- `Forget()` — 发射 fire-and-forget 异步任务（`Awake` 不能是 async，用 UniTask 的 `Forget()` 规避）

**为什么用 `Forget()`：** Unity 的 `Awake` 回调签名是 `void`，不能是 `async`。UniTask 的 `Forget()` 在不阻塞主循环的前提下发射异步流程。

---

### 2. `Entry.RunStartupAsync()` — `Entry.cs:39`

```csharp
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
        BootStartupLog.Error($"[Entry] Startup failed: {e}");
        view?.SetStatus("Startup failed");
        view?.SetRepairVisible(true);
    }
    finally
    {
        _isRunning = false;
    }
}
```

**角色：** AOT 入口的异步协调器。

**四个关键设计：**
1. **防重入** — `_isRunning` 标志位，配合 `Repair()` 方法支持修复按钮（启动失败后可重试，但不会重复启动）
2. **每次重建 BootLoader** — `_loader?.Dispose()` 后新建，确保修复重试时状态干净
3. **全局异常兜底** — 任意阶段失败都会显示错误状态和修复按钮
4. **AOT 日志** — 用 `BootStartupLog` 而非 `GameLog`（此时热更 DLL 还未加载）

---

### 3. `BootLoader` 构造函数 — `BootLoader.cs:35`

```csharp
public BootLoader(BootStartupSettings settings, IBootStartupView view)
```

**角色：** AOT 侧启动编排器。持有配置和 UI 回调。**不引用任何热更新类型。**

**数据来源：**
- `BootStartupSettings` — `[Serializable]` 配置类，通过 `Entry` 的 `[SerializeField]` 在 Inspector 中配置
- `IBootStartupView` — 启动 UI 回调接口（`SetStatus` / `SetProgress` / `SetRepairVisible`），同样通过 Inspector 赋值

**`BootStartupSettings` 关键字段：**

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `EnableHotUpdate` | `true` | 是否启用热更新代码加载 |
| `EnableAssetUpdate` | `true` | 是否启用热更新资源下载 |
| `SkipHotUpdateInEditor` | `true` | Editor 下是否跳过热更（直接使用 Editor 程序集） |
| `StartupTypeName` | `"Project.Bootstrap.ProjectStartup, Project"` | 热更完成后反射调用的入口类型 |
| `StartupMethodName` | `"Start"` | 入口方法名 |
| `HotUpdateAssemblies` | `[]` | 10 个热更 DLL 的路径和名称列表 |
| `AotMetadataAssemblies` | `[]` | AOT 补充元数据 DLL 列表 |
| `AssetDownloadTag` | `""` | 资源下载标签过滤（空=全部） |

---

### 4. `BootLoader.RunAsync()` — `BootLoader.cs:48`

```csharp
public async UniTask RunAsync()
{
    BootStartupLog.Info("[BootLoader] Startup begin");
    _view?.SetRepairVisible(false);
    _view?.SetProgress(0f);

    var config = LoadAssetConfig();                              // 步骤 1
    _package = await InitializeYooAsset(config);                 // 步骤 2
    _view?.SetProgress(0.1f);

    if (_settings.EnableHotUpdate)
    {
        await DownloadHotUpdateAssetsAsync(config);              // 步骤 3
        await LoadHotUpdateCodeAsync();                          // 步骤 4
    }

    _view?.SetProgress(0.9f);
    var bridge = new BootBridge(_package, _settings, _view, config, BootStartupLog.Snapshot);
    ReflectBootUpdateRunnerStart(bridge);                        // 步骤 5
    _view?.SetProgress(1f);
}
```

**角色：** AOT 启动流程总编排。5 个关键步骤，每个有独立错误处理。

**进度条映射：**

| 阶段 | 进度 | 说明 |
|------|------|------|
| 开始 | 0% | |
| YooAsset 就绪 | 10% | 包初始化 + 清单加载完成 |
| 热更下载中 | 10%→45% | Host 模式下插值更新 |
| 代码加载完成 | 90% | DLL + AOT metadata 全部就绪 |
| 交接完成 | 100% | 控制权已移交热更层 |

**关键对象：** `BootBridge` — 携带 AOT 侧所有初始化产物（Package、Settings、View、Config、早期日志）作为"交接包裹"传给热更新侧。

---

### 5. `BootLoader.LoadAssetConfig()` — `BootLoader.cs:80`

```csharp
private AssetConfig LoadAssetConfig()
{
    var config = Resources.Load<AssetConfig>("AssetConfig");
    if (config == null)
        throw new InvalidOperationException(
            "[BootLoader] AssetConfig not found at Resources/AssetConfig.");
    return config;
}
```

**角色：** 从 `Resources/` 加载资源配置 ScriptableObject。

**关键设计：** `AssetConfig` 位于 `Framework.AssetShared`（AOT 共享程序集），AOT 侧和热更侧都能引用此类。包含字段：

| 字段 | 说明 |
|------|------|
| `Mode` | 资源模式（EditorSimulate / Offline / Host） |
| `PackageName` | YooAsset 包名 |
| `CdnBaseUrl` | CDN 热更地址（Host 模式） |
| `DownloadMaxConcurrency` | 最大下载并发数 |
| `DownloadTimeout` | 下载超时秒数 |
| `FailedRetryCount` | 下载失败重试次数 |
| `EditorSimulatePackageRoot` | Editor 模拟模式产物路径 |

---

### 6. `BootLoader.InitializeYooAsset()` — `BootLoader.cs:88`

```csharp
private async UniTask<ResourcePackage> InitializeYooAsset(AssetConfig config)
```

**角色：** 初始化 YooAsset 资源包。共 5 个子步骤：

| 步骤 | API 调用 | 说明 |
|------|----------|------|
| 1 | `YooAssets.Initialize()` | YooAsset 引擎级初始化 |
| 2 | `YooAssets.CreatePackage(packageName)` | 创建资源包实例 |
| 3 | `package.InitializePackageAsync(BuildOptions(...))` | 根据 PlayMode 配置文件系统 |
| 4 | `package.RequestPackageVersionAsync()` | 请求远程包版本号 |
| 5 | `package.LoadPackageManifestAsync(...)` | ⚠️ 加载包清单（**至关重要**，不加载会导致后续 `LoadAssetSync` 抛出 "Active package manifest not found"） |

**PlayMode 文件系统分派：**

| PlayMode | 文件系统配置 |
|----------|-------------|
| `EditorSimulate` | `EditorFileSystemParameters`（直接读编辑器产物目录） |
| `Offline` | `BuiltinFileSystemParameters`（仅 StreamingAssets 内置） |
| `Host` | `BuiltinFileSystemParameters` + `CacheFileSystemParameters`（内置 + CDN 远端 + 沙盒缓存） |

**`BuildOptions` 方法逻辑（`BootLoader.cs:120`）：**

```csharp
private InitializePackageOptions BuildOptions(AssetConfig config, string packageName)
{
    switch (config.Mode)
    {
        case AssetConfig.PlayMode.EditorSimulate:
            return new EditorSimulateModeOptions { ... };
        case AssetConfig.PlayMode.Offline:
            return new OfflinePlayModeOptions { ... };
        case AssetConfig.PlayMode.Host:
            return new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = ...,
                CacheFileSystemParameters = BuildSandboxParameters(config)
            };
    }
}
```

**Host 模式沙盒参数（`BootLoader.cs:167`）：**
- CDN 根 URL 默认 `http://127.0.0.1:8080/CDN`
- 使用 `BootRemoteService`（AOT 侧 YooAsset `IRemoteService` 实现）
- 配置下载并发数和看门狗超时

---

### 7. `BootLoader.DownloadHotUpdateAssetsAsync()` — `BootLoader.cs:179`

```csharp
private async UniTask DownloadHotUpdateAssetsAsync(AssetConfig config)
```

**角色：** Host 模式下检查并下载热更新资源文件（DLL、AOT metadata 等代码资源）。

**执行流程：**

```
非 Host 模式? → 直接返回
    ↓
创建 ResourceDownloader（指定并发数、重试次数、可选标签过滤）
    ↓
TotalDownloadCount == 0? → 日志 "Hot-update files are current"，返回
    ↓
循环等待下载完成，插值更新进度条 10%→45%
    ↓
下载失败 → 抛异常
```

**配置参数来源：**
- `DownloadMaxConcurrency` → `ResourceDownloaderOptions` 的 `maxConcurrency`
- `FailedRetryCount` → `ResourceDownloaderOptions` 的 `retryCount`
- `AssetDownloadTag` → 标签过滤（空=下载全部）

---

### 8. `BootLoader.LoadHotUpdateCodeAsync()` — `BootLoader.cs:211`

```csharp
private UniTask LoadHotUpdateCodeAsync()
```

**角色：** 加载热更新代码（AOT 补充元数据 + 热更 DLL）。两个同步子步骤：

```
if (!EnableHotUpdate) → 跳过，返回
    ↓
#if UNITY_EDITOR && SkipHotUpdateInEditor → 跳过，日志 "Using Editor assemblies"
    ↓
LoadAotMetadata()        ← 步骤 4a: 补充 AOT 元数据
    ↓
LoadHotUpdateAssemblies() ← 步骤 4b: Assembly.Load 热更 DLL
```

**为什么在 Editor 中跳过：** Editor 下程序集已由 Unity 编译加载到 AppDomain，重复 `Assembly.Load` 会出错。`SkipHotUpdateInEditor` 默认 `true`。

---

### 9. `BootLoader.LoadAotMetadata()` — `BootLoader.cs:235`

```csharp
private void LoadAotMetadata()
{
    foreach (var entry in _settings.AotMetadataAssemblies)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
            continue;

        var bytes = LoadRawBytes(entry.AssetPath);
        if (bytes == null || bytes.Length == 0)
            continue;

        var result = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
        if (result != LoadImageErrorCode.OK)
            throw new InvalidOperationException(
                $"[BootLoader] Load AOT metadata failed: {entry.AssemblyName}, result={result}");
    }
}
```

**角色：** 为 AOT 程序集补充元数据。

**为什么需要：** HybridCLR 的 `HomologousImageMode.SuperSet` 模式 — 热更 DLL 可能用到 AOT 程序集中未在原始包中实例化的泛型类型/方法。补充元数据让 IL2CPP 能在运行时正确生成这些代码。

**每个条目的处理：**
1. 跳过空条目
2. `LoadRawBytes()` 从 YooAsset 包加载 `.dll.bytes`（RawFile 类型）
3. 跳过空文件（可能某些 AOT 程序集不需要补充元数据）
4. 调用 `RuntimeApi.LoadMetadataForAOTAssembly(bytes, SuperSet)`
5. 检查返回码，`!= OK` 则抛异常

---

### 10. `BootLoader.LoadHotUpdateAssemblies()` — `BootLoader.cs:253`

```csharp
private void LoadHotUpdateAssemblies()
{
    foreach (var entry in _settings.HotUpdateAssemblies)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.AssemblyName))
            continue;

        if (IsAssemblyLoaded(entry.AssemblyName))
            continue;  // 已加载则跳过（Editor 下常见）

        var bytes = LoadRawBytes(entry.AssetPath);
        if (bytes == null || bytes.Length == 0)
            throw new FileNotFoundException(
                $"[BootLoader] Hot-update DLL not found: {entry.AssemblyName}");

        Assembly.Load(bytes);
    }
}
```

**角色：** 加载所有热更新 DLL 到运行时 AppDomain。

**保护措施：**
- **`IsAssemblyLoaded()` 检查** — 遍历 `AppDomain.CurrentDomain.GetAssemblies()`，避免重复加载
- **DLL 缺失直接抛异常** — 与 metadata 不同，热更 DLL 是必需的
- **加载顺序** — `HotUpdateAssemblies` 数组顺序即加载顺序，必须在 `HybridCLRSettings.asset` 中正确配置依赖关系

**当前 10 个热更程序集：** `Boot, Core, General, Project, Pool, Cache, Event, Asset, Log, RuntimeLog`

---

### 11. `BootLoader.LoadRawBytes()` — `BootLoader.cs:271`

```csharp
private byte[] LoadRawBytes(string assetPath)
{
    if (string.IsNullOrWhiteSpace(assetPath) || _package == null)
        return Array.Empty<byte>();

    var handle = _package.LoadAssetSync<RawFileObject>(assetPath);
    try
    {
        if (handle.Status != EOperationStatus.Succeeded)
            return Array.Empty<byte>();

        var rawFile = handle.GetAssetObject<RawFileObject>();
        return rawFile?.GetBytes() ?? Array.Empty<byte>();
    }
    finally
    {
        handle.Release();
    }
}
```

**角色：** 用原生 YooAsset API 同步加载原始字节文件。

**⚠️ 关键约束：** 这里用 `_package.LoadAssetSync<RawFileObject>()`（YooAsset 原生 API），**不是** `IAssetSystem.LoadAssetAsync<T>()`。因为 `IAssetSystem` 在热更新层，AOT 侧不能引用。

---

### 12. `BootLoader.ReflectBootUpdateRunnerStart()` — `BootLoader.cs:302`

```csharp
private void ReflectBootUpdateRunnerStart(BootBridge bridge)
{
    BootStartupLog.Info("[BootLoader] Handing control to hot-update Boot layer");
    var type = Type.GetType("Boot.BootUpdateRunner, Boot");
    if (type == null)
        throw new InvalidOperationException(
            "[BootLoader] Could not resolve Boot.BootUpdateRunner in the loaded assemblies.");

    var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
    if (method == null)
        throw new InvalidOperationException(
            "[BootLoader] Boot.BootUpdateRunner.Start(BootBridge) was not found.");

    method.Invoke(null, new object[] { bridge });
}
```

**角色：** **AOT ↔ 热更新边界**。通过反射将控制权从 AOT 侧交接给热更新侧。

**为什么用反射：**
- `BootLoader` 在 AOT `KJ.Launcher.asmdef` 中
- `BootUpdateRunner` 在热更新 `KJ.Boot.asmdef` 中
- 编译期 `BootLoader` 不知道 `BootUpdateRunner` 的存在
- 字符串 `"Boot.BootUpdateRunner, Boot"` 是**契约**，改名需同步 `BootLoader` 和 `HybridCLRSettings`

**BootBridge 携带的交接数据：**

| 属性 | 类型 | 用途 |
|------|------|------|
| `Package` | `ResourcePackage` | AOT 已初始化的 YooAsset 资源包 |
| `Settings` | `BootStartupSettings` | 启动配置（包含入口类型/方法名） |
| `View` | `IBootStartupView` | 启动 UI 回调 |
| `Config` | `AssetConfig` | 资源配置 |
| `EarlyLogs` | `IReadOnlyList<BootStartupLogEntry>` | AOT 阶段日志快照（待回放） |

---

## 二、AOT 辅助模块

### BootStartupLog — `Assets/Scripts/Boot/Launcher/BootStartupLog.cs`

**角色：** AOT 侧启动日志。不依赖任何热更新类型（无 `GameLog`、`RuntimeLog` 引用）。

**双重输出：**
1. **内存快照** — `List<BootStartupLogEntry>` 加锁追加，通过 `Snapshot` 属性获取只读副本
2. **文件落盘** — 写入 `Logs/Runtime/boot.log`（纯文本行，`try-catch` 兜底防抛异常）

**日志级别：** `Info` / `Warn` / `Error`（精简枚举，不引入 `GameLogLevel` 依赖）

**快照流转：** `BootStartupLog.Snapshot` → `BootBridge.EarlyLogs` → `BootUpdateRunner.ReplayEarlyLogs()` → `RuntimeLogManager.Current.Write()`

---

### BootRemoteService — `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootRemoteService.cs`

**角色：** AOT 侧 YooAsset `IRemoteService` 实现。Host 模式下将资源文件名映射为 CDN 下载 URL。

**URL 解析：**
```csharp
public IReadOnlyList<string> GetRemoteUrls(string fileName)
{
    if (CustomUrlProvider != null)
        return CustomUrlProvider.Invoke(fileName);  // 测试/外部覆盖

    return new List<string> { $"{_baseUrl}/{fileName}" };
}
```

- 默认: `{CdnBaseUrl}/{fileName}`
- `CustomUrlProvider` 静态委托允许测试注入

---

## 三、热更新入口 (KJ.Boot.asmdef)

> 热更 Boot 引用 `Asset / Log / RuntimeLog / UniTask / AssetShared / YooAsset / Launcher`，不引用 VContainer / Core / General / Project。

### 13. `BootUpdateRunner.Start(BootBridge)` — `BootUpdateRunner.cs:33`

```csharp
public static void Start(BootBridge bridge)
{
    if (bridge == null)
        throw new ArgumentNullException(nameof(bridge));

    new BootUpdateRunner(bridge).RunAsync().Forget();
}
```

**角色：** 热更新侧第一个方法，被 AOT 反射调用。与 `Entry.Awake()` 对称 — 创建实例、fire-and-forget 异步运行。

---

### 14. `BootUpdateRunner` 构造函数 — `BootUpdateRunner.cs:20`

```csharp
private BootUpdateRunner(BootBridge bridge)
{
    _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    _settings = bridge.Settings;
    _view = bridge.View;
    _assetRuntime = AssetRuntimeFactory.Create();
    _assetRuntime.WrapFromExistingPackage(bridge.Config, bridge.Package);
}
```

**角色：** 从 AOT 的 `ResourcePackage`（原生 YooAsset）创建热更新层的 `IAssetRuntime`（Framework.Asset 包装）。

**关键：`WrapFromExistingPackage()`：**
- 不重新初始化 YooAsset
- "接管" AOT 侧已初始化好的 `ResourcePackage`
- 此后热更层通过 `IAssetRuntime` 接口操作资源，不再直接碰 YooAsset 原生 API
- `BootLoader.Dispose()` 被设计为 no-op，**不会**释放 AOT 侧的 Package（所有权已转移）

**`_assetRuntimeTransferred` 标志：** 初始为 `false`。如果后续 `ProjectStartup.Start()` 接收了 `IAssetRuntime` 参数，设为 `true`，`BootUpdateRunner.Dispose()` 就不再调用 `_assetRuntime.Shutdown()`（防止重复释放）。

---

### 15. `BootUpdateRunner.RunAsync()` — `BootUpdateRunner.cs:41`

```csharp
public async UniTask RunAsync()
{
    GameLog.Info("[Boot] Startup begin", "Boot");
    _view?.SetRepairVisible(false);
    _view?.SetProgress(0f);

    BootRuntimeLogBootstrap.EnsureInstalled(_settings);   // 步骤 1: 安装完整日志系统
    ReplayEarlyLogs();                                     // 步骤 2: 回放 AOT 日志
    await UpdateAssetsAsync();                             // 步骤 3: 热更资源检查/下载
    StartGame();                                           // 步骤 4: 启动游戏
    RuntimeLogManager.Flush();                             // 步骤 5: 刷新日志缓冲
}
```

**角色：** 热更侧编排器。从 `GameLog.Info()`（而非 `BootStartupLog`）开始 — 热更新日志系统已就绪。

---

### 16. `BootRuntimeLogBootstrap.EnsureInstalled()` — `BootRuntimeLogBootstrap.cs:13`

```csharp
public static RuntimeLogSession EnsureInstalled(BootStartupSettings settings)
{
    return RuntimeLogManager.InstallIfNone(
        () => CreateSession(settings),
        installGameLogSink: true);
}
```

**角色：** 安装结构化运行时日志系统（单例，只安装一次）。

**`CreateSession` 收集的信息：**
- 环境信息：`Application.productName`、`unityVersion`、`platform`、`version`、`buildGUID`
- 日志配置：`GameLog.Profile.Environment` + `MinimumLevel`
- 资源信息：`AssetConfig.Mode`、`PackageName`
- 热更清单：`HotUpdateAssemblies`、`AotMetadataAssemblies` 的名称列表
- 输出配置：Editor → `{ProjectRoot}/Logs/Runtime/`，Player → `{persistentDataPath}/Logs/Runtime/`

---

### 17. `BootUpdateRunner.ReplayEarlyLogs()` — `BootUpdateRunner.cs:119`

```csharp
private void ReplayEarlyLogs()
{
    foreach (var entry in _bridge.EarlyLogs)
    {
        RuntimeLogManager.Current?.Write(new RuntimeLogEntry
        {
            Level = ToGameLogLevel(entry.Level),
            Module = "Boot.AOT",
            Category = "Boot.AOT",
            Phase = "Boot",
            Message = entry.Message,
            ExceptionType = null,
            ExceptionMessage = null,
            StackTrace = null
        });
    }
}
```

**角色：** 将 AOT 阶段的 `BootStartupLog` 内存快照回放写入正式 `RuntimeLogSession`（JSONL）。

**为什么需要：** AOT 阶段的热更 DLL 加载、YooAsset 初始化等关键日志不能丢失。回放后，启动全链路日志统一在 JSONL 中，AI 分析时无需单独读取 `boot.log`。

---

### 18. `BootUpdateRunner.UpdateAssetsAsync()` — `BootUpdateRunner.cs:64`

```csharp
private async UniTask UpdateAssetsAsync()
```

**角色：** 检查并下载热更新资源（**非代码资源**：prefab、贴图、配置等）。

**与 AOT 侧 `DownloadHotUpdateAssetsAsync` 的区别：**

| 维度 | AOT 侧 | 热更侧 |
|------|--------|--------|
| 下载内容 | 热更新代码（DLL + AOT metadata） | 普通游戏资源 |
| API | YooAsset 原生 `ResourceDownloader` | `IAssetRuntime` 接口 |
| 时机 | 代码加载前 | 代码加载后 |
| 进度条范围 | 10%→45% | 5%→65% |

**执行流程：**

```
if (!EnableAssetUpdate) → 跳过
    ↓
_assetRuntime.UpdateManifest()  ← 检查远端版本
    ↓
等待版本检查完成 (5%→20%)
    ↓
启动清单更新 (20%→35%)
    ↓
_assetRuntime.CreateDownloader() ← 创建资源下载器
    ↓
TotalDownloadCount == 0? → 跳过
    ↓
循环等待下载完成 (35%→65%)
    ↓
下载失败 → 抛异常
```

---

### 19. `BootUpdateRunner.StartGame()` — `BootUpdateRunner.cs:147`

```csharp
private void StartGame()
{
    GameLog.Info("[Boot] Starting game", "Boot");
    _view?.SetStatus("Starting game");
    _view?.SetProgress(0.95f);

    if (string.IsNullOrWhiteSpace(_settings.StartupTypeName))
        throw new InvalidOperationException("[Boot] Startup type name is empty.");

    var type = Type.GetType(_settings.StartupTypeName, throwOnError: false);
    // 默认: "Project.Bootstrap.ProjectStartup, Project"

    var methodName = string.IsNullOrWhiteSpace(_settings.StartupMethodName)
        ? "Start"
        : _settings.StartupMethodName;
    var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

    var parameters = method.GetParameters();
    if (parameters.Length == 0)
        method.Invoke(null, Array.Empty<object>());
    else if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(_assetRuntime))
    {
        method.Invoke(null, new object[] { _assetRuntime });
        _assetRuntimeTransferred = true;  // 所有权转移
    }
    else
        throw new InvalidOperationException(
            $"[Boot] Startup method signature is unsupported: ...");
}
```

**角色：** 反射调用游戏逻辑入口（`ProjectStartup.Start()`），将控制权交给 VContainer DI 容器层。

**支持两种入口签名：**
- `static void Start()` — 无参数
- `static void Start(IAssetRuntime)` — 接收资源运行时，**所有权转移**给 DI 容器

---

## 四、VContainer DI 容器层 (Project / Core / General)

### 20. `ProjectStartup.Start(IAssetRuntime)` — `ProjectStartup.cs:9`

```csharp
public static void Start(IAssetRuntime bootAssetRuntime = null)
{
    if (_rootScope != null)
        return;  // 防止重复初始化

    var root = new UnityEngine.GameObject("ProjectLifetimeScope");
    UnityEngine.Object.DontDestroyOnLoad(root);
    ProjectLifetimeScope.PendingBootAssetRuntime = bootAssetRuntime;
    _rootScope = root.AddComponent<ProjectLifetimeScope>();
}
```

**角色：** 创建 VContainer 的根 `LifetimeScope` GameObject。整个 DI 容器体系从这开始。

**关键设计：**
- **`PendingBootAssetRuntime`** — 静态属性桥接 AOT→DI：Boot 层的 `IAssetRuntime` 通过静态属性传入容器（因为 `ProjectLifetimeScope.Configure()` 由 VContainer 在 `Awake()` 中回调，无法直接传参）
- **`_rootScope != null`** — 防重入，应用生命周期内只构建一次容器

---

### 21. `ProjectLifetimeScope.Configure()` — `ProjectLifetimeScope.cs:13`

```csharp
protected override void Configure(IContainerBuilder builder)
{
    var context = new CoreStartupContext(builder)
    {
        AssetRuntime = PendingBootAssetRuntime
    };
    PendingBootAssetRuntime = null;  // 清除静态桥接

    CoreBootstrapStage.Configure(context);      // 第 1 阶段
    GeneralBootstrapStage.Configure(context);   // 第 2 阶段
    ProjectBootstrapStage.Configure(context);   // 第 3 阶段
}
```

**角色：** VContainer 配置入口，按依赖方向顺序组装三层。

**必须严格遵守的顺序：**
1. **Core** — 先注册 ZLogger、MessagePipe、IAssetSystem（General/Project 依赖它们）
2. **General** — 再注册业务层基础设施（依赖 Core 已注册的服务）
3. **Project** — 最后注册项目专属业务（依赖 Core + General）

**`CoreStartupContext` 上下文传递：**
- `Builder` — VContainer 的 `IContainerBuilder`
- `MessagePipeOptions` — Core 阶段注册后设置，供后续阶段复用
- `AssetRuntime` — AOT 传入的资源运行时

---

### 22. `CoreBootstrapStage.Configure()` — `CoreBootstrapStage.cs:5`

```csharp
public static void Configure(CoreStartupContext context)
{
    context.MessagePipeOptions = context.Builder.RegisterCoreServices(context.AssetRuntime);
}
```

**角色：** 薄委托层，调用 `CoreContainerRegistration.RegisterCoreServices()` 并将 `MessagePipeOptions` 存入 context。

---

### 23. `CoreContainerRegistration.RegisterCoreServices()` — `CoreContainerRegistration.cs:20`

**这是整个 DI 注册中最复杂的方法，分 4 大块：**

#### 块 1: ZLogger / ILoggerFactory（行 22-46）

```csharp
var runtimeLogSession = RuntimeLogBootstrap.EnsureInstalled(assetRuntime);
var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.SetMinimumLevel(LogLevel.Trace);
    logging.AddFilter((category, level) =>
        GameLog.Profile.IsEnabled(category ?? GameLog.DefaultModule, ToGameLogLevel(level)));
    logging.AddProvider(new RuntimeLogLoggerProvider(runtimeLogSession));  // → JSONL
    logging.AddZLoggerUnityDebug(options => { ... });                       // → Unity Console
});
builder.RegisterInstance(loggerFactory).As<ILoggerFactory>();
GameLogBridge.Install(runtimeLogSession, loggerFactory.CreateLogger<GameLogBridge>());
```

- 两个 Provider：`RuntimeLogLoggerProvider`（JSONL 落盘）+ `ZLoggerUnityDebug`（Console）
- `GameLogBridge.Install()` — 将 `Framework.Log.GameLog` 静态门面桥接到 ZLogger 管道
- 销毁回调：`GameLogBridge.Uninstall()` + `loggerFactory.Dispose()` + `RuntimeLogManager.DisposeCurrent()`

#### 块 2: SimpleLogger\<T\>（行 47）

```csharp
builder.Register(typeof(SimpleLogger<>), Lifetime.Singleton).As(typeof(ILogger<>));
```

**解决 IL2CPP AOT 泛型实例化问题：** `Microsoft.Extensions.Logging.Logger<T>` 的泛型方法在 AOT 侧 DLL 中，HybridCLR 无法穿透其泛型实例化。`SimpleLogger<T>` 在热更 Core 程序集中，将 `Log<TState>` 展开为字符串后调非泛型 `ILogger.Log(string)`。

#### 块 3: Framework.Asset 资源桥接（行 52-66）

```csharp
builder.RegisterInstance(assetRuntime).As<IAssetRuntime>();
builder.RegisterInstance(assetSystem).As<IAssetSystem>();
```

将 Boot 层传入的 `IAssetRuntime` 注册为容器单例，Core 层的 `[CoreSystem]` 类可通过构造函数注入使用。

**`null` 降级处理：** 如果 AOT 没有传入 `IAssetRuntime`，注册一个新的 `AssetRuntime`（用于测试/Editor 降级场景）。

#### 块 4: Core 系统扫描 + SystemManager（行 70-72）

```csharp
builder.RegisterCoreTypes(options, typeof(CoreContainerRegistration).Assembly);
builder.RegisterEntryPoint<SystemManager>();
```

- **`RegisterCoreTypes`** — 扫描 Core 程序集中所有 `[CoreSystem]` 类，注册为 Singleton
- **`RegisterEntryPoint<SystemManager>()`** — 告诉 VContainer：容器构建后，自动调用 `SystemManager.Start()`（因为它实现了 `IStartable`）

---

### 24. `CoreTypeRegistration.RegisterCoreTypes()` — `CoreTypeRegistration.cs:28`

**角色：** 反射扫描两样东西：

#### 扫描 1: GameEvent 事件类型

```csharp
private static void RegisterGameEvents(IContainerBuilder builder, MessagePipeOptions options, Assembly[] assemblies)
{
    foreach (var type in GameEventTypeScanner.FindGameEventTypes(assemblies))
    {
        RegisterMessageBrokerMethod.MakeGenericMethod(type)
            .Invoke(null, new object[] { builder, options });
    }
}
```

为所有带 `[GameEvent]` 属性的 struct 注册 MessagePipe `RegisterMessageBroker<T>`，使 `IPublisher<T>` / `ISubscriber<T>` 可以注入。

#### 扫描 2: CoreSystem 类型

```csharp
private static void RegisterSystems(IContainerBuilder builder, Assembly[] assemblies)
{
    foreach (var type in GetLoadableTypes(assemblies)
        .Where(t => t.IsClass && !t.IsAbstract
            && t.GetCustomAttribute<CoreSystemAttribute>() != null))
    {
        ValidateSystem(type);  // ISystem 检查 + Core.* 命名空间检查
        builder.Register(type, Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
    }
}
```

**当前注册的 CoreSystem（按 Priority 排序）：**

| 类 | Priority | Init 行为 |
|----|----------|----------|
| `StartupProbeSystem` | 0 | 打一条日志，验证启动链路 |
| `AssetSystem` | `AssetConstants.SystemPriority` | 验证 IAssetRuntime 就绪，发布 `AssetSystemReadyEvent` |
| `PoolService` | `AssetConstants.SystemPriority + 10` | 初始化对象池，桥接 IAssetSystem |

---

### 25. `GeneralBootstrapStage.Configure()` — `GeneralBootstrapStage.cs:8`

```csharp
public static void Configure(CoreStartupContext context)
{
    context.Builder.RegisterBusinessLayer(options, typeof(GeneralBootstrapStage).Assembly);
}
```

**角色：** 薄委托层，调用 `GeneralContainerRegistration.RegisterBusinessLayer()`。

---

### 26. `GeneralContainerRegistration.RegisterBusinessLayer()` — `GeneralContainerRegistration.cs:22`

**角色：** 与 Core 层类似的反射扫描，但针对业务层：

```csharp
public static void RegisterBusinessLayer(
    this IContainerBuilder builder, MessagePipeOptions options, params Assembly[] assemblies)
{
    RegisterBusinessEvents(builder, options, scanAssemblies);
    RegisterModels(builder, scanAssemblies);
    RegisterModelLifecycle(builder);
}
```

**三步注册：**
1. **`RegisterBusinessEvents`** — 扫描 `[GameEvent]` 标记的 struct，注册 MessagePipe Broker
2. **`RegisterModels`** — 扫描 `[Model]` 标记的 class（须实现 `IModel`），注册为 `Lifetime.Singleton`
3. **`RegisterModelLifecycle`** — 注册 `ModelLifecycle` 为 `IPostStartable`（由 VContainer 在 `SystemManager.Start()` 之后自动调用），带 `builder.Exists()` 防重复

---

### 27. `ProjectBootstrapStage.Configure()` — `ProjectBootstrapStage.cs:6`

```csharp
public static void Configure(CoreStartupContext context)
{
    ProjectBootstrapper.Configure(context.Builder, options);
}
```

### 28. `ProjectBootstrapper.Configure()` — `ProjectBootstrapper.cs:14`

```csharp
public static void Configure(IContainerBuilder builder, MessagePipeOptions options)
{
    builder.RegisterBusinessLayer(options, Assembly.GetExecutingAssembly());
    GameLog.Info("[ProjectBootstrapper] Project layer container registration ready");
}
```

**角色：** 对 Project 程序集再做一轮 `[Model]` / `[GameEvent]` 扫描。与 General 层共用同一个 `RegisterBusinessLayer` 扩展方法，只是扫描的程序集不同。当前 Project 层还没有具体的 Model 类。

---

## 五、VContainer 生命周期回调（容器构建后）

> VContainer 在 `ProjectLifetimeScope.Awake()` 中构建容器，然后自动按接口驱动生命周期。

### 29. `SystemManager.Start()` (IStartable) → `InitAll()` — `SystemManager.cs:87`

```csharp
public void Start()
{
    InitAll();
}
```

**`InitAll()` 完整流程：**

```
Is Initialized? → 跳过（幂等）
    ↓
Sort _systems by Priority (升序)
    ↓
for each ISystem:
    sys.Init()
    ├─ 成功 → _initializedSystems.Add(sys)
    └─ 失败 → _failedSystemNames.Add(name), 继续下一个
    ↓
All Init 成功?
    ├─ Yes → _appStartedPublisher.Publish(new AppStartedEvent())
    └─ No  → 日志记录失败列表（不抛异常）
```

**关键设计：**
- 单个系统 Init 失败**不阻塞**其他系统
- 任意失败则不发布 `AppStartedEvent`，`ModelLifecycle.PostStart()` 会跳过
- `_initializedSystems` 逆序用于 `ShutdownAll()`

**Tick 驱动（`ITickable`）：**

```csharp
public void Tick()
{
    if (!Initialized) return;
    foreach (var t in _tickableSystems)
        t.Update(Time.deltaTime);
}
```

类似地实现了 `LateTick()` 和 `FixedTick()`，分别对应 VContainer 的 `ILateTickable` 和 `IFixedTickable`。

---

### 30. `AssetSystem.Init()` — `Core/Asset/AssetSystem.cs:31`

```csharp
public void Init()
{
    if (_runtime.IsReady)
    {
        _readyPublisher.Publish(new AssetSystemReadyEvent());
        AssetSystemLog.Ready(_logger);
        return;
    }

    throw new InvalidOperationException(
        $"AssetSystem requires a ready IAssetRuntime. Error={_runtime.LastError}");
}
```

**角色：** 验证 `IAssetRuntime` 就绪 + 发布 `AssetSystemReadyEvent`。所有依赖资源的系统可以订阅此事件。

---

### 31. `ModelLifecycle.PostStart()` (IPostStartable) → `LoadAll()` — `ModelLifecycle.cs:48`

```csharp
public void PostStart()
{
    if (!_coreStartupStatus.IsStarted || _coreStartupStatus.HasInitFailures)
    {
        ModelLifecycleLog.CoreStartupFailed(_logger, ...);
        return;  // Core 启动失败，不加载业务模型
    }

    LoadAll();
}
```

**`LoadAll()` 流程：**

```
_loaded? → 跳过（幂等）
    ↓
for each IModel (按 Priority 排序):
    model.Load()
    ├─ 成功 → 日志
    └─ 失败 → 日志 + 继续下一个
    ↓
_loaded = true
```

**与 `SystemManager.InitAll()` 关键区别：**

| 维度 | SystemManager.InitAll | ModelLifecycle.LoadAll |
|------|----------------------|----------------------|
| 管理对象 | `[CoreSystem]` + `ISystem` | `[Model]` + `IModel` |
| 触发时机 | `IStartable.Start()` | `IPostStartable.PostStart()` |
| 前置条件 | 无 | Core 所有系统初始化成功 |
| 命名空间约束 | `Core.*` | `General.*` 或 `Project.*` |
| 关闭顺序 | 逆序 Shutdown | 逆序 Unload |

**`UnloadAll()`：** `IDisposable` 触发，逆序调用 `model.Unload()`，单个失败不阻塞。

---

## 六、完整调用时序图

```
Unity Scene Awake
│
├─[AOT: KJ.Launcher.asmdef]─────────────────────────────────────────────
│
│  1. Entry.Awake()
│     └─ DontDestroyOnLoad(gameObject)
│     └─ RunStartupAsync().Forget()
│         2. Entry.RunStartupAsync()
│            └─ new BootLoader(startupSettings, view)
│            3. BootLoader.RunAsync()
│               ├─ 4. LoadAssetConfig()
│               │     └─ Resources.Load<AssetConfig>("AssetConfig")
│               │
│               ├─ 5. InitializeYooAsset(config)
│               │     ├─ YooAssets.Initialize()
│               │     ├─ YooAssets.CreatePackage()
│               │     ├─ InitializePackageAsync()  ← 根据 PlayMode 配置文件系统
│               │     ├─ RequestPackageVersionAsync()
│               │     └─ LoadPackageManifestAsync() ← ⚠️ 关键步骤
│               │
│               ├─ 6. DownloadHotUpdateAssetsAsync()  ← Host 模式
│               │     └─ ResourceDownloader 循环等待
│               │
│               ├─ 7. LoadHotUpdateCodeAsync()
│               │     ├─ 8. LoadAotMetadata()
│               │     │     └─ RuntimeApi.LoadMetadataForAOTAssembly()
│               │     └─ 9. LoadHotUpdateAssemblies()
│               │           └─ Assembly.Load(bytes) × 10
│               │
│               └─ 10. ReflectBootUpdateRunnerStart(bridge)
│                      └─ Reflection: "Boot.BootUpdateRunner, Boot"
│
├─[HotUpdate: KJ.Boot.asmdef]────────────────────────────────────────────
│
│  11. BootUpdateRunner.Start(BootBridge)
│      └─ new BootUpdateRunner(bridge)
│           └─ AssetRuntimeFactory.Create() + WrapFromExistingPackage()
│      └─ RunAsync().Forget()
│           12. BootUpdateRunner.RunAsync()
│              ├─ 13. BootRuntimeLogBootstrap.EnsureInstalled()
│              │     └─ RuntimeLogManager.InstallIfNone()
│              │
│              ├─ 14. ReplayEarlyLogs()
│              │     └─ AOT BootStartupLog → RuntimeLogSession JSONL
│              │
│              ├─ 15. UpdateAssetsAsync()
│              │     ├─ IAssetRuntime.UpdateManifest()
│              │     └─ IAssetRuntime.CreateDownloader()
│              │
│              └─ 16. StartGame()
│                     └─ Reflection: "Project.Bootstrap.ProjectStartup, Project"
│
├─[VContainer DI: Project / Core / General]──────────────────────────────
│
│  17. ProjectStartup.Start(IAssetRuntime)
│      └─ new GameObject("ProjectLifetimeScope")
│      └─ AddComponent<ProjectLifetimeScope>()
│           └─ VContainer builds container:
│
│               18. ProjectLifetimeScope.Configure()
│                   ├─ 19. CoreBootstrapStage.Configure()
│                   │     └─ 20. CoreContainerRegistration.RegisterCoreServices()
│                   │           ├─ ILoggerFactory (ZLogger + RuntimeLog)
│                   │           ├─ GameLogBridge.Install()
│                   │           ├─ SimpleLogger<T> → ILogger<>
│                   │           ├─ MessagePipe
│                   │           ├─ IAssetRuntime / IAssetSystem (instance)
│                   │           ├─ 21. CoreTypeRegistration.RegisterCoreTypes()
│                   │           │     ├─ [GameEvent] → MessagePipe Broker
│                   │           │     └─ [CoreSystem] → Singleton
│                   │           └─ EntryPoint<SystemManager>
│                   │
│                   ├─ 22. GeneralBootstrapStage.Configure()
│                   │     └─ 23. GeneralContainerRegistration.RegisterBusinessLayer()
│                   │           ├─ [GameEvent] → MessagePipe Broker
│                   │           ├─ [Model] → Singleton
│                   │           └─ ModelLifecycle (IPostStartable)
│                   │
│                   └─ 24. ProjectBootstrapStage.Configure()
│                         └─ 25. ProjectBootstrapper.Configure()
│                               └─ RegisterBusinessLayer(Project assembly)
│
├─[VContainer Lifecycle: IStartable → IPostStartable]───────────────────
│
│  26. SystemManager.Start() (IStartable)
│      └─ InitAll()
│           ├─ 27. StartupProbeSystem.Init()          Priority: 0
│           ├─ 28. AssetSystem.Init()                  Priority: AssetConstants
│           │     └─ Publish AssetSystemReadyEvent
│           ├─ 29. PoolService.Init()                  Priority: AssetConstants+10
│           └─ Publish AppStartedEvent (if all ok)
│
│  30. ModelLifecycle.PostStart() (IPostStartable)
│      └─ Check ICoreStartupStatus
│      └─ LoadAll()
│           └─ [遍历所有 [Model] 类，按 Priority 调用 .Load()]
│
▼ 应用就绪，等待用户交互
```

---

## 七、关键架构约束

### AOT/热更新边界

| 规则 | 说明 |
|------|------|
| `KJ.Launcher.asmdef` 引用白名单 | 仅 `UniTask / YooAsset / HybridCLR.Runtime / AssetShared` |
| 反射契约 | `"Boot.BootUpdateRunner, Boot"` — 改名需同步 `BootLoader` 和 `HybridCLRSettings` |
| 资源加载 | AOT 侧用 YooAsset 原生 API，热更侧用 `IAssetRuntime` 接口 |
| 日志 | AOT 用 `BootStartupLog`，热更后用 `GameLog` / `ILogger<T>` |

### DI 注册顺序

```
Core → General → Project
  ↓        ↓         ↓
ZLogger   [Model]   [Model]
MessagePipe 扫描     扫描
IAssetSystem        (Project
[CoreSystem]         assembly)
```

### 生命周期顺序

```
IStartable.Start()
  └─ SystemManager.InitAll()
       └─ [CoreSystem].Init() 按 Priority ↑
            └─ AppStartedEvent
                 ↓
IPostStartable.PostStart()
  └─ ModelLifecycle.LoadAll()  ← 检查 Core 启动成功
       └─ [Model].Load() 按 Priority ↑
```

---

## 八、登录流程的设计位置

### 当前状态

**登录代码尚未实现。** 在 ROADMAP.md 中的规划如下：

| 模块 | 复杂度 | 位置 | 前置依赖 | 说明 |
|------|--------|------|----------|------|
| **Login** | Medium | `General/Login/` | UIManager, NetManager, ConfigManager | 登录/公告/服务器列表/账号状态 |
| UIManager | — | 待定 | — | 尚未实现 |
| NetManager | — | 待定 | — | 尚未实现 |
| ConfigManager (Luban) | Medium | `General/Config/` | Framework.Asset | 尚未实现 |

### 设计约束（来自 `.planning/` 文档）

1. **登录是业务层逻辑** — `Login, account SDK flow, server list, and role selection are General/Project business flows, not Boot or Core logic.`
2. **实现方式** — 作为 `[Model]` + `IModel`，在 `ModelLifecycle.LoadAll()` 阶段加载
3. **目录位置** — `Assets/Scripts/General/Login/`
4. **UI 资源** — `GameRes/UI/General/LoginForm.prefab`
5. **安全红线** — 日志禁止记录 token、密码、实名账号等敏感数据
6. **事件规划** — 架构研究文档中预留了 `OnLoginSuccess = 30001` 等事件 ID

### 推测的接入流程

当依赖模块就绪后，登录流程大致为：

```
ModelLifecycle.LoadAll()
  └─ LoginModel.Load()                   ← [Model], Priority 靠前
       ├─ 订阅 AssetSystemReadyEvent
       ├─ 订阅 AppStartedEvent
       └─ 启动登录状态机
            ├─ 检查公告/维护状态 (ConfigManager)
            ├─ 显示登录界面 (UIManager)
            ├─ 获取服务器列表 (NetManager)
            ├─ 账号鉴权 (SDK / Token)
            └─ 进入游戏大厅
```

---

## 九、文件索引

| 文件 | 所属层 | 关键类/方法 |
|------|--------|------------|
| `Assets/Scripts/Boot/Launcher/Entry.cs` | AOT | `Entry.Awake()`, `Entry.RunStartupAsync()` |
| `Assets/Scripts/Boot/Launcher/BootLoader.cs` | AOT | `BootLoader.RunAsync()`, `LoadAssetConfig()`, `InitializeYooAsset()`, `LoadHotUpdateCodeAsync()`, `ReflectBootUpdateRunnerStart()` |
| `Assets/Scripts/Boot/Launcher/BootBridge.cs` | AOT | `BootBridge` (AOT→热更数据 DTO) |
| `Assets/Scripts/Boot/Launcher/BootStartupLog.cs` | AOT | `BootStartupLog.Info/Warn/Error`, `Snapshot` |
| `Assets/Scripts/Boot/Launcher/Data/BootStartupSettings.cs` | AOT | 启动配置 |
| `Assets/Scripts/Boot/Launcher/Data/BootAssemblyEntry.cs` | AOT | DLL 条目 |
| `Assets/Scripts/Boot/Launcher/Data/BootMetadataEntry.cs` | AOT | AOT 元数据条目 |
| `Assets/Scripts/Boot/Launcher/Data/IBootStartupView.cs` | AOT | 启动 UI 接口 |
| `Assets/Scripts/Boot/Launcher/YooAssetStrategy/BootRemoteService.cs` | AOT | CDN URL 解析 |
| `Assets/Scripts/Boot/BootUpdateRunner.cs` | 热更 Boot | `Start()`, `RunAsync()`, `StartGame()` |
| `Assets/Scripts/Boot/BootRuntimeLogBootstrap.cs` | 热更 Boot | `EnsureInstalled()` |
| `Assets/Scripts/Project/Bootstrap/ProjectStartup.cs` | Project | `Start(IAssetRuntime)` |
| `Assets/Scripts/Project/Bootstrap/ProjectLifetimeScope.cs` | Project | `Configure()` — VContainer 根 |
| `Assets/Scripts/Project/Bootstrap/ProjectBootstrapStage.cs` | Project | `Configure()` |
| `Assets/Scripts/Project/Bootstrap/ProjectBootstrapper.cs` | Project | `Configure()` |
| `Assets/Scripts/Core/Bootstrap/CoreBootstrapStage.cs` | Core | `Configure()` |
| `Assets/Scripts/Core/Bootstrap/CoreContainerRegistration.cs` | Core | `RegisterCoreServices()` |
| `Assets/Scripts/Core/Bootstrap/CoreTypeRegistration.cs` | Core | `RegisterCoreTypes()` |
| `Assets/Scripts/Core/Bootstrap/CoreStartupContext.cs` | Core | DI 上下文 DTO |
| `Assets/Scripts/Core/Systems/SystemManager.cs` | Core | `InitAll()`, `ShutdownAll()` |
| `Assets/Scripts/Core/Asset/AssetSystem.cs` | Core | `Init()`, `AssetSystemReadyEvent` |
| `Assets/Scripts/Core/Systems/StartupProbeSystem.cs` | Core | 启动探针 |
| `Assets/Scripts/Core/PoolService.cs` | Core | 对象池 DI 桥接 |
| `Assets/Scripts/General/Bootstrap/GeneralBootstrapStage.cs` | General | `Configure()` |
| `Assets/Scripts/General/Bootstrap/GeneralContainerRegistration.cs` | General | `RegisterBusinessLayer()` |
| `Assets/Scripts/General/Models/ModelLifecycle.cs` | General | `LoadAll()`, `UnloadAll()` |
| `Assets/Scripts/General/Models/IModel.cs` | General | 业务模型接口 |
| `Assets/Scripts/Core/Systems/ISystem.cs` | Core | 系统生命周期接口 |
