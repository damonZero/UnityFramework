---
gsd_state_version: 1.0
milestone: v4.10.1
milestone_name: milestone
status: unknown
last_updated: "2026-07-08T14:00:00.000Z"
---

# Project State: KJ Unity Framework

**Last Updated:** 2026-07-19
**Current Status:** ✅ Phase 0 完成，🔄 Phase 1 稳定性验证中（HybridCLR 边界【含 HYB-03 裂变】、最小加载闭环、编辑器同步工具、AI_RUNTIME_LOGGING 已落地；构建管线已收敛为 BuildProfile-only + P0-P9 + fingerprint + BuildTransaction，并新增 AOP-PERF-01 显式打包耗时监控；Unity 编译通过；AOP/Build Pipeline 定向 EditMode 测试 14/14 全绿；Android P0-P9 APK 构建已于 2026-07-19 真实通过，下一步是设备 Runtime smoke、Standalone E2E 与资源加载矩阵）

## 进度

- [x] 需求讨论完成
- [x] 命名规范确定（Core=System，业务=Model）
- [x] FOUND-01: Boot Layer (Entry + serialized startup settings + minimal update UI hook)
- [x] FOUND-02: ISystem 接口 (ISystem + ITickableSystem)
- [x] FOUND-03: SystemManager
- [x] DI-01: VContainer 接入方案（Core 注册入口 + MessagePipe）
- [x] DI-02: Boot 层最小依赖约束落地（Boot 不引用 Core/General/Project）
- [x] DI-03: Core System 迁移到容器驱动注册（`[CoreSystem]` + 反射扫描 `AsSelf().AsImplementedInterfaces()`）
- [x] EVT-01: 删除旧 `EventId + object payload` 事件总线，统一 `Framework.Event.GameEventAttribute`
- [x] BOOT-CHAIN-01: prefab 字符串链式启动协议（历史方案；当前判断 prefab 仅作为 MonoBehaviour/SerializeField 载体，不再作为后续目标）
- [x] RES-01: `Framework.Asset` — 基于 YooAsset 3.0 的底层资源适配（owned/cached 双通道、SemaphoreSlim 并发保护、AssetCacheKey 类型感知缓存）
- [x] RES-02: AssetHandle<T> / AssetInstanceHandle / AssetSceneHandle / AssetDownloadHandle / IAssetSystem — Framework 统一 API + 实例生命周期管理 + 场景串行化加载卸载
- [x] RES-03: `Core.AssetSystem` 薄编排 — 复用 Boot 已初始化的资源运行时，负责 Shutdown 和 AssetSystemReadyEvent 发布
- [x] POOL-01: PoolService.cs — Framework/Pool + Framework/Cache + Framework.Asset 的 DI 桥接
- [x] TEST-01: `Framework.TestKit` — 基于 Unity Test Framework / NUnit 的通用断言、Fake、Probe、Fixture、手动时间驱动
- [x] BOOT-CHAIN-02: Boot 收敛为更新壳（Entry 序列化启动配置；Boot 仅更新资源/代码并反射调用热更层正式入口；VContainer root 由 ProjectStartup 创建）
- [x] MODEL-01: General/Project Model 生命周期接入（`ModelLifecycle` 由 VContainer `IPostStartable` 在 Core 系统 Start 后驱动 `LoadAll()`）
- [x] LOG-01: `Framework.Log` 稳定日志门面 + 环境/模块开关接口 + Core ZLogger Unity Console provider 桥接（含现有 ZLoggerMessage 调用点裁剪）
- [x] LOG-AI-00: AI 运行日志规范落地（`.planning/AI_RUNTIME_LOGGING.md`：JSONL + session 清单、Boot/Core 职责、AI 分析流程、验收标准）
- [x] LOG-AI-01: AI 运行日志落盘与会话清单（`Framework.RuntimeLog` 纯 C# session writer + Boot 早期安装 + Core `GameLog`/`ILogger<T>`/ZLogger 接入）
- [x] LOG-AI-02: 首版日志收集与 AI 分析入口（`KJ/Runtime Logs/*` Editor 菜单：打开 latest、生成摘要、导出诊断包、清理本地日志）
- [x] HYB-00: HybridCLR 热更边界固化（托管 DLL 下发 / 需重启生效 / 真正换包规则）
- [x] HYB-01: HybridCLR 最小加载闭环代码落地（Boot 加载 AOT metadata + Core/General/Project DLL 后反射调用 ProjectStartup；Unity Editor/Player 验证待 HYB-02 工具链）
- [x] HYB-02A: 热更构建同步工具（`KJ/HybridCLR/*` Editor 菜单：生成/编译 HybridCLR 产物，同步 `.dll.bytes` RawFile，维护 YooAsset collector，回写打开的 Entry 序列化配置；日常 smoke 与完整构建前生成已拆分；工具归属 `Assets/Scripts/Boot.Editor/HybridCLR/`）
- [x] HYB-03: HybridCLR 热更边界裂变（AOT `Launcher` 壳 + 热更 `Boot`；10 热更程序集；`AssetConfig`/`AssetConstants` 迁入 AOT 共享 `Framework.AssetShared`；`IRemoteService` 死锁修复 `BootRemoteService`；AOT 极简日志 `BootStartupLog`；反射入口 `"Boot.BootUpdateRunner, Boot"`；对应 EditMode 测试 45/45 全绿、含 15 例 HYB-03 边界用例）
- [x] PKG-00: 构建打包全流程管线设计（S0–S9 全阶段编排，见 `ProgressDoc/Discuss/资源系统/Hy3_构建打包全流程管线_需求分析与设计.md`）
- [x] PKG-01: KJBuildPipeline 编排器入口（`Build(BuildProfile)` + 默认 Profile 菜单 + CI）
- [x] PKG-02: YooAsset 生产构建 Stage（当前 `DefaultPackage` 为纯 RawFile，使用 `RawFileBuildPipeline + RawBundle` → StreamingAssets；与旧 `EditorSimulateBuildInvoker` 不同 API）
- [x] PKG-03: AssetConfig.Mode 直接 YAML 写入 + 回滚（`StageApplyConfig.cs`：设 Offline 后保存三连，构建完成后 `RollbackAssetConfig()` 恢复 Editor 状态）
- [x] PKG-04: BuildPlayer + Gradle 编译（`StageBuildPlayer.cs`：IL2CPP Android）
- [x] PKG-05: fingerprint 增量构建（`BuildPipelineRunner`：Profile/Input/Tool/Output 指纹；成功后写 `state/{StageId}.fingerprint.json`）
- [x] PKG-06: 构建管理界面（Odin Dashboard 单一人工入口；不提供手动 mask/marker）
- [x] PKG-07: APK 冒烟验证（`StageSmokeRun.cs`：ADB 安装 + logcat 监听 + `latest.jsonl` 启动链验证）
- [x] PKG-08: 产物校验（`StageValidateArtifacts.cs`：APK 存在 + StreamingAssets 内容完整性）
- [x] PKG-09: 构建管线数据模型层（`Assets/Framework/BuildPipeline/` — 纯契约程序集：`BuildEnvironment`、`BuildIssue`/`BuildErrorCodes`、`BuildPlan`/`BuildStageFingerprint`/`BuildSkipDecision`、`BuildArtifactManifest`/`AiBuildHandoff`、`BuildExitCode`）
- [x] PKG-10: 构建管线配置与执行框架（`BuildProfile`/`BuildProfileValidator`、`BuildContext`/`BuildPaths`/`BuildEnvironmentSnapshot`、`IBuildStage`/`BuildStageBase`/`BuildStageRegistry`、`BuildPipelineRunner`、`BuildTransaction` 事务系统）
- [x] PKG-11: Stage 插件化重写（P0–P9 Stage 类实现 `IBuildStage`，输入/输出/跳过/验证/失败分析；`BuildStageRegistry` 注册与依赖验证）
- [x] PKG-12: 诊断与报告系统（`SmokeLogParser` 多里程碑判定、`FormalLeakageVerifier` Formal/Audit 泄露检查、`BuildAnalyzer` 规则化诊断、`BuildKnowledgeBase` 已知错误库、`BuildReportWriter` JSON/MD/HTML）
- [x] PKG-13: Odin Build Dashboard（`BuildDashboardWindow : OdinMenuEditorWindow` — Profile/Plan/Stage Monitor/Reports/Artifacts/Diagnostics 六视图）
- [x] PKG-14: CI 无头入口（`BuildCommandLine.cs` — `-executeMethod` + `-profile`/`-mode Full` 参数；退出码按阶段区分）
- [x] PKG-15: 构建管线 EditMode 验证（74/74 全绿：BuildProfile/BuildPaths/BuildStageRegistry/BuildReportData/BuildTransaction + BoundedStore/Cache/GameObjectPool/ObjectPool/CollectionPool/TypePool/Pool + HybridCLR HYB-03 15 边界 + ModelLifecycle + RuntimeLog + SystemManager + TestKit）
- [x] AOP-PERF-01: 打包耗时监控雏形（Editor-only、非 auto-reference 的 `Aop.asmdef`（ns `Framework.Aop`）单调时钟 Span/session/有界 Collector + `BuildTelemetry`；P2/P3/P4/P6 关键步骤埋点；`build_report.json/.md` schema 1.1.0 输出性能明细；Unity 编译通过，定向 EditMode 14/14 全绿；真实 P0-P9 数据待 E2E 构建）
- [x] DOC-01: 构建流程文档对齐（`ProgressDoc/Result/hybridclr_workflow.md` §4、`AGENTS.md` 构建管线章节）
- [x] BUGFIX-01: YooAsset EditorFileSystem 错误（APK 中 `AssetConfig.Mode` 未正确序列化为 Offline → 改用 YAML 直写）
- [x] BUGFIX-02: BuiltinFileSystem URI 错误（`BootLoader` 传入 `packageName` 作为 `packageRoot` → 改用无参重载取 `GetDefaultBuiltinPackageRoot()`）
- [x] BUGFIX-03: Android P0-P9 E2E 修复（YooAsset builtin 根统一为 `StreamingAssets/yoo`；P3 精确同步 10+3 DLL 且保留 P2 AOT；APK 构建事务化关闭 Gradle Export；StageVersion + 产物/事务依赖级联失效）。最终 RunId `20260719_131655_9b8295750`，P3-P9 实际执行并 `AllPassed=true`，APK 74,653,754 bytes。
- [x] BUGFIX-04: Android RawFile manifest 类型修复（P4 从 `ScriptableBuildPipeline + AssetBundle` 改为 `RawFileBuildPipeline + RawBundle`，解决设备端 `ABHLoadAssetOperation` 无法读取热更 DLL/AOT metadata）。
- [ ] UI-01: UISystem（UI 管理）
- [ ] UI-02: UIWindow 基类

## 文件清单

```
Assets/Scripts/
├── Boot/
│   ├── KJ.Boot.asmdef           ← 热更程序集（HYB-03 裂变后）。引用 Asset/Log/RuntimeLog/UniTask/AssetShared/YooAsset/Launcher；不引用 VContainer/HybridCLR.Runtime/Core/General/Pool/Cache/Event/MessagePipe
│   ├── BootUpdateRunner.cs      ← 热更入口：被 Launcher 反射启动；资源版本检查/清单更新/下载/AOT metadata/Assembly.Load，反射 Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)，并回放早期日志
│   ├── BootRuntimeLogBootstrap.cs ← 热更层早期 RuntimeLog session 安装；Core/ZLogger 尚未接管前也能落盘
│   └── Launcher/                      → KJ.Launcher.asmdef (AOT Shell，HYB-03 裂变新增)
│       ├── KJ.Launcher.asmdef         ← 仅引用 UniTask/YooAsset/HybridCLR.Runtime/AssetShared；硬约束：不引用任何 Framework/热更程序集
│       ├── Entry.cs              ← AOT 入口 MonoBehaviour：Awake → DontDestroyOnLoad → new BootLoader().RunAsync()
│       ├── BootLoader.cs         ← AOT 壳：初始化 YooAsset、加载全部热更 DLL、构造 BootBridge、反射 BootUpdateRunner
│       ├── BootBridge.cs         ← 跨 AOT→热更边界的状态载体（Package/Settings/View/Config/EarlyLogs）
│       ├── BootStartupLog.cs     ← AOT 阶段日志（纯文本 + 内存快照，不依赖 Framework.Log/RuntimeLog）
│       ├── IsExternalInit.cs
│       ├── Data/
│       │   ├── BootStartupSettings.cs ← Entry 序列化配置：资源更新、热更 DLL/AOT metadata、正式入口
│       │   ├── BootAssemblyEntry.cs   ← 热更 DLL 条目
│       │   ├── BootMetadataEntry.cs   ← AOT metadata 条目
│       │   └── IBootStartupView.cs    ← 启动更新 UI 最小接口（状态/进度/修复可见）
│       └── YooAssetStrategy/
│           └── BootRemoteService.cs   ← AOT 侧 IRemoteService（死锁修复点）

├── Boot.Editor/
│   ├── Boot.Editor.asmdef       ← Editor-only，引用 Boot + Asset + Framework.BuildPipeline + Framework.Asset.Editor + Framework.External.Sirenix.Odin.Editor + HybridCLR.Editor + YooAsset + YooAsset.Editor + AssetShared + Launcher（共 10）
│   ├── Build/
│   │   ├── KJBuildPipeline.cs         ← BuildProfile-only 构建入口 + KJ/Build/* 菜单
│   │   ├── Stages/
│   │   │   ├── P0_PlanStage.cs        ← P0: Profile/BuildPlan/输出目录
│   │   │   ├── P1_PreflightStage.cs   ← P1: 环境与约束预检
│   │   │   ├── P2_GenerateStage.cs    ← P2: HybridCLR GenerateAll
│   │   │   ├── P3_HybridCLRStage.cs   ← P3: DLL/AOT metadata 编译同步
│   │   │   ├── P4_BuildAssetStage.cs  ← P4: YooAsset 生产构建
│   │   │   ├── P5_ApplyConfigStage.cs ← P5: 事务化运行时配置
│   │   │   ├── P6_BuildPlayerStage.cs ← P6: IL2CPP Player 构建
│   │   │   ├── P7_VerifyStage.cs      ← P7: 静态产物/Formal 校验
│   │   │   ├── P8_SmokeStage.cs       ← P8: Standalone/Android Runtime smoke
│   │   │   └── P9_ReportStage.cs      ← P9: 日志归档
│   │   ├── Config/
│   │   │   ├── BuildProfile.cs        ← 🆕 ScriptableObject 环境/平台/签名/日志/冒烟/输出配置
│   │   │   ├── BuildProfileValidator.cs ← 🆕 Formal/Audit 强约束校验规则
│   │   │   └── BuildProfileSet.cs     ← 🆕 Profile 集合（Odin 列表入口）
│   │   ├── Pipeline/
│   │   │   ├── BuildContext.cs        ← 🆕 单次构建上下文（RunId/Plan/Artifacts/Issues/Transaction）
│   │   │   ├── BuildPaths.cs          ← 🆕 输出路径集（ArchiveRoot/ArtifactsDir/LogsDir/ReportsDir/StateDir）
│   │   │   ├── BuildEnvironmentSnapshot.cs ← 🆕 Unity/Git/OS/SDK 版本快照
│   │   │   ├── IBuildStage.cs         ← 🆕 Stage 接口（GetInputs/CanSkip/Execute/Verify/AnalyzeFailure/Rollback）
│   │   │   ├── BuildStageBase.cs      ← 🆕 Stage 抽象基类（默认实现 + 输出验证）
│   │   │   ├── BuildStagePolicy.cs    ← 🆕 Stage 策略标志（Required/Optional/AlwaysRun/NoSkip/Transactional/…）
│   │   │   ├── BuildStageRegistry.cs  ← 🆕 Stage 注册/排序/依赖验证
│   │   │   ├── BuildPipelineRunner.cs ← 🆕 Plan 驱动编排器 + 报告写入
│   │   │   └── BuildTransaction.cs    ← 文件/PlayerSettings snapshot + rollback
│   │   ├── Diagnostics/
│   │   │   ├── SmokeLogParser.cs      ← 🆕 多里程碑冒烟判定（boot.log + latest.jsonl）
│   │   │   ├── FormalLeakageVerifier.cs ← 🆕 Formal/Audit 泄露检查
│   │   │   ├── BuildAnalyzer.cs       ← 🆕 问题分类/合并/推荐
│   │   │   └── BuildKnowledgeBase.cs  ← 🆕 常见错误 → 修复建议映射
│   │   ├── Reports/
│   │   │   └── (报告写入由 BuildPipelineRunner 内嵌)
│   │   ├── UI/
│   │   │   └── BuildDashboardWindow.cs ← 🆕 OdinMenuEditorWindow：Profile/Plan/Stage/Reports/Artifacts/Diagnostics
│   │   └── CI/
│   │       └── BuildCommandLine.cs    ← 🆕 batchmode CI 入口（-profile/-mode Full 参数）
│   └── HybridCLR/
│       └── KJHybridClrBuildTools.cs ← `KJ/HybridCLR` 菜单：4 个开发入口；Install / Prepare Boot Scene / Validate Outputs 收入 Maintenance 子菜单；同步与 Entry 写入子步骤仅供复合流程内部调用
├── Core/
│   ├── KJ.Core.asmdef          ← 引用 Asset+Event+Pool+Cache+Log+RuntimeLog+VContainer+MessagePipe+MessagePipe.VContainer+UniTask+ZLinq+ZLogger.Unity+AssetShared（共 13）；不引用 General/Project
│   ├── PoolService.cs          ← [CoreSystem] Framework.Pool DI 桥接 + 集合池快捷入口
│   ├── Logging/
│   │   ├── GameLogBridge.cs    ← IGameLogSink 到 RuntimeLog + Core ZLogger 管线的桥接（adapter，不是 CoreSystem）
│   │   ├── RuntimeLogBootstrap.cs       ← Core 侧 RuntimeLog session 创建/补全：Unity 信息、资源信息、路径、frame provider
│   │   └── RuntimeLogLoggerProvider.cs  ← Microsoft.Extensions.Logging provider，把 ILogger<T>/ZLoggerMessage 写入同一 JSONL session
│   ├── Bootstrap/
│   │   ├── CoreStartupContext.cs
│   │   ├── CoreTypeRegistration.cs
│   │   ├── CoreContainerRegistration.cs
│   │   └── CoreBootstrapStage.cs
│   ├── Systems/
│   │   ├── ISystem.cs              ← ISystem + ITickableSystem 接口
│   │   ├── SystemManager.cs        ← 系统生命周期管理器（VContainer 驱动）
│   │   ├── SystemManagerLog.cs     ← SystemManager ZLogger 源生成日志
│   │   ├── StartupProbeSystem.cs   ← 启动链路验证系统
│   │   ├── StartupProbeSystemLog.cs← StartupProbeSystem ZLogger 源生成日志
│   │   ├── Attributes/CoreSystemAttribute.cs
│   │   ├── Events/AppStartedEvent.cs
│   │   ├── Events/AppShuttingDownEvent.cs
│   │   └── ICoreStartupStatus.cs ← Core 启动结果，供业务层决定是否加载
│   └── Asset/
│       ├── AssetSystem.cs              ← [CoreSystem] Framework.Asset 生命周期编排 + Ready 事件
│       ├── AssetSystemLog.cs           ← AssetSystem ZLogger 源生成日志
│       └── AssetSystemReadyEvent.cs    ← [GameEvent] 就绪通知
├── Core.Editor/
│   ├── Core.Editor.asmdef       ← Editor-only，引用 Core + Log + RuntimeLog
│   └── Logging/
│       └── RuntimeLogEditorTools.cs ← `KJ/Runtime Logs` 菜单：打开 latest、生成摘要、导出诊断包、清理日志
├── General/
│   ├── KJ.General.asmdef       ← 引用 Core+Event+Log+MessagePipe+MessagePipe.VContainer+VContainer+VContainer.Unity+ZLinq（共 8）；不引用 Project
│   ├── Bootstrap/
│   │   ├── GeneralContainerRegistration.cs
│   │   └── GeneralBootstrapStage.cs
│   └── Models/
│       ├── IModel.cs
│       ├── ModelAttribute.cs
│       ├── ModelLifecycleLog.cs
│       └── ModelLifecycle.cs
└── Project/
    ├── KJ.Project.asmdef       ← 引用 Asset+Core+General+Event+Log+MessagePipe+MessagePipe.VContainer+VContainer+VContainer.Unity（共 9）；可引用所有下层
    └── Bootstrap/
        ├── ProjectStartup.cs        ← Boot 反射调用的正式热更入口，接收 IAssetRuntime
        ├── ProjectLifetimeScope.cs  ← VContainer root，串联 Core→General→Project 注册，并复用 Boot AssetRuntime
        ├── ProjectBootstrapper.cs   ← 静态 Project 注册入口
        └── ProjectBootstrapStage.cs

Assets/Editor/
└── .gitkeep                     ← 仅保留跨层 Editor 工具占位；模块工具放各自 `*.Editor/`

Assets/GameRes/
└── HotUpdate/
    ├── Dlls/                    ← Core/General/Project `.dll.bytes` RawFile 输出目录
    └── AotMetadata/             ← mscorlib/System/System.Core `.dll.bytes` RawFile 输出目录

Assets/Framework/
├── Pool/
│   ├── Pool.asmdef             ← 引用 UniTask + Cache
│   ├── IPool.cs                ← IPool<T> / IPoolLease<T> / IPoolable
│   ├── ObjectPool.cs            ← 泛型对象池 (lock 并发安全 + 重复归还防护)
│   ├── SingleThreadObjectPool.cs ← 单线程轻量对象池（CollectionPool 内部热路径使用；UNITY_ASSERTIONS 下校验线程/重复归还）
│   ├── CollectionPool.cs       ← 集合池静态入口 (List/HashSet/Queue/Stack/Dictionary)，内部使用 SingleThreadObjectPool
│   ├── PooledCollections.cs    ← RAII using 包装 (PooledList<T> 等)
│   ├── PoolDependencies.cs     ← 静态委托注入 (LoadAssetAsync / ReleaseAsset)
│   ├── PoolLease.cs            ← PoolLease<T> : IPoolLease<T>
│   ├── PoolStatistics.cs       ← 池统计 (IdleCount / RentCount 等)
│   ├── InstanceRecyclePolicy.cs ← IInstanceRecyclePolicy / CapacityInstancePolicy / PersistentInstancePolicy
│   ├── Types/TypePool.cs       ← ConcurrentDictionary 类型池注册表
│   ├── Unity/GameObjectPool.cs  ← GameObject 池（PrefabPoolState 合并状态 + 反向索引防双回收/污染；prefab 缓存走 BoundedStore+LruPolicy；IInstanceRecyclePolicy 可插拔库存；[MainThread] 断言）
│   └── Unity/PoolContainerMode.cs ← ChangeParent / MovePos
├── Cache/
│   ├── Cache.asmdef             ← 无外部依赖
│   ├── ICache.cs               ← ICache<TKey,TValue> 接口（由 BoundedStore 实现）
│   ├── IStoreEvictionPolicy.cs ← 淘汰策略契约（原 ICacheEvictionPolicy，方法改 OnAdded/OnAccessed/OnRemoved）
│   ├── BoundedStore.cs         ← 当前缓存容器（实现 ICache；single-flight GetOrAdd；Put 覆盖走 Remove+Add；onEvicted 锁外）
│   ├── Cache.cs                ← [Obsolete] 转发壳，内部委托 BoundedStore（新代码直接用 BoundedStore）
│   ├── ResContainer/ICacheResContainer.cs ← 资源容器接口
│   ├── ResContainer/ResourceCache.cs     ← 工厂模式资源容器
│   └── Strategy/
│       ├── LruPolicy.cs        ← O(1) LinkedList + Dictionary LRU（原 LruCachePolicy）
│       ├── TtlPolicy.cs        ← 按 TTL 过期淘汰（读路径可由 BoundedStore 清理过期项）
│       ├── CapacityPolicy.cs   ← FIFO 容量淘汰
│       └── CompositePolicy.cs  ← 多策略扇出组合
├── Asset/
│   ├── Asset.asmdef                  ← 引用 UniTask + YooAsset + Log + AssetShared（共 4）
│   ├── AssetConfig.cs                ← ScriptableObject: PlayMode + CDN URL
│   ├── AssetConstants.cs             ← Priority 常量
│   ├── AssetRuntime.cs               ← YooAsset 适配实现，运行时加载/释放/下载/RawFile bytes 读取
│   ├── AssetRuntimeFactory.cs        ← Boot 通过工厂获取 IAssetRuntime，避免直接触碰完整实现类型
│   ├── IAssetRuntime.cs              ← Boot/Core 共享的启动期资源运行时接口
│   ├── IAssetSystem.cs               ← 对上层暴露的稳定资源接口
│   ├── AssetHandle.cs                ← 对外句柄封装 (IDisposable)
│   ├── AssetInstanceHandle.cs        ← 实例化句柄
│   ├── AssetSceneHandle.cs           ← 场景句柄
│   └── AssetDownloadHandle.cs        ← 下载器句柄，不暴露 YooAsset 类型
├── Asset.Editor/
│   ├── Framework.Asset.Editor.asmdef ← Editor-only，引用 YooAsset.Editor
│   └── YooAsset/
│       └── KJAssetIgnoreRule.cs      ← YooAsset 收集忽略规则：`_` 前缀路径段不参与资源包
├── Event/
│   ├── Event.asmdef                  ← 引用 ZLinq（零分配 LINQ，第三方包）
│   ├── GameEventAttribute.cs         ← 统一事件标记
│   └── GameEventTypeScanner.cs       ← 事件类型扫描与 struct 校验
├── AssetShared/              ← HYB-03 新增：AOT 与热更共享的资产配置
│   ├── AssetShared.asmdef        ← 零外部依赖，AOT Launcher 与热更 Boot 均可引用
│   ├── AssetConfig.cs            ← ScriptableObject: PlayMode + CDN URL 等（从 Asset/ 迁入）
│   └── AssetConstants.cs         ← Priority 常量（从 Asset/ 迁入）
├── Log/
│   ├── Log.asmdef                    ← 无外部依赖，noEngineReferences=true
│   ├── GameLog.cs                    ← 稳定日志门面、模块树声明
│   ├── GameLogSymbols.cs             ← 编译期裁剪符号唯一常量入口
│   ├── GameLogProfile.cs             ← 环境默认级别 + 模块覆盖规则
│   ├── GameLogConfig.cs              ← 面板/打包脚本配置入口数据
│   ├── GameLogSwitches.cs            ← 应用配置到 GameLog 的统一入口
│   ├── GameLogEntry.cs               ← 日志条目
│   ├── IGameLogSink.cs               ← 输出端口，由 Core 桥接到 ZLogger
│   ├── GameLogEnvironment.cs         ← dev/formal/qa 等环境枚举
│   ├── GameLogModuleRule*.cs         ← 模块规则数据
│   └── GameLog*Attribute.cs          ← 未来 Editor 面板扫描用声明属性
├── RuntimeLog/
│   ├── RuntimeLog.asmdef             ← 只引用 Log，noEngineReferences=true
│   ├── RuntimeLogSession.cs          ← JSONL writer + session manifest + latest 指针 + flush/dispose
│   ├── RuntimeLogManager.cs          ← 当前 session 管理；避免覆盖 Core GameLogBridge
│   ├── RuntimeLogEntry.cs            ← AI 可读运行日志条目
│   ├── RuntimeLogSessionInfo.cs      ← session 清单数据：Unity/平台/资源包/热更程序集/AOT metadata
│   ├── RuntimeLogJson.cs             ← 无第三方依赖 JSON serializer
│   ├── RuntimeLogPhaseResolver.cs    ← Boot/HybridCLR/Core.Asset/Core.Init/ModelLifecycle 等 phase 归类
│   ├── RuntimeLogFileName.cs
│   └── RuntimeLogSessionId.cs
└── TestKit/
    ├── TestKit.asmdef                ← 引用 Unity Test Framework / NUnit，autoReferenced=false
    ├── Assertions/AssertEx.cs        ← NUnit 断言扩展
    ├── Fakes/RecordingAssetSystem.cs ← 可记录资源系统 fake
    ├── Fakes/RecordingRuntimeLogSink.cs ← 可记录 GameLog sink，测试启动缓冲和 RuntimeLog 接入顺序
    ├── Fixtures/TestGameObjectRoot.cs← 临时 GameObject 根节点
    ├── Probes/CallProbe.cs           ← 调用顺序记录
    ├── Probes/RecordingEventSink.cs  ← 事件记录
    └── Time/                         ← ManualClock / ManualTickDriver

Assets/Framework/Aop/                        ← 🆕 Editor-only 单调时钟（AOP-PERF-01）
├── Aop.asmdef                        ← autoReferenced=false，纯 C# Span/session/Collector
├── IAopClock.cs                      ← 单调时钟接口
├── IAopCollector.cs                  ← Span 收集器接口
├── InMemoryAopCollector.cs           ← 有界内存收集 + 故障隔离
├── AopSpan.cs                        ← 父子 Span 结构
├── AopRuntime.cs                     ← Aop 运行时入口
└── AopEvent.cs                       ← Aop 事件定义

Assets/Framework/BuildPipeline/           ← 🆕 构建管线纯数据契约层（PKG-09）
├── Framework.BuildPipeline.asmdef    ← noEngineReferences=false，不引用 UnityEditor/Boot/Core
├── Environment/
│   └── BuildEnvironment.cs          ← Dev/QA/Profiling/Audit/Formal/Pre 环境枚举
├── Plan/
│   ├── BuildPlan.cs                 ← 构建计划 + 跳过/运行计数
│   ├── BuildStageInputs.cs          ← Stage 输入规格（源路径/工具版本/Profile 哈希）
│   ├── BuildStageOutputs.cs         ← Stage 输出规格（RequiredFiles/RequiredDirectories）
│   ├── BuildStageFingerprint.cs     ← Stage 指纹（Pipeline/stage 版本 + 输入/输出/工具 hash）
│   └── BuildSkipDecision.cs         ← 跳过决策（原因代码/人类理由/证据）
├── Diagnostics/
│   ├── BuildIssue.cs                ← 结构化问题（Code/Severity/StageId/Evidence/SuggestedFix）
│   ├── BuildIssueSeverity.cs        ← Error/Warning/Info
│   └── BuildErrorCodes.cs           ← 50+ 稳定错误码（KJ-BUILD-PLAN-* 到 KJ-BUILD-REPORT-*）
├── Reports/
│   ├── BuildArtifactManifest.cs     ← 产物清单（路径/大小/SHA256）
│   └── AiBuildHandoff.cs            ← AI 可读交接数据（失败阶段/阻断问题/日志路径/建议）
└── CI/
    └── BuildExitCode.cs             ← CI 退出码（0/10/20/…/99）
```

## 外部依赖

| 包名 | 来源 | 版本 |
|------|------|------|
| VContainer | GitHub (hadashiA) | 1.1.0 |
| UniTask | GitHub (Cysharp) | 2.5.11 |
| MessagePipe | GitHub (Cysharp) | — |
| MessagePipe.VContainer | GitHub (Cysharp) | — |
| YooAsset | GitHub (tuyoogame) | 3.0 (UPM git, path=Assets/YooAsset) |
| ZLogger | GitHub UPM + NuGetForUnity (Cysharp) | 2.5.10 |
| ZLinq | GitHub UPM + NuGetForUnity (Cysharp) | 1.5.6 |
| ZString | GitHub UPM (Cysharp) | 2.6.0 |

## 编码约束补充

- ZLogger / ZString / ZLinq 已纳入技术栈；后续新模块默认考虑这些库，尤其是 Framework/Core 的热路径。
- 日志接口在 `Framework.Log`；AI runtime logging 文件能力在 `Framework.RuntimeLog`；Core 注册 `ILoggerFactory` / `ILogger<T>` / ZLogger Unity provider，并通过 `GameLogBridge : IGameLogSink` 同时桥接到 RuntimeLog 和 Console。
- 普通日志使用 `GameLog` 门面，业务层不直接 `Debug.Log`，也不散写 `[Conditional]`。
- 高频日志优先使用 ZLogger Source Generator：`static partial` 扩展方法 + `[ZLoggerMessage]`，并在 `XxxLog.cs` 日志声明方法上集中加对应 `[Conditional(GameLogSymbols...)]` 以支持 formal 包裁剪。
- dev/formal/qa 等打包环境通过 `KJ_LOG_*` 编译符号控制调用点保留，符号常量唯一入口是 `Framework.Log.GameLogSymbols`；多个 `[Conditional]` 是 OR 关系，模块开关通过 `GameLogConfig` / `GameLogSwitches` / `GameLogProfile` 控制。
- AI 运行日志规范见 `.planning/AI_RUNTIME_LOGGING.md`：后续 Editor/dev/QA 运行应生成 JSONL + session 清单；AI 调试、Player smoke、资源矩阵和热更验证优先读取日志文件分析，不默认要求用户截图 Console。
- `Logs/Runtime/` 是本地生成物，已被 `.gitignore` 的 `Logs/` 规则覆盖；日志不得记录 token、密码、实名账号、支付信息等敏感数据。
- ZString 用于热路径字符串构建；ZLinq 用于可读但低分配的集合查询，优先 `AsValueEnumerable()`，暂不默认开启全局 DropIn Generator。
- 已建 `Framework.Pool` / `Framework.Cache` 是后续模块的默认性能基础设施：主线程临时集合用 `CollectionPool` / `PooledCollections`（内部 `SingleThreadObjectPool`，正式包无 lock）；短生命周期/跨线程普通对象用 `ObjectPool<T>` / `TypePool`；GameObject 复用走 `GameObjectPool` + `PoolService`，严格主线程调用；有容量/淘汰/TTL 需求的数据用 `BoundedStore<TKey,TValue>` + `IStoreEvictionPolicy` / `IStoreExpirationPolicy` 策略（新代码直接用 `BoundedStore`）。
- 新模块不得重复实现私有对象池或缓存容器；确有特殊需求时，优先扩展 Framework.Pool / Framework.Cache 的稳定接口，再由 Core 层桥接。
- Unity 2022.3.62f2 满足 Source Generator 版本前提；使用需要预览语法的生成器能力前，确认项目已启用 `-langVersion:preview`。

## 依赖红线与文档同步（2026-07-09 补充）

- **`.asmdef` 文件是唯一事实源**。`.planning/STATE.md` / `CODEMAP.md` 的引用清单由 `.asmdef` 派生；文档漂移由仓库根目录的 `asmdef_dependency_validator.py` 检测：
  `python asmdef_dependency_validator.py [ROOT]` 输出真实引用表并校验全部红线（Launcher 最小依赖、Boot 不引用上层/pkg、Framework→Scripts 禁止、Framework 分层单向、无环、HybridCLR 一致性、Editor 隔离）。改 `.asmdef` 后重跑该脚本，文档以脚本输出为准更新。
- **Framework 两层单向约定（已通过校验，当前即 acyclic / one-way）**：
  - Tier0 叶子（禁止引用任何 Framework 包）：`AssetShared`、`Log`、`Cache`、`BuildPipeline`。
  - Tier1 组合（只允许引用**更低 Tier** 的 Framework 包 + Packages）：`Event`、`Pool`、`RuntimeLog`、`Asset`。
  - 当前合法依赖边：`Pool→Cache`、`RuntimeLog→Log`、`Asset→Log+AssetShared`（均 T1→T0）。
  - 红线：禁止 Tier0→Tier1（向上）、禁止 Tier1→Tier1（横向）；新增/修改 Framework 引用必须先确认方向，否则校验失败。`TestKit` 为测试层（`autoReferenced=false`，不进生产包），不受此约束但也不应被生产代码引用。
- 物理拆分 asmdef（如把 Framework 改名分层）**当前非必需**：方向已正确且无环，靠上面约定 + 校验脚本即可长期保持单向；如未来想要更显式的边界再议。

## 下一步

Phase 1 剩余事项：

- **当前优先级：构建管线 EditMode gate ✅ 已通过**（74/74 全绿）。PKG-09~PKG-15 全部完成。下一步：
  - [ ] **端到端构建验证**：从 `KJ/Build/Dashboard` 执行完整构建，验证 P0-P9 全链路通过（Standalone + Android）
  - [ ] **Odin Dashboard 手工验证**：打开 `KJ/Build/Dashboard`，验证 Profile 列表、Stage 监控、报告查看、构建按钮交互
- PKG-VERIFY: 构建后运行 Player，确认 Launcher→YooAsset→HybridCLR→Boot→Core 全链路成功
- RES-VERIFY-01: 资源加载矩阵验证
- RES-VERIFY-02: PlayMode 覆盖（EditorSimulate + Player Offline）
- HYB-VERIFY-01: 热更新行为 smoke
- LOGIN-01: General/Login 业务模型

Phase 2 规划：

- NET-01~05: 网络层
- UI-03~04: 窗口模式 + 窗口栈
- CFG-01~02: Luban 配置表集成

## 最新验证记录

- 2026-07-19: **Android P0-P9 E2E 通过**：Dashboard 自动构建 RunId `20260719_131655_9b8295750`，仅 P2 合法跳过，P3/P4/P5/P6/P7/P8/P9 全部实际通过；生成 `BuildBackup/Dev/1.0.0/1/KJ.apk`（74,653,754 bytes）。修复 P4/P7 YooAsset `StreamingAssets/yoo` 路径、P6 Gradle Export 状态泄漏、P3 AOT 清理与重复同步实现、StageVersion 与下游级联失效。
- 2026-07-19: **AOP-PERF-01 打包耗时监控雏形落地**：新增 `Aop.asmdef` 纯 C# 单调时钟 Span、session、父子关系、有界内存 Collector 和故障隔离；`BuildPipelineRunner` 管理会话并将 P2/P3/P4/P6 内部步骤写入 `BuildReportData.PerformanceSpans`，JSON/Markdown schema 升至 1.1.0；Unity batchmode 编译通过，`Boot.Editor.Build.Tests` 14/14 全绿（含 5 个 AOP/报告新用例）。完整 P0-P9 打包耗时数据仍待 Standalone/Android E2E。
- 2026-07-10: **构建管线单架构收敛 + Unity 编译通过**：删除旧 `BuildConfig.cs/.asset`、旧 `BuildReport.cs`、`StageDependencyTracker.cs`、marker/mask 入口；配置统一为 `BuildProfile`，执行统一为 `BuildPipelineRunner` P0-P9；实现真实 Profile/Input/Tool/Output fingerprint；`BuildConfigTransaction` 重命名并强化为 `BuildTransaction`，统一回滚 AssetConfig、Defines、ScriptingBackend、Editor build flags；修复 P0 初始化、P9 报告顺序、Formal/Audit mandatory smoke、测试 asmdef 引用。用户确认 Unity 编译无错误。待 EditMode、Standalone P0-P9 E2E、Android ADB smoke。
- 2026-07-09: **构建管线工业级重构落地**：PKG-09~PKG-14 全部代码完成 + Unity 编译通过。删除旧 S0-S9 static 方法（StagePreFlightCheck/StageGenerateAll/StageCompile/StageSync/StageBuildYooAsset/StageApplyConfig/StageBuildPlayer/StageValidateArtifacts/StageSmokeRun/StageReport + AndroidToolResolver + PlayerBuildPrivatePathValidator）。`KJBuildPipeline.cs` 重写为委托 `BuildPipelineRunner`，`BuildDashboardWindow` 新增 Odin 六视图面板；兼容 `BuildStagePanel` 后于 2026-07-17 删除，人工入口收敛为 Dashboard。
- 2026-07-08: 构建打包全流程管线 PKG-00~08 落地：`KJBuildPipeline.cs` S0–S9 全阶段编排、`StageBuildYooAsset` 生产构建（`ScriptableBuildPipeline`，非旧 `EditorSimulateBuildInvoker`）、`StageApplyConfig` YAML 直写 `AssetConfig.Mode=Offline`+回滚、`StageBuildPlayer` IL2CPP Android、`StageValidateArtifacts` 产物校验、`StageSmokeRun` ADB 冒烟验证、`StageDependencyTracker` 变更检测级联、`BuildStagePanel` 可视化管理面板、`BuildConfig` 构建配置。
- 2026-07-08: BUGFIX-01 — 修复 YooAsset APK EditorFileSystem 错误：`StageApplyConfig` 从 ScriptableObject API（SetDirty+SaveAssets）改为 YAML 正则直写 `Mode: 0→Mode: 1` + `ImportAsset(ForceSynchronousImport)`，消除 `AssetDatabase.Refresh` 竞态。
- 2026-07-08: BUGFIX-02 — 修复 BuiltinFileSystem URI 格式错误：`BootLoader` 不再传 `packageName` 给 `CreateDefaultBuiltinFileSystemParameters`，改用无参重载取 `GetDefaultBuiltinPackageRoot()` 自动计算 `jar:file://` 路径。
- 2026-07-08: DOC-01 — 文档对齐：更新 `CODEMAP.md`（HYB-03 裂变后文件路径/启动流/依赖矩阵）、`AGENTS.md`（新增构建管线章节）、`STATE.md`（新增 PKG-* 和 BUGFIX-* 项）、`hybridclr_workflow.md` §4（构建管线入口）。
- 2026-07-08: 全量构建已在 Gradle 编译阶段（S1 MethodBridge 迭代 10/10）；`maxMethodBridgeGenericIteration` 保持 10 层（参考旧项目 `KJBuildSettings.cs`）。
- 2026-07-07: HYB-03 热更边界裂变已实现并验证。EditMode 全工程 **45/45 全绿**。
- 2026-07-05: 用户确认 Unity 编译与 Editor Play 无报错。

---
*Phase 0 Architecture 完成: 2026-06-29 | Phase 1 资源系统 Framework 化: 2026-07-02 | 启动链去 prefab 化落地: 2026-07-04 | 构建打包管线落地: 2026-07-08*
