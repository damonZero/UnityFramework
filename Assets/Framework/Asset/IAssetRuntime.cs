using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Framework.Asset
{
    public interface IAssetRuntime : IAssetSystem
    {
        AssetConfig Config { get; }
        bool IsReady { get; }
        void Initialize(AssetConfig config);
        void Shutdown();
    }
}
