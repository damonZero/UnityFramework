using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

using Framework.Log;

namespace Framework.ViewCache
{
    public class GameObjectResContainer : AbstractResContainer<GameObject>
    {
        public static GameObjectResContainer Create(Transform root)
        {
            return new GameObjectResContainer(root);
        }

        private GameObjectResContainer(Transform rootParent) : base(rootParent)
        {
        }

        protected override Transform GetTransform(GameObject instance)
        {
            return instance.transform;
        }

        public override async UniTask<GameObject> InstanceAsync(string assetName)
        {
            GameLog.Debug($"缓存Miss：{assetName}", module: CacheFactory.CACHER_LOG_DEBUG);
            return await CacheDependencies.InstantiateGameObject(assetName, _cacheRoot);
        }

        protected override GameObject Instance(string assetName)
        {
            return null;
        }

        public override void DestroyObj(GameObject go)
        {
            if (go == null) return;
            Object.Destroy(go);
        }

        // public void Destroy(string assetName)
        // {
        //     if (_dictionary.TryGetValue(assetName, out var list))
        //     {
        //         var len = list.Count;
        //         if (len > 0)
        //         {
        //             var go = list[len - 1];
        //             DestroyObj(go);
        //             list.RemoveAt(len - 1);
        //             return;
        //         }
        //     }
        //
        //     Debug.LogError($"GameObject Destroy error [{assetName}]");
        // }
        //
        // public void Clear()
        // {
        //     _dictionary.Clear();
        //     _cacheRoot.DestroyChildren();
        // }
        //
        // public override string ToString()
        // {
        //     var str = "";
        //     _dictionary.Keys.ForEach(name =>
        //     {
        //         var count = _dictionary[name].Count;
        //         if (count > 0)
        //             str += $"{name}={count};\n";
        //     });
        //     return str;
        // }
    }
}
