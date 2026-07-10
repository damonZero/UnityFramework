# Module Status Board

> **用途：** 记录框架需要哪些模块、每个模块当前处于什么状态、它依赖什么。
> **不做：** 固定执行顺序、工时估计、强制时间表。什么时候做什么模块取决于当时的需求和优先级判断。

**Last Updated:** 2026-07-09

---

## ✅ 已完成

| 模块 | 位置 | 说明 |
|------|------|------|
| Boot 启动协议 | `Boot/` | Entry + 序列化启动配置 + 最小启动更新 UI。Boot 只做资源/代码更新和反射启动，不引用 VContainer/Core/General/Project。 |
| VContainer DI | `Project/Bootstrap/` + `Core/Bootstrap/` + `Core/Systems/` | `ProjectStartup` 创建正式 VContainer root，`CoreStartupContext` 串联 Core → General → Project 注册 |
| Event 基础层 | `Framework/Event/` + `Core/Systems/` + `Core/Bootstrap/` | 统一 `[GameEvent]` 标记和类型扫描；MessagePipe 是当前 broker 注册后端 |
| ISystem + SystemManager | `Core/` | `ISystem` / `ITickableSystem` + `[CoreSystem]` 属性扫描 + `SystemManager` Priority 排序 → Init/Shutdown + VContainer Tick 驱动 |
| IModel + ModelLifecycle | `General/` | `IModel` / `[Model]` + `ModelLifecycle` Priority 排序 → Core 启动成功后 `IPostStartable.PostStart()` Load / Dispose Unload |
| Asset 基础层 | `Framework/Asset/` + `Core/Asset/` | `Framework.Asset` 提供统一资源 API、句柄和 YooAsset 适配；`Core.AssetSystem` 只负责生命周期编排和 ready 事件 |
| Log 基础层 | `Framework/Log/` + `Core/Logging/` | `GameLog` 稳定门面、环境/模块开关配置、编译期裁剪符号；Core 通过 ZLogger Unity provider 输出到 Console |
| LOG-AI-00 AI 运行日志规范 | `.planning/AI_RUNTIME_LOGGING.md` | 确立 JSONL + session 清单、Boot/Core 分层职责、AI 分析工作流和验收标准 |
| LOG-AI-01 运行日志落盘与会话清单 | `Framework/RuntimeLog/` + `Boot/` + `Core/Logging/` | 独立 RuntimeLog session writer；Boot 早期安装；Core 接入 GameLog/ILogger/ZLogger；输出 JSONL、session 清单、latest 指针 |
| LOG-AI-02 首版日志收集与 AI 分析入口 | `Assets/Scripts/Core.Editor/Logging/` | `KJ/Runtime Logs/*` 菜单：打开 latest、生成摘要、导出诊断包、清理本地日志 |
| TestKit 测试基础设施 | `Framework/TestKit/` | 基于 Unity Test Framework / NUnit，提供通用断言、Fake、Probe、Fixture 和手动时间驱动；具体测试用例放 `Assets/Tests/` |
| HYB-02A 热更同步工具 | `Assets/Scripts/Boot.Editor/HybridCLR/` + `Assets/GameRes/HotUpdate/` | 生成/同步 HybridCLR 热更 DLL 与 AOT metadata 为 YooAsset RawFile，并回写 Boot Entry 序列化配置；日常 smoke 走 `Prepare Runtime Assets And Boot`，正式构建前走完整 `Generate All And Sync` |
| HYB-03 热更边界裂变 | `Assets/Scripts/Boot/Launcher/` + `Assets/Framework/AssetShared/` | AOT `Launcher` 壳 + 热更 `Boot`；10 热更程序集；`AssetConfig`/`AssetConstants` 迁入 `Framework.AssetShared`；`BootRemoteService` 修复 IRemoteService 死锁；AOT 日志 `BootStartupLog`；反射入口 `"Boot.BootUpdateRunner, Boot"`；EditMode 测试 45/45 全绿含 15 例 HYB-03 边界 |
| Build Pipeline 构建打包管线 | `Assets/Scripts/Boot.Editor/Build/` + `Assets/Framework/BuildPipeline/` + `.planning/` | S0-S9 十阶段全量构建管线（PreFlight→GenerateAll→Compile→Sync→YooAsset→ApplyConfig→BuildPlayer→Validate→SmokeRun→Report）；`BuildConfig` + `BuildProfile` 双配置驱动；`KJBuildPipeline` 编排器支持续跑标记/差量检测/CI 无头；`BuildPipelineRunner` 新 Plan 驱动编排器 + 事务系统；`IBuildStage` 阶段插件化（P0-P9）；`StageDependencyTracker` 文件时间戳差量引擎；`SmokeLogParser` 多里程碑判定；`FormalLeakageVerifier` Formal/Audit 泄露检查；`BuildAnalyzer` + `BuildKnowledgeBase` 诊断系统；`BuildDashboardWindow` OdinMenuEditorWindow 六视图面板；`BuildCommandLine` CI 命令行入口；`BuildConfigTransaction` 事务化文件/设置修改与回滚 |
| Object Pool & Cache 重构 | `Framework/Pool/` + `Framework/Cache/` | `BoundedStore<TKey,TValue>` 替代旧 `Cache`（Put 覆盖两步 Remove+Add、Clear/Remove/淘汰统一 onEvicted、GetOrAdd single-flight、TTL 读路径清理）；`IStoreEvictionPolicy`/`IStoreExpirationPolicy` + `LruPolicy`/`TtlPolicy`/`CapacityPolicy`/`CompositePolicy`；`ObjectPool<T>` 保持 lock 并发安全，`CollectionPool` 使用 `SingleThreadObjectPool<T>` 主线程热路径；`GameObjectPool` 五字典合并 `PrefabPoolState`+实例库存策略 `IInstanceRecyclePolicy`+反向索引污染检测+[MainThread] 断言；`PoolService.cs` DI 桥接；相关 EditMode 单测全绿 |

---

## 🚧 当前验证 Gate

在继续 UI/Login/Config/Network 等新模块前，先确认已有底层框架在 Editor 和 Player 中稳定可用。

| 验证项 | 状态 | 范围 | 说明 |
|------|------|------|------|
| Editor Play 启动链 smoke | Done | Boot + YooAsset + HybridCLR + Core | 用户已确认无报错；`Editor.log` 已看到 `[AssetSystem] Ready` 与 `[SystemManager] 全部初始化完成` |
| Player 打包 smoke | Next | Boot + HybridCLR + YooAsset + Core | 构建并运行 Player，验证完整启动链、AOT metadata/DLL 加载、资源清单/下载流程、无启动期 Error/Exception |
| 资源加载矩阵 | Next | Framework.Asset + Core.AssetSystem | 验证 RawFile bytes、cached/owned 资源加载、实例化、场景加载/卸载、下载器、Release、UnloadUnused |
| PlayMode 覆盖 | Next | EditorSimulate / Offline / Host | 已通过 EditorSimulate Play；下一步至少覆盖 Player Offline，Host/CDN 后续用本地 HTTP 或测试服验证 |
| 热更新行为 smoke | Next | Core/General/Project DLL + 资源 | 修改 Project 层代码/资源后重新同步，验证无需整包；已加载 DLL 替换需重启/下次启动生效 |

---

## 🔲 待实现

### 基础设施

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| Timer | Low | `Core/Timer/` | ISystem | Tick-based（非协程），一次性 + 循环，暂停/恢复，最小 GC |
| Object Pool | Low-Medium | `Core/Pool/` | Framework.Asset | ✅ 代码已重构完成（见上方"已完成"）；`PoolService.cs` DI 桥接；`BoundedStore` 替代旧 `Cache`；Pool/Cache 相关 EditMode 单测全绿 |
| PERF-01 已实现模块性能治理 | Low-Medium | `Core/Systems/`, `Core/Bootstrap/`, `General/Bootstrap/`, `Boot/` | ZLogger, ZLinq, Pool/Cache | 接入 ZLogger + VContainer 日志注册；将 SystemManager/ModelLifecycle 生命周期日志迁移为 `[ZLoggerMessage]`；启动期反射扫描和注册链路去普通 LINQ/临时数组；补 Unity Editor 编译/Test Runner 验证 |
| LOG-TOOLS 日志工具面板/打包接入 | Medium | `Assets/Framework/Log.Editor/` + build pipeline | Framework.Log | 参考旧 DebugSwitches，实现模块树 Editor 面板、保存/加载 GameLogConfig、打包时注入 `KJ_LOG_*` 符号和模块规则；跨层入口才放 `Assets/Editor/` |
| CI 打包脚本与产物管理 | Low-Medium | 待定（可能 `ci/` 或 `Assets/Editor/`） | Build Pipeline | 代码已有 `BuildFromCommandLine()`（Boot.Editor.Build.KJBuildPipeline），待规划：① 封装为 `ci/build.ps1` 一键脚本（自动定位 Unity 路径、传参、捕获退出码）；② 产物输出路径规范（APK/IPA/Standalone 放到哪里）；③ 版本号/环境自动注入策略；④ 与外部 CI（Jenkins/蓝盾）对接方式 |

### 配置与数据

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| ConfigManager (Luban) | Medium | `General/Config/` | Framework.Asset | Luban v4.10.1 集成，二进制格式，懒加载策略，快速 ID 查找 |
| Login | Medium | `General/Login/` | UIManager, NetManager, ConfigManager | 登录/公告/服务器列表/账号状态属于业务层；Boot 只负责更新界面和修复入口 |

### 资源分包与本地化（后期规划）

> 这组任务不阻塞当前 Player smoke / 资源加载矩阵 / 热更新 smoke。它们属于商业化资源运营能力，应该在 Build Pipeline v2、AssetSystem 验证稳定、Localization 基础能力确定后进入实现。

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| RES-PACK-01 YooAsset 包裹布局策略 | Medium | `Assets/Framework/Asset/` + `Assets/Scripts/Boot.Editor/Build/` | Framework.Asset, Build Pipeline v2 | 明确 `DefaultPackage` / `CorePackage` / `LanguagePackage.{locale}` / `LevelPackage.{id}` 等包裹命名、用途、生命周期和下载策略；BuildProfile 必须可声明 package layout、collector group、tags、版本和 CDN 根路径 |
| RES-PACK-02 首包瘦身 CorePackage | Medium-High | `Assets/GameRes/` + Build Pipeline v2 | RES-PACK-01, Boot, Framework.Asset | 将启动链、登录前、首 5 分钟新手教程必需资源收敛进 CorePackage；非首屏 UI、后续关卡、高清语音、可延迟资源进入独立包裹；构建报告需要输出首包体积、CorePackage 内容清单和超预算告警 |
| RES-PACK-03 边玩边下后台下载 | High | `Framework.Asset` + `Core`/`General` 下载编排 | RES-PACK-01, RES-PACK-02, RuntimeLog | 基于 YooAsset downloader 封装 package/tag/group 下载任务；支持优先级、并发、暂停/恢复、失败重试、磁盘空间预检、进度事件、AI-readable RuntimeLog；业务侧只感知下载意图，不直接触碰 YooAsset 类型 |
| L10N-PACK-01 多语言资源包 | Medium-High | `General/L10N/` + `Assets/GameRes/Localization/` | Localization, ConfigManager, RES-PACK-01 | 文本表、字体、语音、区域化图片按 locale 物理隔离为独立 LanguagePackage；首次进入根据系统语言/账号设置只下载目标语言；切换语言时可增量下载新语言包并可清理旧语言包，实现“零污染”本地化资源管理 |
| RES-PACK-04 分包依赖图与 AI 诊断 | Medium | `Assets/Framework/BuildPipeline/` + `Boot.Editor/Build/` | Build Pipeline v2, RuntimeLog | 生成 package manifest、资源归属表、依赖图、下载组报告和缺失资源诊断；构建失败或运行期加载失败时，AI 可以从报告定位是 collector、package、tag、CDN、版本还是 AssetConfig 配置问题 |

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
| Localization | Medium | `General/L10N/` | ConfigManager, Framework.Asset | 键值查找，运行时切换，Luban 配置表集成；后续与 L10N-PACK-01 多语言资源包打通 |

### 热更新

| 模块 | 复杂度 | 位置 | 依赖 | 说明 |
|------|--------|------|------|------|
| HYB-00 热更边界固化 | Medium | `.planning/` + asmdef 策略 | Boot, Framework | 明确托管 DLL 下发、重启生效、真正换包三类边界，作为 UI/Config/Net 前置约束 |
| HYB-01 HybridCLR 最小加载闭环 | High | `Boot/` | 稳定 Framework 资源接口 | Boot 侧热更配置、AOT 元数据补充、Core/General/Project DLL 加载、再反射创建热更 Stage |
| HYB-02B 热更 smoke test | Medium | Unity Editor + Player | HYB-02A, YooAsset | 先完成当前验证 gate：Player 打包 smoke、资源加载矩阵、PlayMode 覆盖、“改 Project 代码后无整包更新”验证；正式包前再跑完整 `Generate All And Sync` |
| ~~HYB-03 Boot.Update 拆分~~ | ✅ 已完成（见上方"已完成"） | `Scripts/Boot/Launcher/` + `Framework/AssetShared/` | HYB-01, HYB-02 | 落地为 AOT `Launcher` + 热更 `Boot`；Boot 变更可下载但已加载 DLL 替换需重启/下次启动生效 |

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
PERF-01 ← Framework.Log + ZLogger + ZLinq + Pool/Cache
LOG-AI-01 ← Framework.Log + Framework.RuntimeLog + ZLogger + Boot/Core.Logging
LOG-AI-02 ← LOG-AI-01 + Core.Editor
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
RES-PACK-01 ← Framework.Asset + Build Pipeline v2
RES-PACK-02 ← RES-PACK-01 + Boot + Framework.Asset
RES-PACK-03 ← RES-PACK-01 + RES-PACK-02 + RuntimeLog
L10N-PACK-01 ← Localization + ConfigManager + RES-PACK-01
RES-PACK-04 ← Build Pipeline v2 + RuntimeLog
HybridCLR ← Framework.Asset + Boot
UIManager ← HybridCLR boundary + ISystem + Framework.Asset
ConfigManager (Luban) ← HybridCLR boundary + Framework.Asset
```

---

*Boards updated: 2026-07-09*
