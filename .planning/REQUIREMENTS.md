# Requirements: KJ Unity Framework

**Created:** 2026-06-26
**Goal:** 商业级可用的 Unity 客户端框架，模块化、可复用、逐模块验证。

## 核心原则

- 一个模块一个模块地搭建，每个模块写完 → 编译通过 → 测试通过 → 再做下一个
- 不堆砌代码，不跳过验证
- 商业级质量：接口设计合理、边界情况处理、可扩展
- 依赖方向固定为 `Boot <- Core <- General <- Project`，下层不得依赖上层
- Boot 层保持最小依赖，只保留稳定启动壳、阶段协议和类型名驱动的 Stage 编排能力
- 启动采用 Boot 反射创建普通 C# Stage：Core → General → Project，按 Priority 执行
- 依赖注入采用 VContainer，MessagePipe 作为当前类型安全事件后端
- 稳定底层模块下沉到 `Assets/Framework/`，上层通过统一接口访问，不直接绑定第三方实现
- Core 使用 System，业务层使用 Model + ViewModel，不引入业务 System

## 需求清单

1. 建立基于 VContainer 的分阶段容器注册体系
2. 建立 Boot 层稳定启动协议：`BootstrapContext` / `IBootstrapStage`
3. 建立无 prefab 启动链：Boot 通过类型名创建 Stage，按 Priority 执行各层注册
4. 建立 Core Architecture：`ISystem`、`[CoreSystem]`、`SystemManager`
5. 建立类型安全事件注册：`Framework.Event.GameEventAttribute` + 当前 MessagePipe `IPublisher<T>` / `ISubscriber<T>` 后端
6. 删除旧式 `EventId + object payload` 事件总线设计
7. 建立业务 Model 注册：`IModel`、`[Model]`、`ModelLifecycle`
8. 确保热更新模块可独立替换，不因 Project/General 改动要求重启 App
9. 保持四层 .asmdef 编译隔离和单向依赖约束
10. 建立对象池/缓存体系：纯 C# 对象池（`Framework/Pool/`）、Unity 资源缓存（`Framework/Cache/`）、集合租借 `using` 统一入口（`Framework/Pool/Collections/`），由 Core 层 `PoolService` 桥接注册到 DI 容器
11. 建立底层资源统一接口：`Framework/Asset/` 封装资源配置、句柄、下载器和 YooAsset 适配，上层只依赖 `IAssetSystem`

## Framework 下沉硬约束

- `Assets/Framework/` 直接承载稳定底层模块，不创建 `Assets/Framework/Package/` 子目录。
- Framework 不引用 `Assets/Scripts/` 下任何汇编。
- Framework 可以封装第三方库，但对上层暴露 KJ 自己的稳定 API，例如 `IAssetSystem`、`AssetHandle<T>`、`AssetDownloadHandle`、`GameEventAttribute`。
- Core 负责项目编排：DI 注册、System 生命周期、ready 事件、Framework 委托注入。
- General/Project 优先依赖 Core/General 门面；确需直接使用底层能力时，只依赖 Framework 的稳定接口。
- 切换 YooAsset、MessagePipe 等第三方库时，优先只修改 Framework 适配代码和 Core 注册代码。

## 对象池 / 缓存硬约束

- 集合租借必须保留 `using` 使用方式，不能退化成手动 `Release()` 的唯一方式
- `List / HashSet / Queue / Stack / Dictionary` 等集合统一走租约封装
- 纯 C# 对象池（`Framework/Pool/`）、Unity 资源缓存（`Framework/Cache/`）、集合租借（`Framework/Pool/Collections/`）三者分层实现，由 Core 层 `PoolService` 桥接暴露统一门面
- 资源缓存必须支持预热、回收、淘汰、污染检测、统计
- Unity 对象池必须兼容 `GameObject` / `Component` / 单资源与多资源场景
- 缓存策略与容器解耦，默认支持 LRU，后续可扩展 FIFO / 自定义策略

---
*当前重点：Phase 1 — 验证启动链路、实现 UI 框架*
