# YooAsset 与 HybridCLR 学习指南

> 基于 KJ 项目的实际使用方式编写。读完这份指南，你应该能理解这两个库的**核心概念**以及它们**在项目中的具体用法**。

---

## 第一部分：YooAsset — 资源管理

### 1. YooAsset 是什么？

YooAsset 是一个 Unity 资源管理系统，解决的核心问题是：**如何把游戏资源（prefab、贴图、场景、DLL 等）从项目里拆出去，按需下载和加载。**

没有 YooAsset 时，所有资源打包在 `AssetBundle` 或直接在 `Resources` 里，玩家下载的 APK/IPA 体积巨大。有了 YooAsset，资源可以放在远程 CDN 上，玩家边玩边下载（或者启动时先下载必要的）。

### 2. 核心概念速览

| 概念 | 含义 |
|------|------|
| **Package** | 一个资源包的逻辑分组。一个游戏可以有多套资源包（如"默认包"、"DLC 包"） |
| **AssetBundle** | Unity 原生的资源打包格式。YooAsset 在它之上做了一整套管理 |
| **Play Mode** | YooAsset 的资源加载模式，有三种 |
| **ResourcePackage** | YooAsset 中代表一个资源包的管理对象 |
| **AssetHandle** | 资源的"引用句柄"——持有它，资源就不会被释放 |
| **RawFile** | 非 Unity 资源格式的原始文件（如 `.dll.bytes`、`.json`）

### 3. 三种 Play Mode

这是理解 YooAsset 最重要的概念：

```
┌────────────────────┬──────────────────────────────────────────┐
│ Play Mode          │ 行为                                      │
├────────────────────┼──────────────────────────────────────────┤
│ EditorSimulate     │ Editor 下模拟，直接从项目目录加载，       │
│                    │ 不真实打包。开发时最常用。                 │
├────────────────────┼──────────────────────────────────────────┤
│ Offline            │ 从 StreamingAssets 里加载内置资源，       │
│                    │ 不从网络下载。适用于首次安装后、没网的场景。│
├────────────────────┼──────────────────────────────────────────┤
│ Host               │ 从 StreamingAssets 加载内置资源，         │
│                    │ 同时从远程 CDN 下载更新。线上真实模式。    │
└────────────────────┴──────────────────────────────────────────┘
```

**KJ 项目中的配置位置**：`Assets/Framework/Asset/AssetConfig.cs`

```csharp
// 15 行（位于 Framework/Asset/AssetConfig.cs）
public enum PlayMode
{
    EditorSimulate,  // Editor 开发用
    Offline,         // 仅内置资源，不联网
    Host             // 联网从 CDN 下载更新
}
```

### 4. KJ 项目如何使用 YooAsset

#### 4.1 架构：隔离 YooAsset 依赖

KJ 项目的关键设计原则是：**上层代码永远不直接引用 YooAsset**。

```
Project/General/Core  →  IAssetSystem（稳定接口）
                              ↓
Framework/Asset       →  AssetRuntime（YooAsset 适配实现）
                              ↓
                        YooAsset（第三方库，可以随时换）
```

**上层使用的接口**（`Assets/Framework/Asset/IAssetSystem.cs`）：
```csharp
public interface IAssetSystem
{
    UniTask<T> LoadAssetAsync<T>(string path) where T : Object;
    UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path) where T : Object;
    UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent = null);
    UniTask<AssetSceneHandle> LoadSceneAsync(string path, ...);
    void Release<T>(string path);
    void Release(string path);
    void UnloadUnused();
}
```

业务代码只调这些方法，完全不知道背后是 YooAsset。

#### 4.2 初始化流程

在 `BootUpdateRunner.InitializeAssets()` 中（`Assets/Scripts/Boot/BootUpdateRunner.cs`）：

```csharp
// 1. 读取配置
var config = Resources.Load<AssetConfig>("AssetConfig");

// 2. 异步初始化 YooAsset
var initHandle = _assetRuntime.BeginInitialize(config);
//     ↓ 内部调用链：
//     YooAssets.Initialize();
//     _defaultPackage = YooAssets.CreatePackage("DefaultPackage");
//     _defaultPackage.InitializePackageAsync(BuildOptions(config));
//
//     BuildOptions 根据 config.Mode 返回不同的选项：
//     - EditorSimulate → 模拟模式参数
//     - Offline → 内置文件系统参数
//     - Host → 内置 + 远程 CDN 文件系统参数

// 3. 协程里轮询等待完成
while (!initHandle.IsDone) { yield return null; }
```

#### 4.3 资源更新流程

初始化之后是版本检查和资源下载（同一文件的 `UpdateAssets()`）：

```csharp
// 1. 请求最新资源版本号
manifest = _assetRuntime.UpdateManifest();   // → RequestPackageVersionAsync
//    这一步向 CDN 请求：现在最新版本是多少？

// 2. 更新资源清单（知道有哪些资源要下载）
manifest.StartManifest();                    // → UpdatePackageManifestAsync

// 3. 创建下载器
var downloader = _assetRuntime.CreateDownloader();  // → CreateResourceDownloader
// 4. 下载所有需要更新的资源
downloader.Start();
while (!downloader.IsDone) { yield return null; }
```

#### 4.4 加载资源的两种通道

`AssetRuntime` 提供了两种加载模式（`Assets/Framework/Asset/AssetRuntime.cs`）：

**通道一：Owned（非缓存）→ `LoadAssetHandleAsync<T>`**

```csharp
// 每次调用都直接向 YooAsset 发起新的加载请求
var handle = await assetSystem.LoadAssetHandleAsync<GameObject>("path/to/prefab");
// handle 持有者负责 Dispose，Dispose 时释放底层 YooAsset handle
var instance = handle.Instantiate(parent);
// ...
handle.Dispose();  // 释放资源
```

**通道二：Cached（引用计数缓存）→ `LoadAssetAsync<T>`**

```csharp
// 同一个 key 多次调用只加载一次，缓存 YooAsset handle
var prefab1 = await assetSystem.LoadAssetAsync<GameObject>("path/to/prefab");
var prefab2 = await assetSystem.LoadAssetAsync<GameObject>("path/to/prefab");
// prefab1 和 prefab2 是同一个实例，共享底层 handle
// 释放时调用 assetSystem.Release<GameObject>("path/to/prefab")
```

两种通道的对比：

| | Owned (`LoadAssetHandleAsync`) | Cached (`LoadAssetAsync`) |
|---|---|---|
| 每次调用 | 新建 YooAsset handle | 复用已有 handle |
| 生命周期 | 调用方通过 Dispose 控制 | AssetRuntime 统一管理 |
| 适用场景 | 临时加载后很快释放 | 频繁使用的共享资源 |
| 释放 | `handle.Dispose()` | `assetSystem.Release<T>(path)` |

#### 4.5 RawFile 加载（DLL 等非 Unity 资源）

```csharp
byte[] bytes = _assetRuntime.LoadRawBytes("Assets/GameRes/HotUpdate/Dlls/Core.dll.bytes");
```

这是 `LoadAssetSync<RawFileObject>` 的同步封装，专门用于加载 `.dll.bytes` 等 RawFile 资源。

#### 4.6 YooAsset Collector 配置

在 Editor 中，哪些资源要打包、以什么方式打包，由 **Bundle Collector** 决定。

KJ 项目通过 `KJHybridClrBuildTools.EnsureYooAssetCollector()` 自动配置：

```csharp
// 创建一个 Package → 创建一个 Group → 添加 Collector
// Collector 配置：
//   CollectPath  = "Assets/GameRes/HotUpdate/Dlls"
//   PackRule     = PackRawFile        ← 作为原始文件打包，不做 AssetBundle 处理
//   FilterRule   = CollectAll         ← 收集该目录下所有文件
//   AssetTags    = "hotupdate"
```

编辑器窗口：`YooAsset → AssetBundle Collector` 可以可视化查看和管理这些配置。

---

## 第二部分：HybridCLR — 代码热更新

### 1. HybridCLR 是什么？它解决什么问题？

**普通 iOS/Android 游戏的问题**：Unity 会把所有 C# 代码编译成 IL2CPP 的 C++ 代码，再编译成原生机器码。这意味着——改一行 C# 代码就得重新出包、重新上架应用商店。一个更新周期可能好几天。

**HybridCLR 做了什么**：它是一个 IL2CPP 的扩展。它在运行时可以解释执行 C# DLL（.NET 字节码/IL），这样 C# 代码就可以像资源一样从 CDN 下发，不需要重新出包。

```
传统 IL2CPP：
C# 源码 → [编译] → IL 字节码 → [IL2CPP] → C++ → [编译] → 原生机器码
                                                          ↑
                                               必须随 App 包一起分发

HybridCLR：
C# 源码 → [编译] → IL 字节码 → [IL2CPP] → C++ → [编译] → 原生机器码（稳定部分）
              ↘
                [解释执行] ← 可以从 CDN 下发，不需要重新出包！
```

### 2. 核心概念速览

| 概念 | 含义 |
|------|------|
| **AOT（Ahead-of-Time）** | 提前编译成机器码。IL2CPP 把部分代码编译成 AOT，运行时不能改 |
| **解释执行（Interpreter）** | HybridCLR 在运行时逐条解释执行 IL 字节码，比 AOT 慢但可以热更 |
| **AOT 补充元数据（Supplemental Metadata）** | 热更 DLL 调用 AOT 代码时需要"补充元数据"来弥合 AOT 和解释器之间的鸿沟 |
| **Hot Update Assembly** | 可以热更的程序集（DLL），如 `Core.dll`、`General.dll`、`Project.dll` |
| **HomologousImageMode** | 元数据加载模式。`SuperSet` 表示补充元数据是 AOT 元数据的超集 |
| **Patch AOT Assemblies** | 需要在打包时生成补充元数据的 AOT 程序集列表 |

**为什么需要 AOT 补充元数据？**

简单的类比：AOT 代码是一本编译好的"快查手册"（机器码，直接跳转），热更 DLL 是"新写的笔记"（IL 字节码，解释执行）。新笔记有时需要引用快查手册里的条目，但两者格式不互通。AOT 补充元数据就像给新笔记一份"快查手册的索引"——告诉解释器 AOT 代码的函数签名和类型布局。

### 3. KJ 项目的程序集分组

KJ 项目把程序集分成两组（参考 `.planning/HOT_UPDATE_BOUNDARY.md`）：

```
┌──────────────────────────────────────────────────────┐
│ AOT / 包内（必须随 App 包分发，非常稳定）              │
│                                                      │
│   Boot        启动壳，仅 ~300 行                       │
│   Framework.* Asset/Pool/Cache/Event/Log              │
│   Packages     VContainer、UniTask、YooAsset...        │
│   Unity        引擎内置                                │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│ 热更程序集（可以从 CDN 下发新版本 DLL，无需重装 App）   │
│                                                      │
│   Core         引擎基础设施（SystemManager, DI...）     │
│   General      通用业务（登录、红点...）                │
│   Project      项目专属业务                            │
│   Boot.Update  未来拆分的启动更新流程（未实现）          │
└──────────────────────────────────────────────────────┘
```

**关键理解**：热更 DLL 替换后需要**重启 App**（下次冷启动生效），因为 HybridCLR 社区版不支持运行时卸载已加载的程序集。

### 4. KJ 项目的完整启动链路

这是理解所有内容的关键 —— 一条协程串联了 YooAsset 和 HybridCLR：

```
Entry.Awake()
  └→ BootUpdateRunner.Run()    [协程]
       │
       ├─ ① InitializeAssets()      ← YooAsset 初始化
       │   └→ Resources.Load<AssetConfig>("AssetConfig")
       │   └→ _assetRuntime.BeginInitialize(config)
       │
       ├─ ② UpdateAssets()          ← YooAsset 资源更新
       │   └→ _assetRuntime.UpdateManifest()    // 检查版本
       │   └→ _assetRuntime.CreateDownloader()  // 下载更新
       │
       ├─ ③ LoadHotUpdateCode()     ← HybridCLR 加载热更代码
       │   ├→ LoadAotMetadata()
       │   │   └→ LoadRawBytes(marshal.dll.bytes)        // 通过 YooAsset!
       │   │   └→ HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(bytes, SuperSet)
       │   │
       │   └→ LoadHotUpdateAssemblies()
       │       └→ LoadRawBytes(Project.dll.bytes)        // 通过 YooAsset!
       │       └→ Assembly.Load(bytes)                   // 加载 DLL 到内存
       │       └→ LoadRawBytes(General.dll.bytes)
       │       └→ Assembly.Load(bytes)
       │       └→ LoadRawBytes(Core.dll.bytes)
       │       └→ Assembly.Load(bytes)
       │
       └─ ④ StartGame()             ← 反射启动热更层
           └→ Type.GetType("Project.Bootstrap.ProjectStartup, Project")
           └→ method.Invoke(null, new[] { _assetRuntime })
               └→ ProjectStartup.Start(IAssetRuntime)
                   └→ new GameObject → AddComponent<ProjectLifetimeScope>
                       └→ VContainer: Core → General → Project 注册
```

### 5. Boot 层的关键设计细节

#### 5.1 Boot 不直接引用 HybridCLR

注意 `HybridClrReflection.cs`（`Assets/Scripts/Boot/HybridClrReflection.cs`）：

```csharp
// 全部通过反射调用，避免 Boot.asmdef 直接依赖 HybridCLR.Runtime
var runtimeApiType = Type.GetType("HybridCLR.RuntimeApi, HybridCLR.Runtime");
var method = runtimeApiType.GetMethod("LoadMetadataForAOTAssembly", ...);
var result = method.Invoke(null, new object[] { bytes, mode });
```

这样 Boot 层可以保持极小的依赖面。

#### 5.2 DLL + AOT Metadata 的加载优先级

`LoadBytesCoroutine` 中有三级 fallback（`BootUpdateRunner.cs:273-318`）：

```
优先级 1: YooAsset RawFile    → _assetRuntime.LoadRawBytes(assetPath)
优先级 2: StreamingAssets      → UnityWebRequest / File.ReadAllBytes
优先级 3: Resources            → Resources.Load<TextAsset>
```

线上环境用优先级 1（通过 YooAsset 下发），本地开发兜底用 2 和 3。

#### 5.3 启动配置全部序列化

`BootStartupSettings` 存储在 Entry prefab 上，包含：
- 是否启用资源更新 / 热更新
- 是否在 Editor 中跳过热更（`skipHotUpdateInEditor = true`）
- AOT metadata 程序集列表
- 热更 DLL 程序集列表
- 启动入口类型名和方法名

这意味着不需要改代码就可以调整启动行为。

### 6. HybridCLR 构建工具链

菜单路径 `KJ/HybridCLR/`，核心工具位于 `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs`：

| 菜单 | 作用 |
|------|------|
| `Generate All And Sync` | 完整 HybridCLR 生成 + 同步 DLL |
| `Prepare Runtime Assets And Boot` | **日常开发用**：只编译 DLL + 同步 + 准备 EditorSimulate + 更新 Entry 配置 |
| `Generate Runtime Assets And Sync` | 编译热更 DLL + AOT metadata + 同步 |
| `Compile Dlls And Sync` | 只编译热更 DLL |
| `Prepare YooAsset Editor Simulate Package` | 生成 YooAsset 模拟包，让 Editor Play 模式可用 |
| `Prepare Boot Scene` | 打开 Boot 场景 + 更新 Entry 配置 + 加入 Build Settings |
| `Apply To Open Entry` | 把 HybridCLRSettings 中的程序集列表写入当前打开的 Entry |
| `Validate Outputs` | 校验同步产物是否完整 |

**典型开发流程**：
1. `KJ/HybridCLR/Prepare Runtime Assets And Boot` → 编译 DLL、同步、准备模拟
2. 在 Unity Editor 中 Play → 通过 EditorSimulate + skipHotUpdateInEditor 运行
3. 改 C# 代码后，重复步骤 1

**典型打包流程**：
1. `KJ/HybridCLR/Generate All And Sync`
2. `KJ/HybridCLR/Prepare Boot Scene`
3. Build Player

### 7. 热更新能做什么、不能做什么

来自 `.planning/HOT_UPDATE_BOUNDARY.md` 的总结：

| 场景 | 做法 |
|------|------|
| 修改 Core/General/Project 的 C# 逻辑 | 编译新 DLL，上 CDN，用户下载后**下次启动**生效 |
| 修改 Prefab/贴图/配置等资源 | 上 CDN，用户下载后**不需要重启**即可生效（如果是未加载的资源） |
| 修改 Boot 的 C# 逻辑 | 当前版本需要重新出包（Boot 作为 AOT 包内代码）。未来拆出 `Boot.Update` 后可热更但需重启 |
| 修改 Unity 引擎或 Native 代码 | **必须重新出包** |

---

## 第三部分：二者如何协作

YooAsset 和 HybridCLR 在 KJ 项目中是**协作关系**，不是竞争关系：

```
YooAsset 负责：
  ├── 管理所有资源（prefab、贴图、场景...）
  ├── 版本检查与 CDN 下载
  ├── 把 .dll.bytes 作为 RawFile 资源下发  ←── 这是桥梁！
  └── 提供 IAssetRuntime 给启动流程

HybridCLR 负责：
  ├── 在运行时解释执行 C# 代码
  ├── 加载 AOT 补充元数据
  └── 让 Assembly.Load(bytes) 加载的 DLL 可以工作

二者的桥梁：
  Boot → YooAsset.LoadRawBytes("Core.dll.bytes")  ← 通过 YooAsset 下载 DLL
       → Assembly.Load(bytes)                     ← 通过 HybridCLR 加载执行
```

**一句话总结**：YooAsset 是"怎么把 DLL 文件下载到手机"，HybridCLR 是"怎么让下载来的 DLL 跑起来"。

---

## 第四部分：如果从零开始使用

如果你想在新项目中引入这套方案，推荐顺序：

1. **先看懂启动链路**：`Entry.cs → BootUpdateRunner.cs`，它是所有内容的入口
2. **理解 AssetConfig 的 Play Mode**：开发用 EditorSimulate，线上用 Host
3. **理解 IAssetSystem 接口**：这是上层代码唯一需要知道的资源加载方式
4. **理解 HybridCLR 的两个产物**：热更 DLL + AOT 补充元数据
5. **理解 HybridCLR 构建工具**：`KJ/HybridCLR/` 菜单下的工具链
6. **阅读热更新边界文档**：`.planning/HOT_UPDATE_BOUNDARY.md`

**关键文件速查**：

| 想看什么 | 文件 |
|----------|------|
| 启动入口 | `Assets/Scripts/Boot/Entry.cs` |
| 启动流程 | `Assets/Scripts/Boot/BootUpdateRunner.cs` |
| YooAsset 适配 | `Assets/Framework/Asset/AssetRuntime.cs` |
| 资源接口 | `Assets/Framework/Asset/IAssetSystem.cs` |
| 资源配置 | `Assets/Framework/Asset/AssetConfig.cs` |
| Boot 配置 | `Assets/Scripts/Boot/BootStartupSettings.cs` |
| HybridCLR 反射 | `Assets/Scripts/Boot/HybridClrReflection.cs` |
| HybridCLR 工具 | `Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs` |
| Core 桥接 | `Assets/Scripts/Core/Asset/AssetSystem.cs` |
| 项目入口 | `Assets/Scripts/Project/Bootstrap/ProjectStartup.cs` |
| 架构全貌 | `.planning/PROJECT.md` |
| 热更边界 | `.planning/HOT_UPDATE_BOUNDARY.md` |
