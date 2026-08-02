# 代码规范 — nameof 与硬编码字符串

> 2026-08-01 | 强制使用 `nameof` 替代直接写字符串的场景

## 红线

- **GameObject / 资源名**：`new GameObject(nameof(XxxScope))` 而非 `new GameObject("XxxScope")` —— 用类型名作为对象名时用 `nameof`，改名时 IDE 自动同步。
- **反射类型字符串**：`Type.GetType(nameof(BootUpdateRunner))` 只适用于同类引用；**跨程序集反射入口**（如 `"Boot.BootUpdateRunner, Boot"`）是程序集限定名契约，`nameof` 无法表达完整限定名，**保留字符串 + 契约测试**，不强行改。
- **日志模块名 / category**：`module: nameof(XxxLog)` 而非硬编码 `"Xxx"`，避免类名重构后日志标签失联。
- **magic string 判断**：代码里 if 比较字符串常量，优先提取 `const` 或枚举，不要散落字面量。

## 例外

- 程序集限定名反射契约（见上）。
- 第三方 API 要求的固定字符串（如 YooAsset/HybridCLR 的固定 key）。
- 对外协议 / 序列化字段名（保持与配置/文档一致）。

## 参照

- 本规则适用于所有 `Assets/Scripts/` 运行时代码；Editor 工具同理。
