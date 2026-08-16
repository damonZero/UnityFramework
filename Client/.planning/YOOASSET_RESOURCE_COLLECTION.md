# YooAsset 资源收集与分包配置（规划文档）

> 状态：**待开发**（不属于本次 UI 框架移植范畴，独立后续开发）
> 记录日期：2026-08-16
> 关联：`.planning/UI_FRAMEWORK_REVIEW_37.md`（UI 框架对照审查）

---

## 1. 背景与定位

本次 UI 框架移植（View/MVVM/Navigation/Coverage/Touch/编辑器工具）已完成，但 **DemoForm 预制体要真正被 `IAssetSystem.LoadAssetAsync` 加载，还缺「YooAsset 资源收集配置」这一步**。这一步属于「资源/构建管线」范畴，与 UI 框架无关，故单独立档，后续开发。

## 2. YooAsset 版本

- **3.0.3-beta**（git commit `bfb166ad91`，manifest 里 `com.tuyoogame.yooasset` 走 `?path=Assets/YooAsset`，未锁 tag）。
- `IAddressRule` 接口在 `YooAsset.Editor`，签名：
  ```csharp
  public interface IAddressRule
  {
      string GetAssetAddress(AddressRuleData data);
  }
  // AddressRuleData: AssetPath(全路径) / CollectPath(收集路径) / GroupName / UserData
  ```

## 3. 现状

- `Assets/BundleCollectorSetting.asset` 只有一个 `DefaultPackage`，一个 `HotUpdate` group，两个 collector（`GameRes/HotUpdate/Dlls`、`GameRes/HotUpdate/AotMetadata`），全部 `PackRawFile`。
- `P4_BuildAssetStage` 用 `RawFileBuildPipeline + EBundleType.RawBundle`（专为 HybridCLR DLL 热更）。
- **结论：UI prefab 等常规资源当前完全未被收集**，`LoadAssetAsync<GameObject>("DemoForm.prefab")` 必然失败。
- 且 RawFile 管线不能承载常规资源（会把 prefab 当原始文件打包），所以常规资源需另建 **Builtin 管线的 Package**。

## 4. 内置地址规则（3.0.3-beta 仅 4 条，均不带扩展名/不带完整路径）

| 规则 | 地址结果（以 `Assets/GameRes/UI/Project/DemoForm.prefab` 为例） |
|---|---|
| `AddressDisable` | 空 |
| `AddressByFileName` | `DemoForm` |
| `AddressByGroupAndFileName` | `UI_DemoForm` |
| `AddressByFolderAndFileName` | `Project_DemoForm` |

⚠️ 此版本**没有** `AddressByFilePath`（完整路径规则）。

## 5. 地址规则决策：自定义「相对收集根 + 带扩展名」

需求：地址带扩展名（如 `.prefab`），但不强制带物理路径（`Assets/...`），且多语言时地址跨包稳定。

决策：自定义 `IAddressRule`，返回「相对收集根的逻辑路径 + 扩展名」。

```csharp
using System.IO;
using YooAsset.Editor;

namespace Framework.Asset.Editor
{
    [DisplayName("定位地址: 相对收集根(带扩展名)")]
    public class KJAddressByRelativePath : IAddressRule
    {
        public string GetAssetAddress(AddressRuleData data)
        {
            var collect = data.CollectPath.Replace('\\', '/').TrimEnd('/');
            var asset = data.AssetPath.Replace('\\', '/');
            return asset.StartsWith(collect)
                ? asset.Substring(collect.Length).TrimStart('/')
                : Path.GetFileName(asset);
        }
    }
}
```

地址长短由「收集路径粒度」控制：
- 收集路径 = `Assets/GameRes/UI` → 地址 `Project/DemoForm.prefab`
- 收集路径 = `Assets/GameRes/UI/Project` → 地址 `DemoForm.prefab`

## 6. 多语言方案：多 Package + 同逻辑路径

- Package 之间**地址空间隔离**：同一地址字符串在不同包内指向各自资源，跨包同名不冲突。
- 多语言：`zh` 包收集 `Assets/Lang/zh/UI`，`en` 包收集 `Assets/Lang/en/UI`，内部结构一致 → 两包都产生地址 `Project/DemoForm.prefab`，按当前语言包加载。
- 关键：地址用「逻辑相对路径」，**不含**会随语言/目录变的物理前缀（`Assets/Lang/zh/...`）。

## 7. 多 Package 优缺点

**优点**：独立版本/独立更新/独立下载（活动包、语言包、DLC 按需下载）；生命周期隔离（卸载活动包不动基础包）；按平台/渠道打子集。

**缺点**：
- 每个 Package 独立 `InitializeAsync + UpdateManifestAsync` + 缓存 + 下载失败处理，复杂度上升。
- KJ 当前单包硬编码（`AssetConfig.PackageName` 单值、`AssetRuntime._defaultPackage` 单字段、`IAssetSystem.LoadAssetAsync(path)` 无 package 参数），多包需改造 API。
- 内存多一份 manifest/缓存字典（开销小）。

**建议**：
- 起步 **2 个 Package**：`DefaultPackage`（RawFile，DLL）+ `GameRes`（Builtin，游戏资源）。
- 目录维度（UI/Scene/Config/Effects）用 **Group/Collector** 分，不建 Package。
- 只有「独立发版/按需下载」的资源（语言包、活动/DLC）才新增 Package。
- 概念对齐：**Package = 运行期独立更新/下载单元（粗、少）；Group/Collector = 构建期目录分包（细、多）**。37 的自研系统只有一级「bundle」，对应 YooAsset 的 Group/Collector，不是 Package。

## 8. 37 分包 → YooAsset 映射

| 37 分包思路 | YooAsset 等价 |
|---|---|
| 按目录分包（目录=一个 bundle） | `MainAssetCollector` + `PackDirectory` |
| 公共资源 `_common` 提取 | YooAsset 依赖分析自动处理，或单独 collector |
| 场景独立 bundle | `PackScene` 或单场景 collector |
| 小写命名 | `FileNameStyle`（KJ 已用 `HashName`，可保留） |
| 超包数字拆分 | YooAsset 自动按大小拆 |

## 9. 地址契约调整（连带）

地址变为「逻辑相对路径 + 扩展名」后，链路契约从纯文件名升级为逻辑路径：

- `FormOptions.AssetName = "Project/DemoForm"`（逻辑相对路径，不带扩展名）。
- `FormManager.LoadForm` 拼 `"Project/DemoForm.prefab"`。
- 多语言：换当前 Package + 同 AssetName，上层业务零改动。

## 10. 待办清单（后续开发）

- [ ] 新建 `Framework.Asset.Editor` 下的 `KJAddressByRelativePath` 自定义地址规则。
- [ ] 用 YooAsset 3.0.3 的 `AssetBundleCollectorSetting` API 写 Editor 脚本，生成 `GameRes` Package + 按目录收集（UI/Scene/Config/Effects）。
- [ ] 确认 `FormOptions.AssetName` 升级为逻辑相对路径的契约，并同步 `ViewDemo.OpenDemoForm`。
- [ ] 决定多语言是否落地「多 Package」方案，若落地则改造 `AssetRuntime`/`IAssetSystem` 支持多包（默认包 + 可选命名包）。
- [ ] 验证 `LoadAssetAsync("DemoForm.prefab")`（或逻辑路径）能命中 DemoForm。
