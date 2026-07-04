# Module Status Board

> **用途：** 记录框架需要哪些模块、每个模块当前处于什么状态、它依赖什么。
> **不做：** 固定执行顺序、工时估计、强制时间表。什么时候做什么模块取决于当时的需求和优先级判断。

**Last Updated:** 2026-07-04

---

## ✅ 已完成

| 模块 | 位置 | 说明 |
|------|------|------|
| Boot 启动协议 | `Boot/` | Entry + AppLifetimeScope + BootstrapContext + IBootstrapStage + BootLifetimeScope。已迁移为无 prefab 的普通 C# Stage 编排；Boot 通过类型名反射创建阶段，仍不引用 Core/General/Project。 |
| VContainer DI | `Core/Bootstrap/` + `Core/Systems/` | 分层容器注册，`IContainerBuilder` 扩展方法，`BootstrapContext` 传递上下文 |
| Event 基础层 | `Framework/Event/` + `Core/Systems/` + `Core/Bootstrap/` | 统一 `[GameEvent]` 标记和类型扫描；MessagePipe 是当前 broker 注册后端 |
| ISystem + SystemManager | `Core/` | `ISystem` / `ITickableSystem` + `[CoreSystem]` 属性扫描 + `SystemManager` Priority 排序 → Init/Shutdown + VContainer Tick 驱动 |
| IModel + ModelLifecycle | `General/` | `IModel` / `[Model]` + `ModelLifecycle` Priority 排序 → Core 启动成功后 `IPostStartable.PostStart()` Load / Dispose Unload |
| Asset 基础层 | `Framework/Asset/` + `Core/Asset/` | `Framework.Asset` 提供统一资源 API、句柄和 YooAsset 适配；`Core.AssetSystem` 只负责生命周期编排和 ready 事件 |
| TestKit 测试基础设施 | `Framework/TestKit/` | 基于 Unity Test Framework / NUnit，提供通用断言、Fake、Probe、Fixture 和手动时间驱动；具体测试用例放 `Assets/Tests/` |

---

## 🔲 待实现

### 基础设施

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| Timer | Low | `Core/Timer/` | ISystem | Tick-based（非协程），一次性 + 循环，暂停/恢复，最小 GC |
| Object Pool | Low-Medium | `Core/Pool/` | Framework.Asset | Framework/Pool + Framework/Cache 代码已完成，`PoolService.cs` 负责 DI 桥接注册 |
| PERF-01 已实现模块性能治理 | Low-Medium | `Core/Systems/`, `Core/Bootstrap/`, `General/Bootstrap/`, `Boot/Bootstrap/` | ZLogger, ZLinq, Pool/Cache | 接入 ZLogger + VContainer 日志注册；将 SystemManager/ModelLifecycle 生命周期日志迁移为 `[ZLoggerMessage]`；启动期反射扫描和 Bootstrap stage 收集去普通 LINQ/临时数组；补 Unity Editor 编译/Test Runner 验证 |

### 配置与数据

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| ConfigManager (Luban) | Medium | `General/Config/` | Framework.Asset | Luban v4.10.1 集成，二进制格式，懒加载策略，快速 ID 查找 |

### UI

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| UIManager | Medium-High | `Core/UI/` | ISystem, Framework.Asset | 6 层排序（Background/Normal/Popup/Top/Loading/System），窗口注册/打开/关闭 |
| UIWindow | Low | `Core/UI/` | UIManager | 基类，OnInit/OnOpen/OnClose/OnPause/OnResume 生命周期 |
| 窗口模式 | Low | `Core/UI/` | UI-01, UI-02 | Normal/Single/HideOthers/Overlay 四种打开模式 + 窗口栈导航 |

### 网络

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| NetManager | High | `Core/Network/` | ISystem, MessagePipe backend | 会话管理：CreateSession / CloseSession / GetSession |
| Session | High | `Core/Network/` | NetManager | 状态机：Disconnected→Connecting→Connected→Authenticating→Reconnecting；心跳 + 断线重连（指数退避） |
| Protobuf 序列化 | Medium | `Core/Network/` | — | `IMessage` 接口，`ProtoMessage<T>` 包装，Google.Protobuf 3.35.1 |
| MessageRouter | Medium | `Core/Network/` | MessagePipe, Protobuf | MsgId→Handler 映射分发，RegisterHandler / Route |
| Proto 自动生成 | Medium | `Core/Network/` | NetManager, Protobuf | `.proto` → C# + Handler 注册代码自动生成 |

### 游戏系统

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| AudioManager | Low-Medium | `General/Audio/` | Framework.Asset, ObjectPool | BGM/SFX/Voice 通道，音量控制，AudioSource 池化 |
| RedDot | Medium | `General/RedDot/` | Event backend | 树形节点，事件驱动传播，脏标记优化 |
| Guide | High | `General/Guide/` | Event backend, UIManager, ConfigManager | 步骤式状态机，配置驱动，事件触发过渡 |
| Localization | Medium | `General/L10N/` | ConfigManager, Framework.Asset | 键值查找，运行时切换，Luban 配置表集成 |

### 热更新

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| HybridCLR 集成 | High | `Boot/` | 稳定 Framework 资源接口 | 热更 DLL 加载，AOT 元数据补充，版本检查 + 更新管线 |

---

## 🚫 明确不做

| 项目 | 原因 |
|------|------|
| ECS / DOTS 架构 | 复杂度远超收益，仅特殊场景需要时局部使用 |
| 内置 Server 实现 | 客户端框架，Server 独立项目 |
| 可视化脚本编辑器 | 独立产品级工程，集成第三方即可 |
| 内置 Analytics / Crash | 耦合特定服务，提供扩展点即可 |
| 自定义序列化格式 | Protobuf + Luban + JSON 已覆盖所有需求 |
| 完整 MVVM 绑定框架 | 游戏 UI 是事件驱动刷新，不需要持续数据绑定 |
| 事件总线字符串 ID | 已用 MessagePipe 强类型替代 |

---

## 依赖关系速查

```
Framework.Asset ← YooAsset adapter
Core.AssetSystem ← ISystem + Framework.Asset
Timer ← ISystem
ObjectPool ← Framework.Asset
PERF-01 ← ZLogger + ZLinq + Pool/Cache
UIManager ← ISystem + Framework.Asset
UIWindow ← UIManager
ConfigManager (Luban) ← Framework.Asset
AudioManager ← Framework.Asset + ObjectPool
NetManager ← ISystem + Event backend
Session ← NetManager
MessageRouter ← Event backend + Protobuf
RedDot ← Event backend
Guide ← Event backend + UIManager + ConfigManager
Localization ← ConfigManager + Framework.Asset
HybridCLR ← Framework.Asset + Boot
```

---

*Boards updated: 2026-07-04*
