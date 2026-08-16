# KJ vs P37(int_37_pack) 启动架构对比分析

> 生成日期: 2026-07-31 | 聚焦: BootStartupSettings、HybridCLR 配置分离、DLL 加载策略

---

## 一、两项目概览

| 维度 | KJ | P37 (int_37_pack) |
|------|-----|-------------------|
| 代码规模 | ~100 文件（框架阶段） | ~6,600+ C# 文件（成熟商业项目） |
| 启动层位置 | `Assets/Scripts/Boot/` | `Assets/ScriptsC#/Boot/` |
| 资源加载 | YooAsset 3.0 | 自研 `UpdateMgr` + AssetBundle |
| 热更新方案 | HybridCLR | HybridCLR |
| DI 容器 | VContainer + MessagePipe | 自研 IoC（VContainer 历史版本） |
| 业务状态 | 框架搭建中，Login 未实现 | 完整商业游戏，Login/Role/Fight 全部就绪 |
| C# 文件数 | ~100 | ~3,842 (runtime + editor) |

---

## 二、核心对比：启动配置的存放方式

### 2.1 KJ 的做法：独立的 `BootStartupSettings`

**位置：** `Assets/Scripts/Boot/Launcher/Data/BootStartupSettings.cs`
**挂载：** 序列化在 `Entry` MonoBehaviour 上

```csharp
public class Entry : MonoBehaviour
{
    [SerializeField]
    private BootStartupSettings startupSettings = new BootStartupSettings();
    // ...
}
```

```csharp
[Serializable]
public sealed class BootStartupSettings
{
    // 运行时行为开关
    private bool enableAssetUpdate = true;
    private bool enableHotUpdate = true;
    private bool skipHotUpdateInEditor = true;

    // 入口契约
    private string startupTypeName = "Project.Bootstrap.ProjectStartup, Project";
    private string startupMethodName = "Start";

    // HybridCLR 运行时寻址
    private BootAssemblyEntry[] hotUpdateAssemblies;    // 10 项
    private BootMetadataEntry[] aotMetadataAssemblies;  // 13 项

    // 资源下载
    private string assetDownloadTag;
}
```

**每个 DLL 条目包含 4 个字段（以 Core 为例）：**

```csharp
new BootAssemblyEntry(
    assemblyName: "Core",                                       // Assembly.Load 用的程序集名
    fileName:     "Dlls/Core.dll.bytes",                       // 调试/日志标识
    resourcesPath: null,                                        // 旧方案残留
    assetPath:    "Assets/GameRes/HotUpdate/Dlls/Core.dll.bytes" // YooAsset 资源路径
)
```

**特点：**
- **类型安全** — 编译期可验证字段类型
- **集中管理** — 所有启动参数在一个对象中
- **Editor 工具自动回写** — `KJHybridClrBuildTools.ApplyToOpenEntry()` 从 `HybridCLRSettings.asset` 读取程序集名 → 拼接完整路径 → SerializedObject 写入
- **单一职责** — 纯粹的运行时数据，不含编译逻辑

---

### 2.2 P37 的做法：常量 + 清单文件 + Inspector 字段分散

P37 **没有** `BootStartupSettings` 这样的集中配置类。启动配置分散在三个地方：

#### 第一处：硬编码常量 — `BootEntry.cs`

```csharp
public class BootEntry : MonoBehaviour
{
    public const string STARTUP_ASSEMBLY = "Boot.Update";  // 硬编码
    private const string BOOT_CSHARP_TAG = "BOOT_CSHARP";  // 硬编码
    private const string CSHARP_SUFFIX = ".bytes";          // 硬编码
    private const string CSHARP_AOT = ".aot";               // 硬编码
    private const string STARTUP_TYPE = "Boot.Update.BootUpdate";  // 硬编码
    private const string STARTUP_METHOD = "BootStartup";    // 硬编码
    private const string UPDATE_DIR = "Updates";            // 硬编码
    public const string UPDATE_FILE = "update.txt";        // 硬编码
}
```

```csharp
// BootUpdate.cs 也有自己的硬编码常量
public static class BootUpdate
{
    public const string STARTUP_ASSEMBLY = "Core";           // 硬编码
    private const string STARTUP_TYPE = "Core.Main";        // 硬编码
    private const string STARTUP_METHOD = "Startup";        // 硬编码
}
```

#### 第二处：`update.txt` 清单文件决定 DLL 加载

```
// update.txt 格式（每行用 | 分隔）:
Assets/GameRes/HotUpdate/Dlls/Boot.Update.dll.bytes|...|...|BOOT_CSHARP
Assets/GameRes/HotUpdate/Dlls/Core.dll.bytes|...|...|GAME_CSHARP
Assets/GameRes/HotUpdate/AotMetadata/mscorlib.dll.aot|...|...|BOOT_CSHARP
Assets/GameRes/HotUpdate/AotMetadata/System.dll.aot|...|...|GAME_CSHARP
```

**这个 `update.txt` 是 P37 的核心清单文件**，它承载了 KJ 的 `BootStartupSettings.hotUpdateAssemblies` + `aotMetadataAssemblies` 的职责，同时还包含所有普通资源（prefab/贴图/音频等）的更新信息。

#### 第三处：Inspector 可配置的运行参数 — `UpdateFlowMgr.cs`

```csharp
public class UpdateFlowMgr : MonoBehaviour
{
    [Header("Boot阶段更新资源编号")] public int bootAssetIndex = 302030000;
    [Header("Boot阶段更新关键字")] public string bootAssetKey = "subBootOrder";
    [Header("提示更新大小")] public int hintDownSize = 10485760;      // 10MB
    [Header("静默更新大小")] public int directDownSize = 2097152;     // 2MB
    [Header("Boot阶段更新提示")] public string bootDownloadTips;
    [Header("Boot阶段Game更新提示")] public string bootGameDownloadTips;
    [Header("更新错误提示")] public string updateErrorTips;
    [Header("磁盘满提示")] public string diskFullTips;
    [Header("弱网异常")] public string bootWeakNetHint;
    [Header("低磁盘空间提示大小(MB)")] public float lowDiskSpaceSize;
    // ... 还有 10+ 个 Inspector 字段
}
```

**这些字段对应 KJ 的 `BootStartupSettings` 中的行为开关部分，但 P37 把它们分散在 `UpdateFlowMgr` 这个 MonoBehaviour 上，而不是一个独立的配置类。**

---

### 2.3 配置分离的本质原因

```
┌──────────────────────────────────────────────────────────────────┐
│                        KJ                                        │
│                                                                  │
│  HybridCLRSettings.asset (Editor only)                           │
│  ├─ hotUpdateAssemblies: [Boot, Core, General, ...]              │
│  └─ patchAOTAssemblies: [mscorlib, System, ..., ZLogger]         │
│         │                                                        │
│         ├──→ HybridCLR 编译工具链                                │
│         │                                                        │
│         └──→ KJHybridClrBuildTools.ApplyToOpenEntry()            │
│                  │                                                │
│                  ▼                                                │
│  BootStartupSettings (Runtime, 序列化在场景)                      │
│  ├─ hotUpdateAssemblies: [{Name, AssetPath}]                     │
│  ├─ aotMetadataAssemblies: [{Name, AssetPath}]                   │
│  ├─ enableHotUpdate / skipHotUpdateInEditor                      │
│  ├─ startupTypeName / startupMethodName                          │
│  └─ assetDownloadTag                                             │
│         │                                                        │
│         ▼                                                        │
│  BootLoader.RunAsync()  ← 运行时消费                             │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                        P37 (int_37_pack)                          │
│                                                                  │
│  HybridCLRSettings.asset (Editor only)                           │
│  ├─ hotUpdateAssemblies: [...]                                   │
│  └─ patchAOTAssemblies: [...]                                    │
│         │                                                        │
│         ├──→ HybridCLR 编译工具链                                │
│         │                                                        │
│         └──→ 构建管线写入 update.txt                             │
│                  │                                                │
│                  ▼                                                │
│  update.txt (Runtime, 文件系统)                                   │
│  ├─ 所有文件的清单（资源 + DLL + AOT metadata）                  │
│  └─ TAG 字段区分: BOOT_CSHARP / GAME_CSHARP / 普通资源          │
│         │                                                        │
│         ├──→ BootEntry.LoadBootCSharpFiles()                     │
│         │     └─ 只加载 TAG=BOOT_CSHARP 的 DLL/AOT              │
│         │                                                        │
│         └──→ BootUpdate.LoadRemainingCSharpFiles()               │
│               └─ 加载剩余 TAG=GAME_CSHARP 的 DLL/AOT            │
│                                                                  │
│  UpdateFlowMgr (MonoBehaviour, Inspector 可配)                    │
│  ├─ bootAssetIndex, hintDownSize, directDownSize                 │
│  ├─ 各种提示文字                                                 │
│  └─ 磁盘/网络阈值                                                │
│                                                                  │
│  硬编码常量 (BootEntry.cs / BootUpdate.cs)                       │
│  ├─ STARTUP_ASSEMBLY = "Boot.Update"                             │
│  ├─ STARTUP_TYPE = "Core.Main"                                   │
│  └─ CSHARP_SUFFIX = ".bytes"                                     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 三、DLL 加载策略对比

这是两项目**最核心的架构差异**。

### 3.1 KJ: 单阶段全量加载

```
BootLoader.RunAsync()
  ├─ InitializeYooAsset()         ← 先初始化资源系统
  ├─ DownloadHotUpdateAssetsAsync() ← Host 模式下载资源
  └─ LoadHotUpdateCodeAsync()     ← 一次性全部加载
       ├─ LoadAotMetadata()       ← 全部 13 个 AOT metadata
       └─ LoadHotUpdateAssemblies() ← 全部 10 个热更 DLL
            └─ Assembly.Load() × 10
                 ↓
       ReflectBootUpdateRunnerStart()  ← 交接给热更层
```

**所有 DLL 在进入热更层之前一次性全部加载完毕。** 热更层 `BootUpdateRunner` 不需要再加载任何 DLL，只做资源更新和启动游戏。

### 3.2 P37: 两阶段增量加载（BOOT_CSHARP → GAME_CSHARP）

```
阶段1: BootEntry.LoadBootCSharpFiles()
  ├─ 读取 update.txt
  ├─ 遍历，只处理 TAG == "BOOT_CSHARP" 的项
  │   ├─ *.dll.bytes → Assembly.Load()
  │   └─ *.dll.aot   → RuntimeApi.LoadMetadataForAOTAssembly()
  ├─ 找到 "Boot.Update" 程序集
  └─ 反射调用 BootUpdate.BootStartup(encryptType)
       │
       ├─ BootUpdate.BootStartup()
       │   ├─ InvokeLoadedRuntimeInitializeOnLoadMethods(BootCSharpLoaded)
       │   ├─ 修复更新缓存
       │   └─ Startup() → 更新管线（UI/服务器选择/资源下载）
       │
       └─ 更新完成后 → BootUpdateComplete()
            └─ LoadRemainingCSharpFiles()   ← 阶段2: 加载剩余 DLL
                 ├─ 读取 update.txt
                 ├─ 遍历，加载所有阶段1跳过的 DLL/AOT
                 │   ├─ *.dll.bytes (跳过 BootLoadedAssemblyFileNames)
                 │   └─ *.dll.aot   (跳过 BootLoadedAotFileNames)
                 └─ InvokeLoadedRuntimeInitializeOnLoadMethods(RemainingCSharpLoaded)
                      ↓
            StartupGame()  ← 反射 Core.Main.Startup()
```

**为什么 P37 要两阶段？**

| 原因 | 说明 |
|------|------|
| **启动速度** | BOOT_CSHARP 只包含更新流程需要的程序集（`Boot.Update` + 少量依赖），剩余 50+ DLL 延迟到资源下载完成后 |
| **更新UI需要** | 更新阶段要显示 UI（进度条、服务器选择、公告），这些依赖的 DLL 必须在更新前就绪 |
| **边玩边下** | 游戏已启动后还可后台下载剩余资源，此时 DLL 早已全部加载完毕 |
| **RuntimeInitialize** | HybridCLR 手动 `Assembly.Load` 不会自动触发 `[RuntimeInitializeOnLoadMethod]`，P37 必须分两阶段手动调用 |

KJ 当前只有 10 个 DLL，全部加载的代价很小，不需要分阶段。但 **P37 有 50+ 个热更程序集**，全部在启动阶段加载会显著增加耗时。

---

## 四、资源配置方式的根本差异

### 4.1 YooAsset vs 自研资源系统

| 维度 | KJ | P37 |
|------|-----|-----|
| 资源系统 | YooAsset 3.0 | 自研 `UpdateMgr` + AssetBundle |
| DLL 加载 API | `_package.LoadAssetSync<RawFileObject>(assetPath)` | `File.ReadAllBytes(path)` + 可选 `RemoteAssetLoader.LoadCsharp()` |
| 路径解析 | YooAsset 虚拟路径（`Assets/GameRes/HotUpdate/Dlls/Core.dll.bytes`） | 文件系统物理路径（`persistentDataPath/Updates/` 或 `streamingAssetsPath/`） |
| 清单格式 | YooAsset 内置 manifest | 自研 `update.txt`（`文件名|大小|MD5|TAG`） |
| PlayMode | EditorSimulate / Offline / Host | 正式版/内部版分流 |
| CDN 下载 | YooAsset `ResourceDownloader` | 自研 `UpdateDownloader`（HTTP Range 断点续传） |
| 加密 | 无 | `EncryptLib.DecryptBytes(bytes, EncryptType)` |

### 4.2 为什么这个差异影响配置设计

**KJ 必须用 `BootStartupSettings`（带 YooAsset assetPath）** 的原因是：

```csharp
// KJ 的 BootLoader 用 YooAsset API 加载 DLL
var handle = _package.LoadAssetSync<RawFileObject>(assetPath);
// assetPath 必须能直接传给 YooAsset:
//   "Assets/GameRes/HotUpdate/Dlls/Core.dll.bytes"
// 这个路径是 YooAsset Collector 配置决定的，不是简单的文件名拼接
```

**P37 不需要这样的配置**，因为它直接用文件系统路径：

```csharp
// P37 直接用物理路径读文件
var path = $"{Application.persistentDataPath}/Updates/{fileName}";
if (File.Exists(path))
    return File.ReadAllBytes(path);  // 不需要 YooAsset 的虚拟路径映射
```

所以 P37 的 `update.txt` 里只存 `fileName`（相对于 `Updates/` 目录的文件名），不需要完整的 YooAsset assetPath。

---

## 五、P37 有而 KJ 没有的特性

### 5.1 BootTypeMgr — 启动类型检测

```csharp
public enum BootType
{
    First = 0,      // 首次安装启动 — 无缓存，全量下载
    Normal = 1,     // 正常启动 — 使用已有缓存
    Cover = 2,      // 覆盖安装 — streamingAssetsPath 变化，强制从包内加载
    BootDllAbnormal = 3  // 上次 Boot DLL 异常 — 回退到包内版本
}
```

**判断逻辑（`BootTypeMgr.InitBootType()`）：**
1. 记录上次的 `Application.version` 和 `Application.streamingAssetsPath`
2. 本次启动时对比：
   - 两者都为 null → **首次安装**（`First`）
   - streamingAssetsPath 变化 → **覆盖安装**（`Cover`）
   - BootDllChange 标记为 true → **DLL 异常回退**（`BootDllAbnormal`）
   - 版本号相同 → **正常启动**（`Normal`）
3. 写入新值，供下次启动对比

**影响：** `LoadPkgAsset = (BootType == Cover || BootType == BootDllAbnormal)` → 强制从 StreamingAssets 加载原始的 DLL，不使用热更新版本。

**KJ 没有这个机制。** 如果热更 DLL 损坏，KJ 只能走到 `Entry.cs` 的全局 catch → 显示 Repair 按钮，但 Repair 只是重新走一遍 `RunStartupAsync()`，如果热更缓存未清除，可能陷入死循环。

### 5.2 软重启 / 硬重启

P37 有完整的分级重启机制：

| 重启类型 | 触发条件 | 行为 |
|----------|----------|------|
| `GameRestartType.Without` | 更新不涉及 C# / 启动资源 | 不重启 |
| `GameRestartType.InGame` | 更新了启动阶段资源（资源级） | `GameRestart.SoftRestart()` — 不退出进程，重载资源 |
| `GameRestartType.OutsideGame` | 更新了 C# DLL / AOT metadata | `GameRestart.HardRestart()` — 杀掉进程，重新启动 App |

**KJ 没有重启机制。** 当前设计是热更 DLL 只在启动阶段一次性加载，没有运行时热更 DLL 替换的场景。

### 5.3 RuntimeInitializeOnLoadMethod 手动触发

Unity 的 `[RuntimeInitializeOnLoadMethod]` 在正常 IL2CPP 构建中由引擎自动调用。但 **HybridCLR 通过 `Assembly.Load(byte[])` 加载的 DLL 不会自动触发这些方法**。

P37 的方案：
- 构建时生成 `HotFixRuntimeInitializeOnLoadMethod.cs`（索引文件，记录所有带 `[RuntimeInitializeOnLoadMethod]` 的方法）
- 分两阶段手动调用：Boot 阶段 DLL 加载后 → 调一次；Remain 阶段 DLL 加载后 → 再调一次
- 用 `_invokedRuntimeInitializeMethodIndexes` 去重

**KJ 没有处理这个问题。** 如果未来的热更 DLL 中有依赖 `[RuntimeInitializeOnLoadMethod]` 的第三方库（如 Unity 的 `InputSystem`、一些插件），它们不会自动初始化。

### 5.4 DLL 加密

```csharp
// P37: DLL 在磁盘上是加密的，加载时解密
var decryptData = EncryptLib.DecryptBytes(dllBytes, encryptType);
return Assembly.Load(decryptData);
```

加密类型从 `update.txt` 的配置段解析（`csharpEncrypt = BinaryXor`）。

**KJ 没有加密。** DLL 以原始 `.dll.bytes` 形式存储在 YooAsset 包中。

### 5.5 更新流程与登录动画衔接

P37 的 `UpdateFlowMgr.OnBootUpdateOver()` 中有显式的登录动画衔接逻辑：

```csharp
// 和登录动画的播放衔接上,避免动画首帧姿势不对或卡顿
CoroutineUtil.DelayFrame(BootScene.Instance.DelayShowFrame, bootUpdateComplete);
```

`BootScene` / `LoginShowNode` / `BaseLoginShowNode` 这些类负责启动闪屏和登录场景的过渡动画。

**KJ 没有这些** — 当前 `IBootStartupView` 只支持简单的进度条和状态文字更新。

### 5.6 正式版/内部版分流

```csharp
private static void StartupByEnv()
{
    if (AppConst.IsFormal())
        Formal();     // 直接开始更新
    else
        Internal();   // 显示服务器地址选择 + 可取消更新
}
```

内部版支持：
- `AddressSelectForm` — 服务器地址选择界面
- `CanCancelBootUpdate = true` — 开发时可取消更新流程
- `ServerConfigInternalMgr` — 内部服务器地址管理

**KJ 没有环境分流。** 当前只有单一的 `BootStartupSettings`，没有正式/内部的概念。

---

## 六、KJ 有而 P37 没有的特性

### 6.1 类型安全的启动配置

KJ 的 `BootStartupSettings` 是纯 C# 类，所有字段有明确的类型和默认值。P37 的大量字符串常量（`"Boot.Update"`, `"Core.Main"`, `"BootStartup"`）散布在代码中，IDE 无法提供重构支持。

### 6.2 Editor 工具自动同步

KJ 的 `KJHybridClrBuildTools.ApplyToOpenEntry()` 通过 SerializedObject 自动将编译产物路径写入场景中的 `Entry.startupSettings`。P37 的 `update.txt` 是构建管线直接写入文件系统，走的是完全不同的路径。

### 6.3 YooAsset 抽象的资源配置

KJ 的资源模式（EditorSimulate/Offline/Host）通过 `AssetConfig.PlayMode` 切换，不需要改代码。P37 的资源路径硬编码在 `BootEntry.GetFilePath()` 中：

```csharp
var path = $"{Application.persistentDataPath}/{UPDATE_DIR}/{fileName}";
if (File.Exists(path)) return path;
return Path.Combine(Application.streamingAssetsPath, fileName);
```

### 6.4 DI 容器驱动的生命周期

KJ 用 VContainer 的 `IStartable` → `IPostStartable` 驱动 `SystemManager.InitAll()` → `ModelLifecycle.LoadAll()`，类型扫描自动注册。P37 使用的是手动反射调用 `Core.Main.Startup()`，没有容器驱动的生命周期管理。

### 6.5 AOT 日志回放

KJ 的 `BootStartupLog → BootBridge.EarlyLogs → BootUpdateRunner.ReplayEarlyLogs()` 链路保证了 AOT 阶段的日志不丢失。P37 的 AOT 阶段直接用 `Debug.Log`，没有结构化的日志回放机制。

---

## 七、架构演进的关系

从架构对比可以看出，KJ 可以视为对 P37 的一次**重新设计**：

| P37 模式 | KJ 重构为 |
|----------|----------|
| 硬编码字符串常量 | `BootStartupSettings` 类型安全配置 |
| `update.txt` 文本清单 | YooAsset 结构化 manifest + `BootAssemblyEntry` / `BootMetadataEntry` |
| 文件系统路径直读 | YooAsset `LoadAssetSync<RawFileObject>` 虚拟路径 |
| `UpdateFlowMgr` Inspector 字段分散 | `BootStartupSettings` + `AssetConfig` 集中管理 |
| `BootEntry` + `BootUpdate` 手动两阶段反射 | `BootLoader` → `BootUpdateRunner` → `ProjectStartup` 清晰交接 |
| 无日志回放 | `BootStartupLog` → `ReplayEarlyLogs` 完整链路 |
| 手动 `Core.Main.Startup()` | VContainer DI 容器驱动生命周期 |

---

## 八、KJ 后续可借鉴 P37 的设计

| P37 特性 | 价值 | 复杂度 |
|----------|------|--------|
| **BootTypeMgr 启动类型检测** | 覆盖安装/首次安装/异常回退场景 | 低 |
| **两阶段 DLL 加载** | DLL 超过 20 个时启动速度优化明显 | 中 |
| **软重启/硬重启分级** | C# 热更后不杀进程（资源级）vs 必须杀进程（DLL 级） | 中 |
| **RuntimeInitialize 手动触发** | 第三方库兼容性 | 低 |
| **加密** | 保护 DLL 不被轻易逆向 | 中 |
| **正式/内部版分流** | 开发调试便利性 | 中 |
| **启动场景动画衔接** | 用户体验 | 低 |

**当前优先级最高的三项：**
1. **RuntimeInitialize 手动触发** — 以后引用更多第三方库时大概率会遇到
2. **BootTypeMgr 启动类型检测** — 覆盖安装、热更损坏等异常场景的容错基础
3. **分级重启** — 热更新上线后必然需要
