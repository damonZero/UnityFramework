# Host 热更验证状态（2026-07-20 更新）

## 结论

当前尚未完成 Android Host 热更闭环验证。但基础设施已大幅改进：

- **YooAsset "Read-only file system" 已修复**（BUGFIX-05: `BootLoader.cs`/`AssetRuntime.cs` `packageName` 不再错误地当 `packageRoot`）
- **AOT 启动链全部通过**（BootLoader→YooAsset→HybridCLR metadata/DLL→BootUpdateRunner→ProjectBootstrapper）
- **ZLogger AOT 泛型实例化有架构级修复**（PLATFORM-03: `SimpleLogger<T>` 落地 Core 层，`CoreContainerRegistration` 已改用 `SimpleLogger<>` 替代 `Logger<>`）
- **Dashboard 中文化** + 热更补丁发布按钮 + 设备安装勾选就绪（PLATFORM-02）
- **构建管线日志统一**（PLATFORM-01: `BuildLogger` 替换全部 `Debug.Log*`）
- **`.gitignore` 更新**（PLATFORM-04），生成产物不再混入版本库

## AOT 泛型实例化修复详解

### 问题

`[ZLoggerMessage]` 源生成代码调用 `ILogger.Log<TState>()`。VContainer 以 `Microsoft.Extensions.Logging.Logger<T>`（AOT 侧 DLL 类）作为 `ILogger<T>` 实现。`Logger<T>` 包装的非泛型 `Logger` 内部调 `Logger.Log<TState>`（也在 AOT 侧 DLL），HybridCLR 无法穿透 AOT 程序集的泛型实例化路径。设备端报：

```
MissingMethodException: Microsoft.Extensions.Logging.Logger::Log<Core.Systems.SystemManagerLog+InitStartState>(...)
```

### 修复

`Core/Logging/SimpleLogger<T>` — 在热更 Core 程序集中直接实现 `ILogger<T>`。`Log<TState>` 方法将 state 通过 formatter 展开为字符串，调非泛型 `ILogger.Log(string)`。泛型在热更程序集内自行消化，不再跨越 AOT 边界。CoreContainerRegistration 已改用 `typeof(SimpleLogger<>)` 替代 `typeof(Logger<>)`。

### 验证状态

待一键打包（含 P2 GenerateAll）→ 设备验证 MissingMethodException 消除。

## 后续步骤

1. **一键打包** → 生成包含 `SimpleLogger<T>` 的新 APK
2. **安装设备验证**：
   - MissingMethodException 消失
   - JSONL 日志 ≥ seq:11（应出现 `AssetSystem.Init` / `SystemManager.Init` 等）
   - `[AssetSystem] Ready` 出现
   - `[SystemManager] 全部初始化完成` 出现
3. **热更验证**：
   - 记录基线 Project DLL 标记
   - 修改 Project 层代码 → 点「发布热更补丁」→ 输入 1.0.1
   - 不重装 APK，force-stop/restart 同一安装实例
   - 确认设备请求 1.0.1 → 下载热更文件 → 加载新 Project DLL
   - 再次到达完整启动链，无 Error

## 判定标准（不变）

同时满足以下条件才记录"热更验证通过"：
1. 首次启动到达 `[AssetSystem] Ready` + `[SystemManager]` 无 Error
2. Server 提供 1.0.1 version/manifest + 热更文件
3. 第二次启动无重装 APK
4. 日志显示 1.0.1 被检查/下载
5. 新 Project DLL 标记出现
6. 再次到达 AssetSystem Ready + SystemManager
7. 无启动期异常

在此之前状态标记为：**AOT 链通过，ZLogger SimpleLogger<T> 修复待验证，热更闭环未通过**。
