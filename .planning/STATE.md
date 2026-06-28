# Project State: KJ Unity Framework

**Last Updated:** 2026-06-28
**Current Status:** 🔄 Phase 0 规划与重构准备中

## 进度

- [x] 需求讨论完成
- [x] 命名规范确定（Core=System，业务=Model）
- [x] FOUND-01: Boot Layer (Entry + GameLaunch)
- [x] FOUND-02: ISystem 接口 (ISystem + ITickableSystem)
- [x] FOUND-03: SystemManager
- [ ] DI-01: VContainer 接入方案
- [ ] DI-02: Boot 层最小依赖约束落地
- [ ] DI-03: 现有系统迁移到容器驱动注册

## 文件清单

```
Assets/Scripts/
├── Core/
│   ├── KJ.Core.asmdef          ← 最底层，无框架依赖
│   ├── ISystem.cs              ← ISystem + ITickableSystem 接口
│   └── SystemManager.cs        ← 系统生命周期管理器（单例）
├── Boot/
│   ├── KJ.Boot.asmdef          ← 引用 KJ.Core
│   ├── Entry.cs                ← 启动入口 MonoBehaviour
│   └── GameLaunch.cs           ← 注册系统 + InitAll
├── General/
│   └── KJ.General.asmdef       ← 引用 KJ.Core（空，待用）
└── Project/
    └── KJ.Project.asmdef       ← 引用 KJ.Core + KJ.General（空，待用）
```

## 下一步

Phase 0 / Phase 1 前置事项：
- DI-01: 设计 VContainer 接入边界
- DI-02: 收敛 Boot 层依赖，确保热更新时不强制重启
- DI-03: 制定 SystemManager 到容器注册的迁移顺序
- FOUND-04: EventSystem（事件系统）
- RES-01: ResourceSystem（资源管理）
- RES-02: AssetHandle（句柄式 API）
- UI-01: UISystem（UI 管理）
- UI-02: UIWindow 基类

---
*Phase 0 重构准备: 2026-06-28*
