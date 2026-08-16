using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Framework.Asset;
using Framework.Log;
using Framework.Restart;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Boot.GameLife
{
    /// <summary>
    /// 游戏软/硬重启协调器（镜像参考项目 Boot.GameLife.GameRestart）。
    ///
    /// 软重启（进程内）：保留 AOT Entry 与 YooAsset 资产运行时，按正确顺序拆除游戏层（Core/General/Project）
    /// 并重建，见 <see cref="SoftRestartAsync"/>。
    /// 硬重启（进程外）：按平台重启应用进程，见 <see cref="HardRestart"/>。
    ///
    /// 位于 Boot 层（KJ.Boot）：Boot 不在 <see cref="ResetStaticFields"/> 的扫描范围内，故本类自身的
    /// <see cref="beforeRestart"/>/<see cref="afterRestart"/>/<see cref="bootComponent"/> 等静态不会被重置。
    /// Boot 不编译期引用 Core，对 Core 的拆除/重建经程序集限定名反射（同 <c>Entry.Repair</c> 契约）。
    /// </summary>
    public static class GameRestart
    {
        /// <summary>Core 反射入口契约（Boot 不编译期引用 Core）。</summary>
        private const string CoreStartupTypeName = "Core.Bootstrap.CoreStartup, Core";

        private static GameObject _bg;

        /// <summary>重启前钩子（业务层挂载，如关闭启动表现）。</summary>
        public static Action beforeRestart;

        /// <summary>重启后钩子（业务层挂载，如重新打开登录界面）。</summary>
        public static Action afterRestart;

        /// <summary>持久根 GameObject（AOT Entry），软重启时保留。</summary>
        public static GameObject bootComponent;

        /// <summary>软重启保留的资产运行时（BootUpdateRunner 创建后注入，跨 Core scope 重建复用）。</summary>
        public static IAssetRuntime AssetRuntime { get; set; }

        /// <summary>异步软重启（进程内）。</summary>
        public static void SoftRestart()
        {
            SoftRestartAsync().Forget();
        }

        /// <summary>
        /// 软重启主流程。销毁顺序与参考项目相反（修复其 bug）：
        /// 先销毁非持久根 GameObject（此时事件/DI/池/计时器仍存活，OnDisable/OnDestroy 可安全退订/归还/释放），
        /// 再重置静态、释放 Core scope、重新进入 CoreStartup。
        /// </summary>
        public static async UniTaskVoid SoftRestartAsync()
        {
            try { beforeRestart?.Invoke(); }
            catch (Exception e) { GameLog.Exception(e, "[GameRestart] beforeRestart failed", "Boot.GameRestart"); }

            // ① 销毁所有非持久根 GameObject（先删 prefab：OnDisable/OnDestroy 此刻系统仍存活）。
            DestroyNonPersistentRoots();

            // ② 释放 Core scope（同步级联 General/Project；AssetSystem 走软释放不拆 YooAsset）。
            //    必须先于静态重置：CoreStartup.Reset 同步 Dispose 后旧系统停止 Tick，避免读空静态 NRE。
            ResetCoreScope();

            // ③ 重置静态变量（约定 + 强制检查，见 Framework.Restart.StaticReset）。
            StaticReset.ResetAll();

            // ④ GC。
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect();

            await UniTask.Yield();

            // ⑤ 重新进入 CoreStartup.Start(保留的资产运行时)。
            StartCore(AssetRuntime);

            await UniTask.Yield();

            try { afterRestart?.Invoke(); }
            catch (Exception e) { GameLog.Exception(e, "[GameRestart] afterRestart failed", "Boot.GameRestart"); }

            // ⑥ 销毁过渡背景（当前 KJ 无 ReStartBg.prefab，占位）。
            Release();
        }

        /// <summary>
        /// 销毁所有非持久根 GameObject。跳过：bootComponent（AOT Entry）、过渡背景、
        /// 非根对象（子节点随父销毁）、以及 DontDestroyOnLoad 持久层（Entry / CoreLifetimeScope，
        /// 其销毁分别由进程结束 / ResetCoreScope 负责）。
        /// </summary>
        private static void DestroyNonPersistentRoots()
        {
            var objs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objs)
            {
                if (obj == null) continue;
                if (bootComponent != null && ReferenceEquals(obj, bootComponent)) continue;
                if (_bg != null && ReferenceEquals(obj, _bg)) continue;
                if (obj.transform.parent != null) continue; // 只处理根对象
                if (bootComponent != null && obj.scene == bootComponent.scene) continue; // 跳过 DontDestroyOnLoad 持久层

                obj.SetActive(false);   // OnDisable 同步触发（系统仍存活）
                Object.Destroy(obj);    // OnDestroy 帧末触发
            }
        }

        /// <summary>反射调用 Core.Bootstrap.CoreStartup.Reset() 释放 Core scope（级联 General/Project）。</summary>
        private static void ResetCoreScope()
        {
            try
            {
                var type = Type.GetType(CoreStartupTypeName, throwOnError: false);
                var method = type?.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                    method.Invoke(null, Array.Empty<object>());
                else
                    GameLog.Error("[GameRestart] CoreStartup.Reset not found", "Boot.GameRestart");
            }
            catch (Exception e)
            {
                var cause = e is TargetInvocationException tie ? tie.InnerException ?? tie : e;
                GameLog.Exception(cause, "[GameRestart] CoreStartup.Reset failed", "Boot.GameRestart");
            }
        }

        /// <summary>反射调用 Core.Bootstrap.CoreStartup.Start(IAssetRuntime) 重建启动链。</summary>
        private static void StartCore(IAssetRuntime assetRuntime)
        {
            try
            {
                var type = Type.GetType(CoreStartupTypeName, throwOnError: false);
                if (type == null)
                {
                    GameLog.Error("[GameRestart] CoreStartup type not found", "Boot.GameRestart");
                    return;
                }

                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    GameLog.Error("[GameRestart] CoreStartup.Start not found", "Boot.GameRestart");
                    return;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(assetRuntime))
                    method.Invoke(null, new object[] { assetRuntime });
                else
                    GameLog.Error("[GameRestart] CoreStartup.Start signature unsupported", "Boot.GameRestart");
            }
            catch (Exception e)
            {
                var cause = e is TargetInvocationException tie ? tie.InnerException ?? tie : e;
                GameLog.Exception(cause, "[GameRestart] CoreStartup.Start failed", "Boot.GameRestart");
            }
        }

        /// <summary>
        /// 硬重启（进程外）。Editor 下回退为软重启，便于调试。
        /// </summary>
        public static void HardRestart(string appVer = null, string resVer = null, string reason = null)
        {
#if UNITY_EDITOR
            SoftRestart();
            return;
#else
            HardRestartAsync(appVer, resVer, reason).Forget();
#endif
        }

        private static async UniTaskVoid HardRestartAsync(string appVer, string resVer, string reason)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            RestartOutAppAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
            Application.Quit();
            // TODO: iOS 重启地址（参考项目用 Application.OpenURL 拉起）。
#else
            Application.Quit();
#endif
            // 平台差异的强行退出保底，避免造成卡住错觉。
            await UniTask.Delay(1000);
            Application.Quit();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void RestartOutAppAndroid()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            const int K_INTENT_FLAG_ACTIVITY_CLEAR_TASK = 0x00008000;
            const int K_INTENT_FLAG_ACTIVITY_NEW_TASK = 0x10000000;

            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
            var intent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", Application.identifier);
            intent.Call<AndroidJavaObject>("setFlags",
                K_INTENT_FLAG_ACTIVITY_NEW_TASK | K_INTENT_FLAG_ACTIVITY_CLEAR_TASK);
            currentActivity.Call("startActivity", intent);
            currentActivity.Call("finish");

            var process = new AndroidJavaClass("android.os.Process");
            var pid = process.CallStatic<int>("myPid");
            process.CallStatic("killProcess", pid);
        }
#endif

        private static void Release()
        {
            if (_bg != null)
            {
                Object.Destroy(_bg);
                _bg = null;
            }
        }
    }
}
