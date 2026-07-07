# HybridCLR Hot Update Boundary

> Date: 2026-07-05
> Purpose: Separate three different decisions that are easy to mix up:
> resource/DLL delivery, current-process restart, and app package replacement.

## Core Position

KJ uses the community/non-commercial HybridCLR baseline. The baseline supports
loading managed DLLs as hot-update code, but it does not provide commercial-only
semantics such as unloading/reloading an already loaded assembly in the same
process or Differential Hybrid Execution for changed AOT assemblies.

Therefore:

- A C# DLL update is not automatically an app package update.
- If the old version of that DLL has already been loaded in the current process,
  the new DLL normally takes effect after an app restart/cold start/next launch.
- An app package update is reserved for native/player-level changes, or for old
  shipped versions that do not yet contain any loader path capable of receiving
  the new managed code.

## What Really Requires A Package Update

Package update means a new APK/IPA/app-store build is required. It should be
treated as the last resort.

Required cases:

- Unity player/native code changes, including C++ engine-side changes.
- IL2CPP output or runtime native binaries that must change, such as
  `libil2cpp.so`.
- HybridCLR native runtime, installation, or low-level loading mechanism changes.
- Platform code changes: Android Java/Kotlin, iOS Objective-C/Swift, native
  plugins, permissions, signing, player settings, or other package metadata.
- A previously shipped version lacks the loader/manifest capability needed to
  download and load a managed replacement at all. This is a capability gap in
  that old package, not a rule that the C# layer itself must always be packaged.

Not package-required by itself:

- `Boot`, `Framework`, `Core`, `General`, or `Project` C# logic changes.
- C# framework implementation changes.
- C# public API changes, as long as the loader path, restart policy, caller DLLs,
  AOT supplemental metadata, and compatibility plan are handled.

## Restart Classification

### No package update, no app restart

Allowed when the update is downloaded before the relevant code/resource is first
loaded:

- `Core` / `General` / `Project` DLL updates discovered before formal startup.
- New or changed YooAsset resources that have not yet been loaded.
- Config, prefab, scene, and UI resources loaded after the manifest update.

### No package update, app restart required

Required when the new managed code or resource can be downloaded, but the old
version has already been loaded or instantiated in the current process:

- Any managed DLL update discovered after that assembly was loaded.
- Startup/update-flow DLL changes after the old startup flow has already run.
- Assembly list, AOT metadata list, startup type/method, or manifest contract
  changes discovered after boot.
- Loaded resources whose owners cannot safely release and rebuild them in place.
- Any fix that would require commercial hot reload/unload semantics to apply
  without restarting.

The restart can be an in-game soft restart, a full app cold restart, or simply
"next launch", depending on platform capability and how early the changed code
is needed.

## Boot Is Two Logical Parts

Boot should be designed as two logical layers:

| Part | Current-process behavior | Package behavior |
| --- | --- | --- |
| BootLoader / Entry | Tiny loader that finds manifests, initializes the minimal resource path, loads startup code, and calls the next stage. If its old code already ran, replacement code needs a restart/next launch. | Not inherently package-required if the shipped loader already knows how to fetch/load its managed replacement. Keep it extremely stable because old packages depend on it. |
| Boot.Update / startup update flow | Version check, repair button, update UI, managed DLL/resource download, restart classification. A new DLL takes effect after restart once the old one has loaded. | Delivered as hot-update managed code after the split. |

The old project follows this model: `Boot.Entry` is the tiny package-resident
entry, while `Boot.Update` is loaded through the boot C# asset list. Its restart
policy separates no restart, in-game restart, and outside-app restart.

Current KJ has split `Boot.Update` into AOT `Launcher` + hot-update `Boot` (HYB-03,
2026-07-07). The editor/runtime tooling now publishes the formal runtime preload
list (`Boot, Core, General, Project, Pool, Cache, Event, Asset, Log, RuntimeLog`)
and the `Boot` hot-update assembly is the startup-update flow. That is a loader/
tooling scope decision, not a statement that Boot C# changes always require an app
package update.

## Assembly Groups

### Minimal Loader And Stable Contracts

These are intentionally small and stable because they participate before the
normal hot-update runtime is available:

| Assembly | Role |
| --- | --- |
| `Launcher` (AOT) | Tiny loader that finds manifests, initializes the minimal resource path, loads startup code, and calls the hot-update entry via reflection. Extremely stable; old packages depend on it. (HYB-03 implemented.) |
| `Boot` (HotUpdate) | Update runner: version check, repair button, update UI, managed DLL/resource download, restart classification. Hot-update managed code after the split; replacement of an already-loaded DLL needs restart/next launch. (HYB-03 implemented; formerly the "Boot.Update" concept.) |
| `Framework.Asset` / `Asset` | Minimal resource API and YooAsset adapter needed by Boot/Core handoff. |
| `Framework.Event` / `Event` | Stable event marker and scanner. |
| `Framework.Log` / `Log` | Stable logging facade available during early boot. |
| `Framework.Pool` / `Pool` | Stable low-level pool primitives. |
| `Framework.Cache` / `Cache` | Stable low-level cache primitives. |
| Third-party packages | Package-managed dependencies, native/editor tooling, generated runtime support. |

Rules:

- Loader/stable assemblies must not depend on upper gameplay assemblies.
- Loader-facing APIs should stay tiny and backward-compatible across versions.
- If a stable contract changes, coordinate all caller DLLs and AOT metadata, and
  classify it as a restart-required managed update unless native/player code is
  touched.

### Runtime Hot-Update Assemblies

These assemblies are loaded by HybridCLR before their runtime entry is invoked:

| Assembly | Scope |
| --- | --- |
| `Core` | Engine infrastructure: DI registration, SystemManager, asset/pool bridge, UI manager, network core. |
| `General` | Reusable business modules: config, audio, red dot, guide, localization, login. |
| `Project` | Project-specific gameplay, windows, flows, and content-facing logic. |
| `Boot` | Hot-update startup update flow and repair/update UI (the former `Boot.Update` concept, now the hot-update `Boot` assembly after HYB-03). |

Rules:

- Hot-update assemblies may reference lower/stable assemblies according to the
  dependency direction.
- Hot-update assembly names and startup type/method names come from Entry
  serialized settings or a boot manifest. They must not be hard-coded into the
  long-term Boot code path.
- Once a hot-update assembly has been loaded in a process, the community
  HybridCLR baseline treats its replacement as restart/next-launch behavior.

## Resource-Only Update Details

- Unloaded resources can update without restart.
- Loaded assets need their owning module to release and reload them.
- Scene/UI resources that are already active usually need a local teardown and
  rebuild. If that cannot be done safely, treat it as an in-game restart.
- Boot/startup resources are special: if they are used by the active startup
  flow, prefer a soft restart or outside-app restart depending on whether any
  startup DLL/AOT metadata changed.

## Boot Sequence

Current KJ sequence:

```text
Entry
  -> Entry serialized startup settings
  -> initialize minimal Framework.Asset runtime
  -> request resource version and update manifest
  -> download required resources
  -> load AOT supplemental metadata
  -> load Core/General/Project hot-update assemblies
  -> reflect Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)
  -> Project creates the VContainer root and registers Core -> General -> Project
```

Target sequence after a future Boot split:

```text
BootLoader (AOT Launcher) / Entry
  -> load startup manifest
  -> load Boot (hot-update) DLL/AOT metadata if present
  -> reflect Boot.BootUpdateRunner.Start(BootBridge)
  -> Boot checks/downloads resources and remaining DLLs
  -> classify restart requirement
  -> load Core/General/Project
  -> reflect Project.Bootstrap.ProjectStartup.Start(IAssetRuntime)
```

Boot must not create VContainer, MessagePipe, Core, General, or Project objects
before the configured hot-update assemblies are loaded.

## Default Assembly Policy

| Layer | Current policy | Target / notes |
| --- | --- | --- |
| `Boot` (HotUpdate) | Hot-update update-runner assembly. | C# changes are restart-classified when an already-loaded DLL is replaced; delivered as hot-update managed code. (HYB-03 implemented.) |
| `Launcher` (AOT) | Tiny AOT loader in the current package and startup path. | Keep extremely stable; old packages depend on it. Must not reference Framework/hot-update assemblies. |
| `Framework` | Stable contracts and low-level adapters. | Keep loader-facing APIs stable; C# implementation changes are not native package changes by definition. |
| `Core` | Runtime hot update. | Engine infrastructure can iterate through hot update. |
| `General` | Runtime hot update. | Reusable business logic, including login. |
| `Project` | Runtime hot update. | Project-specific logic. |

## New Module Placement

Use this checklist before creating a module:

1. If it must run before any hot-update assembly can be loaded, keep it in the
   tiny loader or a stable `Framework/*` contract.
2. If it is startup-update behavior that can be loaded by a tiny loader, place
   it in future `Boot.Update`.
3. If it is stable, reusable, and must not reference `Assets/Scripts`, place it
   in `Framework/*`.
4. If it coordinates engines, services, lifecycle, or third-party backends for
   upper layers, place it in `Core`.
5. If it is reusable game/business functionality, place it in `General`.
6. If it is specific to this game's content or flow, place it in `Project`.

When in doubt, choose the higher hot-update layer. Moving code downward later
requires compatibility work and possibly a restart-gated rollout, so keep
downward moves deliberate.

## First Implementation Scope

HYB-00:

- Document this boundary.
- Keep the current runtime preload list to `Core` / `General` / `Project`.
- Record the future `Boot.Update` split as a separate follow-up, not an implied
  behavior of the current `Boot` assembly.

HYB-01:

- Add Entry-side serialized startup configuration.
- Load configured DLLs and AOT metadata before creating `Core/General/Project`
  runtime objects.
- Keep Editor/non-HybridCLR mode able to run from already compiled assemblies.
- Prefer configured YooAsset raw asset paths for DLL/AOT bytes, with
  `StreamingAssets/HotUpdate` and `Resources` kept only as local fallback paths.

Boot startup UI may show progress, retry, and repair controls, but it must stay
limited to update operations. Login, account SDK flow, server list, and role
selection are General/Project business flows, not Boot or Core logic.

Boot initializes a minimal `Framework.Asset.IAssetRuntime` for update work and
passes that same instance to `ProjectStartup`. Core must register the handed-off
runtime instead of creating a second YooAsset runtime.

HYB-02:

- Add build/editor tooling for generating hot-update DLL assets and AOT
  metadata assets.
- Package hot-update DLLs and AOT metadata as YooAsset-managed raw assets under
  `Assets/GameRes/HotUpdate/`. `StreamingAssets/HotUpdate` may be a local
  Editor/standalone fallback, but should not be the primary mobile path.
- Use `KJ/HybridCLR/Prepare Runtime Assets And Boot` for the normal smoke path.
- Use `KJ/HybridCLR/Generate All And Sync` for the full formal-build pipeline.
- The final YooAsset hot-update DLL directory must contain only the configured
  runtime preload assemblies. HybridCLR intermediate output can contain many
  player script DLLs and is not the runtime publication boundary.
- Use `KJ/HybridCLR/Apply To Open Entry` to write HybridCLRSettings assembly
  lists into the open Boot `Entry` serialized settings.

HYB-03 (done, 2026-07-07):

- Implemented as AOT `Launcher` (tiny loader) + hot-update `Boot` (update runner),
  not a separate `Boot.Update` assembly. The `Launcher` asmdef references only
  `UniTask / YooAsset / HybridCLR.Runtime / AssetShared` and never Framework or
  hot-update assemblies; it calls the hot-update entry via reflection:
  `Boot.BootUpdateRunner.Start(BootBridge)`.
- Restart classification (no restart / in-game restart / outside-app restart)
  already documented above and applies: a replaced already-loaded managed DLL
  takes effect after restart/next launch.
- A dedicated startup-update manifest is still the `Boot` hot-update assembly
  itself, loaded via the configured hot-update list; `AssetConfig`/`AssetConstants`
  live in AOT-shared `Framework.AssetShared` so the loader can read them.
- 10 hot-update assemblies: `Boot, Core, General, Project, Pool, Cache, Event,
  Asset, Log, RuntimeLog`.
