# Project State: KJ Unity Framework

**Last Updated:** 2026-06-26
**Current Status:** 🔄 重新开始 — 增量开发模式

## 开发方式

一个模块一个模块地开发：**写代码 → 编译通过 → 测试通过 → 提交 → 下一个**

## 模块开发顺序

| # | 模块 | 程序集 | 包含需求 | 状态 |
|---|------|--------|----------|------|
| M1 | IModule 接口 | KJ.Core | FOUND-02 | ⬜ 待开始 |
| M2 | ModuleManager | KJ.Core | FOUND-03 | ⬜ 待开始 |
| M3 | EventManager | KJ.Core | FOUND-04 | ⬜ 待开始 |
| M4 | ResourceManager | KJ.Core | RES-01, RES-02 | ⬜ 待开始 |
| M5 | Boot 层 | KJ.Boot | FOUND-01 | ⬜ 待开始 |
| M6 | ObjectPoolManager | KJ.Core | RES-03 | ⬜ 待开始 |
| M7 | UIManager + UIWindow | KJ.Core | UI-01~04 | ⬜ 待开始 |
| M8 | Network | KJ.Core | NET-01~06 | ⬜ 待开始 |
| M9 | TimerManager | KJ.Core | TIMER-01~03 | ⬜ 待开始 |
| M10 | ConfigManager | KJ.General | CFG-01~03 | ⬜ 待开始 |
| M11 | LocalizationManager | KJ.General | L10N-01~03 | ⬜ 待开始 |
| M12 | HotUpdateManager | KJ.Boot | HOT-01~03 | ⬜ 待开始 |

## 当前进度

**当前模块:** M1 — IModule 接口
**完成:** 0/12

## Decisions Made

- UI: UGUI
- Config: Luban (open source)
- Protocol: Protobuf
- Hot Update: HybridCLR (needed)
- **开发方式: 增量开发，逐模块验证**
- **不提前引入外部依赖，用 #if 或空实现预留**

## 文件清单

```
Assets/Scripts/
├── (待创建)
```

---
*v1 增量开发重启: 2026-06-26*
