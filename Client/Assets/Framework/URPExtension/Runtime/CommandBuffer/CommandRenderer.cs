using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.URPExtension
{
    /// <summary>替换材质的索引。</summary>
    [Serializable]
    public struct ReplaceMaterialPair
    {
        public Renderer renderer;
        public int subMeshIndex;
        public Material material; // 默认使用接管材质
    }

    /// <summary>CommandBuffer 渲染器：接管子节点 Renderer 的材质在指定时机绘制。对应参考项目 Package/URPExtension/CommandBuffer/CommandRenderer.cs。</summary>
    [ExecuteAlways]
    public class CommandRenderer : MonoBehaviour
    {
        [Header("渲染时机")] public RenderPassEvent evt = RenderPassEvent.AfterRenderingTransparents;
        [Header("渲染层级")] public LayerMask layerMask;
        [Header("接管材质")] public Material material;
        [Header("是否隐藏原始材质")] public bool hideRender;
        [Header("使用原始材质")] public bool useOrgMat;

        [Header("被接管的材质索引（不设置则全部替换）"), SerializeField]
        private List<ReplaceMaterialPair> _replaceSubMeshMaterialList;

        private CommandBufferPass _pass;
        private CommandBuffer _cmd;
        private readonly Dictionary<Renderer, Material[]> _renderDict = new();

        public static readonly Material[] EmptyMats = Array.Empty<Material>();

        /// <summary>缓存列表（单次 IsRender 判定复用）。</summary>
        private readonly List<Material> _cacheList = new List<Material>();

        private void OnEnable()
        {
            if (material == null && _replaceSubMeshMaterialList?.Count <= 0 && !useOrgMat) return;
#if !UNITY_EDITOR
            if (_pass == null)
#endif
                Init();
            if (_pass == null) return;
            CustomRenderFeature.Show(_pass, gameObject.layer);
        }

        private void OnDisable()
        {
            if (_pass == null) return;
            CustomRenderFeature.Hide(_pass);
            foreach (var (r, mats) in _renderDict)
            {
                if (r == null) continue;
                r.enabled = true;
                r.materials = mats;
            }

            _renderDict.Clear();
            _cacheList.Clear();
        }

        /// <summary>某个 Renderer 的第几个 SubMesh 是否参与接管。</summary>
        private bool IsRender(Renderer r, int subMesh)
        {
            _cacheList.Clear();
            if (_replaceSubMeshMaterialList == null || _replaceSubMeshMaterialList.Count == 0)
                return true;

            foreach (var pair in _replaceSubMeshMaterialList)
            {
                if (pair.renderer == r && pair.subMeshIndex == subMesh)
                    _cacheList.Add(pair.material);
            }

            return _cacheList.Count > 0;
        }

        private void Init()
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;

            _pass = new CommandBufferPass(evt);
            _cmd = new CommandBuffer();
            _pass.AddCommand(_cmd);
            _renderDict.Clear();

            foreach (var r in rs)
            {
                if ((layerMask & (1 << gameObject.layer)) == 0) return;

                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    if (useOrgMat)
                    {
                        if (mats[i]) _cmd.DrawRenderer(r, mats[i], i);
                    }
                    else
                    {
                        if (!IsRender(r, i)) continue;

                        if (_cacheList.Count == 0)
                        {
                            var mat = material;
                            if (mat) _cmd.DrawRenderer(r, mat, i);
                        }
                        else
                        {
                            foreach (var t in _cacheList)
                            {
                                if (t) _cmd.DrawRenderer(r, t, i);
                            }
                        }
                    }
                }

                if (hideRender && r.enabled)
                {
                    _renderDict[r] = mats;
                    if (useOrgMat) r.materials = EmptyMats; // 清空材质以不渲染
                    else r.enabled = false;
                }
            }
        }

        /// <summary>方便外部调用初始化，不必设置是否可见来触发 OnEnable。</summary>
        public void ReInit()
        {
            OnDisable();
            _pass = null;
            OnEnable();
        }

        public void SetLayerMask(int layer)
        {
            layerMask = 1 << layer;
        }
    }
}
