# TMP 文本体系移植分析（P33 → KJ）

> 2026-08-16 | 目标：梳理 P33 的 t2d/t3d（`Core/TextMeshPro/`）功能全景，标注哪些 KJ 需要、哪些是 P33 业务绑定，供后续实现参考。
> 现状：PSD2UGUI 移植时已剥离 P33 的自定义 TMP 体系，当前生成的文本是**裸标准 `TextMeshProUGUI`**（只设 text/color/fontSize/fontStyle/alignment/characterSpacing + 分段颜色富文本）。

## 1. 核心类（P33 运行时）

| 类 | 基类 | 行数 | 职责 |
|---|---|---|---|
| `TextMeshPro2D` | `TextMeshProUGUI` | 164 | UI 文本（t2d） |
| `TextMeshPro3D` | `TextMeshPro` | 161 | 世界/3D 文本（t3d） |
| `TMP_TextEx` | — | 1269 | **扩展核心**：两个文本类几乎把所有功能委托给它 |
| `TextMeshProParser` | — | 584 | 简化标签解析（色号/表情/预制体/本地化） |
| `TextMeshProMgr` | IModule | 768 | 颜色配置、文本属性表、自定义图片/预制体 |
| `TextMeshProStyle` | — | 1139 | 编辑器端：样式批量生成/替换/刷新 |
| `TMPStyleMgr` / `TmpMatStyle` / `TmpMatStyleAssets` | — | 51/68/20 | 预制样式数据 + 加载 |
| `ITextMeshPro` | — | 18 | 统一接口 `TextEx()` + `SetSpriteAsset()` |

关键架构：`TextMeshPro2D/3D` 只做壳，持有 `TMP_TextEx _textEx`，把 `text` 属性 setter 拦截、样式应用、文本属性、内嵌内容高度等全部委托给 `_textEx`。

## 2. 功能全景（按模块）

### A. 文本属性表（textTid）— 程序只传 id，样式由配置统一管理
- 数据：`TextProperty{ id, font, fontSize, fontColor, isBold, fontGradient, isInstanceMat, glow }`
- 入口：`SetTextProperty(int propertyTid)`
- 数据源：**Lua 桥接委托** `GetProperty` / `SetPropertyByLua`（P33 业务绑定）

### B. 预制样式（TmpMatStyle / StyleName）— 美术定义整套字体效果，按名复用
- 数据：`TmpMatStyle{ name, fontName, vertexGradient, faceDilate, outlineColor/Width/Softness(描边), underlay*(投影), glow*(外发光), sweepLight*(扫光) }`
- 加载：`TMPStyleMgr.LoadConfig()` 读 `TMP_Style.txt`（JSON）
- 入口：`SetStyleByName(styleName)` → `TMP_TextEx.ApplyStyle()` / `FlushStyle()` / `SetOutlineColor` / `SetUnderlayColor` / `SetGlowColor` / `SetFaceDilate` 等
- 编辑器：`TextMeshProStyle` 批量生成/替换字体样式资产

### C. 颜色配置表（`c17` → 色值）— 统一管理颜色，简短编码引用
- `GetColorCfg()` → `Dictionary<string,string>`
- 简化标签 `<color=c17>` → 完整色值；`GetCurColorCode` 反查

### D. 简化标签解析（`TextMeshProParser.AnalysisTag`）— 富文本增强
- 颜色简化标签、自定义图片/表情占位、自定义预制体占位、本地化标签（`pl_`）、maxHeight/voffset
- 零 GC 优化（手动扫描替代 Regex）

### E. 自定义图片/表情 + 预制体嵌入 — 文本里嵌表情/图标/预制体
- `ShowCustomImage`（占位符 `v.color.a==0` → 图片节点）、`ShowCustomPrefab`（嵌入预制体）
- 支撑类：`TextMeshProImage`(2016)、`TMPImageAnimator`(表情动画)、`TextMeshProRenderer`、`TextMeshProDissolve`

### F. 本地化 + 格式化文本
- `SetLocalText(int textId, params)`、`SetText(txt, params)`（`{0}` 占位符）、`_useLocalize`/`_formatL`

### G. 竖向文本 + 排版扩展
- `_verticalText`、`_isLeftToRight`、`_isStaticRotation`、`_isFirstWordOfLine`、`GetCompleteHeight()`（含内嵌预制体的完整高度）

### H. UIState 状态元素（t2d）— 状态切换时改文本表现
- `T2DColorElement / T2DFontStyleElement / T2DOutlineElement / T2DFontSizeElement / T2DGradientElement / T2DCharacterSpacingElement / T2DTextElement / T2DOutlineSizeElement / T2DTranslateTextCodeElement / T2DTranslateTextIdElement`

### I. 材质管理 + 置灰
- `_enableInsMat`（按需实例化材质，避免污染共享材质）、`TMPMaterialMgr`、`IsWhiteColor`（置灰判断）

### J. 编辑器工具（`Core.Editor/TextMeshPro/`）
- `TextMeshProEditor2D/3D`、`TextMeshProEditorEx/Extend`(30KB)、`TextMeshProStyle`(批量样式)、`TMPFontCharacterChecker`、`TMPSpriteImporter`、`TMP_TextExDrawer`

## 3. 为什么"臃肿"

1. **功能本身多**：描边/投影/外发光/扫光/渐变 + 表情 + 预制体 + 本地化 + 竖排 + 格式化 + 状态切换，全叠在 `TMP_TextEx`（1269 行）一个类里。
2. **业务强绑定**：文本属性表走 Lua 配置（`GetProperty`/`SetPropertyByLua`）、挂靠 `App`/`Module` 系统、本地化/表情/预制体都依赖 P33 自有体系、状态切换绑定 P33 的 UIState。
3. **材质参数散改**：`ApplyOutlineWidthToMaterial`/`ApplyFaceDilateToMaterial` 等直接改 TMP 材质实例，逻辑分散且易出 bug。
4. **编辑器工具庞大**：30KB 的 `TextMeshProEditorExtend` + 批量字体替换工具，多为 P33 一次性的美术工作流。

## 4. KJ 建议实现（分优先级）

### P0 — 基础（先落地，解锁 PSD2UGUI 可用）
1. **标准 TMP** 直接用（已具备 `com.unity.textmeshpro`）。
2. **字体资产**：导入正式 `TMP_FontAsset`，建一个默认字体配置入口（当前 `GameRes/Font` 为空，文本会落到默认 LiberationSans）。
3. **简化颜色标签** `<color=c17>` → 色值：一个轻量 `TextMeshProParser`（只保留颜色替换，去掉表情/预制体/本地化）+ 颜色配置 ScriptableObject。

### P1 — 样式复用（美术工作流核心）
4. **文本样式**（P33 的 TmpMatStyle 精简版）：`TextStyle` ScriptableObject（描边/投影/外发光/渐变/扫光）+ 一个扩展方法把样式应用到 `TextMeshProUGUI`。替代 P33 的 `TMP_TextEx` + `TMPStyleMgr` + 编辑器批量工具。
5. **文本属性表**（TextProperty 精简版）：font/fontSize/fontColor/isBold/gradient，一个配置表 + `Apply(int tid)`。

### P2 — 按需（等具体需求再上）
6. **表情/图片嵌入**：用 TMP 标准 `<sprite>` 精灵图集替代 P33 的自定义占位符方案（更标准、更省）。
7. **预制体嵌入**：P33 方案复杂，KJ 有需求再设计。
8. **竖向文本 / 完整高度计算**：按需。
9. **状态切换元素**：等 KJ 的 UI 状态方案定型后，在 KJ 框架层做。
10. **本地化**：等 KJ 本地化方案定型后接入。

### 不移植（P33 业务绑定）
- Lua 配置驱动（`GetProperty`/`SetPropertyByLua`）、`App`/`Module` 系统。
- P33 的自定义表情/预制体/本地化体系。
- 那套 30KB 的编辑器批量工具（一次性的美术工作流）。

## 5. 与 PSD2UGUI 的关系

- 现状：PSD2UGUI 生成文本 = 裸 `TextMeshProUGUI` + 基础属性 + 分段颜色富文本（`<color=#hex>`）。
- P33 里 PSD 图层名 `t2d@fswb@c17` 这类 tag（样式/色号）在移植时被剥离了。
- **后续接入点**：等 P1 的 `TextStyle` 落地后，把 PSD2UGUI 文本生成改回「解析 `@fsxx@cxx` tag → 应用 KJ 的 TextStyle + 简化色号」，即恢复「美术样式复用」能力，但走 KJ 自己的轻量体系。

## 6. 关键源码索引（P33，供回溯）

- `ScriptsC#/Core/TextMeshPro/TextMeshPro2D.cs` / `TextMeshPro3D.cs` / `TMP_TextEx.cs`
- `ScriptsC#/Core/TextMeshPro/TextMeshProParser.cs` / `TextMeshProMgr.cs`
- `ScriptsC#/Core/TextMeshPro/TMPStyleMgr.cs` / `TmpMatStyle.cs` / `TmpMatStyleAssets.cs`
- `ScriptsC#/Core/TextMeshPro/TextMeshProStyle.cs`（运行时样式应用）+ `ScriptsC#/Core.Editor/TextMeshPro/TextMeshProStyle.cs`（编辑器批量）
- `ScriptsC#/Core/Utils/UIState/Element/t2d/*`（状态元素）
