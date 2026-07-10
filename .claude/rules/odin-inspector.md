# Odin Inspector 使用规范

> 2026-07-09 | 位置 `Assets/Framework/1External/Sirenix/` | asmdef: `Framework.External.Sirenix.Odin.Editor`

## 引用规则

- Editor asmdef 按需添加 `Framework.External.Sirenix.Odin.Editor` 到 references，用到再加
- Runtime 代码**默认不引用** Odin Attributes DLL；`[SerializeField]` 足够。确有需要时引用 `Sirenix.OdinInspector.Attributes`，禁止引用 `*.Editor.dll`

## 使用优先级

1. **属性美化优先** — `[BoxGroup]` `[FoldoutGroup]` `[TitleGroup]` `[HorizontalGroup]` `[TabGroup]` `[LabelText]` `[ReadOnly]` `[ShowInInspector]` `[PropertyOrder]` `[InfoBox]` `[InlineProperty]` `[GUIColor]` `[HideLabel]`
2. **交互增强** — `[Button]` `[ShowIf]`/`[HideIf]` `[EnableIf]`/`[DisableIf]` `[OnValueChanged]` `[ValueDropdown]`
3. **EditorWindow** — 新建窗口用 `OdinEditorWindow` / `OdinMenuEditorWindow` 替代原生 `EditorWindow`

## 红线

- **属性优先**：能组合 Odin 属性就不写 `CustomEditor`/`PropertyDrawer`
- **无副作用**：`[ShowInInspector]` getter 不触发逻辑、`[Button]` 回调不做 Runtime 状态修改
- **性能**：`[ShowInInspector]` getter 不重计算；`[ValueDropdown]` 数据源缓存；`[TableList]` 超 100 项加分页
- **现有工具不强制迁移**：`BuildStagePanel`、`RuntimeLogEditorTools`、`KJHybridClrBuildTools` 保持现状，新增/重写时再用 Odin
- `#if UNITY_EDITOR` 不应散落——Editor 逻辑放 `*.Editor.asmdef`

## 参考

Odin 当前配置：Inspector 默认启用；模块仅启用 Unity.Mathematics；其余按需开启。
