using Core.ViewSystem;
using Cysharp.Threading.Tasks;
using Framework.Log;
using Framework.View;

namespace Project.Demo
{
    /// <summary>
    /// 演示入口：打开 DemoForm 界面。
    /// 使用前需满足：ViewSystem 已初始化（Core 启动完成）、DemoForm.prefab 已加入资源（YooAsset）集合。
    /// 可在任意业务代码 / GM 命令 / 测试中调用 ViewDemo.OpenDemoForm()。
    /// </summary>
    public static class ViewDemo
    {
        public static async UniTask<BaseForm> OpenDemoForm()
        {
            var formSystem = ViewSystem.FormSubSystem;
            if (formSystem == null)
            {
                GameLog.Error("ViewSystem 未初始化，无法打开 DemoForm", module: nameof(ViewDemo));
                return null;
            }

            return await formSystem.Open(new FormOptions
            {
                AssetName = nameof(DemoForm),
                Layer = 1,
                Data = "hello-demo"
            });
        }
    }
}
