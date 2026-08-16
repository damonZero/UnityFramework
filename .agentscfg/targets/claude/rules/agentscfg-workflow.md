# 指令 / 规则 / 技能写入规范（agentscfg 单一事实源）

> 2026-08-16 | 工具：`npx agentscfg`（配置 `.agentscfg/agentscfg.jsonc`，清单 `.agentscfg/.managed.json`）

## 铁律：只改 `.agentscfg/` 源，不改目标

`.agentscfg/` 是单一事实源，`npx agentscfg` 同步到各 AI 工具目标（`claude` → `.claude/`，`codex` → `.codex/`）。目标文件是生成的，直接改会被下次同步覆盖/丢失。

| 内容 | 写这里（源） | 不要写这里（目标） |
|---|---|---|
| 主指令 | `.agentscfg/instructions/BASE.md`（+ `PROJECT.md` 若有） | `CLAUDE.md` / `AGENTS.md` |
| 规则 | `.agentscfg/targets/claude/rules/*.md` | `.claude/rules/*.md` |
| 技能 | `.agentscfg/skills/*/SKILL.md` | `.claude/skills/` / `.codex/skills/` |
| 设置 | `.agentscfg/targets/claude/settings.local.json` | `.claude/settings.local.json` |

受管清单（`.managed.json` 的 `managed`）：`.claude/**`、`.codex/**`、`.mcp.json`、`.opencode/**`、`AGENTS.md`、`CLAUDE.md` —— 全部是同步产物。

## 流程

1. 改 `.agentscfg/` 源。
2. 跑 `npx agentscfg` 同步（生成 CLAUDE.md / AGENTS.md、拷贝 rules/skills/settings 到目标）。

## 新增规则文件时

同时写两处，保证同步前立即可用、同步后内容一致：
1. 源：`.agentscfg/targets/claude/rules/<name>.md`
2. 目标：`.claude/rules/<name>.md`（`cp` 同内容）

⚠️ `.claude/rules/naming-nameof.md` 是历史遗留（只写了目标、没写源），新增规则**不要**学它。

## 补充：BASE.md 里引用了规则才需同步到 CLAUDE.md

内联在 BASE.md 里「详见 `.claude/rules/xxx.md`」的引用，要等 `npx agentscfg` 重新生成 `CLAUDE.md`/`AGENTS.md` 后才会出现在主指令里；而规则文件本身（`.claude/rules/*.md`）由 Claude Code 自动加载，两者是独立机制。
