using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation.Editor
{
    public class EditorNavigationSceneLoader : NavigationSceneLoader
    {
        public override UniTask<bool> LifeCycleExecuteCloseState(Framework.View.LifeCycleArgs args)
        {
            NavigationRecordMgr.Instance.AddRecord(this, NavigationStateType.Close);
            return base.LifeCycleExecuteCloseState(args);
        }

        public override async UniTask<bool> Clear()
        {
            NavigationRecordMgr.Instance.AddRecord(this, NavigationStateType.Clear);
            return await base.Clear();
        }
    }
}
