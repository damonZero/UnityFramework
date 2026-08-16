# 相机系统 + 渲染管线移植（37 → KJ）

> 2026-08-16 | 本文由 `CAMERA_SYSTEM_37.md`（37 相机系统梳理）与 `CAMERA_PORT_PLAN.md`（移植计划）合并而成。
> 参考项目：`F:\int_37_pack\client`（P37，C# Unity，URP 14.0.8 本地修改版）。
> 状态图例：✅ 已移植　🟡 部分/有差异　❌ 未移植　⛔ 依赖缺失阻塞。

## 目录
- [Part A — 37 相机系统梳理（基线）](#part-a--37-相机系统梳理基线)
- [Part B — 移植计划（Phase 0-8）](#part-b--移植计划phase-0-8)

---

## Part A — 37 相机系统梳理（基线）

## 0. 相机全景图（分层）

37 的相机不是一个孤立的「主相机」，而是一套按职责分层的相机群：

```
                         ┌─────────────────────────────────────────────┐
                         │            URP 相机栈（Base + Overlay）        │
                         │   BootBaseCamera / BootOverlayCamera          │
                         │   （1 个 Base 主相机 + N 个 Overlay 叠加）      │
                         └─────────────────────────────────────────────┘
                                        │ 由 cameraStack 统一管理
        ┌───────────────┬───────────────┼───────────────────┬───────────────┐
        │               │               │                   │               │
   [UI 相机]        [主/场景相机]     [UI 内嵌模型相机]    [特效/动态相机]   [战斗相机]
  tag=UICamera      tag=MainCamera   layer=UIModel        运行时创建        游戏特定
  UIRootCamera      BaseCamera       UIModelImg 管理       Mirror 反射相机   FightCameraNode
  Screen Space      场景 Camera +    RenderTexture→        PlanarShadow      (站位/震动/特效)
  Camera Canvas     OverlayCamera    RawImage              等                LocationAndCamera
        │               │
   ScreenHelper.UICamera  Camera.main
   UICamera / UICameraAdapter
```

两条正交的主线：

1. **渲染栈主线（URP）**：所有相机通过 `BootBaseCamera` 的 `cameraStack` 统一管理 Base/Overlay 关系，只有 1 个 Base（主相机），其余 Overlay 叠加其上。
2. **遮挡/显隐主线（Coverage）**：动态创建的相机由 `CoverageChecker` 监听并补挂 `CameraCoverage` / `CameraCoverageChild`，使其纳入「界面遮挡时自动隐藏相机」的渲染优化体系。

---

## 1. Tag / Layer 命名约定

| 标识 | 值 | 用途 | 关键消费方 |
|---|---|---|---|
| **Tag: `MainCamera`** | tag | 主相机（场景渲染） | `BootBaseCamera.CacheCamera`（识别 Base）、`CameraCoverage.mainCamera` 缓存、`CameraColorTexture` 编辑器回退 |
| **Tag: `UICamera`** | tag | UI 相机（渲染 Screen Space Camera Canvas） | `BootBaseCamera.uiCamera`、`CameraColorTexture` 编辑器回退、`ScreenDissolveController` 校验 |
| **Layer: `UI`** | layer | UI 本体 | `BaseCamera.CheckOverlayCamera` 校验 cullingMask 不得含 UI |
| **Layer: `CopyUI`** | layer | UI 副本（镜像/屏幕溶解等二次采样场景） | `Mirror.cs`、`CheckRaycastTarget` |
| **Layer: `UIModel`** | layer | UI 内嵌 3D 模型 | `UIModelImg` 把模型/相机/容器统一设到此层 |

**约定**：场景/主相机（MainCamera）的 cullingMask **不得包含** `UI`/`CopyUI`/`UIModel`（`BaseCamera.CheckOverlayCamera` 在 Editor 下进 Play 时会检测并报错）；UI 与 UI 模型由各自专用相机渲染，避免主相机误渲。

---

## 2. URP 相机栈（Base / Overlay）

> 背景：URP 相机叠加模型改变后，只允许 1 个 Base 相机 + 其余以 Overlay 挂到它身上。37 通过「挂脚本」的方式在运行时把相机改造成 Base/Overlay，编辑器里仍保持普通 Camera 方便预览。

### 2.1 本地修改版 URP

37 用的 `com.unity.render-pipelines.universal@14.0.8` 是 **本地 embedded 修改版**（在 `Packages/` 目录下，非 Library/PackageCache）。它对 `UniversalAdditionalCameraData` 扩展了 3 个自定义字段：

| 字段 | 位置 | 用途 |
|---|---|---|
| `ShotInUI` | `Runtime/UniversalAdditionalCameraData.cs:372` | 截屏时是否包含该相机（`BootOverlayCamera.shotInUI`） |
| `showDepthTextrue` | 同上 `:482` | 按需开启深度纹理（`CameraDepthTexture` 驱动） |
| `showColorTextrue` | 同上 `:507` | 按需开启颜色纹理（`CameraColorTexture` 驱动，热扰动等） |

> ⚠️ **移植红线**：这三个字段是 37 对 URP 的私有改动。KJ 的 manifest 里 `com.unity.render-pipelines.universal: 14.0.8` 是**标准版**（无本地包），没有这些字段。任何依赖它们的代码（`BootBaseCamera`/`CameraColorTexture`/`CameraDepthTexture`/`CameraSwitch`）**不能直接移植**，需先决策「引入并修改本地 URP」还是「用替代方案（如自定义 RenderFeature/URP 官方 camera stack 覆盖方案）」。

### 2.2 四个关键类

| 类 | 层 | 挂在 | 职责 |
|---|---|---|---|
| `BootBaseCamera` | Boot | UI 相机（tag UICamera） | 全局 `cameraStack` 管理：`uiCamera`/`uiData`/`mainCamera`/`MainData` 静态 + `AddOverlay`/`RemoveOverlay`/`ResetCamera`（按 order 排序、设 renderType、清/填 cameraStack） |
| `BootOverlayCamera` | Boot | 需要叠加的相机 | OnEnable 时 `AddOverlay(self, order)`、OnDisable `RemoveOverlay`；字段 `order`（排序）、`shotInUI`（→ `ShotInUI`） |
| `BaseCamera` | Core | 主相机（tag MainCamera） | 继承 `BootBaseCamera`；Editor 下 `CheckOverlayCamera` 校验场景相机是否漏挂 `OverlayCamera`、cullingMask 是否误含 UI/CopyUI/UIModel |
| `OverlayCamera` | Core | 场景相机 | 继承 `BootOverlayCamera`；Awake 时把自身 `AddCoverage(this)` 挂到同物件的 `CameraCoverage`/`CameraCoverageChild`（使 Overlay 相机的 enabled 纳入遮挡屏蔽） |

**机制**：`ResetCamera` 把 `cameraStack[0]` 设为 Base，其余设为 Overlay 并依次加入 Base 的 `cameraStack`。`cameraStack` 同时是「外部截屏用相机列表」。

---

## 3. UI 相机

### 3.1 相机实体

UI 相机在启动 prefab（`GameRes/UI/Boot/1/StartScreen.prefab`）里叫 **`UIRootCamera`**（tag `UICamera`，带 `Camera` 组件 + 自定义 `isUICamera:1`）。UI 采用 **Screen Space Camera** Canvas，`ScreenHelper.UICamera = RootCanvas.worldCamera` 即指向它。

### 3.2 三块访问/适配代码

| 类 | 作用 |
|---|---|
| `ScreenHelper.UICamera` / `RootCanvas` | 全局唯一事实源。`SetRootCanvas` 里 `UICamera = canvas.worldCamera`、`RootCanvas = canvas`。所有「世界↔UI 坐标换算」都经它（`WorldPointToUIPoint`/`UIPointToWorldPoint` 等） |
| `UICamera`（`Core/UI/Util/UICamera.cs`） | UI 相机操作门面：缓存 `ScreenHelper.UICamera`；`UpdateCameraPos` 根据分辨率重算 position/orthographicSize/planeDistance（`DEFAULT_PLANE_DISTANCE = 750`）；监听 `ScreenHelper.OnResolutionChange` |
| `UICameraAdapter` | `[ExecuteAlways]` 挂在 UI 相机上，`Update` 里检测 `Screen.width/height` 变化后直接改 `planeDistance=500`、position、orthographicSize（另一套更轻量的适配实现） |

> 两者职责重叠但 planeDistance 不同（750 vs 500），说明是历史演化的两套实现并存。

---

## 4. UI 内嵌 3D 模型相机（UIModel）

把 3D 模型渲染到 `RenderTexture` → 显示到 `RawImage`，是「UI 里显示角色/道具预览」的机制。

| 文件 | 职责 |
|---|---|
| `UIModelImg` | 挂在 `RawImage` 上的总控制器。`Init()` 按序建 RT → 建 `UIModelContent`/`ModelContainer` → 建 `UIModelCamera` 相机；`Play/Pause/Stop/Snapshot` 管理渲染；`AddObject/ReplaceObject/ClearObject` 管理模型；模型及相机 layer 统一设为 `UIModel` |
| `UIModelCam` | 挂在 `UIModelCamera` 上，`Awake` 里 `OnCreateCamera?.Invoke(camera)` —— 这是 `CoverageChecker` 监听动态相机的唯一实际来源 |
| `UIModelLocMgr` | 静态位置分配器：把各 UIModel 分散到 3D 空间（`PER_COUNT=4`、`INTERVAL=500`，最多 64 格），避免模型重叠互相影响；`GetEmptyPos`/`RecyclePos` 复用 |
| `UIModelScreenFitting`（类名 `UIModeScreenFitting`） | 根据相机 aspect 缩放模型，适配不同屏宽高比 |
| `UIModelBg`（Core/UI/Effect） | 用 `ScreenHelper.UICamera.WorldToScreenPoint` 做 UI 模型背景定位 |

---

## 5. Coverage 遮挡相机

「界面全屏遮挡时自动隐藏底层相机/模型」的渲染优化。核心对象是 `BaseCoverage`（显示单位）与 `CoverageChild`（随父显隐的子节点）。

| 类 | 继承 | 作用 |
|---|---|---|
| `BaseCoverage` | — | 显示单位基类，`ShowRectList`/`CoverRectList`/`VerticalSideList` + `SetVisible`/`RegisterChild`；`Holder` 为 `CoverageRoot` |
| `CameraCoverage` | BaseCoverage | 场景相机显示对象。`shieldType`（Enable 切 enabled / ClipPlane 改 farClipPlane）；缓存 `mainCamera`（tag MainCamera）；`AddCoverage(behaviour)` 把外部组件纳入一起屏蔽 |
| `CoverageChild` | — | 子节点基类，`Start` 自动向上 `RegisterChild`，`OnShow/OnHide` 默认切 `gameObject.SetActive` |
| `CameraCoverageChild` | CoverageChild | UI 内相机子节点，`OnShow/OnHide` 切相机 `enabled` + `PhysicsRaycaster`；`AddCoverage` 外部组件 |
| `CameraCoverageChildActive` | CoverageChild | 显式向上找 `CameraCoverage` 注册（区别于普通 `CoverageChild` 找最近 `BaseCoverage`），随父切 `SetActive` |
| `CoverageChecker` | — | 挂 SafeUIRoot 上，监听 `UIModelCam.OnCreateCamera`，为动态相机补挂 `CameraCoverageChild`（相机上层有 Form）或 `CameraCoverage`（无 Form，场景相机） |
| `CoverageCheckerInEditor` | Editor-only | 进 Play 模式校验界面/场景是否漏挂 Coverage；`CoverageChecker.uiCamera/.baseCamera` 是它的排除项 |

**关键点**：`CoverageChecker.uiCamera` / `.baseCamera` 是**序列化占位字段**，运行时代码从不读取；唯一消费方是 Editor 工具 `CoverageCheckerInEditor`（排除 UI 相机/基础相机不参与校验）。37 的 `StartScreen.prefab` 里 `uiCamera` 指向 UIRootCamera、`baseCamera=null`（未启用独立 base 相机）。详见 ROADMAP UI 待实现。

---

## 6. 场景相机显隐策略

| 类 | 作用 |
|---|---|
| `SceneCoverage` | 场景遮挡组件，`_visibleStrategy` 二选一：`CameraCoverage`（交给相机上的 `CameraCoverage`）或 `RootGameObjectsEnable`（切场景根节点 enabled） |
| `SceneVisibleStrategyByCameras` | 控制场景相机「隐藏时如何降级渲染」的可组合策略，`CameraRenderControlFlags`（Flags）：`CullingMask`/`ClearFlags`/`ClipPlane`/`Enable`。隐藏时缓存原始字段、显示时恢复。默认 `Default = CullingMask \| ClearFlags`（只改渲染输出，不动 Camera.enabled），提供 `CreateRenderSafePreset`/`CreateHardOffPreset`/`CreateHybridPreset` 三个预设 |

---

## 7. 相机操控工具

| 类 | 位置 | 类型 | 相机来源 | 作用 |
|---|---|---|---|---|
| `CameraMoveAdv` | Core/UI/Util | 组件 | 序列化 `Cam` 注入，`MainCamera` 回退 `Camera.main` | 最全的操控组件：Move/LookAround/Rotate/Around 四种模式 + 缩放 + 惯性 + 多边形限位；DOTween 补间；静态单例 `Inst` |
| `CameraDrag` | General/Camera | 组件 | 操作自身 transform | 鼠标拖动相机，水平/垂直限位 |
| `VirtualCameraMov` | Core/UI/Util | 组件 | 注入 `CinemachineVirtualCamera` + `camTarget` | 基于 Cinemachine：拖动 Follow 目标在 XZ 平面移动并钳制 |
| `LookAtCamera` | Core/UI/Util | 组件 | 注入 `Transform toCamera` | 公告板：让物体始终面向相机 |
| `GravityCamera` | General/GravitySensor | 组件 | 操作自身 transform | 重力感应（`Input.acceleration`）旋转镜头 |
| `CameraAngle` | Framework/Package/CameraTool | 组件 | `GetComponent<Camera>` | 按角度换算 FOV + 底部适配 |
| `CameraUtil` | General/Utils | 静态 | 缺省回退 `CameraMoveAdv.Inst?.MainCamera` | 视口/屏幕/UI 区域判定与坐标换算 |
| `CameraRoll` | General/Camera | 组件 | 注入 `Transform _trCenter` | 把滚动中心喂给全局 Shader（视差），非相机本体 |
| `CameraExtension` | Core/UnityExtension | 扩展方法 | 参数传入 | World/UI/Screen/Viewport 坐标换算 + `GetSceneGroundPosXYZ`（地面取点） |

---

## 8. Timeline / 战斗相机

### 8.1 Timeline 相机

| 片段 | 位置 | 作用 |
|---|---|---|
| `CameraSwitchClip`/`CameraSwitchTrack` | Framework/External/DefaultPlayables | 通用切镜：淡黑屏时切换两个相机的 GameObject 激活状态，用于剧情/演出切镜 |
| `UIFlowClip`/`UIFlowBehaviour` | Core/TimeLine/UIFlow | UI 跟随：每帧 `uiCamera.WorldToScreenPoint` 把世界物体投影到屏幕，把 UI 元素钉在该点（名牌/血条跟随） |
| `CameraShakeClip`/`CameraShakeBehaviour` | Project.Fight/.../CameraShake | 技能震屏轨：把震动委托给 `FightScene.cameraNode.ShakeCamera`，用 Timeline 时间回扫 DOTween，使震动与技能同步 |

### 8.2 战斗相机

| 类 | 作用 |
|---|---|
| `FightCameraNode` | 战斗相机唯一权威持有者。经自动生成 Binding 拿到相机 `_caCamera` + `_trRoot`/`_trShake`/`_trMove` 子节点；对外暴露 FOV/裁剪面读写、`Get/SetCameraSetting`（位姿快照）、`ShakeCamera`（委托 `_trShake` 上的 `ShakeEffect`，带优先级）、`TweenToFight`（站位↔战斗位 DOTween 补间）、`AddScreenEffect` |
| `LocationAndCameraNode` | 「站位 + 相机」聚合 prefab 根，`GetCameraNode()`/`GetLocationNode()`；由 `FightScene.LoadLocationAndCamera` 实例化并按局填充 |

**震屏相机来源**：`CameraShakeBehaviour` 不直接拿 `Camera.main`，而是 `FightSceneStateUtil.GetFightSceneFightState()` → `FightScene.cameraNode` → `ShakeCamera`，最终物理扰动 `_trShake` 子 Transform 带动相机。优先级来自技能表 `CameraShakeSpriteActionData.GetShakePriority`。

---

## 9. 特效相机 / URP 扩展

| 类 | 位置 | 机制 |
|---|---|---|
| `Mirror` | Core/EffectSystem/Mirror | 平面反射：`beginFrameRendering` 回调里 `CreateMirrorObjects` 动态 `new GameObject(... typeof(Camera))` 建反射相机，改 worldToCameraMatrix/projectionMatrix，`RenderSingleCamera` 渲染到 RenderTexture |
| `PlanarShadow` | Framework/Package/URPSceneEffect | URP 平面阴影：`RenderObjectsPass`（Feature 模式）或 `CommandBufferPass`（CommonBuffer 模式），经 `CustomRenderFeature.Show/Hide` 注册 |
| `CameraColorTexture` | Core/URP | 按需开启颜色纹理（引用计数缓存 + Editor 校验 shader 白名单），供热扰动等效果 |
| `CameraDepthTexture` | Core/URP | 按需开启深度纹理（引用计数缓存 + Editor 校验），供水面/雾等效果 |
| `CustomRenderFeature` | Framework/Package/URPExtension | URP RenderFeature 注册门面，`PlanarShadow` 等依赖 |

---

## 10. Editor 工具

| 工具 | 位置 | 作用 |
|---|---|---|
| `CameraMoveDevEditor` | Core.Editor/GUI/UGUITools | 编辑器里调试相机移动 |
| `CameraAngleEditor` | Framework/Package/CameraTool/Editor | `CameraAngle` 的 Inspector 定制 |
| `ProfilerCameraListener` | Framework/External/SRDebugger | SRDebugger 的相机 Profiler 监听 |
| `OrthCameraCaptureScript` | Project.Base/Art/GroundBlend | 正交相机捕捉（地形混合烘焙），Editor 版另存 |

---

## 11. KJ 移植对照

> 状态图例：✅ 已移植　🟡 部分/位置有差异　❌ 未移植　⛔ 依赖缺失阻塞

| 37 模块 | KJ 对应 | 状态 | 说明 |
|---|---|---|---|
| `ScreenHelper`（UICamera/RootCanvas/安全区/分辨率） | `Scripts/Core/UI/ScreenHelper.cs` | ✅ | D2 已落地 |
| `UICamera` | `Scripts/Core/UI/UICamera.cs` | ✅ | 已移植 |
| `UICameraAdapter` | `Scripts/Core/UI/UICameraAdapter.cs` | ✅ | 已移植 |
| `CameraMoveAdv` | `Scripts/General/Camera/CameraMoveAdv.cs` | ✅ | 已移植（Core/UI/Util → General/Camera，partial 拆分 + 7 功能 Inspector 开关）；DOTween 补间（MoveCamByPos/RotateYCamByPos/MoveCamByAxisLocalX）与偏移累计管理（GetZoomOffset/ResetZoomOffset/SetOffset）已补齐；多边形限位（LimitPosList）与 Ground 射线按设计精简 |
| `UIModelImage`（UIModelImg/UIModelCam/UIModelLocMgr/UIModelScreenFitting） | `Framework/UIEffectExtension/Runtime/UIModelImage/` | ✅ | 4 文件全量移植 |
| `Framework/Coverage`（CameraCoverage/CameraCoverageChild/CoverageChild/BaseCoverage 等） | `Framework/Coverage/` | ✅ | D7 已落地 |
| `CoverageChecker` | `Core/ViewSystem/Coverage/CoverageChecker.cs` | ✅ | 已移植（uiCamera/baseCamera 占位字段保留） |
| `CoverageCheckerInEditor` | — | ❌ | Editor 校验工具未移植，待场景需求（见 ROADMAP） |
| `SceneCoverage` / `SceneVisibleStrategyByCameras` / `CameraRenderControlFlags` | `Core/ViewSystem/Coverage/SceneCoverage.cs` 等 | ✅/🟡 | SceneCoverage 已移植；`SceneVisibleStrategyByCameras` 需核对是否引入 |
| `BootBaseCamera` / `BootOverlayCamera` / `BaseCamera` / `OverlayCamera` | `Scripts/Core/URP/CameraStackBase.cs`、`CameraStackOverlay.cs`、`BaseCamera.cs`、`OverlayCamera.cs` | ✅ | 已移植（Phase 1）。按 Phase 0 决策用标准 URP `renderType`/`cameraStack` 替代 37 私有 `ShotInUI`/`isUICamera`；`Boot*` 改名 `CameraStack*`（落 Core 而非 Boot）；去 `uiCamera/uiData`（KJ 已有 `Core.UI.UICamera`）；Editor 校验简化为 cullingMask 不含 UI 层 |
| `CameraColorTexture` / `CameraDepthTexture` | — | ⛔ | 同上，依赖自定义 URP 字段 |
| `CameraSwitch`（DefaultPlayables） | — | ⛔ | 依赖 `ShotInUI`；且属过场切镜能力，按需 |
| `Mirror` / `PlanarShadow` / `CustomRenderFeature` | — | ❌ | URP 扩展，依赖 URP + 自定义 RenderFeature 体系 |
| `VirtualCameraMov` | — | ⛔ | 依赖 Cinemachine（KJ 未引入） |
| `CameraUtil` | `Scripts/General/Utils/CameraUtil.cs` | ✅ | 已移植（视口/屏幕/UI 区域判定 + 坐标换算；37 的 UtilUIKit.IsInScreen 内联为包围盒重叠；Debug.LogWarning → GameLog） |
| `CameraExtension` | `Scripts/Core/UnityExtension/CameraExtension.cs` | ✅ | 已移植（World/Screen/Viewport/UI 坐标互转 + GetSceneGroundPosXYZ 地面取点；依赖 ScreenHelper 新增 WorldPointToUIPoint/UIPointToWorldPoint） |
| `CameraDrag` / `CameraRoll` / `LookAtCamera` / `GravityCamera` / `CameraAngle` | — | ❌ | 相机操控/工具类，按业务需要移植 |
| `UIFlow` / `CameraShake` / `FightCameraNode` / `LocationAndCameraNode` | — | ❌ | Timeline 相机 + 战斗相机，游戏特定 |
| `SkeletonCoverageChild` | — | ⛔ | 依赖 Spine |

### 关键结论

1. **URP 状态已变化**：KJ 的 `manifest.json` 现在已含 `com.unity.render-pipelines.universal: 14.0.8`（**标准版**）。但 37 用的是**本地修改版 URP**，在 `UniversalAdditionalCameraData` 上加了 `ShotInUI`/`showDepthTextrue`/`showColorTextrue` 三个私有字段。**相机栈（Base/Overlay）与按需纹理这两条线都依赖这些字段，不能照搬。**
2. **相机渲染栈是 37 相机系统的骨架**：`BootBaseCamera` 的 `cameraStack` 统一管理 Base/Overlay，KJ 目前没有对应物（KJ 的 UIRoot 是裸 Canvas + 单 UI 相机）。若 KJ 需要「场景相机 + UI 相机 + 特效相机」多相机叠加，这是第一个要补的。
3. **UI 内嵌 3D 模型（UIModel）与 Coverage 遮挡已基本就绪**，这是最可能先被业务用到的两块，KJ 已移植运行时层。
4. **游戏特定层（战斗相机/震屏/Timeline 切镜/重力感应）不在框架移植范围**，属 Project/General 业务，按需实现。

---

## 12. 参考文件速查

| 作用 | 37 路径（`F:\int_37_pack\client`） |
|---|---|
| URP 相机栈 | `Assets/ScriptsC#/Boot/Update/View/BootBaseCamera.cs`、`BootOverlayCamera.cs`、`Assets/ScriptsC#/Core/URP/BaseCamera.cs`、`OverlayCamera.cs` |
| UI 相机 | `Assets/ScriptsC#/Core/UI/Screen/ScreenHelper.cs`、`Core/UI/Util/UICamera.cs`、`UICameraAdapter.cs` |
| UI 内嵌模型 | `Assets/Framework/Package/UIEffectExtension/Runtime/UIModelImage/*.cs` |
| Coverage | `Assets/Framework/Package/Coverage/*.cs`、`Assets/ScriptsC#/Core/ViewSystem/Coverage/*.cs` |
| 场景显隐策略 | `Assets/Framework/Package/View/BaseView/Scene/Visible/SceneVisibleStrategyByCameras.cs` |
| 相机操控 | `Assets/ScriptsC#/Core/UI/Util/CameraMoveAdv.cs` 等、`Assets/ScriptsC#/General/Camera/*` |
| 特效相机 | `Assets/ScriptsC#/Core/EffectSystem/Mirror/Mirror.cs`、`Assets/Framework/Package/URPSceneEffect/Runtime/PlanarShadow.cs`、`Assets/ScriptsC#/Core/URP/CameraColorTexture.cs`、`CameraDepthTexture.cs` |
| Timeline/战斗 | `Assets/Framework/External/DefaultPlayables/CameraSwitch/*`、`Assets/ScriptsC#/Core/TimeLine/UIFlow/*`、`Assets/ScriptsC#/Project.Fight/FightPresentation/*` |


---

## Part B — 移植计划（Phase 0-8）

## 0. 总览与依赖顺序

```
Phase 0   URP 决策（✅ 已定：方案 B 标准 URP，不 fork）
   │
Phase 1   URP 相机栈（BootBaseCamera/OverlayCamera 等）
   │
   ├── Phase 2  按需纹理（ColorTexture/DepthTexture）
   ├── Phase 3  URPExtension RenderFeature 门面
   │               │
   │               └── Phase 4  特效相机（Mirror/PlanarShadow/HighQualityShadow）
   │
   ├── Phase 5  Timeline 相机（CameraSwitch/UIFlow）
   │
Phase 6   相机操控工具（CameraMoveAdv 补全/Drag/LookAt/Gravity/Angle/Util/Extension）
Phase 7   Editor 工具（CoverageCheckerInEditor 等）
Phase 8   战斗相机（游戏特定，明确后置）
```

**关键依赖引入**：DOTween（CameraMoveAdv 补间 / FightCameraNode）、Cinemachine（VirtualCameraMov）、Spine（SkeletonCoverageChild）。

---

## Phase 0 — URP 方案决策（🔴 阻塞项）

> 37 用的是**本地修改版 URP 14.0.8**，对 `UniversalAdditionalCameraData` 和渲染管线做了私有改动。KJ 当前 manifest 里是**标准版** 14.0.8。所有下游（相机栈/按需纹理/切镜）都依赖此决策。

### 37 的 URP 私有改动清单（需复刻/替代）

| # | 改动 | 位置（`Packages/com.unity.render-pipelines.universal@14.0.8/`） | 作用 | 依赖方 |
|---|---|---|---|---|
| 1 | `isUICamera` 字段 + `useUILineToSRGB` 颜色空间管线 | `UniversalAdditionalCameraData.cs`、`UniversalRenderPipeline.cs`、`UniversalRenderPipelineCore.cs`、`FinalBlitPass.cs`、`PostProcessPass.cs`、`RenderTargetBufferSystem.cs` | UI 相机在线性空间下的 sRGB 转换（UI 线性化渲染） | 相机栈、UI 相机正确性 |
| 2 | `ShotInUI` 字段 | `UniversalAdditionalCameraData.cs:372` | 截屏时是否含该相机 | `BootOverlayCamera`、`CameraSwitch` |
| 3 | `showDepthTextrue` 字段 | `UniversalAdditionalCameraData.cs:482` | 按需开启深度纹理 | `CameraDepthTexture` |
| 4 | `showColorTextrue` 字段 | `UniversalAdditionalCameraData.cs:507` | 按需开启颜色纹理 | `CameraColorTexture` |

### 决策结论（2026-08-16，✅ 已定：方案 B，不 fork URP）

调查 37 改包 URP（`F:\int_37_pack\client\Packages\com.unity.render-pipelines.universal@14.0.8`，逐文件 diff 标准 14.0.12）后，发现原「4 处改动」的清单**严重低估了实际改动量**：

| 原表 # | 字段 | 实际性质 |
|---|---|---|
| 1 | `isUICamera` + `useUILineToSRGB` | ❌ **不是简单字段**，而是 37 整套「UI 分离渲染」系统：`isUISplit`/`useBlit`/MRT/PreZ、`RenderTargetBufferSystem` 整体重写（UI/def 双缓冲）、`UniversalRenderPipeline.cs` 约 50 处 hunk、`UniversalRenderPipelineCore.cs` 新增 15+ 字段。游戏特定渲染架构，**不可移植、框架也不需要** |
| 2 | `ShotInUI` | 游戏特定「截屏只含 UI」开关，且用法与 `isUICamera` 同表达式纠缠（`!isUICamera && !ShotInUI`） |
| 3 | `showDepthTextrue` | 37 用自定义 bool **硬改替代**标准 `requiresDepthTexture` getter（绕开官方 option 机制） |
| 4 | `showColorTextrue` | 同上，替代标准 `requiresColorTexture` |

**关键结论**：标准 URP 14.0.12 已原生提供全部框架所需 API——`CameraRenderType`（Base/Overlay）、`cameraStack`、`requiresDepthTexture`、`requiresColorTexture`。因此：

- **相机栈（Phase 1）** 用标准 `renderType`/`cameraStack` 即可，无需 `ShotInUI`（截图是游戏特定功能）。
- **按需纹理（Phase 2）** 用标准 `requiresDepthTexture`/`requiresColorTexture` 即可，无需 `showDepth/ColorTextrue`。
- **不 fork URP**，规避分支维护成本与渲染破坏风险。

> **决策输出物（方案 B 替代映射表）**：`showDepthTextrue`→`requiresDepthTexture`、`showColorTextrue`→`requiresColorTexture`、`isUICamera`→`CompareTag("UICamera")`、相机栈→`renderType`/`cameraStack`、`ShotInUI`/UI 分离渲染→不移植（游戏特定）。

---

## Phase 1 — URP 相机栈（依赖 Phase 0）

| # | 待移植文件（37） | KJ 目标位置 | 依赖 | 说明 |
|---|---|---|---|---|
| 1.1 | `Boot/Update/View/BootBaseCamera.cs` | `Scripts/Core/URP/CameraStackBase.cs` | 标准 URP（renderType/cameraStack） | ✅ 全局 `Stack` 管理 Base/Overlay（改标准 API，去 uiCamera/uiData） |
| 1.2 | `Boot/Update/View/BootOverlayCamera.cs` | `Scripts/Core/URP/CameraStackOverlay.cs` | 1.1 | ✅ Overlay 相机 OnEnable 挂栈（去 shotInUI） |
| 1.3 | `Core/URP/BaseCamera.cs` | `Scripts/Core/URP/BaseCamera.cs` | 1.1 | ✅ 主相机 + 最小 Editor 校验（cullingMask 不含 UI 层） |
| 1.4 | `Core/URP/OverlayCamera.cs` | `Scripts/Core/URP/OverlayCamera.cs` | 1.2 + Coverage | ✅ 挂接 CameraCoverage/CameraCoverageChild |

**验证**：场景相机 + UI 相机 + 一个特效相机同时存在时，`cameraStack` 正确维护 1 Base + N Overlay，渲染层级正确；Editor 下 `CheckOverlayCamera` 能报出漏挂 OverlayCamera 的场景相机。

---

## Phase 2 — 按需纹理（依赖 Phase 0）

| # | 待移植文件 | KJ 目标位置 | 依赖 | 说明 |
|---|---|---|---|---|
| 2.1 | `Core/URP/CameraColorTexture.cs` | `Scripts/Core/URP/` | Phase 0（showColorTextrue） | 颜色纹理按需开关 + Editor shader 校验 |
| 2.2 | `Core/URP/CameraDepthTexture.cs` | 同上 | Phase 0（showDepthTextrue） | 深度纹理按需开关 + Editor shader 校验 |

**验证**：挂 `CameraDepthTexture` 后水面/雾 shader 能读到深度；移除后 `showDepthTextrue=false`（引用计数归零）。

---

## Phase 3 — URPExtension RenderFeature 门面（依赖 Phase 1）

| # | 待移植文件 | KJ 目标位置 | 依赖 | 说明 |
|---|---|---|---|---|
| 3.1 | `URPExtension/Runtime/CustomRenderFeature.cs` | `Framework/URPExtension/` | Phase 1 | RenderFeature 注册门面（Show/Hide） |
| 3.2 | `URPExtension/Runtime/CommandBuffer/CommandBufferPass.cs` | 同上 | 3.1 | CommandBuffer 通道 |
| 3.3 | `URPExtension/Runtime/CommandBuffer/CommandRenderer.cs` | 同上 | 3.2 | CommandBuffer 渲染器 |
| 3.4 | `URPExtension/Runtime/CommandBuffer/InstancingRenderPass.cs` | 同上 | 3.1 | 实例化通道 |
| 3.5 | `URPExtension/Runtime/SetupAble.cs`、`MaterialPropertyBlockCache.cs` | 同上 | — | 辅助 |
| 3.6 | `URPExtension/Runtime/Instancing/*`、`OverDrawStatic.cs` | 同上 | — | 实例化渲染（可选，按需） |

**验证**：一个最小 RenderFeature 能经 `CustomRenderFeature.Show/Hide` 注册/卸载并在正确时机执行。

---

## Phase 4 — 特效相机（依赖 Phase 3）

| # | 待移植文件 | KJ 目标位置 | 依赖 | 说明 |
|---|---|---|---|---|
| 4.1 | `Core/EffectSystem/Mirror/Mirror.cs` | `Scripts/Core/EffectSystem/` | Phase 1 | 平面反射（动态反射相机） |
| 4.2 | `URPSceneEffect/Runtime/PlanarShadow.cs` | `Framework/URPSceneEffect/` | Phase 3 | 平面阴影（Feature/CommandBuffer 两模式） |
| 4.3 | `URPExtension/Runtime/Shadow/HighQualityShadow*.cs` | `Framework/URPExtension/` | Phase 3 | 高质量阴影（可选） |

**验证**：Mirror 反射正确、PlanarShadow 阴影朝向/颜色/平面跟随正确，切换 Feature/CommonBuffer 模式均正常。

---

## Phase 5 — Timeline 相机（依赖 Phase 1）

| # | 待移植文件 | KJ 目标位置 | 依赖 | 说明 |
|---|---|---|---|---|
| 5.1 | `External/DefaultPlayables/CameraSwitch/*` | `Framework/External/DefaultPlayables/` 或等价 | Phase 0（ShotInUI） | 过场切镜（淡黑切换相机） |
| 5.2 | `Core/TimeLine/UIFlow/*` | `Scripts/Core/TimeLine/` | 标准 URP 即可 | UI 跟随（WorldToScreenPoint） |

**验证**：Timeline 里能切相机（淡入淡出）、UI 元素钉在世界物体上跟随。

---

## Phase 6 — 相机操控工具（独立，不依赖 URP）

| # | 待移植文件 | KJ 目标位置 | 依赖 | 说明 |
|---|---|---|---|---|
| 6.1 | `Core/UI/Util/CameraMoveAdv.cs`（补全） | `Scripts/General/Camera/` | DOTween | ✅ 已补全（2026-08-16）：Move/LookAround/Rotate/Around + 缩放/惯性/限位 + DOTween 补间已就绪，补齐 `MoveCamByAxisLocalX` 与偏移累计管理 API（`GetZoomOffset`/`ResetZoomOffset`/`SetOffset`） |
| 6.2 | `General/Camera/CameraDrag.cs`、`CameraRoll.cs` | `Scripts/General/Camera/` | — | 拖动、Shader 全局参数 |
| 6.3 | `Core/UI/Util/LookAtCamera.cs` | `Scripts/Core/UI/` | — | 公告板 |
| 6.4 | `General/GravitySensor/GravityCamera.cs` | `Scripts/General/GravitySensor/` | — | 重力感应 |
| 6.5 | `Framework/Package/CameraTool/CameraAngle.cs` | `Framework/CameraTool/` | — | FOV 角度适配 |
| 6.6 | `General/Utils/CameraUtil.cs` | `Scripts/General/Utils/` | 6.1 | ✅ 已移植：视口/屏幕/UI 区域判定（37 `UtilUIKit.IsInScreen` 内联为包围盒重叠，`Debug.LogWarning` → `GameLog`） |
| 6.7 | `Core/UnityExtension/CameraExtension.cs` | `Scripts/Core/UnityExtension/` | — | ✅ 已移植：坐标换算扩展（依赖 `ScreenHelper` 新增 `WorldPointToUIPoint`/`UIPointToWorldPoint`） |
| 6.8 | `Core/UI/Util/VirtualCameraMov.cs` | `Scripts/Core/UI/` | **Cinemachine** | Cinemachine 拖动 Follow |

**验证**：各操控组件在 Play 下正常驱动相机，CameraUtil 判定正确。

---

## Phase 7 — Editor 工具（依赖 Phase 1 + Coverage）

| # | 待移植文件 | KJ 目标位置 | 依赖 | 说明 |
|---|---|---|---|---|
| 7.1 | `Core.Editor/ViewSystem/Coverage/CoverageCheckerInEditor.cs` | `Scripts/Core.Editor/ViewSystem/Coverage/` | Coverage + Phase 1 | 进 Play 校验漏挂 Coverage（含 uiCamera/baseCamera 排除项）；裁剪 Spine 的 `SkeletonCoverageChild` 部分 |
| 7.2 | `CameraTool/Editor/CameraAngleEditor.cs` | `Framework/CameraTool/Editor/` | 6.5 | CameraAngle Inspector |
| 7.3 | `Core.Editor/GUI/UGUITools/CameraMoveDevEditor.cs` | `Scripts/Core.Editor/GUI/` | 6.1 | 编辑器相机调试 |

**验证**：进 Play 模式，界面/场景漏挂 Coverage 会报错；Inspector 正常。

---

## Phase 8 — 战斗相机（🟢 游戏特定，明确后置）

| # | 待移植文件 | 说明 |
|---|---|---|
| 8.1 | `FightCameraNode` / `LocationAndCameraNode` | 战斗相机本体（站位↔战斗补间、震屏、FOV、屏幕特效） |
| 8.2 | `CameraShake*`（Timeline） | 技能震屏轨 |
| 8.3 | `UIFlow`/`CameraSwitch` 之外的战斗切镜 | 按战斗系统需求 |

> 依赖 DOTween + 战斗系统（`FightScene`/`FightSceneStateUtil`/`CameraShakeSpriteActionData`）未落地，**不在当前移植范围**，战斗系统立项后再做。

---

## 迁移执行顺序总结

1. **先定 Phase 0**（URP fork vs 替代）——这是唯一硬阻塞，决定后续代码能否照搬。
2. **Phase 1 → 2/3 → 4/5** 依序推进（渲染栈是骨架，特效相机/切镜是叶子）。
3. **Phase 6/7 可并行**（不依赖 URP 决策，优先补 UI 内嵌模型 + 操控工具，业务最可能先用）。
4. **Phase 8 后置**，等战斗系统。

### 建议的第一批（无 URP 阻塞、业务价值高）

- ✅ 已完成：UIModelImage 四件套、Framework/Coverage、CoverageChecker、ScreenHelper/UICamera/UICameraAdapter。
- ✅ 已完成（2026-08-16）：**Phase 6.1（CameraMoveAdv 补全）+ 6.6（CameraUtil）+ 6.7（CameraExtension）**——纯 C# 无 URP 依赖，UI 模型预览/镜头操控已可用。
- 下一步可选：Phase 6.2~6.8 其余操控工具（`CameraDrag`/`CameraRoll`/`LookAtCamera`/`GravityCamera`/`CameraAngle`/`VirtualCameraMov`），按业务需要移植；其中 `VirtualCameraMov` 依赖 Cinemachine（KJ 未引入）。
