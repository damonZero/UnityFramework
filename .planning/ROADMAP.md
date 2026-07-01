# Module Status Board

> **用途：** 记录框架需要哪些模块、每个模块当前处于什么状态、它依赖什么。
> **不做：** 固定执行顺序、工时估计、强制时间表。什么时候做什么模块取决于当时的需求和优先级判断。

**Last Updated:** 2026-06-30

---

## ✅ 已完成

| 模块 | 位置 | 说明 |
|------|------|------|
| Boot 启动协议 | `Boot/` | Entry + AppLifetimeScope + BootstrapContext + IBootstrapStage + BootLifetimeScope + prefab 字符串链式启动。Boot 层最小依赖。 |
| VContainer DI | `Core/Architecture/` | 分层容器注册，`IContainerBuilder` 扩展方法，`BootstrapContext` 传递上下文 |
| MessagePipe 事件 | `Core/Architecture/` | `[GameEvent]` + `IPublisher<T>` / `ISubscriber<T>`，编译期类型安全，反射扫描注册，`AsImplementedInterfaces()` 自动绑定 |
| ISystem + SystemManager | `Core/` | `ISystem` / `ITickableSystem` + `[CoreSystem]` 属性扫描 + `SystemManager` Priority 排序 → Init/Shutdown + VContainer Tick 驱动 |
| IModel + ModelLifecycle | `General/` | `IModel` / `[Model]` + `ModelLifecycle` Priority 排序 → Load/Unload |
| Asset 系统 | `Core/Asset/` | 基于 YooAsset 3.0 封装，`IAssetSystem` 对外接口，owned/cached 双通道，`AssetCacheKey` 类型感知缓存，`SemaphoreSlim` 并发保护，`AssetInstanceHandle` / `AssetSceneHandle` 联合生命周期，下载器暴露给更新管线 |

---

## 🔲 待实现

### 基础设施

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| Timer | Low | `Core/Timer/` | ISystem | Tick-based（非协程），一次性 + 循环，暂停/恢复，最小 GC |
| Object Pool | Low-Medium | `Core/Pool/` | AssetSystem | Framework/Pool + Framework/Cache 代码已完成，缺 `PoolService.cs` DI 桥接注册 |

### 配置与数据

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| ConfigManager (Luban) | Medium | `General/Config/` | AssetSystem | Luban v4.10.1 集成，二进制格式，懒加载策略，快速 ID 查找 |

### UI

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| UIManager | Medium-High | `Core/UI/` | ISystem, AssetSystem | 6 层排序（Background/Normal/Popup/Top/Loading/System），窗口注册/打开/关闭 |
| UIWindow | Low | `Core/UI/` | UIManager | 基类，OnInit/OnOpen/OnClose/OnPause/OnResume 生命周期 |
| 窗口模式 | Low | `Core/UI/` | UI-01, UI-02 | Normal/Single/HideOthers/Overlay 四种打开模式 + 窗口栈导航 |

### 网络

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| NetManager | High | `Core/Network/` | ISystem, MessagePipe | 会话管理：CreateSession / CloseSession / GetSession |
| Session | High | `Core/Network/` | NetManager | 状态机：Disconnected→Connecting→Connected→Authenticating→Reconnecting；心跳 + 断线重连（指数退避） |
| Protobuf 序列化 | Medium | `Core/Network/` | — | `IMessage` 接口，`ProtoMessage<T>` 包装，Google.Protobuf 3.35.1 |
| MessageRouter | Medium | `Core/Network/` | MessagePipe, Protobuf | MsgId→Handler 映射分发，RegisterHandler / Route |
| Proto 自动生成 | Medium | `Core/Network/` | NetManager, Protobuf | `.proto` → C# + Handler 注册代码自动生成 |

### 游戏系统

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| AudioManager | Low-Medium | `General/Audio/` | AssetSystem, ObjectPool | BGM/SFX/Voice 通道，音量控制，AudioSource 池化 |
| RedDot | Medium | `General/RedDot/` | MessagePipe | 树形节点，事件驱动传播，脏标记优化 |
| Guide | High | `General/Guide/` | MessagePipe, UIManager, ConfigManager | 步骤式状态机，配置驱动，事件触发过渡 |
| Localization | Medium | `General/L10N/` | ConfigManager, AssetSystem | 键值查找，运行时切换，Luban 配置表集成 |

### 热更新

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| HybridCLR 集成 | High | `Boot/` | AssetSystem | 热更 DLL 加载，AOT 元数据补充，版本检查 + 更新管线 |

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
AssetSystem ← ISystem
Timer ← ISystem
ObjectPool ← AssetSystem
UIManager ← ISystem + AssetSystem
UIWindow ← UIManager
ConfigManager (Luban) ← AssetSystem
AudioManager ← AssetSystem + ObjectPool
NetManager ← ISystem + MessagePipe
Session ← NetManager
MessageRouter ← MessagePipe + Protobuf
RedDot ← MessagePipe
Guide ← MessagePipe + UIManager + ConfigManager
Localization ← ConfigManager + AssetSystem
HybridCLR ← AssetSystem + Boot
```

---

*Boards updated: 2026-06-30*
