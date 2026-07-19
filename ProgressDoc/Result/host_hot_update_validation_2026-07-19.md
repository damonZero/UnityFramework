# Host 热更验证状态（2026-07-19）

## 结论

当前尚未完成 Android Host 热更闭环验证。

- Android Host 基线 APK 已成功生成。
- P6 Player 与 P7 Verify 已通过。
- 本次 Host 基线入口只执行 P4 验证、P5 配置、P6 Player、P7 校验，**没有重复执行 P2 GenerateAll/MethodBridge**。
- APK 已安装到 MuMu `127.0.0.1:7555` 并成功启动进程。
- 运行时尚未到达 `[AssetSystem] Ready`、`[SystemManager] 全部初始化完成`。
- 1.0.1 资源/DLL 尚未完成“同一 APK 下载并加载新版本”的验证，因此不能宣称热更新成功。

## 已完成事项

### 构建

Host 基线构建日志：

```text
Logs/HostBaselineHttpAllowed.log
```

关键结果：

```text
[P6] BuildPlayer: DONE
[P7] Verify: ALL CHECKS PASSED
[KJBuildPipeline] Host baseline Player build succeeded without P2/MethodBridge.
```

APK 输出：

```text
BuildBackup/Dev/1.0.0/1/KJ.apk
```

### 服务器

本地 Host 服务器目录：

```text
Server/CDN/Android/DefaultPackage
```

Windows 侧已确认可以访问 `DefaultPackage.version`。MuMu 使用的地址为：

```text
http://10.0.2.2:8080/CDN/Android/DefaultPackage
```

### 运行时问题演进

1. 初始运行时被 Unity 明文 HTTP 策略阻止：`Insecure connection not allowed`。
2. 已将开发验证用 `ProjectSettings/ProjectSettings.asset` 的 `insecureHttpOption` 调整为允许 HTTP，并补充 Android Manifest 合并配置。
3. HTTP 阻塞消失后，暴露出真正的 YooAsset 缓存问题：

```text
YooAsset load package manifest failed: System.IO.IOException: Read-only file system
```

错误发生在 `DownloadPackageHashOperation` 创建/写入缓存目录时，位置位于 `BootLoader.InitializeYooAsset` 的 Host sandbox 文件系统初始化与 manifest 加载阶段。

## 当前阻塞

当前阻塞不是 MethodBridge，也不是 APK 构建失败，而是 Android Host 模式的 YooAsset Sandbox 缓存目录不可写。

在该问题修复前，以下验收项均保持未通过：

- Host 版本检查与 manifest 下载
- Hot-update tag 下载
- 10 个热更 DLL 与 13 个 AOT metadata 的加载
- `[AssetSystem] Ready`
- `[SystemManager] 全部初始化完成`
- 同一 APK 从 1.0.0 更新到 1.0.1
- Project 新 DLL 标记的出现

## 下一步处理顺序

1. 检查 `BootLoader.BuildSandboxParameters` 与 YooAsset 3.0 `CreateDefaultSandboxFileSystemParameters` 的默认根目录，确认 Android 路径是否落到了只读位置。
2. 将 Sandbox/Cache 根目录显式设置到 `Application.persistentDataPath` 对应的可写目录；不要使用 StreamingAssets、APK 内置目录或其他只读路径作为下载缓存。
3. 重新执行 Host 基线 Player 构建。只需 P5-P7；不需要重新执行 P2/MethodBridge，除非热更程序集列表、AOT 泛型元数据需求或 MethodBridge 输入发生变化。
4. 安装 APK、清理应用数据后冷启动，确认日志至少包含：
   - `[BootLoader] YooAsset ready`
   - `[BootLoader] Hot-update files are current` 或下载完成日志
   - `[AssetSystem] Ready`
   - `[SystemManager] 全部初始化完成`
5. 记录当前 `ProjectBootstrapper` 基线日志标记。
6. 使用 `HostUpdatePublisher` 编译热更 DLL、同步已有 HybridCLR 输出、构建 YooAsset RawFile 1.0.1 并发布到 Server；该步骤不执行 GenerateAll、不执行 BuildPlayer。
7. 不重装 APK，只 force-stop/restart 同一个安装实例，确认请求 1.0.1、下载更新文件、加载新的 Project DLL 标记，并再次到达 SystemManager。

## 判定标准

只有在同一个已安装 APK 的两次启动之间同时满足以下条件，才记录“热更验证通过”：

- 首次启动基线版本成功完成完整生命周期；
- Server 提供 1.0.1 version/manifest 和热更文件；
- 第二次启动没有重新安装 APK；
- 日志显示 1.0.1 被检查/下载；
- 新 Project DLL 标记出现；
- 生命周期再次到达 `[AssetSystem] Ready` 与 `[SystemManager] 全部初始化完成`；
- 没有启动期异常。

在此之前，状态应标记为：**APK 构建通过，Host 运行时验证阻塞，热更闭环未通过**。
