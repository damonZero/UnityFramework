using Framework.Log;
using Framework.Touch;
using Framework.MVVM;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Demo
{
    /// <summary>
    /// 演示界面：展示 KJ View 框架最简用法。
    ///
    /// 使用方式（对应参考项目 CSharpForm 的打开流程）：
    ///   1. 预制体挂载本脚本 + Canvas（BaseForm 已 [RequireComponent(typeof(Canvas))]）。
    ///   2. 子节点按命名约定命名：_txtTitle（Text）、_btnClose（Button）。
    ///   3. 选中预制体 → Inspector「🔗 自动绑定变量」→ 生成 .Binding.cs。
    ///   4. 运行时：Core.ViewSystem.ViewSystem.FormSubSystem.Open(new FormOptions
    ///      { AssetName = nameof(DemoForm), Layer = 1, Data = "hello" })。
    /// </summary>
    public partial class DemoForm : MvvmForm
    {
        protected override void OnFormAwake()
        {
            _btnClose.Click = (_, _) => GameLog.Debug("Close button clicked", module: nameof(DemoForm));
        }

        protected override void OnOpen(object data)
        {
            GameLog.Info($"DemoForm opened, data: {data}", module: nameof(DemoForm));
        }
    }
}