---
name: kj-asset
description: >
  KJ Framework 资源系统指南。涵盖 IAssetSystem（统一资源加载 API）、IAssetRuntime（可初始化/关闭的资源运行时）、AssetHandle<T>（句柄模式，IDisposable）、AssetInstanceHandle（实例+源句柄联合生命周期）、AssetSceneHandle（场景异步加载/卸载，串行化保护）、AssetDownloadHandle（下载器封装）、AssetConfig（ScriptableObject 配置，PlayMode 策略模式）。
  触发场景：加载资源/场景、创建 AssetHandle、使用 InstantiateAsync、配置资源系统、实现资源下载、释放资源、理解 owned vs cached 通道。
  核心规则：所有 YooAsset 类型封装在 Framework.Asset 内部不对外暴露；上层只依赖 IAssetSystem 接口；AssetHandle 通过 IDisposable 管理生命周期；LoadAssetAsync 使用系统缓存（cached），LoadAssetHandleAsync 调用方自管（owned）。
metadata:
  doc: CODEMAP.md
  layer: Framework
---

# KJ 资源系统 (Framework.Asset)

完整源码在 `Assets/Framework/Asset/`，实现细节见 `CODEMAP.md` Framework: Asset 章节。

## 架构速查

```
IAssetSystem (对上层统一 API)
    ↑
        IAssetRuntime (扩展 bool Initialize/Shutdown/Config/IsReady)
    ↑
AssetRuntime (YooAsset 3.0 适配器，唯一引用 YooAsset 的文件)

AssetHandle<T>        — 类型句柄 (IDisposable)，构造函数和 Instantiate 为 internal
AssetInstanceHandle   — 实例+源句柄联合生命周期 (IDisposable)，构造函数为 internal
AssetSceneHandle      — 场景异步加载/卸载 (IDisposable)，构造函数为 internal
AssetDownloadHandle   — 下载器封装，构造函数为 internal
AssetConfig           — ScriptableObject: PlayMode + CDN + 超时/重试
```

## 核心 API

### IAssetSystem — 对上层暴露

```csharp
// 系统缓存通道（cached）— 多次加载同一路径共享句柄
UniTask<T> LoadAssetAsync<T>(string path) where T : Object;

// 调用方自管通道（owned）— 每次返回新句柄，调用方负责 Dispose
UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path) where T : Object;

// 实例化
UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent = null);

// 场景（串行化保护：同路径场景加载会等待前一个卸载完成）
UniTask<AssetSceneHandle> LoadSceneAsync(
    string path,
    LoadSceneMode mode = LoadSceneMode.Single,
    Action<float> onProgress = null);

// 下载器
AssetDownloadHandle CreateDownloader(string tag = null);
AssetDownloadHandle CreateDownloader(string[] tags);

// 释放
void Release<T>(string path) where T : Object;
void Release(string path);
void UnloadUnused();
```

### 双通道缓存设计

| 通道 | 方法 | 句柄管理 | 适用场景 |
|------|------|----------|----------|
| **cached** | `LoadAssetAsync<T>` | 系统管理，`Release<T>(path)` 释放 | 全局共享资源（UI 图集、音频） |
| **owned** | `LoadAssetHandleAsync<T>` | 调用方 `Dispose()` 释放 | 临时资源、需要独立生命周期的 |

**AssetCacheKey**: `AssetRuntime` 内部的 `private readonly struct`，key 为 `(string path, Type type)`，确保同一 path 的不同类型加载不会冲突。外部不可见，仅供理解内部去重逻辑。

**SemaphoreSlim(1,1) 并发保护**: cached 通道在 `LoadAssetAsync<T>` 内部使用 `_gate`（全局 SemaphoreSlim），防止同一资源被并发加载两次。

### AssetHandle<T> — 类型句柄

```csharp
var handle = await _assetSystem.LoadAssetHandleAsync<Texture2D>("Assets/Textures/hero.png");
handle.Progress   // float
handle.IsDone     // bool
handle.IsValid    // bool (未 Dispose 且底层有效)
handle.Error      // string
handle.Asset      // T (从 AssetObject as T)
handle.Dispose()  // 释放底层 YooAsset 句柄
```

> **注意**: `AssetHandle<T>` 的构造函数和 `Instantiate(Transform)` 均为 `internal`，外部代码不能直接 new 或调用同步实例化。实例化请通过 `IAssetSystem.InstantiateAsync()` 获取 `AssetInstanceHandle`。

### AssetInstanceHandle — 实例句柄

```csharp
var instanceHandle = await _assetSystem.InstantiateAsync("Assets/Prefabs/Bullet.prefab", parent);
instanceHandle.Instance  // GameObject
instanceHandle.Dispose() // 先 Destroy(GameObject) 再 Dispose(源 AssetHandle)
```

### AssetSceneHandle — 场景句柄

```csharp
var handle = await _assetSystem.LoadSceneAsync("Assets/Scenes/Battle.unity");
handle.ActivateScene();                // 激活场景
await handle.UnloadAsync();            // 等待卸载完成
handle.Dispose();                      // Fire-and-forget 卸载
```

**串行化保护**: `AssetRuntime` 内部维护 `_sceneUnloadTasks` 字典，同一路径的场景加载会等待已有的卸载 Task 完成。

### AssetConfig — 配置

```csharp
// ScriptableObject，放在 Resources/AssetConfig.asset
// AssetSystem.Init() 时自动加载
[CreateAssetMenu]
public class AssetConfig : ScriptableObject
{
    public enum PlayMode { EditorSimulate, Offline, Host }
    public PlayMode Mode;
    public string PackageName;          // "DefaultPackage"
    public string CdnBaseUrl;           // Host 模式 CDN 地址
    public int DownloadTimeout;         // 下载超时
    public int DownloadMaxConcurrency;  // 下载并发数
    public int FailedRetryCount;        // 下载失败重试
}
```

PlayMode 对应 YooAsset 初始化策略：
- `EditorSimulate` → 编辑器模拟（不打包直接加载）
- `Offline` → 离线模式（从 StreamingAssets 加载）
- `Host` → 在线模式（从 CDN 下载）

## Core 层编排

Core 层的 `AssetSystem` (`[CoreSystem] Priority=100`) 负责：
1. `Init()` — `Resources.Load<AssetConfig>("AssetConfig")` → `_runtime.Initialize(config)` 成功且 `_runtime.IsReady` → 发布 `AssetSystemReadyEvent`
2. `Shutdown()` — 释放所有 cached handles、场景 handles、owned handles，调 `_runtime.Shutdown()`

`Initialize` 返回 `false` 时必须保持 runtime 不可用并清理 YooAsset 状态；Core 不发布 ready event。

## 最佳实践

1. **优先用 LoadAssetAsync（cached 通道）** — 自动去重、共享句柄，适合大多数场景
2. **需要独立生命周期用 LoadAssetHandleAsync（owned 通道）** — 记得 Dispose
3. **场景用 LoadSceneAsync** — 自带串行化保护，不用担心快速切场景的竞态
4. **AssetConfig 放在 Resources/** — 这是唯一必须放 Resources 的配置
5. **不要在 hot path 上同步 Instantiate** — 重资源实例化尽量异步
6. **Release 要及时** — cached 通道不释放会累积句柄
7. **Resources 不放场景或启动 Stage prefab** — 场景放 `Assets/GameRes/Scene/{Layer}/`
