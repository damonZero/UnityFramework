# 1External 第三方本地库规范

> 2026-07-09 | 定位: `Assets/Framework/1External/` 存放不通过 UPM 管理的第三方本地库（预编译 DLL 或 Editor-only 源码包），与 Framework 自研代码严格隔离

## 选择: 1External vs Packages (UPM)

- **UPM**: 标准 UPM 包、纯 C# 逻辑、频繁更新 → `Packages/manifest.json`
- **1External**: 无 UPM 包、需本地 patch、Editor-only 插件、预编译 DLL → `1External/{Vendor}/{Plugin}/`

## 目录结构

```
1External/{Vendor}/{Plugin}/
├── {Plugin}.Editor.asmdef    # Editor-only, autoReferenced: false
├── Assemblies/
│   ├── NoEditor/             # Runtime DLL（Editor + Player 变体）
│   ├── NoEmitAndNoEditor/    # 仅构建用 DLL
│   └── link.xml              # IL2CPP 裁剪保留
├── Plugins/                  # 原生插件 (.aar/.framework/.jar)
└── Resources/                # 必需资源（尽量少放）
```

## 引用隔离红线

| 允许引用 | 禁止引用 |
|----------|----------|
| Unity 内置程序集 | Packages (UPM) |
| Unity Package (Mathematics/UI 等) | Framework/（KJ 自研） |
| 同目录其他 1External 子目录 | Scripts/（Core/General/Project/Boot） |

- Editor-only: `includePlatforms: ["Editor"]`, `autoReferenced: false`
- Runtime + Editor 拆分两个 asmdef，Runtime 不引用 Editor
- 预编译 DLL 用 `precompiledReferences`，不在 `references` 中声明

## 添加新库清单

1. 确认放 1External 还是 UPM
2. 创建 `1External/{Vendor}/{Plugin}/` 目录
3. 放入 DLL/源码 + `.meta`
4. 创建 asmdef: name 不含项目前缀，references 仅 Unity 内置 + 同目录第三方
5. 验证 asmdef references 不含项目程序集 GUID
6. IL2CPP 需要时添加 `link.xml`
