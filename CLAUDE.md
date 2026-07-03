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
Packages (UPM 第三方)
    ↑
Framework (KJ 自有框架包，不含业务逻辑)
    ↑
Boot ──▶ Core ──▶ General ──▶ Project
```

- **Packages**：UPM 管理的第三方库（VContainer、UniTask、MessagePipe、YooAsset 等）。可以互相引用。
- **Framework**：KJ 自有的独立包（`Asset/`、`Event/`、`Pool/`、`Cache/`）。**最多只能依赖 Packages**，不能引用 Scripts 下任何代码。稳定底层模块直接放在 `Assets/Framework/`，不使用 `Assets/Framework/Package/`。如果需要项目能力，通过接口、适配器或静态委托注入，由 Core 层桥接。
- **Boot**：启动壳，最小依赖（仅 VContainer）。不引用 Core/General/Project，也不直接引用 Framework。
- **Core**：引擎基础设施。可以引用 Boot、Framework、Packages。不引用 General/Project。
- **General**：通用业务。可以引用 Core、Packages。不引用 Project。
- **Project**：项目专属业务。可以引用所有下层。

编译边界由 `.asmdef` 强制检查。

## 命名空间

**命名空间 = 目录路径，不带项目名前缀。**

| 目录 | 命名空间 |
|------|---------|
| `Scripts/Boot/` | `Boot` |
| `Scripts/Core/` | `Core` |
| `Scripts/General/` | `General` |
| `Scripts/Project/` | `Project` |
| `Framework/` | `Framework.Asset`、`Framework.Event`、`Framework.Pool`、`Framework.Cache` 等 |

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
