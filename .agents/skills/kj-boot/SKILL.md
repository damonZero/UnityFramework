---
name: kj-boot
description: >
  KJ Framework Boot 层指南（HYB-03 裂变后）。涵盖 AOT 壳 Launcher（Entry / BootLoader / BootBridge / BootStartupLog）与热更 Boot（BootUpdateRunner / BootRuntimeLogBootstrap）的启动链；HybridCLR 热更加载；启动配置 BootStartupSettings / IBootStartupView；AOT 阶段日志 BootStartupLog。触发场景：理解启动流程、配置热更 DLL/AOT metadata、调试 Boot 到 ProjectStartup 的反射入口、保持 Launcher(Boot) 最小依赖与边界、讨论 Boot.Update 拆分和重启策略。核心规则：Launcher(AOT) 只引用 UniTask/YooAsset/HybridCLR.Runtime/AssetShared，绝不引用任何 Framework.* 或热更程序集；Boot(热更) 引用 Asset/Log/RuntimeLog/UniTask/AssetShared/YooAsset/Launcher，不引用 VContainer/Core/General/Project/HybridCLR.Runtime；正式 VContainer root 由 ProjectStartup/ProjectLifetimeScope 创建。
metadata:
  doc: .planning/HOT_UPDATE_BOUNDARY.md
  layer: Boot
---

# KJ Boot 层 — 启动更新壳（HYB-03 裂变）

源码在 `Assets/Scripts/Boot/`，完整边界设计见 `.planning/HOT_UPDATE_BOUNDARY.md`。

## 架构速查

```
Assets/Scripts/Boot/
├── Launcher/                      → KJ.Launcher.asmdef (AOT 壳，HYB-03 裂变)
│   ├── Entry.cs                   — MonoBehaviour 入口：Awake → DontDestroyOnLoad → new BootLoader().RunAsync()
│   ├── BootLoader.cs              — AOT 启动壳：初始化 YooAsset、加载全部热更 DLL、反射 BootUpdateRunner
│   ├── BootBridge.cs              — 跨 AOT→热更边界的状态载体（Package/Settings/View/Config/EarlyLogs）
│   ├── BootStartupLog.cs          — AOT 阶段日志（纯文本 + 内存快照，不依赖 Framework.Log/RuntimeLog）
│   ├── IsExternalInit.cs
│   ├── Data/
│   │   ├── BootStartupSettings.cs — Entry 序列化启动配置（资源更新 / 热更 DLL / AOT metadata / 正式入口）
│   │   ├── BootAssemblyEntry.cs   — 热更 DLL 条目
│   │   ├── BootMetadataEntry.cs   — AOT metadata 条目
│   │   └── IBootStartupView.cs    — 启动 UI 最小接口（状态 / 进度 / 修复可见）
│   └── YooAssetStrategy/
│       └── BootRemoteService.cs   — AOT 侧 IRemoteService（死锁修复点）
├── BootUpdateRunner.cs            → KJ.Boot.asmdef（热更，由 Launcher 反射启动）
└── BootRuntimeLogBootstrap.cs    → 热更层早期安装 RuntimeLog session
```

## 当前启动流程

```
Entry.Awake()  (Launcher / AOT)
  ↓ DontDestroyOnLoad
  new BootLoader(startupSettings, view).RunAsync()
  ↓
BootLoader.RunAsync()  (AOT)
  ├─ 加载 AssetConfig（来自 AOT 共享程序集 AssetShared）
  ├─ 初始化 YooAsset 并创建默认 ResourcePackage
  ├─ 下载 + 加载全部热更 DLL（含 Boot 自身）via YooAsset RawFile API
  │     （绝不走热更 IAssetRuntime，否则形成 AOT→热更反向引用）
  ├─ 加载 AOT 补充 metadata（直接引用 HybridCLR.Runtime，AOT 侧）
  ├─ 构造 BootBridge（携带 Package / Settings / View / Config / EarlyLogs）
  └─ 反射 Boot.BootUpdateRunner.Start(bridge)   ← 移交热更层
        ↓
        BootUpdateRunner.RunAsync()  (热更 / Boot)
        ├─ AssetRuntime.WrapFromExistingPackage(bridge.Config, bridge.Package)
        ├─ 资源版本检查 / 清单更新 / 下载 / AOT metadata / Assembly.Load
        ├─ ReplayEarlyLogs()（把 AOT BootStartupLog 回放到 RuntimeLog）
        └─ 反射 Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)
              ↓
              ProjectStartup 创建 ProjectLifetimeScope (DontDestroyOnLoad)
              ↓
              ProjectLifetimeScope.Configure → CoreStartupContext
                → CoreBootstrapStage.Configure(context)
                → GeneralBootstrapStage.Configure(context)
                → ProjectBootstrapStage.Configure(context)   （正式 VContainer root）
```

## 核心约束

- **Launcher (AOT)**：`KJ.Launcher.asmdef` 只引用 `UniTask / YooAsset / HybridCLR.Runtime / AssetShared`。硬约束：不得引用任何 `Framework.*` 包或热更程序集（由 asmdef 强制）。它只定位并加载热更代码，通过反射字符串 `"Boot.BootUpdateRunner, Boot"` 调用热更入口，不编译期依赖 Boot。
- **Boot (热更)**：`KJ.Boot.asmdef` 引用 `Asset / Log / RuntimeLog / UniTask / AssetShared / YooAsset / Launcher`。**不引用 VContainer、HybridCLR.Runtime、Core / General / Project**。AOT metadata/DLL 加载由 Launcher 代为完成，Boot 自身不再直接引用 `HybridCLR.Runtime`。
- Boot 只做启动期资源/代码更新、修复入口、最小进度 UI 和反射启动正式游戏环境；不创建正式业务容器。
- 正式 VContainer root 由 `Project.Bootstrap.ProjectStartup` / `ProjectLifetimeScope` 创建，并复用 Boot 已初始化的 `IAssetRuntime`（通过 `BootBridge` → `WrapFromExistingPackage`）。
- 启动 UI 只承载更新/修复功能；登录、公告、服务器列表等属于 General/Project 业务。
- **反射入口契约**：`BootLoader` 用字面串 `"Boot.BootUpdateRunner, Boot"` 反射解析；程序集名是启动契约的一部分，改名需同步 `BootLoader` 与 `HybridCLRSettings`。同样 `BootStartupSettings.startupTypeName` 默认 `"Project.Bootstrap.ProjectStartup, Project"` 决定正式入口。
- 改热更边界时：先改 `HybridCLRSettings.asset`，再确认 `KJHybridClrBuildTools.ValidateRuntimePreloadAssemblyName` 拦截名单（当前 `{Launcher, TestKit}`）；`Launcher` 不得新增任何 Framework/热更引用。
- C# 层改动不等同于必须换包；若旧 DLL 已加载，新 DLL 通常需重启/下次启动生效。

## 启动配置（BootStartupSettings）

序列化于 Entry prefab / scene，字段包括：

- `enableAssetUpdate` / `enableHotUpdate`：是否走资源/代码更新
- `skipHotUpdateInEditor`：Editor 下跳过 Assembly.Load（仍反射 `BootUpdateRunner.Start`）
- `streamingAssetsRoot`：本地兜底资源根目录（如 `HotUpdate`）
- `assetDownloadTag`：YooAsset 下载标签
- `startupTypeName` / `startupMethodName`：正式入口（默认 `Project.Bootstrap.ProjectStartup` / `Start`）
- `aotMetadataAssemblies` / `hotUpdateAssemblies`：AOT metadata 与热更 DLL 清单（**不要硬编码在 Boot 代码里**）

## 最佳实践

1. Entry 永远保持最小：只做 DontDestroyOnLoad 与启动 BootLoader；错误处理走 `BootStartupLog.Error` + `Debug.LogError`（不引用 Framework.Log）。
2. 所有热更 DLL / AOT metadata 清单来源于 `BootStartupSettings`，不在 Boot 代码里硬编码。
3. 新增 AOT 侧能力只能放在 `Launcher` 且只能引用 `UniTask / YooAsset / HybridCLR.Runtime / AssetShared`；其余逻辑放热更层。
4. 启动期日志：AOT 阶段用 `BootStartupLog`，热更层初始化后由 `BootUpdateRunner.ReplayEarlyLogs()` 回放至 RuntimeLog。
5. 资源运行时只初始化一次（Launcher 创建，Boot 通过 `WrapFromExistingPackage` 接管），不要创建第二套 YooAsset runtime。
