using System.IO;
using YooAsset.Editor;

namespace Framework.Asset.Editor
{
    /// <summary>
    /// 地址 = 文件名（带扩展名）。例如 Assets/GameRes/UI/Project/DemoForm.prefab → DemoForm.prefab。
    /// 对应代码里 IAssetSystem.LoadAssetAsync 传的「文件名.扩展名」契约（见 .planning/YOOASSET_RESOURCE_COLLECTION.md）。
    /// 临时验证用；正式多语言方案见同文档第 5 节的 KJAddressByRelativePath。
    /// </summary>
    [DisplayName("定位地址: 文件名(带扩展名)")]
    public class AddressByFileNameAndExt : IAddressRule
    {
        public string GetAssetAddress(AddressRuleData data)
        {
            return Path.GetFileName(data.AssetPath);
        }
    }
}
