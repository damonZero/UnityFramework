---
name: module-scaffold
description: >
  KJ项目目录结构规范。用于判断代码应该放在 Assets/Scripts/{Boot|Core|General|Project}、
  Assets/Framework/ 还是 Assets/GameRes/ 的哪一层。
  触发场景：新建或移动任何 .cs 文件、新建模块/系统/功能、讨论目录结构、
  代码审查中涉及命名空间或 asmdef 引用、询问"这段代码放哪里"、
  写 ObjectPool/Cache/UI/Net/Audio/Config/Localization/Timer/RedDot/Guide 等任何新模块、
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
- 通用业务 → `Scripts/General/`；项目专属 → `Scripts/Project/`
- 资源按类型+层放 `GameRes/{类型}/{层}/`
