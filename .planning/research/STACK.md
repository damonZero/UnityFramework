# Technology Stack

**Project:** Unity Framework
**Researched:** 2026-06-26 / Updated: 2026-06-30

## Recommended Stack

### Core Engine

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Unity | 2022.3.62f2 LTS | Game engine | Long-term support, stability priority, IL2CPP support for HybridCLR |

**Confidence:** HIGH - Version specified in PROJECT.md

### Dependency Injection

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| VContainer | 1.1.0 | DI / IoC | Lightweight, fast, pure C# — no MonoBehaviour coupling |

**Installation:** Via Unity Package Manager (UPM) from GitHub
```
https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer
```

**Confidence:** HIGH - Integrated and verified

### Event Bus

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| MessagePipe | Latest | Type-safe async message pipeline | Zero-allocation, supports pub/sub and request/response, VContainer integration |

**Installation:** Via Unity Package Manager (UPM) from GitHub
```
https://github.com/Cysharp/MessagePipe.git
```

**Confidence:** HIGH - Integrated with VContainer bridge (MessagePipe.VContainer)

### Asset Management

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| YooAsset | 3.0 (UPM) | AssetBundle management, hot-update resource pipeline | Industry standard (8300+ GitHub stars), 5+ years maintenance, built-in incremental update, encryption, editor toolchain |

**Installation:** Via Unity Package Manager (UPM) from GitHub
```
https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset
```

**Confidence:** HIGH - Integrated via Core/Asset/ with owned/cached dual-channel handle management

### Hot Update

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| HybridCLR | Latest (main branch) | C# hot update | Industry standard for Unity C# hot update, supports all IL2CPP platforms including iOS |

**Status:** Planned, **not yet installed** in manifest.json. Listed here for architecture awareness.

**Installation:** Via Unity Package Manager (UPM) from GitHub
```
https://github.com/focus-creative-games/hybridclr.git
```

**Confidence:** MEDIUM — not yet integrated

### Configuration Tables

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Luban | v4.10.1 | Config table generation | Open source, mature solution, supports C# code generation, Excel/JSON sources |

**Installation:** Download from GitHub releases or use NuGet
```
https://github.com/focus-creative-games/luban
```

**Key Code Generator:** `code_cs_unity_json` (uses SimpleJson, compatible with Unity's .NET)

**Confidence:** HIGH - Verified latest version from GitHub releases (June 2025)

### Network Communication

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Google.Protobuf | 3.35.1 | Serialization | Official Protobuf C# implementation, high performance binary format |
| Protobuf Compiler (protoc) | Match library version | Code generation | Generate C# classes from .proto files |

**Installation:** NuGet package or manual DLL import
```
https://www.nuget.org/packages/Google.Protobuf
```

**Note for Unity:** Download the netstandard2.0 or net45 DLL for Unity compatibility. Place in `Assets/Plugins/`.

**Confidence:** HIGH - Verified latest version from NuGet (June 2026)

### Async/Await

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| UniTask | v2.5.11 | Zero-allocation async/await | Essential for modern Unity development, integrates with Unity lifecycle |

**Installation:** Unity Package Manager via OpenUPM or Git URL
```
https://github.com/Cysharp/UniTask.git
```

**Confidence:** HIGH - Verified latest version from GitHub releases (May 2025)

### Logging and Allocation Tools

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| ZLogger | 2.5.10 | Structured logging backend | Built on `Microsoft.Extensions.Logging`, supports source-generated zero-allocation log methods |
| ZString | 2.6.0 | Low-allocation string construction | Reduces formatting/string builder allocations in hot paths |
| ZLinq | 1.5.6 | Zero-allocation LINQ-style queries | Keeps query code readable while avoiding regular LINQ allocations |

**Installation:** UPM Git packages plus NuGetForUnity packages where required.

```json
"com.cysharp.zlogger": "https://github.com/Cysharp/ZLogger.git?path=src/ZLogger.Unity/Assets/ZLogger.Unity",
"com.cysharp.zlinq": "https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity",
"com.cysharp.zstring": "https://github.com/Cysharp/ZString.git?path=src/ZString.Unity/Assets/Scripts/ZString"
```

**Usage Policy:**
- Prefer ZLogger source-generated `static partial` extension methods with `[ZLoggerMessage]` for stable high-frequency logs.
- Register logging in Core through VContainer (`ILoggerFactory`, `ILogger<T>`, providers and levels). Keep Framework independent by using interfaces or delegate bridges when it needs logging.
- Prefer ZString for hot-path string building and ZLinq `AsValueEnumerable()` for allocation-sensitive collection queries.
- Unity 2022.3.62f2 satisfies the Unity source generator version floor. If a generator path requires preview syntax, configure `-langVersion:preview`.
- Do not enable ZLinq DropIn Generator globally without a scoped compatibility review.

**Confidence:** HIGH - Installed in manifest/packages and compatible with current Unity version.

### Animation/Tweening

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| DOTween | Latest (Asset Store) | UI animations, tweening | Industry standard, free version available, excellent performance |

**Status:** Planned, **not yet installed**.

**Installation:** Unity Asset Store (free) or http://dotween.demigiant.com

**Confidence:** MEDIUM - Version not available via GitHub releases, distributed via Asset Store

### UI Framework

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| UGUI (Unity built-in) | Unity 2022.3 built-in | UI system | Native, stable, no external dependencies, sufficient for most game UI |

**Enhancement Libraries:**
- DOTween for UI animations
- Custom UI Manager for window/layer management

**Confidence:** HIGH - Built into Unity, specified in requirements

### Audio

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Unity Audio (built-in) | Unity 2022.3 built-in | Basic audio | Simple, no dependencies for framework layer |
| FMOD (optional) | Latest | Advanced audio | Only if project needs complex audio (3D, effects, mixing) |

**Recommendation:** Start with Unity's built-in AudioManager. Add FMOD only if specific audio requirements emerge at project layer.

**Confidence:** HIGH - Built-in audio sufficient for framework

## Supporting Libraries (Recommended)

### Utilities

| Library | Purpose | When to Use |
|---------|---------|-------------|
| UniRx | Reactive extensions | Only if team prefers reactive patterns (not recommended for this project) |
| MessagePack | Binary serialization | Alternative to Protobuf for internal data (not needed if using Protobuf) |

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Hot Update | HybridCLR | ILRuntime | HybridCLR is successor, better performance, active development |
| Hot Update | HybridCLR | Lua (xLua/toLua) | C# hot update preferred, no Lua learning curve |
| Config Tables | Luban | ExcelDataReader | Luban provides code generation, validation, multi-format export |
| Config Tables | Luban | GameFramework's config | Luban is standalone, more flexible, better tooling |
| Async | UniTask | Unity Coroutines | UniTask is zero-allocation, supports async/await, better error handling |
| Tweening | DOTween | LeanTween | DOTween more feature-rich, better community support |
| Tweening | DOTween | Unity DOTween (Anim) | DOTween is simpler for programmatic animation |
| UI | UGUI | FairyGUI | UGUI is native, no external dependency, simpler for framework |
| UI | UGUI | UI Toolkit | UI Toolkit not mature enough for production games in 2022.3 |

## Installation Summary

### Package Manager (manifest.json)

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.cysharp.messagepipe": "https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe",
    "com.cysharp.messagepipe.vcontainer": "https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer",
    "com.cysharp.zlogger": "https://github.com/Cysharp/ZLogger.git?path=src/ZLogger.Unity/Assets/ZLogger.Unity",
    "com.cysharp.zlinq": "https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity",
    "com.cysharp.zstring": "https://github.com/Cysharp/ZString.git?path=src/ZString.Unity/Assets/Scripts/ZString",
    "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer",
    "com.tuyoogame.yooasset": "https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset"
  }
}
```

**Note:** HybridCLR (`com.focus-creative-games.hybridclr`) is planned but not yet added to manifest.json.

### NuGet/Manual DLLs (Assets/Plugins/)

```
Google.Protobuf.dll (netstandard2.0)
ZLogger / ZLinq / Microsoft.Extensions.* dependencies via NuGetForUnity
```

### Asset Store

```
DOTween (free version)
```

### External Tools

```
Luban (download from GitHub releases)
protoc (download from protobuf releases)
```

## Project Structure Recommendation

```
Assets/
├── Scripts/
│   ├── Boot/          # Entry point, initialization
│   ├── Core/          # Framework core systems
│   │   ├── Architecture/  # SystemManager, CoreSystemAttribute, events
│   │   ├── Bootstrap/     # CoreContainerRegistration, CoreBootstrapStage
│   │   └── Asset/         # AssetSystem (YooAsset integration)
│   ├── General/       # Shared utilities, Model lifecycle
│   └── Project/       # Game-specific code
├── Resources/         # Unity Resources (minimal use — only AssetConfig)
├── StreamingAssets/   # Config tables, hot update DLLs
├── Plugins/           # Third-party DLLs
└── Packages/          # UPM packages
```

## Sources

- HybridCLR: https://github.com/focus-creative-games/hybridclr (Context7 + GitHub)
- Luban: https://github.com/focus-creative-games/luban (Context7 + GitHub releases)
- UniTask: https://github.com/Cysharp/UniTask (GitHub releases)
- Google.Protobuf: https://www.nuget.org/packages/Google.Protobuf (NuGet)
- DOTween: http://dotween.demigiant.com (Official site)
- Knight Framework: https://github.com/winddyhe/knight (Reference architecture)

## Version Verification Notes

| Library | Source | Verification Date | Confidence |
|---------|--------|-------------------|------------|
| Google.Protobuf 3.35.1 | NuGet | 2026-06-26 | HIGH |
| UniTask v2.5.11 | GitHub | 2026-06-26 | HIGH |
| VContainer 1.1.0 | GitHub | 2026-06-30 | HIGH |
| MessagePipe | GitHub | 2026-06-30 | HIGH |
| YooAsset 3.0 | GitHub | 2026-06-30 | HIGH |
| ZLogger 2.5.10 | UPM + NuGetForUnity | 2026-07-03 | HIGH |
| ZLinq 1.5.6 | UPM + NuGetForUnity | 2026-07-03 | HIGH |
| ZString 2.6.0 | UPM | 2026-07-03 | HIGH |
| Luban v4.10.1 | GitHub | 2026-06-26 | HIGH |
| HybridCLR (no version tags) | GitHub | 2026-06-26 | HIGH (use main branch) |
| DOTween | Asset Store | N/A | MEDIUM (version not verified) |
