# KJ 构建打包全流程管线 — 需求分析与设计文档

> 文档前缀 `Hy3_`：由本 agent（Hy3）基于项目真实代码产出，便于在 `ProgressDoc/Discuss/` 追溯。
> 关联：`.planning/STATE.md`（运行验证 gate）、`AGENTS.md` / `CLAUDE.md`（架构边界）、`KJHybridClrBuildTools.cs`（现有构建工具）。
> 目标：在动手写代码前，先就"为什么做 / 做什么 / 怎么做 / 验证什么"达成一致。

---

## 1. 背景与目标

KJ 是一个 Unity 2022.3.62f2 客户端框架，采用 **HybridCLR（热更新）+ YooAsset（资源）+ VContainer/MessagePipe** 技术栈。HYB-03 已将启动链裂变为 AOT 壳 `Launcher` + 热更 `Boot`，共 10 个热更程序集。

当前的核心诉求是 **"打包验证"** —— 即产出一份可在目标平台运行的 Player，并验证其能走完 Boot → 资源更新 → 热更加载 → `ProjectStartup` 的完整链路。但现状是：**现有工具只覆盖了 Editor 内循环，没有任何一条能产出"可运行的 Player 包"的链路**。因此必须先补上"构建打包全流程管线"，打包验证才有载体。

**本管线的目标（可量化）：**
1. 一条命令（或一次 CI 调用）产出 **目标平台的 IL2CPP Player + 对应的 YooAsset 正式资源包**。
2. 流程 **可重复、可中断续跑、失败早退、产物可校验**。
3. 产出 **机器可读的构建报告（JSON）+ 人读摘要**，并支持 **无头运行 + 启动日志抓取** 做真正的"启动冒烟"。
4. **不改坏现有开发内循环**（`KJ/HybridCLR/*` 菜单继续可用）。

---

## 2. 现状盘点（基于真实代码）

### 2.1 已有的能力（`Assets/Scripts/Boot.Editor/HybridCLR/KJHybridClrBuildTools.cs`）

该文件提供一批 **离散的 Editor 菜单项**，本质是"开发内循环"工具，关键链路如下：

| 菜单项 | 做的事 | 产出 |
| --- | --- | --- |
| `Install HybridCLR Runtime` | 安装/校验 HybridCLR 运行时 | `HybridCLR` 运行时就位 |
| `Generate All And Sync` | 安装运行时 → 确保 Boot 场景在 BuildSettings → `PrebuildCommand.GenerateAll()` → `SyncExistingOutputs()` | 编译 DLL + 同步进 `Assets/GameRes/HotUpdate/Dlls` 与 `/AotMetadata` |
| `Compile Dlls And Sync` | `CompileDllCommand.CompileDll` → `SyncExistingOutputs` | 同上（仅重编译） |
| `Generate Runtime Assets And Sync` | 安装运行时 → `CompileDll` → `GenerateAotMetadataIfNeeded`（strip AOT）→ 同步 | 热更 DLL + AOT metadata DLL 同步进资产目录 |
| `Prepare YooAsset Editor Simulate Package` | `EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualRawBundle)` → 写 `AssetConfig.EditorSimulatePackageRoot` | **仅 EditorSimulate 包**（供 Editor Play Mode 用） |
| `Apply To Open Entry` | 把 `hotUpdateAssemblies` / `aotMetadataAssemblies` 条目写进 `Entry.startupSettings` | BootStartupSettings 序列化数据 |
| `Prepare Boot Scene` | 打开 Boot 场景 → Apply → 确保进 BuildSettings → 保存 | 场景就位 |
| `Validate Outputs` | 检查同步的 DLL 都在、无多余 DLL | 静态校验通过/抛异常 |

**关键事实（来自代码）：**
- 热更 DLL 源：`HybridCLRData/HotUpdateDlls/<target>/`（10 个热更程序集）。
- AOT metadata 源：`HybridCLRData/AssembliesPostIl2CppStrip/<target>/`（`patchAOTAssemblies`: `mscorlib, System, System.Core`）。
- 同步落点：`Assets/GameRes/HotUpdate/Dlls/*.dll.bytes`、`Assets/GameRes/HotUpdate/AotMetadata/*.dll.bytes`。
- YooAsset 收集器：包 `DefaultPackage`（来自 `AssetConfig.PackageName`）、组 `HotUpdate`（tag `hotupdate`）、两个 RawFile 收集器指向上面两个目录。
- 拦截名单 `ValidateRuntimePreloadAssemblyName`：拒绝 `Launcher` / `TestKit` 作为热更发布（符合 HYB-03 边界）。
- 运行时加载：`BootLoader` 用 `package.LoadAssetSync<RawFileObject>(assetPath)` 从 YooAsset 包加载热更 DLL（**所以 DLL 必须打进正式 YooAsset 包**）。

### 2.2 缺口（Gap）—— 这正是本管线要补的

1. **缺 YooAsset 正式包构建**：现有 `Prepare YooAsset Editor Simulate Package` 只产出 `Simulate` 包（`Bundles/Android/DefaultPackage/Simulate/` 已存在，是模拟产物，**不是 Player 可加载的真包**）。没有任何代码调用 YooAsset 的生产构建（Builtin/Scriptable Build Pipeline）产出 `Bundles/<Platform>/DefaultPackage/` 真包。
2. **缺 `BuildPlayer` 调用**：全工程搜索 `BuildPipeline` / `BuildPlayer` = 0 处。没有任何脚本驱动的 Unity Player 构建。
3. **缺 IL2CPP 后端强制**：`ProjectSettings.asset` 中 `scriptingBackend` 为空（取平台默认）；HybridCLR 在 Player 端必须 IL2CPP。管线必须显式 `PlayerSettings.SetScriptingBackend(IL2CPP)`。
4. **缺产物校验**：`ValidateOutputs` 只校验"同步的 DLL 文件存在"，不校验 Player 本体、不校验真资源包、不校验包内确实含 DLL、不校验 AOT metadata 齐全。
5. **缺运行期冒烟**：没有任何"构建后真正跑起来、抓启动日志判定成功"的机制——这是 STATE.md 里反复提到的"运行验证 gate"一直没走完的根因。
6. **缺单一编排入口 / CI 入口**：现有都是散装的 MenuItem，没有 `Build(target, config)` 总入口，也没有 `-executeMethod` 无头入口。

> 结论：开发内循环（Editor Play）是通的；**但"出真包 + 出真包后真跑"彻底没有**。本管线 = 把现有散装步骤按正确顺序串成一条可重复的"真包"链路，并补齐"构建 + 运行 + 校验"。

---

## 3. 范围

### 3.1 做（In Scope）
- 新增 **单一编排器** `KJBuildPipeline`（Editor 程序集），按正确顺序串联：HybridCLR 生成 → 编译 DLL → 同步 → YooAsset 正式包 → 写 Entry 配置 → BuildPlayer(IL2CPP) → 产物校验 → 无头运行冒烟 → 报告。
- **配置驱动** 的 `BuildConfig`（平台 / 后端 / development / 输出目录 / 包名 / CDN / 版本）。
- **可中断续跑**（按 stage 重入）。
- **机器可读报告 + 人读摘要**。
- **离线冒烟**优先：先用 builtin 文件系统从 StreamingAssets 加载，不依赖 CDN。

### 3.2 不做（Out of Scope，v1）
- CDN 发布 / 版本服务器推送（仅预留 `cdnBaseUrl` 字段，不实现上传）。
- Android/iOS 签名与商店发布（仅保证能出未签名包并本地安装运行）。
- 资源内容本身的构建优化（图集/分帧/依赖裁剪归 YooAsset 既有配置，不在本管线重做）。
- 热更差分 / 增量更新发布（v1 全量包即可）。

---

## 4. 总体架构

管线是一个 **有向、可续跑、校验前置** 的阶段流。每个 stage 满足：输入来自上游产物或配置；输出落到确定目录；结束前做本 stage 不变量校验；失败立即终止并写入报告。

```
                        ┌─────────────────────────────┐
                        │   BuildConfig (资产/JSON)    │
                        │ 平台/后端/dev/输出目录/CDN/版本│
                        └──────────────┬──────────────┘
                                       │
  Stage 0  预检 Guard ─────────────────┤  HC运行时匹配? 平台匹配? Boot场景? 失败早退
                                       ▼
  Stage 1  HybridCLR 生成 (PrebuildCommand.GenerateAll)
           → link.xml / AOTGenericReferences / 编译骨架
                                       ▼
  Stage 2  编译热更 + AOT metadata DLL
           CompileDll + GenerateStripedAOTDlls → HybridCLRData/...
                                       ▼
  Stage 3  同步 DLL 进 YooAsset 源目录
           SyncExistingOutputs → Assets/GameRes/HotUpdate/{Dlls,AotMetadata}
                                       ▼
  Stage 4  YooAsset 正式包构建 (Builtin Build Pipeline)
           → Bundles/<Platform>/DefaultPackage/{version,hash,bundles含hotupdate}
                                       ▼
  Stage 5  写 Entry 启动配置 (ApplyToOpenEntry)
           hotUpdateAssemblies / aotMetadataAssemblies / streamingAssetsRoot
                                       ▼
  Stage 6  Unity Player 构建 (IL2CPP, BuildPlayer)
           → Build/<Platform>/KJ.{apk|exe|ipa}
                                       ▼
  Stage 7  产物静态校验 (BuildReport 写入)
           本体非空 / 包含hotupdate DLL / AOT metadata 3 件套齐全 / 清单一致
                                       ▼
  Stage 8  无头运行冒烟 (离线优先)
           adb/exec 启动 → 抓 Logs/Runtime/latest.jsonl → 判定 Boot 到 ProjectStartup 成功
                                       ▼
  Stage 9  归档 + 报告 (JSON + 人读)  ──→  产出可验证的 Player 包
```

> 排序硬约束（错序必炸）：**Stage 2（编译）→ Stage 3（同步）→ Stage 4（打正式包）→ Stage 6（BuildPlayer）**。
> 因为 DLL 必须先编译、先同步进源目录、先被打进 YooAsset 真包，BuildPlayer 才会把它们封进 StreamingAssets；反序则包里是旧 DLL 或空包。

---

## 5. 逐阶段详细设计

### Stage 0 — 预检 Guard（`PreFlightCheck`）
- 校验 HybridCLR 运行时已安装且 `PackageVersion == InstalledLibil2cppVersion`（复用 `InstallerController` 的判定逻辑）。
- 校验 `EditorUserBuildSettings.activeBuildTarget` 等于 `BuildConfig.Platform`；不等则**报错退出**（不擅自切换，避免污染工程状态）。
- 校验 `Assets/GameRes/Scene/Boot/Main.unity` 存在且已在 `EditorBuildSettings`。
- 校验 `AssetConfig`（DefaultPackage）存在；不存在则报错（不自动造，避免配置漂移）。
- 输出：`PreFlightResult { ok, errors[] }`，任一 error → 终止并写报告。

### Stage 1 — HybridCLR 生成（`GenerateAll`）
- 调用 `PrebuildCommand.GenerateAll()`（封装了 CompileDll + StripAOTDll + link.xml + AOTGenericReferences）。
- 目的：生成 AOT 裁剪所需的 link.xml、AOT 泛型桥接引用，确保 IL2CPP 裁剪后热更能补充元数据。
- 不变量：`HybridCLRGenerate/link.xml` 存在且非空。

### Stage 2 — 编译热更 + AOT metadata DLL（`Compile`）
- `CompileDllCommand.CompileDll(target, development)` → `HybridCLRData/HotUpdateDlls/<target>/`（10 个热更 dll）。
- `GenerateAotMetadataIfNeeded(target)`（即 `StripAOTDllCommand.GenerateStripedAOTDlls`）→ `HybridCLRData/AssembliesPostIl2CppStrip/<target>/`（mscorlib/System/System.Core）。
- **幂等清理**：先清空两目录旧产物再编译，避免"改了代码但旧 DLL 还在"导致热更不生效（这是经典坑）。
- 不变量：10 个热更 dll 全部存在；3 个 AOT metadata dll 全部存在。

### Stage 3 — 同步 DLL 进 YooAsset 源目录（`Sync`）
- 复用现有 `SyncExistingOutputs` 的 `CopyHotUpdateAssemblies` / `CopyAotMetadataAssemblies`：
  - `HybridCLRData/HotUpdateDlls/<target>/*.dll` → `Assets/GameRes/HotUpdate/Dlls/*.dll.bytes`
  - `HybridCLRData/AssembliesPostIl2CppStrip/<target>/*.dll` → `Assets/GameRes/HotUpdate/AotMetadata/*.dll.bytes`
  - 调用 `CleanObsoleteSyncedFiles` 删除过期 `.dll.bytes`（防程序集改名后残留旧文件）。
- `AssetDatabase.Refresh()` + 确保 `DefaultPackage/HotUpdate` 收集器就位（复用 `EnsureYooAssetCollector`）。
- 不变量：源与目标文件数、文件名一致；无 `ValidateRuntimePreloadAssemblyName` 拦截名混入。

### Stage 4 — YooAsset 正式包构建（`BuildYooAsset`）★ 新增核心
- 复用现有收集器配置（`DefaultPackage` 包 + `HotUpdate` 组，已含 RawFile 收集器指向 `Dlls`/`AotMetadata`）。
- 调用 YooAsset **生产构建**（与现有 `EditorSimulateBuildInvoker.Build` 同源，但走 Builtin/Scriptable Build Pipeline 而非 VirtualRawBundle 模拟）：
  - BuildTarget = `BuildConfig.Platform`
  - PackageName = `DefaultPackage`
  - 输出根 = `Bundles/<Platform>/DefaultPackage/`
  - 文件系统 = **Builtin**（使 Player 从 StreamingAssets 加载，满足离线冒烟）
  - 包含 `hotupdate` tag 的 `HotUpdate` 组（即 DLL + AOT metadata 的 rawfile bundle）
- **注意**：正式构建会把 `Assets/GameRes/HotUpdate/*` 作为资源打进 bundle；因此 Stage 3 必须在 Stage 4 之前。
- 不变量：`Bundles/<Platform>/DefaultPackage/DefaultPackage.version` 与 `_hash` 存在；`hotupdate` bundle 文件存在且体积 > 0；包内 `Dlls/` 含 10 个 `.dll.bytes`、`AotMetadata/` 含 3 个（用 YooAsset 清单或解包校验）。

### Stage 5 — 写 Entry 启动配置（`ApplyConfig`）
- 复用 `ApplyToOpenEntry`：把 `hotUpdateAssemblies`/`aotMetadataAssemblies` 写进 `Entry.startupSettings`，设置 `assetDownloadTag = "hotupdate"`、`startupTypeName = "Project.Bootstrap.ProjectStartup, Project"`、`startupMethodName = "Start"`。
- **新增**：根据 `BuildConfig` 设置 `streamingAssetsRoot`（离线冒烟走 builtin，故指向 local StreamingAssets）、`skipHotUpdateInEditor = false`（Player 不跳过）。
- 保存场景与资产（`PrepareBootScene` 逻辑）。

### Stage 6 — Unity Player 构建（`BuildPlayer`）★ 新增核心
- 强制 IL2CPP：`PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.IL2CPP)`（HybridCLR 在 Player 必须 IL2CPP）。
- `BuildPlayerOptions`：
  - `scenes` = `[Boot/Main.unity, ...]`（从 `EditorBuildSettings.scenes` 取，Boot 为首）。
  - `locationPathName` = `Build/<Platform>/KJ.<ext>`（Android=.apk, StandaloneWin=.exe, iOS=.ipa）。
  - `target` = `BuildConfig.Platform`。
  - `options` = `development ? BuildOptions.Development : BuildOptions.None`（冒烟用 Development 便于抓日志与调试；发布用 Release）。
  - `extraScriptingDefines` 可加 `KJ_BUILD_PIPELINE` 供条件编译。
- 调用 `BuildPipeline.BuildPlayer(opts)`；`BuildReport.summary.result != Success` → 终止。
- 不变量：输出文件存在且 size > 0；IL2CPP 后端生效（读 `PlayerSettings.GetScriptingBackend` 复核）。

### Stage 7 — 产物静态校验（`ValidateArtifacts`）
- Player 本体：`Build/<Platform>/KJ.<ext>` 存在且非空。
- YooAsset 包：`version`/`_hash` 存在；`hotupdate` bundle 含 10+3 个 `.dll.bytes`。
- HybridCLR 目录：与 Stage 2 同源，复核 10+3 齐全（作为物料清单留档）。
- 一致性：`Entry.startupSettings.hotUpdateAssemblies` 的 10 个名 = `HybridCLRSettings.hotUpdateAssemblies` 的 10 个名（防配置漂移）。
- 全部写入 `BuildReport.artifacts`。

### Stage 8 — 无头运行冒烟（`SmokeRun`）★ 验证 gate 的核心
- **优先离线**：Player 用 builtin 文件系统从 StreamingAssets 读 `DefaultPackage`，不访问 CDN。
- 启动方式：
  - **Standalone (Win) 首选**：`Build/<Platform>/KJ.exe -batchmode -nographics`；重定向 `Logs/Runtime/latest.jsonl`（或 stdout）抓取。
  - **Android**：`adb install` → `adb shell am start` → `adb logcat`（过滤 KJ/Boot 标签）→ 抓取 `latest.jsonl` 落地（若运行时写入外部存储）。
- 判定：解析启动日志，确认出现 `Boot` 阶段完成、`ProjectStartup.Start` 被调用、`ProjectLifetimeScope` 创建成功等关键里程碑（建议在 `BootStartupLog`/RuntimeLog 打可识别的成功标记）。
- 超时（如 120s）未达标 → 冒烟失败，保留日志供排查。
- 不变量：`latest.jsonl` 含成功里程碑且无非预期异常。

### Stage 9 — 归档 + 报告（`Report`）
- 汇总 `BuildReport`：各 stage 状态、耗时、产物路径与大小、SHA256、冒烟结论。
- 输出：
  - 机器可读：`Build/<Platform>/build_report.json`
  - 人读摘要：`Build/<Platform>/build_report.md`
- 可选：把 `Build/<Platform>/` + `Bundles/<Platform>/` 打成 `KJ_<Platform>_<version>.zip` 归档。

---

## 6. 关键决策与权衡（推荐）

1. **单一编排器 `KJBuildPipeline.Build(BuildConfig)`，而非再加一堆散装 MenuItem。**
   现有菜单保留为"开发内循环"，新增一个总入口 `KJ/Build/Full Player Build & Validate` + 一个无头入口 `KJ.Build.KJBuildPipeline.BuildFromCommandLine`（供 CI `-executeMethod`）。总入口内部按 Stage 顺序调用现有方法 + 新增的 Stage 4/6/8。

2. **推荐先以 Windows Standalone (x64) IL2CPP 打通端到端，再上 Android。**
   理由：Standalone 不需要 Android SDK/NDK/JDK，能在无头 `-batchmode` 下直接跑、直接抓日志，**最快验证"编译→同步→打正式包→BuildPlayer→启动冒烟"全链路是否成立**。Android 只是把 Stage 6 的 target 换成 Android + 增加 `adb` 步骤，架构不变。避免在没验证链路前就被 Android 工具链卡住。

3. **IL2CPP 强制。**
   HybridCLR 的 AOT 补充元数据 + 解释执行在 Player 端依赖 IL2CPP；Mono 后端不支持运行时补充元数据。管线在 Stage 6 显式 `SetScriptingBackend(IL2CPP)` 并复核。

4. **配置驱动 + 可续跑。**
   `BuildConfig` 持有所有可变项；每个 Stage 写"完成标记"（如 `Build/<Platform>/.stageN.done` + 产物指纹），重跑时跳过已完成且指纹未变的 Stage，避免每次都重编 IL2CPP（耗时大户）。

5. **校验前置、失败早退。**
   每 Stage 结束即做不变量校验；Stage 0 预检把 90% 的低级错误（运行时没装、平台不对、场景缺失）挡在编译之前，省时间。

6. **DLL 必须打进正式 YooAsset 包（不是另放 StreamingAssets 根）。**
   因为 `BootLoader` 用 `package.LoadAssetSync<RawFileObject>` 从 YooAsset 包加载；若 DLL 放在包外，启动链读不到。Stage 4 的正式包必须包含 `hotupdate` 组。

7. **离线冒烟优先。**
   先验证"包能自洽启动"，再谈 CDN 分发。Stage 8 默认走 builtin 文件系统，把 `streamingAssetsRoot` 指向本地资源，不依赖网络。

---

## 7. 配置模型（`BuildConfig`）

建议用一个 `ScriptableObject`（`Assets/Scripts/Boot.Editor/Build/BuildConfig.asset`）或同构 JSON，字段：

| 字段 | 类型 | 说明 | 默认 |
| --- | --- | --- | --- |
| `platform` | enum | StandaloneWindows / Android / iOS | StandaloneWindows |
| `scriptingBackend` | enum | 固定 IL2CPP | IL2CPP |
| `development` | bool | 冒烟=true（带符号/日志）；发布=false | true |
| `outputDir` | string | `Build/<Platform>` | 自动推导 |
| `packageName` | string | YooAsset 包名 | 取 AssetConfig（DefaultPackage） |
| `assetDownloadTag` | string | hotupdate | "hotupdate" |
| `cdnBaseUrl` | string | 真分发用；冒烟置空走 builtin | "" |
| `streamingAssetsRoot` | string | 离线走 StreamingAssets | "HotUpdate"（本地兜底根） |
| `version` | string | 产物版本号 | 取自 `Application.version` 或 pipeline 派生 |
| `smokeEnabled` | bool | 是否跑 Stage 8 | true |
| `smokeTimeoutSec` | int | 冒烟超时 | 120 |

---

## 8. 验证策略（对应 STATE.md 的"运行验证 gate"）

分三层，层层递进：

- **L1 静态产物校验（Stage 7）**：Player 本体非空、YooAsset 真包含 10+3 个 DLL、配置一致。低成本、必做。
- **L2 无头运行冒烟（Stage 8）**：真正启动 Player，抓 `latest.jsonl` 判定走完 Boot→ProjectStartup。这是"打包验证"的本体。
- **L3 资源加载矩阵（后续）**：在冒烟成功基础上，补充对 RawFile/cached-owned/场景/下载器的自动化 PlayMode/Player 覆盖（STATE 提到的资源加载矩阵）。v1 先不做，但管线要预留钩子（冒烟后可扩展为跑一组启动后自检用例）。

报告格式：`build_report.json` 含 `stages[]`（name/status/durationSec）、`artifacts[]`（path/size/hash）、`smoke{passed, milestones[], logPath}`、`summary{passed, failedStage}`。

---

## 9. 风险与缓解

| 风险 | 影响 | 缓解 |
| --- | --- | --- |
| IL2CPP + HybridCLR strip 后热更元数据不全 | 运行时 `LoadMetadataForAOTAssembly` 失败、崩溃 | Stage 1 `GenerateAll` 保证 link.xml/AOTGenericReferences；Stage 7 校验 3 件套齐全；Stage 8 抓日志 |
| 旧 DLL 残留（改名/删程序集） | 热更加载到旧代码 | Stage 2/3 先清空再编译+同步；`CleanObsoleteSyncedFiles` 删过期 |
| YooAsset 正式包 API 与现有 `EditorSimulateBuildInvoker` 差异 | Stage 4 编译不过 | 实现前先对照工程内 YooAsset 版本的 `BuildParameters`/`AssetBundleBuilder` 校验 API；可复用同一收集器配置降低风险 |
| Android SDK/NDK/JDK 缺失 | Stage 6 直接失败 | 先 Standalone 打通；Android 单独前置检查（sdk.dir / ndk / jdk） |
| 启动日志里程碑不可识别 | Stage 8 误判 | 在 `BootStartupLog`/RuntimeLog 增加明确的成功里程碑标记（如 `[BOOT_OK]`/`[PROJECTSTARTUP_OK]`） |
| 构建耗时长（IL2CPP 慢） | 迭代效率低 | Stage 续跑 + 仅增量变更重编；development 包先验证链路，Release 最后出 |
| 现有菜单被破坏 | 开发体验回退 | 新增 `KJ.Build` 程序集与总入口，**不删除/不改现有菜单语义**；总入口复用现有方法而非复制 |

---

## 10. 实施里程碑（建议拆分）

- **M0 脚手架**：新增 `KJ.Build` Editor 程序集 + `BuildConfig` + `KJBuildPipeline` 骨架（空 Stage + 报告结构）。不动现有代码。
- **M1 串起已有步骤**：总入口调用 Stage 0/1/2/3/5（全为现有方法），产出"同步好的 DLL + 配置"，先不 BuildPlayer。用手工 BuildPlayer 验证 DLL 进了包。
- **M2 Stage 4（YooAsset 正式包）**：实现生产构建调用，校验 `Bundles/<Platform>/DefaultPackage/` 真包含 hotupdate DLL。
- **M3 Stage 6（BuildPlayer IL2CPP）**：Standalone Win IL2CPP 出 exe；静态校验（L1）。
- **M4 Stage 8（无头冒烟）**：Standalone `-batchmode` 启动 + 抓 `latest.jsonl` 判定；打通端到端。
- **M5 Android 扩展**：target=Android，加 `adb` 步骤与 SDK/NDK 预检。
- **M6 报告/归档/CI 入口**：`build_report.json/.md`、zip 归档、`-executeMethod` 无头入口、续跑标记。

---

## 11. 与现有工具的关系

- **保留**：`KJ/HybridCLR/*` 全部菜单（开发内循环：改代码 → `Generate Runtime Assets And Sync` → `Prepare YooAsset Editor Simulate Package` → Editor Play）。
- **新增**：`KJ/Build/Full Player Build & Validate`（总入口）+ `KJ.Build.KJBuildPipeline.BuildFromCommandLine`（CI）。
- **复用**：`PrebuildCommand.GenerateAll`、`CompileDllCommand.CompileDll`、`StripAOTDllCommand.GenerateStripedAOTDlls`、`SyncExistingOutputs`、`EnsureYooAssetCollector`、`ApplyToOpenEntry`、`PrepareBootScene`、`ValidateOutputs`、`InstallerController` 判定——均为现有方法，不重复造。
- **新增方法**：`BuildYooAsset`（Stage 4）、`BuildPlayer`（Stage 6）、`SmokeRun`（Stage 8）、`WriteReport`（Stage 9）、`PreFlightCheck`（Stage 0）。

---

## 12. 验收标准（Definition of Done）

1. 一条命令（或一次 `-executeMethod`）可在 **Standalone Win IL2CPP** 产出 `Build/StandaloneWindows/KJ.exe` + `Bundles/StandaloneWindows/DefaultPackage/` 真包。
2. 该 Player **离线** 启动后能走完 Boot → 资源/热更加载 → `ProjectStartup.Start` → `ProjectLifetimeScope` 创建（Stage 8 冒烟通过，`latest.jsonl` 含成功里程碑）。
3. `build_report.json` 记录各 Stage 状态、产物大小/哈希、冒烟结论；任一 Stage 失败则报告明确标红且无半成品被当作成功。
4. 重跑时未变更 Stage 可跳过（续跑生效），不重复 IL2CPP 全量编译。
5. 现有 `KJ/HybridCLR/*` 菜单功能不受影响。
6. Android 目标可在 SDK/NDK 就绪后复用同一管线出包（架构一致，仅 target 与 `adb` 步骤不同）。

---

## 附录 A — 推荐入口签名（伪代码）

```csharp
namespace KJ.Build
{
    public static class KJBuildPipeline
    {
        // 编辑器菜单 / CI 共用
        public static BuildReport Build(BuildConfig config);

        // 无头 CI: -executeMethod KJ.Build.KJBuildPipeline.BuildFromCommandLine
        public static void BuildFromCommandLine();

        // 各 Stage（内部）
        static PreFlightResult        PreFlightCheck(BuildConfig c);
        static void                   StageGenerateAll(BuildConfig c);
        static void                   StageCompile(BuildConfig c);
        static void                   StageSync(BuildConfig c);
        static void                   StageBuildYooAsset(BuildConfig c);   // 新增
        static void                   StageApplyConfig(BuildConfig c);
        static void                   StageBuildPlayer(BuildConfig c);     // 新增
        static BuildReport            StageValidateArtifacts(BuildConfig c);
        static SmokeResult            StageSmokeRun(BuildConfig c);        // 新增
        static BuildReport            WriteReport(BuildConfig c, ...);     // 新增
    }
}
```

## 附录 B — 关键路径速查（来自本次代码核查）

- 热更 DLL 源：`HybridCLRData/HotUpdateDlls/<target>/`（10 个）
- AOT metadata 源：`HybridCLRData/AssembliesPostIl2CppStrip/<target>/`（mscorlib/System/System.Core）
- 同步落点：`Assets/GameRes/HotUpdate/Dlls/*.dll.bytes`、`Assets/GameRes/HotUpdate/AotMetadata/*.dll.bytes`
- YooAsset 包/组：`DefaultPackage` / `HotUpdate`（tag `hotupdate`，RawFile 收集器）
- 现有模拟包产物（**非真包**）：`Bundles/Android/DefaultPackage/Simulate/`
- 运行时加载：`BootLoader` → `package.LoadAssetSync<RawFileObject>(assetPath)`
- 拦截名单：`ValidateRuntimePreloadAssemblyName` 拒绝 `Launcher` / `TestKit`
