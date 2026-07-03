# YooAsset 通用接入模式分析（适用于KJ框架）

> 基于 GitHub 上 4 个 ET+YooAsset 项目的逆向分析，抽取框架无关的通用模式。
> KJ 技术栈：VContainer + UniTask + MessagePipe + HybridCLR + Luban
> 不含 ET 特有概念（ETTask、Entity、CoroutineLock、PackageType 等）

---

## 一、所有项目共同的接入模式

无论底层用什么框架，YooAsset 集成都遵循这 6 个步骤：

```
① 启动层初始化     → YooAssets.InitializeAsync(parameters)
② 包创建与清单     → CreatePackage → InitializeAsync → UpdateManifest
③ 资源加载接口     → LoadAssetAsync<T>(location) 封装
④ Handle 缓存管理  → Dictionary 缓存 + Release 时机
⑤ 更新管线         → 版本检查 → 下载 → 热更
⑥ 构建管线         → AssetBundle Builder + HybridCLR 打包流程
```

---

## 二、从 ETPro / X-ET7 提取的通用实现（翻译为 KJ 技术栈）

### 2.1 PlayMode 判断模式（所有项目一致）

```csharp
// 通用模式——决定用模拟模式还是 Host 模式
var playMode = IsEditorDebugMode 
    ? EPlayMode.EditorSimulateMode   // 编辑器开发：不打包，虚拟文件系统
    : EPlayMode.HostPlayMode;        // 真机/构建：从 CDN 下载
```

三个项目的判断逻辑完全相同：

| 项目 | 判断方式 |
|------|---------|
| Legends-Of-Heroes | `YooConfig.EPlayMode` (ScriptableObject，编辑器手动切换) |
| X-ET7 | `Define.IsAsync` (`#if UNITY_EDITOR && !ASYNC` → false, else true) |
| ETPro | `CDNConfig` (ScriptableObject，按渠道/平台配置) |

**推荐用于 KJ：** ScriptableObject 方案（最简单，不需要条件编译）

---

### 2.2 初始化代码（X-ET7 的 MonoResComponent → KJ 等价）

**X-ET7 原始（ETTask）：**
```csharp
// MonoResComponent.cs — 非热更层
public class MonoResComponent : Singleton<MonoResComponent>
{
    public async ETTask<bool> InitAsync()
    {
        EPlayMode playMode = Define.IsAsync
            ? EPlayMode.HostPlayMode
            : EPlayMode.EditorSimulateMode;

        var parameters = playMode switch
        {
            EPlayMode.EditorSimulateMode => new EditorSimulateModeParameters()
            {
                LocationServices = new AddressLocationServices()
            },
            EPlayMode.HostPlayMode => new HostPlayModeParameters()
            {
                LocationServices = new AddressLocationServices(),
                DecryptionServices = null,
                ClearCacheWhenDirty = false,
                DefaultHostServer = GetCdnUrl(),
                FallbackHostServer = GetCdnUrl()
            },
            _ => throw new Exception()
        };

        var initOp = YooAssets.InitializeAsync(parameters);
        await initOp;  // 通过 GetAwaiter 扩展方法桥接到 ETTask
        return initOp.Status == EOperationStatus.Succeed;
    }
}
```

**KJ 等价（UniTask + VContainer）：**
```csharp
// Boot/ResourceInitSystem.cs — 在 Boot 层，先于其他系统初始化
[CoreSystem(Priority = int.MinValue)] // 最早初始化
public class ResourceInitSystem : ISystem
{
    private readonly YooAssetConfig _config;

    public ResourceInitSystem(YooAssetConfig config) { _config = config; }

    public async UniTask InitAsync()  // ← UniTask 替代 ETTask
    {
        var parameters = _config.PlayMode switch
        {
            EPlayMode.EditorSimulateMode => new EditorSimulateModeParameters()
            {
                LocationServices = new AddressLocationServices()
            },
            EPlayMode.HostPlayMode => new HostPlayModeParameters()
            {
                LocationServices = new AddressLocationServices(),
                DefaultHostServer = _config.GetCdnUrl(),
                FallbackHostServer = _config.GetFallbackCdnUrl()
            },
            _ => throw new ArgumentOutOfRangeException()
        };

        var initOp = YooAssets.InitializeAsync(parameters);
        await initOp.ToUniTask();  // ← UniTask 原生支持 Task/AsyncOperation
    }
}
```

**关键差异：YooAsset 的 AsyncOperationBase 提供 `.Task` 属性返回 `System.Threading.Tasks.Task`，UniTask 对其有原生支持（不需要像 X-ET7 那样写 GetAwaiter 扩展方法）。**

---

### 2.3 ETTask → UniTask 桥接对比

**YooAsset 异步操作的兼容性：**

```
X-ET7 方式（需要手动桥接）：
  ETTask.Create(true) + handle.Completed += () => task.SetResult()

Legends-Of-Heroes / cn.etetet 方式（直接 await .Task）：
  await handle.Task;

KJ（UniTask 原生支持）：
  await handle.ToUniTask();        // YooAsset 的 AsyncOperationBase
  // 或直接
  await handle.Task;               // 回退到普通 Task
```

**结论：UniTask 比 ETTask 在这里更简单，不需要任何额外桥接代码。**

---

### 2.4 PackageManager 模式（ETPro 核心 → KJ 等价）

ETPro 有一个 391 行的 `PackageManager.cs`，是 YooAsset 的中央管理器。去 ET 化后核心逻辑：

```csharp
// KJ.Core/Resource/PackageManager.cs
public class PackageManager : IDisposable
{
    private readonly Dictionary<string, ResourcePackage> _packages = new();
    private ResourcePackage _defaultPackage;

    // 初始化默认包（ETPro: InitPackage）
    public async UniTask<ResourcePackage> InitDefaultPackageAsync(
        EPlayMode playMode, string packageName, IRemoteServices remoteServices)
    {
        YooAssets.Initialize(); // ← YooAsset 的静态入口

        var package = YooAssets.CreatePackage(packageName);
        YooAssets.SetDefaultPackage(package);

        var fsParams = CreateFileSystemParams(playMode, remoteServices);
        await package.InitializeAsync(fsParams).ToUniTask();

        // 请求包版本
        var versionOp = package.RequestPackageVersionAsync();
        await versionOp.ToUniTask();
        var version = versionOp.PackageVersion;

        // 更新清单
        var manifestOp = package.UpdatePackageManifestAsync(version);
        await manifestOp.ToUniTask();

        _packages[packageName] = package;
        _defaultPackage = package;
        return package;
    }

    // 按需初始化分包（ETPro: OtherPackageUpdateProcess）
    public async UniTask<ResourcePackage> InitSubPackageAsync(
        string packageName, EPlayMode playMode, IRemoteServices remoteServices)
    {
        var package = YooAssets.CreatePackage(packageName);
        // ... 同上
        _packages[packageName] = package;
        return package;
    }

    // 加载资产（ETPro: ResourcesComponentSystem.LoadAsync<T>）
    public async UniTask<T> LoadAssetAsync<T>(string location, string packageName = null) 
        where T : UnityEngine.Object
    {
        var pkg = packageName != null ? _packages[packageName] : _defaultPackage;
        var handle = pkg.LoadAssetAsync<T>(location);
        await handle.ToUniTask();
        return handle.AssetObject as T;
    }

    // 资源释放（ETPro: ResourcesComponent.ReleaseAsset）
    // 注意：ETPro 用的是 YooAsset 1.2.4 的旧版 API
    // YooAsset 2.3+/3.0 API 变了，handle.Release() 即可
}
```

---

### 2.5 Handle 缓存模式（所有项目共有）

这是最重要的模式——3 个主要项目都用 `Dictionary<string, HandleBase>` 缓存句柄：

```csharp
// KJ.Core/Resource/ResourceCache.cs
// 通用实现（不依赖任何框架特有概念）

public class ResourceCache : IDisposable
{
    private readonly Dictionary<string, HandleBase> _handles = new();
    private readonly SemaphoreSlim _gate = new(1, 1); // ← 替代 ET 的 CoroutineLock

    // ET 用 CoroutineLockType.ResourcesLoader 防并发加载
    // KJ 用 SemaphoreSlim 或 Channel
    public async UniTask<T> LoadAssetAsync<T>(string location, 
        ResourcePackage package) where T : UnityEngine.Object
    {
        // 检查缓存（ET 的 TryGetValue 模式）
        if (_handles.TryGetValue(location, out var cached))
        {
            return (cached as AssetHandle).AssetObject as T;
        }

        // 并发保护（替代 ET 的 CoroutineLock）
        await _gate.WaitAsync();
        try
        {
            // 双重检查（其他协程可能已加载）
            if (_handles.TryGetValue(location, out cached))
                return (cached as AssetHandle).AssetObject as T;

            var handle = package.LoadAssetAsync<T>(location);
            await handle.ToUniTask();
            _handles[location] = handle;
            return handle.AssetObject as T;
        }
        finally { _gate.Release(); }
    }

    public void Dispose()
    {
        // 所有项目的 Destroy 模式：遍历释放
        foreach (var kv in _handles)
        {
            switch (kv.Value)
            {
                case AssetHandle h:       h.Release(); break;
                case AllAssetsHandle h:   h.Release(); break;
                case SubAssetsHandle h:   h.Release(); break;
                case RawFileHandle h:     h.Release(); break;
                case SceneHandle h:       h.UnloadAsync(); break;
            }
        }
        _handles.Clear();
    }
}
```

**ET CoroutineLock → KJ 等价方案：**
| ET 做法 | KJ 可选方案 |
|---------|-----------|
| `CoroutineLockComponent.Wait(CoroutineLockType.ResourcesLoader, hash)` | `SemaphoreSlim(1,1)` + `WaitAsync()` |
| `using var lock = await ...` | `try/finally { _gate.Release(); }` |
| 按 location hash 分 key | 同上，或直接用 `ConcurrentDictionary` |

---

### 2.6 两层资源管理架构（Legends-Of-Heroes 模式）

```
┌──────────────────────────────────────────────────┐
│  Layer 1: 全局 ResourceManager (VContainer 单例)  │
│  - 任务：包生命周期、基础设施资源（DLL/Config）    │
│  - 时机：Boot 阶段，无纤程、无完整容器             │
│  - API：LoadAssetAsync<T>(location)               │
├──────────────────────────────────────────────────┤
│  Layer 2: 场景级 ResourceCache (按需创建)          │
│  - 任务：游戏内资源加载、Handle 缓存               │
│  - 时机：各系统/场景运行时                         │
│  - API：LoadAssetAsync<T>(location) + 缓存        │
│  - 生命周期：绑定到场景/系统，Dispose 时清理       │
└──────────────────────────────────────────────────┘
```

**KJ 映射：**
- Layer 1 → `Core/ResourceManager`（Core 层，ISystem，Priority 最高）
- Layer 2 → 各系统自己的 `ResourceHandle` 管理（传递 `ResourcePackage` 引用）

---

### 2.7 YooAsset 版本差异注意事项

| 特性 | YooAsset 1.x (ETPro用) | YooAsset 2.3+ (cn.etetet包) | YooAsset 3.0 (推荐) |
|------|----------------------|---------------------------|-------------------|
| 初始化 | `InitializeAsync(params)` | 同上 | 同上 |
| 包管理 | `CreatePackage()`, `SetDefaultPackage()` | 同上 | 同上 |
| 加载 | `LoadAssetAsync<T>()` → `AssetOperationHandle` | `LoadAssetAsync<T>()` → `AssetHandle` | 同左 |
| 释放 | `handle.Release()` | 同左 | 同左 |
| 句柄类型 | 旧版命名 | `AssetHandle`, `SceneHandle`, `AllAssetsHandle` 等 | 同左 |
| EPlayMode | `EditorSimulateMode`, `OfflinePlayMode`, `HostPlayMode` | 4 种（+ `WebPlayMode`） | 同左 |

**⚠️ ETPro 用的是 YooAsset 1.2.4（API 旧），你的 KJ 应该直接上 YooAsset 3.0。**

---

## 三、HybridCLR + YooAsset 协作模式

### 3.1 所有项目的共同流程

```
1. 应用启动 → YooAsset 初始化（HostPlayMode）
2. 版本检查 → 下载热更 DLL（通过 YooAsset 加载 Code.DLL.bytes）
3. HybridCLR LoadMetadataForAOTAssembly（AOT元数据也走 YooAsset）
4. Assembly.Load(热更DLL字节)
5. 启动热更层入口
```

### 3.2 CodeLoader 模式（ETPro → KJ 通用化）

```csharp
// KJ.Boot/HotfixLoader.cs
public class HotfixLoader
{
    private readonly PackageManager _packageManager;

    public async UniTask<Assembly> LoadHotfixAsync()
    {
        // 1. 从 YooAsset 加载 AOT 元数据 DLL（HybridCLR 需要）
        foreach (var aotDllName in GetAotMetaAssemblyNames())
        {
            var aotHandle = _packageManager.LoadAssetAsync<TextAsset>($"AOT/{aotDllName}.bytes");
            await aotHandle;
            var aotData = (aotHandle.AssetObject as TextAsset).bytes;
            HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aotData,
                HybridCLR.HomologousImageMode.SuperSet);
        }

        // 2. 从 YooAsset 加载热更 DLL
        var dllHandle = _packageManager.LoadAssetAsync<TextAsset>("Code/Hotfix.dll.bytes");
        await dllHandle;
        var dllBytes = (dllHandle.AssetObject as TextAsset).bytes;

        // 3. 加载 PDB（可选，调试用）
        // var pdbHandle = _packageManager.LoadAssetAsync<TextAsset>("Code/Hotfix.pdb.bytes");

        // 4. 加载程序集
        return Assembly.Load(dllBytes);
    }
}
```

### 3.3 构建管线（X-ET7 的 6 步流程 → KJ 版本）

```
Step 1: 编译热更 DLL       → dotnet build Hotfix.csproj
Step 2: HybridCLR 生成     → HybridCLR/Generate/All
Step 3: 首次 IL2CPP 构建   → 提取剥离的 AOT 元数据
Step 4: 复制 AOT DLL       → 将元数据放入 YooAsset 收集目录
Step 5: YooAsset 打包      → AssetBundle Builder（含 DLL + AOT）
Step 6: 最终出包           → BuildPlayer（含正确的 AB）
```

---

## 四、ETPro 更新管线（可复用）

ETPro 的 `MainPackageUpdateProcess.cs`（166行）——这是最完整的生产级实现：

```csharp
// 通用化后的更新流程
public class UpdatePipeline
{
    public async UniTask<bool> UpdateAsync(ResourcePackage package)
    {
        // 1. 获取资源版本
        var versionOp = package.RequestPackageVersionAsync();
        await versionOp.ToUniTask();
        var version = versionOp.PackageVersion;

        // 2. 更新清单（ETPro: UpdatePackageManifestAsync）
        var manifestOp = package.UpdatePackageManifestAsync(version);
        await manifestOp.ToUniTask();

        // 3. 创建下载器
        var downloader = package.CreateResourceDownloader(10, 5); // maxConcurrency=10, retryCount=5
        if (downloader.TotalDownloadCount == 0)
            return true; // 无需下载

        // 4. 计算下载大小
        long totalSize = downloader.TotalDownloadBytes;
        // → 弹出确认对话框，用户确认后继续

        // 5. 开始下载（ETPro 用回调通知进度）
        downloader.OnDownloadProgressCallback = 
            (total, current, fileTotal, fileCurrent) =>
        {
            // 更新进度条: (float)current / total
        };

        downloader.BeginDownload();
        await UniTask.WaitUntil(() => downloader.IsDone);

        return downloader.Status == EOperationStatus.Succeed;
    }
}
```

**ETPro 的分包模式：**
```
DefaultPackage（首包）→ 登录/更新UI
OtherPackage（分包）  → 各渠道/各DLC资源
```
流程：先更新 DefaultPackage，展示更新UI；再按需更新 OtherPackage。

---

## 五、对象池 + YooAsset 协作（ETPro 独有）

ETPro 的 `GameObjectPoolComponentSystem.cs` (698行) 是最完整的预制体池化实现：

```csharp
// 核心流程
GameObjectPool.GetGameObjectAsync(prefabPath)
  → PoolCache.TryGet(prefabPath) → 命中? Instantiate + 返回
  → Unpooled: ResourcesComponent.LoadAsync<GameObject>(prefabPath)
    → YooAsset LoadAssetAsync
  → 实例化 N 个副本（预加载计数）
  → 缓存到池中
  → LRU 淘汰时调用 ResourcesComponent.ReleaseAsset()
```

**与纯资源加载的关键区别：** 对象池不仅缓存 Handle（AB引用），还缓存实例化的 GameObject。释放时既要 `Destroy(go)` 或 `SetActive(false)`，也要 `handle.Release()`。

---

## 六、精灵/图集加载（ETPro 独有）

ETPro 的 `ImageLoaderComponentSystem.cs` (460行) 实现了三级精灵加载：

```
路径模式              加载方式              缓存策略
------------------------------------------------------
/Sprite/xxx          LoadAssetAsync<Sprite>        LRU
/Atlas/xxx           LoadAssetAsync<SpriteAtlas>    LRU  
/DynamicAtlas/xxx    动态合批纹理                  LRU（同时刷脏）
```

参考计数机制：每张图被几个 UI 引用，refCount=0 时从缓存移除。

---

## 七、对你 KJ 项目的具体建议

### 7.1 架构位置

```
Boot/
  YooAssetConfig.cs         ← ScriptableObject (PlayMode + CDN URL)
  ResourceInitSystem.cs     ← [CoreSystem] Priority=-999，最早初始化，调 YooAssets.InitializeAsync
  HotfixLoader.cs           ← HybridCLR 热更DLL加载（也走 YooAsset）

Core/
  PackageManager.cs         ← 中央包管理器，创建/管理 ResourcePackage
  ResourceCache.cs          ← Handle 缓存 + 释放
  IRemoteServices.cs + RemoteServices.cs  ← CDN URL 构建
  ResourceUpdateManager.cs  ← 更新管线（版本检查/下载/进度通知）

General/
  LubanTablesLoader.cs  ← 从 YooAsset 加载 Luban 生成的配置表 bytes
```

### 7.2 优先级排序

```
系统初始化优先级：
  ResourceInitSystem   Priority = 0   ← 最早，YooAsset 必须先初始化
  HotfixLoader          Priority = 1   ← 加载热更 DLL（如果有）
  ConfigManager         Priority = 2   ← 加载 Luban 配置表（依赖 YooAsset）
  SystemManager         Priority = 3   ← 触发 AppStartedEvent，其他系统可以启动
```

### 7.3 关键决策

| 决策 | 建议 | 依据 |
|------|------|------|
| YooAsset 版本 | **3.0** | 所有 ET 新项目都用 3.0，1.x 已过时 |
| PlayMode 配置方式 | **ScriptableObject** | 简单，不需要条件编译 |
| Handle 缓存粒度 | **场景级** | Legends-Of-Heroes 和 ETPro 都用两层架构 |
| 并发控制 | **SemaphoreSlim** | 替代 ET 的 CoroutineLock |
| 构建管线 | Editor 菜单 + Shell | X-ET7 6步流程通用 |
| 加密 | XOR（先不配） | ETPro 用的 XOR，排期到 Phase 3 |
