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
        // 可注入的诊断日志出口（Pool.asmdef 不引用 Log，运行时错误不能直接 Debug.LogError）；null 时静默丢弃。
        public static Action<string> LogError;

        // soft-restart caveat：LoadGates 是 static readonly，StaticReset 跳过 IsInitOnly 字段，软重启不会清空它。
        // 若重启发生在 prefab 加载在途，残留的 SemaphoreSlim 可能停在 count=0（旧 owner 的 finally 未执行），
        // 导致该 prefabPath 后续加载永久阻塞。当前无重置钩子（Pool 不引用 Framework.Restart），如需根治需重启入口显式 Clear。
        public static readonly ConcurrentDictionary<string, SemaphoreSlim> LoadGates = new();
    }
}
