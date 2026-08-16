using System.Threading;
using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation.Editor
{
    public class EditorNavigationFormLoader : NavigationFormLoader
    {
        public override UniTask<bool> Close(CancellationToken cancellationToken = default)
        {
            NavigationRecordMgr.Instance.AddRecord(this, NavigationStateType.Close);
            return base.Close(cancellationToken);
        }

        public override async UniTask<bool> Clear()
        {
            NavigationRecordMgr.Instance.AddRecord(this, NavigationStateType.Clear);
            return await base.Clear();
        }
    }
}
