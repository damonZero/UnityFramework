# Requirements: KJ Unity Framework

**Defined:** 2026-06-26
**Core Value:** 模块化、可复用的客户端框架，一个模块一个模块搭建并验证，确保每个模块独立可用、稳定可靠。

## v1 Requirements

### Foundation (Core Systems)

- [ ] **FOUND-01**: Boot Layer 引导层实现，包含 Entry 入口、启动流程状态机、.asmdef 分层（Boot/Core/General/Project）
- [ ] **FOUND-02**: IModule 接口定义，包含 Priority、Init()、Shutdown()，支持可选接口（ITickable 等）
- [ ] **FOUND-03**: ModuleManager 模块管理器，按优先级排序初始化、逆序关闭，驱动模块生命周期
- [ ] **FOUND-04**: EventManager 事件系统，基于枚举事件ID、优先级排序、Owner 管理（自动清理）、同步分发

### Resource (资源管理)

- [ ] **RES-01**: ResourceManager 资源管理器，支持异步加载、引用计数、缓存策略
- [ ] **RES-02**: AssetHandle<T> 句柄式 API，统一资源加载接口
- [ ] **RES-03**: ObjectPoolManager 对象池管理器，支持通用对象池 + GameObject 池、IPoolable 重置契约

### UI (界面框架)

- [ ] **UI-01**: UIManager 界面管理器，6 层排序系统（Background/Normal/Popup/Top/Loading/System）
- [ ] **UI-02**: UIWindow 基类，包含 OnInit/OnOpen/OnClose/OnPause/OnResume 生命周期
- [ ] **UI-03**: 窗口模式支持（Normal/Single/HideOthers/Overlay）
- [ ] **UI-04**: 异步预制体加载和窗口栈管理

### Network (网络通信)

- [ ] **NET-01**: NetManager 网络管理器，会话管理（创建/销毁/获取）
- [ ] **NET-02**: Session 会话类，状态机（Disconnected/Connecting/Connected/Authenticating/Reconnecting）
- [ ] **NET-03**: Protobuf 消息序列化/反序列化集成
- [ ] **NET-04**: MessageRouter 消息路由器，协议分发到处理器
- [ ] **NET-05**: 心跳机制和断线重连支持
- [ ] **NET-06**: Message Auto-Generation，.proto 文件自动生成处理器注册代码

### Config (配置表)

- [ ] **CFG-01**: ConfigManager 配置管理器，集成 Luban v4.10.1
- [ ] **CFG-02**: 二进制格式加载、懒加载策略
- [ ] **CFG-03**: 自动生成配置类、快速 ID 查找

### Timer (计时器)

- [ ] **TIMER-01**: TimerManager 计时器管理器，基于 Tick（非协程）
- [ ] **TIMER-02**: 支持一次性定时器和循环定时器
- [ ] **TIMER-03**: 暂停/恢复功能

### Hot Update (热更新)

- [ ] **HOT-01**: HybridCLR 集成，热更新加载流程
- [ ] **HOT-02**: AOT 元数据补充
- [ ] **HOT-03**: 版本检查和更新机制

### Localization (本地化)

- [ ] **L10N-01**: LocalizationManager 本地化管理器，键值查找
- [ ] **L10N-02**: 运行时语言切换
- [ ] **L10N-03**: Luban 配置表集成

## v2 Requirements

### Audio (音频)

- **AUDIO-01**: AudioManager 音频管理器，BGM/SFX/Voice 通道
- **AUDIO-02**: AudioSource 池化、音量控制
- **AUDIO-03**: 音效淡入淡出

### Game Systems (游戏系统)

- **RED-01**: RedDotManager 红点系统，树结构节点、事件驱动传播
- **RED-02**: 脏标记优化、批量更新
- **GUIDE-01**: GuideManager 引导系统，步骤状态机、配置驱动
- **GUIDE-02**: 事件触发、条件判断

### Animation (动画)

- **ANIM-01**: DOTween 集成，Tween 动画支持
- **ANIM-02**: 动画队列、回调支持

## Out of Scope

| Feature | Reason |
|---------|--------|
| ECS/DOTS | 复杂度高，不适合大多数游戏类型 |
| 内置服务器 | 框架层不含服务器实现，后期用 C# 独立实现 |
| 可视化脚本 | 非核心需求，增加框架复杂度 |
| 数据统计/崩溃上报 | 业务层需求，不属于框架层 |
| 自定义序列化 | Protobuf 已满足需求 |
| MVVM 绑定框架 | 过度设计，UGUI 不适合 MVVM |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FOUND-01 | Phase 1 | Pending |
| FOUND-02 | Phase 1 | Pending |
| FOUND-03 | Phase 1 | Pending |
| FOUND-04 | Phase 1 | Pending |
| RES-01 | Phase 1 | Pending |
| RES-02 | Phase 1 | Pending |
| RES-03 | Phase 2 | Pending |
| UI-01 | Phase 2 | Pending |
| UI-02 | Phase 2 | Pending |
| UI-03 | Phase 2 | Pending |
| UI-04 | Phase 2 | Pending |
| NET-01 | Phase 3 | Pending |
| NET-02 | Phase 3 | Pending |
| NET-03 | Phase 3 | Pending |
| NET-04 | Phase 3 | Pending |
| NET-05 | Phase 3 | Pending |
| NET-06 | Phase 3 | Pending |
| CFG-01 | Phase 3 | Pending |
| CFG-02 | Phase 3 | Pending |
| CFG-03 | Phase 3 | Pending |
| TIMER-01 | Phase 3 | Pending |
| TIMER-02 | Phase 3 | Pending |
| TIMER-03 | Phase 3 | Pending |
| HOT-01 | Phase 4 | Pending |
| HOT-02 | Phase 4 | Pending |
| HOT-03 | Phase 4 | Pending |
| L10N-01 | Phase 4 | Pending |
| L10N-02 | Phase 4 | Pending |
| L10N-03 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 25 total
- Mapped to phases: 25
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-26*
*Last updated: 2026-06-26 after initial definition*
