using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Framework.Asset
{
    public interface IAssetSystem
    {
        UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path) where T : Object;
        UniTask<T> LoadAssetAsync<T>(string path) where T : Object;
        UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent = null);
        UniTask<AssetSceneHandle> LoadSceneAsync(
            string path,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null);
        AssetDownloadHandle CreateDownloader(string tag = null);
        AssetDownloadHandle CreateDownloader(string[] tags);
        void Release<T>(string path) where T : Object;
        void Release(string path);
        void UnloadUnused();
    }
}
