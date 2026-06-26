# Roadmap: KJ Unity Framework

**Created:** 2026-06-26
**Mode:** Vertical MVP — 每阶段产出可运行、可验证的里程碑
**Total v1 Requirements:** 25

---

## Phase 1: 空壳能跑（Boot + Foundation + Resource + UI 最小集）

> **里程碑：** 一个能启动、加载资源、显示 UI 窗口的空项目
> **验证方式：** Unity Editor 中运行 → 看到 ModuleManager 初始化日志 → 加载一个 Prefab → 弹出一个 UI 窗口

| ID | Requirement | Module | Description | Est. |
|----|-------------|--------|-------------|------|
| 1.1 | FOUND-01 | Boot | Boot Layer 引导层：Entry 入口、启动流程状态机、.asmdef 四层分离（Boot/Core/General/Project） | 2h |
| 1.2 | FOUND-02 | Core | IModule 接口定义：Priority、Init()、Shutdown()、可选 ITickable | 1h |
| 1.3 | FOUND-03 | Core | ModuleManager：按优先级排序初始化、逆序关闭、MonoBehaviour 驱动 Update/LateUpdate/FixedUpdate | 2h |
| 1.4 | FOUND-04 | Core | EventManager：枚举事件 ID、优先级排序、Owner 管理（自动清理）、同步分发 Fire/FireUntil | 2h |
| 1.5 | RES-01 | Core | ResourceManager：异步加载、引用计数、强弱引用缓存策略 | 3h |
| 1.6 | RES-02 | Core | AssetHandle\<T\> 句柄式 API：IsDone/Progress/Dispose 统一接口 | 1h |
| 1.7 | UI-01 | Core | UIManager：6 层排序（Background/Normal/Popup/Top/Loading/System）、窗口注册/打开/关闭 | 3h |
| 1.8 | UI-02 | Core | UIWindow 基类：OnInit/OnOpen/OnClose/OnPause/OnResume 生命周期 | 1h |

**Phase 1 总计:** 8 个需求，约 15h
**Phase 1 交付物:**
- `Assets/Scripts/Boot/` — Entry.cs、GameLaunch.cs
- `Assets/Scripts/Core/` — ModuleManager、EventManager、ResourceManager、UIManager、UIWindow
- 4 个 .asmdef 文件
- 一个测试场景 + 一个测试 UI 窗口 Prefab
- 编辑器运行日志验证模块初始化顺序

---

## Phase 2: 能通信（+ Network + ObjectPool + UI 完善 + Config）

> **里程碑：** 模拟登录流程：启动 → 加载配置 → 连接服务器（本地 mock）→ 登录 → 进入主界面
> **验证方式：** 运行项目 → 自动连 MockServer → 发送 LoginReq → 收到 LoginResp → UIManager 打开 MainWindow

| ID | Requirement | Module | Description | Est. |
|----|-------------|--------|-------------|------|
| 2.1 | NET-01 | Core | NetManager：会话管理（CreateSession/CloseSession/GetSession） | 2h |
| 2.2 | NET-02 | Core | Session 类：状态机（Disconnected→Connecting→Connected→Authenticating→Reconnecting） | 3h |
| 2.3 | NET-03 | Core | Protobuf 消息序列化/反序列化：IMessage 接口、ProtoMessage\<T\> 包装 | 2h |
| 2.4 | NET-04 | Core | MessageRouter：MsgId→Handler 映射分发、RegisterHandler/Route | 2h |
| 2.5 | NET-05 | Core | 心跳机制（可配置间隔）+ 断线重连（指数退避） | 2h |
| 2.6 | RES-03 | Core | ObjectPoolManager：通用对象池 + GameObject 池、IPoolable 重置契约 | 2h |
| 2.7 | UI-03 | Core | 窗口模式：Normal/Single/HideOthers/Overlay 四种打开模式 | 1h |
| 2.8 | UI-04 | Core | 异步预制体加载 + 窗口栈管理（返回逻辑） | 2h |
| 2.9 | CFG-01 | General | ConfigManager：集成 Luban v4.10.1，二进制格式加载 | 3h |
| 2.10 | CFG-02 | General | 懒加载策略 + 自动生成配置类 + 快速 ID 查找 | 2h |

**Phase 2 总计:** 10 个需求，约 21h
**Phase 2 交付物:**
- `Assets/Scripts/Core/Network/` — NetManager、Session、MessageRouter、ProtoMessage
- `Assets/Scripts/Core/Pool/` — ObjectPoolManager、ObjectPool\<T\>
- `Assets/Scripts/General/Config/` — ConfigManager、Luban 集成
- 一个 MockServer 测试工具
- 登录流程端到端验证

---

## Phase 3: 补齐收尾（+ Timer + Hot Update + Localization + NET-06）

> **里程碑：** 完整框架可用：热更新加载、多语言切换、定时器驱动、proto 自动生成
> **验证方式：** HybridCLR 热更新一个测试 DLL → 运行时切换语言 → Timer 驱动倒计时 → .proto 自动生成代码

| ID | Requirement | Module | Description | Est. |
|----|-------------|--------|-------------|------|
| 3.1 | TIMER-01 | Core | TimerManager：基于 Tick（非协程）、驱动 Update 中执行 | 2h |
| 3.2 | TIMER-02 | Core | 一次性定时器 + 循环定时器 | 1h |
| 3.3 | TIMER-03 | Core | 暂停/恢复功能 | 1h |
| 3.4 | NET-06 | Core | Message Auto-Generation：.proto 文件自动生成处理器注册代码 | 3h |
| 3.5 | HOT-01 | Boot | HybridCLR 集成：热更新加载流程、热更 DLL 加载入口 | 4h |
| 3.6 | HOT-02 | Boot | AOT 元数据补充 | 2h |
| 3.7 | HOT-03 | Boot | 版本检查和更新机制 | 2h |
| 3.8 | L10N-01 | General | LocalizationManager：键值查找、多语言表结构 | 2h |
| 3.9 | L10N-02 | General | 运行时语言切换 + 事件通知 | 1h |
| 3.10 | L10N-03 | General | Luban 配置表集成（本地化文本走 Luban） | 1h |

**Phase 3 总计:** 10 个需求，约 19h
**Phase 3 交付物:**
- `Assets/Scripts/Core/Timer/` — TimerManager
- `Assets/Scripts/Boot/HotUpdate/` — HybridCLR 入口、版本检查
- `Assets/Scripts/General/Localization/` — LocalizationManager
- proto 自动生成工具链
- 完整框架 .asmdef 依赖图验证

---

## 总览

| Phase | 需求数 | 预估工时 | 里程碑 | 累计完成 |
|-------|--------|----------|--------|----------|
| Phase 1 | 8 | ~15h | 空壳能跑 | 8/25 (32%) |
| Phase 2 | 10 | ~21h | 能通信 | 18/25 (72%) |
| Phase 3 | 10 | ~19h | 补齐收尾 | 25/25 (100%) |
| **Total** | **25** | **~55h** | **v1 完成** | **100%** |

---

## 依赖关系

```
Phase 1 内部依赖:
  FOUND-01 (Boot) ← 无依赖，最先做
  FOUND-02 (IModule) ← 无依赖
  FOUND-03 (ModuleManager) ← 需要 FOUND-02
  FOUND-04 (EventManager) ← 需要 FOUND-02
  RES-01 (ResourceManager) ← 需要 FOUND-02
  RES-02 (AssetHandle) ← 需要 RES-01
  UI-01 (UIManager) ← 需要 FOUND-02, RES-01
  UI-02 (UIWindow) ← 需要 UI-01

Phase 2 依赖 Phase 1:
  NET-01~05 ← 需要 FOUND-02, FOUND-04 (EventManager)
  RES-03 (ObjectPool) ← 需要 RES-01
  UI-03~04 ← 需要 UI-01, UI-02, RES-01
  CFG-01~02 ← 需要 RES-01 (ResourceManager)

Phase 3 依赖 Phase 1+2:
  TIMER-01~03 ← 需要 FOUND-02
  NET-06 ← 需要 NET-01~04
  HOT-01~03 ← 需要 FOUND-01
  L10N-01~03 ← 需要 RES-01, CFG-01
```

---

## 推荐执行顺序（Phase 1 内部）

```
1. FOUND-01 (Boot Layer)           — 项目骨架，无依赖
2. FOUND-02 (IModule)              — 接口定义，无依赖
3. FOUND-03 (ModuleManager)        — 依赖 IModule
4. FOUND-04 (EventManager)         — 依赖 IModule
5. RES-01 (ResourceManager)        — 依赖 IModule
6. RES-02 (AssetHandle)            — 依赖 ResourceManager
7. UI-01 (UIManager)               — 依赖 IModule + ResourceManager
8. UI-02 (UIWindow)                — 依赖 UIManager
```

---

## 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| Addressables vs AssetBundle 选型未定 | RES-01 设计可能返工 | Phase 1 先用 Resources.Load + 接口抽象，Phase 2 再决定 |
| Luban 集成复杂度 | CFG 可能超时 | 先写死配置加载，Luban 集成作为独立任务 |
| HybridCLR 环境搭建 | HOT 可能阻塞 | Phase 3 最后做，前期用普通 DLL 加载模拟 |
| 没有真实服务器 | NET 无法端到端测试 | Phase 2 用 MockServer + 本地回环 |

---

## Definition of Done (v1)

- [ ] 所有 25 个 v1 需求标记为 ✅
- [ ] 四层 .asmdef 编译隔离，无循环依赖
- [ ] Phase 1 测试场景：启动 → 模块初始化日志 → 加载 Prefab → 显示 UI 窗口
- [ ] Phase 2 测试场景：MockServer 登录流程端到端
- [ ] Phase 3 验证：HybridCLR 热更新 + 语言切换 + Timer + proto 自动生成
- [ ] 代码注释覆盖率 > 80%（公共 API）
- [ ] 每个 Manager 有对应的单元测试或集成测试场景

---

*Roadmap created: 2026-06-26*
*Mode: Vertical MVP*
