//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航界面的加载器
//@Description 负责实现导航生命周期，以及与Scene的交互
//**************************************************************************************

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.View;
using Framework.View.Navigation;
namespace Framework.View.Navigation
{
    public class NavigationSceneLoader : NavigationLoader
    {
        /// <summary>
        /// 场景对象
        /// </summary>
        public BaseScene Scene => View as BaseScene;

        /// <summary>
        /// 场景的加载和开启参数
        /// </summary>
        public NavigateSceneOptions SceneOptions { get; set; }

        /// <summary>
        /// 实现 INavigateOptions.ViewOptions
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public override INavigateOptions ViewOptions
        {
            get => SceneOptions;
            internal set
            {
                if (value is NavigateSceneOptions options)
                {
                    SceneOptions = options;
                }
                else
                {
                    throw new ArgumentException(
                        $"{nameof(ViewOptions)} must be {nameof(NavigateSceneOptions)}, but got {value}");
                }
            }
        }

        /// <summary>
        /// 加载内存
        /// </summary>
        public override int Memory { get; protected set; } = 30;

        /// <summary>
        /// 实现 <see cref="NavigationLoader.Layer"/>：场景无排序层级概念，固定返回 <see cref="int.MinValue"/>.
        /// Scene has no layer concept; returns <see cref="int.MinValue"/> so all Forms rank above it.
        /// </summary>
        public override int Layer => int.MinValue;


        /// <summary>
        /// 是否逻辑可见
        /// </summary>
        public override bool LogicalVisible => Scene is { LogicalVisible: true };

        /// <summary>
        /// 是否渲染
        /// </summary>
        public override bool Rendering => View != null && Scene.Rendering;

        // /// <summary>
        // /// 是否执行过 OnOpen
        // /// </summary>
        // private bool _opened;

        // /// <summary>
        // /// NavigationLoader：加载场景
        // /// </summary>
        // /// <returns></returns>
        // protected override async UniTask<BaseView> OpenAsync(CancellationToken cancellationToken = default)
        // {
        //     try
        //     {
        //         if (!VerifyOperate(NavigationStateType.Open, true)) return null;
        //
        //         // FIXME by fred 这里状态改为Opening??
        //         BeforeChangeState(NavigationStateType.Open);
        //
        //         var openScene = await NavigateUtils.LoadSceneAsync(Name, loader =>
        //         {
        //             SceneLoader = loader;
        //             NavigateLog.Log($"[{nameof(NavigationSceneLoader)}.{nameof(OpenAsync)}] " +
        //                        $"SceneLoader:{SceneLoader}, this:{this}");
        //             // loader.OpenData = data ?? OpenData;
        //             // loader.ShowData = data ?? ShowData;
        //             loader.LuaScriptOpen = LuaScriptOpen;
        //             loader.LuaScriptShow = LuaScriptShow;
        //             loader.SceneUnloaded += OnCloseEnd;
        //         });
        //         if (openScene == null)
        //             throw new NavigationOpenException($"打开场景失败:{Name}");
        //
        //         ChangeState(NavigationStateType.Open);
        //     }
        //     catch (Exception e) when (e is not OperationCanceledException)
        //     {
        //         OnError(e);
        //     }
        //
        //     var scene = SceneLoader?.Scene;
        //
        //     if (scene is BaseScene view)
        //     {
        //         View = view;
        //         return view;
        //     }
        //
        //     NavigateLog.LogError($"导航加载器打开'{Name}'失败，{scene} 应该继承自 {nameof(BaseScene)}");
        //     return null;
        // }

        // /// <summary>
        // /// NavigationLoader：显示场景
        // /// </summary>
        // /// <param name="data">任意类型参数</param>
        // /// <returns></returns>
        // public override async UniTask<bool> ShowAsync(object data = null)
        // {
        //     try
        //     {
        //         VerifyLuaScene();
        //         if (!VerifyOperate(NavigationStateType.Show, true)) return true;
        //         BeforeChangeState(NavigationStateType.Show);
        //         ShowData = data ?? ShowData;
        //         NavigateUtils.ShowScene(SceneLoader, ShowData);
        //         ChangeState(NavigationStateType.Show);
        //         return true;
        //     }
        //     catch (Exception e)
        //     {
        //         OnError(e);
        //         return false;
        //     }
        // }

        // /// <summary>
        // /// NavigationLoader：隐藏场景
        // /// </summary>
        // /// <returns></returns>
        // public override bool Hide()
        // {
        //     try
        //     {
        //         VerifyLuaScene();
        //         AssertOperate(NavigationStateType.Hide);
        //
        //         BeforeChangeState(NavigationStateType.Hide);
        //
        //         NavigateUtils.HideScene(SceneLoader);
        //
        //         ChangeState(NavigationStateType.Hide);
        //
        //         return true;
        //     }
        //     catch (Exception e)
        //     {
        //         OnError(e);
        //         return false;
        //     }
        //
        //     // return TryCall<NavigationLifecycleException>(() =>
        //     // {
        //     //     VerifyLuaScene();
        //     //     if (!VerifyOperate(NavigationStateType.Hide, true)) return;
        //     //     BeforeChangeState(NavigationStateType.Hide);
        //     //     NavigateUtils.HideScene(SceneLoader);
        //     //     ChangeState(NavigationStateType.Hide);
        //     // });
        // }

        // /// <summary>
        // /// NavigationLoader：关闭场景
        // /// </summary>
        // /// <returns></returns>
        // public override bool Close()
        // {
        //     try
        //     {
        //         VerifyLuaScene();
        //         CloseBegin();
        //
        //         var success = NavigateUtils.UnloadScene(SceneLoader, out var addCache);
        //         if (success && !addCache) return true;
        //
        //         SceneLoader.SceneUnloaded -= OnCloseEnd;
        //         OnCloseEnd(SceneLoader);
        //         return true;
        //     }
        //     catch (Exception e)
        //     {
        //         OnError(e);
        //         return false;
        //     }
        //
        //     // return TryCall<NavigationLifecycleException>(() =>
        //     // {
        //     //     VerifyLuaScene();
        //     //     CloseBegin();
        //     //     var success = NavigateUtils.UnloadScene(SceneLoader, out var addCache);
        //     //     if (success && !addCache) return;
        //     //     SceneLoader.SceneUnloaded -= OnCloseEnd;
        //     //     OnCloseEnd(SceneLoader);
        //     // });
        // }

        // protected override bool CloseView()
        // {
        //     // CloseBegin();
        //
        //     var success = NavigateUtils.UnloadScene(SceneLoader, out var addCache);
        //     if (success && !addCache) return false;
        //
        //     SceneLoader.SceneUnloaded -= OnCloseEnd;
        //     return true;
        //     // OnCloseEnd(SceneLoader);
        // }

        // /// <summary>
        // /// NavigationLoader：清理场景
        // /// </summary>
        // /// <returns></returns>
        // public override bool Clear()
        // {
        //     try
        //     {
        //         VerifyLuaScene();
        //         AssertOperate(NavigationStateType.Clear);
        //
        //         BeforeChangeState(NavigationStateType.Clear);
        //
        //         SceneLoader.Scene.Clear();
        //
        //         ChangeState(NavigationStateType.Clear);
        //         return true;
        //     }
        //     catch (Exception e)
        //     {
        //         OnError(e);
        //         return false;
        //     }
        //
        //     // return TryCall<NavigationLifecycleException>(() =>
        //     // {
        //     //     VerifyLuaScene();
        //     //     if (!VerifyOperate(NavigationStateType.Clear, true)) return;
        //     //     BeforeChangeState(NavigationStateType.Clear);
        //     //     SceneLoader.Scene.Clear();
        //     //     ChangeState(NavigationStateType.Clear);
        //     // });
        // }

        // /// <summary>
        // /// NavigationLoader：保存数据，用于还原
        // /// </summary>
        // /// <returns></returns>
        // internal override object Save()
        // {
        //     try
        //     {
        //         VerifyLuaScene();
        //         return SceneLoader.Scene.Save();
        //     }
        //     catch (Exception e)
        //     {
        //         OnError(e);
        //         return null;
        //     }
        //
        //     // var (_, saveData) = TryCall(() =>
        //     // {
        //     //     VerifyLuaScene();
        //     //     return SceneLoader.Scene.Save();
        //     // });
        //     // return saveData;
        // }

        /// <summary>
        /// 是否为全屏
        /// </summary>
        /// <returns></returns>
        public override bool IsFullScreen()
        {
            return true;
        }

        /// <summary>
        /// 是否全屏且逻辑可见
        /// </summary>
        public override bool FullScreenAndLogicalVisible()
        {
            return LogicalVisible;
        }

        /// <summary>
        /// 是否全屏且正在渲染
        /// </summary>
        public override bool FullScreenAndRendering()
        {
            return Rendering;
        }

        public override string ToString()
        {
            if (Scene == null)
                return base.ToString();
            return
                $"{base.ToString()}, isLoaded:{Scene.UnityScene.isLoaded}, Entrance:{Entrance}";
        }

        // public override void Reset()
        // {
        //     base.Reset();
        //     // SceneOptions.Reset();
        //     // _opened = false;
        //     // if (SceneLoader == null) return;
        //     // SceneLoader.SceneUnloaded -= OnCloseEnd;
        //     // SceneLoader = null;
        //     NavigateLog.Log($"[{nameof(NavigationSceneLoader)}.{nameof(Reset)}] SceneLoader:{SceneLoader}, this:{this}");
        // }

        // /// <summary>
        // /// NavigationLoader：复制Loader信息
        // /// </summary>
        // /// <param name="loader"></param>
        // internal override void CopyInfo(NavigationLoader loader)
        // {
        //     if (!(loader is NavigationSceneLoader copyLoader))
        //         return;
        //     OpenData = copyLoader.OpenData;
        //     ShowData = copyLoader.ShowData;
        //     Mode = copyLoader.Mode;
        // }

        // /// <summary>
        // /// 激活LuaScene脚本
        // ///
        // /// LoadScene和ShowScene都可能触发此函数
        // /// </summary>
        // /// <param name="scene"></param>
        // private void LuaScriptOpen(IScene scene)
        // {
        //     if (SceneLoader == null)
        //         throw new NavigationException($"SceneLoader:{Name} is null");
        //
        //     scene.AssetName = ViewOptions.AssetName;
        //     // UnityEngine.Debug.Log($"[{nameof(NavigationSceneLoader)}.{nameof(LuaScriptOpen)}] " +
        //     //                       $"AssetName: {ViewOptions.AssetName}");
        //     // SceneLoader.Scene.Loader = this;
        //     if (!Transitioning)
        //     {
        //         MaybeOnOpen();
        //         return;
        //     }
        //
        //     var openFirst = TransitionType.HasFlag(NavigationTransitionType.AfterOpen);
        //     var endTransitionNow = !TransitionType.HasFlag(NavigationTransitionType.Custom);
        //
        //     if (openFirst)
        //     {
        //         // 先执行 OnOpen
        //         MaybeOnOpen();
        //     }
        //
        //     if (endTransitionNow)
        //     {
        //         // 再结束转场
        //         EndTransition();
        //     }
        // }

        // /// <summary>
        // /// 检查是否要执行OnOpen
        // /// </summary>
        // protected override void MaybeOnOpen()
        // {
        //     if (_opened || SceneLoader == null || !SceneLoader.Scene.Mono) return;
        //
        //     _opened = true;
        //     SceneLoader.Scene.OnOpen(OpenData);
        // }

        //Lua脚本Show
        // private void LuaScriptShow(IScene luaScene)
        // {
        //     luaScene.Show(ShowData, ++ShowTimes == 1);
        // }

        // internal override void CloseEnd()
        // {
        //     _opened = false;
        //     base.CloseEnd();
        // }
        //
        // //当场景卸载完成(针对父类SceneLoader封装)
        // internal void OnCloseEnd(ISceneLoader loader)
        // {
        //     CloseEnd();
        // }
        //
        // //验证LuaForm是否为空
        // private void VerifyLuaScene()
        // {
        //     if (SceneLoader == null)
        //         throw new NavigationException($"SceneLoader:{Name} is null");
        // }
    }
}
