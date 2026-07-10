using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"MessagePipe.dll",
		"Microsoft.Extensions.Logging.Abstractions.dll",
		"System.Core.dll",
		"System.Runtime.CompilerServices.Unsafe.dll",
		"System.dll",
		"UniTask.dll",
		"UnityEngine.CoreModule.dll",
		"Utf8StringInterpolation.dll",
		"VContainer.dll",
		"YooAsset.dll",
		"ZLinq.dll",
		"ZLogger.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Boot.BootUpdateRunner.<RunAsync>d__8>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Boot.BootUpdateRunner.<UpdateAssetsAsync>d__10>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Core.PoolService.<<Init>b__5_0>d,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Asset.AssetRuntime.<InstantiateAsync>d__34,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Asset.AssetRuntime.<LoadAssetAsync>d__32<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Asset.AssetRuntime.<LoadAssetHandleAsync>d__31<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Asset.AssetRuntime.<StartSceneUnloadAsync>d__48>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Asset.AssetRuntime.<UnloadSceneInternalAsync>d__49>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Asset.AssetSceneHandle.<UnloadAsync>d__18>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Pool.GameObjectPool.<GetAsync>d__9,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Pool.GameObjectPool.<LoadPrefabAsync>d__17,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<Framework.Pool.GameObjectPool.<WarmupInternal>d__16>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Boot.BootUpdateRunner.<RunAsync>d__8>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Boot.BootUpdateRunner.<UpdateAssetsAsync>d__10>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Core.PoolService.<<Init>b__5_0>d,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Asset.AssetRuntime.<InstantiateAsync>d__34,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Asset.AssetRuntime.<LoadAssetAsync>d__32<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Asset.AssetRuntime.<LoadAssetHandleAsync>d__31<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Asset.AssetRuntime.<StartSceneUnloadAsync>d__48>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Asset.AssetRuntime.<UnloadSceneInternalAsync>d__49>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Asset.AssetSceneHandle.<UnloadAsync>d__18>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Pool.GameObjectPool.<GetAsync>d__9,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Pool.GameObjectPool.<LoadPrefabAsync>d__17,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<Framework.Pool.GameObjectPool.<WarmupInternal>d__16>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<Framework.Asset.AssetRuntime.<LoadAssetInternalAsync>d__33<object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<Framework.Asset.AssetRuntime.<LoadAssetInternalAsync>d__33<object>>
	// Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise<object>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.IUniTaskSource<object>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<object>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<object>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<object>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<Cysharp.Threading.Tasks.AsyncUnit>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<object>
	// MessagePipe.IPublisher<Core.Asset.AssetSystemReadyEvent>
	// MessagePipe.IPublisher<Core.Systems.Events.AppShuttingDownEvent>
	// MessagePipe.IPublisher<Core.Systems.Events.AppStartedEvent>
	// Microsoft.Extensions.Logging.Logger<object>
	// System.Action<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Action<System.ValueTuple<object,object>>
	// System.Action<float>
	// System.Action<object,object>
	// System.Action<object>
	// System.ArraySegment.Enumerator<byte>
	// System.ArraySegment.Enumerator<int>
	// System.ArraySegment.Enumerator<object>
	// System.ArraySegment.Enumerator<ushort>
	// System.ArraySegment<byte>
	// System.ArraySegment<int>
	// System.ArraySegment<object>
	// System.ArraySegment<ushort>
	// System.Buffers.ArrayPool<ZLinq.Internal.HashSetSlim.Entry<object>>
	// System.Buffers.ArrayPool<byte>
	// System.Buffers.ArrayPool<int>
	// System.Buffers.ArrayPool<object>
	// System.Buffers.IBufferWriter<byte>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.LockedStack<ZLinq.Internal.HashSetSlim.Entry<object>>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.LockedStack<byte>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.LockedStack<int>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.LockedStack<object>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.PerCoreLockedStacks<ZLinq.Internal.HashSetSlim.Entry<object>>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.PerCoreLockedStacks<byte>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.PerCoreLockedStacks<int>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool.PerCoreLockedStacks<object>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool<ZLinq.Internal.HashSetSlim.Entry<object>>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool<byte>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool<int>
	// System.Buffers.TlsOverPerCoreLockedStacksArrayPool<object>
	// System.ByReference<byte>
	// System.ByReference<int>
	// System.ByReference<object>
	// System.ByReference<ushort>
	// System.Collections.Concurrent.ConcurrentDictionary.<GetEnumerator>d__35<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.DictionaryEnumerator<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Node<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Tables<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary<object,object>
	// System.Collections.Generic.ArraySortHelper<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.ArraySortHelper<System.ValueTuple<object,object>>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.Comparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<Framework.Asset.AssetRuntime.AssetCacheKey,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,long>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<Framework.Asset.AssetRuntime.AssetCacheKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,long>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<Framework.Asset.AssetRuntime.AssetCacheKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,long>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<Framework.Asset.AssetRuntime.AssetCacheKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,long>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<Framework.Asset.AssetRuntime.AssetCacheKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,long>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<Framework.Asset.AssetRuntime.AssetCacheKey,object>
	// System.Collections.Generic.Dictionary<object,Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,long>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.EqualityComparer<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<Framework.Asset.AssetRuntime.AssetCacheKey,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,Cysharp.Threading.Tasks.UniTask>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,long>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.ValueTuple<object,object>>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.IComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.IEnumerable<Framework.Log.GameLogModuleRule>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Framework.Asset.AssetRuntime.AssetCacheKey,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,Cysharp.Threading.Tasks.UniTask>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,long>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.ValueTuple<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.IEnumerator<Framework.Log.GameLogModuleRule>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<Framework.Asset.AssetRuntime.AssetCacheKey,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,Cysharp.Threading.Tasks.UniTask>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,long>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.ValueTuple<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.IList<System.ValueTuple<object,object>>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IReadOnlyCollection<object>
	// System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IReadOnlyList<object>
	// System.Collections.Generic.KeyValuePair<Framework.Asset.AssetRuntime.AssetCacheKey,object>
	// System.Collections.Generic.KeyValuePair<object,Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,long>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.LinkedList.Enumerator<object>
	// System.Collections.Generic.LinkedList<object>
	// System.Collections.Generic.LinkedListNode<object>
	// System.Collections.Generic.List.Enumerator<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.List.Enumerator<System.ValueTuple<object,object>>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.List<System.ValueTuple<object,object>>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<Cysharp.Threading.Tasks.UniTask>
	// System.Collections.Generic.ObjectEqualityComparer<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Queue.Enumerator<Framework.Log.GameLogEntry>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<Framework.Log.GameLogEntry>
	// System.Collections.Generic.Queue<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.ValueTuple<object,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Comparison<System.ValueTuple<object,object>>
	// System.Comparison<object>
	// System.Func<Core.Asset.AssetSystemLog.ConfigNotFoundState,object,object>
	// System.Func<Core.Asset.AssetSystemLog.InitializeFailedState,object,object>
	// System.Func<Core.Asset.AssetSystemLog.ReadyState,object,object>
	// System.Func<Core.Asset.AssetSystemLog.ShutdownState,object,object>
	// System.Func<Core.Systems.StartupProbeSystemLog.ProbeInitState,object,object>
	// System.Func<Core.Systems.StartupProbeSystemLog.ProbeShutdownState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.AlreadyInitializedSkipState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.AlreadyInitializedState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.InitCompleteState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.InitCompleteWithFailuresState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.InitFailedState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.InitProgressState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.InitStartState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.ShutdownCompleteState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.ShutdownFailedState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.ShutdownProgressState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.ShutdownStartState,object,object>
	// System.Func<Core.Systems.SystemManagerLog.SystemAlreadyRegisteredState,object,object>
	// System.Func<General.ModelLifecycleLog.CoreStartupFailedState,object,object>
	// System.Func<General.ModelLifecycleLog.ModelLoadFailedState,object,object>
	// System.Func<General.ModelLifecycleLog.ModelLoadedState,object,object>
	// System.Func<General.ModelLifecycleLog.ModelUnloadFailedState,object,object>
	// System.Func<General.ModelLifecycleLog.ModelUnloadedState,object,object>
	// System.Func<System.DateTimeOffset>
	// System.Func<System.Nullable<int>>
	// System.Func<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Func<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Func<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Func<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Func<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Func<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Func<System.ValueTuple<byte,object>>
	// System.Func<int>
	// System.Func<long>
	// System.Func<object,Cysharp.Threading.Tasks.UniTask>
	// System.Func<object,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Func<object,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Func<object,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Func<object,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Func<object,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Func<object,System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Func<object,System.ValueTuple<byte,object>>
	// System.Func<object,byte>
	// System.Func<object,int,byte>
	// System.Func<object,object,Cysharp.Threading.Tasks.UniTask<object>>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Lazy<object>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,object>
	// System.Linq.Enumerable.WhereSelectListIterator<object,object>
	// System.Nullable<System.DateTime>
	// System.Nullable<System.DateTimeOffset>
	// System.Nullable<System.Decimal>
	// System.Nullable<System.Guid>
	// System.Nullable<System.TimeSpan>
	// System.Nullable<UnityEngine.Vector3>
	// System.Nullable<byte>
	// System.Nullable<double>
	// System.Nullable<float>
	// System.Nullable<int>
	// System.Nullable<long>
	// System.Nullable<sbyte>
	// System.Nullable<short>
	// System.Nullable<uint>
	// System.Nullable<ulong>
	// System.Nullable<ushort>
	// System.Predicate<Framework.Asset.AssetRuntime.AssetCacheKey>
	// System.Predicate<System.ValueTuple<object,object>>
	// System.Predicate<object>
	// System.ReadOnlySpan.Enumerator<byte>
	// System.ReadOnlySpan.Enumerator<int>
	// System.ReadOnlySpan.Enumerator<object>
	// System.ReadOnlySpan.Enumerator<ushort>
	// System.ReadOnlySpan<byte>
	// System.ReadOnlySpan<int>
	// System.ReadOnlySpan<object>
	// System.ReadOnlySpan<ushort>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.ValueTuple<byte,object>>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.ValueTuple<byte,object>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.ValueTuple<byte,object>>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<object>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<System.ValueTuple<byte,object>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter<object>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<System.ValueTuple<byte,object>>
	// System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<object>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Runtime.CompilerServices.TaskAwaiter<System.ValueTuple<byte,object>>
	// System.Runtime.CompilerServices.TaskAwaiter<object>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<System.ValueTuple<byte,object>>
	// System.Runtime.CompilerServices.ValueTaskAwaiter<object>
	// System.Span<byte>
	// System.Span<int>
	// System.Span<object>
	// System.Span<ushort>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.ValueTuple<byte,object>>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<object>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Threading.Tasks.Sources.IValueTaskSource<System.ValueTuple<byte,object>>
	// System.Threading.Tasks.Sources.IValueTaskSource<object>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Threading.Tasks.Task<System.ValueTuple<byte,object>>
	// System.Threading.Tasks.Task<object>
	// System.Threading.Tasks.TaskCompletionSource<object>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Threading.Tasks.TaskFactory<System.ValueTuple<byte,object>>
	// System.Threading.Tasks.TaskFactory<object>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<System.ValueTuple<byte,object>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c<object>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<System.ValueTuple<byte,object>>
	// System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask<object>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Threading.Tasks.ValueTask<System.ValueTuple<byte,object>>
	// System.Threading.Tasks.ValueTask<object>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,object>>
	// System.ValueTuple<byte,object>
	// System.ValueTuple<object,object>
	// Utf8StringInterpolation.Utf8StringWriter<object>
	// ZLinq.IValueEnumerator<object>
	// ZLinq.Internal.HashSetSlim<object>
	// ZLinq.Internal.InlineArray16<object>
	// ZLinq.Internal.InlineArray27<object>
	// ZLinq.Internal.SegmentedArrayProvider<object>
	// ZLinq.Linq.ArrayWhere<object>
	// ZLinq.Linq.Distinct<ZLinq.Linq.ArrayWhere<object>,object>
	// ZLinq.Linq.FromArray<object>
	// ZLinq.ValueEnumerable<ZLinq.Linq.ArrayWhere<object>,object>
	// ZLinq.ValueEnumerable<ZLinq.Linq.Distinct<ZLinq.Linq.ArrayWhere<object>,object>,object>
	// ZLinq.ValueEnumerable<ZLinq.Linq.FromArray<object>,object>
	// ZLinq.ValueEnumerator<ZLinq.Linq.ArrayWhere<object>,object>
	// ZLogger.Internal.IObjectPoolNode<object>
	// ZLogger.ZLoggerEntry<Core.Asset.AssetSystemLog.ConfigNotFoundState>
	// ZLogger.ZLoggerEntry<Core.Asset.AssetSystemLog.InitializeFailedState>
	// ZLogger.ZLoggerEntry<Core.Asset.AssetSystemLog.ReadyState>
	// ZLogger.ZLoggerEntry<Core.Asset.AssetSystemLog.ShutdownState>
	// ZLogger.ZLoggerEntry<Core.Systems.StartupProbeSystemLog.ProbeInitState>
	// ZLogger.ZLoggerEntry<Core.Systems.StartupProbeSystemLog.ProbeShutdownState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.AlreadyInitializedSkipState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.AlreadyInitializedState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.InitCompleteState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.InitCompleteWithFailuresState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.InitFailedState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.InitProgressState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.InitStartState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.ShutdownCompleteState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.ShutdownFailedState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.ShutdownProgressState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.ShutdownStartState>
	// ZLogger.ZLoggerEntry<Core.Systems.SystemManagerLog.SystemAlreadyRegisteredState>
	// ZLogger.ZLoggerEntry<General.ModelLifecycleLog.CoreStartupFailedState>
	// ZLogger.ZLoggerEntry<General.ModelLifecycleLog.ModelLoadFailedState>
	// ZLogger.ZLoggerEntry<General.ModelLifecycleLog.ModelLoadedState>
	// ZLogger.ZLoggerEntry<General.ModelLifecycleLog.ModelUnloadFailedState>
	// ZLogger.ZLoggerEntry<General.ModelLifecycleLog.ModelUnloadedState>
	// }}

	public void RefMethods()
	{
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Boot.BootUpdateRunner.<RunAsync>d__8>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Boot.BootUpdateRunner.<RunAsync>d__8&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Framework.Asset.AssetRuntime.<StartSceneUnloadAsync>d__48>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Framework.Asset.AssetRuntime.<StartSceneUnloadAsync>d__48&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Framework.Asset.AssetRuntime.<UnloadSceneInternalAsync>d__49>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Framework.Asset.AssetRuntime.<UnloadSceneInternalAsync>d__49&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Framework.Asset.AssetSceneHandle.<UnloadAsync>d__18>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Framework.Asset.AssetSceneHandle.<UnloadAsync>d__18&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Framework.Pool.GameObjectPool.<WarmupInternal>d__16>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Framework.Pool.GameObjectPool.<WarmupInternal>d__16&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,Boot.BootUpdateRunner.<UpdateAssetsAsync>d__10>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,Boot.BootUpdateRunner.<UpdateAssetsAsync>d__10&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,Framework.Pool.GameObjectPool.<GetAsync>d__9>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,Framework.Pool.GameObjectPool.<GetAsync>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Framework.Asset.AssetRuntime.<LoadAssetHandleAsync>d__31<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Framework.Asset.AssetRuntime.<LoadAssetHandleAsync>d__31<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Core.PoolService.<<Init>b__5_0>d>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Core.PoolService.<<Init>b__5_0>d&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Framework.Asset.AssetRuntime.<InstantiateAsync>d__34>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Framework.Asset.AssetRuntime.<InstantiateAsync>d__34&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Framework.Pool.GameObjectPool.<GetAsync>d__9>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Framework.Pool.GameObjectPool.<GetAsync>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,Framework.Pool.GameObjectPool.<LoadPrefabAsync>d__17>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,Framework.Pool.GameObjectPool.<LoadPrefabAsync>d__17&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Framework.Pool.GameObjectPool.<GetAsync>d__9>(System.Runtime.CompilerServices.TaskAwaiter&,Framework.Pool.GameObjectPool.<GetAsync>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<object>,Framework.Asset.AssetRuntime.<LoadAssetAsync>d__32<object>>(System.Runtime.CompilerServices.TaskAwaiter<object>&,Framework.Asset.AssetRuntime.<LoadAssetAsync>d__32<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Boot.BootUpdateRunner.<RunAsync>d__8>(Boot.BootUpdateRunner.<RunAsync>d__8&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Boot.BootUpdateRunner.<UpdateAssetsAsync>d__10>(Boot.BootUpdateRunner.<UpdateAssetsAsync>d__10&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Framework.Asset.AssetRuntime.<StartSceneUnloadAsync>d__48>(Framework.Asset.AssetRuntime.<StartSceneUnloadAsync>d__48&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Framework.Asset.AssetRuntime.<UnloadSceneInternalAsync>d__49>(Framework.Asset.AssetRuntime.<UnloadSceneInternalAsync>d__49&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Framework.Asset.AssetSceneHandle.<UnloadAsync>d__18>(Framework.Asset.AssetSceneHandle.<UnloadAsync>d__18&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<Framework.Pool.GameObjectPool.<WarmupInternal>d__16>(Framework.Pool.GameObjectPool.<WarmupInternal>d__16&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Core.PoolService.<<Init>b__5_0>d>(Core.PoolService.<<Init>b__5_0>d&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Framework.Asset.AssetRuntime.<InstantiateAsync>d__34>(Framework.Asset.AssetRuntime.<InstantiateAsync>d__34&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Framework.Asset.AssetRuntime.<LoadAssetAsync>d__32<object>>(Framework.Asset.AssetRuntime.<LoadAssetAsync>d__32<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Framework.Asset.AssetRuntime.<LoadAssetHandleAsync>d__31<object>>(Framework.Asset.AssetRuntime.<LoadAssetHandleAsync>d__31<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35>(Framework.Asset.AssetRuntime.<LoadSceneAsync>d__35&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Framework.Pool.GameObjectPool.<GetAsync>d__9>(Framework.Pool.GameObjectPool.<GetAsync>d__9&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<Framework.Pool.GameObjectPool.<LoadPrefabAsync>d__17>(Framework.Pool.GameObjectPool.<LoadPrefabAsync>d__17&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,Framework.Asset.AssetRuntime.<LoadAssetInternalAsync>d__33<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter&,Framework.Asset.AssetRuntime.<LoadAssetInternalAsync>d__33<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<Framework.Asset.AssetRuntime.<LoadAssetInternalAsync>d__33<object>>(Framework.Asset.AssetRuntime.<LoadAssetInternalAsync>d__33<object>&)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Asset.AssetSystemLog.ConfigNotFoundState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Asset.AssetSystemLog.ConfigNotFoundState,System.Exception,System.Func<Core.Asset.AssetSystemLog.ConfigNotFoundState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Asset.AssetSystemLog.InitializeFailedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Asset.AssetSystemLog.InitializeFailedState,System.Exception,System.Func<Core.Asset.AssetSystemLog.InitializeFailedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Asset.AssetSystemLog.ReadyState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Asset.AssetSystemLog.ReadyState,System.Exception,System.Func<Core.Asset.AssetSystemLog.ReadyState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Asset.AssetSystemLog.ShutdownState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Asset.AssetSystemLog.ShutdownState,System.Exception,System.Func<Core.Asset.AssetSystemLog.ShutdownState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.StartupProbeSystemLog.ProbeInitState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.StartupProbeSystemLog.ProbeInitState,System.Exception,System.Func<Core.Systems.StartupProbeSystemLog.ProbeInitState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.StartupProbeSystemLog.ProbeShutdownState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.StartupProbeSystemLog.ProbeShutdownState,System.Exception,System.Func<Core.Systems.StartupProbeSystemLog.ProbeShutdownState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.AlreadyInitializedSkipState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.AlreadyInitializedSkipState,System.Exception,System.Func<Core.Systems.SystemManagerLog.AlreadyInitializedSkipState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.AlreadyInitializedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.AlreadyInitializedState,System.Exception,System.Func<Core.Systems.SystemManagerLog.AlreadyInitializedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.InitCompleteState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.InitCompleteState,System.Exception,System.Func<Core.Systems.SystemManagerLog.InitCompleteState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.InitCompleteWithFailuresState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.InitCompleteWithFailuresState,System.Exception,System.Func<Core.Systems.SystemManagerLog.InitCompleteWithFailuresState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.InitFailedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.InitFailedState,System.Exception,System.Func<Core.Systems.SystemManagerLog.InitFailedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.InitProgressState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.InitProgressState,System.Exception,System.Func<Core.Systems.SystemManagerLog.InitProgressState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.InitStartState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.InitStartState,System.Exception,System.Func<Core.Systems.SystemManagerLog.InitStartState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.ShutdownCompleteState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.ShutdownCompleteState,System.Exception,System.Func<Core.Systems.SystemManagerLog.ShutdownCompleteState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.ShutdownFailedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.ShutdownFailedState,System.Exception,System.Func<Core.Systems.SystemManagerLog.ShutdownFailedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.ShutdownProgressState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.ShutdownProgressState,System.Exception,System.Func<Core.Systems.SystemManagerLog.ShutdownProgressState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.ShutdownStartState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.ShutdownStartState,System.Exception,System.Func<Core.Systems.SystemManagerLog.ShutdownStartState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<Core.Systems.SystemManagerLog.SystemAlreadyRegisteredState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,Core.Systems.SystemManagerLog.SystemAlreadyRegisteredState,System.Exception,System.Func<Core.Systems.SystemManagerLog.SystemAlreadyRegisteredState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<General.ModelLifecycleLog.CoreStartupFailedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,General.ModelLifecycleLog.CoreStartupFailedState,System.Exception,System.Func<General.ModelLifecycleLog.CoreStartupFailedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<General.ModelLifecycleLog.ModelLoadFailedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,General.ModelLifecycleLog.ModelLoadFailedState,System.Exception,System.Func<General.ModelLifecycleLog.ModelLoadFailedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<General.ModelLifecycleLog.ModelLoadedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,General.ModelLifecycleLog.ModelLoadedState,System.Exception,System.Func<General.ModelLifecycleLog.ModelLoadedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<General.ModelLifecycleLog.ModelUnloadFailedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,General.ModelLifecycleLog.ModelUnloadFailedState,System.Exception,System.Func<General.ModelLifecycleLog.ModelUnloadFailedState,System.Exception,string>)
		// System.Void Microsoft.Extensions.Logging.ILogger.Log<General.ModelLifecycleLog.ModelUnloadedState>(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,General.ModelLifecycleLog.ModelUnloadedState,System.Exception,System.Func<General.ModelLifecycleLog.ModelUnloadedState,System.Exception,string>)
		// Microsoft.Extensions.Logging.ILogger<object> Microsoft.Extensions.Logging.LoggerFactoryExtensions.CreateLogger<object>(Microsoft.Extensions.Logging.ILoggerFactory)
		// object System.Activator.CreateInstance<object>()
		// byte[] System.Array.Empty<byte>()
		// object[] System.Array.Empty<object>()
		// System.Void System.Buffers.BuffersExtensions.Write<byte>(System.Buffers.IBufferWriter<byte>,System.ReadOnlySpan<byte>)
		// System.Void System.Buffers.BuffersExtensions.WriteMultiSegment<byte>(System.Buffers.IBufferWriter<byte>,System.ReadOnlySpan<byte>&,System.Span<byte>)
		// int System.HashCode.Combine<object,object>(object,object)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<object,object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<object>.Select<object>(System.Func<object,object>)
		// System.Span<object> System.MemoryExtensions.AsSpan<object>(object[])
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// object& System.Runtime.CompilerServices.Unsafe.As<int,object>(int&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// int& System.Runtime.CompilerServices.Unsafe.AsRef<int>(int&)
		// object& System.Runtime.CompilerServices.Unsafe.AsRef<object>(object&)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.Object.Instantiate<object>(object)
		// object UnityEngine.Resources.Load<object>(string)
		// VContainer.RegistrationBuilder VContainer.ContainerBuilderExtensions.Register<object>(VContainer.IContainerBuilder,VContainer.Lifetime)
		// VContainer.RegistrationBuilder VContainer.ContainerBuilderExtensions.RegisterInstance<object>(VContainer.IContainerBuilder,object)
		// object VContainer.IContainerBuilder.Register<object>(object)
		// VContainer.RegistrationBuilder VContainer.RegistrationBuilder.As<object>()
		// VContainer.RegistrationBuilder VContainer.Unity.ContainerBuilderUnityExtensions.RegisterEntryPoint<object>(VContainer.IContainerBuilder,VContainer.Lifetime)
		// object YooAsset.AssetHandle.GetAssetObject<object>()
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetAsync<object>(string,uint)
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetSync<object>(string)
		// object[] ZLinq.GC.AllocateUninitializedArray<object>(int)
		// object ZLinq.Internal.Throws.NoMatch<object>()
		// ZLinq.ValueEnumerable<ZLinq.Linq.FromArray<object>,object> ZLinq.ValueEnumerable.AsValueEnumerable<object>(object[])
		// ZLinq.ValueEnumerable<ZLinq.Linq.Distinct<ZLinq.Linq.ArrayWhere<object>,object>,object> ZLinq.ValueEnumerableExtensions.Distinct<ZLinq.Linq.ArrayWhere<object>,object>(ZLinq.ValueEnumerable<ZLinq.Linq.ArrayWhere<object>,object>)
		// object ZLinq.ValueEnumerableExtensions.First<ZLinq.Linq.FromArray<object>,object>(ZLinq.ValueEnumerable<ZLinq.Linq.FromArray<object>,object>,System.Func<object,bool>)
		// ZLinq.ValueEnumerator<ZLinq.Linq.ArrayWhere<object>,object> ZLinq.ValueEnumerableExtensions.GetEnumerator<ZLinq.Linq.ArrayWhere<object>,object>(ZLinq.ValueEnumerable<ZLinq.Linq.ArrayWhere<object>,object>&)
		// object[] ZLinq.ValueEnumerableExtensions.ToArray<ZLinq.Linq.Distinct<ZLinq.Linq.ArrayWhere<object>,object>,object>(ZLinq.ValueEnumerable<ZLinq.Linq.Distinct<ZLinq.Linq.ArrayWhere<object>,object>,object>)
		// object[] ZLinq.ValueEnumerableExtensions.ToArray<object>(ZLinq.ValueEnumerable<ZLinq.Linq.ArrayWhere<object>,object>)
		// bool ZLinq.ValueEnumerableExtensions.TryGetFirst<ZLinq.Linq.FromArray<object>,object>(ZLinq.Linq.FromArray<object>&,System.Func<object,bool>,object&)
		// ZLinq.ValueEnumerable<ZLinq.Linq.ArrayWhere<object>,object> ZLinq.ValueEnumerableExtensions.Where<object>(ZLinq.ValueEnumerable<ZLinq.Linq.FromArray<object>,object>,System.Func<object,bool>)
	}
}