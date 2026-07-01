# KJ Unity Framework

## 技术栈

- 引擎: Unity 2022.3.62f2 LTS
- 依赖注入: VContainer
- 热更新: HybridCLR
- 资源管理: YooAsset 3.0
- 配置表: Luban v4.10.1
- 网络通信: Google.Protobuf 3.35.1
- 异步: UniTask v2.5.11
- 事件: MessagePipe
- UI: UGUI (内置)

## 架构设计

四层分层架构（严格单向依赖）：

```text
Boot <- Core <- General <- Project
```

Boot 层必须保持最小依赖，只承担稳定启动壳、阶段协议和 prefab 链式启动能力。Boot 不引用 Core/General/Project，不集中持有完整阶段列表。

启动流程采用当前阶段启动下一阶段：

```text
BootLifetimeScope
  -> nextBootstrapPrefabPath
  -> CoreBootstrapStage
  -> GeneralBootstrapStage
  -> ProjectBootstrapStage
```

依赖注入主体通过 VContainer 在 Core / General / Project 各阶段逐步注册。阶段之间通过 Boot 层稳定协议 `BootstrapContext` / `IBootstrapStage` 传递上下文。

每层对应一个 .asmdef 文件，实现编译隔离。

对象池/缓存体系采用”三段式”边界：
- `Framework/Pool/` 负责纯 C# 对象池（`ObjectPool<T>`）、集合租约（`PooledList<T>` 等）、类型池注册表、GameObject 池（`GameObjectPool`）
- `Framework/Cache/` 负责缓存策略（LRU/FIFO）、资源容器
- `Core/Pool/` 负责桥接：注入 Framework 依赖委托、注册到 VContainer、暴露统一门面 `PoolService`

集合租借必须保留 `using` 用法，不能只提供手动归还 API。

## 命名规范

- **Core 层**：`System`（如 ResourceSystem、UISystem）
  - 接口：`ISystem` / `ITickableSystem` / `IAsyncSystem`
  - 标记：`[CoreSystem]`
  - 管理器：`SystemManager`
- **业务层（General/Project）**：`Model`（MVVM 规范，如 TaskModel、ShopModel）
  - 标记：`[Model]`
  - 业务功能按需实现接口来区分职责（如 ILoginHandler 处理登录数据）
  - 不使用 Module / System 作为业务功能命名

## 事件规范

- 事件使用 MessagePipe 强类型事件。
- Core 和业务层分别使用各自的 `[GameEvent]` 标记扫描注册。
- 业务代码直接依赖 `IPublisher<TEvent>` / `ISubscriber<TEvent>`。
- 不再使用 `EventId + object payload` 的统一事件总线。
