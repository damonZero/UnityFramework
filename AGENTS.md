# Project Instructions

## Project Overview

KJ is a Unity 2022.3.62f2 client game framework using VContainer, MessagePipe,
YooAsset, HybridCLR, and Luban.

For the complete technical stack, read `.planning/PROJECT.md`.
For directory structure rules, read `.planning/目录结构规范.md`.
For AI-readable runtime logs and diagnostic workflow, read `.planning/AI_RUNTIME_LOGGING.md`.
For build/packaging pipeline, read `ProgressDoc/Result/hybridclr_workflow.md` §4 and `ProgressDoc/Discuss/Hy3_构建打包全流程管线_需求分析与设计.md`.

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
- `Boot`: startup update shell (hot-update, `KJ.Boot.asmdef`) that only updates
  resources/code and reflects the hot-update entry. References `Asset, Log, RuntimeLog,
  UniTask, AssetShared, YooAsset, Launcher`; it must not reference VContainer,
  HybridCLR.Runtime, Core, General, or Project. The AOT `Launcher` shell
  (`KJ.Launcher.asmdef`) lives under `Boot/Launcher/` and references only
  `UniTask, YooAsset, HybridCLR.Runtime, AssetShared`.
- `Core`: engine infrastructure. It may reference Boot, Framework, and Packages.
  It must not reference General or Project.
- `General`: reusable business logic. It may reference Core and Packages.
  It must not reference Project.
- `Project`: project-specific business logic. It may reference all lower layers.

Compilation boundaries are enforced by `.asmdef` files.

## KJ Build Pipeline

The build pipeline (`Assets/Scripts/Boot.Editor/Build/`) packages and validates
the full Unity Player + YooAsset resource bundle + HybridCLR hot-update assets.

### Entry Points

| Menu | What it does |
|------|-------------|
| `KJ → Build → Full Player Build & Validate` | Clear all markers, run all stages |
| `KJ → Build → Incremental Player Build` | Change detection, only re-run changed stages |
| `KJ → Build → Build Stage Manager...` | Visual panel: auto-detect changes + manual checkboxes |
| `KJ → Build → Clear All Stage Markers` | Manually clear stage markers |

### Build Stages (S0–S9)

| Stage | Name | What it does |
|-------|------|-------------|
| S0 | Environment Check | Validate BuildConfig, JDK, NDK, Android SDK |
| S1 | Compile HotUpdate DLLs | HybridCLR `CompileDllCommand` + method bridge gen |
| S2 | Generate AOT Metadata | `StripAOTDllCommand` + `MethodBridgeGenericCacheCommand` |
| S3 | Sync to HotUpdate Path | Copy DLLs + metadata as `.bytes` to `Assets/GameRes/HotUpdate/` |
| S4 | Build YooAsset Package | Production build → `StreamingAssets/DefaultPackage/` |
| S5 | Apply Config | Set `AssetConfig.Mode = Offline` (YAML direct write) |
| S6 | Build Unity Player | `BuildPipeline.BuildPlayer` with IL2CPP + Android |
| S7 | Validate Artifacts | Check APK exists, StreamingAssets contains bundles |
| S8 | Smoke Test | Install to device, capture `latest.jsonl`, verify boot chain |
| S9 | Post-Build | Rollback `AssetConfig.Mode`, generate build report |

### Build Pipeline Files

| File | Purpose |
|------|---------|
| `KJBuildPipeline.cs` | Stage orchestrator, `Build()` / `BuildWithMask()` / `IncrementalBuild()` |
| `StageDependencyTracker.cs` | Change detection (S1→S2→S3→S4→S6 chain, S5→S6 independent) |
| `BuildStagePanel.cs` | EditorWindow with auto-detection + manual stage checkboxes |
| `StageBuildYooAsset.cs` | Production YooAsset build via `ScriptableBuildPipeline` |
| `StageApplyConfig.cs` | Direct YAML write `AssetConfig.Mode = Offline` with rollback |
| `StageBuildPlayer.cs` | `BuildPlayer` IL2CPP Android |
| `StageSmokeRun.cs` | ADB install + logcat capture + `latest.jsonl` verification |
| `StageValidateArtifacts.cs` | APK existence + StreamingAssets content validation |
| `BuildConfig.cs` | Serializable build configuration (Platform, BuildType, etc.) |

### Change Detection Rules

- **Cascade trigger**: S1→S2→S3→S4→S6 (any upstream change triggers downstream)
- **Independent**: S5→S6 (AssetConfig change triggers all stages)
- **Monitored paths**: S1/S2 watch `Assets/Scripts/**/*.cs`; S4 watches `Assets/GameRes/HotUpdate/**`; S5 watches `Assets/Resources/AssetConfig.asset`

When modifying build pipeline code, update `ProgressDoc/Discuss/Hy3_构建打包全流程管线_需求分析与设计.md`
and `ProgressDoc/Result/hybridclr_workflow.md` §4 to prevent documentation drift.

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
