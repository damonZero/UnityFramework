# Technology Stack

**Project:** KJ Unity Framework
**Researched:** 2026-06-26

## Recommended Stack

### Core Engine

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Unity | 2022.3.62f2 LTS | Game engine | Long-term support, stability priority, IL2CPP support for HybridCLR |

**Confidence:** HIGH - Version specified in PROJECT.md

### Hot Update

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| HybridCLR | Latest (main branch) | C# hot update | Industry standard for Unity C# hot update, supports all IL2CPP platforms including iOS |

**Installation:** Via Unity Package Manager (UPM) from GitHub
```
https://github.com/focus-creative-games/hybridclr.git
```

**Confidence:** HIGH - Official documentation confirms Unity 2022.3.x support

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

### Animation/Tweening

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| DOTween | Latest (Asset Store) | UI animations, tweening | Industry standard, free version available, excellent performance |

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

### Dependency Injection (Optional)

| Library | Purpose | When to Use |
|---------|---------|-------------|
| VContainer | Lightweight DI | If module dependencies become complex |
| Zenject | Full-featured DI | If team prefers convention-based binding |

**Recommendation:** Start without DI for simplicity. Add VContainer if module coupling becomes an issue.

**Confidence:** MEDIUM - Not required, architecture decision

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
    "com.focus-creative-games.hybridclr": "https://github.com/focus-creative-games/hybridclr.git"
  }
}
```

### NuGet/Manual DLLs (Assets/Plugins/)

```
Google.Protobuf.dll (netstandard2.0)
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
│   │   ├── Event/
│   │   ├── Network/
│   │   ├── UI/
│   │   ├── Resource/
│   │   └── Pool/
│   ├── General/       # Shared utilities
│   └── Project/       # Game-specific code
├── Resources/         # Unity Resources (minimal use)
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
| Luban v4.10.1 | GitHub | 2026-06-26 | HIGH |
| HybridCLR (no version tags) | GitHub | 2026-06-26 | HIGH (use main branch) |
| DOTween | Asset Store | N/A | MEDIUM (version not verified) |
