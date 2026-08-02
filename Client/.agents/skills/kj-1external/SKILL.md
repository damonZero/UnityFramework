---
name: kj-1external
description: >
  KJ Framework 第三方本地库管理指南。涵盖 1External 目录的定位（非 UPM 第三方库存放）、与 Packages (UPM) 的选择标准、添加新库的操作流程（目录结构 + asmdef 配置 + 编译隔离验证）、asmdef 模板（Runtime / Editor-only）、已有第三方库（如 Odin Inspector）的维护规则。
  触发场景：添加新的非 UPM 第三方库、更新已有本地库版本、排查 1External 程序集引用问题、判断第三方库应该放 1External 还是 UPM。
  核心规则：1External 只能引用 Unity 内置程序集和同目录第三方库；禁止引用任何项目程序集（Scripts/）、Framework/ 自研模块和 Packages (UPM)；asmdef 使用 `precompiledReferences` 引用预编译 DLL；Editor-only 插件必须设置 `includePlatforms: ["Editor"]` + `autoReferenced: false`。
metadata:
  doc: 1external.md
  layer: Framework
---

# KJ 第三方本地库管理 (1External)

详细规范见 `.Codex/rules/1external.md`。

## 1External vs UPM 选择标准

| 条件 | 放哪里 |
|------|--------|
| 有官方/社区 UPM 包 | Packages（UPM） |
| 纯 C# 逻辑，可独立编译 | Packages（UPM） |
| 无 UPM 包可用 | **1External** |
| Editor-only Unity Asset Store 插件 | **1External** |
| 预编译 DLL + 少量源码 | **1External** |
| 需要本地 patch/定制 | **1External** |

## 已有库

| 库 | 类型 | 说明 |
|----|------|------|
| **Sirenix / Odin Inspector** | Editor-only | 编辑器增强插件，`Assemblies/` 预编译 DLL + `Odin Inspector/` 编辑器资源 |

## 添加新库步骤

### 1. 创建目录结构

```
1External/{Vendor}/
└── {Plugin}/
    ├── Assemblies/                 # DLL + .pdb + .xml
    │   ├── NoEditor/               # Runtime DLL
    │   └── NoEmitAndNoEditor/     # AOT 专用 DLL
    ├── Plugins/                    # 原生插件（如有）
    └── {Plugin}.Editor.asmdef     # Editor-only 程序集（如有源码）
```

### 2. 配置 asmdef

#### 纯 DLL（无源码）— 不需要 asmdef

如果第三方库完全是预编译 DLL 且通过 `Assets/Plugins/` 或 asmdef 的 `precompiledReferences` 引用，可以不创建独立 asmdef。但为了编译隔离和 Editor 平台限定，建议创建。

#### Editor-only 插件模板

```json
{
    "name": "External.{Vendor}.{Plugin}.Editor",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [
        "{Plugin}.dll",
        "{Plugin}.Editor.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 3. 验证编译隔离

检查清单：
- [ ] asmdef `references` 中不包含项目 GUID（仅 Unity 内置和同目录第三方库）
- [ ] Editor-only 插件已设置 `includePlatforms: ["Editor"]`
- [ ] `autoReferenced: false`（避免被其他程序集自动引用）
- [ ] IL2CPP `link.xml` 已配置（如需要）
- [ ] `.meta` 文件齐全
- [ ] 不修改 `GUID` 导致引用断裂（如有）

### 4. 更新依赖方向文档

在 `.planning/目录结构规范.md` 的依赖方向图中补充 1External 的位置。

## 更新已有库

更新流程：
1. 备份当前版本（rename 为 `{Plugin}_backup_YYYYMMDD`）
2. 复制新版本所有文件
3. 对比新旧 asmdef，确认引用无新增项目程序集
4. 对比 `link.xml`，确认 IL2CPP 保留规则无遗漏
5. 在 Unity 中验证编译通过
6. 删除备份
