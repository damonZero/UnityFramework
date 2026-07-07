# Project Instructions

## Project Overview

KJ is a Unity 2022.3.62f2 client game framework using VContainer, MessagePipe,
YooAsset, HybridCLR, and Luban.

For the complete technical stack, read `.planning/PROJECT.md`.
For directory structure rules, read `.planning/目录结构规范.md`.
For AI-readable runtime logs and diagnostic workflow, read `.planning/AI_RUNTIME_LOGGING.md`.

## Session Startup

At the start of each session, or when receiving a vague task that does not name
specific files:

1. Read `.planning/STATE.md` to understand current progress and important files.
2. Read `.planning/ROADMAP.md` to understand module status and pending work.
3. Use both files to determine the current context and suggest the next practical step.

When there is no explicit instruction, default to recommending the most reasonable
next module from `ROADMAP.md`, based on dependency readiness and complexity.

## Architecture

- This project is a pure C# Unity project.
- Use the current C# architecture direction around VContainer and MessagePipe.
- Do not load or reference any P33 Lua/MVVM skills, conventions, or files for this repository.
- Treat Lua/P33 guidance as unrelated legacy context unless the user explicitly asks for it.
- All stable low-level adapters and shared foundation modules must live directly under
  `Assets/Framework/`, not `Assets/Framework/Package/`.

## Dependency Direction

Assembly dependencies must stay one-way:

```text
Boot <- Core <- General <- Project
```

Upper layers may depend on lower layers; lower layers must not depend on upper layers.

- `Packages`: third-party UPM packages such as VContainer, UniTask, MessagePipe,
  and YooAsset. Packages may reference each other as allowed by Unity/package rules.
- `Framework`: KJ-owned independent packages such as `Asset/`, `Event/`, `Pool/`, and `Cache/`.
  Framework code may depend on Packages only. It must not reference code under
  `Assets/Scripts`. If project capability is needed, expose static delegates and
  bridge them from Core.
- `Boot`: startup shell with minimal dependencies, typically VContainer only.
  It must not reference Core, General, or Project.
- `Core`: engine infrastructure. It may reference Boot, Framework, and Packages.
  It must not reference General or Project.
- `General`: reusable business logic. It may reference Core and Packages.
  It must not reference Project.
- `Project`: project-specific business logic. It may reference all lower layers.

Compilation boundaries are enforced by `.asmdef` files.

## HybridCLR Hot-Update Boundary (HYB-03 implemented)

The boot chain is split into an AOT `Launcher` shell and a hot-update `Boot`
update flow.

- **AOT shell `Launcher` (`KJ.Launcher.asmdef`)**: only locates and loads hot-update
  code. References are limited to `UniTask`, `YooAsset`, `HybridCLR.Runtime`,
  `AssetShared`. Hard rule: it must not reference any `Framework.*` package or
  hot-update assembly (enforced by asmdef). It calls the hot-update entry via the
  reflection string `"Boot.BootUpdateRunner, Boot"`, never by a compile-time
  dependency on Boot.
- **Hot-update `Boot` (`KJ.Boot.asmdef`)**: startup update orchestration (version
  check, download, retry, update UI). References `Asset`, `Log`, `RuntimeLog`,
  `UniTask`, `AssetShared`, `YooAsset`, `Launcher`. No longer references
  `HybridCLR.Runtime` (the AOT shell loads it on Boot's behalf).
- **10 hot-update assemblies**: `Boot, Core, General, Project, Pool, Cache, Event,
  Asset, Log, RuntimeLog` (single source of truth is
  `ProjectSettings/HybridCLRSettings.asset` `hotUpdateAssemblies`).
- **`Framework.AssetShared` (AOT-shared)**: holds `AssetConfig` / `AssetConstants`
  (namespace stays `Framework.Asset`) so both the AOT shell and hot-update layer
  can reference them across the boundary.
- **AOT-stage logging**: `BootStartupLog` (plain text + in-memory), independent of
  `Framework.Log` / `RuntimeLog`; replayed into the RuntimeLog session by
  `BootUpdateRunner.ReplayEarlyLogs()` after the hot-update layer initializes.
- **Reflection entry contract**: `BootLoader` resolves the entry using the literal
  string `"Boot.BootUpdateRunner, Boot"`. The assembly name is part of the boot
  contract; renaming requires updating both `BootLoader` and `HybridCLRSettings`.

When changing the hot-update boundary: edit `HybridCLRSettings.asset` first, then
confirm `KJHybridClrBuildTools.ValidateRuntimePreloadAssemblyName` blocklist
(currently `{Launcher, TestKit}`). Never add a Framework or hot-update reference to
`Launcher`.

## AI Runtime Diagnostics

- Runtime debugging should use generated log files as the default evidence.
- Prefer reading `Logs/Runtime/latest.jsonl` and `latest.session.json` when investigating Editor/Player runtime issues, once LOG-AI runtime support exists.
- Do not ask the user for Unity Console screenshots when AI-readable runtime logs are available.
- New logging infrastructure must follow `.planning/AI_RUNTIME_LOGGING.md`: JSON Lines logs, session manifest, Boot/Core boundary safety, no sensitive data in logs.

## Namespaces

Namespace equals directory path and never includes a project prefix.

| Directory | Namespace |
| --- | --- |
| `Scripts/Boot/` | `Boot` |
| `Scripts/Core/` | `Core` |
| `Scripts/General/` | `General` |
| `Scripts/Project/` | `Project` |
| `Framework/` | `Framework.Asset`, `Framework.Event`, `Framework.Pool`, `Framework.Cache`, etc. |

Never write `namespace KJ.*`.

## Naming Conventions

- Core layer: `[CoreSystem]` + `ISystem`, managed by `SystemManager`.
- General and Project layers: `[Model]` + `IModel`, managed by `ModelLifecycle`.
- Business layers should not use `System` naming.
- Do not use `Module` naming.

## New Modules

When creating or moving modules, systems, features, C# files, ScriptableObjects,
or prefabs, use the local `module-scaffold` skill if available, or read
`.planning/目录结构规范.md`.
