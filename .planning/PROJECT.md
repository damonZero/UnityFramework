# KJ Unity Framework

## What This Is

一套通用的 Unity 客户端项目框架，适用于各类游戏开发。采用分层架构（Boot/Core/General/Project），模块化设计，支持 Protobuf 网络通信、Luban 配置表、HybridCLR 热更新。服务器后期可用 C# 实现，当前先聚焦客户端框架搭建。

## Core Value

模块化、可复用的客户端框架，一个模块一个模块搭建并验证，确保每个模块独立可用、稳定可靠。

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] 项目基础结构搭建（Boot/Core/General/Project 分层）
- [ ] 事件系统（EventManager - 同步/异步、优先级、owner 管理）
- [ ] 网络模块（NetMgr/Session - 会话管理、消息收发）
- [ ] Protobuf 通信协议集成
- [ ] 配置表系统（Luban 集成）
- [ ] UI 框架（UGUI 封装 - 窗口管理、层级管理）
- [ ] 资源管理模块（AssetManager - 加载/卸载/缓存）
- [ ] HybridCLR 热更新集成
- [ ] 对象池系统
- [ ] 红点系统
- [ ] 引导系统
- [ ] 音频管理模块

### Out of Scope

- 服务器端实现 — 先聚焦客户端，服务器后期用 C# 实现
- 具体游戏业务逻辑 — 框架层不含业务代码
- 多平台适配细节 — 先 PC/移动端通用，平台特殊处理后续扩展

## Context

- Unity 版本：2022.3.62f2 LTS
- 开发者背景：熟悉公司 Erlang 服务器架构的 Unity 框架（Boot/Core/General/Project 分层）
- 公司框架特点：模块化设计（IModule）、事件驱动（EventManager）、会话管理（NetMgr/Session）、配置表驱动
- 新框架需要：保留架构思想，简化实现，用 C# 生态替代 Erlang 生态

## Constraints

- **Tech Stack**: Unity 2022.3 LTS + C# — 长期支持版本，稳定性优先
- **网络协议**: Protobuf — 高性能、跨语言，后期服务器切换无感
- **配置表**: Luban — 开源成熟方案，策划友好
- **热更新**: HybridCLR — 业界标准 C# 热更方案
- **搭建方式**: 逐模块搭建并验证 — 确保每个模块独立可用

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 分层架构 Boot/Core/General/Project | 参考公司成熟框架，职责清晰 | — Pending |
| UGUI 而非 FairyGUI | Unity 原生，简单稳定，无额外依赖 | — Pending |
| Luban 配置表 | 开源成熟，支持多语言导出，策划友好 | — Pending |
| Protobuf 通信 | 高性能二进制，跨语言，后期服务器切换无感 | — Pending |
| HybridCLR 热更新 | 业界标准 C# 热更方案，社区活跃 | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-26 after initialization*
