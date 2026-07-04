using System;
using Core.Systems;
using Core.Systems.Attributes;
using Framework.Asset;
using Framework.Pool;
using UnityEngine;

namespace Core
{
    [CoreSystem]
    public sealed class PoolService : ISystem
    {
        private readonly IAssetSystem _assetSystem;
        private GameObjectPool _gameObjectPool;

        public int Priority => AssetConstants.SystemPriority + 10;

        public PoolService(IAssetSystem assetSystem)
        {
            _assetSystem = assetSystem ?? throw new ArgumentNullException(nameof(assetSystem));
        }

        public void Init()
        {
            PoolDependencies.LoadAssetAsync = async (path, parent) => await _assetSystem.LoadAssetAsync<GameObject>(path);
            PoolDependencies.ReleaseAssetByPath = path => _assetSystem.Release<GameObject>(path);
            _gameObjectPool = new GameObjectPool(null, 64);
        }

        public void Shutdown()
        {
            _gameObjectPool?.Clear();
            _gameObjectPool = null;
            PoolDependencies.LoadAssetAsync = null;
            PoolDependencies.ReleaseAssetByPath = null;
        }

        public GameObjectPool GameObjectPool => _gameObjectPool;

        public static PooledList<T> RentList<T>() => CollectionPool.RentList<T>();
        public static PooledHashSet<T> RentHashSet<T>() => CollectionPool.RentHashSet<T>();
        public static PooledQueue<T> RentQueue<T>() => CollectionPool.RentQueue<T>();
        public static PooledStack<T> RentStack<T>() => CollectionPool.RentStack<T>();
        public static PooledDictionary<TKey, TValue> RentDictionary<TKey, TValue>() where TKey : notnull => CollectionPool.RentDictionary<TKey, TValue>();
    }
}
