using Cysharp.Threading.Tasks;
using Framework.View;
namespace Framework.View.Navigation.Editor
{
    public class EditorNavigateContainer : NavigateContainer
    {
        // public override async UniTask<NavigationLoader> OpenForm(NavigationOpenParam param)
        // {
        //     NavigationLoader formLoader = await base.OpenForm(param);
        //     NavigationRecordMgr.Instance.AddRecord(formLoader, NavigationStateType.Open, param);
        //     return formLoader;
        // }
        //
        // public override async UniTask<NavigationLoader> OpenScene(NavigationOpenParam param)
        // {
        //     NavigationLoader sceneLoader = await base.OpenScene(param);
        //     NavigationRecordMgr.Instance.AddRecord(sceneLoader, NavigationStateType.Open, param);
        //     return sceneLoader;
        // }

        // FIXME by fred 所有操作记录到 NavigationRecordMgr

        public override UniTask<TView> OpenViewAsync<TView>(INavigateOptions options)
        {
            NavigationRecordMgr.Instance.AddRecord(this, NavigationStateType.Open, options.Data);
            return base.OpenViewAsync<TView>(options);
        }

        public override async UniTask<bool> Clear()
        {
            NavigationRecordMgr.Instance.AddRecord(this, NavigationStateType.Clear);
            return await base.Clear();
        }

        public override UniTask<bool> LifeCycleExecuteCloseState(Framework.View.LifeCycleArgs args)
        {
            NavigationRecordMgr.Instance.AddRecord(this, NavigationStateType.Close);
            return base.LifeCycleExecuteCloseState(args);
        }
    }
}
