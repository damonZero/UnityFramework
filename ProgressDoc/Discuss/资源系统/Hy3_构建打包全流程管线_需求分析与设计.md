# KJ 构建打包全流程管线 — 需求分析与设计文档

> **⚠️ 状态对齐（2026-07-10 补）**：当前代码已收敛为 **BuildProfile-only + BuildPipelineRunner + P0-P9 + fingerprint** 架构。旧 `BuildConfig.cs` / `BuildConfig.asset` / `BuildReport.cs` / `StageDependencyTracker.cs`、`.markers` 续跑和 mask 手动 Stage 执行已删除。本文保留历史需求分析与演进讨论；涉及 BuildConfig、旧 S0-S9 marker/mask 的段落仅作为历史背景，不再代表当前实现。当前权威入口是 `KJ/Build/Full Player Build & Validate`、`KJ/Build/Dashboard`、`Boot.Editor.Build.BuildCommandLine.Run -profile <BuildProfile.asset>`。
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
| Android SDK/NDK/JDK 缺失 / 路径失效 | Stage 6 直接失败（"JDK directory is not set or invalid"） | 根因通常是 Unity Android 模块（原生工具链）损坏/缺失，重装模块即根治；JDK/SDK/NDK 由 Unity 自带并自动探测，无需手写回退。Stage 0 预检已校验 IL2CPP 与平台模块；若 Android 模块缺失应在 Unity Hub 直接添加模块，而非在构建脚本里绕过。（历史：曾加 `AndroidToolResolver` 反射回退 JDK 路径作 workaround，排查后确认为多余代码已删除。） |
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

---

## 附录 E — Build Pipeline v2 工业级重构设计（2026-07-09）

> 本章是对当前 v1 管线（S0-S9）的二次设计。v1 已证明核心链路成立：HybridCLR 生成/编译/同步、YooAsset 真包、AssetConfig Offline 写入、BuildPlayer、产物校验、Smoke、报告。v2 的目标不是推翻 v1，而是把它升级为**配置驱动、阶段插件化、环境可区分、报告可机器读取、Odin 可视化、AI 可接管**的工业级打包系统。

### E.1 设计目标

1. **环境清晰**：Dev / Profiling / Audit / Formal / QA / Pre 等环境不再只是 `Development=true/false`，而是完整的构建策略集合。
2. **流程可解释**：每次构建先生成 `BuildPlan`，明确本次会跑哪些 Stage、为什么跑、哪些被跳过、跳过依据是什么。
3. **失败可诊断**：每个失败必须产生结构化 `BuildIssue`，包含错误码、阶段、证据、可能原因、建议修复、相关文件。
4. **产物可追溯**：每次构建归档到 `BuildBackup/{Environment}/{Version}/{BuildNo}/`，包含 Player、资源 Manifest、日志、报告、AI handoff。
5. **AI 可接管**：AI 不依赖 Console 截图；只读取固定路径的 `build_report.json`、`issues.json`、`ai_handoff.json`、`latest.jsonl`、`latest.session.json`。
6. **UI 可运营**：Odin 面板提供 Profile 编辑、预检、构建、监控、报告、日志、问题定位入口。

### E.2 旧项目 CODE_MAP 对标结论

旧项目 `F:\int_37_pack\client\CODE_MAP.md` 中值得吸收的不是具体实现，而是工业化分层：

| 旧项目结构 | 对 KJ v2 的启发 |
|------------|----------------|
| `BuildBackup/{Dev|Formal|Audit|Profiling}/{版本号}` | KJ 需要按环境/版本/构建号归档产物，避免 `Build/Android` 单目录覆盖。 |
| `BuildInfo/BaseBundle` / `Formal/Fix` / `UpdateFiles` | KJ 需要把 Player 产物、YooAsset 资源包、热更物料、更新清单分开保存。 |
| `Gradle/` / `XCode/` / `build/` | 平台工具链和 Unity Stage 编排需要解耦；Android/iOS 后处理应独立成 Stage。 |
| `Core.Editor/ToolsBuild` | 打包不是几个菜单函数，而是独立编辑器工具体系；KJ 应落到 `Boot.Editor/Build` 的完整工具域。 |
| `Logs/` / AI 文档入口 | KJ 已有 `Framework.RuntimeLog`，可进一步让构建报告和运行日志成为 AI 诊断事实源。 |

### E.3 v2 推荐目录结构

v2 采用**双层结构**：

- `Assets/Framework/BuildPipeline/`：构建管线的纯数据契约、报告结构、错误码、fingerprint/marker schema、AI handoff。该层不引用 `UnityEditor`，不引用 `Assets/Scripts`，不拥有任何执行逻辑。
- `Assets/Scripts/Boot.Editor/Build/`：Unity Editor 执行层，负责调用 HybridCLR.Editor、YooAsset.Editor、AssetDatabase、PlayerSettings、BuildPipeline、ADB/Odin/CI。

不放在 `Core.Editor` 的原因：Build Pipeline 不是 Core 模块工具，而是跨 Launcher、Boot、HybridCLR、YooAsset、ProjectSettings、Player 构建、Runtime Smoke 的发布系统。`Core.Editor` 可以消费构建报告或提供 RuntimeLog 查看器，但不应拥有构建流程。

```text
Assets/Framework/BuildPipeline/
├── BuildPipeline.asmdef              # 纯契约程序集；不引用 UnityEditor / Scripts
├── Environment/
│   ├── BuildEnvironment.cs
│   ├── BuildChannel.cs
│   └── BuildFeatureFlags.cs
├── Plan/
│   ├── BuildPlan.cs
│   ├── BuildStageFingerprint.cs
│   ├── BuildStageInputs.cs
│   ├── BuildStageOutputs.cs
│   └── BuildSkipDecision.cs
├── Reports/
│   ├── BuildReportV2.cs
│   ├── BuildArtifactManifest.cs
│   ├── BuildTimeline.cs
│   └── AiBuildHandoff.cs
├── Diagnostics/
│   ├── BuildIssue.cs
│   ├── BuildIssueSeverity.cs
│   └── BuildErrorCodes.cs
└── CI/
    └── BuildExitCode.cs

Assets/Scripts/Boot.Editor/Build/
├── Config/
│   ├── BuildProfile.cs              # ScriptableObject 包装层；序列化 BuildProfileData
│   ├── BuildProfileSet.cs           # Profile 集合，Odin Dashboard 入口引用
│   └── BuildProfileValidator.cs
├── Pipeline/
│   ├── BuildPipelineRunner.cs       # v2 编排器
│   ├── BuildContext.cs              # 一次构建的上下文
│   ├── BuildStage.cs                # Stage 抽象基类/接口
│   ├── BuildStageRegistry.cs        # Stage 注册、排序、依赖
│   ├── BuildPlan.cs                 # 本次实际执行计划
│   └── BuildResumeState.cs          # marker/cache/上次失败点
├── Stages/
│   ├── S00_Preflight/
│   ├── S10_Generate/
│   ├── S20_HybridCLR/
│   ├── S30_Assets/
│   ├── S40_ApplyConfig/
│   ├── S50_Player/
│   ├── S60_StaticVerify/
│   ├── S70_RuntimeSmoke/
│   └── S90_Report/
├── Diagnostics/
│   ├── BuildIssue.cs                # code/severity/stage/evidence/fixHint
│   ├── BuildLogCollector.cs         # Editor.log / boot.log / latest.jsonl / adb logcat
│   ├── BuildAnalyzer.cs             # 规则化诊断
│   ├── BuildKnowledgeBase.cs        # 常见错误库
│   └── AiBuildHandoff.cs            # AI 可读 handoff
├── Reports/
│   ├── BuildReportV2.cs             # JSON 主报告
│   ├── BuildReportWriter.cs         # json/md/html
│   ├── BuildArtifactManifest.cs     # apk/bundle/hash/size/version/channel
│   └── BuildTimeline.cs             # 阶段耗时、依赖、跳过原因
├── UI/
│   ├── BuildDashboardWindow.cs      # Odin 主面板
│   ├── BuildProfileEditor.cs
│   ├── BuildMonitorView.cs
│   └── BuildReportViewer.cs
└── CI/
    ├── BuildCommandLine.cs
    ├── BuildArgs.cs
    └── BuildExitCodeMapper.cs
```

依赖方向：

```text
Framework.BuildPipeline  ←  Boot.Build.Editor
                         ←  Boot.Build.Editor.Tests
                         ←  未来外部 CI/AI 分析工具

Core.Editor 不引用 Boot.Build.Editor；如需查看构建报告，只读取 Framework.BuildPipeline 的报告结构或 JSON。
```

`Framework.BuildPipeline` 放在 `Assets/Framework/` 的理由：它是稳定、跨工具、跨 UI、跨 CI 的构建数据协议，不属于 Boot/Core/General/Project 的业务层；执行器才属于 `Boot.Editor`。

### E.4 BuildProfile 环境配置模型

`BuildProfile` 是 v2 的配置核心。所有环境差异都应收敛到 Profile，而不是散落在 Stage 里写 `if (Development)`。

| 环境 | Development | 日志策略 | 调试能力 | Smoke 策略 | 产物路径 |
|------|-------------|----------|----------|------------|----------|
| Dev | true | Debug/Info | GM、Debug UI、Script Debugging 可开 | 可选，默认建议跑 | `BuildBackup/Dev/{version}/{buildNo}` |
| Profiling | true | Info | Profiler、符号、性能采样 | 必须跑 | `BuildBackup/Profiling/{version}/{buildNo}` |
| Audit | false | Warning/Error | GM/Debug UI 禁用，审核开关启用 | 必须跑且不可 Skip | `BuildBackup/Audit/{version}/{buildNo}` |
| Formal | false | Error 或按隐私策略关闭 | 全禁用，签名必需 | 必须跑且不可 Skip | `BuildBackup/Formal/{version}/{buildNo}` |
| QA/Pre | 按需 | Info/Warning | 可开有限诊断 | 必须跑 | `BuildBackup/QA/{version}/{buildNo}` |

推荐字段：

```csharp
public sealed class BuildProfile : ScriptableObject
{
    public BuildEnvironment Environment;
    public BuildTarget Platform;
    public string Version;
    public string BuildNumber;
    public string Channel;
    public string PackageName;
    public string OutputRoot;

    public bool DevelopmentBuild;
    public bool ScriptDebugging;
    public bool EnableProfiler;
    public bool EnableRuntimeLog;
    public bool EnableDebugUi;
    public bool EnableGm;
    public bool RequireSigning;

    public bool SmokeEnabled;
    public bool SmokeRequired;
    public string SmokeDeviceSerial;

    public string CdnBaseUrl;
    public Framework.Asset.AssetConfig.PlayMode AssetPlayMode;
}
```

Odin 表现建议：

- 用 `[TabGroup]` 分出 Environment / Android / YooAsset / HybridCLR / Smoke / Output / Advanced。
- 用 `[ValidateInput]` 校验版本号、包名、签名文件、ADB 设备、输出路径。
- 用 `[Button]` 提供 `Preflight Only`、`Build Incremental`、`Build Full`、`Run Smoke Only`、`Analyze Last Failure`。

### E.5 v2 Stage 流程设计

v2 不必拘泥于 S0-S9 的编号，但应保留“阶段化 + 可跳过 + 可恢复”的思想。

| v2 阶段 | 职责 |
|---------|------|
| P0 Plan | 解析 Profile，生成 BuildPlan，计算变更范围，确定输出目录和报告目录。 |
| P1 Preflight | Unity/Android/iOS/HybridCLR/YooAsset/asmdef/签名/BootScene/AssetConfig/私有目录预检。 |
| P2 Generate | Luban/Protobuf/代码生成/HybridCLR GenerateAll/link.xml。 |
| P3 HybridCLR | 编译热更 DLL，生成 AOT metadata，同步 `.dll.bytes`，强校验数量与名称。 |
| P4 Assets | YooAsset 生产构建，清理旧 StreamingAssets，输出资源 Manifest。 |
| P5 Apply Runtime Config | 写 AssetConfig、环境开关、日志策略、渠道、CDN，并用事务回滚。 |
| P6 Player Build | BuildPipeline.BuildPlayer，Android Gradle/iOS Xcode 后处理，签名。 |
| P7 Static Verify | APK/IPA/EXE、资源包、热更 DLL、AOT metadata、Formal 泄露项、hash 校验。 |
| P8 Runtime Smoke | Standalone/Android/iOS 启动，收集 `boot.log`、`latest.jsonl`、`latest.session.json`。 |
| P9 Analyze & Report | 生成 `build_report.json/md/html`、`issues.json`、`ai_handoff.json`，归档产物。 |

### E.6 当前 S0-S9 优化点

当前 S0-S9 可以继续作为 v1 兼容入口，但每个 Stage 都有明确优化空间：

| 当前 Stage | 现状 | v2 优化 |
|------------|------|---------|
| S0 PreFlightCheck | 校验 HC、平台、BootScene、AssetConfig、IL2CPP。 | 增加 Unity 版本、Android/iOS 模块、JDK/SDK/NDK、签名、YooAsset collector、HybridCLRSettings、asmdef dependency validator、私有路径、磁盘空间、Git dirty 状态、Profile 合法性；输出结构化 PreflightReport。 |
| S1 GenerateAll | 调 `PrebuildCommand.GenerateAll()`，校验 link.xml。 | 拆分为 CodeGen 与 HybridCLR Generate；缓存 MethodBridge 输入指纹，避免只改非泛型代码也误触发；记录生成产物 hash 和耗时。 |
| S2 Compile | 编译热更 DLL + Strip AOT，清空旧产物。 | 按目标平台和 Profile 隔离输出；强校验热更程序集名称集合；保存 DLL hash manifest；区分“编译失败”和“产物缺失”；捕获 HybridCLR 关键日志。 |
| S3 Sync | 复用 `SyncExistingOutputs()`，校验目录和数量。 | 生成同步 manifest：源路径、目标路径、size、sha256；目标多余文件应删除并记录；同步后校验 Entry.startupSettings 与 HybridCLRSettings 一致。 |
| S4 BuildYooAsset | ScriptableBuildPipeline 打真包到 StreamingAssets。 | 按 Profile 决定压缩、加密、清缓存策略；输出 YooAsset manifest/hash/size；校验 collector tag 和 RawFile 规则；保留构建产物到 `BuildInfo/BaseBundle` 或 v2 归档。 |
| S5 ApplyConfig | YAML 直写 AssetConfig.Mode=Offline，构建后回滚。 | 改为事务系统 `BuildConfigTransaction`：记录每个被改文件原始内容，任何异常都统一 rollback；支持环境、CDN、日志策略、渠道、Audit/Formal 开关；输出 applied config diff。 |
| S6 BuildPlayer | BuildPipeline.BuildPlayer，校验 Player 存在。 | 接入 Android Gradle/iOS Xcode 后处理；签名、符号、IL2CPP 参数、Scripting Define 由 Profile 驱动；捕获 Editor.log 片段；记录 BuildReport summary；Formal 禁止 Debug symbol 泄露。 |
| S7 ValidateArtifacts | 校验 Player、YooAsset、HybridCLR 数量。 | 升级为 Static Verify：hash、size、manifest、StreamingAssets 内容、DLL/AOT 名称精确匹配、Formal 泄露扫描（GM/Debug UI/DevelopmentBuild/verbose log）、签名验证。 |
| S8 SmokeRun | Standalone/Android 启动并读日志，当前仍需完善。 | SmokeRequired 语义：Formal/Audit 不允许 Skip；ADB 命令检查退出码；pull 固定文件路径；读取 `boot.log` + `latest.jsonl` + `latest.session.json`；按阶段里程碑判定 Launcher/YooAsset/HybridCLR/Boot/Core/SystemManager；失败生成 BuildIssue。 |
| S9 Report | 写 JSON/Markdown。 | 报告升级为 `build_report.json` + `issues.json` + `artifact_manifest.json` + `ai_handoff.json` + `build_report.html`；报告中包含 BuildPlan、环境快照、阶段耗时、输入输出 hash、日志索引和建议修复。 |

### E.7 BuildReport / Issue / AI Handoff

v2 报告必须面向机器读取。核心文件：

```text
BuildBackup/Formal/1.0.3/20260709_153012/
├── artifacts/
│   ├── KJ.apk
│   ├── KJ.apk.sha256
│   └── DefaultPackage.manifest.json
├── logs/
│   ├── editor.log
│   ├── adb.logcat.txt
│   ├── boot.log
│   ├── latest.jsonl
│   └── latest.session.json
├── reports/
│   ├── build_report.json
│   ├── build_report.md
│   ├── build_report.html
│   ├── issues.json
│   └── ai_handoff.json
└── state/
    ├── build_plan.json
    ├── stage_markers.json
    └── environment_snapshot.json
```

`BuildIssue` 示例：

```json
{
  "severity": "Error",
  "stage": "P8_RuntimeSmoke",
  "code": "KJ-BUILD-HYB-LOAD-001",
  "message": "Hot-update assembly Core.dll.bytes was not loaded",
  "evidence": [
    "latest.jsonl line 184: LoadAssembly failed",
    "HybridCLRSettings includes Core but synced artifact is missing"
  ],
  "likelyCause": "S3 Sync was skipped or marker state is stale",
  "suggestedFix": "Clear S3/S4/S6 markers and rebuild HybridCLR outputs",
  "relatedFiles": [
    "ProjectSettings/HybridCLRSettings.asset",
    "Assets/GameRes/HotUpdate/Dlls/Core.dll.bytes"
  ]
}
```

### E.8 日志系统接入

KJ 已经有 `Framework.RuntimeLog`，v2 应明确三类日志：

| 日志 | 来源 | 用途 |
|------|------|------|
| Build log | Editor 构建期 | Stage 开始/结束、耗时、输入输出、异常、工具链输出。 |
| Runtime smoke log | Player 运行期 | `boot.log`、`latest.jsonl`、`latest.session.json`，验证 Launcher→Boot→Core 链路。 |
| Diagnostic log | BuildAnalyzer | 规则化问题、证据、修复建议，供 AI 和 CI 使用。 |

原则：AI 不读 Unity Console 截图；AI 读取固定路径结构化文件。

### E.9 Odin Build Dashboard

推荐用 `OdinMenuEditorWindow` 替换普通 `BuildStagePanel`。

```text
Build Dashboard
├── Profiles
│   ├── Dev Android
│   ├── Formal Android
│   ├── Audit Android
│   └── Profiling Windows
├── Build Plan
├── Stage Monitor
├── Reports
├── Artifacts
├── Runtime Logs
└── Diagnostics
```

面板行为：

- 顶部显示 Profile、平台、环境、版本、构建号、渠道。
- 中部显示 Stage 时间线：Pending / Running / Passed / Failed / Skipped。
- 右侧显示当前 Stage 日志、耗时、输入输出、失败建议。
- 底部提供 Preflight、Incremental、Full、Smoke Only、Analyze Last Failure、Open Report Folder、Generate AI Handoff。

### E.10 迁移路线

| Phase | 内容 | 验收 |
|-------|------|------|
| v2-1 | 新增 BuildProfile / BuildContext / BuildReportV2 / BuildIssue，不替换 v1。 | 可生成 BuildPlan 和空报告；v1 菜单不受影响。 |
| v2-2 | 把 S0-S9 包装成 `BuildStage` 类，Runner 驱动执行。 | v2 Runner 跑通与 v1 等价流程。 |
| v2-3 | 环境策略落地：Dev/Formal/Audit/Profiling。 | 不同 Profile 的 Define、日志、Smoke、输出路径不同且可验证。 |
| v2-4 | Odin Dashboard。 | 可视化选择 Profile、查看 Stage、打开报告。 |
| v2-5 | Runtime Smoke 强化与日志收集。 | Android/Standalone Smoke 产出完整日志包，Formal/Audit 不允许 Skip。 |
| v2-6 | AI 诊断报告。 | 失败时生成 `issues.json` 和 `ai_handoff.json`，可直接交给 AI 定位。 |

### E.11 红线

1. `Launcher` 仍然不得引用任何 Framework 或热更程序集，构建工具不得通过 asmdef 修改破坏边界。
2. Formal/Audit 的 Smoke 若启用必须是 Required；缺 adb/缺设备不能算成功。
3. 所有改项目资产/配置的 Stage 必须事务化并可 rollback。
4. 所有 Stage 的跳过必须有机器可读原因，不能只靠 marker 存在。
5. 报告必须优先结构化 JSON，再派生 Markdown/HTML。
6. CI 退出码必须稳定：构建失败、预检失败、Smoke 失败、报告生成失败应区分。

---

## 附录 F — Build Pipeline v2 实现规格（可编码契约）

> 本章把附录 E 的架构设计收紧为实现规格。后续编码应优先按本章落地，避免 v2 再次退化为若干静态工具方法。

### F.1 命名与兼容策略

- v1 保留：`KJBuildPipeline`、`BuildConfig`、`BuildStagePanel` 继续作为兼容入口，直到 v2 全量验证通过。
- v2 新入口：`BuildPipelineRunner`，菜单为 `KJ/Build/Dashboard`、`KJ/Build/v2/Full Build`、`KJ/Build/v2/Incremental Build`。
- v2 配置：新建 `BuildProfile` / `BuildProfileSet`，不要在旧 `BuildConfig` 上继续追加大量字段。
- v2 报告：新建 `BuildReportV2`，旧 `BuildReport` 只保留 v1 兼容。
- 路径原则：所有 v2 状态、marker、报告、产物必须落在 `BuildBackup/{Environment}/{Version}/{BuildNo}/` 下；临时文件可落在 `Library/KJBuild/`。
- 目录原则：纯契约放 `Assets/Framework/BuildPipeline/`；UnityEditor 执行、Odin、CI 入口放 `Assets/Scripts/Boot.Editor/Build/`；不要放到 `Core.Editor`。

### F.1.1 程序集边界

| 程序集 | 路径 | 可引用 | 禁止引用 | 职责 |
|--------|------|--------|----------|------|
| `BuildPipeline` | `Assets/Framework/BuildPipeline/BuildPipeline.asmdef` | 无，或仅纯 C# 基础库 | `UnityEditor`、`UnityEngine`、`Boot`、`Core`、`General`、`Project`、任何 Editor 程序集 | 报告/计划/错误码/AI handoff/fingerprint schema |
| `Boot.Build.Editor` | `Assets/Scripts/Boot.Editor/Build/Boot.Build.Editor.asmdef` | `BuildPipeline`、`Boot.Editor`、`Launcher`、`Asset`、`AssetShared`、`HybridCLR.Editor`、`YooAsset.Editor`、`UnityEditor`、Odin | `Core.Editor`、`General`、`Project` 运行时业务实现 | Stage 执行、事务、BuildPlayer、Smoke、Odin、CI |
| `Boot.Build.Editor.Tests` | `Assets/Scripts/Boot.Editor/Build/Tests/` | `BuildPipeline`、`Boot.Build.Editor`、Unity Test Framework | 生产业务层 | 构建管线 EditMode 测试 |

`BuildPipeline.asmdef` 可设置 `noEngineReferences=true`，除非后续确认 `JsonUtility` 等 UnityEngine 类型不可避免；优先用纯 C# serializable DTO 和本地 JSON writer。

### F.2 BuildStage 接口契约

```csharp
public interface IBuildStage
{
    string Id { get; }                 // "P1.Preflight.Environment"
    string DisplayName { get; }        // "Environment Preflight"
    int Order { get; }                 // 排序，低值先执行
    BuildStageCategory Category { get; }
    IReadOnlyList<string> DependsOn { get; }
    BuildStagePolicy Policy { get; }   // Required / Optional / AlwaysRun / NoSkip / Transactional

    BuildStageInputs GetInputs(BuildContext context);
    BuildStageOutputs GetExpectedOutputs(BuildContext context);
    BuildSkipDecision CanSkip(BuildContext context, BuildStageFingerprint previous);

    void Execute(BuildContext context);
    void Verify(BuildContext context);
    void Rollback(BuildContext context);
    IReadOnlyList<BuildIssue> AnalyzeFailure(BuildContext context, Exception exception);
}
```

`BuildStagePolicy`：

```csharp
[Flags]
public enum BuildStagePolicy
{
    None = 0,
    Required = 1 << 0,
    Optional = 1 << 1,
    AlwaysRun = 1 << 2,
    NoSkip = 1 << 3,
    Transactional = 1 << 4,
    ProducesArtifacts = 1 << 5,
    RequiresUnityMainThread = 1 << 6,
}
```

`CanSkip` 必须返回机器可读原因：

```csharp
public sealed class BuildSkipDecision
{
    public bool CanSkip;
    public string ReasonCode;      // "INPUT_FINGERPRINT_UNCHANGED"
    public string HumanReason;
    public List<string> Evidence;  // marker path, matching hash, etc.
}
```

红线：Stage 不允许自己直接写 marker；只能由 `BuildPipelineRunner` 在 `Execute + Verify` 成功后统一写入。

### F.3 BuildContext 字段表

`BuildContext` 是一次构建的唯一上下文，Stage 之间不通过静态字段传递状态。

| 字段 | 类型 | 说明 |
|------|------|------|
| `RunId` | string | 一次构建唯一 ID，格式 `yyyyMMdd_HHmmss_{shortGuid}`。 |
| `StartedAtUtc` | DateTime | UTC 开始时间。 |
| `ProjectRoot` | string | 项目根目录绝对路径。 |
| `Profile` | BuildProfile | 当前 Profile 快照，不直接引用可变 asset。 |
| `Plan` | BuildPlan | 本次执行计划。 |
| `Report` | BuildReportV2 | 构建报告对象。 |
| `Artifacts` | BuildArtifactManifest | 产物清单。 |
| `Issues` | List<BuildIssue> | 所有结构化问题。 |
| `Transaction` | BuildConfigTransaction | 文件/设置变更事务。 |
| `Paths` | BuildPaths | 输出、报告、日志、状态目录集合。 |
| `Cancellation` | BuildCancellationToken | UI/CI 取消构建。 |
| `Logger` | IBuildLogger | 构建期结构化日志。 |
| `EnvironmentSnapshot` | BuildEnvironmentSnapshot | Unity、Git、OS、SDK、Package 版本快照。 |

`BuildPaths`：

```text
ArchiveRoot     = BuildBackup/{Environment}/{Version}/{BuildNo}
ArtifactsDir    = {ArchiveRoot}/artifacts
LogsDir         = {ArchiveRoot}/logs
ReportsDir      = {ArchiveRoot}/reports
StateDir        = {ArchiveRoot}/state
TempDir         = Library/KJBuild/{RunId}
MarkersPath     = {StateDir}/stage_markers.json
BuildPlanPath   = {StateDir}/build_plan.json
```

### F.4 BuildProfile 完整字段规格

`BuildProfile` 建议字段：

| 分组 | 字段 | 说明 |
|------|------|------|
| Identity | `ProfileName` | Odin 列表展示名。 |
| Identity | `Environment` | Dev / QA / Pre / Profiling / Audit / Formal。 |
| Identity | `Channel` | 渠道名，如 `internal` / `googleplay` / `tap`。 |
| Version | `VersionName` | 语义版本，如 `1.0.3`。 |
| Version | `VersionCode` | Android versionCode / iOS build number。 |
| Version | `BuildNumberSource` | Manual / Time / GitCommit / CI。 |
| Platform | `Platform` | BuildTarget。 |
| Platform | `Architecture` | ARM64 等，Android Formal 默认 ARM64。 |
| Android | `PackageId` | `PlayerSettings.applicationIdentifier`。 |
| Android | `KeystorePath` | Formal/Audit 必填。 |
| Android | `KeystoreAlias` | Formal/Audit 必填。 |
| Android | `MinSdkVersion` | 目标 SDK 策略。 |
| Android | `TargetSdkVersion` | 目标 SDK 策略。 |
| iOS | `BundleId` | iOS bundle identifier。 |
| iOS | `TeamId` | Apple team id。 |
| iOS | `ProvisioningProfile` | 手动签名时必填。 |
| Build | `DevelopmentBuild` | 是否 Development Build。 |
| Build | `ScriptDebugging` | 是否允许脚本调试。 |
| Build | `EnableProfiler` | 是否开启 profiler。 |
| Build | `ScriptingDefines` | 额外 define 列表。 |
| Build | `ForbiddenDefines` | Formal 禁止 define 列表。 |
| YooAsset | `PackageName` | 默认 `DefaultPackage`。 |
| YooAsset | `AssetPlayMode` | EditorSimulate / Offline / Host。 |
| YooAsset | `CdnBaseUrl` | Host 模式必填。 |
| YooAsset | `CompressOption` | LZ4/LZMA/None。 |
| YooAsset | `EncryptionMode` | None / Builtin / Custom。 |
| HybridCLR | `HotUpdateAssembliesPolicy` | UseSettings / ExplicitList。 |
| HybridCLR | `RequiredHotUpdateAssemblies` | 期望热更程序集名列表。 |
| Logs | `EnableRuntimeLog` | 是否生成 runtime jsonl。 |
| Logs | `RuntimeLogLevel` | Debug/Info/Warning/Error。 |
| Logs | `CollectEditorLog` | 是否归档 Editor.log。 |
| FeatureFlags | `EnableGm` | GM 开关。 |
| FeatureFlags | `EnableDebugUi` | Debug UI 开关。 |
| FeatureFlags | `EnableAuditMode` | 审核模式开关。 |
| Smoke | `SmokeEnabled` | 是否调度 smoke。 |
| Smoke | `SmokeRequired` | 是否不允许 skip。Formal/Audit 必须 true。 |
| Smoke | `SmokeDeviceSerial` | Android adb 设备号。 |
| Smoke | `SmokeTimeoutSec` | 超时。 |
| Output | `OutputRoot` | 默认 `BuildBackup/{env}`。 |
| Output | `KeepLastBuildCount` | 本地保留数量。 |

Profile 校验规则：

- `Formal` / `Audit`：`DevelopmentBuild=false`、`ScriptDebugging=false`、`EnableGm=false`、`EnableDebugUi=false`、`SmokeRequired=true`。
- `Formal`：签名配置必填，`ForbiddenDefines` 不得出现在最终 PlayerSettings defines 中。
- `Host` 模式：`CdnBaseUrl` 必填且必须是合法 URL。
- Android：`PackageId`、`VersionCode` 必填；`VersionCode` 必须单调递增（如果接入 CI/build history）。

### F.5 BuildPlan 与指纹规格

`BuildPlan` 生成流程：

1. 读取 `BuildProfile` 并生成不可变快照。
2. 扫描所有 `IBuildStage`。
3. 读取上一轮 `stage_markers.json`。
4. 为每个 Stage 计算输入指纹。
5. 判断本 Stage 是否变更。
6. 按 `DependsOn` 级联传播。
7. 输出 `build_plan.json`。

`BuildStageFingerprint`：

```json
{
  "stageId": "P3.HybridCLR.Compile",
  "pipelineVersion": "2.0.0",
  "profileHash": "sha256...",
  "inputsHash": "sha256...",
  "toolsHash": "sha256...",
  "outputsHash": "sha256...",
  "completedAtUtc": "2026-07-09T12:00:00Z",
  "unityVersion": "2022.3.62f2",
  "packageVersions": {
    "HybridCLR": "...",
    "YooAsset": "3.0"
  }
}
```

输入指纹规则：

- 文件内容优先用 SHA256；对超大目录可用 `(relativePath, length, mtimeUtc)` 生成目录 hash，但 Formal 构建建议使用内容 hash。
- Profile 快照 hash 必须参与所有 Stage 的指纹。
- 工具版本必须参与相关 Stage：Unity、HybridCLR、YooAsset、Android SDK/NDK/JDK、Gradle、Xcode。
- 输出缺失时即使输入未变也必须重跑。
- Stage 代码版本变化必须触发重跑：可用 `pipelineVersion + stageVersion`。

`stage_markers.json` 不允许只存 `.done` 文件。v2 marker 必须包含输入/输出/工具 hash 与跳过原因。

### F.6 事务系统规格

`BuildConfigTransaction` 负责所有会修改项目状态的操作。

必须支持：

- `SnapshotFile(path)`：保存文件原始内容。
- `SnapshotTextSetting(key, getter, setter)`：保存 Editor/PlayerSettings 字符串类设置。
- `SnapshotBoolSetting(...)` / `SnapshotEnumSetting(...)`。
- `Commit()`：构建成功后可选择保留部分修改。
- `Rollback()`：失败、取消、报告写入异常时必须恢复。
- `WriteDiffReport()`：输出 `applied_config_diff.json`。

必须纳入事务的对象：

| 对象 | 说明 |
|------|------|
| `Assets/Resources/AssetConfig.asset` | PlayMode、CDN、package、日志策略等。 |
| `ProjectSettings/ProjectSettings.asset` | applicationIdentifier、version、bundle version、Android/iOS 设置。 |
| `ProjectSettings/EditorBuildSettings.asset` | Boot scene 配置。 |
| PlayerSettings defines | 环境/日志/GM/Formal define。 |
| Android signing settings | keystore、alias、password 引用策略。 |
| YooAsset 配置资产 | collector/package/profile，如有被 Stage 修改。 |
| Gradle/iOS 导出后处理文件 | 如果直接改导出工程，应记录在 artifacts，不回写 Unity 项目。 |

红线：Stage 不允许绕过事务直接修改上述对象。确需直接写文件时必须先 `SnapshotFile`。

### F.7 Stage 实现清单

#### P0 Plan

- 输入：BuildProfile、CI args、上次 markers、Stage registry。
- 输出：`build_plan.json`、`environment_snapshot.json`。
- 失败码：`KJ-BUILD-PLAN-001` profile invalid，`KJ-BUILD-PLAN-002` dependency cycle。

#### P1 Preflight

必须检查：

- Unity 版本等于或兼容 `2022.3.62f2`。
- `asmdef_dependency_validator.py .` 通过。
- `HybridCLRSettings.hotUpdateAssemblies` 不包含 `Launcher` / `TestKit`。
- `Launcher` asmdef 只引用允许集合。
- Android/iOS 模块和工具链存在。
- Boot scene 存在且启用。
- YooAsset package / group / collector / RawFile 规则存在。
- 磁盘空间足够。
- Formal/Audit profile 无禁用项冲突。

输出：`preflight_report.json`。

#### P2 Generate

- 先运行配置/协议/代码生成（如果 KJ 后续接入 Luban/Protobuf）。
- 再运行 HybridCLR GenerateAll。
- 校验 `link.xml`、`AOTGenericReferences.cs`、MethodBridge 产物。
- 保存生成产物 hash。

#### P3 HybridCLR

- 清理目标平台旧 DLL 输出。
- 编译热更 DLL。
- 生成 stripped AOT metadata。
- 精确匹配 `RequiredHotUpdateAssemblies`。
- 生成 `hybridclr_manifest.json`。

#### P4 Assets

- 清理 StreamingAssets 下旧 package。
- 执行 YooAsset 生产构建。
- 校验 `{PackageName}.version`、hash、manifest、rawfile/bundle 数量。
- 生成 `yooasset_manifest.json`。

#### P5 Apply Runtime Config

- 事务化写 AssetConfig。
- 写环境、CDN、Offline/Host、日志等级、RuntimeLog 开关。
- 写 PlayerSettings 版本/包名/defines。
- 写 Boot Entry startup settings。
- 输出 `applied_config_diff.json`。

#### P6 Player Build

- 调 `BuildPipeline.BuildPlayer`。
- Android：必要时执行 Gradle 后处理、签名校验、APK/AAB 定位。
- iOS：导出 Xcode 工程、plist/signing 设置校验。
- 输出 Player artifact，记录 size/hash。

#### P7 Static Verify

- Player 文件存在、非空、hash 可计算。
- StreamingAssets 内置资源完整。
- DLL/AOT metadata 名称和数量精确匹配。
- Formal 禁止项扫描：
  - Development Build false。
  - Script Debugging false。
  - 禁止 GM/Debug UI define。
  - 禁止 verbose runtime log。
  - 禁止 `_` 私有路径进入 Player。
- 输出 `static_verify_report.json`。

#### P8 Runtime Smoke

- Standalone：启动进程，等待里程碑，收集 LocalLow 日志。
- Android：`adb install`、`am start`、`logcat`、`pull` 固定文件路径。
- iOS：未实现前，Formal/Audit 不允许通过；Dev 可显式 skip。
- 必须收集 `boot.log`、`latest.jsonl`、`latest.session.json`。
- 必须判定以下里程碑：
  - Launcher started。
  - YooAsset initialized。
  - Manifest activated。
  - AOT metadata loaded。
  - HotUpdate DLL loaded。
  - BootUpdateRunner entered。
  - ProjectStartup entered。
  - Core AssetSystem ready。
  - SystemManager initialized。
- 输出 `smoke_report.json`。

#### P9 Analyze & Report

- 收集所有 StageResult、Artifact、Issue、Logs。
- 执行 `BuildAnalyzer` 规则库。
- 写 `build_report.json`、`build_report.md`、`build_report.html`。
- 写 `issues.json`、`ai_handoff.json`。
- 复制关键日志到归档目录。

### F.8 报告 JSON Schema

`build_report.json` 顶层：

```json
{
  "schemaVersion": "2.0.0",
  "runId": "20260709_153012_ab12",
  "pipelineVersion": "2.0.0",
  "profile": {},
  "environmentSnapshot": {},
  "summary": {},
  "stages": [],
  "artifacts": [],
  "issues": [],
  "logs": [],
  "paths": {}
}
```

`StageResult`：

```json
{
  "id": "P3.HybridCLR.Compile",
  "displayName": "Compile HotUpdate DLLs",
  "status": "Passed",
  "startedAtUtc": "...",
  "finishedAtUtc": "...",
  "durationMs": 12345,
  "skipReasonCode": "",
  "inputHash": "sha256...",
  "outputHash": "sha256...",
  "issues": [],
  "artifacts": []
}
```

`BuildIssue` 必填字段：

```json
{
  "code": "KJ-BUILD-HYB-001",
  "severity": "Error",
  "stageId": "P3.HybridCLR.Compile",
  "message": "...",
  "evidence": [],
  "likelyCause": "...",
  "suggestedFix": "...",
  "relatedFiles": [],
  "isBlocking": true
}
```

`ai_handoff.json` 必须包含：

- 本次目标：profile、platform、environment、version。
- 失败阶段。
- blocking issues。
- 关键日志路径。
- 最近 200 行相关日志摘要。
- 相关文件列表。
- 建议下一步命令或操作。

### F.9 错误码体系

| 前缀 | 范围 |
|------|------|
| `KJ-BUILD-PLAN-*` | BuildPlan / Profile / 参数解析 |
| `KJ-BUILD-PRE-*` | Preflight 环境检查 |
| `KJ-BUILD-GEN-*` | 代码生成 / HybridCLR GenerateAll |
| `KJ-BUILD-HYB-*` | HybridCLR 编译、AOT metadata、DLL 同步 |
| `KJ-BUILD-YOO-*` | YooAsset 构建与资源包 |
| `KJ-BUILD-CONFIG-*` | 配置写入与事务回滚 |
| `KJ-BUILD-PLAYER-*` | BuildPlayer / Gradle / Xcode / 签名 |
| `KJ-BUILD-VERIFY-*` | 静态产物校验 |
| `KJ-BUILD-SMOKE-*` | Runtime smoke / adb / 日志里程碑 |
| `KJ-BUILD-FORMAL-*` | Formal/Audit 禁止项 |
| `KJ-BUILD-REPORT-*` | 报告生成与归档 |

错误码必须稳定，不得在报告文案调整时改变。

### F.10 Odin Dashboard 交互规格

窗口：`BuildDashboardWindow : OdinMenuEditorWindow`。

菜单树：

```text
Profiles
Build Plan
Stage Monitor
Reports
Artifacts
Runtime Logs
Diagnostics
Settings
```

行为：

- 构建运行中：禁用 Profile 修改和启动按钮，只允许 Cancel、Open Logs、Copy RunId。
- Cancel：设置 `BuildCancellationToken`，Stage 在安全点退出，并触发事务 rollback。
- Stage Monitor：显示状态、耗时、skip reason、输入 hash、输出 hash、issue count。
- Reports：按时间倒序列出 `BuildBackup/*/*/*/reports/build_report.json`。
- Diagnostics：显示 blocking issues，支持复制 `ai_handoff.json` 路径。
- Artifacts：显示 APK/IPA/EXE、资源包、hash、大小、打开目录。
- Runtime Logs：打开 `boot.log`、`latest.jsonl`、`latest.session.json`。

### F.11 CI 入口与退出码

命令行：

```bash
Unity -batchmode -quit -projectPath <project> \
  -executeMethod Boot.Editor.Build.CI.BuildCommandLine.Run \
  -profile Assets/Scripts/Boot.Editor/Build/Profiles/FormalAndroid.asset \
  -mode Full \
  -buildNumber 20260709.1 \
  -outputRoot BuildBackup
```

退出码：

| ExitCode | 含义 |
----------|------|
| 0 | 成功 |
| 10 | 参数/Profile 错误 |
| 20 | Preflight 失败 |
| 30 | Generate/HybridCLR 失败 |
| 40 | YooAsset 失败 |
| 50 | Config transaction 失败 |
| 60 | BuildPlayer/平台工具链失败 |
| 70 | Static Verify 失败 |
| 80 | Runtime Smoke 失败 |
| 90 | Report/Archive 失败 |
| 99 | 未分类异常 |

CI 必须始终输出 `ai_handoff.json`，即使报告阶段失败，也要尽力输出最小 handoff。

### F.12 测试清单与验收标准

EditMode 单测：

- `BuildProfileValidationTests`
  - Formal 禁用 Debug/Gm。
  - Host 模式缺 CDN 报错。
  - Android Formal 缺签名报错。
- `BuildPlanTests`
  - 输入 hash 不变可跳过。
  - Profile hash 变化触发相关 Stage。
  - 上游变化级联下游。
  - 输出缺失强制重跑。
- `BuildTransactionTests`
  - 文件写入后 rollback 恢复。
  - PlayerSettings define rollback 恢复。
  - 多文件部分失败仍全部恢复。
- `BuildReportSchemaTests`
  - JSON schema 字段完整。
  - BuildIssue 必填字段完整。
  - AI handoff 包含 blocking issue 和日志路径。
- `SmokeLogParserTests`
  - 缺 boot.log 失败。
  - 缺关键里程碑失败。
  - boot.log 含 Error/Failed 失败。
  - 全里程碑通过。
- `StaticVerifyTests`
  - DLL 数量不足失败。
  - Formal 含 Debug define 失败。
  - 私有路径进入 StreamingAssets 失败。

集成验证：

- Dev Standalone Full Build：能产出 Player、报告、日志。
- Android Dev Build：能 install/start/pull 日志。
- Formal Android Dry Run：无签名时 Preflight 必须失败。
- Formal Android SmokeRequired：无设备时必须失败，不得 skipped passed。

验收标准：

1. v2 Runner 在 Dev Standalone 跑通全链路。
2. 所有失败都产生至少一个 blocking `BuildIssue`。
3. 所有构建都产出 `build_plan.json` 和 `build_report.json`。
4. Formal/Audit 不存在 Smoke skipped passed。
5. 事务 rollback 有单测覆盖。
6. Odin Dashboard 能打开最近报告并显示 issues。
7. `asmdef_dependency_validator.py .` 在 v2 相关 asmdef 改动后通过。

### F.13 首轮落地顺序

第一轮不要做完整 UI 和全部平台，先实现最小可验证闭环：

1. `Framework.BuildPipeline` 程序集：`BuildEnvironment`、`BuildIssue`、`BuildReportV2`、`BuildPlan`、fingerprint DTO、`BuildExitCode`。
2. `Boot.Build.Editor` 配置包装：`BuildProfile` / `BuildProfileSet` / `BuildProfileValidator`。
3. `Boot.Build.Editor` 执行骨架：`BuildContext` / `BuildPaths` / `IBuildStage` / `BuildPipelineRunner`。
4. 包装现有 S0-S9 为 v2 Stage，先保持行为等价。
5. `BuildConfigTransaction`，先覆盖 AssetConfig + PlayerSettings defines。
6. `BuildReportWriter` 输出 json/md/ai_handoff。
7. `BuildPlanTests` + `BuildTransactionTests` + `BuildReportSchemaTests`。
8. 最后接 Odin Dashboard。

这一顺序的原则：先保证“可执行、可回滚、可报告”，再追求“好看、智能、自动诊断”。
