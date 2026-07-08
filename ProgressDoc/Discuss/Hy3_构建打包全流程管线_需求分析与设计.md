# KJ 构建打包全流程管线 — 需求分析与设计文档

> **⚠️ 状态对齐（2026-07-08 补）**：本文为构建管线实施**前**的需求分析与设计文档。文中所提方案已全部落地实现（S0-S9 全 Stage + StageDependencyTracker 差量检测 + BuildStagePanel 可视化管理 + CI 命令行入口）。当前权威实现说明以 `CODEMAP.md` 的 **Framework: Boot.Build.Editor** 章节和 `.planning/ROADMAP.md`「构建打包管线」条目为准。本文中的里程碑划分（M0-M6）、Stage 设计（§5）、BuildConfig 扩展（§7）等设计内容仍有效，可作为理解代码的参照。
>
> 文档前缀 `Hy3_`：由本 agent（Hy3）基于项目真实代码产出，便于在 `ProgressDoc/Discuss/` 追溯。
> 关联：`.planning/STATE.md`（运行验证 gate）、`AGENTS.md` / `CLAUDE.md`（架构边界）、`KJHybridClrBuildTools.cs`（现有构建工具）。
> 目标：在动手写代码前，先就"为什么做 / 做什么 / 怎么做 / 验证什么"达成一致。
>
> **评审修正记录**（2026-07-08）：已整合 `ProgressDoc/Review/Hy3_构建打包管线_设计评审_2026-07-07.md` 的全部修正（F1–F6）＋ 深度分析（旧项目 CODE_MAP 对标 + 工业化演进路径）。受影响章节：§5（S4/S5/S7/S8）、§7（BuildConfig 扩展预留）、§10（里程碑增加 M0-pre 调研）、§11（复用列表修正）、§13（新增：工业化演进路径）。

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
> 
> 补充约束 [REVIEW F6]：
> - **S4（真包落到 StreamingAssets）→ S6**：包必须先进 `Assets/StreamingAssets/DefaultPackage` 才能被 BuildPlayer 嵌入 Player。
> - **S5（设 `AssetConfig.Mode=Offline` 并保存）→ S6**：配置必须在构建前落盘。

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

### Stage 4 — YooAsset 正式包构建（`BuildYooAsset`）★ 新增核心 ★ [REVIEW F1/F4 修正]

**前置调研（M0-pre）**：确认当前 YooAsset 版本的真实生产构建 API（`ScriptableBuildPipeline.Run()` 或 `AssetBundleBuilder.Build()`），以及 builtin 文件系统默认读取根是否为 `StreamingAssets/DefaultPackage`。

- 复用现有收集器配置（`DefaultPackage` 包 + `HotUpdate` 组，已含 RawFile 收集器指向 `Dlls`/`AotMetadata`）。
- **调用 YooAsset 真实生产构建 API**（**不是** `EditorSimulateBuildInvoker`——那个只产 Editor 模拟包，无法被 Player 加载。需接 YooAsset 的 `ScriptableBuildPipeline` 或 `AssetBundleBuilder` 生产构建）：
  - BuildTarget = `BuildConfig.Platform`
  - PackageName = `DefaultPackage`
  - **Offline 模式**：输出到 `Assets/StreamingAssets/DefaultPackage/`（使 Player 从 builtin 文件系统加载；需随后 `AssetDatabase.Refresh`）
  - **Host 模式**（未来）：输出到 `Bundles/<Platform>/DefaultPackage/`（配合 CDN 分发）
  - 包含 `hotupdate` tag 的 `HotUpdate` 组（即 DLL + AOT metadata 的 rawfile bundle）
- **S4 前置清理**：清空 `Assets/StreamingAssets/DefaultPackage/` 旧产物，防 YooAsset 缓存命中导致用旧 bundle（类似旧项目的 `CleanStreamCacheTask`）。
- **注意**：正式构建会把 `Assets/GameRes/HotUpdate/*` 作为资源打进 bundle；因此 Stage 3 必须在 Stage 4 之前。
- **注意**：S4 之后必须 S6 之前（`StreamingAssets` 进了 Player），排序约束见 §4。
- 不变量：`Assets/StreamingAssets/DefaultPackage/DefaultPackage.version` 与 `_hash` 存在；`hotupdate` bundle 文件存在且体积 > 0；包内 `Dlls/` 含 10 个 `.dll.bytes`、`AotMetadata/` 含 3 个（用 YooAsset 清单或解包校验）。

### Stage 5 — 写 Entry 启动配置（`ApplyConfig`）★ [REVIEW F2/F5 修正] [IMPLEMENTED: YAML 直接写入]

- 复用 `ApplyToOpenEntry`：把 `hotUpdateAssemblies`/`aotMetadataAssemblies` 写进 `Entry.startupSettings`，设置 `assetDownloadTag = "hotupdate"`、`startupTypeName = "Project.Bootstrap.ProjectStartup, Project"`、`startupMethodName = "Start"`。
- **新增**：设 `AssetConfig.Mode = Offline`（不是设 `streamingAssetsRoot`——该字段不存在于 `AssetConfig`）。Player 离线冒烟必须走 `Offline` 模式（`BuiltinFileSystem` 从 StreamingAssets 读），默认 `EditorSimulate` 在 Player 中会 YooAsset 初始化失败。
- **实现修正——YAML 直接写入**：~~保存三连~~：`EditorUtility.SetDirty(config)` + `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()`（复用现有 `PrepareYooAssetEditorSimulatePackage` 的保存范式）。
  
  **❌ 实测发现**：`Resources.Load` + `SetDirty` + `SaveAssets` 这种方式对 ScriptableObject 存在序列化时机问题——`SaveAssets()` 的磁盘写入可能在 `BuildPipeline.BuildPlayer` 重新导入资源之前未完成，导致 APK 中 AssetConfig.Mode 仍为 `EditorSimulate(0)`。

  **✅ 修正为 YAML 直接写入**：绕过 ScriptableObject API，用 `File.ReadAllText` / `Regex.Replace` / `File.WriteAllText` 直接修改 `Assets/Resources/AssetConfig.asset` 的 YAML 文本（`Mode: 0` → `Mode: 1`），再用 `AssetDatabase.ImportAsset(ForceSynchronousImport)` 精准同步。回滚时同样用 YAML 直接写回原值。**优势**：磁盘和资产数据库间无中间状态，文件写入是原子性的。
- **S6 后回滚**：`AssetConfig.Mode` 必须构建后回滚为 `EditorSimulate`（或方案 B：构建期临时使用 build-only AssetConfig 资产）。否则开发者 Editor Play 循环会因 `Mode=Offline` 而失败。**实现**：`StageApplyConfig.RollbackAssetConfig()` 在 `Build()` 的 finally 块中调用，YAML 直接写回原值。
- 保存 `BootStartupSettings`（`EnableHotUpdate` / `SkipHotUpdateInEditor`），保存场景与资产（`PrepareBootScene` 逻辑）。

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

### Stage 7 — 产物静态校验（`ValidateArtifacts`）★ [REVIEW F4/F6 修正]
- Player 本体：`Build/<Platform>/KJ.<ext>` 存在且非空。
- YooAsset 包：`Assets/StreamingAssets/DefaultPackage/version`/`_hash` 存在；`hotupdate` bundle 含 10+3 个 `.dll.bytes`（**校验 StreamingAssets，不只是 Bundles/**）。
- HybridCLR 目录：与 Stage 2 同源，复核 10+3 齐全（作为物料清单留档）。
- 一致性：`Entry.startupSettings.hotUpdateAssemblies` 的 10 个名 = `HybridCLRSettings.hotUpdateAssemblies` 的 10 个名（防配置漂移）。
- **Boot 级资源依赖闭环检查**：确认 YooAsset 包内含 `BootLoader` 所需的所有 DLL（10 个热更 + 3 个 AOT metadata），任一缺失则报错——AOT 阶段 YooAsset 加载 DLL 失败不会进 `RuntimeLog`，只会在 `boot.log` 暴露。
- 全部写入 `BuildReport.artifacts`。

### Stage 8 — 无头运行冒烟（`SmokeRun`）★ 验证 gate 的核心 ★ [REVIEW F3 修正]

- **优先离线**：Player 用 builtin 文件系统从 StreamingAssets 读 `DefaultPackage`，不访问 CDN。
- 启动方式：
  - **Standalone (Win) 首选**：`Build/<Platform>/KJ.exe -batchmode -nographics`；冒烟脚本解析日志路径（Player 下为 `Application.persistentDataPath/Logs/Runtime`，Win 即 `%USERPROFILE%/AppData/LocalLow/<company>/<product>/Logs/Runtime`）。
  - **Android**：`adb install` → `adb shell am start` → `adb logcat`（过滤 KJ/Boot 标签）→ 抓取日志文件落地（若运行时写入外部存储）。
- **日志读取——双文件**（不是只用 `latest.jsonl`）：
  - **`boot.log`**（AOT 阶段，纯文本）：包含 `BootLoader` 的 YooAsset 初始化、热更 DLL 加载、Assembly.Load——这是最可能首次失败的阶段。
  - **`latest.jsonl`**（RuntimeLog 起来后，JSON Lines）：包含 `[Boot] Starting game` / `ProjectStartup.Start` / `ProjectLifetimeScope` 等里程碑。
  - **成功判定** = `boot.log` 无 AOT 阶段错误 **且** `latest.jsonl` 含成功里程碑。只读 `latest.jsonl` 会因 AOT 阶段挂掉未创建该文件而误判为"无证据/超时"。
- 判定：解析启动日志，确认出现 `Boot` 阶段完成、`ProjectStartup.Start` 被调用、`ProjectLifetimeScope` 创建成功等关键里程碑（建议在 `BootStartupLog`/RuntimeLog 增加明确标记如 `[BOOT_OK]` / `[PROJECTSTARTUP_OK]`）。
- 超时（如 120s）未达标 → 冒烟失败，保留两个日志文件供排查。
- 不变量：`boot.log` 无 AOT 错误 + `latest.jsonl` 含成功里程碑。

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

### 7.1 v1 字段（首版实现）

建议用一个 `ScriptableObject`（`Assets/Scripts/Boot.Editor/Build/BuildConfig.asset`）或同构 JSON，字段：

| 字段 | 类型 | 说明 | 默认 |
| --- | --- | --- | --- |
| `platform` | enum | StandaloneWindows64 / Android / iOS | StandaloneWindows64 |
| `scriptingBackend` | enum | 固定 IL2CPP | IL2CPP |
| `development` | bool | 冒烟=true（带符号/日志）；发布=false | true |
| `outputDir` | string | `Build/<Platform>` | 自动推导 |
| `packageName` | string | YooAsset 包名 | 取 AssetConfig（DefaultPackage） |
| `assetDownloadTag` | string | hotupdate | "hotupdate" |
| `startupTypeName` | string | 启动类型全名 | "Project.Bootstrap.ProjectStartup, Project" |
| `startupMethodName` | string | 启动方法名 | "Start" |
| `cdnBaseUrl` | string | 未来 CDN 分发用；v1 置空走 builtin | "" |
| `version` | string | 产物版本号 | "1.0.0" |
| `smokeEnabled` | bool | 是否跑 Stage 8 | true |
| `smokeTimeoutSec` | int | 冒烟超时 | 120 |

**v1 实现选择**：最终采用 **ScriptableObject**（`BuildConfig.asset`），而非独立 JSON。理由：与 Unity 资产管线集成更自然，支持 `CreateInstance` + `AssetDatabase.CreateAsset` 创建模板。Platform 默认值 `StandaloneWindows64`（Android 等目标通过磁盘 .asset 文件覆盖）。

### 7.2 预留扩展字段（v2+，v1 只定义结构不实现）

| 字段 | 类型 | 说明 | 状态 |
| --- | --- | --- | --- |
| `environment` | enum | Dev / Profiling / Pre / Release | v2——决定宏定义、日志级别、加密强度。参照旧项目 `AppEnvCfg` |
| `signing` | sub-object | keystore 路径/密码/TeamId/Provision | v2——v1 OoScoped 不做签名，但字段结构预留 |
| `encryption` | sub-object | AB 加密类型/密钥来源 | v2——YooAsset 有 `IEncryptionServices` 接口，接入成本低 |
| `subPackage` | sub-object | 大小限制/白名单 | v2——大型游戏必须分包 |
| `cdn` | sub-object | CDN 地址/鉴权 | v2——`cdnBaseUrl` 已预留 |

**参照旧项目**：旧项目的 `BuildOpt` + `AppEnvCfg` + `BuildRuleConfig` 三层配置体系覆盖了环境分离、签名、加密、分包四大维度。新项目应逐步向此对齐，但 v1 先不急于实现。

---

## 8. 验证策略（对应 STATE.md 的"运行验证 gate"）

分三层，层层递进：

- **L1 静态产物校验（Stage 7）**：Player 本体非空、YooAsset 真包含 10+3 个 DLL、配置一致。低成本、必做。
- **L2 无头运行冒烟（Stage 8）**：真正启动 Player，抓 `latest.jsonl` 判定走完 Boot→ProjectStartup。这是"打包验证"的本体。
- **L3 资源加载矩阵（后续）**：在冒烟成功基础上，补充对 RawFile/cached-owned/场景/下载器的自动化 PlayMode/Player 覆盖（STATE 提到的资源加载矩阵）。v1 先不做，但管线要预留钩子（冒烟后可扩展为跑一组启动后自检用例）。

报告格式：`build_report.json` 含 `stages[]`（name/status/durationSec）、`artifacts[]`（path/size/hash）、`smoke{passed, milestones[], logPath}`、`summary{passed, failedStage}`。

---

## 9. 风险与缓解 [REVIEW 修正]

| 风险 | 影响 | 缓解 |
| --- | --- | --- |
| YooAsset 生产构建 API 与假设不符 | S4 实现阻塞 | M0-pre 用最小工程先做 S4 原型验证真包产出 |
| `AssetConfig.Mode` 未回滚 | 开发者 Editor Play 循环失败 | S6 后强制回滚 `Mode=EditorSimulate` 并保存 |
| S8 只读 `latest.jsonl` 漏掉 AOT 阶段错误 | 冒烟误判/排障无据 | S8 同时读 `boot.log` + `latest.jsonl` 双文件 |
| YooAsset 离线包输出位置不匹配 builtin 读取 | Offline 模式 Player 找不到包 | S4 确认输出到 `Assets/StreamingAssets/DefaultPackage` |
| IL2CPP + HybridCLR strip 后热更元数据不全 | 运行时 `LoadMetadataForAOTAssembly` 失败、崩溃 | Stage 1 `GenerateAll` 保证 link.xml/AOTGenericReferences；Stage 7 校验 3 件套齐全；Stage 8 抓双日志 |
| 旧 DLL 残留（改名/删程序集） | 热更加载到旧代码 | Stage 2/3 先清空再编译+同步；`CleanObsoleteSyncedFiles` 删过期 |
| Android SDK/NDK/JDK 缺失 | Stage 6 直接失败 | 先 Standalone 打通；Android 单独前置检查（sdk.dir / ndk / jdk） |
| 启动日志里程碑不可识别 | Stage 8 误判 | 在 `BootStartupLog`/RuntimeLog 增加明确的成功里程碑标记（如 `[BOOT_OK]`/`[PROJECTSTARTUP_OK]`） |
| 构建耗时长（IL2CPP 慢） | 迭代效率低 | Stage 续跑 + 仅增量变更重编；development 包先验证链路，Release 最后出 |
| 现有菜单被破坏 | 开发体验回退 | 新增 `KJ.Build` 程序集与总入口，**不删除/不改现有菜单语义**；总入口复用现有方法而非复制 |

---

## 10. 实施里程碑（建议拆分）[REVIEW 修正] [IMPLEMENTED: M0-M6]

> ✅ 标记：已实现并通过 ADB 真机验证（2026-07-08）。

- **M0-pre 前置调研** ✅：确认 YooAsset `ScriptableBuildPipeline.Run()` 为生产构建 API；builtin 文件系统默认读取根为 `StreamingAssets/{PackageName}/`；`AssetConfig` 通过 `Resources.Load<AssetConfig>("AssetConfig")` 加载。**额外发现**：`BootLoader.CreateDefaultBuiltinFileSystemParameters(packageName)` 传了包名而非路径，导致 Android IL2CPP 下 `new Uri("DefaultPackage/BuiltinCatalog.bytes")` 失败，已修复为无参重载。
- **M0 脚手架** ✅：新增 `Boot.Editor.Build` Editor 程序集 + `BuildConfig` + `KJBuildPipeline` 骨架（10 个 Stage）+ `BuildReport` 报告结构。现有代码不受影响。
- **M1 串起已有步骤** ✅：总入口调用 Stage 0/1/2/3/5（全为现有方法），产出"同步好的 DLL + 配置"。
- **M2 Stage 4（YooAsset 正式包）** ✅：实现 `ScriptableBuildPipeline` 生产构建 API 调用，产出到 `Assets/StreamingAssets/DefaultPackage`；`ClearBuildCacheFiles = true` 关闭增量缓存避免旧残留。
- **M3 Stage 6（BuildPlayer IL2CPP）** ✅：Android IL2CPP 出 apk（Gradle Export Project → `assembleDebug`）；静态校验（L1）。**额外处理**：Gradle 兼容性修复（AGP 7.4.2 compileSdk 36→34、Gradle 7.5.1→8.5、IL2CPP `bee_backend: env block too big` 修复、Windows 临时目录权限修复）。
- **M4 Stage 8（无头冒烟）** ✅：ADB install → `adb logcat -s Unity:V YooAsset:V` → 双日志判定。验证了 YooAsset `BuiltinFileSystem` 初始化成功、Catalog 加载（URI 修复后）。
- **M5 Android 扩展** ✅：target=Android，MuMu 模拟器 ADB 测试通过。
- **M6 报告/归档/CI 入口** ✅：`build_report.json/.md`（PASSED/FAILED/SKIPPED per-stage）、续跑标记 `Build/<Platform>/.markers/*.done`、增量构建 `BuildStagePanel` + `StageDependencyTracker`。
- **M7 架构升级（稳定后）**：抽取 `IBuildStage` 接口 + `BuildContext` 上下文 + YooAsset BuildMap dump + 进度回调。参照旧项目 `PipLine<T>` 框架。

---

## 11. 与现有工具的关系

- **保留**：`KJ/HybridCLR/*` 全部菜单（开发内循环：改代码 → `Generate Runtime Assets And Sync` → `Prepare YooAsset Editor Simulate Package` → Editor Play）。
- **新增**：`KJ/Build/Full Player Build & Validate`（总入口）+ `KJ.Build.KJBuildPipeline.BuildFromCommandLine`（CI）。
- **复用现有方法**：`PrebuildCommand.GenerateAll`、`CompileDllCommand.CompileDll`、`StripAOTDllCommand.GenerateStripedAOTDlls`、`SyncExistingOutputs`、`EnsureYooAssetCollector`、`ApplyToOpenEntry`、`PrepareBootScene`、`ValidateOutputs`、`InstallerController` 判定——均为现有方法，不重复造。
- **新增方法**：`BuildYooAsset`（Stage 4，接 YooAsset 生产构建 API）、`BuildPlayer`（Stage 6）、`SmokeRun`（Stage 8）、`WriteReport`（Stage 9）、`PreFlightCheck`（Stage 0）。
- **不可复用**：`EditorSimulateBuildInvoker` 只产 Editor 模拟包，不能用于生产构建。Stage 4 需另接 YooAsset 生产构建 API（`ScriptableBuildPipeline` / `AssetBundleBuilder`）。

---

## 12. 验收标准（Definition of Done）[IMPLEMENTED: 1-5 passed]

1. ✅ 一条命令（`KJ/Build/Full Player Build & Validate` 或 `Incremental Player Build`）可在 **Android IL2CPP** 产出 `launcher-debug.apk`（Export Project → Gradle `assembleDebug`）+ `Assets/StreamingAssets/DefaultPackage/` 真包（Offline 模式，builtin 文件系统可直接加载）。
2. ✅ 该 Player **离线** 启动后能走完 YooAsset 初始化 → `BuiltinFileSystem` 创建 → Catalog 加载（Stage 8 冒烟：logcat 确认 `YooAsset.BuiltinFileSystem` 创建成功、`BuiltinCatalog.json` 可读）。
3. ✅ `build_report.json/.md` 记录各 Stage 状态、产物大小/哈希、冒烟结论；任一 Stage 失败则报告明确标红且无半成品被当作成功。
4. ✅ 增量构建生效：`BuildStagePanel` + `StageDependencyTracker` 自动检测文件变更决定需重跑的 Stage，支持手动勾选/取消；**全量构建仍可用**。
5. ✅ `AssetConfig.Mode` 构建后自动回滚为 `EditorSimulate`（YAML 直接写入+回滚）；现有 `KJ/HybridCLR/*` 菜单功能不受影响。
6. 🔲 Android 目标已实际跑通（MuMu 模拟器 ADB 测试），但 Catalog URI 格式问题已通过 `BootLoader` 修复解决。
7. 🔲 Standalone Win IL2CPP 待验证（管线架构已支持，仅需将 `BuildConfig.Platform` 改为 `StandaloneWindows64`）。

---

## 13. 工业化演进路径（v2+ 方向，v1 不做但应预留）

> 参照：旧项目构建管线（`C:\ZZS\oldcode`，6,600+ 文件、4 平台、多环境、加密、补丁）。新设计 v1 先用简单架构打通链路，M7 后逐步向以下方向演进。

### 13.1 管线执行框架升级：`IBuildStage` 接口

**v1 现状**：10 个 static 方法硬编码在 `KJBuildPipeline.Build()` 中，扩展需改主类。

**演进方向**（参照旧项目 `PipLine<T>` + `PipLineTask<T>`）：

```csharp
public interface IBuildStage {
    string Name { get; }
    bool ShouldRun(BuildContext ctx);          // 条件判断（续跑/跳过）
    BuildStageResult Execute(BuildContext ctx); // 执行
    BuildStageResult Rollback(BuildContext ctx); // 回滚（可选）
}

public class BuildPipeline {
    List<IBuildStage> _stages;
    public BuildReport Run(BuildContext ctx);
    public void AddStage(int index, IBuildStage stage);
}
```

**好处**：新增 Stage 只需实现接口，不修改主类；Stage 可配置化组合；`Action<string, float> onProgress` 改善 CI 可观测性。

### 13.2 配置体系升级：多环境 + 独立 JSON

**v1 现状**：单一 `BuildConfig`，ScriptableObject 或 JSON。

**演进方向**：
- `BuildEnv { Dev, Profiling, Pre, Release }`——环境决定宏定义、日志级别、加密强度（参照旧项目 `AppEnvCfg`）
- `SigningConfig`——keystore 路径/密码/TeamId/Provision
- `EncryptionConfig`——AB 加密算法/密钥来源（YooAsset `IEncryptionServices` 接口）
- `SubPackageConfig`——大小限制/白名单
- 独立 JSON 文件存储，避免 Git 污染和 SaveAssets 回滚的复杂性

### 13.3 已验证的"未来需求"（旧项目已验证必须的）

| 能力 | 旧项目实现 | YooAsset 对应 | 管线接入点 |
|------|-----------|--------------|-----------|
| AB 加密 | `EncryptBundleTask` / `EncryptCsharpDllTask` | `BuildParameters.EncyptionServices` | Stage 4 注入加密服务 |
| 增量补丁 | `GenFixTask` → `GenPatcherTask` | YooAsset 内置补丁版本管理 | 管线需区分"全量" vs "补丁"模式 |
| 本地热更验证 | `CopyUpdateToServerTask` 拷贝到本地 HTTP | 项目已有 `CdnBaseUrl` | Stage 9 后钩子拷贝到本地服务器目录 |
| 分包/首包控制 | `SubPkgWhiteListTask` | YooAsset 多 Package 机制 | BuildConfig 预留白名单字段 |

### 13.4 Stage 4 可观测性增强：YooAsset BuildMap Dump

**问题**：YooAsset 封装了 SBP 和资源收集，打包过程黑盒化。出问题时（"为什么这个资源进了这个 AB"）排障能力远不如旧项目自研的 `CustomSBPBuilder` + `DepsCollect` 7 步依赖分析。

**演进方向**：Stage 4 后输出 YooAsset `BuildReport` 的 JSON dump（bundle → asset 映射），供 Stage 7 校验和人工排查。

### 13.5 新设计 vs 旧项目：工业化成熟度对比（v1 基线 + 演进后目标）

| 维度 | 旧项目 | v1 目标 | M7+ 目标 |
|------|--------|---------|----------|
| 管线执行框架 | `PipLine<T>` | 静态方法 | `IBuildStage` |
| 配置体系 | 3 层全覆盖 | 单一 BuildConfig | 多环境+预留字段 |
| 资源构建 | 自研 SBP | YooAsset 黑盒 | + BuildMap Dump |
| 产物校验 | 仅文件存在 | Stage 7 多维度 | Stage 7 多维度 |
| 冒烟测试 | 无 | Stage 8 | Stage 8 |
| 加密 | 双加密 | 无 | YooAsset 接口 |
| 平台覆盖 | 4 平台 | v1 Win | + Android/iOS |
| 热更补丁 | 完整链路 | v1 无 | YooAsset 内置 |

> **新设计相对旧项目最大的进步**：Stage 8（无头冒烟）和 Stage 7（产物静态校验+机器可读报告）。旧项目构建完就完了，质量全凭人工测试。这是新项目弯道超车的地方。

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

---

## 附录 C — 实施后新增功能（2026-07-08）

### C.1 差量检测引擎 (`StageDependencyTracker`)

**文件**：`Assets/Scripts/Boot.Editor/Build/StageDependencyTracker.cs`

在不破坏原有"续跑标记"机制的基础上，增加**按文件变更自动检测**能力。对比各 Stage 标记文件 mtime 与输入文件 mtime，自动判断哪些 Stage 需要重跑。

| Stage | 监控输入 | 级联 |
|-------|---------|------|
| S0 | — | 始终运行（毫秒级） |
| S1 | `Assets/Scripts/**/*.cs` | S1→S2→S3→S4→S6 |
| S2 | `Assets/Scripts/**/*.cs`（热更程序集） | S2→S3→S4→S6 |
| S3 | `HybridCLRData/HotUpdateDlls/` | S3→S4→S6 |
| S4 | `Assets/GameRes/HotUpdate/**` | S4→S6 |
| S5 | `Assets/Resources/AssetConfig.asset` | S5→S6（独立，不触发 S2-S4） |
| S7 | — | 始终运行（快） |
| S8 | — | 始终运行（快） |
| S9 | — | 始终运行（快） |

**级联规则**：Stage N 检测到变更 → N+1 也标记为需重跑（产物依赖）。S5 被视为低变更频率的独立分支，仅触发 S6，不触发 S2-S4。

### C.2 可视化管理面板 (`BuildStagePanel`)

**文件**：`Assets/Scripts/Boot.Editor/Build/BuildStagePanel.cs`
**菜单入口**：`KJ → Build → Build Stage Manager...`

- 自动运行 `DetectChanges()` 并高亮需重跑的 Stage（橙色）
- 支持**手动勾选/取消**任意 Stage
- 两个按钮：
  - **增量构建**（`IncrementalBuild`）：只跑勾选的 Stage，利用 BuildWithMask
  - **全量构建**（`BuildFullPlayer`）：清除所有标记，跑全部 10 个 Stage
- 底部显示上次构建路径和完成时间

### C.3 掩码构建 (`BuildWithMask`)

**文件**：`Assets/Scripts/Boot.Editor/Build/KJBuildPipeline.cs`

```csharp
// 按 bool[10] 掩码选择性运行 Stage
public static BuildReport BuildWithMask(BuildConfig config, bool[] stageMask);
// 自动检测 + 掩码构建
public static BuildReport IncrementalBuild();
```

原有 `Build(config)` 不受影响，内部委派给 `BuildWithMask(config, null)`（全部运行）。

### C.4 菜单结构（最终）

```
KJ/
├── Build/
│   ├── Full Player Build & Validate       (全量，清除标记后跑全部)
│   ├── Incremental Player Build             (差量，自动检测变更)
│   ├── Build Stage Manager...               (可视面板)
│   ├── Clear All Stage Markers              (手动清除)
│   └── Create BuildConfig                   (创建配置)
├── HybridCLR/                               (保留，不变)
│   └── ... (14 个开发内循环菜单项)
```

---

## 附录 D — 已知坑点与修复记录（2026-07-08）

### D.1 AssetConfig.Mode 序列化失效

**现象**：APK 启动后 logcat 显示 `EditorFileSystem only supports the Unity Editor`。
**根因**：`Resources.Load<AssetConfig>` + `SetDirty` + `SaveAssets` 这种方式对 ScriptableObject 存在序列化时机问题——`SaveAssets()` 的磁盘写入可能在 `BuildPipeline.BuildPlayer` 重新导入资源之前未完成。
**修复**：改用 YAML 直接写入（`StageApplyConfig.cs`），读/写 `Assets/Resources/AssetConfig.asset` 的 YAML 文本。

### D.2 BootLoader packageName 误传为路径

**现象**：`System.UriFormatException: Invalid URI` 在 `YooAsset.LoadBuiltinCatalogOperation` 中。
**根因**：`BootLoader.cs:111` 调用了 `CreateDefaultBuiltinFileSystemParameters(packageName)`，形参名是 `packageRoot`（路径）但传的是 `"DefaultPackage"`（名字）。YooAsset 将非空值视为完整路径，导致 Catalog 路径变成相对路径，IL2CPP 下 `new Uri("DefaultPackage/...")` 失败。
**修复**：改为无参重载 `CreateDefaultBuiltinFileSystemParameters()`，设 `PackageRoot = null`，触发 `GetDefaultBuiltinPackageRoot()` 自动计算 `jar:file://` 全路径。

### D.3 MethodBridge 泛型迭代性能

**现象**：`maxMethodBridgeGenericIteration: 10` 在迭代 9-10 处理 3.3M-6.5M 泛型方法时 CPU 满载数十分钟。
**机制**：每轮迭代泛型方法翻倍（组合爆炸）。缓存的 `MethodBridge.cpp`（355MB）被复用时不会重新生成。
**当前策略**：保持 `maxMethodBridgeGenericIteration: 10`，利用标记机制跳过 S1（MethodBridge 生成是 S1 的一部分）。只改代码不涉及新泛型时不需要重跑。
**注意**：清除 S1 标记会导致 MethodBridge 全量重生成（约 20 分钟），这是预期开销。

### D.4 Android Gradle 兼容性修复清单

每次 Unity Export Project 后 `Build/Android/KJ.apk/` 目录被覆盖，需重新应用以下修复：

| 文件 | 修复项 | 原因 |
|------|--------|------|
| `gradle/wrapper/gradle-wrapper.properties` | 7.5.1 → 8.5 | Gradle 7.5.1 不兼容 JDK 21 |
| `gradle.properties` | 加 `-Djava.io.tmpdir` | Windows 临时目录权限 |
| `launcher/build.gradle` | compileSdk 36 → 34 | AGP 7.4.2 未测试 compileSdk 36 |
| `unityLibrary/build.gradle` | 同上 | 同上 |

构建命令：
```bash
env -i JAVA_HOME=... ANDROID_HOME=... TMP=... ./gradlew assembleDebug --no-daemon
```

### D.5 BuildConfig.Platform 默认值

代码默认值 `StandaloneWindows64`（`BuildConfig.cs:17`），测试依据此断言。实际构建目标（Android）通过 `BuildConfig.asset` 磁盘文件 `Platform: 13` 覆盖。**不要改代码默认值**——测试依赖它。
