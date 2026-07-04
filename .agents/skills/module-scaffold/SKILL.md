---
name: module-scaffold
description: >
  KJ项目目录结构规范。用于判断代码应该放在 Assets/Scripts/{Boot|Core|General|Project}、
  Assets/Framework/ 还是 Assets/GameRes/ 的哪一层。
  触发场景：新建或移动任何 .cs 文件、新建模块/系统/功能、讨论目录结构、
  代码审查中涉及命名空间或 asmdef 引用、询问"这段代码放哪里"、
  写 ObjectPool/Cache/UI/Net/Audio/Config/Localization/Timer/RedDot/Guide 等任何新模块、
  新增日志门面/日志模板/日志配置时先参考 kj-log、
  创建 ScriptableObject 或 Prefab 等资源时决定路径。
  核心规则：命名空间=目录路径，不带项目名前缀；Framework 不引用 Scripts。
metadata:
  doc: .planning/目录结构规范.md
---

# 模块脚手架规范

完整规范加载 `.planning/目录结构规范.md`。

## 速查

- 命名空间 = 目录路径，**永远不带** `KJ.` 前缀
- 纯逻辑无依赖 → `Framework/`；需要引用 Core → `Scripts/Core/`
- 启动期 DI/Stage 放各层 `Bootstrap/`；Core 生命周期协议和 `SystemManager` 放 `Core/Systems/`
- 通用业务 → `Scripts/General/`；项目专属 → `Scripts/Project/`
- 资源按类型+层放 `GameRes/{类型}/{层}/`
- 场景放 `Assets/GameRes/Scene/{Layer}/`；`Assets/Resources/` 只放最小启动配置（如 `AssetConfig.asset`）
- `XxxLog.cs` 日志源生成模板跟随 `Xxx.cs` 所属模块目录；`Core/Logging/` 只放日志管线/桥接；日志规则详见 `kj-log`
- ⚠️ asmdef 文件名当前仍带 `KJ.` 前缀（如 `KJ.Boot.asmdef`），后续迁移为不带前缀（如 `Boot.asmdef`）。`rootNamespace` 已正确设置为不带前缀的名称。新建 asmdef 时请直接用不带前缀的文件名。
