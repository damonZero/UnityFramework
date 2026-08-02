#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
KJ asmdef dependency validator + ground-truth generator.

Design notes (related to the flaky G: mount):
    * The project lives under a sandbox mount whose folder name is a long run of
    'M' characters. We NEVER hardcode that literal here. Instead the KJ root is
    resolved dynamically by scanning G:/ for the M-folder that actually contains
    NewProjectK/KJ/Assets/AGENTS.md. Pass an explicit root as argv[1] to bypass.
  * Every file read is retried, because the mount intermittently returns empty.

What it does:
  1. Emits a ground-truth reference table (use this to FIX doc drift in
     .planning/STATE.md and CODEMAP.md -- the .asmdef files are the source of truth).
  2. Validates architecture rules:
       R1  Launcher(AOT) references only {UniTask, YooAsset, HybridCLR.Runtime,
           AssetShared} (+ engine). No Framework.* / no hot-update assembly.
       R2  Boot(hot) must NOT reference HybridCLR.Runtime / VContainer / upper layers.
       R3  No Framework package may reference a Scripts-layer assembly
           (Boot/Core/General/Project/Launcher).
       R4  Framework tiering -- strictly ONE-WAY, acyclic:
             Tier0 leaves : AssetShared, Log, Cache   (may reference NO Framework pkg)
             Tier1 composites: Event, Pool, RuntimeLog, Asset
                             (may reference only LOWER-tier Framework pkgs)
           => no lateral (T1->T1) and no upward (T0->T1) references.
       R5  No cycles anywhere in the dependency graph.
       R6  HybridCLR hotUpdateAssemblies consistency (10 expected; Launcher excluded).
       R7  Editor assemblies are Editor-gated (includePlatforms: Editor).

Run:  python asmdef_dependency_validator.py [ROOT]
"""
import os, sys, time, json, re

# ---------------------------------------------------------------- dynamic root
def resolve_root(max_tries=40, sleep=0.3):
    for _ in range(max_tries):
        try:
            top = os.listdir("G:\\")
        except Exception:
            time.sleep(sleep)
            continue
        for name in top:
            if name.startswith("M") and os.path.isdir(os.path.join("G:\\", name)):
                cand = os.path.join("G:\\", name, "NewProjectK", "KJ")
                probe = os.path.join(cand, "Assets", "AGENTS.md")
                for _2 in range(5):
                    if os.path.isfile(probe):
                        return cand
                    time.sleep(0.1)
        time.sleep(sleep)
    raise SystemExit("Could not resolve KJ root under G:\\ (mount flaky?)")

ROOT = sys.argv[1] if len(sys.argv) > 1 else resolve_root()

# ----------------------------------------------- known asmdef relative paths
# (stable; the long M-folder name is NOT hardcoded anywhere in this list)
ASMDEFS = [
    "Assets/Scripts/Boot/Launcher/KJ.Launcher.asmdef",
    "Assets/Scripts/Boot/KJ.Boot.asmdef",
    "Assets/Scripts/Core/KJ.Core.asmdef",
    "Assets/Scripts/General/KJ.General.asmdef",
    "Assets/Scripts/Project/KJ.Project.asmdef",
    "Assets/Scripts/Boot.Editor/Boot.Editor.asmdef",
    "Assets/Scripts/Core.Editor/Core.Editor.asmdef",
    "Assets/Framework/Asset/Asset.asmdef",
    "Assets/Framework/Pool/Pool.asmdef",
    "Assets/Framework/Cache/Cache.asmdef",
    "Assets/Framework/Event/Event.asmdef",
    "Assets/Framework/Log/Log.asmdef",
    "Assets/Framework/RuntimeLog/RuntimeLog.asmdef",
    "Assets/Framework/AssetShared/Framework.AssetShared.asmdef",
    "Assets/Framework/Asset.Editor/Framework.Asset.Editor.asmdef",
    "Assets/Framework/TestKit/TestKit.asmdef",
]

def read_json(rel, max_tries=40, sleep=0.4):
    p = os.path.join(ROOT, rel)
    for _ in range(max_tries):
        try:
            with open(p, encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            time.sleep(sleep)
    raise SystemExit("Failed to read %s" % rel)

def read_text(rel, max_tries=10, sleep=0.25):
    p = os.path.join(ROOT, rel)
    for _ in range(max_tries):
        try:
            with open(p, encoding="utf-8") as f:
                return f.read()
        except Exception:
            time.sleep(sleep)
    raise SystemExit("Failed to read %s" % rel)

# ---------------------------------------------------------------- load graph
graph = {}
meta = {}
order = []
for rel in ASMDEFS:
    d = read_json(rel)
    name = d["name"]
    refs = d.get("references", []) or []
    inc = d.get("includePlatforms", []) or []
    graph[name] = list(refs)
    meta[name] = {"rel": rel, "include": inc,
                  "autoRef": d.get("autoReferenced", True),
                  "noEngine": d.get("noEngineReferences", False)}
    order.append(name)

# HybridCLR
hc_text = read_text("ProjectSettings/HybridCLRSettings.asset")
def parse_list(block_name, text):
    m = re.search(r"%s:\s*\n((?:[ \t]*-\s*\S+\s*\n)+)" % block_name, text)
    if not m:
        return []
    return [ln.strip()[1:].strip() for ln in m.group(1).split("\n")
            if ln.strip().startswith("-")]
hot = parse_list("hotUpdateAssemblies", hc_text)
patch = parse_list("patchAOTAssemblies", hc_text)

# ---------------------------------------------------------------- classify
def layer_of(name):
    if name == "Launcher": return "Launcher(AOT)"
    if name == "Boot": return "Boot(hot)"
    if name == "Core": return "Core"
    if name == "General": return "General"
    if name == "Project": return "Project"
    if name in ("Boot.Editor", "Core.Editor", "Framework.Asset.Editor"): return "Editor"
    if name == "TestKit": return "Test"
    if name in ("AssetShared", "Log", "Cache", "Event", "Pool",
                "RuntimeLog", "Asset"): return "Framework"
    return "Other"

FRAMEWORK = {"AssetShared", "Log", "Cache", "Event", "Pool", "RuntimeLog", "Asset"}
TIER0 = {"AssetShared", "Log", "Cache"}            # leaves
TIER1 = {"Event", "Pool", "RuntimeLog", "Asset"}   # composites
SCRIPTS = {"Boot", "Core", "General", "Project", "Launcher"}
HOT = {"Boot", "Core", "General", "Project", "Pool", "Cache",
       "Event", "Asset", "Log", "RuntimeLog"}

ENGINE_OK = {"UnityEngine", "UnityEngine.CoreModule", "UnityEngine.AssetBundleModule",
             "UnityEditor", "Assembly-CSharp", "mscorlib", "netstandard", "System",
             "System.Core", "System.Runtime", "System.Collections.Immutable",
             "System.Memory", "Unity.Collections", "Unity.Burst", "Unity.Mathematics",
             "Cysharp.Threading.Tasks"}
PKG_OK = {"UniTask", "YooAsset", "MessagePipe", "MessagePipe.VContainer", "VContainer",
          "VContainer.Unity", "HybridCLR.Runtime", "ZLinq", "ZLogger", "ZLogger.Unity",
          "ZString", "Luban", "Newtonsoft.Json", "UniRx"}

# ---------------------------------------------------------------- validate
errors = []
warnings = []

def check(name, cond, msg):
    if not cond:
        errors.append("[%s] %s" % (name, msg))

# R1 Launcher  (AssetShared is the deliberate AOT-shared exception)
if "Launcher" in graph:
    bad = [r for r in graph["Launcher"]
           if r not in PKG_OK and r not in ENGINE_OK and r != "AssetShared"]
    check("Launcher", not bad, "references disallowed: %s" % bad)
    badfw = [r for r in graph["Launcher"]
             if (r in FRAMEWORK and r != "AssetShared") or r in HOT or r == "Boot"]
    check("Launcher", not badfw,
          "references Framework/hot-update (AssetShared allowed): %s" % badfw)

# R2 Boot
if "Boot" in graph:
    banned = {"HybridCLR.Runtime", "VContainer", "Core", "General", "Project",
              "MessagePipe", "MessagePipe.VContainer", "Pool", "Cache", "Event"}
    bad = [r for r in graph["Boot"] if r in banned]
    check("Boot", not bad, "references banned upper/pkg: %s" % bad)

# R3 Framework -> Scripts
for name in FRAMEWORK:
    if name in graph:
        bad = [r for r in graph[name] if r in SCRIPTS]
        check(name, not bad, "references Scripts-layer: %s" % bad)

# R4 Framework tiering (strictly one-way)
for name in FRAMEWORK:
    if name not in graph:
        continue
    for r in graph[name]:
        if r not in FRAMEWORK:
            continue
        if name in TIER1 and r in TIER0:
            continue  # OK: T1 -> T0
        if name in TIER0 and r in TIER0:
            errors.append("[%s] Tier0 references another Tier0 Framework pkg: %s" % (name, r))
        elif name in TIER1 and r in TIER1:
            errors.append("[%s] lateral Tier1->Tier1 reference: %s "
                          "(must be strictly lower tier)" % (name, r))
        elif name in TIER0 and r in TIER1:
            errors.append("[%s] UPWARD Tier0->Tier1 reference: %s (forbidden)" % (name, r))

# R5 cycle detection
def find_cycle():
    WHITE, GRAY, BLACK = 0, 1, 2
    color = {n: WHITE for n in graph}
    stack = []
    cyc = [None]
    def dfs(u):
        color[u] = GRAY
        stack.append(u)
        for v in graph.get(u, []):
            if v not in graph:
                continue
            if color.get(v) == GRAY:
                i = stack.index(v)
                cyc[0] = stack[i:] + [v]
                return True
            if color.get(v) == WHITE and dfs(v):
                return True
        color[u] = BLACK
        stack.pop()
        return False
    for n in list(graph):
        if color[n] == WHITE and dfs(n):
            return cyc[0]
    return None
cyc = find_cycle()
check("GRAPH", cyc is None,
      "dependency cycle: %s" % (" -> ".join(cyc) if cyc else ""))

# R6 HybridCLR
missing_in_hot = [h for h in HOT if h not in hot]
check("HybridCLR", not missing_in_hot,
      "hot-update asmdefs missing from hotUpdateAssemblies: %s" % missing_in_hot)
check("HybridCLR", "Launcher" not in hot, "Launcher must NOT be in hotUpdateAssemblies")
for h in hot:
    if h not in graph:
        warnings.append("[HybridCLR] hot assembly '%s' has no matching .asmdef" % h)

# R7 Editor gating
for name in ("Boot.Editor", "Core.Editor", "Framework.Asset.Editor"):
    if name in meta:
        check(name, "Editor" in meta[name]["include"],
              "must be Editor-gated (includePlatforms: Editor)")

# ---------------------------------------------------------------- report
out = []
out.append("=" * 72)
out.append("KJ asmdef dependency validator")
out.append("=" * 72)
out.append("ROOT                  = %s" % ROOT)
out.append("resolved M-folder len = %d" % len(os.path.basename(os.path.dirname(os.path.dirname(ROOT)))))
out.append("")
out.append("== Ground-truth reference table (source of truth = .asmdef files) ==")
for name in order:
    refs = graph[name]
    out.append("%-22s [%-13s] refs(%d): %s"
               % (name, layer_of(name), len(refs), ", ".join(refs) if refs else "(none)"))
out.append("")
out.append("== HybridCLR hotUpdateAssemblies (%d) ==" % len(hot))
out.append("  " + ", ".join(hot))
out.append("== patchAOTAssemblies ==")
out.append("  " + ", ".join(patch))
out.append("")
out.append("== Validation ==")
if not errors and not warnings:
    out.append("ALL CHECKS PASSED")
else:
    if errors:
        out.append("FAILURES (%d):" % len(errors))
        for e in errors:
            out.append("  [x] " + e)
    if warnings:
        out.append("WARNINGS (%d):" % len(warnings))
        for w in warnings:
            out.append("  [!] " + w)
out.append("=" * 72)

report = "\n".join(out)
print(report)
with open(r"C:\Users\Administrator\AppData\Local\Temp\asmdef_report.txt",
          "w", encoding="utf-8") as f:
    f.write(report)
