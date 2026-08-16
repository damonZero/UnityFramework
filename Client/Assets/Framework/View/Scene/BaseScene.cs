// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// ********************************************************************

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
namespace Framework.View
{
    public class BaseScene : ViewBase
    {
        /// <summary>
        /// 是否为激活场景
        /// the active Scene is the Scene which will be used as the target for new GameObjects instantiated by scripts and from what Scene the lighting settings are used.
        /// https://docs.unity3d.com/2018.4/Documentation/ScriptReference/SceneManagement.SceneManager.SetActiveScene.html
        /// </summary>
        public bool IsActiveScene { get; private set; }

        /// <summary>
        /// 所属Unity场景
        /// </summary>
        public Scene UnityScene => gameObject.scene;

        /// <summary>
        /// 缓存的光照贴图
        /// </summary>
        public LightmapData[] CachedLightmaps { get; protected set; }

        /// <summary>
        /// 界面的逻辑可见性控制器
        /// </summary>
        public static VisibleController SceneLogicalVisibleController { get; } = new(
            nameof(SceneLogicalVisibleController), SceneVisibleStrategyByRootGameObjects.Shared);

        public override VisibleController LogicalVisibleController => SceneLogicalVisibleController;

        /// <summary>
        /// 缓存的根节点active状态
        /// </summary>
        protected Dictionary<GameObject, bool> _cachedRootObjStates;


        #region 生命周期方法

        protected override void OnViewAwake()
        {
            AssetName = gameObject.scene.name;
            OnSceneAwake();
        }

        protected virtual void OnSceneAwake()
        {
        }

        protected override void OnViewDestroy()
        {
            OnSceneDestroy();
        }

        protected virtual void OnSceneDestroy()
        {
        }

        #endregion

        #region public

        /// <summary>
        /// 设置此场景的光照贴图
        /// </summary>
        /// <param name="lightmaps"></param>
        public void SetLightmap(LightmapData[] lightmaps)
        {
            if (IsActiveScene)
            {
                LightmapSettings.lightmaps = lightmaps;
            }
            else
            {
                CachedLightmaps = lightmaps;
            }
        }

        #endregion

        #region internal

        /// <summary>
        /// 变为非active场景前调用
        /// </summary>
        internal UniTask BeforeLoseActive()
        {
            IsActiveScene = false;
            CachedLightmaps = LightmapSettings.lightmaps;
            LightmapSettings.lightmaps = null;
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 变为active场景后调用
        /// </summary>
        internal UniTask AfterGainActive()
        {
            IsActiveScene = true;
            if (CachedLightmaps != null)
            {
                LightmapSettings.lightmaps = CachedLightmaps;
                CachedLightmaps = null;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 启用所有的根节点（还原到禁用之前的状态，如果本来就是禁用的则保持禁用）
        /// </summary>
        internal void EnableRootGameObjects()
        {
            if (_cachedRootObjStates == null) return;

            var list = CollectionPool<List<GameObject>, GameObject>.Get();
            UnityScene.GetRootGameObjects(list);

            foreach (var rootGameObject in list)
            {
                if (rootGameObject == null) continue;

                if (_cachedRootObjStates.TryGetValue(rootGameObject, out var originState))
                {
                    Log.Debug($"{nameof(OnShow)} : SetActive({rootGameObject}, {originState})");
                    rootGameObject.SetActive(originState);
                }
            }

            CollectionPool<List<GameObject>, GameObject>.Release(list);
        }

        /// <summary>
        /// 禁用所有的根节点
        /// </summary>
        internal void DisableRootGameObjects()
        {
            var list = CollectionPool<List<GameObject>, GameObject>.Get();

            UnityScene.GetRootGameObjects(list);

            _cachedRootObjStates ??= new Dictionary<GameObject, bool>(list.Count);

            // 记录根节点状态并disable
            foreach (var rootGameObject in list)
            {
                if (rootGameObject == null) continue;
                _cachedRootObjStates[rootGameObject] = rootGameObject.activeSelf;
                rootGameObject.SetActive(false);
            }

            CollectionPool<List<GameObject>, GameObject>.Release(list);
        }

        #endregion

        #region 其它

        protected override IVisibleStrategy CreateDefaultVisibleStrategy()
        {
            // 默认采用“仅影响渲染输出”的安全预设，不改 Camera.enabled。
            return SceneVisibleStrategyByCameras.CreateRenderSafePreset();
        }


        public override string ToString()
        {
            return $"{GetType().Name}({AssetName}, IsActiveScene:{IsActiveScene}, " +
                   $"CurrentPhase:{CurrentPhase}, PendingPhase:{PendingPhase})";
        }

        #endregion
    }
}
