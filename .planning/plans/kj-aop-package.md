# Framework.Aop 工业级设计与开发计划

> 状态：Phase 1 雏形已实现，真实构建验证待执行  
> 日期：2026-07-19  
> 适用项目：KJ Unity 2022.3.62f2  
> 核心决策：先建设可靠的 Observability，再通过统一 ILPostProcessor 提供透明 AOP

---

## 1. 执行摘要

KJ 当前首先需要解决的是构建管线内部缺乏细粒度性能数据，而不是立即建设一个覆盖所有场景的通用 AOP 框架。

本计划将工作拆成两条相互支撑、但可独立交付的路线：

1. **近期 Observability 路线**：在构建管线中使用显式 `AopSpan`，采集 P2/P3/P4/P6 等关键步骤的耗时、状态和指标，尽快形成可用报告。
2. **长期透明织入路线**：使用 Unity ILPostProcessor + Mono.Cecil 实现方法级透明织入；先限定在 `Boot.Editor`，验证成熟后再扩展到 Runtime 和 HybridCLR 热更新程序集。

Source Generator 不承担任意方法织入。它只能新增源码，不能改写已有方法体或调用点，因此在本方案中只用于：

- 编译期规则诊断；
- Attribute 合法性检查；
- 稳定方法描述符或强类型辅助代码生成；
- IDE 开发体验增强。

最终目标不是堆积 Attribute，而是形成一套可验证、可观测、可关闭、可演进且不破坏业务语义的横切基础设施。

---

## 2. 当前需求与问题边界

### 2.1 已有能力

`BuildPipelineRunner` 已经集中执行所有 P0-P9 Stage，并记录：

- Stage 开始和结束时间；
- Stage 状态；
- Stage 总耗时；
- 异常和诊断结果；
- 构建报告与 AI handoff。

因此，重新对所有 `Execute()` 做一层方法计时不会带来足够价值。

### 2.2 当前真实缺口

优先补齐以下观测能力：

- P2 GenerateAll 内部步骤耗时；
- P3 DLL 编译、同步和 metadata 处理耗时；
- P4 YooAsset 收集、构建、拷贝耗时；
- P6 Unity Player、Gradle、IL2CPP 等关键阶段耗时；
- 外部工具调用的耗时、退出码和超时状态；
- bundle 数量、DLL 大小、APK 大小等构建指标；
- 同一 Profile、平台和工具版本下的跨次性能对比；
- CI 可解释的预算告警；
- Dashboard、Markdown、JSON、Chrome Trace 和 AI handoff 的统一数据来源。

### 2.3 中长期需求

当出现经过验证的 Runtime 需求后，支持：

- 方法性能 Span；
- 调用链追踪；
- 主线程契约检查；
- 异常观测；
- 资源加载等关键链路的低开销埋点；
- HybridCLR 热更新程序集的安全织入。

### 2.4 非目标

本计划不采用：

- Runtime 动态代理；
- `DispatchProxy` / `MethodInfo.Invoke`；
- Harmony Hook；
- 运行时程序集扫描作为织入入口；
- 未经约束的参数值和返回值采集；
- 在 AOP 中实现通用业务重试；
- 向 AOT `Launcher` 添加任何 Framework 或热更新程序集引用。

---

## 3. 核心设计原则

### 3.1 观测不能改变业务结果

性能采集、Trace 和 Sink 失败不得：

- 吞掉业务异常；
- 修改返回值；
- 改变取消语义；
- 阻断构建或游戏主流程；
- 覆盖原始异常堆栈。

仅当 CI Policy 明确判定预算超限时，才允许在业务执行完成后改变 CI 退出结果。

### 3.2 显式能力先于透明能力

先用显式 Span 验证数据模型、报告价值和运行成本，再引入 IL 织入。透明语法不能成为跳过语义验证的理由。

### 3.3 单一织入语义

Editor 和 Runtime 的透明方法织入统一使用 ILPostProcessor。避免 Editor Source Generator 与 Runtime ILPostProcessor 形成两套行为不同的实现。

### 3.4 编译期 Opt-in

只有同时满足以下条件的程序集才允许织入：

1. 引用 `Aop` 程序集；
2. 声明 `[assembly: AopEnabled]`；
3. 位于 Weaver allowlist；
4. 不位于永久 blocklist；
5. 通过 Attribute 和方法签名诊断。

### 3.5 默认安全与有界

- 默认不采集参数值、返回值和完整异常消息；
- Tag 数量和长度有上限；
- Sink 队列有容量和丢弃策略；
- 高频方法支持采样和聚合；
- 绝不输出本机绝对源码路径；
- 禁止把密钥、签名信息和用户数据写入性能产物。

### 3.6 Framework 独立性

`Assets/Framework/Aop/` 只依赖 Packages 或纯 C# BCL，不引用 `Assets/Scripts`。项目接入和构建报告桥接放在 `Boot.Editor`。

---

## 4. 总体架构

```text
Assets/Framework/Aop/
  Aop.asmdef                         Runtime 契约和轻量采集核心
  Attributes/                       Attribute 稳定契约
  Runtime/                          Span、Clock、Context、Collector
  Model/                            版本化事件模型

Assets/Framework/Aop.Editor/
  Aop.Editor.asmdef                  Editor-only
  Weaving/                          Unity ILPostProcessor + Mono.Cecil
  Diagnostics/                      Weaver 报告和 IL 校验

Assets/Framework/Aop.Analyzers/
  Aop.Analyzers.dll                  RoslynAnalyzer 预编译 DLL
  Aop.Analyzers.dll.meta             RoslynAnalyzer 标签

Tools/Aop.Analyzers/
  Aop.Analyzers.csproj               Analyzer/Generator 独立构建工程
  Tests/                             Roslyn 编译测试和 golden tests

Assets/Scripts/Boot.Editor/Build/Telemetry/
  BuildTelemetrySession.cs           构建会话接入
  BuildTelemetrySink.cs              BuildReport/Trace 转换
  BuildPerformancePolicy.cs          基线和预算判定

Assets/Tests/EditMode/Aop/
  Runtime/                           Span、Collector、故障隔离测试
  Weaving/                           织入行为与程序集边界测试
```

代码命名空间统一为：

```text
Framework.Aop
Framework.Aop.Editor
Boot.Editor.Build.Telemetry
```

不使用 `KJ.Aop` 命名空间。新 asmdef 文件不使用 `KJ.` 前缀。

---

## 5. 模块职责

### 5.1 Aop Runtime

`Aop.asmdef` 建议保持轻量，初期包含契约和运行核心，避免过早拆出大量程序集。

Phase 1 雏形暂设为 `includePlatforms: [Editor]` 且 `autoReferenced: false`，只由
`Boot.Build.Editor` 和测试程序集显式引用。Phase 6 完成热更新归属评审前，不进入 Player。

主要类型：

```csharp
PerformanceAttribute
AopEnabledAttribute
AopExcludeAttribute
AopSpan
AopSpanContext
IAopCollector
IAopClock
AopEvent
AopSessionInfo
AopSettings
```

约束：

- `noEngineReferences=true`；
- 不引用 UnityEditor；
- 不引用 Core、General、Project、Boot；
- 不直接依赖 ZLogger 或 RuntimeLog；
- Collector 未安装时为低成本 no-op；
- Collector 抛异常时自动隔离并记录有限的内部诊断。

### 5.2 ILPostProcessor

负责真正的透明方法织入：

```text
原始方法
  -> 进入时创建 Span
  -> 正常返回时标记 Success
  -> 异常路径标记 Failure 后原样 rethrow
  -> finally 中结束 Span
```

必须具备：

- `WillProcess` 精确程序集过滤；
- 只识别完整限定名匹配的 Attribute；
- 稳定、确定性的 MethodId；
- 异步和 iterator 状态机识别；
- 重复织入检测；
- Weaver 版本标记；
- PDB/MDB symbol 保留；
- 原始异常堆栈和控制流保持；
- 失败时输出可定位的编译诊断，而不是静默跳过。

### 5.3 Analyzer

Analyzer 不执行织入，只负责提前阻止错误使用：

- 未启用程序集使用 AOP Attribute；
- 不支持的方法签名；
- 冲突或重复 Attribute；
- Launcher 或 blocklist 程序集引用 Aop；
- 超出允许范围的参数采集配置；
- Runtime 高频方法未配置采样策略；
- async/iterator 语义尚未启用时的误用。

Analyzer 必须作为独立预编译 DLL，通过 `RoslynAnalyzer` 标签接入 Unity，不能放在普通 Editor asmdef 中期待自动运行。

### 5.4 Collector 与 Sink

Collector 负责接收结构化事件，不负责具体输出格式。Sink 负责消费事件并写入：

- 构建报告 JSON；
- Markdown 摘要；
- Chrome Trace Event；
- Dashboard 数据；
- RuntimeLog session；
- 测试内存记录器。

Sink 必须相互独立。Chrome Trace、Dashboard 或 AI handoff 失败不能影响主报告生成。

---

## 6. Attribute 语义

### 6.1 第一阶段只提供 Performance

```csharp
[Performance(
    Name = "Build.P4.BuildPackage",
    Category = "Build",
    SampleRate = 1.0)]
private void BuildPackage(BuildContext context)
{
}
```

初始字段：

| 字段 | 含义 |
|------|------|
| `Name` | 稳定逻辑名称；缺省时由程序集、类型和方法生成 |
| `Category` | Build、Asset、Startup 等有界类别 |
| `SampleRate` | 0-1；构建管线默认 1，Runtime 高频路径按需降低 |
| `CaptureExceptionType` | 默认 true，只记录类型，不记录敏感 message |
| `Enabled` | 允许按编译配置或 Profile 关闭 |

### 6.2 暂不作为方法 AOP 的能力

`BuildMetric` 不应伪装为普通方法 Attribute，因为 bundle 数量、DLL 大小等值通常来自方法内部。使用显式 API：

```csharp
BuildTelemetry.RecordMetric("Bundle.Count", bundleCount, "count");
BuildTelemetry.RecordMetric("Player.Size", fileSize, "bytes");
```

`Retry` 不进入通用 AOP：

- 它改变控制流；
- 依赖幂等性；
- 与事务、取消、超时和退避强相关；
- 可能重复上传、写文件或修改配置。

未来如有需求，应独立设计 Resilience Policy，只允许在显式声明幂等的异步操作上使用。

### 6.3 后续候选能力

按真实使用场景逐个评审：

- `[MainThread]`：Analyzer + Runtime guard；
- `[Trace]`：父子 Span 和链路上下文；
- `[ExceptionReport]`：只做观测，保持原异常；
- `[Performance]`：Runtime 采样和聚合。

每个新 Attribute 至少需要：

1. 三处真实消费者；
2. 明确的控制流语义；
3. 支持矩阵；
4. Analyzer 规则；
5. IL 行为测试；
6. 关闭策略和性能预算。

---

## 7. 方法支持矩阵

### 7.1 首版支持

Editor-only ILPP Spike 只支持：

- 普通同步方法；
- static 和 instance 方法；
- public/internal/private；
- void 和普通返回值；
- 正常返回和异常返回；
- 非泛型 class 中的方法。

### 7.2 首版拒绝并报告诊断

- `async` / `UniTask`；
- iterator / coroutine；
- generic method 或 generic declaring type；
- `ref` return；
- `ref struct` / byref-like 参数；
- constructor / static constructor；
- property/event accessor；
- abstract、extern、P/Invoke；
- compiler-generated method；
- lambda 和 local function；
- Unity Burst/Jobs 相关程序集。

### 7.3 后续逐项开放

每类签名独立完成 IL 设计、行为测试和性能测试后才能从拒绝列表移除。

对于 async/UniTask，必须测量异步操作的真实完成时间，而不是仅测量状态机创建时间；需要对 `MoveNext` 状态机做专门织入，不能复用同步模板。

---

## 8. 数据模型

### 8.1 AopEvent

建议字段：

| 字段 | 说明 |
|------|------|
| `SchemaVersion` | 数据协议版本 |
| `RunId` | 构建或运行会话 ID |
| `TraceId` | 调用链 ID |
| `SpanId` | 当前 Span ID |
| `ParentSpanId` | 父 Span ID |
| `MethodId` | 稳定方法标识 |
| `Name` | 逻辑名称 |
| `Category` | 业务类别 |
| `StartTimestamp` | 单调时钟起点 |
| `DurationTicks` | 单调时钟耗时 |
| `ThreadId` | 执行线程 |
| `Status` | Success、Failure、Cancelled |
| `ExceptionType` | 可选异常类型 |
| `Tags` | 有界、允许列表内的标签 |

### 8.2 时钟

耗时必须使用单调时钟，例如 `Stopwatch.GetTimestamp()`，不能使用 `DateTime.UtcNow` 相减作为精确性能数据。

UTC 时间只用于报告展示和会话关联。

### 8.3 MethodId 稳定性

MethodId 由以下规范化内容生成：

```text
assembly + declaring type + method name + generic arity + parameter type signature
```

不得包含：

- 本机绝对路径；
- 编译时随机值；
- MetadataToken 单独值；
- 会因无关源码顺序变化而改变的编号。

### 8.4 数据容量

- 构建管线默认保留完整 Span；
- Runtime 高频路径默认聚合 count/min/max/mean/p50/p95/p99；
- 原始事件采用有界队列；
- 队列满时按策略丢弃并记录 dropped count；
- `KeepLastBuildCount` 管理历史构建产物；
- 保留策略不得删除当前运行或正在写入的会话。

---

## 9. 构建管线近期接入

### 9.1 Stage 级数据

继续由 `BuildPipelineRunner.ExecuteStage()` 统一记录，不对所有 `Execute()` 重复织入。

建议把当前基于 `DateTime` 的耗时计算逐步替换为单调时钟，同时保留 UTC 展示字段。

### 9.2 内部步骤

关键步骤使用显式 Span：

```csharp
using (var span = AopSpan.Start("Build.P4.CollectAssets", "Build"))
{
    CollectAssets(context);
    span.SetTag("package", context.Profile.PackageName);
}
```

显式 Span 的价值：

- 当前即可工作；
- 调用范围清晰；
- 支持任意代码块，不局限于方法；
- 可记录内部指标；
- 不依赖 Weaver 成熟度；
- 与未来 `[Performance]` 输出同一数据模型。

### 9.3 Session 生命周期

`KJBuildPipeline.Build()` 或 Runner 外层负责：

```text
Begin session
  -> 执行 P0-P9
  -> 增量 checkpoint
  -> Flush sinks
  -> 生成 BuildReport
  -> 运行 Performance Policy
End session
```

Session 必须：

- 在异常和取消路径 Flush；
- 在 Domain Reload 前 checkpoint；
- 支持从未完成的 session 恢复或标记 Aborted；
- 不依赖静态内存保存全部状态；
- 先写临时文件，再原子替换最终报告。

---

## 10. 性能基线与 CI Policy

禁止仅用“比上一次慢 20%”作为失败条件。上一次构建可能存在冷缓存、机器负载和网络波动。

### 10.1 可比性条件

只有以下维度兼容时才允许比较：

- BuildProfile；
- 平台和架构；
- Unity 版本；
- HybridCLR/YooAsset 版本；
- 构建模式和缓存模式；
- 构建机类别；
- Stage 输入 fingerprint；
- 是否冷构建。

### 10.2 判定规则

建议组合：

- 最近 N 次成功构建的 median；
- MAD 或类似稳健离散度；
- 相对增长阈值；
- 最小绝对增长值；
- 最少样本数；
- 明确的固定预算。

示例：

```text
Warning = delta > max(baseline * 20%, 5 seconds)
Failure = fixed budget exceeded AND sample count >= required count
```

### 10.3 退出策略

- Local：只展示，不阻断；
- Development CI：Warning；
- Audit：按 Profile 决定 Warning 或 Failure；
- Formal：只对稳定且样本充足的固定预算允许 Failure。

预算检查在构建和报告完成后执行，使用独立的 `PerformancePolicyResult`，避免观测代码直接抛出业务异常。

---

## 11. HybridCLR 与程序集边界

### 11.1 永久规则

`Launcher`：

- 不引用 `Aop`；
- 不允许 `[assembly: AopEnabled]`；
- 不参与织入；
- 加入 ILPP blocklist 和架构测试。

### 11.2 Editor 阶段

首个 ILPP Spike 只处理 `Boot.Editor`，不处理 Boot/Core/General/Project/Framework Runtime 程序集。

### 11.3 Runtime 阶段

Runtime 接入前必须先决定 `Aop` 程序集属于：

- 热更新程序集；或
- 稳定 AOT 契约程序集。

不得隐式决定。若加入热更新程序集：

1. 先修改 `ProjectSettings/HybridCLRSettings.asset`；
2. 更新热更新程序集数量和边界文档；
3. 检查 `ValidateRuntimePreloadAssemblyName` blocklist；
4. 验证 BootLoader 加载顺序；
5. 执行 HybridCLR P2-P8 E2E；
6. 验证旧 Player 与新热更新 DLL 的兼容策略。

### 11.4 Runtime 发布门禁

必须验证：

```text
Unity compile
  -> ILPostProcessor
  -> HybridCLR GenerateAll
  -> MethodBridge generation
  -> hot-update DLL compile/sync
  -> AOT metadata
  -> YooAsset build
  -> IL2CPP Player
  -> Standalone/Android smoke
```

---

## 12. 织入顺序与冲突

未来多 Aspect 时必须有固定顺序，不允许依赖 Attribute 在源码中的排列顺序。

建议顺序：

```text
MainThread Guard
  -> Trace
  -> Performance
  -> Exception Observation
  -> Original Method
```

规则：

- 观测型 Aspect 不吞异常；
- 相同 Attribute 默认禁止重复；
- 冲突组合由 Analyzer 报错；
- Weaver 输出最终应用顺序到诊断报告；
- Weaver 版本和配置进入构建 fingerprint；
- 同一程序集不得被重复织入。

---

## 13. 测试与质量门禁

### 13.1 Runtime 单元测试

- Span 正常完成；
- Span 异常完成；
- 嵌套父子 Span；
- 多线程 Collector；
- no-op Collector；
- Collector 抛异常；
- 有界队列溢出；
- Tag 限制；
- Clock 可替换和确定性测试；
- Session checkpoint 和恢复。

### 13.2 Analyzer 测试

- 合法 Attribute 无诊断；
- 每类不支持签名产生稳定错误码；
- Launcher 引用产生错误；
- 未 opt-in 程序集使用 Attribute 产生错误；
- 冲突组合产生错误；
- Diagnostic ID、位置和消息稳定。

### 13.3 Weaver 测试

- 使用 Mono.Cecil 检查目标 IL；
- 检查非目标方法未修改；
- 正常返回值不变；
- 异常类型和堆栈不变；
- private/static/instance 方法行为一致；
- symbol 保留；
- 重复运行不重复织入；
- Weaver 失败给出确定性错误；
- `Launcher` 永不处理。

### 13.4 Unity 验证矩阵

- Unity Editor 编译；
- EditMode；
- PlayMode；
- Domain Reload 开启/关闭；
- Standalone Mono；
- Standalone IL2CPP；
- Android IL2CPP；
- HybridCLR hot-update；
- Development/Audit/Formal Profile。

### 13.5 性能预算

- 功能关闭且未织入时：零方法级运行开销；
- 已织入但 Collector 未安装时：无分配、固定低开销；
- 构建管线开启完整采集：不影响业务结果；
- Runtime 热路径：零 GC，开销通过基准测试确定后写入预算；
- Sink I/O 不在被观测热路径同步阻塞。

---

## 14. 分阶段路线

### Phase 0：契约冻结与 Spike 设计

目标：在写 Weaver 前关闭架构歧义。

- 定义 `AopEvent` schema v1；
- 定义 `PerformanceAttribute`；
- 定义首版方法支持矩阵；
- 定义 assembly allowlist/blocklist；
- 确定 MethodId 算法；
- 定义 Collector 故障隔离；
- 建立测试目录和 Diagnostic ID 规则。

验收：设计评审通过，明确不支持项，不承诺 async/Runtime。

### Phase 1：构建管线显式 Observability

目标：不依赖 ILPP，产生第一份有价值的内部性能报告。

- [x] 创建 `Aop.asmdef`；
- [x] 实现 Clock、Span、Collector、Event；
- [x] 由 `BuildPipelineRunner` 管理 telemetry session；
- [x] 在 P2/P3/P4/P6 添加关键显式 Span；
- [ ] 记录 bundle/DLL/APK 等显式 Metric；
- [x] 通过 `build_report.json` 和 `build_report.md` 输出；
- [x] 异常路径记录 Failure，Collector 失败与业务构建隔离；
- [ ] 完整 P0-P9 构建验证并审阅真实耗时数据。

当前验证：Unity batchmode 编译通过；`Boot.Editor.Build.Tests` 14/14 全绿，其中 5 个新增用例覆盖单调耗时/父子 Span、异常脱敏、Collector 故障隔离、容量丢弃和报告 JSON 序列化。最终验收仍需一次真实构建可定位关键内部耗时。

### Phase 2：Editor-only ILPostProcessor Spike

目标：验证透明织入的真实可行性。

- 创建 `Aop.Editor.asmdef`；
- 实现 `WillProcess`；
- 仅 allowlist `Boot.Editor`；
- 只支持同步普通方法；
- 选择 2-3 个私有方法试点；
- 输出 Weaver 诊断报告；
- 完成 IL 和行为测试。

验收：添加/删除 `[Performance]` 可稳定启停织入，调用点无需修改，异常语义不变。

### Phase 3：Analyzer 与工程化

目标：让错误在编译期可理解、可定位。

- 创建独立 Analyzer 工程；
- 构建并导入 `RoslynAnalyzer` DLL；
- 支持矩阵诊断；
- Attribute 冲突诊断；
- Launcher/blocklist 架构诊断；
- Analyzer CI 和 golden tests；
- Weaver 版本进入 fingerprint。

验收：所有不支持用法产生稳定 Diagnostic ID，无静默降级。

### Phase 4：报告、Trace 与基线

目标：把数据转化为工程决策。

- Chrome Trace Event 输出；
- BuildReport 性能章节；
- Dashboard 性能视图；
- AI handoff 回归摘要；
- 兼容性分组的历史基线；
- median/MAD/绝对阈值策略；
- Local/CI/Formal 分级策略。

验收：回归判定可解释，不因单次噪声轻易失败。

### Phase 5：复杂方法语义

目标：逐项扩大支持面。

按独立子阶段处理：

1. generic type/method；
2. inheritance/interface；
3. async Task；
4. UniTask；
5. iterator/coroutine；
6. struct/byref 场景。

每项必须独立通过 IL、行为、异常和性能测试，未通过的继续由 Analyzer 拒绝。

### Phase 6：Runtime 与 HybridCLR

目标：只在真实 Runtime 需求存在时开放。

- 选择一个低频真实消费者；
- 确定 Aop 程序集热更新归属；
- 更新 HybridCLRSettings 和边界文档；
- 完整 P2-P8 验证；
- Standalone/Android smoke；
- 验证裁剪、AOT metadata 和 MethodBridge；
- 设置 Runtime 采样与聚合默认值。

验收：Editor、Player、HybridCLR 行为一致，Launcher 边界不变。

### Phase 7：第二类 Aspect

优先评估 `[MainThread]` 或 `[Trace]`，不默认选择 Retry。

验收：证明 Handler/Weaver 扩展机制无需修改核心控制流，并完成 Aspect 顺序和冲突测试。

---

## 15. 文件变更规划

### Phase 1 新建

| 文件/目录 | 说明 |
|-----------|------|
| `Assets/Framework/Aop/Aop.asmdef` | Runtime 契约与采集核心 |
| `Assets/Framework/Aop/Attributes/` | Attribute 契约 |
| `Assets/Framework/Aop/Runtime/` | Span、Clock、Collector、Session |
| `Assets/Framework/Aop/Model/` | 版本化事件模型 |
| `Assets/Scripts/Boot.Editor/Build/Telemetry/` | 构建管线桥接和 Sink |
| `Assets/Tests/EditMode/Aop/` | AOP Runtime 测试 |

### Phase 1 修改

| 文件 | 修改 |
|------|------|
| `Boot.Editor.asmdef` | 引用 `Aop` |
| `BuildPipelineRunner.cs` | Session 生命周期、单调时钟、报告接入 |
| P2/P3/P4/P6 Stage | 添加少量关键显式 Span 和 Metric |
| Build report writer | 输出性能摘要和明细 |

### Phase 2-3 新建

| 文件/目录 | 说明 |
|-----------|------|
| `Assets/Framework/Aop.Editor/Aop.Editor.asmdef` | ILPP Editor 程序集 |
| `Assets/Framework/Aop.Editor/Weaving/` | Weaver 实现 |
| `Assets/Framework/Aop.Editor/Diagnostics/` | Weaver 诊断 |
| `Tools/Aop.Analyzers/` | Analyzer 独立工程和测试 |
| `Assets/Framework/Aop.Analyzers/` | 预编译 Analyzer 产物 |

### 永久不修改

- `Assets/Scripts/Boot/Launcher/` 不添加 Aop 引用；
- Launcher asmdef 不添加 Framework 引用；
- Framework.Aop 不引用 Core/General/Project/Boot；
- 第三方程序集不参与织入。

---

## 16. 文档同步要求

修改 AOP 或 Runtime 热更新边界时同步：

- `.planning/STATE.md`；
- `.planning/ROADMAP.md`；
- `.planning/HOT_UPDATE_BOUNDARY.md`；
- `ProgressDoc/Result/hybridclr_workflow.md`；
- 构建管线需求与设计文档；
- 对应的本地 skill 文档。

Phase 1 只涉及 Build Pipeline 时，也要更新构建报告 schema 和 Dashboard/CI 使用说明。

---

## 17. 风险与应对

| 风险 | 应对 |
|------|------|
| ILPP 破坏控制流或异常语义 | 首版只支持同步普通方法，Cecil IL 检查 + 行为测试 |
| Unity/Mono.Cecil API 版本差异 | 固定 Unity 2022.3.62f2 验证，封装 Cecil adapter |
| HybridCLR 链路不兼容 | Runtime 延后，完整 P2-P8 门禁，不以 Editor 成功代替 Player 验证 |
| 重复织入 | assembly/method marker + Weaver version + 幂等测试 |
| 观测代码影响构建 | Collector/Sink 故障隔离，异步或批量输出，有界队列 |
| 性能报告噪声过大 | 兼容性分组、多样本稳健统计、绝对阈值 |
| 数据泄露 | 默认不采参数/返回值，路径归一化，Tag allowlist |
| Attribute 膨胀 | 三处真实消费者 + 完整语义/测试门禁 |
| Domain Reload 丢数据 | 增量 checkpoint、Aborted session、原子报告写入 |
| Launcher 边界被破坏 | ILPP blocklist + asmdef 架构测试 + Analyzer 错误 |

---

## 18. 最终验收标准

Framework.Aop 被视为工业级前，必须同时满足：

1. 当前构建管线能稳定产出方法/步骤级性能数据；
2. 数据 schema 有版本且向后兼容策略明确；
3. 观测失败不改变业务结果；
4. 所有不支持的方法签名都有编译诊断；
5. ILPP 只处理显式 opt-in 程序集；
6. Launcher 永不引用、永不织入；
7. 异常、返回值、取消和 async 语义通过测试；
8. 关闭时零织入开销，开启时满足明确性能预算；
9. Dashboard、CI 和 AI handoff 使用同一结构化数据源；
10. Runtime 发布通过 Unity + IL2CPP + Android + HybridCLR 全链路验证；
11. 文档、测试和 hot-update 边界同步更新；
12. 任一新 Aspect 都能在不修改核心控制流的前提下扩展，并具有独立诊断和测试。

---

## 19. 结论

KJ 的 AOP 建设顺序应当是：

```text
可靠的数据模型
  -> 显式 Observability
  -> Editor-only ILPP
  -> Analyzer 和工程门禁
  -> 报告与性能基线
  -> 复杂方法语义
  -> Runtime/HybridCLR
  -> 新 Aspect
```

Attribute 是使用契约，ILPostProcessor 是透明织入后端，Collector/Sink 是数据消费后端。三者独立演进，但必须共享同一语义、同一事件模型和同一质量门禁。

这条路线既能解决当前构建管线的实际问题，也为未来 Runtime 性能追踪、主线程契约、调用链和更多横切能力保留足够空间，同时避免在技术前提尚未成立时过早承诺“只加 Attribute 即自动生效”。

---

*Plan rewritten: 2026-07-17 | Phase 1 prototype updated: 2026-07-19*
