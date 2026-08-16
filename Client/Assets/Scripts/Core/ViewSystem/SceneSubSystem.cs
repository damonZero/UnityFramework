using Cysharp.Threading.Tasks;
using Framework.Asset;
using UnityEngine.SceneManagement;

namespace Core.ViewSystem
{
    /// <summary>
    /// ViewSystem 中的场景子系统，继承 Framework.View.SceneManager，
    /// 补上场景资源加载注入点。
    /// </summary>
    public class SceneSubSystem : Framework.View.SceneManager
    {
        private IAssetSystem _assetSystem;

        public void Init(IAssetSystem assetSystem)
        {
            _assetSystem = assetSystem;
            base.Init();
        }

        protected override UniTask LoadUnitySceneAsync(string sceneName, LoadSceneMode mode)
        {
            return LoadSceneAsync(sceneName, mode);
        }

        private async UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode)
        {
            // 注意：AssetSceneHandle 的持有/释放策略留待场景导航完善时处理，
            // 最小骨架只保证场景能被加载。
            await _assetSystem.LoadSceneAsync(sceneName, mode);
        }
    }
}
