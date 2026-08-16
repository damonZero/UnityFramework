// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// ********************************************************************

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Framework.View
{
    public interface ISceneManager : IViewManager
    {
        /// <summary>
        /// 场景完成所有准备工作，变为激活状态（新建的物体会自动放到此场景下、光照贴图会使用此场景的）
        /// the active Scene is the Scene which will be used as the target for new GameObjects instantiated by scripts and from what Scene the lighting settings are used.
        /// https://docs.unity3d.com/2018.4/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html
        /// </summary>
        event Action<BaseScene> SceneActive;

        /// <summary>
        /// 调用卸载场景时的回调(调用之后,可能缓存,也可能立即销毁)
        /// </summary>
        event Action<Scene> SceneUnloaded;

        /// <summary>
        /// 查找GameObject所在场景对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        BaseScene FindScene(GameObject obj);

        /// <summary>
        /// 根据Unity Scene查找场景对象
        /// </summary>
        /// <param name="unityScene"></param>
        /// <returns></returns>
        BaseScene FindScene(Scene unityScene);

        /// <summary>
        /// 根据名字查找场景对象
        ///
        /// 注意：不排除同时存在多个同名场景的情况，这里只会返回一个
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        BaseScene FindScene(string sceneName);

        /// <summary>
        /// 当前激活的场景（新建的物体会自动放到此场景下、光照贴图会使用此场景的）
        /// </summary>
        BaseScene ActiveScene { get; }

        /// <summary>
        /// 是否有任何场景处于渲染状态
        /// </summary>
        /// <returns></returns>
        bool HasRenderingScene();
    }
}
