# CLAUDE.md

## 项目概述

KJ — Unity 2022.3.62f2 客户端游戏框架。VContainer + MessagePipe + YooAsset + HybridCLR + Luban。

完整技术栈见 `.planning/PROJECT.md`，目录结构规范见 `.planning/目录结构规范.md`。

## 会话启动

每次会话开始或接到不涉及具体文件的模糊任务时：
1. 读取 `.planning/STATE.md` → 了解当前进度和文件清单
2. 读取 `.planning/ROADMAP.md` → 了解模块状态和待实现列表
3. 结合两者确定当前上下文和合理的下一步建议

无明确指令时的默认行为：基于 ROADMAP.md 中待实现模块的依赖关系和复杂度，推荐最合理的下一步（通常是已满足依赖、复杂度低的模块）。

## 依赖方向（强制单向）

```
1External (非UPM第三方本地库，如 Odin)
    ↑
Packages (UPM 第三方)
    ↑
Framework (KJ 自有框架包，不含业务逻辑)
    ↑
Boot ──▶ Core ──▶ General ──▶ Project
```

- **1External**：非 UPM 管理的第三方本地库（预编译 DLL、Editor-only 插件如 Odin Inspector）。**只能引用 Unity 内置程序集和同目录第三方库**，禁止引用 Packages、Framework 及 Scripts 下任何代码。详见 `.claude/rules/1external.md`。
- **Packages**：UPM 管理的第三方库（VContainer、UniTask、MessagePipe、YooAsset 等）。可以互相引用。
- **Framework**：KJ 自有的独立包（`Asset/`、`Event/`、`Pool/`、`Cache/`、`BuildPipeline/`）。**最多只能依赖 Packages**，不能引用 Scripts 下任何代码。稳定底层模块直接放在 `Assets/Framework/`，不使用 `Assets/Framework/Package/`。`BuildPipeline/` 是纯数据契约层（`noEngineReferences=false`），不引用 `UnityEditor`/`Boot`/`Core` 等业务层。如果需要项目能力，通过接口、适配器或静态委托注入，由 Core 层桥接。
- **Boot**：启动更新壳（热更 `KJ.Boot.asmdef`），只做资源/代码更新与反射启动。引用 `Asset / Log / RuntimeLog / UniTask / AssetShared / YooAsset / Launcher`；不引用 VContainer、HybridCLR.Runtime、Core/General/Project。AOT 壳 `Launcher`（`KJ.Launcher.asmdef`，位于 `Boot/Launcher/`）只引用 `UniTask / YooAsset / HybridCLR.Runtime / AssetShared`。
- **Core**：引擎基础设施。可以引用 Boot、Framework、Packages。不引用 General/Project。
- **General**：通用业务。可以引用 Core、Packages。不引用 Project。
- **Project**：项目专属业务。可以引用所有下层。

编译边界由 `.asmdef` 强制检查。

## HybridCLR 热更边界（HYB-03 已落地）

启动链已裂变：AOT `Launcher` 壳 + 热更 `Boot` 更新流程。

- **AOT 壳 `Launcher`（`KJ.Launcher.asmdef`）**：只做"找到并加载热更代码"。引用仅 `UniTask / YooAsset / HybridCLR.Runtime / AssetShared`。**硬约束：不得引用任何 `Framework.*` 包或热更程序集**（靠 asmdef 强制）。通过反射字符串 `"Boot.BootUpdateRunner, Boot"` 调用热更入口，不编译期依赖 Boot。
- **热更 `Boot`（`KJ.Boot.asmdef`）**：启动更新编排（资源版本检查/下载/重试/更新 UI）。引用 `Asset / Log / RuntimeLog / UniTask / AssetShared / YooAsset / Launcher`。不再引用 `HybridCLR.Runtime`（AOT 壳代为加载）。
- **10 个热更程序集**：`Boot, Core, General, Project, Pool, Cache, Event, Asset, Log, RuntimeLog`（事实源 `ProjectSettings/HybridCLRSettings.asset` 的 `hotUpdateAssemblies`）。
- **`Framework.AssetShared`（AOT 共享）**：承载 `AssetConfig` / `AssetConstants`（namespace 保留 `Framework.Asset`），供 AOT 壳与热更层双向引用，解决 AssetConfig 跨边界共享。
- **AOT 阶段日志**：`BootStartupLog`（纯文本 + 内存），不依赖 `Framework.Log`/`RuntimeLog`；热更层初始化后由 `BootUpdateRunner.ReplayEarlyLogs()` 回放至 RuntimeLog session。
- **反射入口契约**：`BootLoader` 用字面串 `"Boot.BootUpdateRunner, Boot"` 反射解析；程序集名是启动契约的一部分，改名需同步 `BootLoader` 与 `HybridCLRSettings`。

改动热更边界时：先改 `HybridCLRSettings.asset`，再确认 `KJHybridClrBuildTools.ValidateRuntimePreloadAssemblyName` 拦截名单（当前 `{Launcher, TestKit}`）；`Launcher` 不得新增任何 Framework/热更引用。

## 命名空间

**命名空间 = 目录路径，不带项目名前缀。**

| 目录 | 命名空间 |
|------|---------|
| `Scripts/Boot/` | `Boot` |
| `Scripts/Core/` | `Core` |
| `Scripts/General/` | `General` |
| `Scripts/Project/` | `Project` |
| `Framework/` | `Framework.Asset`、`Framework.Event`、`Framework.Pool`、`Framework.Cache`、`Framework.BuildPipeline` 等 |

## 底层模块原则

- 资源、事件、池、缓存等稳定底层能力优先放入 `Assets/Framework/`。
- Core 只做启动编排、DI 注册、生命周期管理和 Framework 能力桥接。
- 上层不直接依赖 YooAsset 等资源库实现；通过 `Framework.Asset.IAssetSystem` 等统一接口访问。
- 事件统一使用 `Framework.Event.GameEventAttribute`，MessagePipe 只是当前后端。

❌ 永远不写 `namespace KJ.XXX`。

## 命名约定

- **Core 层**：`[CoreSystem]` + `ISystem` → 由 `SystemManager` 管理生命周期
- **General / Project 层**：`[Model]` + `IModel` → 由 `ModelLifecycle` 管理生命周期
- 业务层不使用 `System` 命名，不使用 `Module` 命名

## 新建模块时

加载 `module-scaffold` skill，或查阅 `.planning/目录结构规范.md`。
