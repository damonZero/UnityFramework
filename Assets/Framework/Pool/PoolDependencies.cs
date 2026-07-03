using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Pool
{
    public static class PoolDependencies
    {
        public static Func<string, Transform, UniTask<GameObject>> LoadAssetAsync;
        public static Action<string> ReleaseAssetByPath;
        public static readonly ConcurrentDictionary<string, SemaphoreSlim> LoadGates = new();
    }
}
