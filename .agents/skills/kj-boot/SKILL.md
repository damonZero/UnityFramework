---
name: kj-boot
description: >
  KJ Framework Boot 层指南。涵盖 Entry（游戏入口+MonoBehaviour+DontDestroyOnLoad）、BootStartupSettings（Entry 序列化启动配置）、BootUpdateRunner（资源版本检查/清单更新/下载/HybridCLR DLL 与 AOT metadata 加载/反射启动正式入口）、BootAssemblyEntry/BootMetadataEntry（热更程序集与补充元数据配置）、HybridClrReflection（反射调用 HybridCLR.RuntimeApi）、IBootStartupView（启动更新 UI 最小接口）。触发场景：理解启动流程、配置热更 DLL/AOT metadata、调试 Boot 到 ProjectStartup 的反射入口、保持 Boot 最小依赖、讨论 Boot.Update 拆分和重启策略。核心规则：Boot 只引用 Framework.Asset，不引用 VContainer/Core/General/Project；Boot 只做资源/代码更新和反射启动；正式 VContainer root 由 ProjectStartup/ProjectLifetimeScope 创建；Login 等业务流程放 General/Project；C# 层更新不等同必须换包，已加载程序集替换通常重启/下次启动生效。
metadata:
  doc: CODEMAP.md
  layer: Boot
---

# KJ Boot 层 — 启动更新壳

源码在 `Assets/Scripts/Boot/`，完整文档见 `CODEMAP.md` Layer: Boot 章节和 `.planning/HOT_UPDATE_BOUNDARY.md`。

## 架构速查

```
Entry.cs                 — Awake() → DontDestroyOnLoad(gameObject) → RunStartup()
BootStartupSettings.cs   — Entry 序列化配置：资源更新、热更 DLL/AOT metadata、正式入口
BootUpdateRunner.cs      — 初始化 Framework.Asset、更新清单/资源、加载 metadata/DLL、反射启动
BootAssemblyEntry.cs     — 热更 DLL 条目（assemblyName/fileName/resourcesPath/assetPath）
BootMetadataEntry.cs     — AOT metadata 条目
HybridClrReflection.cs   — 反射调用 HybridCLR.RuntimeApi，Boot 不直接引用 HybridCLR.Runtime
IBootStartupView.cs      — 启动 UI 最小接口：状态、进度、修复按钮可见性
```

## 当前启动流程

```
Entry.Awake()
  ↓
DontDestroyOnLoad(gameObject)
  ↓
BootUpdateRunner.Run()
  ├─ Resources.Load<AssetConfig>("AssetConfig")
  ├─ AssetRuntimeFactory.Create().BeginInitialize(config) 并在协程中轮询
  ├─ UpdateManifest(): 请求资源版本并更新清单
  ├─ CreateDownloader(tag): 下载资源/热更 DLL/AOT metadata RawFile
  ├─ LoadMetadataForAotAssembly(bytes) by HybridClrReflection
  ├─ Assembly.Load(Core/General/Project dll bytes)
  └─ reflect Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)
       ↓
       ProjectStartup 创建 ProjectLifetimeScope
       ↓
       ProjectLifetimeScope 串联 CoreBootstrapStage → GeneralBootstrapStage → ProjectBootstrapStage
```

## 核心约束

- Boot asmdef 只引用 `Framework.Asset`，不引用 VContainer、Core、General、Project、MessagePipe 或 HybridCLR.Runtime。
- Boot 只做启动期资源/代码更新、修复入口、最小进度 UI 和反射启动正式游戏环境。
- Boot 不创建正式业务容器；正式 VContainer root 由 `Project.Bootstrap.ProjectStartup` 创建。
- `BootStartupSettings` 是 Entry/prefab/scene 序列化配置，热更 DLL/AOT metadata 列表不要硬编码在 Boot 代码里。
- 启动 UI 只能承载更新/修复功能；登录、公告、服务器列表、账号 SDK 和角色选择属于 General/Project 业务。
- Boot 创建的 `IAssetRuntime` 在成功启动后交给 Project/Core 复用，避免创建第二套 YooAsset runtime。
- YooAsset 3.0 package 初始化不支持 `WaitForCompletion()`；Boot 必须用协程轮询 `BeginInitialize()` 返回的 `AssetInitializeHandle`。
- C# 层改动不等同于必须换包；如果旧 DLL 已加载，新 DLL 通常需要重启 APP/下次启动生效。
- 真正必须换包只限 native/player/HybridCLR 底层加载机制、IL2CPP 产物、Java/OC/C++、原生插件，或旧包缺少对应托管加载能力。

## 热更边界

当前工具默认将 `Core` / `General` / `Project` 写入 Entry 的正式运行时预加载 DLL 列表。

`Boot` / `Framework` 的托管更新需要单独设计：

1. 启动更新 manifest。
2. 加载顺序。
3. API/ABI 兼容与 AOT metadata 策略。
4. 重启分类：无重启、游戏内重启、APP 外重启/下次启动。

目标形态是未来拆出 `Boot.Update`：极薄 BootLoader 先加载 `Boot.Update`，由 `Boot.Update` 执行版本检查、修复按钮、启动更新 UI 和后续 DLL/资源下载。`Boot.Update` 更新后通常重启/下次启动生效。

## 配置热更程序集

`BootAssemblyEntry` 优先使用 YooAsset RawFile 路径：

```csharp
new BootAssemblyEntry(
    assemblyName: "Project",
    fileName: "Dlls/Project.dll.bytes",
    resourcesPath: null,
    assetPath: "Assets/GameRes/HotUpdate/Dlls/Project.dll.bytes");
```

`BootMetadataEntry` 同理用于补充元数据：

```csharp
new BootMetadataEntry(
    assemblyName: "mscorlib",
    fileName: "AotMetadata/mscorlib.dll.bytes",
    resourcesPath: null,
    assetPath: "Assets/GameRes/HotUpdate/AotMetadata/mscorlib.dll.bytes");
```

日常使用 Editor 菜单生成并回写 Entry：

```
KJ/HybridCLR/Prepare Runtime Assets And Boot
```

该菜单会同时生成 YooAsset EditorSimulate 虚拟 RawFile 包，并把输出 root 写入 `Assets/Resources/AssetConfig.asset` 的 `EditorSimulatePackageRoot`。如果只改了 YooAsset 收集或资源配置，也可以单独运行：

```
KJ/HybridCLR/Prepare YooAsset Editor Simulate Package
```

正式构建前使用完整流程：

```
KJ/HybridCLR/Generate All And Sync
```

## 当前验证 Gate

当前阶段先暂停新增业务模块，优先确认底层框架稳定：

1. Editor Play 启动链已验证：用户确认无报错，`Editor.log` 已看到 `[AssetSystem] Ready` 与 `[SystemManager] 全部初始化完成`。
2. 下一步做 Player 打包 smoke：正式打包前跑 `Generate All And Sync`，构建并运行 Player，确认 Boot -> YooAsset init -> manifest/download -> AOT metadata/DLL load -> ProjectStartup -> Core/SystemManager 全链路成功。
3. 资源加载矩阵验证：RawFile、cached/owned asset、Instantiate、Scene load/unload、Downloader、Release、UnloadUnused。
4. 热更新 smoke：修改 `Project` 层代码/资源后重新同步，验证无需整包；已加载 DLL 替换需重启/下次启动生效。

未完成 Player smoke 与资源加载矩阵前，不把 UI/Login/Config/Network 作为下一步默认推荐。

## 最佳实践

1. Entry 永远保持最小：`DontDestroyOnLoad`、启动协程、修复重试入口。
2. Boot 代码不要引用业务类型；只通过 `startupTypeName` / `startupMethodName` 反射调用正式入口。
3. 启动更新失败时显示修复按钮，不进入登录流程。
4. 资源更新必须先于登录/业务流程；登录模型放 `General/Login/`。
5. 新增 Boot 功能前先判断是否可以放到未来 `Boot.Update` 或更高热更层。
6. 修改 Boot/Framework 稳定契约时，同步考虑调用方 DLL、AOT metadata、旧包加载能力和重启策略。
