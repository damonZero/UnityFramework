using System;
using System.Collections.Generic;
using Framework.Log;
using Framework.URPExtension;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.URPSceneEffect
{
    /// <summary>
    /// 基于 URP 的平面阴影（Feature/CommonBuffer 两模式）。对应参考项目 Package/URPSceneEffect/Runtime/PlanarShadow.cs。
    /// 需配合平面阴影 shader（<see cref="shadowShader"/>）使用。
    /// </summary>
    [ExecuteAlways]
    public class PlanarShadow : MonoBehaviour
    {
        private static readonly int _shadowColId = Shader.PropertyToID("_ShadowColor");
        private static readonly int _shadowPlaneId = Shader.PropertyToID("_ShadowPlane");
        private static readonly int _shadowProjDirId = Shader.PropertyToID("_ShadowProjDir");
        private static readonly int _offsetFactorId = Shader.PropertyToID("_OffsetFactor");
        private static readonly int _shadowOffsetId = Shader.PropertyToID("_ShadowOffset");

        public enum ShadowEnum
        {
            Feature,
            CommonBuffer
        }

        [Header("构建类型")] public ShadowEnum type = ShadowEnum.Feature;
        [Header("阴影shader")] public Shader shadowShader;
        [Header("接管队列")] public RenderQueueType renderQueueType = RenderQueueType.Transparent;
        [Header("渲染时机")] public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        [Header("投射阴影层")] public LayerMask layerMask;
        [Header("阴影颜色")] public Color shadowCol = Color.black;
        [Header("深度偏移")] public int OffsetFactor = -1;
        [Header("是否基于世界坐标")] public bool isWorldPos = true;
        [Header("阴影平面，w平面高度")] public Vector4 ShadowPlane = new Vector4(0, 1, 0, 0);
        [Header("阴影平面高度跟随目标")] public Transform FollowTargetTrans;
        [Header("阴影偏移")] public Vector3 ShadowOffset = new Vector3(0, 0, 0);
        [Header("阴影方向")] public Vector3 ShadowProjDir = new Vector3(0.4f, -0.5f, -0.7f);

        private Material _overrideMaterial;
        private ScriptableRenderPass _pass;
        private CommandBuffer _cmd;
        private readonly List<Renderer> _list = new List<Renderer>(40);

        private void OnEnable()
        {
            if (_pass == null || !Application.isPlaying)
            {
                _overrideMaterial = new Material(shadowShader) { hideFlags = HideFlags.HideAndDontSave };
                _pass = type switch
                {
                    ShadowEnum.Feature => InitFeaturePass(),
                    ShadowEnum.CommonBuffer => InitCommandBufferPass(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                InitMat();
            }

            CustomRenderFeature.Show(_pass, gameObject.layer);
        }

        private void Update()
        {
#if UNITY_EDITOR
            InitMat();
#endif
            if (FollowTargetTrans)
                SetShadowHeight(FollowTargetTrans.position.y);
        }

        private void OnDisable()
        {
            CustomRenderFeature.Hide(_pass);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying) Destroy(_overrideMaterial);
            else DestroyImmediate(_overrideMaterial);
        }

        /// <summary>单个物件添加阴影（受 layerMask 影响，仅 CommonBuffer 模式）。</summary>
        public void AddShadow(GameObject obj, bool includeInactive = false)
        {
            if (type != ShadowEnum.CommonBuffer)
            {
                GameLog.Error($"==={type}不支持 AddShadow======", nameof(PlanarShadow));
                return;
            }

            if (obj == null) return;
            var renders = obj.GetComponentsInChildren<Renderer>(includeInactive);
            foreach (var r in renders)
            {
                if ((layerMask & (1 << r.gameObject.layer)) == 0) return;
                if (_list.Contains(r)) continue;

                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    _cmd.DrawRenderer(r, _overrideMaterial, i);
                }

                _list.Add(r);
            }
        }

        /// <summary>单个物件移除阴影。</summary>
        public void RemoveShadow(GameObject obj, bool includeInactive = false)
        {
            if (obj == null) return;
            var renders = obj.GetComponentsInChildren<Renderer>(includeInactive);
            foreach (var r in renders)
            {
                _list.Remove(r);
            }

            FlushShadow();
        }

        /// <summary>刷新阴影，并清理被销毁渲染器。</summary>
        public void FlushShadow()
        {
            _cmd.Clear();
            for (var j = _list.Count - 1; j >= 0; j--)
            {
                var r = _list[j];
                if (r == null)
                {
                    _list.RemoveAt(j);
                    continue;
                }

                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    _cmd.DrawRenderer(r, _overrideMaterial, i);
                }
            }
        }

        /// <summary>设置阴影高度。</summary>
        public void SetShadowHeight(float height)
        {
            ShadowPlane.w = height;
            _overrideMaterial.SetVector(_shadowPlaneId, ShadowPlane);
        }

        /// <summary>设置阴影 alpha。</summary>
        public void SetShadowAlpha(float alpha)
        {
            shadowCol.a = alpha;
            _overrideMaterial.SetColor(_shadowColId, shadowCol);
        }

        private ScriptableRenderPass InitCommandBufferPass()
        {
            var pass = new CommandBufferPass(renderPassEvent);
            _cmd = new CommandBuffer();
            pass.AddCommand(_cmd);
            return pass;
        }

        private ScriptableRenderPass InitFeaturePass()
        {
            var settings = new RenderObjects.CustomCameraSettings();
            // 注意：RenderObjectsPass 构造函数已按 renderQueueType 设置内部 FilteringSettings（m_FilteringSettings 为 private，14.0.12 无法再外部覆写）
            var pass = new RenderObjectsPass("", renderPassEvent, null, renderQueueType, layerMask, settings)
            {
                overrideMaterial = _overrideMaterial
            };
            return pass;
        }

        private void InitMat()
        {
            _overrideMaterial.SetColor(_shadowColId, shadowCol);
            _overrideMaterial.SetVector(_shadowProjDirId, ShadowProjDir);
            _overrideMaterial.SetVector(_shadowPlaneId, ShadowPlane);
            _overrideMaterial.SetFloat(_offsetFactorId, OffsetFactor);
            _overrideMaterial.SetVector(_shadowOffsetId, ShadowOffset);
            if (isWorldPos) _overrideMaterial.EnableKeyword("WORLD_POS");
            else _overrideMaterial.DisableKeyword("WORLD_POS");
        }
    }
}
